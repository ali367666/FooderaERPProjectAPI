using Domain.Entities;
using Domain.Entities.WarehouseAndStock;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Persistence.Configurations;

namespace Infrastructure.Persistence.Context;

public class AppDbContext : IdentityDbContext<User, IdentityRole<int>, int>
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }


    // Core entities
    public DbSet<Company> Companies { get; set; } = null!;
    public DbSet<Restaurant> Restaurants { get; set; } = null!;

    // Stock system
    public DbSet<StockCategory> Categories { get; set; } = null!;
    public DbSet<StockItem> StockItems { get; set; } = null!;
    public DbSet<StockMovement> StockMovements { get; set; } = null!;

    // Warehouse
    public DbSet<Warehouse> Warehouses { get; set; } = null!;
    public DbSet<WarehouseStock> WarehouseStocks { get; set; } = null!;

    // Request system
    public DbSet<StockRequest> StockRequests { get; set; } = null!;
    public DbSet<StockRequestLine> StockRequestLines { get; set; } = null!;

    // Transfer system
    public DbSet<WarehouseTransfer> WarehouseTransfers { get; set; } = null!;
    public DbSet<WarehouseTransferLine> WarehouseTransferLines { get; set; } = null!;

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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}