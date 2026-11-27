using Domain.Enums;

namespace Application.Finance.Dtos;

public enum FinanceGranularity
{
    Week = 1,
    Month = 2,
    Year = 3,
}

public class FinanceReportRequest
{
    public FinanceGranularity Granularity { get; set; } = FinanceGranularity.Month;

    /// <summary>Any day inside the wanted week / month / year; defaults to the current business day.</summary>
    public DateTime? Date { get; set; }

    /// <summary>Limit to one branch; the whole company when empty.</summary>
    public int? RestaurantId { get; set; }
}

/// <summary>One income or expense line of a day. Manual entries carry their <see cref="EntryId"/>; lines read from sales, purchases and the till are totalled per name.</summary>
public class FinanceLineDto
{
    public int? EntryId { get; set; }
    public string Name { get; set; } = default!;
    public decimal Amount { get; set; }
    public string Source { get; set; } = default!;
    public string? Note { get; set; }
}

public class FinanceDayDto
{
    public DateTime Date { get; set; }
    public decimal Income { get; set; }
    public decimal Expense { get; set; }
    public decimal Profit { get; set; }
    public List<FinanceLineDto> IncomeLines { get; set; } = new();
    public List<FinanceLineDto> ExpenseLines { get; set; } = new();
}

/// <summary>A column of the summary chart: a day for week/month reports, a month for the year report.</summary>
public class FinanceBucketDto
{
    public string Label { get; set; } = default!;
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public decimal Income { get; set; }
    public decimal Expense { get; set; }
    public decimal Profit { get; set; }
}

public class FinanceNameTotalDto
{
    public string Name { get; set; } = default!;
    public int Count { get; set; }
    public decimal Total { get; set; }
}

public class FinanceReportResponse
{
    public FinanceGranularity Granularity { get; set; }
    public DateTime FromDate { get; set; }
    /// <summary>Last day of the period (inclusive).</summary>
    public DateTime ToDate { get; set; }
    public int? RestaurantId { get; set; }

    public decimal TotalIncome { get; set; }
    public decimal TotalExpense { get; set; }
    /// <summary>Income minus expense: what is left at the end of the period.</summary>
    public decimal Profit { get; set; }

    public List<FinanceBucketDto> Buckets { get; set; } = new();
    /// <summary>Only the days that had at least one income or expense.</summary>
    public List<FinanceDayDto> Days { get; set; } = new();
    public List<FinanceNameTotalDto> IncomeByName { get; set; } = new();
    public List<FinanceNameTotalDto> ExpenseByName { get; set; } = new();
}

/// <summary>A money line read from sales / purchases / the till, before it is placed on a business day.</summary>
public record FinanceSourceLine(DateTime AtUtc, FinanceEntryType Type, string Name, decimal Amount, string Source);
