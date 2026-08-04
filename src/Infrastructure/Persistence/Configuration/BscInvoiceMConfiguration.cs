using Domain.Entities.BscInvoice;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class BscInvoiceMConfiguration : IEntityTypeConfiguration<BscInvoiceM>
{
    public void Configure(EntityTypeBuilder<BscInvoiceM> builder)
    {
        builder.ToTable("BscInvoiceMs");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.BscInvoiceMId).IsRequired();
        builder.HasIndex(x => x.BscInvoiceMId).IsUnique();

        builder.Property(x => x.DocNo).HasMaxLength(50);
        builder.Property(x => x.Amt).HasColumnType("decimal(18,4)");
        builder.Property(x => x.AmtVat).HasColumnType("decimal(18,4)");

        builder.HasMany(x => x.Lines)
            .WithOne(x => x.InvoiceM)
            .HasForeignKey(x => x.BscInvoiceMId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
