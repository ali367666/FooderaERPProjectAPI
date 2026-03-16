using Domain.Entities;
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

        // WarehouseTransfer relation (optional)
        builder.HasOne(x => x.WarehouseTransfer)
               .WithMany()
               .HasForeignKey(x => x.WarehouseTransferId)
               .OnDelete(DeleteBehavior.Restrict);

        // Axtarışlar üçün faydalı index-lər
        builder.HasIndex(x => new { x.CompanyId, x.WarehouseId });
        builder.HasIndex(x => new { x.CompanyId, x.StockItemId });
        builder.HasIndex(x => x.WarehouseTransferId);
    }
}