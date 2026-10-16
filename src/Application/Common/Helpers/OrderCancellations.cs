using Domain.Entities;

namespace Application.Common.Helpers;

/// <summary>Builds the ləğv jurnalı snapshot for a cancelled order or a removed product.</summary>
public static class OrderCancellations
{
    /// <summary>Recorded when a product is removed before it reached the kitchen — no reason is asked then.</summary>
    public const string RemovedBeforeKitchenReason = "Göndərilmədən silindi";

    public static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    public static OrderCancellation ForOrder(Domain.Entities.Order order, string reason, string? note, int? userId) => new()
    {
        CompanyId = order.CompanyId,
        RestaurantId = order.RestaurantId,
        OrderId = order.Id,
        OrderNumber = order.OrderNumber,
        ReceiptNumber = order.ReceiptNumber,
        TableId = order.TableId,
        TableName = order.Table?.Name ?? $"#{order.TableId}",
        IsWholeOrder = true,
        Quantity = order.Lines.Where(l => l.ParentLineId == null).Sum(l => l.Quantity),
        Amount = order.TotalAmount,
        Reason = reason,
        Note = Clean(note),
        CreatedByUserId = userId,
        CreatedAtUtc = DateTime.UtcNow,
    };

    public static OrderCancellation ForLine(Domain.Entities.Order order, OrderLine line, string reason, string? note, bool beforeKitchen, int? userId) => new()
    {
        CompanyId = order.CompanyId,
        RestaurantId = order.RestaurantId,
        OrderId = order.Id,
        OrderNumber = order.OrderNumber,
        ReceiptNumber = order.ReceiptNumber,
        TableId = order.TableId,
        TableName = order.Table?.Name ?? $"#{order.TableId}",
        IsWholeOrder = false,
        MenuItemId = line.MenuItemId,
        MenuItemName = line.MenuItem?.Name ?? $"#{line.MenuItemId}",
        Quantity = line.Quantity,
        Amount = line.LineTotal,
        Reason = reason,
        Note = Clean(note),
        BeforeKitchen = beforeKitchen,
        CreatedByUserId = userId,
        CreatedAtUtc = DateTime.UtcNow,
    };
}
