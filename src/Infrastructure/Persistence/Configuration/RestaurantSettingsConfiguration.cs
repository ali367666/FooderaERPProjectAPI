using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class RestaurantSettingsConfiguration : IEntityTypeConfiguration<RestaurantSettings>
{
    public void Configure(EntityTypeBuilder<RestaurantSettings> builder)
    {
        builder.HasKey(x => x.Id);

        builder.HasOne(x => x.Restaurant)
               .WithMany()
               .HasForeignKey(x => x.RestaurantId)
               .OnDelete(DeleteBehavior.Cascade);

        // hər filial üçün tək tənzimləmə sətri
        builder.HasIndex(x => x.RestaurantId)
               .IsUnique();
    }
}
