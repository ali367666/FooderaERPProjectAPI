using Domain.Constants;
using Domain.Entities;
using Infrastructure.Persistence.Context;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Tests;

public class PaymentBankProvisionerTests
{
    private const int Company = 1;

    private static (AppDbContext Db, PaymentBankProvisioner Provisioner) Create()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var db = new AppDbContext(options);
        return (db, new PaymentBankProvisioner(db));
    }

    [Fact]
    public void The_catalog_holds_rabitabank_and_unibank()
    {
        Assert.Contains("Rabitəbank", PaymentBankCatalog.Names);
        Assert.Contains("Unibank", PaymentBankCatalog.Names);
        Assert.True(PaymentBankCatalog.IsBankName("  unibank "));
    }

    [Fact]
    public async Task A_new_company_has_no_banks_until_they_are_given()
    {
        var (_, provisioner) = Create();

        Assert.All(await provisioner.GetAsync(Company, default), b => Assert.False(b.Enabled));
    }

    [Fact]
    public async Task Given_banks_are_created_as_active_bank_counterparties()
    {
        var (db, provisioner) = Create();

        await provisioner.SetAsync(Company, new[] { "Kapital", "Rabitəbank" }, default);

        var banks = await db.Counterparties.Where(c => c.CompanyId == Company).ToListAsync();
        Assert.Equal(2, banks.Count);
        Assert.All(banks, b => { Assert.True(b.IsBank); Assert.True(b.IsActive); });
        var status = await provisioner.GetAsync(Company, default);
        Assert.True(status.Single(s => s.Name == "Rabitəbank").Enabled);
        Assert.False(status.Single(s => s.Name == "Unibank").Enabled);
    }

    [Fact]
    public async Task A_bank_taken_away_is_switched_off_not_deleted_and_can_come_back()
    {
        var (db, provisioner) = Create();
        await provisioner.SetAsync(Company, new[] { "Kapital", "Unibank" }, default);

        await provisioner.SetAsync(Company, new[] { "Kapital" }, default);

        var unibank = await db.Counterparties.SingleAsync(c => c.Name == "Unibank");
        Assert.False(unibank.IsActive);
        Assert.True(unibank.IsBank);

        await provisioner.SetAsync(Company, new[] { "Kapital", "Unibank" }, default);
        Assert.True((await db.Counterparties.SingleAsync(c => c.Name == "Unibank")).IsActive);
        Assert.Equal(2, await db.Counterparties.CountAsync());
    }

    [Fact]
    public async Task One_company_does_not_touch_the_banks_of_another()
    {
        var (db, provisioner) = Create();
        await provisioner.SetAsync(1, new[] { "ABB" }, default);
        await provisioner.SetAsync(2, new[] { "Unibank" }, default);

        Assert.True((await provisioner.GetAsync(1, default)).Single(s => s.Name == "ABB").Enabled);
        Assert.False((await provisioner.GetAsync(2, default)).Single(s => s.Name == "ABB").Enabled);
    }
}
