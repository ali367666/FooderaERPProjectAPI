using System.Text.Json;
using Application.Common.Exceptions;
using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Common.Interfaces.Abstracts.Services;
using Application.StoreMode;
using Application.StoreMode.Notifications;
using Application.StoreMode.Pricing;
using Application.StoreMode.Sales;
using Application.StoreMode.Sync;
using Domain.Entities.StoreMode;
using Domain.Enums;
using Infrastructure.Persistence.Context;
using Infrastructure.Persistence.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace Tests;

public class StoreSyncTests
{
    private readonly AppDbContext _db;
    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly StoreSyncHandlers _handlers;

    public StoreSyncTests()
    {
        _db = new InMemoryAppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

        var user = Substitute.For<ICurrentUserService>();
        user.UserId.Returns(7);
        user.CompanyId.Returns(1);

        var provider = Substitute.For<IServiceProvider>();
        provider.GetService(typeof(IMediator)).Returns(_mediator);
        var scope = Substitute.For<IServiceScope>();
        scope.ServiceProvider.Returns(provider);
        var factory = Substitute.For<IServiceScopeFactory>();
        factory.CreateScope().Returns(scope);

        var accessRepo = new StoreAccessRepository(_db);
        var notifier = new StoreNotifier(Substitute.For<IIdentityAdminService>(), Substitute.For<INotificationService>(), user, NullLogger<StoreNotifier>.Instance);
        _handlers = new StoreSyncHandlers(
            new StoreSyncRepository(_db), new StoreRepository(_db), user, factory, notifier,
            NullLogger<StoreSyncHandlers>.Instance, new StorePricingService(new StorePricingRepository(_db), accessRepo, user));
    }

    private static PushStoreSyncCommand Push(Guid id) => new(new StoreSyncPushRequest
    {
        DeviceId = "till-1",
        Operations =
        {
            new StoreSyncOperationRequest
            {
                OperationId = id,
                Type = "Sale",
                Payload = JsonSerializer.SerializeToElement(new { storeCashRegisterId = 1, lines = Array.Empty<object>(), payments = Array.Empty<object>() }),
            },
        },
    });

    [Fact]
    public async Task Accepted_operation_is_synced_once_and_a_resend_does_not_reach_the_sale_handler_again()
    {
        var id = Guid.NewGuid();
        _mediator.Send(Arg.Any<CreateStoreSaleCommand>(), Arg.Any<CancellationToken>()).Returns(new StoreSaleResponse { Id = 5, DocumentNo = "S-0000005" });

        var first = await _handlers.Handle(Push(id), default);
        var again = await _handlers.Handle(Push(id), default);

        Assert.Equal(StoreSyncStatus.Synced, first[0].Status);
        Assert.Equal("S-0000005", again[0].DocumentNo);
        await _mediator.Received(1).Send(Arg.Any<CreateStoreSaleCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Stock_shortage_is_kept_as_a_conflict_with_its_reason()
    {
        var id = Guid.NewGuid();
        _mediator.Send(Arg.Any<CreateStoreSaleCommand>(), Arg.Any<CancellationToken>())
            .Returns<StoreSaleResponse>(_ => throw new BadRequestException("\"Süd\" anbarda kifayət qədər yoxdur."));

        var result = await _handlers.Handle(Push(id), default);

        Assert.Equal(StoreSyncStatus.Conflict, result[0].Status);
        Assert.Contains("anbarda kifayət qədər yoxdur", result[0].Error);
        var saved = _db.StoreSyncOperations.AsNoTracking().Single();
        Assert.Equal(StoreSyncStatus.Conflict, saved.Status);
        Assert.Equal("till-1", saved.DeviceId);
    }

    [Fact]
    public async Task A_refused_operation_can_be_sent_again_and_then_syncs()
    {
        var id = Guid.NewGuid();
        _mediator.Send(Arg.Any<CreateStoreSaleCommand>(), Arg.Any<CancellationToken>())
            .Returns<StoreSaleResponse>(_ => throw new BadRequestException("Ödəniş kifayət etmir."));
        Assert.Equal(StoreSyncStatus.Failed, (await _handlers.Handle(Push(id), default))[0].Status);

        _mediator.Send(Arg.Any<CreateStoreSaleCommand>(), Arg.Any<CancellationToken>()).Returns(new StoreSaleResponse { Id = 9, DocumentNo = "S-9" });
        var retried = await _handlers.Handle(Push(id), default);

        Assert.Equal(StoreSyncStatus.Synced, retried[0].Status);
        Assert.Equal(2, retried[0].AttemptCount);
    }
}
