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
