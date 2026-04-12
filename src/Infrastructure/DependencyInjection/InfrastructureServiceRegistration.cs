using Application.Abstractions.Repositories;
using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Common.Interfaces.Abstracts.Services;
using Domain.Entities;
using Infrastructure.Identity;
using Infrastructure.Persistence.Context;
using Infrastructure.Persistence.Repositories;
using Infrastructure.Repositories;
using Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Persistence.Repositories;
using Persistence.UnitOfWork;

namespace Infrastructure.DependencyInjection;

public static class InfrastructureServiceRegistration
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // 🔹 DbContext
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));
        services.AddScoped<IIdentityService, IdentityService>();
        services.AddScoped<IRestaurantRepository, RestaurantRepository>();

        // 🔹 Identity
        services.AddIdentity<User, IdentityRole<int>>(options =>
        {
            // 🔐 Password settings
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireUppercase = false;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequiredLength = 6;

            // 👤 User settings
            options.User.RequireUniqueEmail = true;

            // 🔒 Lockout (optional amma faydalıdır)
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
        })
        .AddEntityFrameworkStores<AppDbContext>()
        .AddDefaultTokenProviders();

        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        // 🔹 Repository-lər
        services.AddScoped<ICompanyRepository,       CompanyRepository>();
        services.AddScoped<IStockCategoryRepository, StockCategoryRepository>();
        services.AddScoped<IWarehouseRepository,     WarehouseRepository>();
        services.AddScoped<IUserRepository,          UserRepository>();
        services.AddScoped<IStockItemRepository,     StockItemRepository>();
        services.AddScoped<IWarehouseStockRepository, WarehouseStockRepository>();
        services.AddScoped<IAuditLogService, AuditLogService>();
        services.AddScoped<IAuditLogRepository, AuditLogRepository>();
        services.AddScoped<IStockRequestRepository, StockRequestRepository>();
        services.AddScoped<IWarehouseTransferRepository, WarehouseTransferRepository>();
        services.AddScoped<IStockMovementRepository, StockMovementRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IStockRequestLineRepository, StockRequestLineRepository>();
        services.AddScoped<IDepartmentRepository, DepartmentRepository>();
        services.AddScoped<IPositionRepository, PositionRepository>();
        services.AddScoped<IEmployeeRepository, EmployeeRepository>();
        services.AddScoped<IMenuCategoryRepository, MenuCategoryRepository>();
        services.AddScoped<IMenuItemRepository, MenuItemRepository>();  
        services.AddScoped<IRestaurantTableRepository, RestaurantTableRepository>();
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IOrderLineRepository, OrderLineRepository>();


        return services;
    }
}