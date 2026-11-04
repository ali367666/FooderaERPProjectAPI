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
using Application.StoreMode.Sales;
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

/// <summary>The real sale handler and repositories on an in-memory database: stock, idempotency and cancellation.</summary>
public class StoreSaleIntegrationTests
{
    private const int Company = 1, Branch = 1, Warehouse = 1, Register = 1, Product = 1;

    private readonly AppDbContext _db;
    private readonly StoreSalesHandlers _handlers;

    public StoreSaleIntegrationTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        _db = new AppDbContext(options);

        var user = Substitute.For<ICurrentUserService>();
        user.UserId.Returns(7);
        user.CompanyId.Returns(Company);
        user.HasPermission(Arg.Any<string>()).Returns(true);

        var store = new StoreRepository(_db);
        var trade = new StoreTradeRepository(_db);
        var customers = new StoreCustomerRepository(_db);
        var accessRepo = new StoreAccessRepository(_db);
        var identity = Substitute.For<IIdentityAdminService>();

        var approvals = new StoreApprovalService(user, identity, store, Substitute.For<Application.Common.Interfaces.Abstracts.Services.IStaffCodeResolver>());
        var access = new StoreAccessService(accessRepo, user);
        var pricing = new StorePricingService(new StorePricingRepository(_db), accessRepo, user);
        var fiscal = new StoreFiscalService(Substitute.For<IStoreFiscalDevice>(), store, NullLogger<StoreFiscalService>.Instance);
        var processor = new StoreSaleProcessor(store, customers, trade, user, approvals, new StoreBonusExpiryService(customers, store), fiscal, access, pricing);
        var notifier = new StoreNotifier(identity, Substitute.For<INotificationService>(), user, NullLogger<StoreNotifier>.Instance);

        _handlers = new StoreSalesHandlers(access, store, user, new UnitOfWork(_db), processor, approvals, customers, trade, notifier, Substitute.For<ICompanySettingsRepository>());

        _db.Restaurants.Add(new Domain.Entities.Restaurant { Id = Branch, CompanyId = Company, Name = "Filial" });
        _db.Warehouses.Add(new Domain.Entities.Warehouse { Id = Warehouse, CompanyId = Company, RestaurantId = Branch, Name = "Anbar" });
        _db.StoreCashRegisters.Add(new StoreCashRegister { Id = Register, CompanyId = Company, RestaurantId = Branch, WarehouseId = Warehouse, Name = "K1" });
        _db.StoreCashShifts.Add(new StoreCashShift { Id = 1, CompanyId = Company, StoreCashRegisterId = Register, RestaurantId = Branch, CashierUserId = 7, OpeningCash = 0 });
        _db.StoreProducts.Add(new StoreProduct { Id = Product, CompanyId = Company, Name = "Süd", SalePrice = 2m, AverageCost = 1m, Unit = UnitOfMeasure.Piece });
        _db.StoreStockBalances.Add(new StoreStockBalance { CompanyId = Company, WarehouseId = Warehouse, StoreProductId = Product, Quantity = 10, RowVersion = new byte[8] });
        _db.SaveChanges();
    }

    private static CreateStoreSaleRequest Sale(decimal quantity, Guid? operationId = null) => new()
    {
        OperationId = operationId ?? Guid.NewGuid(),
        StoreCashRegisterId = Register,
        Lines = { new CreateStoreSaleLineRequest { ProductId = Product, Quantity = quantity } },
        Payments = { new CreateStoreSalePaymentRequest { Method = StorePaymentMethod.Cash, Amount = quantity * 2m } },
    };

    private decimal Stock() => _db.StoreStockBalances.AsNoTracking().Single().Quantity;

    [Fact]
    public async Task Sale_lowers_stock_and_writes_a_ledger_movement()
    {
        var sale = await _handlers.Handle(new CreateStoreSaleCommand(Sale(3)), default);

        Assert.Equal(6m, sale.Total);
        Assert.Equal(7m, Stock());
        var movement = _db.StoreStockMovements.AsNoTracking().Single();
        Assert.Equal(StoreStockMovementType.Sale, movement.Type);
        Assert.Equal(-3m, movement.Quantity);
        Assert.Equal(10m, movement.BalanceBefore);
        Assert.Equal(7m, movement.BalanceAfter);
    }

    [Fact]
    public async Task Resending_the_same_operation_does_not_sell_twice()
    {
        var operationId = Guid.NewGuid();
        var first = await _handlers.Handle(new CreateStoreSaleCommand(Sale(2, operationId)), default);
        var again = await _handlers.Handle(new CreateStoreSaleCommand(Sale(2, operationId)), default);

        Assert.Equal(first.Id, again.Id);
        Assert.Single(_db.StoreSales.AsNoTracking());
        Assert.Equal(8m, Stock());
    }

    [Fact]
    public async Task Selling_more_than_is_in_stock_is_refused_and_changes_nothing()
    {
        await Assert.ThrowsAsync<BadRequestException>(() => _handlers.Handle(new CreateStoreSaleCommand(Sale(11)), default));

        Assert.Equal(10m, Stock());
        Assert.Empty(_db.StoreSales.AsNoTracking());
    }

    [Fact]
    public async Task Payment_that_does_not_cover_the_total_is_refused()
    {
        var request = Sale(2);
        request.Payments[0].Amount = 1m;

        await Assert.ThrowsAsync<BadRequestException>(() => _handlers.Handle(new CreateStoreSaleCommand(request), default));
        Assert.Equal(10m, Stock());
    }

    [Fact]
    public async Task Cancelling_a_sale_puts_the_stock_back()
    {
        var sale = await _handlers.Handle(new CreateStoreSaleCommand(Sale(4)), default);
        Assert.Equal(6m, Stock());

        var cancelled = await _handlers.Handle(new CancelStoreSaleCommand(sale.Id, new CancelStoreSaleRequest { Reason = "səhv" }), default);

        Assert.Equal(StoreSaleStatus.Cancelled, cancelled.Status);
        Assert.Equal(10m, Stock());
    }
}
