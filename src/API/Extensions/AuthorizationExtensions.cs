using Domain.Constants;
using Microsoft.Extensions.DependencyInjection;

namespace API.Extensions;

public static class AuthorizationExtensions
{
    public static IServiceCollection AddPermissionPolicies(this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            options.AddPolicy("CompanyView",
                policy => policy.RequireClaim("Permission", AppPermissions.CompanyView));

            options.AddPolicy("CompanyCreate",
                policy => policy.RequireClaim("Permission", AppPermissions.CompanyCreate));

            options.AddPolicy("CompanyUpdate",
                policy => policy.RequireClaim("Permission", AppPermissions.CompanyUpdate));

            options.AddPolicy("CompanyDelete",
                policy => policy.RequireClaim("Permission", AppPermissions.CompanyDelete));



            options.AddPolicy("UserView",
                policy => policy.RequireClaim("Permission", AppPermissions.UserView));

            options.AddPolicy("UserCreate",
                policy => policy.RequireClaim("Permission", AppPermissions.UserCreate));

            options.AddPolicy("UserUpdate",
                policy => policy.RequireClaim("Permission", AppPermissions.UserUpdate));

            options.AddPolicy("UserDelete",
                policy => policy.RequireClaim("Permission", AppPermissions.UserDelete));


            options.AddPolicy("RestaurantView",
                policy => policy.RequireClaim("Permission", AppPermissions.RestaurantView));
            options.AddPolicy("RestaurantCreate",
                policy => policy.RequireClaim("Permission", AppPermissions.RestaurantCreate));
            options.AddPolicy("RestaurantUpdate",
                policy => policy.RequireClaim("Permission", AppPermissions.RestaurantUpdate));
            options.AddPolicy("RestaurantDelete",
                policy => policy.RequireClaim("Permission", AppPermissions.RestaurantDelete));

            options.AddPolicy("StockCategoryView",
                policy => policy.RequireClaim("Permission", AppPermissions.StockCategoryView));
            options.AddPolicy("StockCategoryCreate",
                policy => policy.RequireClaim("Permission", AppPermissions.StockCategoryCreate));
            options.AddPolicy("StockCategoryUpdate",
                policy => policy.RequireClaim("Permission", AppPermissions.StockCategoryUpdate));
            options.AddPolicy("StockCategoryDelete",
                policy => policy.RequireClaim("Permission", AppPermissions.StockCategoryDelete));
            

            options.AddPolicy("WarehouseView",
                policy => policy.RequireClaim("Permission", AppPermissions.WarehouseView));
            options.AddPolicy("WarehouseCreate",
                policy => policy.RequireClaim("Permission", AppPermissions.WarehouseCreate));
            options.AddPolicy("WarehouseUpdate",
                policy => policy.RequireClaim("Permission", AppPermissions.WarehouseUpdate));
            options.AddPolicy("WarehouseDelete",
                policy => policy.RequireClaim("Permission", AppPermissions.WarehouseDelete));

            options.AddPolicy("StockItemView",
                policy => policy.RequireClaim("Permission", AppPermissions.StockItemView));
            options.AddPolicy("StockItemCreate",
                policy => policy.RequireClaim("Permission", AppPermissions.StockItemCreate));
            options.AddPolicy("StockItemUpdate",
                policy => policy.RequireClaim("Permission", AppPermissions.StockItemUpdate));
            options.AddPolicy("StockItemDelete",
                policy=>policy.RequireClaim("Permission", AppPermissions.StockItemDelete));

            options.AddPolicy("WarehouseStockView",
                policy => policy.RequireClaim("Permission", AppPermissions.WarehouseStockView));
            options.AddPolicy("WarehouseStockCreate",
                policy => policy.RequireClaim("Permission", AppPermissions.WarehouseStockCreate));
            options.AddPolicy("WarehouseStockUpdate",
                policy => policy.RequireClaim("Permission", AppPermissions.WarehouseStockUpdate));
            options.AddPolicy("WarehouseStockDelete",
                policy => policy.RequireClaim("Permission", AppPermissions.WarehouseStockDelete));

            options.AddPolicy("StockRequestView",
                policy => policy.RequireClaim("Permission", AppPermissions.StockRequestView));
            options.AddPolicy("StockRequestCreate",
                policy => policy.RequireClaim("Permission", AppPermissions.StockRequestCreate));
            options.AddPolicy("StockRequestUpdate",
                policy => policy.RequireClaim("Permission", AppPermissions.StockRequestUpdate));
            options.AddPolicy("StockRequestDelete",
                policy => policy.RequireClaim("Permission", AppPermissions.StockRequestDelete));
            options.AddPolicy("StockRequestSubmit",
                policy => policy.RequireClaim("Permission", AppPermissions.StockRequestSubmit));
            options.AddPolicy("StockRequestReject",
                policy => policy.RequireClaim("Permission", AppPermissions.StockRequestReject));

            options.AddPolicy("AuditLogView",
                policy => policy.RequireClaim("Permission", AppPermissions.AuditLogView));

            options.AddPolicy("WarehouseTransferView",
                policy => policy.RequireClaim("Permission", AppPermissions.WarehouseTransferView));
            options.AddPolicy("WarehouseTransferCreate",
                policy => policy.RequireClaim("Permission", AppPermissions.WarehouseTransferCreate));
            options.AddPolicy("WarehouseTransferUpdate",
                policy => policy.RequireClaim("Permission", AppPermissions.WarehouseTransferUpdate));
            options.AddPolicy("WarehouseTransferDelete",
                policy => policy.RequireClaim("Permission", AppPermissions.WarehouseTransferDelete));
            options.AddPolicy("WarehouseTransferApprove",
                policy => policy.RequireClaim("Permission", AppPermissions.WarehouseTransferApprove));
            options.AddPolicy("WarehouseTransferReject",
                policy => policy.RequireClaim("Permission", AppPermissions.WarehouseTransferReject));
            options.AddPolicy("WarehouseTransferCancel",
                policy => policy.RequireClaim("Permission", AppPermissions.WarehouseTransferCancel));
            options.AddPolicy("WarehouseTransferSubmit",
                policy => policy.RequireClaim("Permission", AppPermissions.WarehouseTransferSubmit));
            options.AddPolicy("WarehouseTransferDispatch",
                policy => policy.RequireClaim("Permission", AppPermissions.WarehouseTransferDispatch));
            options.AddPolicy("WarehouseTransferReceive",
                policy => policy.RequireClaim("Permission", AppPermissions.WarehouseTransferReceive));

        });

        return services;
    }
}