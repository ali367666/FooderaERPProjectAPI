using Domain.Entities;
using Domain.Entities.BscInvoice;
using Domain.Entities.WarehouseAndStock;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Persistence.Configurations;

namespace Infrastructure.Persistence.Context;

public class AppDbContext : IdentityDbContext<User, IdentityRole<int>, int>
{
    // SQL Server's datetime2 has no timezone concept, so EF Core loses DateTimeKind on read.
    // All persisted timestamps in this app are written with DateTime.UtcNow, so we re-tag them
    // as Utc on the way out — otherwise System.Text.Json omits the "Z" suffix and clients in
    // non-UTC timezones (e.g. Azerbaijan, UTC+4) misinterpret the value as local time.
    private static readonly ValueConverter<DateTime, DateTime> UtcDateTimeConverter = new(
        v => v,
        v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

    private static readonly ValueConverter<DateTime?, DateTime?> UtcNullableDateTimeConverter = new(
        v => v,
        v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : v);

    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }


    // Core entities
    public DbSet<Company> Companies { get; set; } = null!;
    public DbSet<CompanySettings> CompanySettings { get; set; } = null!;
    public DbSet<RestaurantSection> RestaurantSections { get; set; } = null!;
    public DbSet<Printer> Printers { get; set; } = null!;
    public DbSet<PrinterStationType> PrinterStationTypes { get; set; } = null!;
    public DbSet<Counterparty> Counterparties { get; set; } = null!;
    public DbSet<CounterpartyCategory> CounterpartyCategories { get; set; } = null!;
    public DbSet<MenuItemType> MenuItemTypes { get; set; } = null!;
    public DbSet<MenuItemSetComponent> MenuItemSetComponents { get; set; } = null!;
    public DbSet<Shift> Shifts { get; set; } = null!;
    public DbSet<Restaurant> Restaurants { get; set; } = null!;
    public DbSet<Reservation> Reservations { get; set; } = null!;
    public DbSet<Discount> Discounts { get; set; } = null!;

    // Stock system
    public DbSet<StockCategory> Categories { get; set; } = null!;
    public DbSet<StockItem> StockItems { get; set; } = null!;
    public DbSet<StockMovement> StockMovements { get; set; } = null!;
    public DbSet<MenuItemRecipeLine> MenuItemRecipeLines { get; set; } = null!;
    public DbSet<StockPurchase> StockPurchases { get; set; } = null!;
    public DbSet<StockPurchaseLine> StockPurchaseLines { get; set; } = null!;

    // Warehouse
    public DbSet<Warehouse> Warehouses { get; set; } = null!;
    public DbSet<WarehouseStockDocument> WarehouseStockDocuments { get; set; } = null!;
    public DbSet<WarehouseStockLine> WarehouseStockLines { get; set; } = null!;
    public DbSet<WarehouseStock> WarehouseStocks { get; set; } = null!;

    // Request system
    public DbSet<StockRequest> StockRequests { get; set; } = null!;
    public DbSet<StockRequestLine> StockRequestLines { get; set; } = null!;

    // BSC Invoice sync
    public DbSet<BscInvoiceM> BscInvoiceMs { get; set; } = null!;
    public DbSet<BscInvoiceD> BscInvoiceDs { get; set; } = null!;

    //Loggin system
    public DbSet<AuditLog> AuditLogs { get; set; } = null!;

    public DbSet<Department> Departments { get; set; } = null!;
    public DbSet<Employee> Employees { get; set; } = null!;
    public DbSet<Position> Positions { get; set; } = null!;

    public DbSet<MenuCategory> MenuCategories { get; set; } = null!;
    public DbSet<MenuItem> MenuItems { get; set; } = null!;
    public DbSet<Order> Orders { get; set; } = null!;
    public DbSet<OrderLine> OrderLines { get; set; } = null!;
    public DbSet<RestaurantTable> RestaurantTables { get; set; } = null!;
    public DbSet<RefreshToken> RefreshTokens { get; set; } = null!;
    public DbSet<Notification> Notifications { get; set; } = null!;
    public DbSet<Permission> Permissions { get; set; } = null!;
    public DbSet<RolePermission> RolePermissions { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                if (property.ClrType == typeof(DateTime))
                {
                    property.SetValueConverter(UtcDateTimeConverter);
                }
                else if (property.ClrType == typeof(DateTime?))
                {
                    property.SetValueConverter(UtcNullableDateTimeConverter);
                }
            }
        }
    }
}