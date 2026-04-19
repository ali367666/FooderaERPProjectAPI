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
} from "lucide-react";

export interface NavItem {
  title: string;
  href: string;
  icon: LucideIcon;
  badge?: string;
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
    ],
  },
  {
    title: "SYSTEM",
    items: [
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
