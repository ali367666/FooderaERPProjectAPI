using Domain.Entities.StoreMode;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.Configurations;

public class StoreCategoryConfiguration : IEntityTypeConfiguration<StoreCategory>
{
    public void Configure(EntityTypeBuilder<StoreCategory> b)
    {
        b.ToTable("StoreCategories");
        b.HasKey(x => x.Id);
        b.Property(x => x.Name).IsRequired().HasMaxLength(200);
        b.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.ParentCategory).WithMany().HasForeignKey(x => x.ParentCategoryId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class StoreProductConfiguration : IEntityTypeConfiguration<StoreProduct>
{
    public void Configure(EntityTypeBuilder<StoreProduct> b)
    {
        b.ToTable("StoreProducts");
        b.HasKey(x => x.Id);
        b.Property(x => x.Name).IsRequired().HasMaxLength(300);
        b.Property(x => x.Sku).HasMaxLength(100);
        b.Property(x => x.Brand).HasMaxLength(100);
        b.Property(x => x.ImageUrl).HasMaxLength(500);
        b.Property(x => x.Unit).HasConversion<int>();
        b.Property(x => x.PurchasePrice).HasColumnType("decimal(18,2)");
        b.Property(x => x.SalePrice).HasColumnType("decimal(18,2)");
        b.Property(x => x.WholesalePrice).HasColumnType("decimal(18,2)");
        b.Property(x => x.CampaignPrice).HasColumnType("decimal(18,2)");
        b.Property(x => x.VatPercent).HasColumnType("decimal(5,2)");
        b.Property(x => x.AverageCost).HasColumnType("decimal(18,4)");
        b.Property(x => x.MinimumStock).HasColumnType("decimal(18,4)");

        b.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Category).WithMany().HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Restrict);

        b.HasIndex(x => new { x.CompanyId, x.Name });
        b.HasIndex(x => new { x.CompanyId, x.Sku }).IsUnique().HasFilter("[Sku] IS NOT NULL");
    }
}

public class StoreProductBarcodeConfiguration : IEntityTypeConfiguration<StoreProductBarcode>
{
    public void Configure(EntityTypeBuilder<StoreProductBarcode> b)
    {
        b.ToTable("StoreProductBarcodes");
        b.HasKey(x => x.Id);
        b.Property(x => x.Barcode).IsRequired().HasMaxLength(100);
        b.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.StoreProduct).WithMany(p => p.Barcodes)
            .HasForeignKey(x => x.StoreProductId).OnDelete(DeleteBehavior.Cascade);
        b.HasIndex(x => new { x.CompanyId, x.Barcode }).IsUnique();
    }
}

public class StoreStockBalanceConfiguration : IEntityTypeConfiguration<StoreStockBalance>
{
    public void Configure(EntityTypeBuilder<StoreStockBalance> b)
    {
        b.ToTable("StoreStockBalances");
        b.HasKey(x => x.Id);
        b.Property(x => x.Quantity).HasColumnType("decimal(18,4)");
        b.Property(x => x.RowVersion).IsRowVersion();
        b.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Warehouse).WithMany().HasForeignKey(x => x.WarehouseId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.StoreProduct).WithMany().HasForeignKey(x => x.StoreProductId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.WarehouseId, x.StoreProductId }).IsUnique();
    }
}

public class StoreStockMovementConfiguration : IEntityTypeConfiguration<StoreStockMovement>
{
    public void Configure(EntityTypeBuilder<StoreStockMovement> b)
    {
        b.ToTable("StoreStockMovements");
        b.HasKey(x => x.Id);
        b.Property(x => x.Type).HasConversion<int>();
        b.Property(x => x.Quantity).HasColumnType("decimal(18,4)");
        b.Property(x => x.BalanceBefore).HasColumnType("decimal(18,4)");
        b.Property(x => x.BalanceAfter).HasColumnType("decimal(18,4)");
        b.Property(x => x.UnitCost).HasColumnType("decimal(18,4)");
        b.Property(x => x.DocumentType).HasMaxLength(50);
        b.Property(x => x.DocumentNo).HasMaxLength(50);
        b.Property(x => x.Reason).HasMaxLength(500);
        b.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Warehouse).WithMany().HasForeignKey(x => x.WarehouseId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.StoreProduct).WithMany().HasForeignKey(x => x.StoreProductId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.StoreProductId, x.WarehouseId, x.CreatedAtUtc });
    }
}

public class StoreSettingsConfiguration : IEntityTypeConfiguration<StoreSettings>
{
    public void Configure(EntityTypeBuilder<StoreSettings> b)
    {
        b.ToTable("StoreSettings");
        b.HasKey(x => x.Id);
        b.Property(x => x.MaxDiscountPercent).HasColumnType("decimal(5,2)").HasDefaultValue(10m);
        b.Property(x => x.BonusEarnPercent).HasColumnType("decimal(5,2)").HasDefaultValue(0m);
        b.Property(x => x.BonusMaxPaymentPercent).HasColumnType("decimal(5,2)").HasDefaultValue(50m);
        b.Property(x => x.ReceiptWidthMm).HasDefaultValue(80);
        b.Property(x => x.QuickSlotCount).HasDefaultValue(50);
        b.Property(x => x.ReceiptHeader).HasMaxLength(200);
        b.Property(x => x.ReceiptFooter).HasMaxLength(200);
        b.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => x.CompanyId).IsUnique();
    }
}

public class StoreCashRegisterConfiguration : IEntityTypeConfiguration<StoreCashRegister>
{
    public void Configure(EntityTypeBuilder<StoreCashRegister> b)
    {
        b.ToTable("StoreCashRegisters");
        b.HasKey(x => x.Id);
        b.Property(x => x.Name).IsRequired().HasMaxLength(100);
        b.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Restaurant).WithMany().HasForeignKey(x => x.RestaurantId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Warehouse).WithMany().HasForeignKey(x => x.WarehouseId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class StoreCashShiftConfiguration : IEntityTypeConfiguration<StoreCashShift>
{
    public void Configure(EntityTypeBuilder<StoreCashShift> b)
    {
        b.ToTable("StoreCashShifts");
        b.HasKey(x => x.Id);
        b.Property(x => x.Status).HasConversion<int>();
        b.Property(x => x.OpeningCash).HasColumnType("decimal(18,2)");
        b.Property(x => x.ExpectedCash).HasColumnType("decimal(18,2)");
        b.Property(x => x.ActualCash).HasColumnType("decimal(18,2)");
        b.Property(x => x.CashDifference).HasColumnType("decimal(18,2)");
        b.Property(x => x.CloseNote).HasMaxLength(500);
        b.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.StoreCashRegister).WithMany().HasForeignKey(x => x.StoreCashRegisterId).OnDelete(DeleteBehavior.Restrict);

        // One open shift per till.
        b.HasIndex(x => x.StoreCashRegisterId).IsUnique().HasFilter("[Status] = 1")
            .HasDatabaseName("IX_StoreCashShifts_OpenPerRegister");
    }
}

public class StoreCashMovementConfiguration : IEntityTypeConfiguration<StoreCashMovement>
{
    public void Configure(EntityTypeBuilder<StoreCashMovement> b)
    {
        b.ToTable("StoreCashMovements");
        b.HasKey(x => x.Id);
        b.Property(x => x.Type).HasConversion<int>();
        b.Property(x => x.Amount).HasColumnType("decimal(18,2)");
        b.Property(x => x.Reason).IsRequired().HasMaxLength(300);
        b.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.StoreCashShift).WithMany().HasForeignKey(x => x.StoreCashShiftId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class StoreSaleConfiguration : IEntityTypeConfiguration<StoreSale>
{
    public void Configure(EntityTypeBuilder<StoreSale> b)
    {
        b.ToTable("StoreSales");
        b.HasKey(x => x.Id);
        b.Property(x => x.DocumentNo).IsRequired().HasMaxLength(50);
        b.Property(x => x.Status).HasConversion<int>();
        b.Property(x => x.FiscalStatus).HasConversion<int>();
        b.Property(x => x.FiscalReceiptNo).HasMaxLength(100);
        b.Property(x => x.FiscalError).HasMaxLength(500);
        b.Property(x => x.SubTotal).HasColumnType("decimal(18,2)");
        b.Property(x => x.DiscountTotal).HasColumnType("decimal(18,2)");
        b.Property(x => x.VatTotal).HasColumnType("decimal(18,2)");
        b.Property(x => x.Total).HasColumnType("decimal(18,2)");
        b.Property(x => x.PaidTotal).HasColumnType("decimal(18,2)");
        b.Property(x => x.ChangeAmount).HasColumnType("decimal(18,2)");
        b.Property(x => x.BonusEarned).HasColumnType("decimal(18,2)");
        b.Property(x => x.BonusSpent).HasColumnType("decimal(18,2)");
        b.Property(x => x.DebtAmount).HasColumnType("decimal(18,2)");
        b.HasOne(x => x.StoreCustomer).WithMany().HasForeignKey(x => x.StoreCustomerId).OnDelete(DeleteBehavior.Restrict);
        b.Property(x => x.Note).HasMaxLength(500);
        b.Property(x => x.CancelReason).HasMaxLength(500);

        b.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.StoreCashRegister).WithMany().HasForeignKey(x => x.StoreCashRegisterId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.StoreCashShift).WithMany().HasForeignKey(x => x.StoreCashShiftId).OnDelete(DeleteBehavior.Restrict);
        b.HasMany(x => x.Lines).WithOne(l => l.StoreSale).HasForeignKey(l => l.StoreSaleId).OnDelete(DeleteBehavior.Cascade);
        b.HasMany(x => x.Payments).WithOne(p => p.StoreSale).HasForeignKey(p => p.StoreSaleId).OnDelete(DeleteBehavior.Cascade);

        b.HasIndex(x => new { x.CompanyId, x.DocumentNo }).IsUnique();
        b.HasIndex(x => new { x.CompanyId, x.OperationId }).IsUnique();
        b.HasIndex(x => new { x.CompanyId, x.CreatedAtUtc });
    }
}

public class StoreSaleLineConfiguration : IEntityTypeConfiguration<StoreSaleLine>
{
    public void Configure(EntityTypeBuilder<StoreSaleLine> b)
    {
        b.ToTable("StoreSaleLines");
        b.HasKey(x => x.Id);
        b.Property(x => x.ProductName).IsRequired().HasMaxLength(300);
        b.Property(x => x.Barcode).HasMaxLength(100);
        b.Property(x => x.Sku).HasMaxLength(100);
        b.Property(x => x.Quantity).HasColumnType("decimal(18,4)");
        b.Property(x => x.UnitPrice).HasColumnType("decimal(18,2)");
        b.Property(x => x.ListPrice).HasColumnType("decimal(18,2)");
        b.Property(x => x.PriceChangeReason).HasMaxLength(300);
        b.Property(x => x.DiscountAmount).HasColumnType("decimal(18,2)");
        b.Property(x => x.VatPercent).HasColumnType("decimal(5,2)");
        b.Property(x => x.VatAmount).HasColumnType("decimal(18,2)");
        b.Property(x => x.LineTotal).HasColumnType("decimal(18,2)");
        b.Property(x => x.UnitCost).HasColumnType("decimal(18,4)");
        b.HasOne(x => x.StoreProduct).WithMany().HasForeignKey(x => x.StoreProductId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class StoreSalePaymentConfiguration : IEntityTypeConfiguration<StoreSalePayment>
{
    public void Configure(EntityTypeBuilder<StoreSalePayment> b)
    {
        b.ToTable("StoreSalePayments");
        b.HasKey(x => x.Id);
        b.Property(x => x.Method).HasConversion<int>();
        b.Property(x => x.Amount).HasColumnType("decimal(18,2)");
        b.Property(x => x.Reference).HasMaxLength(100);
    }
}
