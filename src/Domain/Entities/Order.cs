using Domain.Common;
using Domain.Enums;

namespace Domain.Entities;
public class Order : CompanyEntity<int>
{
    public string OrderNumber { get; set; } = default!;

    public int RestaurantId { get; set; }   // ✅ əlavə et
    public Restaurant Restaurant { get; set; } = default!;

    public int TableId { get; set; }
    public RestaurantTable Table { get; set; } = default!;

    public int WaiterId { get; set; }
    public Employee Waiter { get; set; } = default!;
    public int? ProcessedByUserId { get; set; }
    public User? ProcessedByUser { get; set; }
    public DateTime? ProcessedAt { get; set; }

    public OrderStatus Status { get; set; } = OrderStatus.Open;

    public string? Note { get; set; }
    public int? GuestCount { get; set; }
    public int? CounterpartyId { get; set; }
    public Counterparty? Counterparty { get; set; }
    public decimal TotalAmount { get; set; }
    public bool IsPaid { get; set; }
    public DateTime? PaidAt { get; set; }
    public PaymentMethod? PaymentMethod { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal ChangeAmount { get; set; }
    public string? ReceiptNumber { get; set; }

    public int? DiscountId { get; set; }
    public Discount? Discount { get; set; }
    public string? DiscountCode { get; set; }
    public decimal DiscountAmount { get; set; } = 0;
    public decimal? ServiceChargeAmount { get; set; }

    public DateTime OpenedAt { get; set; }
    public DateTime? ClosedAt { get; set; }

    /// <summary>
    /// When set, the whole order is "parked" — its lines are hidden from the kitchen queue and
    /// excluded from kitchen ticket printing until released, regardless of any per-line hold.
    /// </summary>
    public DateTime? HoldUntilUtc { get; set; }

    /// <summary>
    /// The restaurant's own courier delivery (as opposed to a third-party integration like
    /// Wolt/Bolt/189) — priced from each item's Package price instead of the usual station price.
    /// </summary>
    public bool IsDelivery { get; set; }
    public string? DeliveryAddress { get; set; }
    public string? DeliveryPhone { get; set; }
    public int? DeliveryDriverEmployeeId { get; set; }
    public Employee? DeliveryDriverEmployee { get; set; }

    public DateTime? TableRentalStartedAt { get; set; }
    public DateTime? TableRentalStoppedAt { get; set; }
    public decimal? TableRentalAmount { get; set; }

    public ICollection<OrderLine> Lines { get; set; } = new List<OrderLine>();
}