using Domain.Common;
using Domain.Enums;

namespace Domain.Entities;

/// <summary>
/// One payment against an order. A table can pay in parts ("Hesab" — every guest settles their own
/// items); the order closes when the payments together cover every line. Orders paid in a single
/// go (the usual way) have no rows here.
/// </summary>
public class OrderPayment : CompanyEntity<int>
{
    public int OrderId { get; set; }
    public Order Order { get; set; } = default!;

    public PaymentMethod Method { get; set; }

    /// <summary>The share of the bill this payment settles — lines after discount, without service charge.</summary>
    public decimal Amount { get; set; }
    public decimal? ServiceChargeAmount { get; set; }

    /// <summary>Money handed over (0 for a credit/"borca yaz" payment).</summary>
    public decimal ReceivedAmount { get; set; }
    public decimal ChangeAmount { get; set; }

    /// <summary>This payment went through the fiscal (tax) register.</summary>
    public bool IsFiscal { get; set; }

    public DateTime PaidAtUtc { get; set; } = DateTime.UtcNow;
    public int? PaidByUserId { get; set; }

    public ICollection<OrderPaymentLine> Lines { get; set; } = new List<OrderPaymentLine>();
}
