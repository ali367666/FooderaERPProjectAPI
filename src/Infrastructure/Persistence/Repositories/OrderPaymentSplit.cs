using Domain.Enums;
using Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

/// <summary>How much of a paid bill went in cash, by card and on credit.</summary>
public record MoneySplit(decimal Cash, decimal Card, decimal Credit)
{
    public static readonly MoneySplit None = new(0, 0, 0);
}

internal static class OrderPaymentSplit
{
    /// <summary>
    /// A bill paid in parts counts each part (with its service charge) under its own method; a bill paid in a single go is all one
    /// method. Orders without an entry here were not paid.
    /// </summary>
    public static async Task<Dictionary<int, MoneySplit>> ComputeAsync(
        AppDbContext context,
        IReadOnlyCollection<(int OrderId, PaymentMethod? Method, decimal Total)> paidOrders,
        CancellationToken cancellationToken)
    {
        if (paidOrders.Count == 0)
            return new();

        var ids = paidOrders.Select(o => o.OrderId).ToList();
        var parts = (await context.OrderPayments.AsNoTracking()
                .Where(p => ids.Contains(p.OrderId))
                .Select(p => new { p.OrderId, p.Method, Amount = p.Amount + (p.ServiceChargeAmount ?? 0) })
                .ToListAsync(cancellationToken))
            .ToLookup(p => p.OrderId);

        return paidOrders.ToDictionary(o => o.OrderId, o =>
        {
            var own = parts[o.OrderId].Select(p => (p.Method, p.Amount)).ToList();
            if (own.Count == 0 && o.Method is { } method)
                own.Add((method, o.Total));

            decimal Sum(PaymentMethod m) => own.Where(p => p.Method == m).Sum(p => p.Amount);
            return new MoneySplit(Sum(PaymentMethod.Cash), Sum(PaymentMethod.Card), Sum(PaymentMethod.Credit));
        });
    }
}
