using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using WindowsAssetInventory.Data;
using WindowsAssetInventory.Services;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 3 * 1024 * 1024;
});
var configuredConnectionString = builder.Configuration.GetConnectionString("Inventory")
    ?? throw new InvalidOperationException("Connection string 'Inventory' is required.");
var connectionString = DatabasePath.ResolveConnectionString(
    configuredConnectionString,
    builder.Environment.ContentRootPath);

builder.Services.AddDbContext<InventoryDbContext>(options => options.UseSqlite(connectionString));
builder.Services.AddScoped<AssetAssignmentPolicy>();
builder.Services.AddSingleton<CsvAssetService>();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = context =>
    {
        context.ProblemDetails.Extensions["traceId"] = context.HttpContext.TraceIdentifier;
    };
});
builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new()
    {
        Title = "Windows Asset Inventory API",
        Version = "v1",
        Description = "Company hardware, assignment, maintenance, and software inventory API."
    });
});
builder.Services
    .AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>("sqlite");

var app = builder.Build();

app.UseExceptionHandler();
app.Use(async (context, next) =>
{
    context.Response.Headers.XContentTypeOptions = "nosniff";
    context.Response.Headers.XFrameOptions = "DENY";
    context.Response.Headers["Referrer-Policy"] = "no-referrer";
    await next();
});
app.UseDefaultFiles();
app.UseStaticFiles();
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.DocumentTitle = "Windows Asset Inventory API";
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Inventory API v1");
});

app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        await JsonSerializer.SerializeAsync(
            context.Response.Body,
            new
            {
                status = report.Status.ToString(),
                checks = report.Entries.ToDictionary(
                    entry => entry.Key,
                    entry => entry.Value.Status.ToString())
            },
            cancellationToken: context.RequestAborted);
    }
});
app.MapControllers();
app.MapFallbackToFile("index.html");

await using (var scope = app.Services.CreateAsyncScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
    await dbContext.Database.MigrateAsync();
    await DatabaseSeeder.SeedAsync(dbContext);
}

app.Run();

public partial class Program;
