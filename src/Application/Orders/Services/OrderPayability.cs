using Domain.Enums;

namespace Application.Orders.Services;

/// <summary>Whether an order in a status may be paid, given the company's kitchen-approval setting.</summary>
public static class OrderPayability
{
    public const string NotPayableMessage = "Only ready or served orders can be paid.";
    public const string NotSentMessage = "Sifariş hələ mətbəxə göndərilməyib (OK basılmayıb).";

    /// <summary>
    /// With kitchen approval the kitchen must have marked the order ready (or it is served). Without it any order already sent
    /// to the kitchen (open, in preparation, ready or served) can be paid; a draft has not been sent yet.
    /// </summary>
    public static bool CanPay(OrderStatus status, bool kitchenApprovalRequired)
        => status is OrderStatus.Ready or OrderStatus.Served
           || (!kitchenApprovalRequired && status is OrderStatus.Open or OrderStatus.InPreparation);

    public static string RefusalMessage(OrderStatus status, bool kitchenApprovalRequired)
        => !kitchenApprovalRequired && status == OrderStatus.Draft ? NotSentMessage : NotPayableMessage;
}
