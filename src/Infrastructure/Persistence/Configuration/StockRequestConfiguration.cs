using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class StockRequestConfiguration : IEntityTypeConfiguration<StockRequest>
{
    public void Configure(EntityTypeBuilder<StockRequest> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Status)
               .IsRequired()
               .HasDefaultValue(StockRequestStatus.Draft);

        builder.Property(x => x.Note)
               .HasMaxLength(500);

        builder.HasOne(x => x.Company)
               .WithMany()
               .HasForeignKey(x => x.CompanyId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.RequestingWarehouse)
               .WithMany()
               .HasForeignKey(x => x.RequestingWarehouseId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.SupplyingWarehouse)
               .WithMany()
               .HasForeignKey(x => x.SupplyingWarehouseId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.Lines)
               .WithOne(x => x.StockRequest)
               .HasForeignKey(x => x.StockRequestId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.CompanyId);
        builder.HasIndex(x => x.RequestingWarehouseId);
        builder.HasIndex(x => x.SupplyingWarehouseId);
        builder.HasIndex(x => x.Status);
    }
}