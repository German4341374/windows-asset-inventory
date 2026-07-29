namespace WindowsAssetInventory.Models;

public sealed class User
{
    public int Id { get; set; }
    public required string FullName { get; set; }
    public required string Email { get; set; }
    public required string Department { get; set; }
    public ICollection<Asset> AssignedAssets { get; } = [];
}
