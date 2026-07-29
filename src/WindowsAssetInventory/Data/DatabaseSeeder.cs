using Microsoft.EntityFrameworkCore;
using WindowsAssetInventory.Models;

namespace WindowsAssetInventory.Data;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(InventoryDbContext dbContext, CancellationToken cancellationToken = default)
    {
        if (await dbContext.Assets.AnyAsync(cancellationToken))
        {
            return;
        }

        var users = new[]
        {
            new User { FullName = "Alex Morgan", Email = "alex.morgan@example.test", Department = "Engineering" },
            new User { FullName = "Jordan Lee", Email = "jordan.lee@example.test", Department = "Finance" },
            new User { FullName = "Taylor Kim", Email = "taylor.kim@example.test", Department = "Operations" },
            new User { FullName = "Casey Brown", Email = "casey.brown@example.test", Department = "Support" },
            new User { FullName = "Riley Davis", Email = "riley.davis@example.test", Department = "People" },
            new User { FullName = "Morgan Wilson", Email = "morgan.wilson@example.test", Department = "Sales" }
        };
        dbContext.Users.AddRange(users);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var assets = new[]
        {
            NewAsset("LT-1001", AssetType.Laptop, "Dell", "Latitude 7450", "DEMO-LT-1001", "ENG-LT-01", AssetStatus.Assigned, today.AddYears(-1), today.AddDays(18), users[0]),
            NewAsset("LT-1002", AssetType.Laptop, "Lenovo", "ThinkPad T14", "DEMO-LT-1002", "FIN-LT-02", AssetStatus.Assigned, today.AddMonths(-10), today.AddDays(70), users[1]),
            NewAsset("LT-1003", AssetType.Laptop, "HP", "EliteBook 840", "DEMO-LT-1003", "SUP-LT-03", AssetStatus.Repair, today.AddYears(-2), today.AddDays(-12)),
            NewAsset("PC-2001", AssetType.Computer, "Dell", "OptiPlex 7020", "DEMO-PC-2001", "OPS-WS-01", AssetStatus.Assigned, today.AddMonths(-8), today.AddDays(130), users[2]),
            NewAsset("PC-2002", AssetType.Computer, "HP", "Pro Mini 400", "DEMO-PC-2002", "SALES-WS-02", AssetStatus.Assigned, today.AddMonths(-6), today.AddDays(200), users[5]),
            NewAsset("PC-2003", AssetType.Computer, "Lenovo", "ThinkCentre M70q", "DEMO-PC-2003", null, AssetStatus.InStock, today.AddMonths(-2), today.AddDays(310)),
            NewAsset("MN-3001", AssetType.Monitor, "Dell", "UltraSharp U2724D", "DEMO-MN-3001", null, AssetStatus.Assigned, today.AddMonths(-11), today.AddDays(22), users[0]),
            NewAsset("MN-3002", AssetType.Monitor, "LG", "27QN880", "DEMO-MN-3002", null, AssetStatus.InStock, today.AddMonths(-4), today.AddDays(240)),
            NewAsset("MN-3003", AssetType.Monitor, "Samsung", "ViewFinity S6", "DEMO-MN-3003", null, AssetStatus.Retired, today.AddYears(-5), today.AddYears(-2)),
            NewAsset("PR-4001", AssetType.Peripheral, "Logitech", "MX Keys", "DEMO-PR-4001", null, AssetStatus.Assigned, today.AddMonths(-9), today.AddDays(15), users[3]),
            NewAsset("PR-4002", AssetType.Peripheral, "Jabra", "Evolve2 65", "DEMO-PR-4002", null, AssetStatus.InStock, today.AddMonths(-3), today.AddDays(270)),
            NewAsset("PR-4003", AssetType.Peripheral, "Logitech", "Brio 4K", "DEMO-PR-4003", null, AssetStatus.InStock, today.AddMonths(-1), today.AddDays(335))
        };
        dbContext.Assets.AddRange(assets);

        dbContext.MaintenanceRecords.AddRange(
            new MaintenanceRecord
            {
                Asset = assets[2],
                Description = "Battery health check and replacement approval.",
                PerformedAt = DateTimeOffset.UtcNow.AddDays(-4),
                Technician = "Service Desk"
            },
            new MaintenanceRecord
            {
                Asset = assets[0],
                Description = "Applied firmware updates and completed hardware diagnostics.",
                PerformedAt = DateTimeOffset.UtcNow.AddDays(-16),
                Technician = "Endpoint Team"
            },
            new MaintenanceRecord
            {
                Asset = assets[3],
                Description = "Cleaned cooling system and verified memory health.",
                PerformedAt = DateTimeOffset.UtcNow.AddDays(-45),
                Technician = "Service Desk"
            });

        dbContext.SoftwareInstallations.AddRange(
            NewSoftware(assets[0], "Microsoft 365 Apps", "Current Channel", -30),
            NewSoftware(assets[0], "Visual Studio Code", "1.112", -14),
            NewSoftware(assets[1], "Finance Desktop", "4.8.2", -24),
            NewSoftware(assets[3], "Operations Console", "2.6.0", -40));

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static Asset NewAsset(
        string assetTag,
        AssetType type,
        string manufacturer,
        string model,
        string serialNumber,
        string? hostname,
        AssetStatus status,
        DateOnly purchaseDate,
        DateOnly warrantyUntil,
        User? assignedUser = null)
    {
        return new Asset
        {
            AssetTag = assetTag,
            Type = type,
            Manufacturer = manufacturer,
            Model = model,
            SerialNumber = serialNumber,
            Hostname = hostname,
            Status = status,
            PurchaseDate = purchaseDate,
            WarrantyUntil = warrantyUntil,
            AssignedUser = assignedUser
        };
    }

    private static SoftwareInstallation NewSoftware(Asset asset, string name, string version, int daysAgo)
    {
        return new SoftwareInstallation
        {
            Asset = asset,
            Name = name,
            Version = version,
            InstalledAt = DateTimeOffset.UtcNow.AddDays(daysAgo)
        };
    }
}
