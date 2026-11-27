using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.Configurations;

public class CompanyDepositConfiguration : IEntityTypeConfiguration<CompanyDeposit>
{
    public void Configure(EntityTypeBuilder<CompanyDeposit> builder)
    {
        builder.ToTable("CompanyDeposits");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Amount).HasColumnType("decimal(18,2)");
        builder.Property(x => x.Note).HasMaxLength(500);

        builder.HasOne(x => x.Company).WithMany()
            .HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);

        // Only one opening deposit per company.
        builder.HasIndex(x => x.CompanyId).IsUnique().HasFilter("[IsOpening] = 1")
            .HasDatabaseName("IX_CompanyDeposits_CompanyId_Opening");
    }
}
