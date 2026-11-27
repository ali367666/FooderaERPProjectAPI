using Domain.Enums;

namespace Application.Finance.Dtos;

public class SaveFinanceEntryRequest
{
    /// <summary>Empty = company-wide.</summary>
    public int? RestaurantId { get; set; }
    public FinanceEntryType Type { get; set; }
    public string Name { get; set; } = default!;
    public decimal Amount { get; set; }
    public DateTime EntryDate { get; set; }
    public string? Note { get; set; }
}

public class FinanceEntryResponse
{
    public int Id { get; set; }
    public int? RestaurantId { get; set; }
    public FinanceEntryType Type { get; set; }
    public string Name { get; set; } = default!;
    public decimal Amount { get; set; }
    public DateTime EntryDate { get; set; }
    public string? Note { get; set; }
    public string? CreatedByUserName { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
