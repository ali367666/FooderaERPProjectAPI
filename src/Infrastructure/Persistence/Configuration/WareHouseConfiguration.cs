using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class WarehouseConfiguration : IEntityTypeConfiguration<Warehouse>
{
    public void Configure(EntityTypeBuilder<Warehouse> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
               .IsRequired()
               .HasMaxLength(150);

        builder.Property(x => x.Type)
               .IsRequired();

        // Company relation
        builder.HasOne(x => x.Company)
               .WithMany()
               .HasForeignKey(x => x.CompanyId)
               .OnDelete(DeleteBehavior.Restrict);

        // Restaurant relation (optional)
        builder.HasOne(x => x.Restaurant)
               .WithMany()
               .HasForeignKey(x => x.RestaurantId)
               .OnDelete(DeleteBehavior.Restrict);

        // Driver relation (vehicle warehouse)
        builder.HasOne(x => x.DriverUser)
               .WithMany()
               .HasForeignKey(x => x.DriverUserId)
               .OnDelete(DeleteBehavior.Restrict);

        // indexlər
        builder.HasIndex(x => x.CompanyId);
        builder.HasIndex(x => x.RestaurantId);
        builder.HasIndex(x => x.DriverUserId);

        // eyni şirkətdə eyni adlı warehouse olmasın
        builder.HasIndex(x => new { x.CompanyId, x.Name })
               .IsUnique();

        builder.HasOne(x => x.ResponsibleEmployee)
               .WithMany()
               .HasForeignKey(x => x.ResponsibleEmployeeId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}