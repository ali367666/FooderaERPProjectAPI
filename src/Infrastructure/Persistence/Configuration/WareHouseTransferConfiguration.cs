using Domain.Entities.WarehouseAndStock;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class WarehouseTransferConfiguration : IEntityTypeConfiguration<WarehouseTransfer>
{
    public void Configure(EntityTypeBuilder<WarehouseTransfer> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.DocumentNo)
            .IsRequired()
            .HasMaxLength(32);

        builder.HasIndex(x => new { x.CompanyId, x.DocumentNo })
            .IsUnique();

        builder.Property(x => x.Status)
               .IsRequired();

        builder.Property(x => x.TransferDate)
               .IsRequired();

        builder.Property(x => x.Note)
               .HasMaxLength(500);

        builder.HasOne(x => x.Company)
               .WithMany()
               .HasForeignKey(x => x.CompanyId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.StockRequest)
               .WithMany()
               .HasForeignKey(x => x.StockRequestId)
               .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(x => x.FromWarehouse)
               .WithMany()
               .HasForeignKey(x => x.FromWarehouseId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.ToWarehouse)
               .WithMany()
               .HasForeignKey(x => x.ToWarehouseId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.VehicleWarehouse)
               .WithMany()
               .HasForeignKey(x => x.VehicleWarehouseId)
               .OnDelete(DeleteBehavior.Restrict);

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