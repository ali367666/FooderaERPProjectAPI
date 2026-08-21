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

        builder.Property(x => x.Code)
               .HasMaxLength(4);

        builder.Property(x => x.RfidCardId)
               .HasMaxLength(64);

        builder.Property(x => x.CanAccessAdminPanel)
            .HasDefaultValue(true);

        builder.Property(x => x.CanAccessFrontOffice)
               .HasDefaultValue(false);

        builder.HasOne(x => x.Company)
               .WithMany()
               .HasForeignKey(x => x.CompanyId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Restaurant)
               .WithMany()
               .HasForeignKey(x => x.RestaurantId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.CompanyId);
        builder.HasIndex(x => x.RestaurantId);
        builder.HasIndex(x => x.WorkplaceType);

        builder.HasIndex(x => new { x.CompanyId, x.Code })
               .IsUnique()
               .HasFilter("[Code] IS NOT NULL");

        builder.HasIndex(x => x.RfidCardId)
               .IsUnique()
               .HasFilter("[RfidCardId] IS NOT NULL");
    }
}