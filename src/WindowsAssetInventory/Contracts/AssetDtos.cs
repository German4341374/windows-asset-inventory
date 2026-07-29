using System.ComponentModel.DataAnnotations;
using WindowsAssetInventory.Models;

namespace WindowsAssetInventory.Contracts;

public sealed record AssetDto(
    int Id,
    string AssetTag,
    AssetType Type,
    string Manufacturer,
    string Model,
    string SerialNumber,
    string? Hostname,
    AssetStatus Status,
    DateOnly? PurchaseDate,
    DateOnly? WarrantyUntil,
    int? AssignedUserId,
    string? AssignedUserName,
    int MaintenanceRecordCount,
    int SoftwareInstallationCount);

public abstract class AssetWriteRequest : IValidatableObject
{
    [Required, StringLength(40, MinimumLength = 2)]
    public required string AssetTag { get; init; }

    [Required]
    public AssetType? Type { get; init; }

    [Required, StringLength(100, MinimumLength = 2)]
    public required string Manufacturer { get; init; }

    [Required, StringLength(120, MinimumLength = 1)]
    public required string Model { get; init; }

    [Required, StringLength(120, MinimumLength = 2)]
    public required string SerialNumber { get; init; }

    [StringLength(100)]
    public string? Hostname { get; init; }

    [Required]
    public AssetStatus? Status { get; init; }

    public DateOnly? PurchaseDate { get; init; }

    public DateOnly? WarrantyUntil { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (PurchaseDate.HasValue &&
            WarrantyUntil.HasValue &&
            WarrantyUntil.Value < PurchaseDate.Value)
        {
            yield return new ValidationResult(
                "WarrantyUntil cannot be earlier than PurchaseDate.",
                [nameof(WarrantyUntil)]);
        }

    }
}

public sealed class CreateAssetRequest : AssetWriteRequest
{
}

public sealed class UpdateAssetRequest : AssetWriteRequest
{
}

public sealed class AssignAssetRequest
{
    [Range(1, int.MaxValue)]
    public int UserId { get; init; }
}

public sealed record AssetAssignmentDto(
    int AssetId,
    string AssetTag,
    AssetStatus Status,
    int? AssignedUserId,
    string? AssignedUserName);

public sealed record CsvImportResultDto(int Imported, IReadOnlyList<string> Warnings);
