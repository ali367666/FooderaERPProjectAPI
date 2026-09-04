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
  FileSpreadsheet,
  Settings,
  Printer as PrinterIcon,
  LayoutTemplate,
  Landmark,
  Scale,
} from "lucide-react";

export interface NavItem {
  title: string;
  href: string;
  icon: LucideIcon;
  badge?: string;
  permission?: string;
  /** Company-level module toggle key (from CompanySettingsBranding) required for this item to show. */
  module?: "moduleFilial" | "moduleAnbar" | "moduleRezervasyon" | "moduleMasaBolge";
}

export interface NavGroup {
  title: string;
  items: NavItem[];
}

export const navGroups: NavGroup[] = [
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
        title: "Food Cost",
        href: "/dashboard/food-cost",
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
        icon: Layers,
        permission: "StockCategory.View",
        module: "moduleAnbar",
      },
      {
        title: "Stock Items",
        href: "/dashboard/stock-items",
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
        icon: FileText,
        module: "moduleAnbar",
      },
      {
        title: "Warehouse stock balances",
        href: "/dashboard/warehouse-stocks",
        icon: ClipboardList,
        permission: "WarehouseStock.View",
        module: "moduleAnbar",
      },
      {
        title: "Stock movements",
        href: "/dashboard/stock-movements",
        icon: History,
        module: "moduleAnbar",
      },
      {
        title: "Stock Requests",
        href: "/dashboard/stock-requests",
        icon: Package,
        permission: "StockRequest.View",
        module: "moduleAnbar",
      },
      {
        title: "Stock Purchases",
        href: "/dashboard/stock-purchases",
        icon: ShoppingCart,
        permission: "StockPurchase.View",
        module: "moduleAnbar",
      },
      {
        title: "BSC İnvoice",
        href: "/dashboard/bsc-invoices",
        icon: FileSpreadsheet,
        permission: "BscInvoice.View",
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
    items: [
      {
        title: "Table Map",
        href: "/dashboard/table-map",
        icon: LayoutGrid,
        permission: "RestaurantTable.View",
        module: "moduleMasaBolge",
      },
      {
        title: "Rezervasiyalar",
        href: "/dashboard/reservations",
        icon: CalendarCheck,
        permission: "Reservation.View",
        module: "moduleRezervasyon",
      },
      {
        title: "Endirimlər",
        href: "/dashboard/discounts",
        icon: Tag,
        permission: "Discount.View",
      },
      {
        title: "Zal Dizaynı",
        href: "/dashboard/floor-plan",
        icon: PenTool,
        permission: "RestaurantTable.View",
        module: "moduleMasaBolge",
      },
      {
        title: "Orders",
        href: "/dashboard/orders",
        icon: ShoppingCart,
        permission: "Orders.View",
      },
      {
        title: "Kitchen",
        href: "/dashboard/kitchen",
        icon: ChefHat,
        permission: "Kitchen.View",
      },
    ],
  },
  {
    title: "RESTAURANT & MENU",
    items: [
      {
        title: "Restaurants",
        href: "/dashboard/restaurants",
        icon: Building2,
        permission: "Restaurant.View",
        module: "moduleFilial",
      },
      {
        title: "Restaurant Tables",
        href: "/dashboard/restaurant-tables",
        icon: UtensilsCrossed,
        permission: "RestaurantTable.View",
        module: "moduleMasaBolge",
      },
      {
        title: "Menu Categories",
        href: "/dashboard/menu-categories",
        icon: Wine,
        permission: "MenuCategory.View",
      },
      {
        title: "Menu Items",
        href: "/dashboard/menu-items",
        icon: ShoppingCart,
        permission: "MenuItem.View",
      },
      {
        title: "Menu Item Recipes",
        href: "/dashboard/menu-item-recipes",
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
        title: "Tərəzilər",
        href: "/dashboard/scale-devices",
        icon: Scale,
        permission: "ScaleDevice.View",
      },
      {
        title: "Restoran Bölmələri",
        href: "/dashboard/restaurant-sections",
        icon: LayoutTemplate,
        permission: "RestaurantSection.View",
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
