using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.Configurations;

public class ScaleDeviceConfiguration : IEntityTypeConfiguration<ScaleDevice>
{
    public void Configure(EntityTypeBuilder<ScaleDevice> builder)
    {
        builder.ToTable("ScaleDevices");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(x => x.Brand)
            .HasMaxLength(150);

        builder.Property(x => x.ConnectionInfo)
            .HasMaxLength(500);

        builder.Property(x => x.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.HasOne(x => x.Restaurant)
            .WithMany()
            .HasForeignKey(x => x.RestaurantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.RestaurantId, x.Name })
            .IsUnique();
    }
}
