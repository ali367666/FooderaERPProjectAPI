using Domain.Common;
using Domain.Enums;

namespace Domain.Entities.StoreMode;

/// <summary>
/// An operation a till device made offline and sent for syncing. The <see cref="OperationId"/> is unique per company, so a
/// resend never repeats the sale; the entry keeps the outcome and, when refused, the reason (a conflict is never lost silently).
/// </summary>
public class StoreSyncOperation : CompanyEntity<int>
{
    public Guid OperationId { get; set; }

    /// <summary>Which device (browser/till) sent it.</summary>
    public string DeviceId { get; set; } = default!;

    /// <summary>What it is, e.g. "Sale".</summary>
    public string Type { get; set; } = default!;

    public StoreSyncStatus Status { get; set; } = StoreSyncStatus.Pending;
    public int AttemptCount { get; set; }

    /// <summary>When the device made it (its clock).</summary>
    public DateTime? ClientCreatedAtUtc { get; set; }
    public DateTime? ProcessedAtUtc { get; set; }

    /// <summary>The document it produced (sale number) once synced.</summary>
    public string? ResultDocumentNo { get; set; }
    public int? ResultEntityId { get; set; }

    /// <summary>Why it was refused, and what to do about it.</summary>
    public string? Error { get; set; }
}

/// <summary>A branch a user is limited to. A user with no rows may work in every branch.</summary>
public class StoreUserBranch : CompanyEntity<int>
{
    public int UserId { get; set; }
    public int RestaurantId { get; set; }
}

/// <summary>A branch's own sale price for a product (the branch price list); without a row the product's normal sale price applies.</summary>
public class StoreBranchPrice : CompanyEntity<int>
{
    public int RestaurantId { get; set; }

    public int StoreProductId { get; set; }
    public StoreProduct StoreProduct { get; set; } = default!;

    public decimal SalePrice { get; set; }
}
