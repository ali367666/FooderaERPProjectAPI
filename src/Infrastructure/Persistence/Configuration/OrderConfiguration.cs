using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.Configurations;

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("Orders");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.OrderNumber)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.Note)
            .HasMaxLength(1000);

        builder.Property(x => x.Status)
            .IsRequired();

        builder.Property(x => x.OpenedAt)
            .IsRequired();

        builder.Property(x => x.ClosedAt);

        builder.HasOne(x => x.Table)
            .WithMany(x => x.Orders)
            .HasForeignKey(x => x.TableId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Waiter)
            .WithMany()
            .HasForeignKey(x => x.WaiterId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.ProcessedByUser)
            .WithMany()
            .HasForeignKey(x => x.ProcessedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.Lines)
       .WithOne(x => x.Order)
       .HasForeignKey(x => x.OrderId)
       .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.CompanyId, x.OrderNumber })
            .IsUnique();

        builder.Property(x => x.TotalAmount)
                .HasColumnType("decimal(18,2)")
                .HasDefaultValue(0);

        builder.Property(x => x.PaidAmount)
            .HasColumnType("decimal(18,2)")
            .HasDefaultValue(0);

        builder.Property(x => x.ChangeAmount)
            .HasColumnType("decimal(18,2)")
            .HasDefaultValue(0);

        builder.Property(x => x.ReceiptNumber)
            .HasMaxLength(100);
    }
}