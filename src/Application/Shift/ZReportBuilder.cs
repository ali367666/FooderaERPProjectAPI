using Application.Shift.Dtos;

namespace Application.Shift;

internal static class ZReportBuilder
{
    public static ZReportResponse Build(Domain.Entities.Shift shift, List<Domain.Entities.Order> paidOrders)
    {
        return new ZReportResponse
        {
            ShiftId = shift.Id,
            RestaurantId = shift.RestaurantId,
            OpenedAt = shift.OpenedAt,
            ClosedAt = shift.ClosedAt,
            OpeningCashAmount = shift.OpeningCashAmount,
            ClosingCashAmount = shift.ClosingCashAmount,
            OrderCount = paidOrders.Count,
            GrossTotal = paidOrders.Sum(o => o.TotalAmount),
            TotalDiscount = paidOrders.Sum(o => o.DiscountAmount),
            TotalServiceCharge = paidOrders.Sum(o => o.ServiceChargeAmount ?? 0),
            PaymentBreakdown = paidOrders
                .GroupBy(o => o.PaymentMethod)
                .Select(g => new ZReportPaymentBreakdown
                {
                    PaymentMethod = g.Key?.ToString() ?? "Naməlum",
                    OrderCount = g.Count(),
                    TotalAmount = g.Sum(o => o.TotalAmount)
                })
                .ToList()
        };
    }
}
