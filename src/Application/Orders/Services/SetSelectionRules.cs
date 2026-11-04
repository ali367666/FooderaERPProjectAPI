using Domain.Entities;
using Domain.Enums;

namespace Application.Orders.Services;

/// <summary>
/// A set may cap how many products the waiter picks from one menu category (3 desserts in the set, only 1 may be chosen).
/// The cap is per 1 set, so 2 sets allow twice as many. Components without a cap are not limited by this rule.
/// </summary>
public static class SetSelectionRules
{
    /// <summary>
    /// The first category whose picked quantity exceeds its cap for the set line, or null when everything is within the caps.
    /// <paramref name="overrideLineId"/> / <paramref name="overrideQuantity"/> test a quantity a component line is about to get.
    /// </summary>
    public static string? FindCapViolation(
        OrderLine setLine,
        IEnumerable<OrderLine> orderLines,
        IReadOnlyCollection<MenuItemSetComponent> components,
        int? overrideLineId = null,
        int? overrideQuantity = null)
    {
        var caps = components
            .Where(c => c.CategoryMaxPick is > 0)
            .GroupBy(c => c.ComponentMenuItem.MenuCategoryId)
            .ToDictionary(g => g.Key, g => (Cap: g.Min(c => c.CategoryMaxPick!.Value), Name: g.First().ComponentMenuItem.MenuCategory?.Name));
        if (caps.Count == 0)
            return null;

        var children = orderLines
            .Where(l => l.ParentLineId == setLine.Id && l.Status != OrderLineStatus.Cancelled)
            .DistinctBy(l => l.Id);

        foreach (var group in children.GroupBy(l => l.MenuItem.MenuCategoryId))
        {
            if (!caps.TryGetValue(group.Key, out var rule))
                continue;
            var picked = group.Sum(l => l.Id == overrideLineId && overrideQuantity is { } q ? q : l.Quantity);
            var allowed = rule.Cap * setLine.Quantity;
            if (picked > allowed)
                return $"{setLine.MenuItem.Name}: {(string.IsNullOrWhiteSpace(rule.Name) ? "bu kateqoriyadan" : $"\"{rule.Name}\" kateqoriyasından")} ən çox {allowed} seçim edilə bilər (seçilib: {picked}).";
        }

        return null;
    }
}
