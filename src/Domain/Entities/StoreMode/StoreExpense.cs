using Domain.Common;

namespace Domain.Entities.StoreMode;

/// <summary>An item of expense (xərc maddəsi): salary, rent, utilities... A company keeps its own list.</summary>
public class StoreExpenseItem : CompanyEntity<int>
{
    public string Name { get; set; } = default!;
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// An expense document (digər xərclər): money paid out of one or more tills for expense items - each line says which till, to whom, for which
/// item and how much. Every line takes its amount out of its till's open shift (a cash movement), so the till's expected cash follows.
/// A document is never deleted, only cancelled.
/// </summary>
public class StoreExpense : CompanyEntity<int>
{
    public string DocumentNo { get; set; } = default!;
    public DateTime DocumentDate { get; set; } = DateTime.UtcNow;
    public string? Note { get; set; }

    public DateTime? CancelledAtUtc { get; set; }
    public int? CancelledByUserId { get; set; }
    public string? CancelReason { get; set; }

    public ICollection<StoreExpenseLine> Lines { get; set; } = new List<StoreExpenseLine>();
}

public class StoreExpenseLine : BaseEntity<int>
{
    public int StoreExpenseId { get; set; }
    public StoreExpense StoreExpense { get; set; } = default!;

    public int StoreCashRegisterId { get; set; }
    public StoreCashRegister StoreCashRegister { get; set; } = default!;

    /// <summary>The shift of that till the money was taken out of.</summary>
    public int StoreCashShiftId { get; set; }

    public int StoreExpenseItemId { get; set; }
    public StoreExpenseItem StoreExpenseItem { get; set; } = default!;

    /// <summary>To whom the money was paid (kimə).</summary>
    public string? Payee { get; set; }

    public decimal Amount { get; set; }
    public string? Note { get; set; }

    /// <summary>The cash movement (out) this line made, and the one that took it back when the document was cancelled.</summary>
    public int? StoreCashMovementId { get; set; }
    public int? ReversalMovementId { get; set; }
}
