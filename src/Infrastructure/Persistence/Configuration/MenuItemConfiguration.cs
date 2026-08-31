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

        builder.Property(x => x.WeightCode)
            .HasMaxLength(50);

        builder.Property(x => x.Barcode)
            .HasMaxLength(50);

        builder.Property(x => x.VatPercent).HasColumnType("decimal(5,2)");
        builder.Property(x => x.StationPrice).HasColumnType("decimal(18,2)");
        builder.Property(x => x.PurchasePrice).HasColumnType("decimal(18,4)");
        builder.Property(x => x.PackagePrice).HasColumnType("decimal(18,2)");
        builder.Property(x => x.SpecialPrice1).HasColumnType("decimal(18,2)");
        builder.Property(x => x.SpecialPrice2).HasColumnType("decimal(18,2)");
        builder.Property(x => x.SpecialPrice3).HasColumnType("decimal(18,2)");
        builder.Property(x => x.SpecialPrice4).HasColumnType("decimal(18,2)");
        builder.Property(x => x.SpecialPrice5).HasColumnType("decimal(18,2)");

        builder.HasIndex(x => new { x.CompanyId, x.WeightCode }).IsUnique().HasFilter("[WeightCode] IS NOT NULL");
        builder.HasIndex(x => new { x.CompanyId, x.Barcode }).IsUnique().HasFilter("[Barcode] IS NOT NULL");

        builder.HasOne(x => x.MenuCategory)
            .WithMany(x => x.MenuItems)
            .HasForeignKey(x => x.MenuCategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Printer)
            .WithMany()
            .HasForeignKey(x => x.PrinterId)
            .OnDelete(DeleteBehavior.ClientSetNull);

        builder.HasOne(x => x.ItemType)
            .WithMany()
            .HasForeignKey(x => x.ItemTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.StockItem)
            .WithMany()
            .HasForeignKey(x => x.StockItemId)
            .OnDelete(DeleteBehavior.ClientSetNull);

        builder.HasMany(x => x.OrderLines)
            .WithOne(x => x.MenuItem)
            .HasForeignKey(x => x.MenuItemId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}