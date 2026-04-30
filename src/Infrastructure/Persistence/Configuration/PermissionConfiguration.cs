using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.Configurations;

public class PermissionConfiguration : IEntityTypeConfiguration<Permission>
{
    public void Configure(EntityTypeBuilder<Permission> builder)
    {
        builder.ToTable("Permissions");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name).IsRequired().HasMaxLength(200);
        builder.Property(x => x.DisplayName).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Module).IsRequired().HasMaxLength(100);
        builder.Property(x => x.Action).IsRequired().HasMaxLength(100);

        builder.HasIndex(x => x.Name).IsUnique();
    }
}
