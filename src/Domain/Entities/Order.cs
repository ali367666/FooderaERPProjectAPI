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
    public decimal TotalAmount { get; set; }
    public bool IsPaid { get; set; }
    public DateTime? PaidAt { get; set; }
    public PaymentMethod? PaymentMethod { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal ChangeAmount { get; set; }
    public string? ReceiptNumber { get; set; }

    public DateTime OpenedAt { get; set; }
    public DateTime? ClosedAt { get; set; }

    public ICollection<OrderLine> Lines { get; set; } = new List<OrderLine>();
}