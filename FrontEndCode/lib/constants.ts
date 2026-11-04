import {
  LayoutDashboard,
  Building2,
  Briefcase,
  Users,
  LayoutGrid,
  PenTool,
  Package,
  ShoppingCart,
  UtensilsCrossed,
  Wine,
  Bell,
  FileText,
  LucideIcon,
  Boxes,
  Warehouse,
  ClipboardList,
  Layers,
  History,
  Shield,
  UserCog,
  ChefHat,
  ShieldCheck,
  BarChart3,
  CalendarCheck,
  Calculator,
  Tag,
  Settings,
  QrCode,
  Printer as PrinterIcon,
  LayoutTemplate,
  Landmark,
  Scale,
  Monitor,
  Gamepad2,
  Truck,
  Wallet,
  TrendingUp,
  Store,
  ToggleRight,
  Undo2,
  ClipboardCheck,
  ArrowLeftRight,
  Timer,
} from "lucide-react";

export interface NavItem {
  title: string;
  href: string;
  icon: LucideIcon;
  badge?: string;
  permission?: string;
  /** Company-level module toggle key (from CompanySettingsBranding) required for this item to show. */
  module?: "moduleFilial" | "moduleAnbar" | "moduleRezervasyon" | "moduleMasaBolge";
  /** Restaurant-only page (tables, kitchen) with no equivalent in a Mağaza (store) business — hidden when moduleDataSecimi is on. */
  hiddenForStoreMode?: boolean;
  /** Label to show instead of `title` when the company is in Mağaza (store) mode — e.g. "Menu" reads as "Products" for a store. */
  storeModeTitle?: string;
  /** Shown only while the selected company works in Mağaza (store) mode. */
  storeOnly?: boolean;
  /** Platform pages for the SuperAdmin only. */
  superAdminOnly?: boolean;
}

export interface NavGroup {
  title: string;
  /** Label to show instead of `title` when the company is in Mağaza (store) mode. */
  storeModeTitle?: string;
  items: NavItem[];
}

export const navGroups: NavGroup[] = [
  {
    title: "MAĞAZA",
    items: [
      { title: "Satışlar", href: "/dashboard/store/sales", icon: FileText, permission: "StoreSale.View", storeOnly: true },
      { title: "Məhsullar", href: "/dashboard/store/products", icon: Package, permission: "StoreProduct.View", storeOnly: true },
      { title: "Stok", href: "/dashboard/store/stock", icon: Boxes, permission: "StoreStock.View", storeOnly: true },
      { title: "Alışlar", href: "/dashboard/store/purchases", icon: Truck, permission: "StorePurchase.View", storeOnly: true },
      { title: "Təchizatçılar", href: "/dashboard/store/suppliers", icon: Briefcase, permission: "StoreSupplier.View", storeOnly: true },
      { title: "İnventarizasiya", href: "/dashboard/store/inventory", icon: ClipboardCheck, permission: "StoreInventory.View", storeOnly: true },
      { title: "Transfer", href: "/dashboard/store/transfers", icon: ArrowLeftRight, permission: "StoreTransfer.View", storeOnly: true },
      { title: "Müştərilər", href: "/dashboard/store/customers", icon: Users, permission: "StoreCustomer.View", storeOnly: true },
      { title: "Növbələr", href: "/dashboard/store/shifts", icon: Timer, permission: "StoreReport.View", storeOnly: true },
      { title: "Kassalar və qaydalar", href: "/dashboard/store/registers", icon: Settings, permission: "StoreRegister.Manage", storeOnly: true },
    ],
  },
  {
    title: "MAĞAZA HESABATLARI",
    items: [
      { title: "Satış hesabatı", href: "/dashboard/store/reports/satis-hesabati", icon: BarChart3, permission: "StoreReport.Finance", storeOnly: true },
      { title: "Satış və mənfəət", href: "/dashboard/store/reports/satis-menfeet", icon: BarChart3, permission: "StoreReport.Finance", storeOnly: true },
      { title: "Ümumi stok", href: "/dashboard/store/reports/umumi-stok", icon: Boxes, permission: "StoreReport.View", storeOnly: true },
      { title: "Stok hesabatı", href: "/dashboard/store/reports/stok-hesabati", icon: Boxes, permission: "StoreReport.View", storeOnly: true },
      { title: "Reytinq", href: "/dashboard/store/reports/reytinq", icon: BarChart3, permission: "StoreReport.View", storeOnly: true },
      { title: "Kassa hesabatı", href: "/dashboard/store/reports/kassa-hesabati", icon: BarChart3, permission: "StoreReport.View", storeOnly: true },
      { title: "Kassa hərəkətləri", href: "/dashboard/store/reports/kassa-hereketleri", icon: BarChart3, permission: "StoreReport.View", storeOnly: true },
      { title: "Bonus hesabatı", href: "/dashboard/store/reports/bonus-hesabati", icon: BarChart3, permission: "StoreReport.Finance", storeOnly: true },
      { title: "Müştəri borcları", href: "/dashboard/store/reports/borclar", icon: BarChart3, permission: "StoreReport.Finance", storeOnly: true },
      { title: "Çeklər, ləğvlər, qaytarmalar", href: "/dashboard/store/reports/senedler", icon: BarChart3, permission: "StoreReport.View", storeOnly: true },
    ],
  },
  {
    title: "MAIN MENU",
    items: [
      {
        title: "Dashboard",
        href: "/dashboard",
        icon: LayoutDashboard,
      },
      {
        title: "Analitika",
        href: "/dashboard/analytics",
        icon: BarChart3,
        permission: "Analytics.View",
      },
      {
        title: "Satış Sənədləri",
        href: "/dashboard/sales-documents",
        hiddenForStoreMode: true,
        icon: FileText,
        permission: "Orders.View",
      },
      {
        title: "Hesabatlar",
        href: "/dashboard/reports",
        icon: Calculator,
        permission: "Pos.ZReport",
      },
      {
        title: "Kassa",
        href: "/dashboard/kassa",
        icon: Wallet,
        permission: "CashRegister.View",
      },
      {
        title: "Maliyyə",
        href: "/dashboard/finance",
        icon: TrendingUp,
        permission: "Finance.View",
      },
      {
        title: "Şirkət depoziti",
        href: "/dashboard/company-deposit",
        icon: Landmark,
        permission: "CompanyDeposit.View",
      },
      {
        title: "Food Cost",
        href: "/dashboard/food-cost",
        hiddenForStoreMode: true,
        icon: Calculator,
        permission: "Analytics.View",
      },
    ],
  },
  {
    title: "HR MANAGEMENT",
    items: [
      {
        title: "Companies",
        href: "/dashboard/companies",
        icon: Building2,
        permission: "Company.View",
      },
      {
        title: "Rejimlər",
        href: "/dashboard/modes",
        icon: ToggleRight,
        permission: "Company.View",
        superAdminOnly: true,
      },
      {
        title: "Departments",
        href: "/dashboard/departments",
        icon: Building2,
        permission: "Department.View",
      },
      {
        title: "Positions",
        href: "/dashboard/positions",
        icon: Briefcase,
        permission: "Position.View",
      },
      {
        title: "Employees",
        href: "/dashboard/employees",
        icon: Users,
        permission: "Employee.View",
      },
    ],
  },
  {
    title: "INVENTORY & OPERATIONS",
    items: [
      {
        title: "Stock Categories",
        href: "/dashboard/stock-categories",
        hiddenForStoreMode: true,
        icon: Layers,
        permission: "StockCategory.View",
        module: "moduleAnbar",
      },
      {
        title: "Stock Items",
        href: "/dashboard/stock-items",
        hiddenForStoreMode: true,
        icon: Boxes,
        permission: "StockItem.View",
        module: "moduleAnbar",
      },
      {
        title: "Warehouses",
        href: "/dashboard/warehouses",
        icon: Warehouse,
        permission: "Warehouse.View",
        module: "moduleAnbar",
      },
      {
        title: "Stock entry documents",
        href: "/dashboard/warehouse-stock-documents",
        hiddenForStoreMode: true,
        icon: FileText,
        module: "moduleAnbar",
      },
      {
        title: "Warehouse stock balances",
        href: "/dashboard/warehouse-stocks",
        hiddenForStoreMode: true,
        icon: ClipboardList,
        permission: "WarehouseStock.View",
        module: "moduleAnbar",
      },
      {
        title: "Stock movements",
        href: "/dashboard/stock-movements",
        hiddenForStoreMode: true,
        icon: History,
        module: "moduleAnbar",
      },
      {
        title: "Stock Requests",
        href: "/dashboard/stock-requests",
        hiddenForStoreMode: true,
        icon: Package,
        permission: "StockRequest.View",
        module: "moduleAnbar",
      },
      {
        title: "Stock Purchases",
        href: "/dashboard/stock-purchases",
        hiddenForStoreMode: true,
        icon: ShoppingCart,
        permission: "StockPurchase.View",
        module: "moduleAnbar",
      },
      {
        title: "Mal alışı xərcləri",
        href: "/dashboard/stock-purchase-costs",
        hiddenForStoreMode: true,
        icon: Calculator,
        permission: "StockPurchase.View",
        module: "moduleAnbar",
      },
      {
        title: "Stok tənzimləmə",
        href: "/dashboard/stock-writeoffs",
        hiddenForStoreMode: true,
        icon: Package,
        permission: "StockWriteOff.View",
        module: "moduleAnbar",
      },
      {
        title: "Konturagentlər",
        href: "/dashboard/counterparties",
        icon: Building2,
        permission: "Counterparty.View",
        module: "moduleAnbar",
      },
    ],
  },
  {
    title: "ORDERS & TABLES",
    storeModeTitle: "ORDERS",
    items: [
      {
        title: "Table Map",
        href: "/dashboard/table-map",
        icon: LayoutGrid,
        permission: "RestaurantTable.View",
        module: "moduleMasaBolge",
        hiddenForStoreMode: true,
      },
      {
        title: "Rezervasiyalar",
        href: "/dashboard/reservations",
        icon: CalendarCheck,
        permission: "Reservation.View",
        module: "moduleRezervasyon",
        hiddenForStoreMode: true,
      },
      {
        title: "Endirimlər",
        href: "/dashboard/discounts",
        hiddenForStoreMode: true,
        icon: Tag,
        permission: "Discount.View",
      },
      {
        title: "Zal Dizaynı",
        href: "/dashboard/floor-plan",
        icon: PenTool,
        permission: "RestaurantTable.View",
        module: "moduleMasaBolge",
        hiddenForStoreMode: true,
      },
      {
        title: "Orders",
        href: "/dashboard/orders",
        hiddenForStoreMode: true,
        icon: ShoppingCart,
        permission: "Orders.View",
      },
      {
        title: "Kitchen",
        href: "/dashboard/kitchen",
        icon: ChefHat,
        permission: "Kitchen.View",
        hiddenForStoreMode: true,
      },
    ],
  },
  {
    title: "BRANCHES & MENU",
    storeModeTitle: "BRANCHES & PRODUCTS",
    items: [
      {
        title: "Branches",
        href: "/dashboard/restaurants",
        icon: Building2,
        permission: "Restaurant.View",
        module: "moduleFilial",
      },
      {
        title: "QR Menyu",
        href: "/dashboard/qr-menu",
        hiddenForStoreMode: true,
        icon: QrCode,
        permission: "Restaurant.View",
      },
      {
        title: "Branch Tables",
        href: "/dashboard/restaurant-tables",
        icon: UtensilsCrossed,
        permission: "RestaurantTable.View",
        module: "moduleMasaBolge",
        hiddenForStoreMode: true,
      },
      {
        title: "Kabinetlər",
        href: "/dashboard/cabinet-stations",
        icon: Gamepad2,
        permission: "RestaurantTable.View",
        module: "moduleMasaBolge",
        hiddenForStoreMode: true,
      },
      {
        title: "Menu Categories",
        storeModeTitle: "Məhsul Kateqoriyaları",
        href: "/dashboard/menu-categories",
        hiddenForStoreMode: true,
        icon: Wine,
        permission: "MenuCategory.View",
      },
      {
        title: "Menu Items",
        storeModeTitle: "Məhsullar",
        href: "/dashboard/menu-items",
        hiddenForStoreMode: true,
        icon: ShoppingCart,
        permission: "MenuItem.View",
      },
      {
        title: "Menu Item Recipes",
        href: "/dashboard/menu-item-recipes",
        hiddenForStoreMode: true,
        icon: FileText,
        permission: "Permissions.MenuItemRecipe.View",
      },
    ],
  },
  {
    title: "SYSTEM",
    items: [
      {
        title: "Tənzimləmələr",
        href: "/dashboard/settings",
        icon: Settings,
        permission: "CompanySettings.View",
      },
      {
        title: "Users",
        href: "/dashboard/users",
        icon: Users,
        permission: "User.View",
      },
      {
        title: "Roles",
        href: "/dashboard/roles",
        icon: Shield,
        permission: "Role.View",
      },
      {
        title: "User Roles",
        href: "/dashboard/user-roles",
        icon: UserCog,
        permission: "UserRole.Manage",
      },
      {
        title: "Role Permissions",
        href: "/dashboard/role-permissions",
        icon: ShieldCheck,
        permission: "UserRole.Manage",
      },
      {
        title: "Printerlər",
        href: "/dashboard/printers",
        icon: PrinterIcon,
        permission: "Printer.View",
      },
      {
        title: "Fiskal Kassalar",
        href: "/dashboard/fiscal-devices",
        icon: Landmark,
        permission: "FiscalDevice.View",
      },
      {
        title: "POS terminalları",
        href: "/dashboard/workstations",
        icon: Monitor,
        permission: "Workstation.View",
      },
      {
        title: "Tərəzilər",
        href: "/dashboard/scale-devices",
        icon: Scale,
        permission: "ScaleDevice.View",
      },
      {
        title: "Çatdırılma İnteqrasiyaları",
        href: "/dashboard/delivery-integrations",
        hiddenForStoreMode: true,
        icon: Truck,
        permission: "DeliveryIntegration.View",
      },
      {
        title: "Filial Bölmələri",
        href: "/dashboard/restaurant-sections",
        icon: LayoutTemplate,
        permission: "RestaurantSection.View",
        hiddenForStoreMode: true,
      },
      {
        title: "Notifications",
        href: "/dashboard/notifications",
        icon: Bell,
      },
      {
        title: "Audit Logs",
        href: "/dashboard/audit-logs",
        icon: FileText,
        permission: "Permissions.AuditLog.View",
      },
    ],
  },
];

export const statusColors: Record<string, string> = {
  active: "bg-emerald-100 text-emerald-800",
  inactive: "bg-slate-100 text-slate-800",
  pending: "bg-amber-100 text-amber-800",
  open: "bg-blue-100 text-blue-800",
  on_leave: "bg-orange-100 text-orange-800",
  draft: "bg-gray-100 text-gray-800",
  submitted: "bg-blue-100 text-blue-800",
  approved: "bg-emerald-100 text-emerald-800",
  rejected: "bg-red-100 text-red-800",
  cancelled: "bg-slate-100 text-slate-800",
  dispatched: "bg-purple-100 text-purple-800",
  received: "bg-green-100 text-green-800",
  in_progress: "bg-blue-100 text-blue-800",
  completed: "bg-green-100 text-green-800",
  available: "bg-emerald-100 text-emerald-800",
  occupied: "bg-orange-100 text-orange-800",
  reserved: "bg-blue-100 text-blue-800",
  maintenance: "bg-amber-100 text-amber-800",
};

export const levelColors: Record<string, string> = {
  junior: "bg-blue-100 text-blue-800",
  mid: "bg-purple-100 text-purple-800",
  senior: "bg-emerald-100 text-emerald-800",
};
