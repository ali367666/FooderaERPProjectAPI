using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.Configurations;

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
