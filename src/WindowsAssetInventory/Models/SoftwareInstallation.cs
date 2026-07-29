namespace WindowsAssetInventory.Models;

public sealed class SoftwareInstallation
{
    public int Id { get; set; }
    public int AssetId { get; set; }
    public Asset Asset { get; set; } = null!;
    public required string Name { get; set; }
    public required string Version { get; set; }
    public DateTimeOffset InstalledAt { get; set; }
}
