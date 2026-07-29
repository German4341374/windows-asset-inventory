using WindowsAssetInventory.Models;

namespace WindowsAssetInventory.Services;

public sealed class AssetAssignmentPolicy
{
    public void Assign(Asset asset, User user)
    {
        ArgumentNullException.ThrowIfNull(asset);
        ArgumentNullException.ThrowIfNull(user);

        if (asset.AssignedUserId.HasValue || asset.AssignedUser is not null || asset.Status == AssetStatus.Assigned)
        {
            throw new DomainConflictException($"Asset {asset.AssetTag} is already assigned.");
        }

        if (asset.Status is AssetStatus.Repair or AssetStatus.Retired)
        {
            throw new DomainConflictException(
                $"Asset {asset.AssetTag} cannot be assigned while its status is {asset.Status}.");
        }

        asset.AssignedUser = user;
        asset.AssignedUserId = user.Id;
        asset.Status = AssetStatus.Assigned;
    }

    public void Return(Asset asset)
    {
        ArgumentNullException.ThrowIfNull(asset);

        if (!asset.AssignedUserId.HasValue && asset.AssignedUser is null)
        {
            throw new DomainConflictException($"Asset {asset.AssetTag} is not assigned.");
        }

        asset.AssignedUser = null;
        asset.AssignedUserId = null;
        asset.Status = AssetStatus.InStock;
    }
}
