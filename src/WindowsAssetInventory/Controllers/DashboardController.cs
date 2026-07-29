using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WindowsAssetInventory.Contracts;
using WindowsAssetInventory.Data;
using WindowsAssetInventory.Models;

namespace WindowsAssetInventory.Controllers;

[ApiController]
[Route("api/dashboard")]
public sealed class DashboardController(InventoryDbContext dbContext) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<DashboardDto>> Get(CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var warrantyLimit = today.AddDays(30);
        var statusCounts = await dbContext.Assets
            .AsNoTracking()
            .GroupBy(asset => asset.Status)
            .Select(group => new { Status = group.Key, Count = group.Count() })
            .ToDictionaryAsync(item => item.Status, item => item.Count, cancellationToken);
        var typeCounts = await dbContext.Assets
            .AsNoTracking()
            .GroupBy(asset => asset.Type)
            .Select(group => new { Type = group.Key, Count = group.Count() })
            .ToDictionaryAsync(
                item => item.Type.ToString(),
                item => item.Count,
                cancellationToken);
        var totalUsers = await dbContext.Users.CountAsync(cancellationToken);
        var assignedUsers = await dbContext.Users
            .CountAsync(user => user.AssignedAssets.Count > 0, cancellationToken);
        var expiringWarranty = await dbContext.Assets.CountAsync(
            asset =>
                asset.Status != AssetStatus.Retired &&
                asset.WarrantyUntil.HasValue &&
                asset.WarrantyUntil >= today &&
                asset.WarrantyUntil <= warrantyLimit,
            cancellationToken);

        return Ok(new DashboardDto(
            statusCounts.Values.Sum(),
            statusCounts.GetValueOrDefault(AssetStatus.InStock),
            statusCounts.GetValueOrDefault(AssetStatus.Assigned),
            statusCounts.GetValueOrDefault(AssetStatus.Repair),
            statusCounts.GetValueOrDefault(AssetStatus.Retired),
            expiringWarranty,
            totalUsers,
            assignedUsers,
            typeCounts));
    }
}
