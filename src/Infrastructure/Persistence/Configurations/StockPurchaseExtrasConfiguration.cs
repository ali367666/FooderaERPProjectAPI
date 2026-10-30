using Domain.Entities.WarehouseAndStock;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.Configurations;

public class StockPurchasePaymentConfiguration : IEntityTypeConfiguration<StockPurchasePayment>
{
    public void Configure(EntityTypeBuilder<StockPurchasePayment> builder)
    {
        builder.ToTable("StockPurchasePayments");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Amount).HasColumnType("decimal(18,2)");
        builder.Property(x => x.RemainingAfterAzn).HasColumnType("decimal(18,2)");
        builder.Property(x => x.Note).HasMaxLength(500);

        builder.HasOne(x => x.StockPurchase).WithMany(p => p.Payments)
            .HasForeignKey(x => x.StockPurchaseId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.Company).WithMany()
            .HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class StockPurchaseReturnConfiguration : IEntityTypeConfiguration<StockPurchaseReturn>
{
    public void Configure(EntityTypeBuilder<StockPurchaseReturn> builder)
    {
        builder.ToTable("StockPurchaseReturns");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Note).HasMaxLength(500);

        builder.HasOne(x => x.StockPurchase).WithMany(p => p.Returns)
            .HasForeignKey(x => x.StockPurchaseId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.Company).WithMany()
            .HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(x => x.Lines).WithOne(l => l.StockPurchaseReturn)
            .HasForeignKey(l => l.StockPurchaseReturnId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class StockPurchaseReturnLineConfiguration : IEntityTypeConfiguration<StockPurchaseReturnLine>
{
    public void Configure(EntityTypeBuilder<StockPurchaseReturnLine> builder)
    {
        builder.ToTable("StockPurchaseReturnLines");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Quantity).HasColumnType("decimal(18,4)");
        builder.Property(x => x.UnitPriceAzn).HasColumnType("decimal(18,4)");

        builder.HasOne(x => x.StockPurchaseLine).WithMany()
            .HasForeignKey(x => x.StockPurchaseLineId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.StockItem).WithMany()
            .HasForeignKey(x => x.StockItemId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class StockWriteOffConfiguration : IEntityTypeConfiguration<StockWriteOff>
{
    public void Configure(EntityTypeBuilder<StockWriteOff> builder)
    {
        builder.ToTable("StockWriteOffs");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Quantity).HasColumnType("decimal(18,4)");
        builder.Property(x => x.UnitCostAzn).HasColumnType("decimal(18,4)");
        builder.Property(x => x.Reason).HasConversion<int>();
        builder.Property(x => x.Note).HasMaxLength(500);

        builder.HasOne(x => x.Company).WithMany()
            .HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Warehouse).WithMany()
            .HasForeignKey(x => x.WarehouseId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.StockItem).WithMany()
            .HasForeignKey(x => x.StockItemId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.StockPurchase).WithMany()
            .HasForeignKey(x => x.StockPurchaseId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.CompanyId, x.CreatedAtUtc });
    }
}
