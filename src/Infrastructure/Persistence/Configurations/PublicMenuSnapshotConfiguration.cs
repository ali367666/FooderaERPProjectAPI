using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.Configurations;

public class PublicMenuSnapshotConfiguration : IEntityTypeConfiguration<PublicMenuSnapshot>
{
    public void Configure(EntityTypeBuilder<PublicMenuSnapshot> builder)
    {
        builder.ToTable("PublicMenuSnapshots");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.MenuJson).IsRequired();
        builder.Property(x => x.ContentHash).IsRequired().HasMaxLength(64);

        builder.HasOne(x => x.Restaurant)
            .WithMany()
            .HasForeignKey(x => x.RestaurantId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.RestaurantId).IsUnique();
    }
}
