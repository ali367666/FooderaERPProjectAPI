namespace Application.BackOffice;

public record CashExpenseCategoryResponse(int Id, string Name);

public class SaveCashExpenseCategoryRequest
{
    public string Name { get; set; } = default!;
}

/// <summary>Today's till of a branch (the company's business day).</summary>
public class CashDeskSummary
{
    public decimal CashSales { get; set; }
    public decimal CardSales { get; set; }
    public decimal CreditSales { get; set; }
    public int ReceiptCount { get; set; }
    public decimal Deposits { get; set; }
    /// <summary>Includes cash handed back for returned goods.</summary>
    public decimal Withdrawals { get; set; }
    /// <summary>Today's cash sales plus deposits less withdrawals.</summary>
    public decimal NetCashBalance { get; set; }
    public bool HasOpenShift { get; set; }
    /// <summary>What should be in the drawer now: the open shift's opening cash and everything since; null without an open shift.</summary>
    public decimal? CurrentDrawerBalance { get; set; }
    public decimal CashPercent { get; set; }
    public decimal CardPercent { get; set; }
}

public interface ICashDeskService
{
    Task<CashDeskSummary> GetSummaryAsync(int companyId, int restaurantId, CancellationToken ct);
    Task<List<CashExpenseCategoryResponse>> GetCategoriesAsync(int companyId, CancellationToken ct);
    Task<CashExpenseCategoryResponse> AddCategoryAsync(int companyId, string name, CancellationToken ct);
    Task DeleteCategoryAsync(int companyId, int id, CancellationToken ct);
}
