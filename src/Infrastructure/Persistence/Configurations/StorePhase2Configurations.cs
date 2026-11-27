using Domain.Entities.StoreMode;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.Configurations;

public class StoreQuickSlotConfiguration : IEntityTypeConfiguration<StoreQuickSlot>
{
    public void Configure(EntityTypeBuilder<StoreQuickSlot> b)
    {
        b.ToTable("StoreQuickSlots");
        b.HasKey(x => x.Id);
        b.Property(x => x.Label).HasMaxLength(60);
        b.Property(x => x.Color).HasMaxLength(20);
        b.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.StoreCashRegister).WithMany().HasForeignKey(x => x.StoreCashRegisterId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.StoreProduct).WithMany().HasForeignKey(x => x.StoreProductId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.StoreCashRegisterId, x.SlotNumber }).IsUnique();
    }
}

public class StoreHeldSaleConfiguration : IEntityTypeConfiguration<StoreHeldSale>
{
    public void Configure(EntityTypeBuilder<StoreHeldSale> b)
    {
        b.ToTable("StoreHeldSales");
        b.HasKey(x => x.Id);
        b.Property(x => x.Title).HasMaxLength(100);
        b.Property(x => x.CartJson).IsRequired();
        b.Property(x => x.Total).HasColumnType("decimal(18,2)");
        b.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.StoreCashRegister).WithMany().HasForeignKey(x => x.StoreCashRegisterId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class StoreSerialUnitConfiguration : IEntityTypeConfiguration<StoreSerialUnit>
{
    public void Configure(EntityTypeBuilder<StoreSerialUnit> b)
    {
        b.ToTable("StoreSerialUnits");
        b.HasKey(x => x.Id);
        b.Property(x => x.SerialNumber).IsRequired().HasMaxLength(100);
        b.Property(x => x.Status).HasConversion<int>();
        b.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.StoreProduct).WithMany().HasForeignKey(x => x.StoreProductId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Warehouse).WithMany().HasForeignKey(x => x.WarehouseId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.StoreSaleLine).WithMany().HasForeignKey(x => x.StoreSaleLineId).OnDelete(DeleteBehavior.Restrict);
        b.HasMany(x => x.Events).WithOne(e => e.StoreSerialUnit).HasForeignKey(e => e.StoreSerialUnitId).OnDelete(DeleteBehavior.Cascade);

        // A serial / IMEI number exists once per company.
        b.HasIndex(x => new { x.CompanyId, x.SerialNumber }).IsUnique();
        b.HasIndex(x => new { x.StoreProductId, x.WarehouseId, x.Status });
    }
}

public class StoreSerialEventConfiguration : IEntityTypeConfiguration<StoreSerialEvent>
{
    public void Configure(EntityTypeBuilder<StoreSerialEvent> b)
    {
        b.ToTable("StoreSerialEvents");
        b.HasKey(x => x.Id);
        b.Property(x => x.Type).HasConversion<int>();
        b.Property(x => x.DocumentType).HasMaxLength(50);
        b.Property(x => x.DocumentNo).HasMaxLength(50);
        b.Property(x => x.Note).HasMaxLength(500);
    }
}

public class StoreApprovalLogConfiguration : IEntityTypeConfiguration<StoreApprovalLog>
{
    public void Configure(EntityTypeBuilder<StoreApprovalLog> b)
    {
        b.ToTable("StoreApprovalLogs");
        b.HasKey(x => x.Id);
        b.Property(x => x.Action).IsRequired().HasMaxLength(60);
        b.Property(x => x.DocumentType).HasMaxLength(50);
        b.Property(x => x.DocumentNo).HasMaxLength(50);
        b.Property(x => x.Reason).HasMaxLength(500);
        b.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class StoreSaleReturnConfiguration : IEntityTypeConfiguration<StoreSaleReturn>
{
    public void Configure(EntityTypeBuilder<StoreSaleReturn> b)
    {
        b.ToTable("StoreSaleReturns");
        b.HasKey(x => x.Id);
        b.Property(x => x.DocumentNo).IsRequired().HasMaxLength(50);
        b.Property(x => x.Reason).IsRequired().HasMaxLength(500);
        b.Property(x => x.TotalAmount).HasColumnType("decimal(18,2)");
        b.Property(x => x.RefundAmount).HasColumnType("decimal(18,2)");
        b.Property(x => x.DebtCreditAmount).HasColumnType("decimal(18,2)");
        b.Property(x => x.BonusRefundAmount).HasColumnType("decimal(18,2)");
        b.Property(x => x.BonusClawbackAmount).HasColumnType("decimal(18,2)");
        b.Property(x => x.RefundMethod).HasConversion<int>();
        b.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.StoreSale).WithMany().HasForeignKey(x => x.StoreSaleId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.StoreCashRegister).WithMany().HasForeignKey(x => x.StoreCashRegisterId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.StoreCashShift).WithMany().HasForeignKey(x => x.StoreCashShiftId).OnDelete(DeleteBehavior.Restrict);
        b.HasMany(x => x.Lines).WithOne(l => l.StoreSaleReturn).HasForeignKey(l => l.StoreSaleReturnId).OnDelete(DeleteBehavior.Cascade);
        b.HasIndex(x => new { x.CompanyId, x.DocumentNo }).IsUnique();
        b.HasIndex(x => new { x.CompanyId, x.OperationId }).IsUnique();
    }
}

public class StoreSaleReturnLineConfiguration : IEntityTypeConfiguration<StoreSaleReturnLine>
{
    public void Configure(EntityTypeBuilder<StoreSaleReturnLine> b)
    {
        b.ToTable("StoreSaleReturnLines");
        b.HasKey(x => x.Id);
        b.Property(x => x.ProductName).IsRequired().HasMaxLength(300);
        b.Property(x => x.Quantity).HasColumnType("decimal(18,4)");
        b.Property(x => x.UnitAmount).HasColumnType("decimal(18,4)");
        b.Property(x => x.Amount).HasColumnType("decimal(18,2)");
        b.Property(x => x.UnitCost).HasColumnType("decimal(18,4)");
        b.Property(x => x.Condition).HasConversion<int>();
        b.HasOne(x => x.StoreSaleLine).WithMany().HasForeignKey(x => x.StoreSaleLineId).OnDelete(DeleteBehavior.Restrict);
    }
}
