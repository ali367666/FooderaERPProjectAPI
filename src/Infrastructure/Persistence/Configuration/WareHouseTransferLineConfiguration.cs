using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class WarehouseTransferLineConfiguration : IEntityTypeConfiguration<WarehouseTransferLine>
{
    public void Configure(EntityTypeBuilder<WarehouseTransferLine> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Quantity)
               .IsRequired()
               .HasColumnType("decimal(18,2)");

        // Company relation
        builder.HasOne(x => x.Company)
               .WithMany()
               .HasForeignKey(x => x.CompanyId)
               .OnDelete(DeleteBehavior.Restrict);

        // WarehouseTransfer relation
        builder.HasOne(x => x.WarehouseTransfer)
               .WithMany(x => x.Lines)
               .HasForeignKey(x => x.WarehouseTransferId)
               .OnDelete(DeleteBehavior.Cascade);

        // StockItem relation
        builder.HasOne(x => x.StockItem)
               .WithMany()
               .HasForeignKey(x => x.StockItemId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.CompanyId);
        builder.HasIndex(x => x.WarehouseTransferId);
        builder.HasIndex(x => x.StockItemId);

        // Eyni transferdə eyni məhsul bir dəfə olsun
        builder.HasIndex(x => new { x.WarehouseTransferId, x.StockItemId })
               .IsUnique();
    }
}