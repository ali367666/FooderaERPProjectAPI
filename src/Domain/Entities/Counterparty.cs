using Domain.Common;

namespace Domain.Entities;

public class Counterparty : CompanyEntity<int>
{
    public string Name { get; set; } = default!;
    public string? PhoneNumber { get; set; }
    public int CategoryId { get; set; }
    public CounterpartyCategory Category { get; set; } = default!;
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// One of the fixed payment banks (Kapital, Paşa, ABB, QR kod). The POS asks which of them a card
    /// payment went through so reports can split card income by bank; they can't be deleted.
    /// </summary>
    public bool IsBank { get; set; }
    public decimal CurrentDebtAmount { get; set; }
}
