using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.Configurations;

public class OrderPaymentLineConfiguration : IEntityTypeConfiguration<OrderPaymentLine>
{
    public void Configure(EntityTypeBuilder<OrderPaymentLine> builder)
    {
        builder.ToTable("OrderPaymentLines");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Amount).HasColumnType("decimal(18,2)");

        builder.HasOne(x => x.OrderPayment)
            .WithMany(x => x.Lines)
            .HasForeignKey(x => x.OrderPaymentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.OrderLine)
            .WithMany()
            .HasForeignKey(x => x.OrderLineId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.OrderLineId);
    }
}
