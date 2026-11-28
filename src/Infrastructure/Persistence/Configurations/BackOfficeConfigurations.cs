using Domain.Entities;
using Domain.Entities.WarehouseAndStock;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class EmployeeLeaveConfiguration : IEntityTypeConfiguration<EmployeeLeave>
{
    public void Configure(EntityTypeBuilder<EmployeeLeave> builder)
    {
        builder.ToTable("EmployeeLeaves");
        builder.Property(x => x.Note).HasMaxLength(300);
        builder.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Employee).WithMany().HasForeignKey(x => x.EmployeeId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(x => new { x.CompanyId, x.FromDate, x.ToDate });
    }
}

public class CashExpenseCategoryConfiguration : IEntityTypeConfiguration<CashExpenseCategory>
{
    public void Configure(EntityTypeBuilder<CashExpenseCategory> builder)
    {
        builder.ToTable("CashExpenseCategories");
        builder.Property(x => x.Name).IsRequired().HasMaxLength(100);
        builder.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.CompanyId, x.Name }).IsUnique();
    }
}
