using Domain.Entities.StoreMode;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.Configurations;

public class StoreSupplierConfiguration : IEntityTypeConfiguration<StoreSupplier>
{
    public void Configure(EntityTypeBuilder<StoreSupplier> b)
    {
        b.ToTable("StoreSuppliers");
        b.HasKey(x => x.Id);
        b.Property(x => x.Name).IsRequired().HasMaxLength(200);
        b.Property(x => x.LegalName).HasMaxLength(300);
        b.Property(x => x.Phone).HasMaxLength(50);
        b.Property(x => x.Email).HasMaxLength(200);
        b.Property(x => x.Address).HasMaxLength(500);
        b.Property(x => x.TaxNumber).HasMaxLength(50);
        b.Property(x => x.Note).HasMaxLength(500);
        b.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.CompanyId, x.Name });
    }
}

public class StorePurchaseConfiguration : IEntityTypeConfiguration<StorePurchase>
{
    public void Configure(EntityTypeBuilder<StorePurchase> b)
    {
        b.ToTable("StorePurchases");
        b.HasKey(x => x.Id);
        b.Property(x => x.DocumentNo).IsRequired().HasMaxLength(50);
        b.Property(x => x.SupplierInvoiceNo).HasMaxLength(100);
        b.Property(x => x.Note).HasMaxLength(500);
        b.Property(x => x.CancelReason).HasMaxLength(500);
        b.Property(x => x.Status).HasConversion<int>();
        b.Property(x => x.Total).HasColumnType("decimal(18,2)");
        b.Property(x => x.VatTotal).HasColumnType("decimal(18,2)");
        b.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.StoreSupplier).WithMany().HasForeignKey(x => x.StoreSupplierId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Warehouse).WithMany().HasForeignKey(x => x.WarehouseId).OnDelete(DeleteBehavior.Restrict);
        b.HasMany(x => x.Lines).WithOne(l => l.StorePurchase).HasForeignKey(l => l.StorePurchaseId).OnDelete(DeleteBehavior.Cascade);
        b.HasIndex(x => new { x.CompanyId, x.DocumentNo }).IsUnique();
        b.HasIndex(x => new { x.StoreSupplierId, x.Status });
    }
}

public class StorePurchaseLineConfiguration : IEntityTypeConfiguration<StorePurchaseLine>
{
    public void Configure(EntityTypeBuilder<StorePurchaseLine> b)
    {
        b.ToTable("StorePurchaseLines");
        b.HasKey(x => x.Id);
        b.Property(x => x.Quantity).HasColumnType("decimal(18,4)");
        b.Property(x => x.UnitPrice).HasColumnType("decimal(18,4)");
        b.Property(x => x.DiscountPercent).HasColumnType("decimal(5,2)");
        b.Property(x => x.VatPercent).HasColumnType("decimal(5,2)");
        b.Property(x => x.LineTotal).HasColumnType("decimal(18,2)");
        b.Property(x => x.NetUnitCost).HasColumnType("decimal(18,4)");
        b.HasOne(x => x.StoreProduct).WithMany().HasForeignKey(x => x.StoreProductId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class StorePurchaseReturnConfiguration : IEntityTypeConfiguration<StorePurchaseReturn>
{
    public void Configure(EntityTypeBuilder<StorePurchaseReturn> b)
    {
        b.ToTable("StorePurchaseReturns");
        b.HasKey(x => x.Id);
        b.Property(x => x.DocumentNo).IsRequired().HasMaxLength(50);
        b.Property(x => x.Reason).IsRequired().HasMaxLength(500);
        b.Property(x => x.Total).HasColumnType("decimal(18,2)");
        b.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.StorePurchase).WithMany().HasForeignKey(x => x.StorePurchaseId).OnDelete(DeleteBehavior.Restrict);
        b.HasMany(x => x.Lines).WithOne(l => l.StorePurchaseReturn).HasForeignKey(l => l.StorePurchaseReturnId).OnDelete(DeleteBehavior.Cascade);
        b.HasIndex(x => new { x.CompanyId, x.DocumentNo }).IsUnique();
    }
}

public class StorePurchaseReturnLineConfiguration : IEntityTypeConfiguration<StorePurchaseReturnLine>
{
    public void Configure(EntityTypeBuilder<StorePurchaseReturnLine> b)
    {
        b.ToTable("StorePurchaseReturnLines");
        b.HasKey(x => x.Id);
        b.Property(x => x.ProductName).IsRequired().HasMaxLength(300);
        b.Property(x => x.Quantity).HasColumnType("decimal(18,4)");
        b.Property(x => x.UnitAmount).HasColumnType("decimal(18,4)");
        b.Property(x => x.Amount).HasColumnType("decimal(18,2)");
        b.HasOne(x => x.StorePurchaseLine).WithMany().HasForeignKey(x => x.StorePurchaseLineId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class StoreSupplierPaymentConfiguration : IEntityTypeConfiguration<StoreSupplierPayment>
{
    public void Configure(EntityTypeBuilder<StoreSupplierPayment> b)
    {
        b.ToTable("StoreSupplierPayments");
        b.HasKey(x => x.Id);
        b.Property(x => x.DocumentNo).IsRequired().HasMaxLength(50);
        b.Property(x => x.Amount).HasColumnType("decimal(18,2)");
        b.Property(x => x.Method).HasConversion<int>();
        b.Property(x => x.Reference).HasMaxLength(100);
        b.Property(x => x.Note).HasMaxLength(500);
        b.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.StoreSupplier).WithMany().HasForeignKey(x => x.StoreSupplierId).OnDelete(DeleteBehavior.Restrict);
        b.HasMany(x => x.Allocations).WithOne(a => a.StoreSupplierPayment).HasForeignKey(a => a.StoreSupplierPaymentId).OnDelete(DeleteBehavior.Cascade);
        b.HasIndex(x => new { x.CompanyId, x.DocumentNo }).IsUnique();
    }
}

public class StorePaymentAllocationConfiguration : IEntityTypeConfiguration<StorePaymentAllocation>
{
    public void Configure(EntityTypeBuilder<StorePaymentAllocation> b)
    {
        b.ToTable("StorePaymentAllocations");
        b.HasKey(x => x.Id);
        b.Property(x => x.Amount).HasColumnType("decimal(18,2)");
        b.HasOne(x => x.StorePurchase).WithMany().HasForeignKey(x => x.StorePurchaseId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class StoreLedgerEntryConfiguration : IEntityTypeConfiguration<StoreLedgerEntry>
{
    public void Configure(EntityTypeBuilder<StoreLedgerEntry> b)
    {
        b.ToTable("StoreLedgerEntries");
        b.HasKey(x => x.Id);
        b.Property(x => x.PartyType).HasConversion<int>();
        b.Property(x => x.Type).HasConversion<int>();
        b.Property(x => x.Amount).HasColumnType("decimal(18,2)");
        b.Property(x => x.DocumentType).HasMaxLength(50);
        b.Property(x => x.DocumentNo).HasMaxLength(50);
        b.Property(x => x.Note).HasMaxLength(500);
        b.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.CompanyId, x.PartyType, x.PartyId, x.EntryDate });
    }
}

public class StorePriceHistoryConfiguration : IEntityTypeConfiguration<StorePriceHistory>
{
    public void Configure(EntityTypeBuilder<StorePriceHistory> b)
    {
        b.ToTable("StorePriceHistories");
        b.HasKey(x => x.Id);
        b.Property(x => x.PriceType).HasConversion<int>();
        b.Property(x => x.OldPrice).HasColumnType("decimal(18,4)");
        b.Property(x => x.NewPrice).HasColumnType("decimal(18,4)");
        b.Property(x => x.Reason).HasMaxLength(300);
        b.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.StoreProduct).WithMany().HasForeignKey(x => x.StoreProductId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.StoreProductId, x.EffectiveFromUtc });
    }
}
