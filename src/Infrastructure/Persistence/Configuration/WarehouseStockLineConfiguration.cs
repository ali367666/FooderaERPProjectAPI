using Domain.Entities.WarehouseAndStock;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.Configurations;

public class WarehouseStockLineConfiguration : IEntityTypeConfiguration<WarehouseStockLine>
{
    public void Configure(EntityTypeBuilder<WarehouseStockLine> builder)
    {
        builder.ToTable("WarehouseStockLines");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.CompanyId).IsRequired();
        builder.Property(x => x.Quantity)
            .IsRequired()
            .HasPrecision(18, 4);

        builder.Property(x => x.UnitId)
            .IsRequired();

        builder.HasOne(x => x.Company)
            .WithMany()
            .HasForeignKey(x => x.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.StockItem)
            .WithMany()
            .HasForeignKey(x => x.StockItemId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.WarehouseStockDocumentId, x.StockItemId })
            .IsUnique();
    }
}
