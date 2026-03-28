using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class WarehouseStockConfiguration : IEntityTypeConfiguration<WarehouseStock>
{
    public void Configure(EntityTypeBuilder<WarehouseStock> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.QuantityOnHand)
               .HasColumnType("decimal(18,2)");

        builder.Property(x => x.MinLevel)
               .HasColumnType("decimal(18,2)");

        builder.HasOne(x => x.StockItem)
               .WithMany(x => x.WarehouseStocks)
               .HasForeignKey(x => x.StockItemId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Warehouse)
               .WithMany()
               .HasForeignKey(x => x.WarehouseId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.StockItemId, x.WarehouseId })
               .IsUnique();
    }
}