using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WindowsAssetInventory.Contracts;
using WindowsAssetInventory.Data;
using WindowsAssetInventory.Models;
using WindowsAssetInventory.Services;

namespace WindowsAssetInventory.Controllers;

[ApiController]
[Route("api/assets")]
public sealed class AssetsController(
    InventoryDbContext dbContext,
    AssetAssignmentPolicy assignmentPolicy,
    CsvAssetService csvService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<AssetDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AssetDto>>> GetAll(
        [FromQuery] string? search,
        [FromQuery] AssetType? type,
        [FromQuery] AssetStatus? status,
        CancellationToken cancellationToken)
    {
        var query = AssetQuery();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(asset =>
                asset.AssetTag.Contains(term) ||
                (asset.Hostname != null && asset.Hostname.Contains(term)) ||
                asset.SerialNumber.Contains(term) ||
                (asset.AssignedUser != null &&
                    (asset.AssignedUser.FullName.Contains(term) ||
                     asset.AssignedUser.Email.Contains(term))));
        }

        if (type.HasValue)
        {
            query = query.Where(asset => asset.Type == type.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(asset => asset.Status == status.Value);
        }

        var assets = await query
            .OrderBy(asset => asset.AssetTag)
            .ToListAsync(cancellationToken);
        return Ok(assets.Select(asset => asset.ToDto()).ToArray());
    }

    [HttpGet("warranty-expiring")]
    [ProducesResponseType<IReadOnlyList<AssetDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AssetDto>>> GetWarrantyExpiring(
        [FromQuery] int days = 30,
        CancellationToken cancellationToken = default)
    {
        if (days is < 1 or > 3650)
        {
            ModelState.AddModelError(nameof(days), "Days must be between 1 and 3650.");
            return ValidationProblem(ModelState);
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var limit = today.AddDays(days);
        var assets = await AssetQuery()
            .Where(asset =>
                asset.Status != AssetStatus.Retired &&
                asset.WarrantyUntil.HasValue &&
                asset.WarrantyUntil >= today &&
                asset.WarrantyUntil <= limit)
            .OrderBy(asset => asset.WarrantyUntil)
            .ToListAsync(cancellationToken);
        return Ok(assets.Select(asset => asset.ToDto()).ToArray());
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType<AssetDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AssetDto>> GetById(int id, CancellationToken cancellationToken)
    {
        var asset = await AssetQuery()
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        return asset is null ? AssetNotFound(id) : Ok(asset.ToDto());
    }

    [HttpPost]
    [ProducesResponseType<AssetDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AssetDto>> Create(
        CreateAssetRequest request,
        CancellationToken cancellationToken)
    {
        if (request.Status == AssetStatus.Assigned)
        {
            ModelState.AddModelError(nameof(request.Status), "Use the assignment endpoint to assign an asset.");
            return ValidationProblem(ModelState);
        }

        await EnsureUniqueAssetTag(request.AssetTag, null, cancellationToken);
        var asset = new Asset
        {
            AssetTag = request.AssetTag.Trim(),
            Type = request.Type!.Value,
            Manufacturer = request.Manufacturer.Trim(),
            Model = request.Model.Trim(),
            SerialNumber = request.SerialNumber.Trim(),
            Hostname = CleanOptional(request.Hostname),
            Status = request.Status!.Value,
            PurchaseDate = request.PurchaseDate,
            WarrantyUntil = request.WarrantyUntil
        };
        dbContext.Assets.Add(asset);
        await dbContext.SaveChangesAsync(cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = asset.Id }, asset.ToDto());
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType<AssetDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AssetDto>> Update(
        int id,
        UpdateAssetRequest request,
        CancellationToken cancellationToken)
    {
        var asset = await dbContext.Assets
            .Include(item => item.AssignedUser)
            .Include(item => item.MaintenanceRecords)
            .Include(item => item.SoftwareInstallations)
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (asset is null)
        {
            return AssetNotFound(id);
        }

        if (asset.AssignedUserId.HasValue && request.Status != AssetStatus.Assigned)
        {
            throw new DomainConflictException("Return the asset before changing its Assigned status.");
        }

        if (!asset.AssignedUserId.HasValue && request.Status == AssetStatus.Assigned)
        {
            ModelState.AddModelError(nameof(request.Status), "Use the assignment endpoint to assign an asset.");
            return ValidationProblem(ModelState);
        }

        await EnsureUniqueAssetTag(request.AssetTag, id, cancellationToken);
        asset.AssetTag = request.AssetTag.Trim();
        asset.Type = request.Type!.Value;
        asset.Manufacturer = request.Manufacturer.Trim();
        asset.Model = request.Model.Trim();
        asset.SerialNumber = request.SerialNumber.Trim();
        asset.Hostname = CleanOptional(request.Hostname);
        asset.Status = request.Status!.Value;
        asset.PurchaseDate = request.PurchaseDate;
        asset.WarrantyUntil = request.WarrantyUntil;
        await dbContext.SaveChangesAsync(cancellationToken);
        return Ok(asset.ToDto());
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var asset = await dbContext.Assets.FindAsync([id], cancellationToken);
        if (asset is null)
        {
            return AssetNotFound(id);
        }

        if (asset.AssignedUserId.HasValue)
        {
            throw new DomainConflictException("Return the assigned asset before deleting it.");
        }

        dbContext.Assets.Remove(asset);
        await dbContext.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:int}/assign")]
    [ProducesResponseType<AssetAssignmentDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<AssetAssignmentDto>> Assign(
        int id,
        AssignAssetRequest request,
        CancellationToken cancellationToken)
    {
        var asset = await dbContext.Assets
            .Include(item => item.AssignedUser)
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (asset is null)
        {
            return AssetNotFound(id);
        }

        var user = await dbContext.Users.FindAsync([request.UserId], cancellationToken);
        if (user is null)
        {
            return Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "User not found",
                detail: $"User {request.UserId} does not exist.");
        }

        assignmentPolicy.Assign(asset, user);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Ok(new AssetAssignmentDto(asset.Id, asset.AssetTag, asset.Status, user.Id, user.FullName));
    }

    [HttpPost("{id:int}/return")]
    [ProducesResponseType<AssetAssignmentDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<AssetAssignmentDto>> Return(
        int id,
        CancellationToken cancellationToken)
    {
        var asset = await dbContext.Assets
            .Include(item => item.AssignedUser)
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (asset is null)
        {
            return AssetNotFound(id);
        }

        assignmentPolicy.Return(asset);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Ok(new AssetAssignmentDto(asset.Id, asset.AssetTag, asset.Status, null, null));
    }

    [HttpGet("{id:int}/software")]
    public async Task<ActionResult<IReadOnlyList<SoftwareInstallationDto>>> GetSoftware(
        int id,
        CancellationToken cancellationToken)
    {
        if (!await dbContext.Assets.AnyAsync(asset => asset.Id == id, cancellationToken))
        {
            return AssetNotFound(id);
        }

        var installations = await dbContext.SoftwareInstallations
            .AsNoTracking()
            .Where(item => item.AssetId == id)
            .OrderBy(item => item.Name)
            .ToListAsync(cancellationToken);
        return Ok(installations.Select(item => item.ToDto()).ToArray());
    }

    [HttpPost("{id:int}/software")]
    public async Task<ActionResult<SoftwareInstallationDto>> AddSoftware(
        int id,
        SoftwareInstallationRequest request,
        CancellationToken cancellationToken)
    {
        if (!await dbContext.Assets.AnyAsync(asset => asset.Id == id, cancellationToken))
        {
            return AssetNotFound(id);
        }

        var installation = new SoftwareInstallation
        {
            AssetId = id,
            Name = request.Name.Trim(),
            Version = request.Version.Trim(),
            InstalledAt = request.InstalledAt ?? DateTimeOffset.UtcNow
        };
        dbContext.SoftwareInstallations.Add(installation);
        await dbContext.SaveChangesAsync(cancellationToken);
        return CreatedAtAction(nameof(GetSoftware), new { id }, installation.ToDto());
    }

    [HttpDelete("{assetId:int}/software/{softwareId:int}")]
    public async Task<IActionResult> DeleteSoftware(
        int assetId,
        int softwareId,
        CancellationToken cancellationToken)
    {
        var installation = await dbContext.SoftwareInstallations
            .SingleOrDefaultAsync(
                item => item.AssetId == assetId && item.Id == softwareId,
                cancellationToken);
        if (installation is null)
        {
            return Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Software installation not found");
        }

        dbContext.SoftwareInstallations.Remove(installation);
        await dbContext.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpPost("import-csv")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<CsvImportResultDto>> ImportCsv(
        IFormFile file,
        CancellationToken cancellationToken)
    {
        if (file.Length == 0 || file.Length > 2 * 1024 * 1024)
        {
            ModelState.AddModelError(nameof(file), "CSV file size must be between 1 byte and 2 MiB.");
            return ValidationProblem(ModelState);
        }

        await using var stream = file.OpenReadStream();
        var requests = await csvService.ParseAsync(stream, cancellationToken);
        var warnings = new List<string>();
        var seenTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var imported = 0;

        foreach (var request in requests)
        {
            if (!seenTags.Add(request.AssetTag) ||
                await dbContext.Assets.AnyAsync(
                    asset => asset.AssetTag == request.AssetTag,
                    cancellationToken))
            {
                warnings.Add($"Skipped duplicate asset tag {request.AssetTag}.");
                continue;
            }

            if (request.Status == AssetStatus.Assigned)
            {
                warnings.Add($"Skipped {request.AssetTag}: Assigned status requires the assignment endpoint.");
                continue;
            }

            dbContext.Assets.Add(new Asset
            {
                AssetTag = request.AssetTag.Trim(),
                Type = request.Type!.Value,
                Manufacturer = request.Manufacturer.Trim(),
                Model = request.Model.Trim(),
                SerialNumber = request.SerialNumber.Trim(),
                Hostname = CleanOptional(request.Hostname),
                Status = request.Status!.Value,
                PurchaseDate = request.PurchaseDate,
                WarrantyUntil = request.WarrantyUntil
            });
            imported++;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return Ok(new CsvImportResultDto(imported, warnings));
    }

    [HttpGet("export-csv")]
    [Produces("text/csv")]
    public async Task<IActionResult> ExportCsv(CancellationToken cancellationToken)
    {
        var assets = await AssetQuery()
            .OrderBy(asset => asset.AssetTag)
            .ToListAsync(cancellationToken);
        var csv = csvService.Export(assets.Select(asset => asset.ToDto()));
        return File(
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: true).GetBytes(csv),
            "text/csv; charset=utf-8",
            $"assets-{DateTime.UtcNow:yyyyMMdd}.csv");
    }

    private IQueryable<Asset> AssetQuery()
    {
        return dbContext.Assets
            .AsNoTracking()
            .AsSplitQuery()
            .Include(asset => asset.AssignedUser)
            .Include(asset => asset.MaintenanceRecords)
            .Include(asset => asset.SoftwareInstallations);
    }

    private async Task EnsureUniqueAssetTag(
        string assetTag,
        int? excludedId,
        CancellationToken cancellationToken)
    {
        var normalized = assetTag.Trim();
        var exists = await dbContext.Assets.AnyAsync(
            asset => asset.AssetTag == normalized && (!excludedId.HasValue || asset.Id != excludedId.Value),
            cancellationToken);
        if (exists)
        {
            throw new DomainConflictException($"Asset tag {normalized} already exists.");
        }
    }

    private ObjectResult AssetNotFound(int id)
    {
        return Problem(
            statusCode: StatusCodes.Status404NotFound,
            title: "Asset not found",
            detail: $"Asset {id} does not exist.");
    }

    private static string? CleanOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
