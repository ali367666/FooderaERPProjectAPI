using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.Configurations;

public class RestaurantSectionConfiguration : IEntityTypeConfiguration<RestaurantSection>
{
    public void Configure(EntityTypeBuilder<RestaurantSection> builder)
    {
        builder.ToTable("RestaurantSections");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(150);

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
