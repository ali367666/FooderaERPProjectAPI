using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.Configurations;

public class RestaurantTableConfiguration : IEntityTypeConfiguration<RestaurantTable>
{
    public void Configure(EntityTypeBuilder<RestaurantTable> builder)
    {
        builder.ToTable("RestaurantTables");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.Capacity)
            .IsRequired();

        builder.Property(x => x.Type)
            .IsRequired()
            .HasConversion<int>()
            .HasDefaultValue(Domain.Enums.RestaurantTableType.Masa);

        builder.Property(x => x.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(x => x.IsOccupied)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(x => x.PosX).HasDefaultValue(0);
        builder.Property(x => x.PosY).HasDefaultValue(0);
        builder.Property(x => x.Width).HasDefaultValue(80);
        builder.Property(x => x.Height).HasDefaultValue(80);
        builder.Property(x => x.Shape).HasMaxLength(20).HasDefaultValue("square");
        builder.Property(x => x.Rotation).HasDefaultValue(0);
        builder.Property(x => x.HourlyRate).HasColumnType("decimal(18,2)");

        builder.HasIndex(x => new { x.CompanyId, x.Name })
            .IsUnique();

        builder.HasOne(x => x.Section)
            .WithMany(x => x.Tables)
            .HasForeignKey(x => x.SectionId)
            .OnDelete(DeleteBehavior.ClientSetNull);
    }
}