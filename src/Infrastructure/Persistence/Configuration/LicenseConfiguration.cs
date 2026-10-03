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

public class LicensePaymentConfiguration : IEntityTypeConfiguration<LicensePayment>
{
    public void Configure(EntityTypeBuilder<LicensePayment> builder)
    {
        builder.ToTable("LicensePayments");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Amount).HasColumnType("decimal(18,2)");
        builder.Property(x => x.Note).HasMaxLength(500);
        builder.HasOne(x => x.CreatedByUser)
            .WithMany()
            .HasForeignKey(x => x.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.CompanyId, x.CreatedAtUtc });
    }
}

public class TrustedDeviceConfiguration : IEntityTypeConfiguration<TrustedDevice>
{
    public void Configure(EntityTypeBuilder<TrustedDevice> builder)
    {
        builder.ToTable("TrustedDevices");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).IsRequired().HasMaxLength(100);
        builder.Property(x => x.KeyHash).IsRequired().HasMaxLength(64);
        builder.HasIndex(x => x.KeyHash).IsUnique();
        builder.HasIndex(x => x.CompanyId);
    }
}

public class DeviceRegistrationCodeConfiguration : IEntityTypeConfiguration<DeviceRegistrationCode>
{
    public void Configure(EntityTypeBuilder<DeviceRegistrationCode> builder)
    {
        builder.ToTable("DeviceRegistrationCodes");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.CodeHash).IsRequired().HasMaxLength(64);
        builder.HasIndex(x => x.CodeHash);
    }
}
