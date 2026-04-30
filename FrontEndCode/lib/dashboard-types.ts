export type DashboardPendingApproval = {
  id: string;
  type: "stock_request" | "warehouse_transfer" | "order";
  title: string;
  createdBy: string;
  status: "pending" | "approved" | "rejected";
  createdAt: string;
  items: string | null;
  quantity: number | null;
  actionable?: boolean;
};

export type DashboardInventoryAlert = {
  id: number;
  itemName: string;
  sku: string;
  currentStock: number;
  minimumStock: number;
  alertType: "critical" | "low_stock";
  unit: string;
};

export type DashboardRecentActivity = {
  id: string;
  type: "stock_movement" | "transfer" | "order_update" | "approval";
  description: string;
  details: string;
  user: string;
  timestamp: string;
  status: "completed" | "in_progress" | "pending";
};
