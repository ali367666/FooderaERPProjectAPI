using Domain.Entities.StoreMode;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.Configurations;

public class StoreCustomerConfiguration : IEntityTypeConfiguration<StoreCustomer>
{
    public void Configure(EntityTypeBuilder<StoreCustomer> b)
    {
        b.ToTable("StoreCustomers");
        b.HasKey(x => x.Id);
        b.Property(x => x.Code).IsRequired().HasMaxLength(30);
        b.Property(x => x.Name).IsRequired().HasMaxLength(200);
        b.Property(x => x.Phone).HasMaxLength(50);
        b.Property(x => x.Email).HasMaxLength(200);
        b.Property(x => x.Note).HasMaxLength(500);
        b.Property(x => x.CreditLimit).HasColumnType("decimal(18,2)");
        b.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.CompanyId, x.Code }).IsUnique();
        b.HasIndex(x => new { x.CompanyId, x.Phone });
    }
}

public class StoreBonusEntryConfiguration : IEntityTypeConfiguration<StoreBonusEntry>
{
    public void Configure(EntityTypeBuilder<StoreBonusEntry> b)
    {
        b.ToTable("StoreBonusEntries");
        b.HasKey(x => x.Id);
        b.Property(x => x.Type).HasConversion<int>();
        b.Property(x => x.Amount).HasColumnType("decimal(18,2)");
        b.Property(x => x.DocumentType).HasMaxLength(50);
        b.Property(x => x.DocumentNo).HasMaxLength(50);
        b.Property(x => x.Note).HasMaxLength(500);
        b.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.StoreCustomer).WithMany().HasForeignKey(x => x.StoreCustomerId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.StoreCustomerId, x.CreatedAtUtc });
    }
}

public class StoreCustomerPaymentConfiguration : IEntityTypeConfiguration<StoreCustomerPayment>
{
    public void Configure(EntityTypeBuilder<StoreCustomerPayment> b)
    {
        b.ToTable("StoreCustomerPayments");
        b.HasKey(x => x.Id);
        b.Property(x => x.DocumentNo).IsRequired().HasMaxLength(50);
        b.Property(x => x.Amount).HasColumnType("decimal(18,2)");
        b.Property(x => x.Method).HasConversion<int>();
        b.Property(x => x.Reference).HasMaxLength(100);
        b.Property(x => x.Note).HasMaxLength(500);
        b.Property(x => x.ReverseReason).HasMaxLength(500);
        b.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.StoreCustomer).WithMany().HasForeignKey(x => x.StoreCustomerId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.StoreCashShift).WithMany().HasForeignKey(x => x.StoreCashShiftId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.CompanyId, x.DocumentNo }).IsUnique();
    }
}
