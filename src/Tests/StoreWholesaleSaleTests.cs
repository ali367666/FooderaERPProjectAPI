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

/// <summary>The wholesale sale of the admin panel (topdan satış): wholesale price, another warehouse, another cashier's shift, partly on credit.</summary>
public class StoreWholesaleSaleTests
{
    private const int Company = 1, Branch = 1, Warehouse = 1, Register = 1, Product = 1;

    private readonly AppDbContext _db;
    private readonly StoreSalesHandlers _handlers;

    public StoreWholesaleSaleTests()
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
        _db.StoreCashShifts.Add(new StoreCashShift { Id = 1, CompanyId = Company, StoreCashRegisterId = Register, RestaurantId = Branch, CashierUserId = 99, OpeningCash = 0 });
        _db.StoreProducts.Add(new StoreProduct { Id = Product, CompanyId = Company, Name = "Süd", SalePrice = 2m, WholesalePrice = 1.5m, AverageCost = 1m, Unit = UnitOfMeasure.Piece });
        _db.StoreStockBalances.Add(new StoreStockBalance { CompanyId = Company, WarehouseId = Warehouse, StoreProductId = Product, Quantity = 10, RowVersion = new byte[8] });
        _db.Warehouses.Add(new Domain.Entities.Warehouse { Id = 2, CompanyId = Company, RestaurantId = Branch, Name = "Mərkəzi anbar" });
        _db.StoreStockBalances.Add(new StoreStockBalance { CompanyId = Company, WarehouseId = 2, StoreProductId = Product, Quantity = 100, RowVersion = new byte[8] });
        _db.StoreCustomers.Add(new StoreCustomer { Id = 1, CompanyId = Company, Code = "C1", Name = "Topdan alıcı", CreditLimit = 1000m, IsActive = true });
        _db.SaveChanges();
    }

    private static CreateStoreSaleRequest Wholesale(decimal quantity, decimal cash, decimal debt) => new()
    {
        OperationId = Guid.NewGuid(),
        StoreCashRegisterId = Register,
        WarehouseId = 2,
        CustomerId = 1,
        Wholesale = true,
        Lines = { new CreateStoreSaleLineRequest { ProductId = Product, Quantity = quantity } },
        Payments =
        {
            new CreateStoreSalePaymentRequest { Method = StorePaymentMethod.Cash, Amount = cash },
            new CreateStoreSalePaymentRequest { Method = StorePaymentMethod.Debt, Amount = debt },
        },
    };

    private decimal Stock(int warehouse) => _db.StoreStockBalances.AsNoTracking().Single(b => b.WarehouseId == warehouse).Quantity;

    [Fact]
    public async Task A_wholesale_sale_uses_the_wholesale_price_takes_goods_from_the_chosen_warehouse_and_the_rest_goes_on_credit()
    {
        var sale = await _handlers.Handle(new CreateStoreSaleCommand(Wholesale(10, cash: 5m, debt: 10m)), default); // 10 x 1.5 = 15

        Assert.True(sale.IsWholesale);
        Assert.Equal(15m, sale.Total);
        Assert.Equal(1.5m, sale.Lines.Single().UnitPrice);
        Assert.Equal(90m, Stock(2));
        Assert.Equal(10m, Stock(1)); // the till's own warehouse is untouched
        Assert.Equal(10m, sale.DebtAmount);
        Assert.Equal(2, _db.StoreSales.AsNoTracking().Single().WarehouseId);
    }

    [Fact]
    public async Task Without_the_manager_permission_a_wholesale_sale_is_refused()
    {
        var user = Substitute.For<ICurrentUserService>();
        user.UserId.Returns(7);
        user.CompanyId.Returns(Company);
        user.HasPermission(Arg.Any<string>()).Returns(false);
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

        await Assert.ThrowsAsync<BadRequestException>(() => processor.PrepareAsync(Wholesale(1, 1.5m, 0m), 0m, null, default));
    }

    [Fact]
    public async Task A_normal_sale_still_needs_the_cashiers_own_shift()
    {
        var request = new CreateStoreSaleRequest
        {
            OperationId = Guid.NewGuid(),
            StoreCashRegisterId = Register,
            Lines = { new CreateStoreSaleLineRequest { ProductId = Product, Quantity = 1 } },
            Payments = { new CreateStoreSalePaymentRequest { Method = StorePaymentMethod.Cash, Amount = 2m } },
        };

        await Assert.ThrowsAsync<BadRequestException>(() => _handlers.Handle(new CreateStoreSaleCommand(request), default));
    }
}
