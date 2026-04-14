using Domain.Constants;

namespace Infrastructure.Identity;

public static class RolePermissionSeeder
{
    public static readonly Dictionary<string, List<string>> Permissions = new()
    {
        {
            AppRoles.Admin,
            new List<string>
            {
                AppPermissions.CompanyView,
                AppPermissions.CompanyCreate,
                AppPermissions.CompanyUpdate,
                AppPermissions.CompanyDelete,

                AppPermissions.UserView,
                AppPermissions.UserCreate,
                AppPermissions.UserUpdate,
                AppPermissions.UserDelete,

                AppPermissions.RestaurantView,
                AppPermissions.RestaurantCreate,
                AppPermissions.RestaurantUpdate,
                AppPermissions.RestaurantDelete,

                AppPermissions.StockCategoryCreate,
                AppPermissions.StockCategoryUpdate,
                AppPermissions.StockCategoryDelete,
                AppPermissions.StockCategoryView,

                AppPermissions.WarehouseView,
                AppPermissions.WarehouseCreate,
                AppPermissions.WarehouseUpdate,
                AppPermissions.WarehouseDelete,

                AppPermissions.StockItemView,
                AppPermissions.StockItemCreate,
                AppPermissions.StockItemUpdate,
                AppPermissions.StockItemDelete,

                AppPermissions.WarehouseStockView,
                AppPermissions.WarehouseStockCreate,
                AppPermissions.WarehouseStockUpdate,
                AppPermissions.WarehouseStockDelete,

                AppPermissions.StockRequestView,
                AppPermissions.StockRequestCreate,
                AppPermissions.StockRequestUpdate,
                AppPermissions.StockRequestDelete,

                AppPermissions.AuditLogView,

                AppPermissions.PositionView,
                AppPermissions.PositionCreate,
                AppPermissions.PositionUpdate,
                AppPermissions.PositionDelete,

                AppPermissions.DepartmentCreate,
                AppPermissions.DepartmentUpdate,
                AppPermissions.DepartmentDelete,
                AppPermissions.DepartmentView,

                AppPermissions.EmployeeView,
                AppPermissions.EmployeeCreate,
                AppPermissions.EmployeeUpdate,
                AppPermissions.EmployeeDelete,

                AppPermissions.OrdersView,
                AppPermissions.OrdersCreate,
                AppPermissions.OrdersUpdate,
                AppPermissions.OrdersDelete,

                AppPermissions.KitchenView,
                AppPermissions.KitchenMarkReady,
                AppPermissions.KitchenStartPreparing,

                AppPermissions.MenuCategoryView,
                AppPermissions.MenuCategoryCreate,
                AppPermissions.MenuCategoryUpdate,
                AppPermissions.MenuCategoryDelete,

                AppPermissions.MenuItemView,
                AppPermissions.MenuItemCreate,
                AppPermissions.MenuItemUpdate,
                AppPermissions.MenuItemDelete,

                AppPermissions.RestaurantTableView,
                AppPermissions.RestaurantTableCreate,
                AppPermissions.RestaurantTableUpdate,
                AppPermissions.RestaurantTableDelete,

                AppPermissions.WarehouseTransferView,
                AppPermissions.WarehouseTransferCreate,
                AppPermissions.WarehouseTransferUpdate,
                AppPermissions.WarehouseTransferDelete,
                AppPermissions.WarehouseTransferApprove,
                AppPermissions.WarehouseTransferReject,
                AppPermissions.WarehouseTransferCancel,
                AppPermissions.WarehouseTransferReceive,
                AppPermissions.WarehouseTransferSubmit,
                AppPermissions.WarehouseTransferDispatch,




            }
        },
        {
            AppRoles.User,
            new List<string>
            {
                AppPermissions.CompanyView,
                AppPermissions.UserView,
                AppPermissions.RestaurantView,
                AppPermissions.StockCategoryView,
                AppPermissions.WarehouseView,
                AppPermissions.WarehouseStockView,
                AppPermissions.PositionView,
                AppPermissions.StockItemView,
                AppPermissions.StockRequestView,
                AppPermissions.DepartmentView,
                AppPermissions.EmployeeView,
                AppPermissions.OrdersView,
                AppPermissions.AuditLogView,
                AppPermissions.KitchenView,
                AppPermissions.MenuCategoryView,
                AppPermissions.MenuItemView,
                AppPermissions.RestaurantTableView,
                AppPermissions.WarehouseTransferView,
            }
        }
    };
}