using Microsoft.EntityFrameworkCore;
using WindowsAssetInventory.Models;

namespace WindowsAssetInventory.Data;

public sealed class InventoryDbContext(DbContextOptions<InventoryDbContext> options)
    : DbContext(options)
{
    public DbSet<Asset> Assets => Set<Asset>();
    public DbSet<User> Users => Set<User>();
    public DbSet<MaintenanceRecord> MaintenanceRecords => Set<MaintenanceRecord>();
    public DbSet<SoftwareInstallation> SoftwareInstallations => Set<SoftwareInstallation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var asset = modelBuilder.Entity<Asset>();
        asset.HasIndex(item => item.AssetTag).IsUnique();
        asset.HasIndex(item => item.SerialNumber);
        asset.HasIndex(item => item.Hostname);
        asset.HasIndex(item => item.Type);
        asset.HasIndex(item => item.Status);
        asset.HasIndex(item => item.WarrantyUntil);
        asset.Property(item => item.AssetTag).HasMaxLength(40);
        asset.Property(item => item.Manufacturer).HasMaxLength(100);
        asset.Property(item => item.Model).HasMaxLength(120);
        asset.Property(item => item.SerialNumber).HasMaxLength(120);
        asset.Property(item => item.Hostname).HasMaxLength(100);
        asset.Property(item => item.Type).HasConversion<string>().HasMaxLength(30);
        asset.Property(item => item.Status).HasConversion<string>().HasMaxLength(30);
        asset.HasOne(item => item.AssignedUser)
            .WithMany(user => user.AssignedAssets)
            .HasForeignKey(item => item.AssignedUserId)
            .OnDelete(DeleteBehavior.Restrict);

        var user = modelBuilder.Entity<User>();
        user.HasIndex(item => item.Email).IsUnique();
        user.Property(item => item.FullName).HasMaxLength(140);
        user.Property(item => item.Email).HasMaxLength(254);
        user.Property(item => item.Department).HasMaxLength(100);

        var maintenance = modelBuilder.Entity<MaintenanceRecord>();
        maintenance.HasIndex(item => new { item.AssetId, item.PerformedAt });
        maintenance.Property(item => item.Description).HasMaxLength(1000);
        maintenance.Property(item => item.Technician).HasMaxLength(140);
        maintenance.HasOne(item => item.Asset)
            .WithMany(item => item.MaintenanceRecords)
            .HasForeignKey(item => item.AssetId)
            .OnDelete(DeleteBehavior.Cascade);

        var software = modelBuilder.Entity<SoftwareInstallation>();
        software.HasIndex(item => new { item.AssetId, item.Name });
        software.Property(item => item.Name).HasMaxLength(160);
        software.Property(item => item.Version).HasMaxLength(80);
        software.HasOne(item => item.Asset)
            .WithMany(item => item.SoftwareInstallations)
            .HasForeignKey(item => item.AssetId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
