import { api } from "@/lib/api";
import { assertApiSuccess } from "@/lib/api-base-response";

export type StockAssetWarehouse = { warehouseId: number; warehouseName: string; quantity: number };

export type StockAsset = {
  stockItemId: number;
  name: string;
  barcode: string | null;
  categoryId: number;
  /** Domain.Enums.StockItemType */
  type: number;
  categoryName: string;
  unit: number;
  totalQuantity: number;
  averageCostAzn: number | null;
  /** Total quantity times the average cost (AZN). */
  valueAzn: number;
  warehouses: StockAssetWarehouse[];
};

export type StockAssetList = { totalValueAzn: number; items: StockAsset[] };

export type StockAssetOperation = {
  id: number;
  date: string;
  stockItemId: number;
  stockItemName: string;
  warehouseId: number;
  warehouseName: string;
  /** Alış, Artım, Sınma, Xərcə silinmə, ... */
  operation: string;
  isIncrease: boolean;
  quantity: number;
  documentNo: string | null;
  userName: string | null;
  note: string | null;
};

export const UNIT_SHORT: Record<number, string> = { 1: "ədəd", 2: "kq", 3: "q", 4: "litr", 5: "ml" };

/**
 * The company's non-food stock items (plates, glasses, cutlery, detergent...) with their stock and value; with `allTypes` every stock item
 * (the stock page).
 */
export async function getStockAssets(allTypes = false): Promise<StockAssetList> {
  const res = await api.get<StockAssetList>("/stockassets", { params: allTypes ? { allTypes: true } : undefined });
  return res.data;
}

export async function getStockAssetHistory(params: { from?: string; to?: string; stockItemId?: number | null }): Promise<StockAssetOperation[]> {
  const res = await api.get<StockAssetOperation[]>("/stockassets/history", {
    params: { from: params.from || undefined, to: params.to || undefined, stockItemId: params.stockItemId || undefined },
  });
  return res.data;
}

/** Raises the stock of an item by hand (a gift, a found item, a correction). */
export async function increaseStockAsset(payload: { warehouseId: number; stockItemId: number; quantity: number; note?: string | null }): Promise<string | null> {
  const res = await api.post("/stockassets/increase", payload);
  assertApiSuccess(res.data);
  return (res.data as { message?: string } | undefined)?.message ?? null;
}
