using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class StockItemConfiguration : IEntityTypeConfiguration<StockItem>
{
    public void Configure(EntityTypeBuilder<StockItem> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
               .IsRequired()
               .HasMaxLength(200);

        builder.Property(x => x.Barcode)
               .HasMaxLength(100);

        builder.Property(x => x.Type)
               .IsRequired();

        builder.Property(x => x.Unit)
               .IsRequired();

        builder.Property(x => x.QuantityOnHand)
               .HasColumnType("decimal(18,2)");

        builder.Property(x => x.MinLevel)
               .HasColumnType("decimal(18,2)");

        // Category relation
        builder.HasOne(x => x.Category)
               .WithMany()
               .HasForeignKey(x => x.CategoryId)
               .OnDelete(DeleteBehavior.Restrict);

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

        // Eyni şirkətdə eyni adlı stock item təkrar olmasın
        builder.HasIndex(x => new { x.CompanyId, x.Name })
               .IsUnique();

        // Barcode varsa, eyni şirkətdə unikal olsun
        builder.HasIndex(x => new { x.CompanyId, x.Barcode })
               .IsUnique()
               .HasFilter("[Barcode] IS NOT NULL");
    }
}