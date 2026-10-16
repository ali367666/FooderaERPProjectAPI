using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.Configurations;

public class CompanyLicenseConfiguration : IEntityTypeConfiguration<CompanyLicense>
{
    public void Configure(EntityTypeBuilder<CompanyLicense> builder)
    {
        builder.ToTable("CompanyLicenses");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.CompanyId).IsUnique();
        builder.Property(x => x.DeploymentType).HasConversion<int>();
        builder.Property(x => x.MonthlyPrice).HasColumnType("decimal(18,2)");
    }
}
