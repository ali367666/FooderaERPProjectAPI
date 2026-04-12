using Domain.Entities.WarehouseAndStock;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class StockMovementConfiguration : IEntityTypeConfiguration<StockMovement>
{
    public void Configure(EntityTypeBuilder<StockMovement> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Type)
               .IsRequired();

        builder.Property(x => x.Quantity)
               .IsRequired()
               .HasColumnType("decimal(18,2)");

        builder.Property(x => x.Note)
               .HasMaxLength(500);

        builder.HasOne(x => x.Company)
               .WithMany()
               .HasForeignKey(x => x.CompanyId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Warehouse)
               .WithMany()
               .HasForeignKey(x => x.WarehouseId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.StockItem)
               .WithMany()
               .HasForeignKey(x => x.StockItemId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.WarehouseTransfer)
               .WithMany()
               .HasForeignKey(x => x.WarehouseTransferId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.CompanyId, x.WarehouseId });
        builder.HasIndex(x => new { x.CompanyId, x.StockItemId });
        builder.HasIndex(x => new { x.CompanyId, x.WarehouseId, x.StockItemId });
        builder.HasIndex(x => x.WarehouseTransferId);
        builder.HasIndex(x => x.Type);
    }
}