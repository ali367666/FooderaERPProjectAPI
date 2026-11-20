using Application.Common.Exceptions;
using Application.Common.Interfaces;
using Application.StoreMode;
using Application.StoreMode.Cash;
using Domain.Entities;
using Domain.Entities.StoreMode;
using Domain.Entities.WarehouseAndStock;
using Domain.Enums;
using Infrastructure.Persistence.Context;
using Infrastructure.Persistence.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using NSubstitute;
using Persistence.UnitOfWork;
using Xunit;

namespace Tests;

/// <summary>Expense documents (digər xərclər): every line takes money out of its till's open shift; cancelling puts it back.</summary>
public class StoreExpenseTests
{
    private const int Company = 1, Register = 1, Shift = 1;

    private readonly AppDbContext _db;
    private readonly StoreExpensesHandlers _handlers;
    private readonly int _salary;

    public StoreExpenseTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        _db = new InMemoryAppDbContext(options);

        _db.Restaurants.Add(new Restaurant { Id = 1, CompanyId = Company, Name = "Filial" });
        _db.Warehouses.Add(new Warehouse { Id = 1, CompanyId = Company, RestaurantId = 1, Name = "A" });
        _db.StoreCashRegisters.Add(new StoreCashRegister { Id = Register, CompanyId = Company, RestaurantId = 1, WarehouseId = 1, Name = "ESAS KASSA", IsActive = true });
        _db.StoreCashShifts.Add(new StoreCashShift { Id = Shift, CompanyId = Company, StoreCashRegisterId = Register, CashierUserId = 7, Status = StoreShiftStatus.Open });
        var salary = new StoreExpenseItem { CompanyId = Company, Name = "Maaş" };
        _db.StoreExpenseItems.Add(salary);
        _db.SaveChanges();
        _salary = salary.Id;

        var user = Substitute.For<ICurrentUserService>();
        user.UserId.Returns(7);
        user.CompanyId.Returns(Company);

        // The till holds 100 AZN.
        var mediator = Substitute.For<IMediator>();
        mediator.Send(Arg.Any<GetStoreShiftReportQuery>(), Arg.Any<CancellationToken>())
            .Returns(new StoreShiftReportResponse { ExpectedCash = 100m });

        _handlers = new StoreExpensesHandlers(new StoreExpenseRepository(_db), new StoreRepository(_db), user, new UnitOfWork(_db), mediator);
    }

    private static CreateStoreExpenseRequest Doc(params decimal[] amounts) => new()
    {
        Lines = amounts.Select(a => new CreateStoreExpenseLineRequest { RegisterId = Register, ExpenseItemId = 0, Payee = "Muxtarov", Amount = a }).ToList(),
    };

    private CreateStoreExpenseRequest Doc(int itemId, params decimal[] amounts)
    {
        var r = Doc(amounts);
        foreach (var l in r.Lines) l.ExpenseItemId = itemId;
        return r;
    }

    [Fact]
    public async Task A_document_takes_its_lines_out_of_the_open_shift_and_gets_a_number()
    {
        var doc = await _handlers.Handle(new CreateStoreExpenseCommand(Doc(_salary, 30m, 20m)), default);

        Assert.StartsWith("XR-", doc.DocumentNo);
        Assert.Equal(50m, doc.Total);
        var movements = await _db.StoreCashMovements.Where(m => m.StoreCashShiftId == Shift).ToListAsync();
        Assert.Equal(new[] { 20m, 30m }, movements.Select(m => m.Amount).OrderBy(a => a));
        Assert.All(movements, m => Assert.Equal(StoreCashMovementType.Out, m.Type));
        Assert.All(_db.StoreExpenseLines, l => Assert.NotNull(l.StoreCashMovementId));
    }

    [Fact]
    public async Task Lines_of_one_till_cannot_take_more_than_the_till_holds()
    {
        await Assert.ThrowsAsync<BadRequestException>(() => _handlers.Handle(new CreateStoreExpenseCommand(Doc(_salary, 60m, 50m)), default));
        Assert.Empty(_db.StoreExpenses);
        Assert.Empty(_db.StoreCashMovements);
    }

    [Fact]
    public async Task A_till_without_an_open_shift_is_refused()
    {
        var shift = await _db.StoreCashShifts.FirstAsync();
        shift.Status = StoreShiftStatus.Closed;
        await _db.SaveChangesAsync();

        await Assert.ThrowsAsync<BadRequestException>(() => _handlers.Handle(new CreateStoreExpenseCommand(Doc(_salary, 10m)), default));
    }

    [Fact]
    public async Task Cancelling_puts_the_money_back_and_cannot_be_done_twice()
    {
        var doc = await _handlers.Handle(new CreateStoreExpenseCommand(Doc(_salary, 30m)), default);

        var cancelled = await _handlers.Handle(new CancelStoreExpenseCommand(doc.Id, "Səhv yazılıb"), default);

        Assert.True(cancelled.IsCancelled);
        var back = await _db.StoreCashMovements.SingleAsync(m => m.Type == StoreCashMovementType.In);
        Assert.Equal(30m, back.Amount);
        await Assert.ThrowsAsync<BadRequestException>(() => _handlers.Handle(new CancelStoreExpenseCommand(doc.Id, "yenə"), default));

        var list = await _handlers.Handle(new GetStoreExpensesQuery(null, null, null, null, null, null, IncludeCancelled: true), default);
        Assert.Equal(0m, list.Total); // a cancelled document is not counted
    }
}
