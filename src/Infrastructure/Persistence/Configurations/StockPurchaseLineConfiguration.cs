using Domain.Entities.WarehouseAndStock;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.Configurations;

public class StockPurchaseLineConfiguration : IEntityTypeConfiguration<StockPurchaseLine>
{
    public void Configure(EntityTypeBuilder<StockPurchaseLine> builder)
    {
        builder.HasKey(l => l.Id);

        builder.Property(l => l.Quantity).HasColumnType("decimal(18,4)");
        builder.Property(l => l.UnitPriceForeign).HasColumnType("decimal(18,4)");
        builder.Property(l => l.UnitPriceAzn).HasColumnType("decimal(18,4)");

        builder.HasOne(l => l.StockItem)
            .WithMany()
            .HasForeignKey(l => l.StockItemId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
