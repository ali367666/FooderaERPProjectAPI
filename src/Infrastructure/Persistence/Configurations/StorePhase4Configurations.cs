using Domain.Entities.StoreMode;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.Configurations;

public class StoreInventoryConfiguration : IEntityTypeConfiguration<StoreInventory>
{
    public void Configure(EntityTypeBuilder<StoreInventory> b)
    {
        b.ToTable("StoreInventories");
        b.HasKey(x => x.Id);
        b.Property(x => x.DocumentNo).IsRequired().HasMaxLength(50);
        b.Property(x => x.Status).HasConversion<int>();
        b.Property(x => x.Note).HasMaxLength(500);
        b.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Warehouse).WithMany().HasForeignKey(x => x.WarehouseId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Category).WithMany().HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Restrict);
        b.HasMany(x => x.Lines).WithOne(l => l.StoreInventory).HasForeignKey(l => l.StoreInventoryId).OnDelete(DeleteBehavior.Cascade);
        b.HasIndex(x => new { x.CompanyId, x.DocumentNo }).IsUnique();
    }
}

public class StoreInventoryLineConfiguration : IEntityTypeConfiguration<StoreInventoryLine>
{
    public void Configure(EntityTypeBuilder<StoreInventoryLine> b)
    {
        b.ToTable("StoreInventoryLines");
        b.HasKey(x => x.Id);
        b.Property(x => x.ExpectedQuantity).HasColumnType("decimal(18,4)");
        b.Property(x => x.CountedQuantity).HasColumnType("decimal(18,4)");
        b.Property(x => x.Difference).HasColumnType("decimal(18,4)");
        b.Property(x => x.UnitCost).HasColumnType("decimal(18,4)");
        b.Property(x => x.Reason).HasMaxLength(300);
        b.HasOne(x => x.StoreProduct).WithMany().HasForeignKey(x => x.StoreProductId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class StoreTransferConfiguration : IEntityTypeConfiguration<StoreTransfer>
{
    public void Configure(EntityTypeBuilder<StoreTransfer> b)
    {
        b.ToTable("StoreTransfers");
        b.HasKey(x => x.Id);
        b.Property(x => x.DocumentNo).IsRequired().HasMaxLength(50);
        b.Property(x => x.Status).HasConversion<int>();
        b.Property(x => x.Note).HasMaxLength(500);
        b.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.FromWarehouse).WithMany().HasForeignKey(x => x.FromWarehouseId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.ToWarehouse).WithMany().HasForeignKey(x => x.ToWarehouseId).OnDelete(DeleteBehavior.Restrict);
        b.HasMany(x => x.Lines).WithOne(l => l.StoreTransfer).HasForeignKey(l => l.StoreTransferId).OnDelete(DeleteBehavior.Cascade);
        b.HasIndex(x => new { x.CompanyId, x.DocumentNo }).IsUnique();
        b.HasIndex(x => new { x.CompanyId, x.Status });
    }
}

public class StoreTransferLineConfiguration : IEntityTypeConfiguration<StoreTransferLine>
{
    public void Configure(EntityTypeBuilder<StoreTransferLine> b)
    {
        b.ToTable("StoreTransferLines");
        b.HasKey(x => x.Id);
        b.Property(x => x.Quantity).HasColumnType("decimal(18,4)");
        b.Property(x => x.ShippedQuantity).HasColumnType("decimal(18,4)");
        b.Property(x => x.ReceivedQuantity).HasColumnType("decimal(18,4)");
        b.Property(x => x.ShortageQuantity).HasColumnType("decimal(18,4)");
        b.Property(x => x.UnitCost).HasColumnType("decimal(18,4)");
        b.HasOne(x => x.StoreProduct).WithMany().HasForeignKey(x => x.StoreProductId).OnDelete(DeleteBehavior.Restrict);
    }
}
