using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class WarehouseTransferConfiguration : IEntityTypeConfiguration<WarehouseTransfer>
{
    public void Configure(EntityTypeBuilder<WarehouseTransfer> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Status)
               .IsRequired();

        // Company relation
        builder.HasOne(x => x.Company)
               .WithMany()
               .HasForeignKey(x => x.CompanyId)
               .OnDelete(DeleteBehavior.Restrict);

        // StockRequest relation (optional)
        builder.HasOne(x => x.StockRequest)
               .WithMany()
               .HasForeignKey(x => x.StockRequestId)
               .OnDelete(DeleteBehavior.Restrict);

        // From warehouse
        builder.HasOne(x => x.FromWarehouse)
               .WithMany()
               .HasForeignKey(x => x.FromWarehouseId)
               .OnDelete(DeleteBehavior.Restrict);

        // To warehouse
        builder.HasOne(x => x.ToWarehouse)
               .WithMany()
               .HasForeignKey(x => x.ToWarehouseId)
               .OnDelete(DeleteBehavior.Restrict);

        // Vehicle warehouse
        builder.HasOne(x => x.VehicleWarehouse)
               .WithMany()
               .HasForeignKey(x => x.VehicleWarehouseId)
               .OnDelete(DeleteBehavior.Restrict);

        // Transfer -> Lines
        builder.HasMany(x => x.Lines)
               .WithOne(x => x.WarehouseTransfer)
               .HasForeignKey(x => x.WarehouseTransferId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.CompanyId);
        builder.HasIndex(x => x.StockRequestId);
        builder.HasIndex(x => x.FromWarehouseId);
        builder.HasIndex(x => x.ToWarehouseId);
        builder.HasIndex(x => x.VehicleWarehouseId);
        builder.HasIndex(x => x.Status);
    }
}