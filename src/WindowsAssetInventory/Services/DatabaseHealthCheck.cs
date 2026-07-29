using Microsoft.Extensions.Diagnostics.HealthChecks;
using WindowsAssetInventory.Data;

namespace WindowsAssetInventory.Services;

public sealed class DatabaseHealthCheck(IServiceScopeFactory scopeFactory) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
        return await dbContext.Database.CanConnectAsync(cancellationToken)
            ? HealthCheckResult.Healthy("SQLite database is reachable.")
            : HealthCheckResult.Unhealthy("SQLite database is not reachable.");
    }
}
