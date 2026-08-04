using Application.Analytics.Dtos;
using Application.Common.Interfaces.Abstracts.Repositories;
using Domain.Enums;
using Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace Persistence.Repositories;

public class AnalyticsRepository : IAnalyticsRepository
{
    private readonly AppDbContext _context;

    public AnalyticsRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<DashboardAnalyticsResponse> GetDashboardAsync(
        int companyId,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var todayStart = now.Date;
        var weekStart = todayStart.AddDays(-6);
        var monthStart = new DateTime(now.Year, now.Month, 1);
        var yearStart = new DateTime(now.Year, 1, 1);

        // All paid orders with minimum needed fields
        var paidOrders = await _context.Orders
            .Where(o => o.CompanyId == companyId && o.IsPaid && o.PaidAt != null)
            .Select(o => new
            {
                o.Id,
                o.TotalAmount,
                PaidAt = o.PaidAt!.Value,
                o.RestaurantId,
                RestaurantName = o.Restaurant != null ? o.Restaurant.Name : null,
                o.WaiterId,
                WaiterName = o.Waiter != null
                    ? (o.Waiter.FirstName + " " + o.Waiter.LastName).Trim()
                    : "Naməlum",
            })
            .ToListAsync(cancellationToken);

        var todayOrders = paidOrders.Where(o => o.PaidAt.Date == todayStart).ToList();
        var monthOrders = paidOrders.Where(o => o.PaidAt >= monthStart).ToList();
        var monthOrderIds = monthOrders.Select(o => o.Id).ToList();

        // Revenue sums
        var todayRevenue = todayOrders.Sum(o => o.TotalAmount);
        var weekRevenue = paidOrders.Where(o => o.PaidAt.Date >= weekStart).Sum(o => o.TotalAmount);
        var monthRevenue = monthOrders.Sum(o => o.TotalAmount);
        var yearRevenue = paidOrders.Where(o => o.PaidAt >= yearStart).Sum(o => o.TotalAmount);
        var avgOrderValue = monthOrders.Count > 0 ? monthOrders.Average(o => o.TotalAmount) : 0m;

        // Table occupancy (live)
        var totalActiveTables = await _context.RestaurantTables
            .CountAsync(t => t.CompanyId == companyId && t.IsActive, cancellationToken);

        var occupiedTables = await _context.Orders
            .CountAsync(o =>
                o.CompanyId == companyId &&
                o.Status != OrderStatus.Paid &&
                o.Status != OrderStatus.Cancelled,
                cancellationToken);

        // Daily revenue — last 7 days
        string[] azDays = ["Baz", "B.e", "Çr", "Çə", "Cü", "Şn", "Şb"];
        var dailyRevenue = Enumerable.Range(0, 7).Select(i =>
        {
            var day = todayStart.AddDays(-6 + i);
            var dayOrders = paidOrders.Where(o => o.PaidAt.Date == day).ToList();
            return new DailyRevenueDto
            {
                Day = azDays[(int)day.DayOfWeek],
                Date = day.ToString("dd.MM"),
                Revenue = dayOrders.Sum(o => o.TotalAmount),
                OrderCount = dayOrders.Count,
            };
        }).ToList();

        // Hourly distribution today (07:00–23:00)
        var hourlySales = Enumerable.Range(7, 17).Select(h =>
        {
            var hOrders = todayOrders.Where(o => o.PaidAt.Hour == h).ToList();
            return new HourlySalesDto
            {
                Hour = h,
                Label = $"{h:D2}:00",
                Revenue = hOrders.Sum(o => o.TotalAmount),
                OrderCount = hOrders.Count,
            };
        }).ToList();

        // Top menu items this month
        var topItems = await _context.OrderLines
            .Where(l => l.CompanyId == companyId && monthOrderIds.Contains(l.OrderId))
            .GroupBy(l => new { l.MenuItemId, l.MenuItem.Name })
            .Select(g => new TopMenuItemDto
            {
                MenuItemId = g.Key.MenuItemId,
                Name = g.Key.Name,
                TotalQuantity = g.Sum(l => l.Quantity),
                TotalRevenue = g.Sum(l => l.LineTotal),
            })
            .OrderByDescending(x => x.TotalQuantity)
            .Take(10)
            .ToListAsync(cancellationToken);

        // Waiter performance this month
        var waiterPerf = monthOrders
            .GroupBy(o => new { o.WaiterId, o.WaiterName })
            .Select(g => new WaiterPerformanceDto
            {
                WaiterId = g.Key.WaiterId,
                WaiterName = g.Key.WaiterName,
                OrderCount = g.Count(),
                TotalRevenue = g.Sum(o => o.TotalAmount),
                AverageOrderValue = Math.Round(g.Average(o => o.TotalAmount), 2),
            })
            .OrderByDescending(x => x.TotalRevenue)
            .ToList();

        // Restaurant revenue this month
        var restaurantRevenue = monthOrders
            .GroupBy(o => new { o.RestaurantId, o.RestaurantName })
            .Select(g => new RestaurantRevenueDto
            {
                RestaurantId = g.Key.RestaurantId,
                RestaurantName = g.Key.RestaurantName ?? $"Restoran #{g.Key.RestaurantId}",
                Revenue = g.Sum(o => o.TotalAmount),
                OrderCount = g.Count(),
            })
            .OrderByDescending(x => x.Revenue)
            .ToList();

        return new DashboardAnalyticsResponse
        {
            TodayRevenue = todayRevenue,
            WeekRevenue = weekRevenue,
            MonthRevenue = monthRevenue,
            YearRevenue = yearRevenue,
            TodayOrderCount = todayOrders.Count,
            MonthOrderCount = monthOrders.Count,
            AverageOrderValue = Math.Round(avgOrderValue, 2),
            TotalActiveTables = totalActiveTables,
            CurrentlyOccupiedTables = Math.Min(occupiedTables, totalActiveTables),
            DailyRevenue = dailyRevenue,
            HourlySales = hourlySales,
            TopMenuItems = topItems,
            WaiterPerformance = waiterPerf,
            RestaurantRevenue = restaurantRevenue,
        };
    }

    public async Task<List<FoodCostResponse>> GetFoodCostAsync(int companyId, CancellationToken cancellationToken)
    {
        // Average AZN unit cost per stock item from purchase history
        var avgCosts = await _context.StockPurchaseLines
            .Where(l => l.StockPurchase.CompanyId == companyId)
            .GroupBy(l => l.StockItemId)
            .Select(g => new { StockItemId = g.Key, AvgUnitCost = g.Average(l => l.UnitPriceAzn) })
            .ToDictionaryAsync(x => x.StockItemId, x => x.AvgUnitCost, cancellationToken);

        var menuItems = await _context.MenuItems
            .Where(m => m.CompanyId == companyId)
            .Select(m => new
            {
                m.Id,
                m.Name,
                m.Price,
                CategoryName = m.MenuCategory != null ? m.MenuCategory.Name : "Naməlum",
                Lines = m.RecipeLines.Select(l => new
                {
                    l.StockItemId,
                    StockItemName = l.StockItem.Name,
                    l.QuantityPerPortion,
                    Unit = l.Unit.ToString(),
                }).ToList(),
            })
            .ToListAsync(cancellationToken);

        var result = new List<FoodCostResponse>();

        foreach (var m in menuItems)
        {
            var lines = new List<FoodCostLineDto>();
            decimal totalCost = 0;
            var hasMissing = false;

            foreach (var l in m.Lines)
            {
                var hasCost = avgCosts.TryGetValue(l.StockItemId, out var unitCost);
                if (!hasCost) hasMissing = true;

                var lineCost = l.QuantityPerPortion * unitCost;
                totalCost += lineCost;

                lines.Add(new FoodCostLineDto
                {
                    StockItemId = l.StockItemId,
                    StockItemName = l.StockItemName,
                    QuantityPerPortion = l.QuantityPerPortion,
                    Unit = l.Unit,
                    UnitCost = Math.Round(unitCost, 4),
                    LineCost = Math.Round(lineCost, 4),
                    MissingCost = !hasCost,
                });
            }

            var hasRecipe = m.Lines.Count > 0;
            var foodCostPct = m.Price > 0 ? Math.Round(totalCost / m.Price * 100, 1) : 0;
            var grossProfit = m.Price - totalCost;
            var grossMargin = m.Price > 0 ? Math.Round(grossProfit / m.Price * 100, 1) : 0;

            result.Add(new FoodCostResponse
            {
                MenuItemId = m.Id,
                MenuItemName = m.Name,
                CategoryName = m.CategoryName,
                SellingPrice = m.Price,
                FoodCost = Math.Round(totalCost, 2),
                FoodCostPercentage = foodCostPct,
                GrossProfit = Math.Round(grossProfit, 2),
                GrossProfitMargin = grossMargin,
                HasRecipe = hasRecipe,
                HasMissingCost = hasMissing,
                Lines = lines,
            });
        }

        return result.OrderByDescending(x => x.FoodCostPercentage).ToList();
    }
}
