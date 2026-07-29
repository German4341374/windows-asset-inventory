namespace WindowsAssetInventory.Models;

public sealed class Asset
{
    public int Id { get; set; }
    public required string AssetTag { get; set; }
    public AssetType Type { get; set; }
    public required string Manufacturer { get; set; }
    public required string Model { get; set; }
    public required string SerialNumber { get; set; }
    public string? Hostname { get; set; }
    public AssetStatus Status { get; set; } = AssetStatus.InStock;
    public DateOnly? PurchaseDate { get; set; }
    public DateOnly? WarrantyUntil { get; set; }
    public int? AssignedUserId { get; set; }
    public User? AssignedUser { get; set; }
    public ICollection<MaintenanceRecord> MaintenanceRecords { get; } = [];
    public ICollection<SoftwareInstallation> SoftwareInstallations { get; } = [];
}
