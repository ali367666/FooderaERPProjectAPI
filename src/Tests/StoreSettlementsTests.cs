using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.StoreMode.Reports;
using Domain.Entities.StoreMode;
using Domain.Enums;
using NSubstitute;
using Xunit;

namespace Tests;

public class StoreSettlementsTests
{
    private static StoreLedgerEntry Entry(int partyId, decimal amount, StoreLedgerEntryType type = StoreLedgerEntryType.Purchase)
        => new() { PartyType = StoreLedgerPartyType.Supplier, PartyId = partyId, Amount = amount, Type = type, DocumentNo = "D" };

    [Fact]
    public async Task Opening_increase_decrease_and_closing_add_up_per_supplier()
    {
        var trade = Substitute.For<IStoreTradeRepository>();
        trade.GetLedgerBalancesBeforeAsync(1, StoreLedgerPartyType.Supplier, Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<int, decimal> { [10] = 1000m, [20] = 0m });
        trade.GetLedgerForAllAsync(1, StoreLedgerPartyType.Supplier, Arg.Any<DateTime?>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(new List<StoreLedgerEntry> { Entry(10, 300m), Entry(10, -100m, StoreLedgerEntryType.SupplierPayment), Entry(30, 50m) });
        trade.GetSuppliersAsync(1, true, Arg.Any<CancellationToken>())
            .Returns(new List<StoreSupplier> { new() { Id = 10, Name = "B" }, new() { Id = 20, Name = "Z" }, new() { Id = 30, Name = "A" } });
        var user = Substitute.For<ICurrentUserService>();
        user.CompanyId.Returns(1);

        var handler = new StoreSettlementsHandler(trade, Substitute.For<IStoreCustomerRepository>(), user);
        var result = await handler.Handle(new GetStoreSettlementsQuery("suppliers", new DateTime(2026, 11, 1), new DateTime(2026, 11, 12)), default);

        // Supplier 20 has nothing at the start and nothing in the period: left out. The rows are sorted by name.
        Assert.Equal(new[] { "A", "B" }, result.Rows.Select(r => r.Name));
        var b = result.Rows.Single(r => r.PartyId == 10);
        Assert.Equal((1000m, 300m, 100m, 1200m), (b.OpeningDebt, b.Increase, b.Decrease, b.ClosingDebt));
        Assert.Equal(2, b.Entries.Count);
        Assert.Equal(1250m, result.ClosingDebt);
        Assert.Equal(1000m, result.OpeningDebt);
    }
}
