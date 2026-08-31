using Domain.Entities.WarehouseAndStock;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.Configurations;

public class StockPurchaseConfiguration : IEntityTypeConfiguration<StockPurchase>
{
    public void Configure(EntityTypeBuilder<StockPurchase> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.DocumentNo).IsRequired().HasMaxLength(50);
        builder.Property(p => p.ExchangeRate).HasColumnType("decimal(18,6)");
        builder.Property(p => p.Currency).HasConversion<int>();
        builder.Property(p => p.Status).HasConversion<int>();

        builder.HasOne(p => p.Company)
            .WithMany()
            .HasForeignKey(p => p.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.Counterparty)
            .WithMany()
            .HasForeignKey(p => p.CounterpartyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.Warehouse)
            .WithMany()
            .HasForeignKey(p => p.WarehouseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(p => p.Lines)
            .WithOne(l => l.StockPurchase)
            .HasForeignKey(l => l.StockPurchaseId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
