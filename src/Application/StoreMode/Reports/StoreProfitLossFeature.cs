using Application.Common.Helpers;
using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Reports;
using MediatR;

namespace Application.StoreMode.Reports;

/// <summary>
/// The result of a period (dövrün nəticəsi): what was sold, what the goods cost, the gross profit, the other expenses paid out of the tills and the
/// net income. The cost of goods follows the company's costing method (average, FIFO or LIFO) because it is the cost stored on each sale.
/// </summary>
public record GetStoreProfitLossQuery(DateTime From, DateTime To) : IRequest<StoreProfitLossResponse>;

public class StoreProfitLossDayResponse
{
    /// <summary>yyyy-MM-dd (business day).</summary>
    public string Day { get; set; } = default!;
    public decimal Sales { get; set; }
    public decimal GrossProfit { get; set; }
    public decimal Expenses { get; set; }
    public decimal NetIncome { get; set; }
}

public class StoreProfitLossExpenseResponse
{
    public string Name { get; set; } = default!;
    public decimal Amount { get; set; }
}

public class StoreProfitLossResponse
{
    public DateTime From { get; set; }
    public DateTime To { get; set; }

    public int SalesCount { get; set; }

    /// <summary>Net sales less returns: the turnover (satış dövriyyəsi).</summary>
    public decimal Sales { get; set; }

    /// <summary>What the goods sold cost, returned goods given back (satılan malların maya dəyəri).</summary>
    public decimal CostOfGoods { get; set; }

    /// <summary>Sales less the cost of goods (ümumi mənfəət).</summary>
    public decimal GrossProfit { get; set; }

    /// <summary>Other expenses paid out of the tills in the period (digər xərclər), cancelled documents left out.</summary>
    public decimal Expenses { get; set; }

    /// <summary>Gross profit less expenses (təmiz gəlir); negative is a loss.</summary>
    public decimal NetIncome { get; set; }

    public List<StoreProfitLossExpenseResponse> ExpensesByItem { get; set; } = new();
    public List<StoreProfitLossDayResponse> Days { get; set; } = new();
}

public class StoreProfitLossHandler : IRequestHandler<GetStoreProfitLossQuery, StoreProfitLossResponse>
{
    private readonly IMediator _mediator;
    private readonly IStoreExpenseRepository _expenses;
    private readonly ICurrentUserService _currentUser;

    public StoreProfitLossHandler(IMediator mediator, IStoreExpenseRepository expenses, ICurrentUserService currentUser)
    {
        _mediator = mediator;
        _expenses = expenses;
        _currentUser = currentUser;
    }

    private static decimal R2(decimal v) => Math.Round(v, 2, MidpointRounding.AwayFromZero);

    public async Task<StoreProfitLossResponse> Handle(GetStoreProfitLossQuery q, CancellationToken ct)
    {
        var sales = await _mediator.Send(new GetStoreSalesReportQuery(q.From, q.To, null, null, null, null, null, false), ct);

        var fromUtc = ReportPeriodResolver.StoreFromUtc(q.From);
        var toUtc = ReportPeriodResolver.StoreToUtc(q.To);
        var docs = (await _expenses.SearchAsync(_currentUser.CompanyId, fromUtc, toUtc, ct)).Where(d => d.CancelledAtUtc == null).ToList();
        var lines = docs.SelectMany(d => d.Lines.Select(l => (Day: DayOf(d.DocumentDate), Line: l))).ToList();

        var expenses = R2(lines.Sum(x => x.Line.Amount));
        var costOfGoods = R2(sales.CostOfGoods - sales.ReturnedCost);
        var gross = R2(sales.Profit);

        // One row per business day that had sales or expenses.
        var expenseByDay = lines.GroupBy(x => x.Day).ToDictionary(g => g.Key, g => R2(g.Sum(x => x.Line.Amount)));
        var salesByDay = sales.ByDay.ToDictionary(r => r.Key, r => r);
        var days = salesByDay.Keys.Union(expenseByDay.Keys).OrderBy(k => k, StringComparer.Ordinal).Select(day =>
        {
            salesByDay.TryGetValue(day, out var s);
            var exp = expenseByDay.GetValueOrDefault(day);
            var profit = R2(s?.Profit ?? 0m);
            return new StoreProfitLossDayResponse { Day = day, Sales = R2(s?.Amount ?? 0m), GrossProfit = profit, Expenses = exp, NetIncome = R2(profit - exp) };
        }).ToList();

        return new StoreProfitLossResponse
        {
            From = q.From.Date,
            To = q.To.Date,
            SalesCount = sales.SalesCount,
            Sales = R2(sales.NetAfterReturns),
            CostOfGoods = costOfGoods,
            GrossProfit = gross,
            Expenses = expenses,
            NetIncome = R2(gross - expenses),
            ExpensesByItem = lines.GroupBy(x => x.Line.StoreExpenseItem?.Name ?? string.Empty)
                .Select(g => new StoreProfitLossExpenseResponse { Name = g.Key, Amount = R2(g.Sum(x => x.Line.Amount)) })
                .OrderByDescending(x => x.Amount).ToList(),
            Days = days,
        };
    }

    private static string DayOf(DateTime utc) => (BusinessTime.FromUtc(utc) - ReportPeriodResolver.DayStart).Date.ToString("yyyy-MM-dd");
}
