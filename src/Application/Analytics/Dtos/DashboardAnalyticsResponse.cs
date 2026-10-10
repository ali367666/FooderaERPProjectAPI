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
