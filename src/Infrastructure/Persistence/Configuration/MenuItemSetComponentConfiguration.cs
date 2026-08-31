using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.Configurations;

public class MenuItemSetComponentConfiguration : IEntityTypeConfiguration<MenuItemSetComponent>
{
    public void Configure(EntityTypeBuilder<MenuItemSetComponent> builder)
    {
        builder.ToTable("MenuItemSetComponents");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Quantity)
            .IsRequired()
            .HasDefaultValue(1);

        builder.HasOne(x => x.SetMenuItem)
            .WithMany(x => x.SetComponents)
            .HasForeignKey(x => x.SetMenuItemId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.ComponentMenuItem)
            .WithMany()
            .HasForeignKey(x => x.ComponentMenuItemId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.SetMenuItemId, x.ComponentMenuItemId })
            .IsUnique();
    }
}
