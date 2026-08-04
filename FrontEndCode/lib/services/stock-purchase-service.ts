import { api } from "@/lib/api";
import { assertApiSuccess, readBaseResponseData, readBaseResponseList } from "@/lib/api-base-response";

export const PurchaseCurrency = {
  AZN: 0,
  USD: 1,
  EUR: 2,
  GBP: 3,
  TRY: 4,
  RUB: 5,
  CNY: 6,
} as const;
export type PurchaseCurrencyValue = (typeof PurchaseCurrency)[keyof typeof PurchaseCurrency];

export const CURRENCY_LABELS: Record<PurchaseCurrencyValue, string> = {
  [PurchaseCurrency.AZN]: "AZN",
  [PurchaseCurrency.USD]: "USD",
  [PurchaseCurrency.EUR]: "EUR",
  [PurchaseCurrency.GBP]: "GBP",
  [PurchaseCurrency.TRY]: "TRY",
  [PurchaseCurrency.RUB]: "RUB",
  [PurchaseCurrency.CNY]: "CNY",
};

export const StockPurchaseStatus = {
  Draft: 0,
  Pending: 1,
  Approved: 2,
  Completed: 3,
  Rejected: 4,
  Cancelled: 5,
} as const;
export type StockPurchaseStatusValue = (typeof StockPurchaseStatus)[keyof typeof StockPurchaseStatus];

export type StockPurchaseLineDto = {
  id: number;
  stockItemId: number;
  stockItemName: string;
  quantity: number;
  unitPriceForeign: number;
  unitPriceAzn: number;
  totalForeign: number;
  totalAzn: number;
};

export type StockPurchaseDto = {
  id: number;
  documentNo: string;
  companyId: number;
  supplierName: string;
  isImport: boolean;
  currency: PurchaseCurrencyValue;
  currencyCode: string;
  exchangeRate: number;
  purchaseDate: string;
  status: StockPurchaseStatusValue;
  warehouseId: number;
  warehouseName: string;
  note: string | null;
  createdAtUtc: string;
  totalForeign: number;
  totalAzn: number;
  lines: StockPurchaseLineDto[];
};

export type CreateStockPurchasePayload = {
  companyId: number;
  supplierName: string;
  isImport: boolean;
  currency: PurchaseCurrencyValue;
  exchangeRate: number;
  purchaseDate: string;
  warehouseId: number;
  note?: string | null;
  lines: { stockItemId: number; quantity: number; unitPriceForeign: number }[];
};

export type UpdateStockPurchasePayload = {
  supplierName: string;
  isImport: boolean;
  currency: PurchaseCurrencyValue;
  exchangeRate: number;
  purchaseDate: string;
  warehouseId: number;
  note?: string | null;
  lines: { id?: number | null; stockItemId: number; quantity: number; unitPriceForeign: number }[];
};

export async function getStockPurchases(): Promise<StockPurchaseDto[]> {
  const res = await api.get("/stockpurchases");
  return readBaseResponseList<StockPurchaseDto>(res.data);
}

export async function getStockPurchaseById(id: number): Promise<StockPurchaseDto> {
  const res = await api.get(`/stockpurchases/${id}`);
  return readBaseResponseData<StockPurchaseDto>(res.data);
}

export async function createStockPurchase(payload: CreateStockPurchasePayload): Promise<number> {
  const res = await api.post("/stockpurchases", payload);
  return readBaseResponseData<number>(res.data);
}

export async function updateStockPurchase(id: number, payload: UpdateStockPurchasePayload): Promise<void> {
  const res = await api.put(`/stockpurchases/${id}`, payload);
  assertApiSuccess(res.data);
}

export async function deleteStockPurchase(id: number): Promise<void> {
  const res = await api.delete(`/stockpurchases/${id}`);
  assertApiSuccess(res.data);
}

export async function submitStockPurchase(id: number): Promise<void> {
  const res = await api.post(`/stockpurchases/${id}/submit`);
  assertApiSuccess(res.data);
}

export async function approveStockPurchase(id: number): Promise<void> {
  const res = await api.post(`/stockpurchases/${id}/approve`);
  assertApiSuccess(res.data);
}

export async function rejectStockPurchase(id: number): Promise<void> {
  const res = await api.post(`/stockpurchases/${id}/reject`);
  assertApiSuccess(res.data);
}

export async function getCbarExchangeRate(currency: string, date: string): Promise<number> {
  const res = await api.get("/stockpurchases/exchange-rate", {
    params: { currency, date },
  });
  return readBaseResponseData<number>(res.data);
}
