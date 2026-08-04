namespace Application.Analytics.Dtos;

public class DashboardAnalyticsResponse
{
    // Revenue
    public decimal TodayRevenue { get; set; }
    public decimal WeekRevenue { get; set; }
    public decimal MonthRevenue { get; set; }
    public decimal YearRevenue { get; set; }

    // Orders
    public int TodayOrderCount { get; set; }
    public int MonthOrderCount { get; set; }
    public decimal AverageOrderValue { get; set; }

    // Tables
    public int TotalActiveTables { get; set; }
    public int CurrentlyOccupiedTables { get; set; }

    // Revenue last 7 days
    public List<DailyRevenueDto> DailyRevenue { get; set; } = [];

    // Hourly distribution today
    public List<HourlySalesDto> HourlySales { get; set; } = [];

    // Top 10 menu items this month
    public List<TopMenuItemDto> TopMenuItems { get; set; } = [];

    // Waiter performance this month
    public List<WaiterPerformanceDto> WaiterPerformance { get; set; } = [];

    // Restaurant breakdown this month
    public List<RestaurantRevenueDto> RestaurantRevenue { get; set; } = [];
}

public class DailyRevenueDto
{
    public string Day { get; set; } = default!;   // "Baz", "B.e", "Ça", "Çə", "Cü", "Şn", "Şb"
    public string Date { get; set; } = default!;  // "15.06"
    public decimal Revenue { get; set; }
    public int OrderCount { get; set; }
}

public class HourlySalesDto
{
    public int Hour { get; set; }          // 0-23
    public string Label { get; set; } = default!;  // "09:00"
    public decimal Revenue { get; set; }
    public int OrderCount { get; set; }
}

public class TopMenuItemDto
{
    public int MenuItemId { get; set; }
    public string Name { get; set; } = default!;
    public int TotalQuantity { get; set; }
    public decimal TotalRevenue { get; set; }
}

public class WaiterPerformanceDto
{
    public int WaiterId { get; set; }
    public string WaiterName { get; set; } = default!;
    public int OrderCount { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal AverageOrderValue { get; set; }
}

public class RestaurantRevenueDto
{
    public int RestaurantId { get; set; }
    public string RestaurantName { get; set; } = default!;
    public decimal Revenue { get; set; }
    public int OrderCount { get; set; }
}
