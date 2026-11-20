using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.StoreMode.Reports;
using Domain.Entities.StoreMode;
using MediatR;
using NSubstitute;
using Xunit;

namespace Tests;

public class StoreProfitLossTests
{
    [Fact]
    public async Task Net_income_is_the_gross_profit_less_the_other_expenses_and_cancelled_documents_do_not_count()
    {
        var mediator = Substitute.For<IMediator>();
        mediator.Send(Arg.Any<GetStoreSalesReportQuery>(), Arg.Any<CancellationToken>()).Returns(new StoreSalesReportResponse
        {
            SalesCount = 3, NetAfterReturns = 420m, CostOfGoods = 150m, ReturnedCost = 30m, Profit = 300m,
        });

        var salary = new StoreExpenseItem { Id = 1, Name = "Maaş" };
        var rent = new StoreExpenseItem { Id = 2, Name = "Kirayə" };
        var when = new DateTime(2026, 11, 12, 10, 0, 0, DateTimeKind.Utc);
        var expenses = Substitute.For<IStoreExpenseRepository>();
        expenses.SearchAsync(1, Arg.Any<DateTime?>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>()).Returns(new List<StoreExpense>
        {
            new() { DocumentDate = when, Lines = { new StoreExpenseLine { Amount = 100m, StoreExpenseItem = salary }, new StoreExpenseLine { Amount = 50m, StoreExpenseItem = rent } } },
            new() { DocumentDate = when, Lines = { new StoreExpenseLine { Amount = 200m, StoreExpenseItem = salary } } },
            new() { DocumentDate = when, CancelledAtUtc = when, Lines = { new StoreExpenseLine { Amount = 999m, StoreExpenseItem = salary } } },
        });
        var user = Substitute.For<ICurrentUserService>();
        user.CompanyId.Returns(1);

        var result = await new StoreProfitLossHandler(mediator, expenses, user)
            .Handle(new GetStoreProfitLossQuery(new DateTime(2026, 11, 1), new DateTime(2026, 11, 30)), default);

        Assert.Equal(420m, result.Sales);
        Assert.Equal(120m, result.CostOfGoods); // 150 - 30 given back
        Assert.Equal(300m, result.GrossProfit);
        Assert.Equal(350m, result.Expenses);
        Assert.Equal(-50m, result.NetIncome);
        Assert.Equal(("Maaş", 300m), (result.ExpensesByItem[0].Name, result.ExpensesByItem[0].Amount));
    }
}
