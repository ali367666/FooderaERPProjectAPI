namespace Domain.Constants;

public static class AppPermissions
{
    public const string CompanyView = "Company.View";
    public const string CompanyCreate = "Company.Create";
    public const string CompanyUpdate = "Company.Update";
    public const string CompanyDelete = "Company.Delete";

    public const string CompanySettingsView = "CompanySettings.View";
    public const string CompanySettingsUpdate = "CompanySettings.Update";

    public const string UserView = "User.View";
    public const string UserCreate = "User.Create";
    public const string UserUpdate = "User.Update";
    public const string UserDelete = "User.Delete";

    public const string RestaurantView = "Restaurant.View";
    public const string RestaurantCreate = "Restaurant.Create";
    public const string RestaurantUpdate = "Restaurant.Update";
    public const string RestaurantDelete = "Restaurant.Delete";

    public const string StockCategoryView = "StockCategory.View";
    public const string StockCategoryCreate = "StockCategory.Create";
    public const string StockCategoryUpdate = "StockCategory.Update";
    public const string StockCategoryDelete = "StockCategory.Delete";

    public const string WarehouseView = "Warehouse.View";
    public const string WarehouseCreate = "Warehouse.Create";
    public const string WarehouseUpdate = "Warehouse.Update";
    public const string WarehouseDelete = "Warehouse.Delete";

    public const string StockItemView = "StockItem.View";
    public const string StockItemCreate = "StockItem.Create";
    public const string StockItemUpdate = "StockItem.Update";
    public const string StockItemDelete = "StockItem.Delete";

    public const string WarehouseStockView = "WarehouseStock.View";
    public const string WarehouseStockCreate = "WarehouseStock.Create";
    public const string WarehouseStockUpdate = "WarehouseStock.Update";
    public const string WarehouseStockDelete = "WarehouseStock.Delete";

    public const string StockRequestView = "StockRequest.View";
    public const string StockRequestCreate = "StockRequest.Create";
    public const string StockRequestUpdate = "StockRequest.Update";
    public const string StockRequestDelete = "StockRequest.Delete";
    public const string StockRequestApprove = "StockRequest.Approve";
    public const string StockRequestReject = "StockRequest.Reject";
    public const string StockRequestSubmit = "StockRequest.Submit";
    public const string StockRequestRecall = "StockRequest.Recall";

    public const string RestaurantTableView = "RestaurantTable.View";
    public const string RestaurantTableCreate = "RestaurantTable.Create";
    public const string RestaurantTableUpdate = "RestaurantTable.Update";
    public const string RestaurantTableDelete = "RestaurantTable.Delete";

    public const string DepartmentView = "Department.View";
    public const string DepartmentCreate = "Department.Create";
    public const string DepartmentUpdate = "Department.Update";
    public const string DepartmentDelete = "Department.Delete";

    public const string PositionView = "Position.View";
    public const string PositionCreate = "Position.Create";
    public const string PositionUpdate = "Position.Update";
    public const string PositionDelete = "Position.Delete";

    public const string EmployeeView = "Employee.View";
    public const string EmployeeCreate = "Employee.Create";
    public const string EmployeeUpdate = "Employee.Update";
    public const string EmployeeDelete = "Employee.Delete";

    public const string OrdersView = "Orders.View";
    public const string OrdersCreate = "Orders.Create";
    public const string OrdersUpdate = "Orders.Update";
    public const string OrdersAdd    = "Orders.Add";
    public const string OrdersServe = "Orders.Serve";
    public const string OrdersPay = "Orders.Pay";
    public const string PaymentCreate = "Payment.Create";
    public const string RoleView = "Role.View";
    public const string RoleCreate = "Role.Create";
    public const string RoleUpdate = "Role.Update";
    public const string RoleDelete = "Role.Delete";
    public const string UserRoleManage = "UserRole.Manage";

    public const string AnalyticsView = "Analytics.View";

    public const string BscInvoiceView = "BscInvoice.View";
    public const string BscInvoiceSync = "BscInvoice.Sync";


    public const string MenuItemView = "MenuItem.View";
    public const string MenuItemCreate = "MenuItem.Create";
    public const string MenuItemUpdate = "MenuItem.Update";
    public const string MenuItemDelete = "MenuItem.Delete";

    public const string MenuCategoryView = "MenuCategory.View";
    public const string MenuCategoryCreate = "MenuCategory.Create";
    public const string MenuCategoryUpdate = "MenuCategory.Update";
    public const string MenuCategoryDelete = "MenuCategory.Delete";

    public const string KitchenView = "Kitchen.View";
    public const string KitchenMarkReady = "Kitchen.MarkReady";
    public const string KitchenStartPreparing = "Kitchen.StartPreparing";


    public const string AuditLogView = "Permissions.AuditLog.View";
    public const string AuditLogsView = AuditLogView;

    public const string StockPurchaseView = "StockPurchase.View";
    public const string StockPurchaseCreate = "StockPurchase.Create";
    public const string StockPurchaseUpdate = "StockPurchase.Update";
    public const string StockPurchaseDelete = "StockPurchase.Delete";
    public const string StockPurchaseSubmit = "StockPurchase.Submit";
    public const string StockPurchaseApprove = "StockPurchase.Approve";
    public const string StockPurchaseReject = "StockPurchase.Reject";

    public const string ReservationView = "Reservation.View";
    public const string ReservationCreate = "Reservation.Create";
    public const string ReservationUpdate = "Reservation.Update";
    public const string ReservationDelete = "Reservation.Delete";
    public const string ReservationConfirm = "Reservation.Confirm";
    public const string ReservationCancel = "Reservation.Cancel";

    public const string DiscountView = "Discount.View";
    public const string DiscountCreate = "Discount.Create";
    public const string DiscountUpdate = "Discount.Update";
    public const string DiscountDelete = "Discount.Delete";
    public const string DiscountApply = "Discount.Apply";

    public const string PosMoveTable = "Pos.MoveTable";
    public const string PosDeleteOrder = "Pos.DeleteOrder";
    public const string PosAccessSettings = "Pos.AccessSettings";
    public const string PosChangePrice = "Pos.ChangePrice";
    public const string PosZReport = "Pos.ZReport";
    public const string PosPrintReceipt = "Pos.PrintReceipt";
    public const string PosOverridePrice = "Pos.OverridePrice";
    public const string PosChangeDepartment = "Pos.ChangeDepartment";
    public const string PosRedirectUser = "Pos.RedirectUser";
    public const string PosEditProductInSale = "Pos.EditProductInSale";
    public const string PosDeleteProductInSale = "Pos.DeleteProductInSale";
    public const string PosTableServiceCharge = "Pos.TableServiceCharge";
    public const string PosDeleteReceipt = "Pos.DeleteReceipt";
    public const string PosWarehouseAmountChange = "Pos.WarehouseAmountChange";
    public const string PosPrintOldReceipt = "Pos.PrintOldReceipt";

    public const string RestaurantSectionView = "RestaurantSection.View";
    public const string RestaurantSectionCreate = "RestaurantSection.Create";
    public const string RestaurantSectionUpdate = "RestaurantSection.Update";
    public const string RestaurantSectionDelete = "RestaurantSection.Delete";

    public const string PrinterView = "Printer.View";
    public const string PrinterCreate = "Printer.Create";
    public const string PrinterUpdate = "Printer.Update";
    public const string PrinterDelete = "Printer.Delete";
    public const string PrinterPrint = "Printer.Print";

    public const string CounterpartyView = "Counterparty.View";
    public const string CounterpartyCreate = "Counterparty.Create";
    public const string CounterpartyUpdate = "Counterparty.Update";
    public const string CounterpartyDelete = "Counterparty.Delete";

    public const string FiscalDeviceView = "FiscalDevice.View";
    public const string FiscalDeviceCreate = "FiscalDevice.Create";
    public const string FiscalDeviceUpdate = "FiscalDevice.Update";
    public const string FiscalDeviceDelete = "FiscalDevice.Delete";

    public const string ScaleDeviceView = "ScaleDevice.View";
    public const string ScaleDeviceCreate = "ScaleDevice.Create";
    public const string ScaleDeviceUpdate = "ScaleDevice.Update";
    public const string ScaleDeviceDelete = "ScaleDevice.Delete";
}