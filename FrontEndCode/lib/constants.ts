import {
  LayoutDashboard,
  Building2,
  Briefcase,
  Users,
  Package,
  Truck,
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
} from "lucide-react";

export interface NavItem {
  title: string;
  href: string;
  icon: LucideIcon;
  badge?: string;
  permission?: string;
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
    ],
  },
  {
    title: "HR MANAGEMENT",
    items: [
      {
        title: "Companies",
        href: "/dashboard/companies",
        icon: Building2,
      },
      {
        title: "Departments",
        href: "/dashboard/departments",
        icon: Building2,
      },
      {
        title: "Positions",
        href: "/dashboard/positions",
        icon: Briefcase,
      },
      {
        title: "Employees",
        href: "/dashboard/employees",
        icon: Users,
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
      },
      {
        title: "Stock Items",
        href: "/dashboard/stock-items",
        icon: Boxes,
      },
      {
        title: "Warehouses",
        href: "/dashboard/warehouses",
        icon: Warehouse,
      },
      {
        title: "Stock entry documents",
        href: "/dashboard/warehouse-stock-documents",
        icon: FileText,
      },
      {
        title: "Warehouse stock balances",
        href: "/dashboard/warehouse-stocks",
        icon: ClipboardList,
      },
      {
        title: "Stock movements",
        href: "/dashboard/stock-movements",
        icon: History,
      },
      {
        title: "Stock Requests",
        href: "/dashboard/stock-requests",
        icon: Package,
      },
      {
        title: "Warehouse Transfers",
        href: "/dashboard/warehouse-transfers",
        icon: Truck,
      },
    ],
  },
  {
    title: "ORDERS",
    items: [
      {
        title: "Orders",
        href: "/dashboard/orders",
        icon: ShoppingCart,
      },
      {
        title: "Kitchen",
        href: "/dashboard/kitchen",
        icon: ChefHat,
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
      },
      {
        title: "Restaurant Tables",
        href: "/dashboard/restaurant-tables",
        icon: UtensilsCrossed,
      },
      {
        title: "Menu Categories",
        href: "/dashboard/menu-categories",
        icon: Wine,
      },
      {
        title: "Menu Items",
        href: "/dashboard/menu-items",
        icon: ShoppingCart,
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
        title: "Users",
        href: "/dashboard/users",
        icon: Users,
      },
      {
        title: "Roles",
        href: "/dashboard/roles",
        icon: Shield,
      },
      {
        title: "User Roles",
        href: "/dashboard/user-roles",
        icon: UserCog,
      },
      {
        title: "Role Permissions",
        href: "/dashboard/role-permissions",
        icon: ShieldCheck,
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
