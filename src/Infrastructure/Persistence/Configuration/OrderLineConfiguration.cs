using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.Configurations;

public class OrderLineConfiguration : IEntityTypeConfiguration<OrderLine>
{
    public void Configure(EntityTypeBuilder<OrderLine> builder)
    {
        builder.ToTable("OrderLines");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Quantity)
            .IsRequired();

        builder.Property(x => x.UnitPrice)
            .HasColumnType("decimal(18,2)");

        builder.Property(x => x.Note)
            .HasMaxLength(500);

        builder.Property(x => x.Status)
            .IsRequired();

        builder.HasOne(x => x.Order)
       .WithMany(x => x.Lines)
       .HasForeignKey(x => x.OrderId)
       .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.MenuItem)
            .WithMany(x => x.OrderLines)
            .HasForeignKey(x => x.MenuItemId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(x => x.UnitPrice)
               .HasColumnType("decimal(18,2)");

        builder.Property(x => x.LineTotal)
              .HasColumnType("decimal(18,2)");
    }
}