using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WindowsAssetInventory.Contracts;
using WindowsAssetInventory.Data;
using WindowsAssetInventory.Models;

namespace WindowsAssetInventory.Controllers;

[ApiController]
[Route("api/maintenance")]
public sealed class MaintenanceController(InventoryDbContext dbContext) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<MaintenanceRecordDto>>> GetAll(
        [FromQuery] int? assetId,
        CancellationToken cancellationToken)
    {
        var query = dbContext.MaintenanceRecords
            .AsNoTracking()
            .Include(record => record.Asset)
            .AsQueryable();
        if (assetId.HasValue)
        {
            query = query.Where(record => record.AssetId == assetId.Value);
        }

        var records = await query.ToListAsync(cancellationToken);
        return Ok(records
            .OrderByDescending(record => record.PerformedAt)
            .Select(record => record.ToDto())
            .ToArray());
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<MaintenanceRecordDto>> GetById(
        int id,
        CancellationToken cancellationToken)
    {
        var record = await dbContext.MaintenanceRecords
            .AsNoTracking()
            .Include(item => item.Asset)
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        return record is null ? RecordNotFound(id) : Ok(record.ToDto());
    }

    [HttpPost]
    public async Task<ActionResult<MaintenanceRecordDto>> Create(
        MaintenanceRecordRequest request,
        CancellationToken cancellationToken)
    {
        var asset = await dbContext.Assets.FindAsync([request.AssetId], cancellationToken);
        if (asset is null)
        {
            return Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Asset not found",
                detail: $"Asset {request.AssetId} does not exist.");
        }

        var record = new MaintenanceRecord
        {
            Asset = asset,
            Description = request.Description.Trim(),
            PerformedAt = request.PerformedAt ?? DateTimeOffset.UtcNow,
            Technician = request.Technician.Trim()
        };
        dbContext.MaintenanceRecords.Add(record);
        await dbContext.SaveChangesAsync(cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = record.Id }, record.ToDto());
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<MaintenanceRecordDto>> Update(
        int id,
        MaintenanceRecordRequest request,
        CancellationToken cancellationToken)
    {
        var record = await dbContext.MaintenanceRecords
            .Include(item => item.Asset)
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (record is null)
        {
            return RecordNotFound(id);
        }

        if (record.AssetId != request.AssetId)
        {
            var asset = await dbContext.Assets.FindAsync([request.AssetId], cancellationToken);
            if (asset is null)
            {
                return Problem(
                    statusCode: StatusCodes.Status404NotFound,
                    title: "Asset not found",
                    detail: $"Asset {request.AssetId} does not exist.");
            }

            record.Asset = asset;
            record.AssetId = asset.Id;
        }

        record.Description = request.Description.Trim();
        record.PerformedAt = request.PerformedAt ?? record.PerformedAt;
        record.Technician = request.Technician.Trim();
        await dbContext.SaveChangesAsync(cancellationToken);
        return Ok(record.ToDto());
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var record = await dbContext.MaintenanceRecords.FindAsync([id], cancellationToken);
        if (record is null)
        {
            return RecordNotFound(id);
        }

        dbContext.MaintenanceRecords.Remove(record);
        await dbContext.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private ObjectResult RecordNotFound(int id)
    {
        return Problem(
            statusCode: StatusCodes.Status404NotFound,
            title: "Maintenance record not found",
            detail: $"Maintenance record {id} does not exist.");
    }
}
