using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class RestaurantConfiguration : IEntityTypeConfiguration<Restaurant>
{
    public void Configure(EntityTypeBuilder<Restaurant> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
               .IsRequired()
               .HasMaxLength(150);

        builder.Property(x => x.Description)
               .HasMaxLength(500);

        builder.Property(x => x.Address)
               .HasMaxLength(500);

        builder.Property(x => x.Phone)
               .HasMaxLength(30);

        builder.Property(x => x.Email)
               .HasMaxLength(256);

        // Company relation
        builder.HasOne(x => x.Company)
               .WithMany(x => x.Restaurants)
               .HasForeignKey(x => x.CompanyId)
               .OnDelete(DeleteBehavior.Restrict);

        // eyni şirkətdə eyni restaurant adı olmasın
        builder.HasIndex(x => new { x.CompanyId, x.Name })
               .IsUnique();
    }
}