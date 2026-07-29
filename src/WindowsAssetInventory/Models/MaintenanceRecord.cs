namespace WindowsAssetInventory.Models;

public sealed class MaintenanceRecord
{
    public int Id { get; set; }
    public int AssetId { get; set; }
    public Asset Asset { get; set; } = null!;
    public required string Description { get; set; }
    public DateTimeOffset PerformedAt { get; set; }
    public required string Technician { get; set; }
}
