using WindowsAssetInventory.Models;

namespace WindowsAssetInventory.Contracts;

public static class Mappings
{
    public static AssetDto ToDto(this Asset asset)
    {
        return new AssetDto(
            asset.Id,
            asset.AssetTag,
            asset.Type,
            asset.Manufacturer,
            asset.Model,
            asset.SerialNumber,
            asset.Hostname,
            asset.Status,
            asset.PurchaseDate,
            asset.WarrantyUntil,
            asset.AssignedUserId,
            asset.AssignedUser?.FullName,
            asset.MaintenanceRecords.Count,
            asset.SoftwareInstallations.Count);
    }

    public static UserDto ToDto(this User user)
    {
        return new UserDto(
            user.Id,
            user.FullName,
            user.Email,
            user.Department,
            user.AssignedAssets.Count);
    }

    public static MaintenanceRecordDto ToDto(this MaintenanceRecord record)
    {
        return new MaintenanceRecordDto(
            record.Id,
            record.AssetId,
            record.Asset.AssetTag,
            record.Description,
            record.PerformedAt,
            record.Technician);
    }

    public static SoftwareInstallationDto ToDto(this SoftwareInstallation installation)
    {
        return new SoftwareInstallationDto(
            installation.Id,
            installation.AssetId,
            installation.Name,
            installation.Version,
            installation.InstalledAt);
    }
}
