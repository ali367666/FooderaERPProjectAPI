using Application.Shift.Dtos;
using Domain.Enums;
using Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

/// <summary>
/// Card income of paid orders grouped by payment bank: the card parts of orders paid in parts plus
/// orders paid by card in one go. Card payments with no bank (made before banks existed) are grouped
/// as "Bank göstərilməyib". Shared by the shift Z report and the sales reports.
/// </summary>
internal static class CardBankTotals
{
    public const string NoBank = "Bank göstərilməyib";

    public static async Task<List<ZReportBankBreakdown>> ComputeAsync(
        AppDbContext context,
        IReadOnlyCollection<int> orderIds,
        CancellationToken cancellationToken)
    {
        if (orderIds.Count == 0)
            return new();

        var ids = orderIds.ToList();

        var parts = await context.OrderPayments.AsNoTracking()
            .Where(p => ids.Contains(p.OrderId) && p.Method == PaymentMethod.Card)
            .Select(p => new { p.BankCounterpartyId, Amount = p.Amount + (p.ServiceChargeAmount ?? 0) })
            .ToListAsync(cancellationToken);

        var withParts = context.OrderPayments.Where(p => ids.Contains(p.OrderId)).Select(p => p.OrderId);
        var singles = await context.Orders.AsNoTracking()
            .Where(o => ids.Contains(o.Id)
                && o.PaymentMethod == PaymentMethod.Card
                && !withParts.Contains(o.Id))
            .Select(o => new { o.BankCounterpartyId, Amount = o.TotalAmount })
            .ToListAsync(cancellationToken);

        var rows = parts.Concat(singles).ToList();
        var bankIds = rows.Where(r => r.BankCounterpartyId != null).Select(r => r.BankCounterpartyId!.Value).Distinct().ToList();
        var names = await context.Counterparties.AsNoTracking()
            .Where(c => bankIds.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, c => c.Name, cancellationToken);

        return rows
            .GroupBy(r => r.BankCounterpartyId is { } id && names.TryGetValue(id, out var name) ? name : NoBank)
            .Select(g => new ZReportBankBreakdown
            {
                BankName = g.Key,
                PaymentCount = g.Count(),
                TotalAmount = g.Sum(r => r.Amount)
            })
            .OrderByDescending(x => x.TotalAmount)
            .ToList();
    }
}
