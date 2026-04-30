using Domain.Constants;

namespace Infrastructure.Identity;

public static class RolePermissionSeeder
{
    public static readonly List<(string Name, string DisplayName, string Module, string Action)> PermissionCatalog =
    [
        (AppPermissions.MenuItemView, "View Menu Items", "MenuItem", "View"),
        (AppPermissions.MenuItemCreate, "Create Menu Item", "MenuItem", "Create"),
        (AppPermissions.MenuItemUpdate, "Update Menu Item", "MenuItem", "Update"),
        (AppPermissions.MenuItemDelete, "Delete Menu Item", "MenuItem", "Delete"),
        (AppPermissions.MenuCategoryView, "View Menu Categories", "MenuCategory", "View"),
        (AppPermissions.MenuCategoryCreate, "Create Menu Category", "MenuCategory", "Create"),
        (AppPermissions.MenuCategoryUpdate, "Update Menu Category", "MenuCategory", "Update"),
        (AppPermissions.MenuCategoryDelete, "Delete Menu Category", "MenuCategory", "Delete"),
        (AppPermissions.OrdersView, "View Orders", "Order", "View"),
        (AppPermissions.OrdersCreate, "Create Order", "Order", "Create"),
        (AppPermissions.OrdersAdd, "Add Order Line", "Order", "Add"),
        (AppPermissions.OrdersUpdate, "Update Order", "Order", "Update"),
        (AppPermissions.OrdersDelete, "Delete Order", "Order", "Delete"),
        (AppPermissions.OrdersServe, "Serve Order", "Order", "Serve"),
        (AppPermissions.OrdersPay, "Create Payment", "Payment", "Create"),
        (AppPermissions.PaymentCreate, "Create Payment", "Payment", "Create"),
        (AppPermissions.KitchenView, "View Kitchen", "Kitchen", "View"),
        (AppPermissions.KitchenMarkReady, "Mark Kitchen Line Ready", "Kitchen", "MarkReady"),
        (AppPermissions.KitchenStartPreparing, "Start Kitchen Preparation", "Kitchen", "StartPreparing"),
        (AppPermissions.UserView, "View Users", "User", "View"),
        (AppPermissions.UserCreate, "Create User", "User", "Create"),
        (AppPermissions.UserUpdate, "Update User", "User", "Update"),
        (AppPermissions.UserDelete, "Delete User", "User", "Delete"),
        (AppPermissions.RoleView, "View Roles", "Role", "View"),
        (AppPermissions.UserRoleManage, "Manage User Roles", "UserRole", "Manage"),
        (AppPermissions.RestaurantView, "View Restaurants", "Restaurant", "View"),
        (AppPermissions.RestaurantTableView, "View Restaurant Tables", "RestaurantTable", "View"),
        (AppPermissions.EmployeeView, "View Employees", "Employee", "View"),
        (AppPermissions.AuditLogView, "View Audit Logs", "AuditLog", "View"),
    ];

    public static readonly Dictionary<string, List<string>> Permissions = new()
    {
        { AppRoles.Admin, PermissionCatalog.Select(x => x.Name).ToList() },
        {
            AppRoles.Manager,
            new List<string>
            {
                AppPermissions.MenuItemView, AppPermissions.MenuItemCreate, AppPermissions.MenuItemUpdate,
                AppPermissions.MenuCategoryView, AppPermissions.MenuCategoryCreate, AppPermissions.MenuCategoryUpdate,
                AppPermissions.OrdersView, AppPermissions.OrdersCreate, AppPermissions.OrdersUpdate, AppPermissions.OrdersPay,
                AppPermissions.OrdersServe,
                AppPermissions.KitchenView, AppPermissions.KitchenMarkReady, AppPermissions.KitchenStartPreparing,
                AppPermissions.UserView, AppPermissions.RoleView, AppPermissions.UserRoleManage
            }
        },
        {
            AppRoles.Waiter,
            new List<string>
            {
                AppPermissions.OrdersView, AppPermissions.OrdersCreate, AppPermissions.OrdersAdd,
                AppPermissions.OrdersServe, AppPermissions.MenuItemView,
                AppPermissions.RestaurantView, AppPermissions.RestaurantTableView, AppPermissions.EmployeeView
            }
        },
        {
            AppRoles.Kitchen,
            new List<string>
            {
                AppPermissions.KitchenView, AppPermissions.KitchenMarkReady, AppPermissions.KitchenStartPreparing
            }
        },
        {
            AppRoles.Cashier,
            new List<string>
            {
                AppPermissions.OrdersView, AppPermissions.OrdersPay, AppPermissions.PaymentCreate
            }
        },
        {
            AppRoles.User,
            new List<string>
            {
                AppPermissions.OrdersView,
                AppPermissions.MenuItemView,
            }
        }
    };
}