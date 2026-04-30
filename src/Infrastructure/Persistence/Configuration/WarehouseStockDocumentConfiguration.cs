using Domain.Entities;
using Domain.Entities.WarehouseAndStock;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.Configurations;

public class WarehouseStockDocumentConfiguration : IEntityTypeConfiguration<WarehouseStockDocument>
{
    public void Configure(EntityTypeBuilder<WarehouseStockDocument> builder)
    {
        builder.ToTable("WarehouseStockDocuments");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.DocumentNo)
            .IsRequired()
            .HasMaxLength(32);

        builder.Property(x => x.CompanyId).IsRequired();
        builder.Property(x => x.WarehouseId).IsRequired();

        builder.Property(x => x.Status)
            .HasConversion<int>()
            .IsRequired();

        builder.HasIndex(x => new { x.CompanyId, x.DocumentNo }).IsUnique();

        builder.HasOne(x => x.Company)
            .WithMany()
            .HasForeignKey(x => x.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Warehouse)
            .WithMany(x => x.WarehouseStockDocuments)
            .HasForeignKey(x => x.WarehouseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.Lines)
            .WithOne(x => x.WarehouseStockDocument)
            .HasForeignKey(x => x.WarehouseStockDocumentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
