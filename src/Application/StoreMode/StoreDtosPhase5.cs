using Domain.Enums;

namespace Application.StoreMode;

public class SaveStoreCustomerRequest
{
    public string Name { get; set; } = default!;
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Note { get; set; }

    /// <summary>The most the customer may owe; 0 means no sale on credit.</summary>
    public decimal CreditLimit { get; set; }
    public bool IsActive { get; set; } = true;

    /// <summary>The customer's own bonus percent; empty means the company's normal percent. Only someone who may adjust bonus can change it.</summary>
    public decimal? BonusPercentOverride { get; set; }

    /// <summary>Own code for the card; left out, one is made (C-000123).</summary>
    public string? Code { get; set; }
}

public class StoreCustomerResponse
{
    public int Id { get; set; }
    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Note { get; set; }
    public decimal CreditLimit { get; set; }
    public decimal? BonusPercentOverride { get; set; }

    /// <summary>The bonus percent the screen shows for every customer: the company's normal one.</summary>
    public decimal BonusPercentShown { get; set; }
    public bool IsActive { get; set; }

    /// <summary>What the customer owes the store (from the ledger).</summary>
    public decimal Debt { get; set; }

    /// <summary>Bonus available to spend (from the bonus ledger).</summary>
    public decimal BonusBalance { get; set; }

    /// <summary>Credit still available: limit minus debt (never below zero).</summary>
    public decimal CreditAvailable { get; set; }
}

public class StoreBonusEntryResponse
{
    public int Id { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public StoreBonusEntryType Type { get; set; }
    public string TypeName { get; set; } = default!;
    public decimal Amount { get; set; }
    public decimal RunningBalance { get; set; }
    public string? DocumentNo { get; set; }
    public string? Note { get; set; }
}

public class StoreCustomerPaymentResponse
{
    public int Id { get; set; }
    public string DocumentNo { get; set; } = default!;
    public DateTime CreatedAtUtc { get; set; }
    public decimal Amount { get; set; }
    public StorePaymentMethod Method { get; set; }
    public string? Reference { get; set; }
    public string? Note { get; set; }
    public bool IsReversed { get; set; }
    public string? ReverseReason { get; set; }
}

/// <summary>A customer's account: debt movements, bonus movements and the payments taken.</summary>
public class StoreCustomerStatementResponse
{
    public StoreCustomerResponse Customer { get; set; } = default!;
    public List<StoreLedgerEntryResponse> DebtEntries { get; set; } = new();
    public List<StoreBonusEntryResponse> BonusEntries { get; set; } = new();
    public List<StoreCustomerPaymentResponse> Payments { get; set; } = new();
}

public class CreateStoreCustomerPaymentRequest
{
    public int CustomerId { get; set; }
    public decimal Amount { get; set; }

    /// <summary>Cash or Card.</summary>
    public StorePaymentMethod Method { get; set; } = StorePaymentMethod.Cash;
    public string? Reference { get; set; }
    public string? Note { get; set; }
}

public class AdjustStoreBonusRequest
{
    public int CustomerId { get; set; }

    /// <summary>Plus adds bonus, minus takes it away.</summary>
    public decimal Amount { get; set; }
    public string Reason { get; set; } = default!;
}
