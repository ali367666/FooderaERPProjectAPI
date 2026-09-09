using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.Configurations;

public class DeliveryIntegrationConfiguration : IEntityTypeConfiguration<DeliveryIntegration>
{
    public void Configure(EntityTypeBuilder<DeliveryIntegration> builder)
    {
        builder.ToTable("DeliveryIntegrations");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(x => x.ExternalVenueId)
            .HasMaxLength(200);

        builder.Property(x => x.ApiKey)
            .HasMaxLength(500);

        builder.Property(x => x.WebhookSecret)
            .HasMaxLength(500);

        builder.Property(x => x.Provider)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(x => x.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.HasOne(x => x.Restaurant)
            .WithMany()
            .HasForeignKey(x => x.RestaurantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.RestaurantId, x.Name })
            .IsUnique();

        builder.HasIndex(x => new { x.Provider, x.ExternalVenueId });
    }
}
