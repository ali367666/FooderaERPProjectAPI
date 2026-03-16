using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class WarehouseStockConfiguration : IEntityTypeConfiguration<WarehouseStock>
{
    public void Configure(EntityTypeBuilder<WarehouseStock> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.QuantityOnHand)
               .IsRequired()
               .HasColumnType("decimal(18,2)");

        builder.Property(x => x.MinLevel)
               .HasColumnType("decimal(18,2)");

        // Company relation
        builder.HasOne(x => x.Company)
               .WithMany()
               .HasForeignKey(x => x.CompanyId)
               .OnDelete(DeleteBehavior.Restrict);

        // Warehouse relation
        builder.HasOne(x => x.Warehouse)
               .WithMany()
               .HasForeignKey(x => x.WarehouseId)
               .OnDelete(DeleteBehavior.Restrict);

        // StockItem relation
        builder.HasOne(x => x.StockItem)
               .WithMany()
               .HasForeignKey(x => x.StockItemId)
               .OnDelete(DeleteBehavior.Restrict);

        // Eyni anbarda eyni məhsul yalnız bir dəfə olsun
        builder.HasIndex(x => new { x.WarehouseId, x.StockItemId })
               .IsUnique();

        builder.HasIndex(x => x.CompanyId);
        builder.HasIndex(x => x.WarehouseId);
        builder.HasIndex(x => x.StockItemId);
    }
}