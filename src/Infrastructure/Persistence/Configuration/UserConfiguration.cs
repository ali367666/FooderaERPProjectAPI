using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.Property(x => x.FullName)
               .IsRequired()
               .HasMaxLength(150);

        builder.Property(x => x.WorkplaceType)
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

        builder.HasIndex(x => x.CompanyId);
        builder.HasIndex(x => x.RestaurantId);
        builder.HasIndex(x => x.WorkplaceType);
    }
}