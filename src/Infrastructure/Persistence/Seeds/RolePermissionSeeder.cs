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
        (AppPermissions.PosEditProductInSale, "Edit Product In Sale", "Pos", "EditProductInSale"),
        (AppPermissions.PosDeleteProductInSale, "Delete Product In Sale", "Pos", "DeleteProductInSale"),
        (AppPermissions.PosDeleteOrder, "Delete Order (POS)", "Pos", "DeleteOrder"),
        (AppPermissions.PosDeleteReceipt, "Delete Receipt (POS)", "Pos", "DeleteReceipt"),
        (AppPermissions.RestaurantSectionView, "View Restaurant Sections", "RestaurantSection", "View"),
        (AppPermissions.PrinterView, "View Printers", "Printer", "View"),
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
                AppPermissions.UserView, AppPermissions.RoleView, AppPermissions.UserRoleManage,
                AppPermissions.DepartmentView, AppPermissions.PositionView, AppPermissions.EmployeeView,
                AppPermissions.RestaurantView, AppPermissions.RestaurantTableView,
                AppPermissions.CounterpartyView, AppPermissions.CounterpartyCreate, AppPermissions.CounterpartyUpdate,
                AppPermissions.StockCategoryView, AppPermissions.StockItemView,
                AppPermissions.WarehouseView, AppPermissions.WarehouseStockView,
                AppPermissions.StockRequestView, AppPermissions.StockPurchaseView,
                AppPermissions.ReservationView, AppPermissions.DiscountView, AppPermissions.DiscountApply,
                AppPermissions.AnalyticsView, AppPermissions.BscInvoiceView,
                AppPermissions.PosDeleteOrder, AppPermissions.PosDeleteReceipt,
                AppPermissions.PosEditProductInSale, AppPermissions.PosDeleteProductInSale,
                AppPermissions.PosMoveTable, AppPermissions.PosRedirectUser, AppPermissions.PosChangePrice,
                AppPermissions.PosOverridePrice, AppPermissions.PosTableServiceCharge,
                AppPermissions.PosWarehouseAmountChange, AppPermissions.PosZReport,
                AppPermissions.PosAccessSettings, AppPermissions.PosPrintOldReceipt, AppPermissions.PosChangeDepartment,
                AppPermissions.PosPrintReceipt,
                AppPermissions.RestaurantSectionView, AppPermissions.RestaurantSectionCreate,
                AppPermissions.RestaurantSectionUpdate, AppPermissions.RestaurantSectionDelete,
                AppPermissions.PrinterView, AppPermissions.PrinterCreate, AppPermissions.PrinterUpdate,
                AppPermissions.PrinterDelete, AppPermissions.PrinterPrint
            }
        },
        {
            AppRoles.Waiter,
            new List<string>
            {
                AppPermissions.OrdersView, AppPermissions.OrdersCreate, AppPermissions.OrdersAdd,
                AppPermissions.OrdersServe, AppPermissions.MenuItemView, AppPermissions.MenuCategoryView,
                AppPermissions.RestaurantView, AppPermissions.RestaurantTableView, AppPermissions.EmployeeView,
                AppPermissions.ReservationView,
                AppPermissions.PosEditProductInSale, AppPermissions.PosDeleteProductInSale,
                AppPermissions.PosPrintReceipt, AppPermissions.PrinterPrint
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
                AppPermissions.OrdersView, AppPermissions.OrdersPay, AppPermissions.PaymentCreate,
                AppPermissions.DiscountApply, AppPermissions.PosPrintReceipt, AppPermissions.PrinterPrint,
                AppPermissions.PosZReport
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