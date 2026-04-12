using Domain.Entities.WarehouseAndStock;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class StockRequestLineConfiguration : IEntityTypeConfiguration<StockRequestLine>
{
    public void Configure(EntityTypeBuilder<StockRequestLine> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Quantity)
               .IsRequired()
               .HasColumnType("decimal(18,2)");

        builder.HasOne(x => x.Company)
               .WithMany()
               .HasForeignKey(x => x.CompanyId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.StockRequest)
               .WithMany(x => x.Lines)
               .HasForeignKey(x => x.StockRequestId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.StockItem)
               .WithMany()
               .HasForeignKey(x => x.StockItemId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.StockRequestId);
        builder.HasIndex(x => x.StockItemId);
        builder.HasIndex(x => x.CompanyId);

        builder.HasIndex(x => new { x.StockRequestId, x.StockItemId })
               .IsUnique();
    }
}