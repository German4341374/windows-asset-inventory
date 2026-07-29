using WindowsAssetInventory.Models;
using WindowsAssetInventory.Services;

namespace WindowsAssetInventory.Tests;

public sealed class AssetAssignmentPolicyTests
{
    private readonly AssetAssignmentPolicy _policy = new();

    [Fact]
    public void Assign_InStockAsset_AssignsUserAndChangesStatus()
    {
        var asset = CreateAsset(AssetStatus.InStock);
        var user = CreateUser();

        _policy.Assign(asset, user);

        Assert.Equal(AssetStatus.Assigned, asset.Status);
        Assert.Equal(user.Id, asset.AssignedUserId);
        Assert.Same(user, asset.AssignedUser);
    }

    [Fact]
    public void Assign_AlreadyAssignedAsset_ThrowsConflict()
    {
        var asset = CreateAsset(AssetStatus.Assigned);
        asset.AssignedUserId = 7;

        var exception = Assert.Throws<DomainConflictException>(
            () => _policy.Assign(asset, CreateUser()));

        Assert.Contains("already assigned", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(AssetStatus.Repair)]
    [InlineData(AssetStatus.Retired)]
    public void Assign_UnavailableAsset_ThrowsConflict(AssetStatus status)
    {
        var asset = CreateAsset(status);

        Assert.Throws<DomainConflictException>(() => _policy.Assign(asset, CreateUser()));
    }

    [Fact]
    public void Return_AssignedAsset_ClearsOwnerAndMovesToStock()
    {
        var asset = CreateAsset(AssetStatus.Assigned);
        asset.AssignedUser = CreateUser();
        asset.AssignedUserId = asset.AssignedUser.Id;

        _policy.Return(asset);

        Assert.Equal(AssetStatus.InStock, asset.Status);
        Assert.Null(asset.AssignedUser);
        Assert.Null(asset.AssignedUserId);
    }

    [Fact]
    public void Return_UnassignedAsset_ThrowsConflict()
    {
        var asset = CreateAsset(AssetStatus.InStock);

        Assert.Throws<DomainConflictException>(() => _policy.Return(asset));
    }

    private static Asset CreateAsset(AssetStatus status)
    {
        return new Asset
        {
            Id = 42,
            AssetTag = "TEST-42",
            Type = AssetType.Laptop,
            Manufacturer = "Example",
            Model = "Test Device",
            SerialNumber = "TEST-SERIAL-42",
            Status = status
        };
    }

    private static User CreateUser()
    {
        return new User
        {
            Id = 7,
            FullName = "Test User",
            Email = "test.user@example.test",
            Department = "Quality"
        };
    }
}
