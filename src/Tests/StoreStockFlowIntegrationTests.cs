using Application.Common.Exceptions;
using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Services;
using Application.StoreMode;
using Application.StoreMode.Access;
using Application.StoreMode.Approvals;
using Application.StoreMode.Customers;
using Application.StoreMode.Fiscal;
using Application.StoreMode.Notifications;
using Application.StoreMode.Pricing;
using Application.StoreMode.Returns;
using Application.StoreMode.Sales;
using Application.StoreMode.Trade;
using Application.StoreMode.WarehouseOps;
using Domain.Entities;
using Domain.Entities.StoreMode;
using Domain.Enums;
using Infrastructure.Persistence.Context;
using Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Persistence.UnitOfWork;
using Xunit;

namespace Tests;

/// <summary>Purchases, transfers and returns on the real handlers and repositories (EF InMemory): stock and supplier balance follow the documents.</summary>
public class StoreStockFlowIntegrationTests
{
    private const int Company = 1, Product = 1, Supplier = 1, Register = 1;
    private const int WhA = 1, WhB = 2;

    private readonly AppDbContext _db;
    private readonly StorePurchasesHandlers _purchases;
    private readonly StoreTransfersHandlers _transfers;
    private readonly StoreSalesHandlers _sales;
    private readonly StoreReturnsHandlers _returns;
    private readonly StoreTradeRepository _trade;
    private readonly StorePaymentReversalHandlers _reversal;

    public StoreStockFlowIntegrationTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        _db = new InMemoryAppDbContext(options);

        var user = Substitute.For<ICurrentUserService>();
        user.UserId.Returns(7);
        user.CompanyId.Returns(Company);
        user.HasPermission(Arg.Any<string>()).Returns(true);

        var store = new StoreRepository(_db);
        _trade = new StoreTradeRepository(_db);
        var customers = new StoreCustomerRepository(_db);
        var ops = new StoreWarehouseOpsRepository(_db);
        var accessRepo = new StoreAccessRepository(_db);
        var identity = Substitute.For<IIdentityAdminService>();
        var uow = new UnitOfWork(_db);

        var approvals = new StoreApprovalService(user, identity, store, Substitute.For<Application.Common.Interfaces.Abstracts.Services.IStaffCodeResolver>());
        var access = new StoreAccessService(accessRepo, user);
        var pricing = new StorePricingService(new StorePricingRepository(_db), accessRepo, user);
        var fiscal = new StoreFiscalService(Substitute.For<IStoreFiscalDevice>(), store, NullLogger<StoreFiscalService>.Instance);
        var saleProcessor = new StoreSaleProcessor(store, customers, trade: _trade, currentUser: user, approvals: approvals,
            bonusExpiry: new StoreBonusExpiryService(customers, store), fiscal: fiscal, access: access, pricing: pricing);
        var notifier = new StoreNotifier(identity, Substitute.For<INotificationService>(), user, NullLogger<StoreNotifier>.Instance);

        _sales = new StoreSalesHandlers(access, store, user, uow, saleProcessor, approvals, customers, _trade, notifier, Substitute.For<ICompanySettingsRepository>());
        _purchases = new StorePurchasesHandlers(_trade, store, user, uow, new StorePaymentService(_trade, user), access);
        _transfers = new StoreTransfersHandlers(ops, store, user, uow, notifier, access);
        _returns = new StoreReturnsHandlers(store, user, uow, new StoreReturnProcessor(store, customers, _trade, user, approvals, access), saleProcessor, access);

        _reversal = new StorePaymentReversalHandlers(customers, _trade, store, user, uow, Substitute.For<MediatR.IMediator>());

        _db.Restaurants.Add(new Restaurant { Id = 1, CompanyId = Company, Name = "Filial" });
        _db.Warehouses.Add(new Warehouse { Id = WhA, CompanyId = Company, RestaurantId = 1, Name = "A" });
        _db.Warehouses.Add(new Warehouse { Id = WhB, CompanyId = Company, RestaurantId = 1, Name = "B" });
        _db.StoreCashRegisters.Add(new StoreCashRegister { Id = Register, CompanyId = Company, RestaurantId = 1, WarehouseId = WhA, Name = "K1" });
        _db.StoreCashShifts.Add(new StoreCashShift { Id = 1, CompanyId = Company, StoreCashRegisterId = Register, RestaurantId = 1, CashierUserId = 7 });
        _db.StoreProducts.Add(new StoreProduct { Id = Product, CompanyId = Company, Name = "Süd", SalePrice = 2m, Unit = UnitOfMeasure.Piece });
        _db.StoreSuppliers.Add(new StoreSupplier { Id = Supplier, CompanyId = Company, Name = "Təchizatçı" });
        _db.SaveChanges();
    }

    private decimal Stock(int warehouse) =>
        _db.StoreStockBalances.AsNoTracking().Where(b => b.WarehouseId == warehouse && b.StoreProductId == Product).Select(b => b.Quantity).FirstOrDefault();

    private async Task<StorePurchaseResponse> ConfirmedPurchase(decimal quantity, decimal unitPrice, decimal? paid = null)
    {
        var draft = await _purchases.Handle(new SaveStorePurchaseCommand(null, new SaveStorePurchaseRequest
        {
            SupplierId = Supplier,
            WarehouseId = WhA,
            Lines = { new SaveStorePurchaseLineRequest { ProductId = Product, Quantity = quantity, UnitPrice = unitPrice } },
        }), default);
        return await _purchases.Handle(new ConfirmStorePurchaseCommand(draft.Id, new ConfirmStorePurchaseRequest
        {
            Payment = paid is { } p ? new ConfirmPurchasePayment { Amount = p } : null,
        }), default);
    }

    [Fact]
    public async Task Draft_purchase_changes_neither_stock_nor_debt()
    {
        await _purchases.Handle(new SaveStorePurchaseCommand(null, new SaveStorePurchaseRequest
        {
            SupplierId = Supplier,
            WarehouseId = WhA,
            Lines = { new SaveStorePurchaseLineRequest { ProductId = Product, Quantity = 5, UnitPrice = 1m } },
        }), default);

        Assert.Equal(0m, Stock(WhA));
        Assert.Equal(0m, await _trade.GetLedgerBalanceAsync(Company, StoreLedgerPartyType.Supplier, Supplier, default));
    }

    [Fact]
    public async Task Confirmed_purchase_raises_stock_and_creates_supplier_debt_for_the_unpaid_part()
    {
        await ConfirmedPurchase(10, 1m, paid: 4m);

        Assert.Equal(10m, Stock(WhA));
        Assert.Equal(6m, await _trade.GetLedgerBalanceAsync(Company, StoreLedgerPartyType.Supplier, Supplier, default));
    }

    [Fact]
    public async Task Confirmed_purchase_sets_the_weighted_average_cost()
    {
        await ConfirmedPurchase(10, 1m);
        await ConfirmedPurchase(10, 3m);

        Assert.Equal(2m, _db.StoreProducts.AsNoTracking().Single().AverageCost);
    }

    [Fact]
    public async Task Reversing_a_supplier_payment_brings_the_debt_back_and_cannot_be_done_twice()
    {
        await ConfirmedPurchase(10, 1m, paid: 4m);
        Assert.Equal(6m, await _trade.GetLedgerBalanceAsync(Company, StoreLedgerPartyType.Supplier, Supplier, default));
        var paymentId = _db.StoreSupplierPayments.AsNoTracking().Single().Id;

        var reversed = await _reversal.Handle(new ReverseStoreSupplierPaymentCommand(paymentId, new ReverseStorePaymentRequest { Reason = "səhv" }), default);

        Assert.True(reversed.IsReversed);
        Assert.Equal(10m, await _trade.GetLedgerBalanceAsync(Company, StoreLedgerPartyType.Supplier, Supplier, default));
        // the purchase counts as unpaid again
        var purchaseId = _db.StorePurchases.AsNoTracking().Single().Id;
        Assert.Equal(0m, (await _trade.GetPaidByPurchaseAsync(new[] { purchaseId }, default)).GetValueOrDefault(purchaseId));

        await Assert.ThrowsAsync<BadRequestException>(() =>
            _reversal.Handle(new ReverseStoreSupplierPaymentCommand(paymentId, new ReverseStorePaymentRequest { Reason = "yenə" }), default));
    }

    [Fact]
    public async Task Reversal_needs_a_reason()
    {
        await ConfirmedPurchase(5, 1m, paid: 2m);
        var paymentId = _db.StoreSupplierPayments.AsNoTracking().Single().Id;

        await Assert.ThrowsAsync<BadRequestException>(() =>
            _reversal.Handle(new ReverseStoreSupplierPaymentCommand(paymentId, new ReverseStorePaymentRequest { Reason = " " }), default));
    }

    [Fact]
    public async Task Receiptless_sale_is_refused_unless_the_company_allows_it()
    {
        await ConfirmedPurchase(5, 1m);
        var request = new CreateStoreSaleRequest
        {
            OperationId = Guid.NewGuid(),
            StoreCashRegisterId = Register,
            Receiptless = true,
            Lines = { new CreateStoreSaleLineRequest { ProductId = Product, Quantity = 1 } },
            Payments = { new CreateStoreSalePaymentRequest { Method = StorePaymentMethod.Cash, Amount = 2m } },
        };
        await Assert.ThrowsAsync<BadRequestException>(() => _sales.Handle(new CreateStoreSaleCommand(request), default));

        _db.StoreSettings.Add(new StoreSettings { CompanyId = Company, AllowReceiptlessSale = true });
        _db.SaveChanges();
        var sale = await _sales.Handle(new CreateStoreSaleCommand(request), default);
        Assert.True(sale.IsReceiptless);
    }

    [Fact]
    public async Task Transfer_moves_stock_in_two_steps_and_shows_the_shortage()
    {
        await ConfirmedPurchase(10, 1m);

        var draft = await _transfers.Handle(new SaveStoreTransferCommand(null, new SaveStoreTransferRequest
        {
            FromWarehouseId = WhA,
            ToWarehouseId = WhB,
            Lines = { new SaveStoreTransferLineRequest { ProductId = Product, Quantity = 6 } },
        }), default);
        await _transfers.Handle(new SubmitStoreTransferCommand(draft.Id), default);
        var shipped = await _transfers.Handle(new ShipStoreTransferCommand(draft.Id), default);

        // sent: gone from the source, not yet at the destination
        Assert.Equal(4m, Stock(WhA));
        Assert.Equal(0m, Stock(WhB));

        await _transfers.Handle(new ReceiveStoreTransferCommand(draft.Id, new ReceiveStoreTransferRequest
        {
            Lines = { new ReceiveStoreTransferLineRequest { LineId = shipped.Lines[0].Id, Quantity = 5 } },
            CloseWithShortage = true,
        }), default);

        Assert.Equal(5m, Stock(WhB));
        Assert.Equal(4m, Stock(WhA));
    }

    [Fact]
    public async Task Transfer_cannot_ship_more_than_the_source_has()
    {
        await ConfirmedPurchase(2, 1m);
        var draft = await _transfers.Handle(new SaveStoreTransferCommand(null, new SaveStoreTransferRequest
        {
            FromWarehouseId = WhA,
            ToWarehouseId = WhB,
            Lines = { new SaveStoreTransferLineRequest { ProductId = Product, Quantity = 5 } },
        }), default);
        await _transfers.Handle(new SubmitStoreTransferCommand(draft.Id), default);

        await Assert.ThrowsAsync<BadRequestException>(() => _transfers.Handle(new ShipStoreTransferCommand(draft.Id), default));
        Assert.Equal(2m, Stock(WhA));
    }

    [Fact]
    public async Task Sellable_return_puts_stock_back_and_cannot_exceed_what_was_sold()
    {
        await ConfirmedPurchase(10, 1m);
        var sale = await _sales.Handle(new CreateStoreSaleCommand(new CreateStoreSaleRequest
        {
            OperationId = Guid.NewGuid(),
            StoreCashRegisterId = Register,
            Lines = { new CreateStoreSaleLineRequest { ProductId = Product, Quantity = 4 } },
            Payments = { new CreateStoreSalePaymentRequest { Method = StorePaymentMethod.Cash, Amount = 8m } },
        }), default);
        Assert.Equal(6m, Stock(WhA));

        var lineId = sale.Lines[0].LineId;
        await _returns.Handle(new CreateStoreReturnCommand(new CreateStoreReturnRequest
        {
            OperationId = Guid.NewGuid(),
            StoreSaleId = sale.Id,
            Reason = "x",
            Lines = { new CreateStoreReturnLineRequest { SaleLineId = lineId, Quantity = 3 } },
        }), default);
        Assert.Equal(9m, Stock(WhA));

        // only 1 of the 4 is still returnable
        await Assert.ThrowsAsync<BadRequestException>(() => _returns.Handle(new CreateStoreReturnCommand(new CreateStoreReturnRequest
        {
            OperationId = Guid.NewGuid(),
            StoreSaleId = sale.Id,
            Reason = "x",
            Lines = { new CreateStoreReturnLineRequest { SaleLineId = lineId, Quantity = 2 } },
        }), default));
        Assert.Equal(9m, Stock(WhA));
    }
}

/// <summary>EF InMemory does not generate SQL Server row versions; this gives new balances one so the code under test can save them.</summary>
public sealed class InMemoryAppDbContext : AppDbContext
{
    public InMemoryAppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        foreach (var e in ChangeTracker.Entries<StoreStockBalance>().Where(e => e.State == EntityState.Added && e.Entity.RowVersion is null))
            e.Entity.RowVersion = new byte[8];
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }
}
