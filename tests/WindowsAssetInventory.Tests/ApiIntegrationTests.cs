using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using WindowsAssetInventory.Contracts;
using WindowsAssetInventory.Models;

namespace WindowsAssetInventory.Tests;

public sealed class ApiIntegrationTests(InventoryWebFactory factory)
    : IClassFixture<InventoryWebFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = CreateJsonOptions();
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Health_ReturnsHealthyDatabaseCheck()
    {
        var response = await _client.GetAsync("/health", CancellationToken.None);

        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadAsStringAsync(CancellationToken.None);
        Assert.Contains("\"status\":\"Healthy\"", body, StringComparison.Ordinal);
        Assert.Contains("\"sqlite\":\"Healthy\"", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Assets_ReturnsSeededInventory()
    {
        var assets = await _client.GetFromJsonAsync<AssetDto[]>(
            "/api/assets",
            JsonOptions,
            CancellationToken.None);

        Assert.NotNull(assets);
        Assert.True(assets.Length >= 12);
        Assert.Contains(assets, asset => asset.AssetTag == "LT-1001");
    }

    [Fact]
    public async Task Assets_SearchesByAssignedUser()
    {
        var assets = await _client.GetFromJsonAsync<AssetDto[]>(
            "/api/assets?search=Alex%20Morgan",
            JsonOptions,
            CancellationToken.None);

        Assert.NotNull(assets);
        Assert.NotEmpty(assets);
        Assert.All(assets, asset => Assert.Equal("Alex Morgan", asset.AssignedUserName));
    }

    [Fact]
    public async Task CreateAsset_ValidRequest_ReturnsCreatedAsset()
    {
        var tag = $"TEST-{Guid.NewGuid():N}"[..13];
        var response = await _client.PostAsJsonAsync("/api/assets", new
        {
            assetTag = tag,
            type = "Laptop",
            manufacturer = "Framework",
            model = "Laptop 13",
            serialNumber = $"SERIAL-{tag}",
            hostname = $"HOST-{tag}",
            status = "In Stock",
            purchaseDate = "2026-01-10",
            warrantyUntil = "2029-01-10"
        }, CancellationToken.None);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var asset = await response.Content.ReadFromJsonAsync<AssetDto>(
            JsonOptions,
            CancellationToken.None);
        Assert.NotNull(asset);
        Assert.Equal(tag, asset.AssetTag);
        Assert.Equal(AssetStatus.InStock, asset.Status);
    }

    [Fact]
    public async Task CreateAsset_InvalidWarranty_ReturnsValidationProblem()
    {
        var response = await _client.PostAsJsonAsync("/api/assets", new
        {
            assetTag = "BAD-DATE",
            type = "Monitor",
            manufacturer = "Example",
            model = "Panel",
            serialNumber = "BAD-DATE-SERIAL",
            status = "In Stock",
            purchaseDate = "2026-10-01",
            warrantyUntil = "2026-01-01"
        }, CancellationToken.None);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(
            "application/problem+json",
            response.Content.Headers.ContentType?.MediaType);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(
            cancellationToken: CancellationToken.None);
        Assert.NotNull(problem);
        Assert.Contains("WarrantyUntil", problem.Errors.Keys);
    }

    [Fact]
    public async Task Assign_AlreadyAssignedAsset_ReturnsConflictProblem()
    {
        var assets = await _client.GetFromJsonAsync<AssetDto[]>(
            "/api/assets?search=LT-1001",
            JsonOptions,
            CancellationToken.None);
        var assignedAsset = Assert.Single(assets!);

        var response = await _client.PostAsJsonAsync(
            $"/api/assets/{assignedAsset.Id}/assign",
            new { userId = 2 },
            CancellationToken.None);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(
            cancellationToken: CancellationToken.None);
        Assert.Equal("Business rule conflict", problem?.Title);
    }

    [Fact]
    public async Task Dashboard_ReturnsExpectedStatusTotals()
    {
        var dashboard = await _client.GetFromJsonAsync<DashboardDto>(
            "/api/dashboard",
            CancellationToken.None);

        Assert.NotNull(dashboard);
        Assert.True(dashboard.TotalAssets >= 12);
        Assert.True(dashboard.Assigned > 0);
        Assert.True(dashboard.InStock > 0);
        Assert.Equal(6, dashboard.TotalUsers);
    }

    [Fact]
    public async Task WarrantyExpiring_ReturnsOnlyActiveUpcomingWarranties()
    {
        var assets = await _client.GetFromJsonAsync<AssetDto[]>(
            "/api/assets/warranty-expiring?days=30",
            JsonOptions,
            CancellationToken.None);

        Assert.NotNull(assets);
        Assert.NotEmpty(assets);
        Assert.DoesNotContain(assets, asset => asset.Status == AssetStatus.Retired);
    }

    [Fact]
    public async Task CsvExport_ReturnsInventoryFile()
    {
        var response = await _client.GetAsync(
            "/api/assets/export-csv",
            CancellationToken.None);

        response.EnsureSuccessStatusCode();
        Assert.Equal("text/csv", response.Content.Headers.ContentType?.MediaType);
        var csv = await response.Content.ReadAsStringAsync(CancellationToken.None);
        Assert.Contains("AssetTag,Type,Manufacturer", csv, StringComparison.Ordinal);
        Assert.Contains("LT-1001", csv, StringComparison.Ordinal);
    }

    [Fact]
    public async Task CsvImport_AddsValidAsset()
    {
        var tag = $"CSV-{Guid.NewGuid():N}"[..12];
        var csv = $"""
            AssetTag,Type,Manufacturer,Model,SerialNumber,Hostname,Status,PurchaseDate,WarrantyUntil
            {tag},Laptop,Example,Portable 14,SERIAL-{tag},CSV-HOST,In Stock,2026-01-01,2029-01-01
            """;
        using var form = new MultipartFormDataContent();
        using var file = new ByteArrayContent(Encoding.UTF8.GetBytes(csv));
        file.Headers.ContentType = new MediaTypeHeaderValue("text/csv");
        form.Add(file, "file", "assets.csv");

        var response = await _client.PostAsync(
            "/api/assets/import-csv",
            form,
            CancellationToken.None);

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<CsvImportResultDto>(
            cancellationToken: CancellationToken.None);
        Assert.Equal(1, result?.Imported);
    }

    [Fact]
    public async Task Maintenance_CreateAndList_ReturnsNewRecord()
    {
        var assets = await _client.GetFromJsonAsync<AssetDto[]>(
            "/api/assets?search=PC-2003",
            JsonOptions,
            CancellationToken.None);
        var asset = Assert.Single(assets!);

        var created = await _client.PostAsJsonAsync("/api/maintenance", new
        {
            assetId = asset.Id,
            description = "Completed integration-test hardware diagnostics.",
            technician = "Test Technician"
        }, CancellationToken.None);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);

        var records = await _client.GetFromJsonAsync<MaintenanceRecordDto[]>(
            $"/api/maintenance?assetId={asset.Id}",
            CancellationToken.None);
        Assert.Contains(records!, record => record.Technician == "Test Technician");
    }

    private static JsonSerializerOptions CreateJsonOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }
}
