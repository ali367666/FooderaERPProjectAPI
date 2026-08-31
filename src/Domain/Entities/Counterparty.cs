using Domain.Common;

namespace Domain.Entities;

public class Counterparty : CompanyEntity<int>
{
    public string Name { get; set; } = default!;
    public string? PhoneNumber { get; set; }
    public int CategoryId { get; set; }
    public CounterpartyCategory Category { get; set; } = default!;
    public bool IsActive { get; set; } = true;
    public decimal CurrentDebtAmount { get; set; }
}
