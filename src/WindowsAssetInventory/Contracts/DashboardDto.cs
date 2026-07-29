namespace WindowsAssetInventory.Contracts;

public sealed record DashboardDto(
    int TotalAssets,
    int InStock,
    int Assigned,
    int Repair,
    int Retired,
    int ExpiringWarranty,
    int TotalUsers,
    int AssignedUsers,
    IReadOnlyDictionary<string, int> AssetsByType);
