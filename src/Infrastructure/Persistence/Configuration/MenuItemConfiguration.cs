using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.Configurations;

public class MenuItemConfiguration : IEntityTypeConfiguration<MenuItem>
{
    public void Configure(EntityTypeBuilder<MenuItem> builder)
    {
        builder.ToTable("MenuItems");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(x => x.Description)
            .HasMaxLength(1000);

        builder.Property(x => x.Price)
            .HasColumnType("decimal(18,2)");

        builder.Property(x => x.Portion)
            .HasMaxLength(100);

        builder.Property(x => x.IsActive)
            .IsRequired();

        builder.HasOne(x => x.MenuCategory)
            .WithMany(x => x.MenuItems)
            .HasForeignKey(x => x.MenuCategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.OrderLines)
            .WithOne(x => x.MenuItem)
            .HasForeignKey(x => x.MenuItemId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}