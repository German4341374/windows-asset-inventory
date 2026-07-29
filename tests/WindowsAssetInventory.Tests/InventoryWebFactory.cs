using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using WindowsAssetInventory.Data;

namespace WindowsAssetInventory.Tests;

public sealed class InventoryWebFactory : WebApplicationFactory<Program>
{
    private readonly string _databasePath =
        Path.Combine(Path.GetTempPath(), $"inventory-tests-{Guid.NewGuid():N}.db");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Inventory"] = $"Data Source={_databasePath}"
            });
        });
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<InventoryDbContext>>();
            services.AddDbContext<InventoryDbContext>(
                options => options.UseSqlite($"Data Source={_databasePath}"));
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (!disposing)
        {
            return;
        }

        TryDelete(_databasePath);
        TryDelete($"{_databasePath}-shm");
        TryDelete($"{_databasePath}-wal");
    }

    private static void TryDelete(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch (IOException)
        {
            // The operating system will clean its temporary directory.
        }
        catch (UnauthorizedAccessException)
        {
            // The operating system will clean its temporary directory.
        }
    }
}
