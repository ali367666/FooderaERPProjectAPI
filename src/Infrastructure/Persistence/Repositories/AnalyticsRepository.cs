using Application.Analytics;
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
                RestaurantName = g.Key.RestaurantName ?? $"Filial #{g.Key.RestaurantId}",
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

    public async Task<SalesReportResponse> GetSalesReportAsync(
        int companyId,
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken)
    {
        var paidOrders = await _context.Orders
            .Where(o => o.CompanyId == companyId && o.IsPaid && o.PaidAt != null && o.PaidAt >= from && o.PaidAt < to)
            .Select(o => new
            {
                o.Id,
                o.TotalAmount,
                o.DiscountAmount,
                o.PaymentMethod,
                o.WaiterId,
                WaiterName = o.Waiter != null
                    ? (o.Waiter.FirstName + " " + o.Waiter.LastName).Trim()
                    : "Naməlum",
                o.ServiceChargeAmount,
                o.TableId,
                TableName = o.Table != null ? o.Table.Name : "",
                o.OrderNumber,
                o.ReceiptNumber,
                o.GuestCount,
                o.OpenedAt,
                o.PaidAt,
            })
            .ToListAsync(cancellationToken);

        var orderIds = paidOrders.Select(o => o.Id).ToList();

        var products = await _context.OrderLines
            .Where(l => l.CompanyId == companyId && orderIds.Contains(l.OrderId) && l.Status != OrderLineStatus.Cancelled)
            .GroupBy(l => new { l.MenuItemId, l.MenuItem.Name })
            .Select(g => new SalesReportProductLineDto
            {
                MenuItemId = g.Key.MenuItemId,
                Name = g.Key.Name,
                Quantity = g.Sum(l => l.Quantity),
                Revenue = g.Sum(l => l.LineTotal),
            })
            .OrderByDescending(x => x.Revenue)
            .ToListAsync(cancellationToken);

        var categories = await _context.OrderLines
            .Where(l => l.CompanyId == companyId && orderIds.Contains(l.OrderId) && l.Status != OrderLineStatus.Cancelled)
            .GroupBy(l => new { l.MenuItem.MenuCategoryId, l.MenuItem.MenuCategory.Name, l.MenuItem.MenuCategory.ImageUrl })
            .Select(g => new SalesReportCategoryLineDto
            {
                CategoryId = g.Key.MenuCategoryId,
                CategoryName = g.Key.Name,
                ImageUrl = g.Key.ImageUrl,
                Quantity = g.Sum(l => l.Quantity),
                Revenue = g.Sum(l => l.LineTotal),
            })
            .OrderByDescending(x => x.Revenue)
            .ToListAsync(cancellationToken);

        var waiters = paidOrders
            .GroupBy(o => new { o.WaiterId, o.WaiterName })
            .Select(g => new SalesReportWaiterLineDto
            {
                WaiterId = g.Key.WaiterId,
                WaiterName = g.Key.WaiterName,
                OrderCount = g.Count(),
                Revenue = g.Sum(o => o.TotalAmount),
                ServiceCharge = g.Sum(o => o.ServiceChargeAmount ?? 0),
            })
            .OrderByDescending(x => x.Revenue)
            .ToList();

        var receipts = paidOrders
            .Select(o => new SalesReportReceiptDto
            {
                OrderId = o.Id,
                OrderNumber = o.OrderNumber,
                ReceiptNumber = o.ReceiptNumber,
                TableId = o.TableId,
                TableName = string.IsNullOrEmpty(o.TableName) ? $"#{o.TableId}" : o.TableName,
                WaiterName = o.WaiterName,
                GuestCount = o.GuestCount,
                OpenedAt = o.OpenedAt,
                PaidAt = o.PaidAt,
                PaymentMethod = o.PaymentMethod?.ToString(),
                Amount = o.TotalAmount,
                DiscountAmount = o.DiscountAmount,
                ServiceCharge = o.ServiceChargeAmount ?? 0,
            })
            .OrderByDescending(r => r.PaidAt)
            .ToList();

        var tables = receipts
            .GroupBy(r => new { r.TableId, r.TableName })
            .Select(g => new SalesReportTableLineDto
            {
                TableId = g.Key.TableId,
                TableName = g.Key.TableName,
                OrderCount = g.Count(),
                Revenue = g.Sum(r => r.Amount),
                Sessions = g.OrderBy(r => r.OpenedAt).ToList(),
            })
            .OrderByDescending(x => x.Revenue)
            .ToList();

        var cancellations = await _context.OrderCancellations
            .AsNoTracking()
            .Where(c => c.CompanyId == companyId && c.CreatedAtUtc >= from && c.CreatedAtUtc < to)
            .OrderByDescending(c => c.CreatedAtUtc)
            .Select(c => new SalesReportCancellationDto
            {
                OrderId = c.OrderId,
                CancelledAt = c.CreatedAtUtc,
                TableName = c.TableName,
                OrderNumber = c.ReceiptNumber ?? c.OrderNumber,
                IsWholeOrder = c.IsWholeOrder,
                MenuItemName = c.MenuItemName,
                Quantity = c.Quantity,
                Amount = c.Amount,
                Reason = c.Reason,
                Note = c.Note,
                BeforeKitchen = c.BeforeKitchen,
                CancelledBy = c.CancelledByName,
            })
            .ToListAsync(cancellationToken);

        var giftRows = await _context.OrderLines
            .AsNoTracking()
            .Where(l => l.CompanyId == companyId && orderIds.Contains(l.OrderId) && l.IsGift
                && l.Status != OrderLineStatus.Cancelled)
            .Select(l => new
            {
                l.OrderId,
                l.Order.OrderNumber,
                l.Order.ReceiptNumber,
                TableName = l.Order.Table != null ? l.Order.Table.Name : "",
                MenuItemName = l.MenuItem.Name,
                l.Quantity,
                l.UnitPrice,
                l.GiftedAtUtc,
                l.GiftedByUserId,
                OrderPaidAt = l.Order.PaidAt,
            })
            .ToListAsync(cancellationToken);
        var gifterIds = giftRows.Where(g => g.GiftedByUserId != null).Select(g => g.GiftedByUserId!.Value).Distinct().ToList();
        var gifterNames = await _context.Users.AsNoTracking()
            .Where(u => gifterIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.FullName ?? u.UserName ?? "", cancellationToken);
        var gifts = giftRows
            .Select(g => new SalesReportGiftDto
            {
                OrderId = g.OrderId,
                OrderNumber = g.OrderNumber,
                ReceiptNumber = g.ReceiptNumber,
                TableName = string.IsNullOrEmpty(g.TableName) ? "—" : g.TableName,
                MenuItemName = g.MenuItemName,
                Quantity = g.Quantity,
                Value = g.UnitPrice * g.Quantity,
                GiftedAt = g.GiftedAtUtc,
                GiftedBy = g.GiftedByUserId is int id && gifterNames.TryGetValue(id, out var name) ? name : "—",
            })
            .OrderByDescending(g => g.GiftedAt ?? DateTime.MinValue)
            .ToList();

        var productTotal = products.Sum(p => p.Revenue);
        foreach (var p in products)
            p.Percent = productTotal > 0 ? Math.Round(p.Revenue / productTotal * 100, 1) : 0;
        var categoryTotal = categories.Sum(c => c.Revenue);
        foreach (var c in categories)
            c.Percent = categoryTotal > 0 ? Math.Round(c.Revenue / categoryTotal * 100, 1) : 0;

        var returns = await _context.SaleReturns
            .Where(r => r.CompanyId == companyId && r.CreatedAtUtc >= from && r.CreatedAtUtc < to)
            .Select(r => new { r.PaymentMethod, r.TotalAmount })
            .ToListAsync(cancellationToken);

        var returnedProducts = await _context.SaleReturnLines
            .Where(l => l.SaleReturn.CompanyId == companyId
                && l.SaleReturn.CreatedAtUtc >= from && l.SaleReturn.CreatedAtUtc < to)
            .GroupBy(l => new { l.MenuItemId, l.MenuItem.Name })
            .Select(g => new SalesReportReturnLineDto
            {
                MenuItemId = g.Key.MenuItemId,
                Name = g.Key.Name,
                Quantity = g.Sum(l => l.Quantity),
                Amount = g.Sum(l => l.Amount),
            })
            .OrderByDescending(x => x.Amount)
            .ToListAsync(cancellationToken);

        var totalRevenue = paidOrders.Sum(o => o.TotalAmount);
        var totalReturns = returns.Sum(r => r.TotalAmount);

        var bankBreakdown = (await Infrastructure.Persistence.Repositories.CardBankTotals.ComputeAsync(_context, paidOrders.Select(o => o.Id).ToList(), cancellationToken))
            .Select(b => new SalesReportBankDto { BankName = b.BankName, PaymentCount = b.PaymentCount, TotalAmount = b.TotalAmount })
            .ToList();

        return new SalesReportResponse
        {
            From = from,
            To = to,
            ReturnCount = returns.Count,
            TotalReturns = totalReturns,
            CashReturns = returns.Where(r => r.PaymentMethod == PaymentMethod.Cash).Sum(r => r.TotalAmount),
            CardReturns = returns.Where(r => r.PaymentMethod == PaymentMethod.Card).Sum(r => r.TotalAmount),
            CreditReturns = returns.Where(r => r.PaymentMethod == PaymentMethod.Credit).Sum(r => r.TotalAmount),
            NetRevenue = totalRevenue - totalReturns,
            ReturnedProducts = returnedProducts,
            TotalRevenue = totalRevenue,
            TotalDiscount = paidOrders.Sum(o => o.DiscountAmount),
            CashTotal = paidOrders.Where(o => o.PaymentMethod == PaymentMethod.Cash).Sum(o => o.TotalAmount),
            CardTotal = paidOrders.Where(o => o.PaymentMethod == PaymentMethod.Card).Sum(o => o.TotalAmount),
            BankBreakdown = bankBreakdown,
            OrderCount = paidOrders.Count,
            Products = products,
            Waiters = waiters,
            Categories = categories,
            Tables = tables,
            Receipts = receipts,
            TotalServiceCharge = paidOrders.Sum(o => o.ServiceChargeAmount ?? 0),
            Cancellations = cancellations,
            Gifts = gifts,
        };
    }

    public async Task AddSalesInsightsAsync(
        int companyId,
        SalesReportResponse report,
        TimeSpan dayStart,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;

        // ---- payment split: a bill paid in parts counts each part under its own method
        var money = await Infrastructure.Persistence.Repositories.OrderPaymentSplit.ComputeAsync(
            _context,
            report.Receipts.Select(r => (r.OrderId, ParseMethod(r.PaymentMethod), r.Amount)).ToList(),
            cancellationToken);
        foreach (var receipt in report.Receipts)
        {
            var split = money.GetValueOrDefault(receipt.OrderId, Infrastructure.Persistence.Repositories.MoneySplit.None);
            receipt.CashAmount = split.Cash;
            receipt.CardAmount = split.Card;
            receipt.CreditAmount = split.Credit;
        }
        report.CashTotal = report.Receipts.Sum(r => r.CashAmount);
        report.CardTotal = report.Receipts.Sum(r => r.CardAmount);
        report.CreditTotal = report.Receipts.Sum(r => r.CreditAmount);

        var paidTotal = report.CashTotal + report.CardTotal + report.CreditTotal;
        report.PaymentMethods = new (PaymentMethod Method, decimal Amount)[]
            {
                (PaymentMethod.Cash, report.CashTotal),
                (PaymentMethod.Card, report.CardTotal),
                (PaymentMethod.Credit, report.CreditTotal),
            }
            .Where(p => p.Amount > 0)
            .OrderByDescending(p => p.Amount)
            .Select(p => new SalesReportPaymentShareDto
            {
                Method = p.Method.ToString(),
                Amount = p.Amount,
                Percent = SalesReportMath.Percent(p.Amount, paidTotal),
            })
            .ToList();

        // ---- earlier period
        var (previousFrom, previousTo) = SalesReportMath.PreviousWindow(report.From, report.To, now);
        var previous = await _context.Orders.AsNoTracking()
            .Where(o => o.CompanyId == companyId && o.IsPaid && o.PaidAt >= previousFrom && o.PaidAt < previousTo)
            .Select(o => new { o.Id, o.PaymentMethod, o.TotalAmount, o.WaiterId })
            .ToListAsync(cancellationToken);
        var previousMoney = await Infrastructure.Persistence.Repositories.OrderPaymentSplit.ComputeAsync(
            _context, previous.Select(o => (o.Id, o.PaymentMethod, o.TotalAmount)).ToList(), cancellationToken);

        report.Previous = new SalesReportPreviousDto
        {
            From = previousFrom,
            To = previousTo,
            TotalRevenue = previous.Sum(o => o.TotalAmount),
            CashTotal = previousMoney.Values.Sum(m => m.Cash),
            CardTotal = previousMoney.Values.Sum(m => m.Card),
            CreditTotal = previousMoney.Values.Sum(m => m.Credit),
            OrderCount = previous.Count,
        };
        report.RevenueChangePercent = SalesReportMath.Change(report.TotalRevenue, report.Previous.TotalRevenue);
        report.CashChangePercent = SalesReportMath.Change(report.CashTotal, report.Previous.CashTotal);
        report.CardChangePercent = SalesReportMath.Change(report.CardTotal, report.Previous.CardTotal);

        // ---- days
        var byDay = report.Receipts
            .Where(r => r.PaidAt != null)
            .ToLookup(r => SalesReportMath.BusinessDay(r.PaidAt!.Value, dayStart));
        report.Daily = SalesReportMath.Days(report.From, report.To, now, dayStart)
            .Select(day => new SalesReportDayDto
            {
                Date = day,
                Revenue = byDay[day].Sum(r => r.Amount),
                CashTotal = byDay[day].Sum(r => r.CashAmount),
                CardTotal = byDay[day].Sum(r => r.CardAmount),
                CreditTotal = byDay[day].Sum(r => r.CreditAmount),
                OrderCount = byDay[day].Count(),
            })
            .ToList();

        // ---- waiters: place, average, comparison and photo
        var waiterIds = report.Waiters.Select(w => w.WaiterId).ToList();
        var photos = (await _context.Employees.AsNoTracking()
                .Where(e => e.CompanyId == companyId && waiterIds.Contains(e.Id) && e.User != null)
                .Select(e => new { e.Id, e.User!.AvatarUrl })
                .ToListAsync(cancellationToken))
            .ToDictionary(e => e.Id, e => e.AvatarUrl);
        var before = previous.GroupBy(o => o.WaiterId).ToDictionary(g => g.Key, g => g.Sum(o => o.TotalAmount));
        var best = report.Waiters.Count == 0 ? 0 : report.Waiters.Max(w => w.Revenue);
        var rank = 0;
        foreach (var waiter in report.Waiters.OrderByDescending(w => w.Revenue).ThenByDescending(w => w.OrderCount))
        {
            waiter.Rank = ++rank;
            waiter.AverageCheck = waiter.OrderCount == 0 ? 0 : Math.Round(waiter.Revenue / waiter.OrderCount, 2, MidpointRounding.AwayFromZero);
            waiter.PercentOfBest = SalesReportMath.Percent(waiter.Revenue, best);
            waiter.PreviousRevenue = before.GetValueOrDefault(waiter.WaiterId);
            waiter.ChangePercent = SalesReportMath.Change(waiter.Revenue, waiter.PreviousRevenue);
            waiter.Status = SalesReportMath.WaiterStatus(waiter.Rank, waiter.Revenue, waiter.PreviousRevenue);
            waiter.PhotoUrl = photos.GetValueOrDefault(waiter.WaiterId);
        }

        // ---- floor tables: every active table, whether it earned anything or not
        var floor = await _context.RestaurantTables.AsNoTracking()
            .Where(t => t.CompanyId == companyId && t.IsActive
                && (t.Type == RestaurantTableType.Masa || t.Type == RestaurantTableType.Kabinet))
            .Select(t => new SalesReportFloorTableDto
            {
                TableId = t.Id,
                TableName = t.Name,
                RestaurantId = t.RestaurantId,
                RestaurantName = t.Restaurant.Name,
                SectionId = t.SectionId,
                SectionName = t.Section != null ? t.Section.Name : null,
                Capacity = t.Capacity,
                PosX = t.PosX,
                PosY = t.PosY,
                Width = t.Width,
                Height = t.Height,
                Shape = t.Shape,
                Rotation = t.Rotation,
            })
            .ToListAsync(cancellationToken);
        var sittings = report.Receipts.ToLookup(r => r.TableId);
        foreach (var table in floor)
        {
            var own = sittings[table.TableId].ToList();
            var counted = own.Where(r => r.GuestCount > 0).ToList();
            table.OrderCount = own.Count;
            table.Revenue = own.Sum(r => r.Amount);
            table.GuestCount = counted.Sum(r => r.GuestCount!.Value);
            table.OccupancyPercent = table.Capacity > 0 && counted.Count > 0
                ? Math.Min(100, SalesReportMath.Percent((decimal)table.GuestCount / counted.Count, table.Capacity))
                : null;
        }
        var bestTable = floor.Count == 0 ? 0 : floor.Max(t => t.Revenue);
        foreach (var table in floor)
            table.Heat = SalesReportMath.Heat(table.Revenue, bestTable);
        report.FloorTables = floor
            .OrderBy(t => t.RestaurantId)
            .ThenBy(t => t.TableName, Application.RestaurantTables.NaturalNameComparer.Instance)
            .ToList();
    }

    private static PaymentMethod? ParseMethod(string? method)
        => Enum.TryParse<PaymentMethod>(method, out var parsed) ? parsed : null;

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
