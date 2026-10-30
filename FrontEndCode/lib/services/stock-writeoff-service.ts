import { api } from "@/lib/api";
import { assertApiSuccess } from "@/lib/api-base-response";

export const StockWriteOffReason = { Waste: 1, Spoiled: 2, Broken: 3, Expired: 4, Other: 5 } as const;
export type StockWriteOffReasonValue = (typeof StockWriteOffReason)[keyof typeof StockWriteOffReason];

export const WRITE_OFF_REASON_LABELS: Record<StockWriteOffReasonValue, string> = {
  [StockWriteOffReason.Waste]: "İstifadəyə yararsız",
  [StockWriteOffReason.Spoiled]: "Xarab olub",
  [StockWriteOffReason.Broken]: "Sınıb",
  [StockWriteOffReason.Expired]: "Müddəti bitib",
  [StockWriteOffReason.Other]: "Digər",
};

export type StockWriteOff = {
  id: number;
  writtenOffAtUtc: string;
  writtenOffByUserName: string | null;
  warehouseId: number;
  warehouseName: string;
  stockItemId: number;
  stockItemName: string;
  quantity: number;
  unitCostAzn: number;
  /** Quantity times unit cost: what the loss is worth (AZN). */
  lossAzn: number;
  reason: StockWriteOffReasonValue;
  note: string | null;
  stockPurchaseId: number | null;
  purchaseDocumentNo: string | null;
};

export type StockWriteOffList = { totalLossAzn: number; items: StockWriteOff[] };

export type CreateStockWriteOffPayload = {
  warehouseId: number;
  stockItemId: number;
  quantity: number;
  reason: StockWriteOffReasonValue;
  note?: string | null;
  stockPurchaseId?: number | null;
};

export async function getStockWriteOffs(params: {
  from?: string;
  to?: string;
  warehouseId?: number | null;
}): Promise<StockWriteOffList> {
  const res = await api.get<StockWriteOffList>("/stockwriteoffs", {
    params: { from: params.from || undefined, to: params.to || undefined, warehouseId: params.warehouseId || undefined },
  });
  return res.data;
}

/** Writes goods off: the warehouse stock drops, a purchase's debt does not change. */
export async function createStockWriteOff(payload: CreateStockWriteOffPayload): Promise<string | null> {
  const res = await api.post("/stockwriteoffs", payload);
  assertApiSuccess(res.data);
  return (res.data as { message?: string } | undefined)?.message ?? null;
}
