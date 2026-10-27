using System.Globalization;
using Application.Common.Helpers;
using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Finance.Dtos;
using Application.Reports;
using Domain.Enums;
using MediatR;

namespace Application.Finance.Queries;

public record GetFinanceReportQuery(FinanceReportRequest Request) : IRequest<FinanceReportResponse>;

/// <summary>
/// Income / expense / profit for a week, month or year. Income and expenses are the manual
/// entries plus what the system already knows (sales, completed stock purchases, till
/// withdrawals, sale returns). Days follow the same 06:00 business-day cut-off as the other reports.
/// </summary>
public class GetFinanceReportQueryHandler : IRequestHandler<GetFinanceReportQuery, FinanceReportResponse>
{
    private const string ManualSource = "Manual";

    private readonly IFinanceRepository _repository;
    private readonly ICurrentUserService _currentUserService;

    public GetFinanceReportQueryHandler(IFinanceRepository repository, ICurrentUserService currentUserService)
    {
        _repository = repository;
        _currentUserService = currentUserService;
    }

    public async Task<FinanceReportResponse> Handle(GetFinanceReportQuery query, CancellationToken cancellationToken)
    {
        var request = query.Request;
        var companyId = _currentUserService.CompanyId;
        int? restaurantId = request.RestaurantId is > 0 ? request.RestaurantId : null;

        var anchor = (request.Date?.Date) ?? (BusinessTime.Now - ReportPeriodResolver.DayStart).Date;
        var (fromDate, toDateExclusive) = Bounds(request.Granularity, anchor);

        var fromUtc = BusinessTime.ToUtc(fromDate + ReportPeriodResolver.DayStart);
        var toUtc = BusinessTime.ToUtc(toDateExclusive + ReportPeriodResolver.DayStart);

        var manual = await _repository.GetEntriesAsync(companyId, restaurantId, fromDate, toDateExclusive, null, cancellationToken);
        var automatic = await _repository.GetAutomaticLinesAsync(companyId, restaurantId, fromUtc, toUtc, cancellationToken);

        // Everything as (business day, type, name, amount, source, entry id, note).
        var rows = manual
            .Select(e => new Row(e.EntryDate.Date, e.Type, e.Name, e.Amount, ManualSource, e.Id, e.Note))
            .Concat(automatic.Select(a => new Row(
                (BusinessTime.FromUtc(a.AtUtc) - ReportPeriodResolver.DayStart).Date,
                a.Type, a.Name, a.Amount, a.Source, null, null)))
            .Where(r => r.Date >= fromDate && r.Date < toDateExclusive)
            .ToList();

        var days = rows
            .GroupBy(r => r.Date)
            .OrderBy(g => g.Key)
            .Select(g =>
            {
                var day = new FinanceDayDto
                {
                    Date = g.Key,
                    IncomeLines = Lines(g, FinanceEntryType.Income),
                    ExpenseLines = Lines(g, FinanceEntryType.Expense),
                };
                day.Income = day.IncomeLines.Sum(l => l.Amount);
                day.Expense = day.ExpenseLines.Sum(l => l.Amount);
                day.Profit = day.Income - day.Expense;
                return day;
            })
            .ToList();

        var totalIncome = days.Sum(d => d.Income);
        var totalExpense = days.Sum(d => d.Expense);

        return new FinanceReportResponse
        {
            Granularity = request.Granularity,
            FromDate = fromDate,
            ToDate = toDateExclusive.AddDays(-1),
            RestaurantId = restaurantId,
            TotalIncome = totalIncome,
            TotalExpense = totalExpense,
            Profit = totalIncome - totalExpense,
            Buckets = Buckets(request.Granularity, fromDate, toDateExclusive, days),
            Days = days,
            IncomeByName = ByName(rows, FinanceEntryType.Income),
            ExpenseByName = ByName(rows, FinanceEntryType.Expense),
        };
    }

    private sealed record Row(
        DateTime Date, FinanceEntryType Type, string Name, decimal Amount, string Source, int? EntryId, string? Note);

    /// <summary>Manual entries stay separate (so they can be edited); automatic ones are totalled per name.</summary>
    private static List<FinanceLineDto> Lines(IEnumerable<Row> dayRows, FinanceEntryType type)
    {
        var typed = dayRows.Where(r => r.Type == type).ToList();

        var manual = typed
            .Where(r => r.EntryId != null)
            .Select(r => new FinanceLineDto { EntryId = r.EntryId, Name = r.Name, Amount = r.Amount, Source = r.Source, Note = r.Note });

        var automatic = typed
            .Where(r => r.EntryId == null)
            .GroupBy(r => (r.Name, r.Source))
            .Select(g => new FinanceLineDto { Name = g.Key.Name, Amount = g.Sum(r => r.Amount), Source = g.Key.Source });

        return automatic.Concat(manual).OrderByDescending(l => l.Amount).ToList();
    }

    private static List<FinanceNameTotalDto> ByName(IEnumerable<Row> rows, FinanceEntryType type) =>
        rows.Where(r => r.Type == type)
            .GroupBy(r => r.Name, StringComparer.CurrentCultureIgnoreCase)
            .Select(g => new FinanceNameTotalDto { Name = g.Key, Count = g.Count(), Total = g.Sum(r => r.Amount) })
            .OrderByDescending(t => t.Total)
            .ToList();

    private static List<FinanceBucketDto> Buckets(
        FinanceGranularity granularity, DateTime fromDate, DateTime toDateExclusive, List<FinanceDayDto> days)
    {
        var buckets = new List<FinanceBucketDto>();

        if (granularity == FinanceGranularity.Year)
        {
            for (var month = fromDate; month < toDateExclusive; month = month.AddMonths(1))
            {
                var next = month.AddMonths(1);
                buckets.Add(Fill(
                    month.ToString("MMMM", CultureInfo.GetCultureInfo("az-Latn-AZ")),
                    month, next.AddDays(-1), days));
            }
        }
        else
        {
            for (var day = fromDate; day < toDateExclusive; day = day.AddDays(1))
                buckets.Add(Fill(day.ToString("dd.MM", CultureInfo.InvariantCulture), day, day, days));
        }

        return buckets;
    }

    private static FinanceBucketDto Fill(string label, DateTime from, DateTime to, List<FinanceDayDto> days)
    {
        var inside = days.Where(d => d.Date >= from && d.Date <= to).ToList();
        var income = inside.Sum(d => d.Income);
        var expense = inside.Sum(d => d.Expense);
        return new FinanceBucketDto
        {
            Label = label,
            FromDate = from,
            ToDate = to,
            Income = income,
            Expense = expense,
            Profit = income - expense,
        };
    }

    private static (DateTime From, DateTime ToExclusive) Bounds(FinanceGranularity granularity, DateTime anchor)
    {
        switch (granularity)
        {
            case FinanceGranularity.Week:
                var monday = anchor.AddDays(-(((int)anchor.DayOfWeek + 6) % 7));
                return (monday, monday.AddDays(7));
            case FinanceGranularity.Year:
                var jan1 = new DateTime(anchor.Year, 1, 1);
                return (jan1, jan1.AddYears(1));
            default:
                var first = new DateTime(anchor.Year, anchor.Month, 1);
                return (first, first.AddMonths(1));
        }
    }
}
