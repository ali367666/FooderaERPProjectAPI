namespace Domain.Enums;

/// <summary>Why a counterparty's debt changed — every change is kept in its debt history.</summary>
public enum CounterpartyDebtEntryType
{
    /// <summary>Debt added by hand ("Borc əlavə et").</summary>
    Added = 1,

    /// <summary>The debt was set to a new figure by hand (a correction, or a payment taken at the cash desk).</summary>
    Adjusted = 2,

    /// <summary>An order was put on the counterparty's account ("Borca yaz").</summary>
    CreditSale = 3
}
