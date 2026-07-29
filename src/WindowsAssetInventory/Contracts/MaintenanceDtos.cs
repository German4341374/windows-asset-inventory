using System.ComponentModel.DataAnnotations;

namespace WindowsAssetInventory.Contracts;

public sealed record MaintenanceRecordDto(
    int Id,
    int AssetId,
    string AssetTag,
    string Description,
    DateTimeOffset PerformedAt,
    string Technician);

public sealed class MaintenanceRecordRequest
{
    [Range(1, int.MaxValue)]
    public int AssetId { get; init; }

    [Required, StringLength(1000, MinimumLength = 3)]
    public required string Description { get; init; }

    public DateTimeOffset? PerformedAt { get; init; }

    [Required, StringLength(140, MinimumLength = 2)]
    public required string Technician { get; init; }
}

public sealed record SoftwareInstallationDto(
    int Id,
    int AssetId,
    string Name,
    string Version,
    DateTimeOffset InstalledAt);

public sealed class SoftwareInstallationRequest
{
    [Required, StringLength(160, MinimumLength = 1)]
    public required string Name { get; init; }

    [Required, StringLength(80, MinimumLength = 1)]
    public required string Version { get; init; }

    public DateTimeOffset? InstalledAt { get; init; }
}
