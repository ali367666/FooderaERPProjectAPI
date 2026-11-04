using Domain.Common;
using Domain.Enums;

namespace Domain.Entities.StoreMode;

/// <summary>A customer card: found by phone or code at the till; carries the credit limit. Debt and bonus come from their ledgers.</summary>
public class StoreCustomer : CompanyEntity<int>
{
    /// <summary>Short code for the card (printed or typed at the till); unique within the company.</summary>
    public string Code { get; set; } = default!;

    public string Name { get; set; } = default!;
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Note { get; set; }

    /// <summary>The most the customer may owe the store; 0 means no sale on credit.</summary>
    public decimal CreditLimit { get; set; }

    /// <summary>This customer's own bonus percent (e.g. a VIP); null means the company's normal percent applies.</summary>
    public decimal? BonusPercentOverride { get; set; }

    public bool IsActive { get; set; } = true;
}

/// <summary>One movement of a customer's bonus (1 bonus = 1 AZN). The balance is the sum of the entries, never an edited number.</summary>
public class StoreBonusEntry : CompanyEntity<int>
{
    public int StoreCustomerId { get; set; }
    public StoreCustomer StoreCustomer { get; set; } = default!;

    public StoreBonusEntryType Type { get; set; }

    /// <summary>Plus adds bonus, minus takes it away.</summary>
    public decimal Amount { get; set; }

    public string? DocumentType { get; set; }
    public int? DocumentId { get; set; }
    public string? DocumentNo { get; set; }
    public string? Note { get; set; }
}

/// <summary>Money a customer paid towards what they owe, taken at the till (cash lands in the shift's cash).</summary>
public class StoreCustomerPayment : CompanyEntity<int>
{
    public string DocumentNo { get; set; } = default!;

    public int StoreCustomerId { get; set; }
    public StoreCustomer StoreCustomer { get; set; } = default!;

    public decimal Amount { get; set; }
    public StorePaymentMethod Method { get; set; } = StorePaymentMethod.Cash;

    /// <summary>The shift of the till it was taken at.</summary>
    public int StoreCashShiftId { get; set; }
    public StoreCashShift StoreCashShift { get; set; } = default!;

    public string? Reference { get; set; }
    public string? Note { get; set; }
    /// <summary>Set when the payment was cancelled (by a counter ledger entry); the payment itself is never deleted.</summary>
    public DateTime? ReversedAtUtc { get; set; }
    public int? ReversedByUserId { get; set; }
    public string? ReverseReason { get; set; }
}
