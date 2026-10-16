import { api } from "@/lib/api";
import { toApiFormError } from "@/lib/api-error";

export type DailyRevenue = {
  day: string;
  date: string;
  revenue: number;
  orderCount: number;
};

export type HourlySales = {
  hour: number;
  label: string;
  revenue: number;
  orderCount: number;
};

export type TopMenuItem = {
  menuItemId: number;
  name: string;
  totalQuantity: number;
  totalRevenue: number;
};

export type WaiterPerformance = {
  waiterId: number;
  waiterName: string;
  orderCount: number;
  totalRevenue: number;
  averageOrderValue: number;
};

export type RestaurantRevenue = {
  restaurantId: number;
  restaurantName: string;
  revenue: number;
  orderCount: number;
};

export type DashboardAnalytics = {
  todayRevenue: number;
  weekRevenue: number;
  monthRevenue: number;
  yearRevenue: number;
  todayOrderCount: number;
  monthOrderCount: number;
  averageOrderValue: number;
  totalActiveTables: number;
  currentlyOccupiedTables: number;
  dailyRevenue: DailyRevenue[];
  hourlySales: HourlySales[];
  topMenuItems: TopMenuItem[];
  waiterPerformance: WaiterPerformance[];
  restaurantRevenue: RestaurantRevenue[];
};

export async function getDashboardAnalytics(): Promise<DashboardAnalytics> {
  try {
    const res = await api.get<DashboardAnalytics>("/Analytics/dashboard");
    return res.data;
  } catch (e) {
    throw toApiFormError(e, "Analitika yüklənmədi");
  }
}

export type FoodCostLine = {
  stockItemId: number;
  stockItemName: string;
  quantityPerPortion: number;
  unit: string;
  unitCost: number;
  lineCost: number;
  missingCost: boolean;
};

export type FoodCostItem = {
  menuItemId: number;
  menuItemName: string;
  categoryName: string;
  sellingPrice: number;
  foodCost: number;
  foodCostPercentage: number;
  grossProfit: number;
  grossProfitMargin: number;
  hasRecipe: boolean;
  hasMissingCost: boolean;
  lines: FoodCostLine[];
};

export async function getFoodCost(): Promise<FoodCostItem[]> {
  try {
    const res = await api.get<FoodCostItem[]>("/Analytics/food-cost");
    return Array.isArray(res.data) ? res.data : [];
  } catch (e) {
    throw toApiFormError(e, "Food cost yüklənmədi");
  }
}

export type ZReportProductLine = {
  menuItemId: number;
  name: string;
  quantity: number;
  revenue: number;
  /** Share of product revenue in the period, 0–100. */
  percent?: number;
};

export type ZReportWaiterLine = {
  waiterId: number;
  waiterName: string;
  orderCount: number;
  revenue: number;
  /** Service charge collected on this waiter's orders — owed to the waiter. */
  serviceCharge?: number;
};

export type ZReportCategoryLine = {
  categoryId: number;
  categoryName: string;
  quantity: number;
  revenue: number;
  /** Share of category revenue in the period, 0–100. */
  percent?: number;
};

/** One paid order (one customer sitting). */
export type ZReportReceipt = {
  orderId: number;
  orderNumber: string;
  receiptNumber: string | null;
  tableId: number;
  tableName: string;
  waiterName: string;
  guestCount: number | null;
  openedAt: string;
  paidAt: string | null;
  paymentMethod: string | null;
  amount: number;
  discountAmount: number;
  serviceCharge: number;
};

export type ZReportCancellation = {
  orderId: number;
  cancelledAt: string;
  tableName: string;
  orderNumber: string;
  isWholeOrder: boolean;
  /** Removed product, or null when the whole order was cancelled. */
  menuItemName: string | null;
  quantity: number;
  amount: number;
  reason: string;
  note: string | null;
  beforeKitchen: boolean;
  cancelledBy: string;
};

export type ZReportGift = {
  orderId: number;
  orderNumber: string;
  receiptNumber: string | null;
  tableName: string;
  menuItemName: string;
  quantity: number;
  /** Menu value of the gifted items. */
  value: number;
  /** Null for gifts made before tracking started. */
  giftedAt: string | null;
  giftedBy: string;
};

export type ZReportTableLine = {
  tableId: number;
  tableName: string;
  orderCount: number;
  revenue: number;
  sessions: ZReportReceipt[];
};

export type ZReport = {
  from: string;
  to: string;
  totalRevenue: number;
  totalDiscount: number;
  cashTotal: number;
  cardTotal: number;
  orderCount: number;
  products: ZReportProductLine[];
  waiters: ZReportWaiterLine[];
  categories: ZReportCategoryLine[];
  tables?: ZReportTableLine[];
  receipts?: ZReportReceipt[];
  totalServiceCharge?: number;
  cancellations?: ZReportCancellation[];
  gifts?: ZReportGift[];
  returnCount?: number;
  totalReturns?: number;
  cashReturns?: number;
  cardReturns?: number;
  creditReturns?: number;
  netRevenue?: number;
  returnedProducts?: ZReportReturnLine[];
};

export type ZReportReturnLine = {
  menuItemId: number;
  name: string;
  quantity: number;
  amount: number;
};

export async function getZReport(from: Date, to: Date): Promise<ZReport> {
  try {
    const res = await api.get<ZReport>("/Analytics/z-report", {
      params: { from: from.toISOString(), to: to.toISOString() },
    });
    return res.data;
  } catch (e) {
    throw toApiFormError(e, "Z hesabatı yüklənmədi");
  }
}
