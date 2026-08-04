using Domain.Entities.BscInvoice;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class BscInvoiceDConfiguration : IEntityTypeConfiguration<BscInvoiceD>
{
    public void Configure(EntityTypeBuilder<BscInvoiceD> builder)
    {
        builder.ToTable("BscInvoiceDs");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.BscInvoiceDId).IsRequired();
        builder.HasIndex(x => x.BscInvoiceDId).IsUnique();

        builder.Property(x => x.Qty).HasColumnType("decimal(18,4)");
        builder.Property(x => x.UnitPrice).HasColumnType("decimal(18,4)");
        builder.Property(x => x.Amt).HasColumnType("decimal(18,4)");
        builder.Property(x => x.AmtVat).HasColumnType("decimal(18,4)");
        builder.Property(x => x.VatRate).HasColumnType("decimal(18,4)");
    }
}
