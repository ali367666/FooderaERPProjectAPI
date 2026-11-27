using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Common.Interfaces.Abstracts.Services;
using Application.Common.Models;
using Application.StoreMode;
using Application.StoreMode.Catalog;
using Application.StoreMode.Access;
using Application.StoreMode.Customers;
using Application.StoreMode.Pricing;
using Application.StoreMode.Sales;
using Domain.Entities.StoreMode;
using Domain.Enums;
using MediatR;
using NSubstitute;
using Xunit;

namespace Tests;

public class BonusExpiryTests
{
    private static (StoreBonusExpiryService Service, IStoreCustomerRepository Customers) Build(int expiryDays, List<StoreBonusEntry> entries)
    {
        var customers = Substitute.For<IStoreCustomerRepository>();
        var store = Substitute.For<IStoreRepository>();
        store.GetOrCreateSettingsAsync(1, Arg.Any<CancellationToken>()).Returns(new StoreSettings { BonusExpiryDays = expiryDays });
        customers.GetBonusEntriesAsync(1, 5, null, null, Arg.Any<CancellationToken>()).Returns(entries);
        return (new StoreBonusExpiryService(customers, store), customers);
    }

    private static StoreBonusEntry Entry(decimal amount, int daysAgo, StoreBonusEntryType type = StoreBonusEntryType.Earned)
        => new() { Amount = amount, Type = type, CreatedAtUtc = DateTime.UtcNow.AddDays(-daysAgo) };

    [Fact]
    public async Task Nothing_expires_when_period_is_zero()
    {
        var (service, customers) = Build(0, new() { Entry(10, 400) });
        await service.ExpireAsync(1, 5, default);
        await customers.DidNotReceive().AddBonusEntryAsync(Arg.Any<StoreBonusEntry>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Old_unused_bonus_expires()
    {
        var (service, customers) = Build(90, new() { Entry(10, 100), Entry(5, 10) });
        await service.ExpireAsync(1, 5, default);
        await customers.Received(1).AddBonusEntryAsync(
            Arg.Is<StoreBonusEntry>(e => e.Type == StoreBonusEntryType.Expired && e.Amount == -10m), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Spending_uses_the_oldest_bonus_first_so_only_the_rest_expires()
    {
        // 10 earned 100 days ago, 4 of it spent: only 6 can expire.
        var (service, customers) = Build(90, new() { Entry(10, 100), Entry(-4, 50, StoreBonusEntryType.Spent) });
        await service.ExpireAsync(1, 5, default);
        await customers.Received(1).AddBonusEntryAsync(
            Arg.Is<StoreBonusEntry>(e => e.Amount == -6m), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Running_again_after_expiry_adds_nothing()
    {
        var (service, customers) = Build(90, new() { Entry(10, 100), Entry(-10, 1, StoreBonusEntryType.Expired) });
        await service.ExpireAsync(1, 5, default);
        await customers.DidNotReceive().AddBonusEntryAsync(Arg.Any<StoreBonusEntry>(), Arg.Any<CancellationToken>());
    }
}

public class AuditBehaviorTests
{
    [Fact]
    public async Task Store_commands_are_audited_without_the_managers_password()
    {
        var audit = Substitute.For<IAuditLogService>();
        var user = Substitute.For<ICurrentUserService>();
        user.UserId.Returns(7);
        user.CompanyId.Returns(1);

        var behavior = new StoreAuditBehavior<CreateStoreSaleCommand, StoreSaleResponse>(audit, user, Substitute.For<IStoreRepository>());
        var command = new CreateStoreSaleCommand(new CreateStoreSaleRequest
        {
            OperationId = Guid.NewGuid(),
            Approval = new StoreApprovalRequest { UserName = "boss", Password = "secret-pass", Reason = "x" },
        });

        await behavior.Handle(command, () => Task.FromResult(new StoreSaleResponse { Id = 42, DocumentNo = "S-1" }), default);

        await audit.Received(1).LogAsync(
            Arg.Is<AuditLogEntry>(e => e.EntityId == "42" && e.UserId == 7 && e.NewValues != null && !e.NewValues.Contains("secret-pass")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Product_change_keeps_the_previous_prices()
    {
        var audit = Substitute.For<IAuditLogService>();
        var user = Substitute.For<ICurrentUserService>();
        user.CompanyId.Returns(1);
        var store = Substitute.For<IStoreRepository>();
        store.GetProductAsync(9, 1, Arg.Any<CancellationToken>()).Returns(new StoreProduct { Name = "Süd", SalePrice = 1.5m });
        var behavior = new StoreAuditBehavior<SaveStoreProductCommand, StoreProductResponse>(audit, user, store);

        await behavior.Handle(new SaveStoreProductCommand(9, new SaveStoreProductRequest { Name = "Süd", SalePrice = 2m }),
            () => Task.FromResult(new StoreProductResponse { Id = 9 }), default);

        await audit.Received(1).LogAsync(Arg.Is<AuditLogEntry>(e => e.OldValues!.Contains("1.5") && e.NewValues!.Contains("2")), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Queries_are_not_audited()
    {
        var audit = Substitute.For<IAuditLogService>();
        var behavior = new StoreAuditBehavior<GetStoreProductQuery, StoreProductResponse>(audit, Substitute.For<ICurrentUserService>(), Substitute.For<IStoreRepository>());
        await behavior.Handle(new GetStoreProductQuery(1), () => Task.FromResult(new StoreProductResponse()), default);
        await audit.DidNotReceiveWithAnyArgs().LogAsync(default!, default);
    }

    [Fact]
    public async Task A_failing_audit_never_breaks_the_operation()
    {
        var audit = Substitute.For<IAuditLogService>();
        audit.LogAsync(Arg.Any<AuditLogEntry>(), Arg.Any<CancellationToken>()).Returns<Task>(_ => throw new InvalidOperationException("db down"));
        var behavior = new StoreAuditBehavior<CreateStoreSaleCommand, StoreSaleResponse>(audit, Substitute.For<ICurrentUserService>(), Substitute.For<IStoreRepository>());

        var result = await behavior.Handle(new CreateStoreSaleCommand(new CreateStoreSaleRequest()), () => Task.FromResult(new StoreSaleResponse { Id = 1 }), default);

        Assert.Equal(1, result.Id);
    }
}

public class ProductCsvTests
{
    private static (StoreProductCsvHandlers Handlers, IMediator Mediator) Build()
    {
        var repo = Substitute.For<IStoreRepository>();
        repo.GetCategoriesAsync(1, Arg.Any<CancellationToken>()).Returns(new List<StoreCategory> { new() { Id = 3, Name = "Süd" } });
        var user = Substitute.For<ICurrentUserService>();
        user.CompanyId.Returns(1);
        var mediator = Substitute.For<IMediator>();
        return (new StoreProductCsvHandlers(repo, user, mediator), mediator);
    }

    [Fact]
    public async Task Valid_rows_create_products_and_bad_rows_are_reported_with_their_line()
    {
        var (handlers, mediator) = Build();
        var csv = StoreProductCsvHandlers.Header + "\n"
            + "Süd 1L;SKU1;111;Süd;Pinar;litr;1,10;1.80;;18;5;0;1\n"
            + "Xəta;SKU2;222;Yoxdur;;ədəd;1;2;;;;0;1\n";

        var result = await handlers.Handle(new ImportStoreProductsCommand(csv), default);

        Assert.Equal(1, result.Created);
        Assert.Single(result.Errors);
        Assert.Contains("Sətir 3", result.Errors[0]);
        await mediator.Received(1).Send(
            Arg.Is<SaveStoreProductCommand>(c => c.Id == null && c.Request.Name == "Süd 1L" && c.Request.PurchasePrice == 1.10m
                && c.Request.CategoryId == 3 && c.Request.Unit == UnitOfMeasure.Liter && c.Request.Barcodes.SequenceEqual(new[] { "111" })),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Quoted_cells_may_hold_separators()
    {
        var (handlers, mediator) = Build();
        var csv = StoreProductCsvHandlers.Header + "\n\"Çay; qara\";;;;;;1;2;;;;;\n";
        await handlers.Handle(new ImportStoreProductsCommand(csv), default);
        await mediator.Received(1).Send(Arg.Is<SaveStoreProductCommand>(c => c.Request.Name == "Çay; qara"), Arg.Any<CancellationToken>());
    }
}

public class PricingTests
{
    private static readonly DateTime Now = new(2026, 11, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Normal_price_when_nothing_else_applies() =>
        Assert.Equal(10m, StorePricingService.Effective(10m, null, null, null, null, Now));

    [Fact]
    public void Branch_price_replaces_the_normal_price() =>
        Assert.Equal(9m, StorePricingService.Effective(10m, null, null, null, 9m, Now));

    [Fact]
    public void Running_campaign_wins_over_branch_price() =>
        Assert.Equal(7m, StorePricingService.Effective(10m, 7m, Now.AddDays(-1), Now.AddDays(1), 9m, Now));

    [Fact]
    public void Expired_or_future_campaign_is_ignored()
    {
        Assert.Equal(9m, StorePricingService.Effective(10m, 7m, Now.AddDays(-5), Now.AddDays(-1), 9m, Now));
        Assert.Equal(10m, StorePricingService.Effective(10m, 7m, Now.AddDays(1), null, null, Now));
    }
}

public class AccessTests
{
    private static (StoreAccessService Service, IStoreAccessRepository Repo) Build(params int[] branches)
    {
        var repo = Substitute.For<IStoreAccessRepository>();
        repo.GetBranchIdsAsync(1, 5, Arg.Any<CancellationToken>()).Returns(branches.ToList());
        var user = Substitute.For<ICurrentUserService>();
        user.CompanyId.Returns(1);
        user.UserId.Returns(5);
        return (new StoreAccessService(repo, user), repo);
    }

    [Fact]
    public async Task User_without_rows_may_use_every_branch()
    {
        var (service, _) = Build();
        Assert.Null(await service.AllowedBranchesAsync(default));
        await service.EnsureBranchAsync(99, default);
    }

    [Fact]
    public async Task Limited_user_is_refused_other_branches()
    {
        var (service, _) = Build(2, 3);
        await service.EnsureBranchAsync(2, default);
        await Assert.ThrowsAsync<Application.Common.Exceptions.BadRequestException>(() => service.EnsureBranchAsync(4, default));
    }

    [Fact]
    public async Task Limited_user_sees_only_warehouses_of_their_branches()
    {
        var (service, repo) = Build(2);
        repo.GetWarehouseIdsInBranchesAsync(1, Arg.Any<IReadOnlyCollection<int>>(), Arg.Any<CancellationToken>()).Returns(new List<int> { 10, 11 });
        var allowed = await service.AllowedWarehousesAsync(default);
        Assert.Equal(new[] { 10, 11 }, allowed!.OrderBy(x => x).ToArray());
    }
}
