using Domain.Common;

namespace Domain.Entities;

/// <summary>What money taken out of the restaurant till was spent on.</summary>
public class CashExpenseCategory : CompanyEntity<int>
{
    public string Name { get; set; } = default!;
}
