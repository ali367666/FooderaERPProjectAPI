using Domain.Entities;
using Domain.Entities.WarehouseAndStock;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

/// <summary>Maps <see cref="WarehouseStock"/> only. Do not configure other entities here.</summary>
public class WarehouseStockConfiguration : IEntityTypeConfiguration<WarehouseStock>
{
    public void Configure(EntityTypeBuilder<WarehouseStock> builder)
    {
        builder.ToTable("WarehouseStocks");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedOnAdd();

        builder.Property(x => x.CompanyId).IsRequired();
        builder.Property(x => x.WarehouseId).IsRequired();
        builder.Property(x => x.StockItemId).IsRequired();
        builder.Property(x => x.UnitId).IsRequired();

        builder.Property(x => x.Quantity)
            .IsRequired()
            .HasPrecision(18, 4);

        builder.HasIndex(x => new { x.CompanyId, x.WarehouseId, x.StockItemId })
            .IsUnique();

        builder.HasIndex(x => x.CompanyId);
        builder.HasIndex(x => x.WarehouseId);
        builder.HasIndex(x => x.StockItemId);

        builder.HasOne(x => x.Company)
            .WithMany()
            .HasForeignKey(x => x.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.StockItem)
            .WithMany(x => x.WarehouseStocks)
            .HasForeignKey(x => x.StockItemId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Warehouse)
            .WithMany(x => x.WarehouseStocks)
            .HasForeignKey(x => x.WarehouseId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
