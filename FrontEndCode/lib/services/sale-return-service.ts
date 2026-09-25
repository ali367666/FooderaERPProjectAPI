import { api } from "@/lib/api";
import { readBaseResponseData } from "@/lib/api-base-response";
import { toApiFormError } from "@/lib/api-error";

export type ReturnableOrderLine = {
  orderLineId: number;
  menuItemId: number;
  menuItemName: string;
  isWeightBased: boolean;
  quantity: number;
  returnedQuantity: number;
  returnableQuantity: number;
  lineTotal: number;
  refundUnitAmount: number;
};

export type SaleReturnLine = {
  orderLineId: number;
  menuItemName: string;
  quantity: number;
  amount: number;
};

export type SaleReturn = {
  id: number;
  returnNumber: string;
  orderId: number;
  orderNumber: string | null;
  paymentMethod: string;
  totalAmount: number;
  restockItems: boolean;
  reason: string | null;
  createdByUserName: string | null;
  createdAtUtc: string;
  lines: SaleReturnLine[];
};

export type ReturnableOrder = {
  orderId: number;
  orderNumber: string;
  receiptNumber: string | null;
  restaurantId: number;
  restaurantName: string | null;
  tableName: string | null;
  waiterName: string | null;
  counterpartyName: string | null;
  paidAt: string | null;
  paymentMethod: string | null;
  totalAmount: number;
  returnedAmount: number;
  lines: ReturnableOrderLine[];
  returns: SaleReturn[];
};

export type CreateSaleReturnInput = {
  orderId: number;
  reason: string | null;
  restockItems: boolean;
  lines: { orderLineId: number; quantity: number }[];
};

type Raw = Record<string, unknown>;

function pick<T>(o: Raw, camel: string, pascal: string): T | undefined {
  if (o[camel] !== undefined) return o[camel] as T;
  if (o[pascal] !== undefined) return o[pascal] as T;
  return undefined;
}

function unwrap(body: unknown): Raw | null {
  const data = readBaseResponseData<unknown>(body);
  const value = data ?? body;
  return value && typeof value === "object" ? (value as Raw) : null;
}

const str = (o: Raw, c: string, p: string) => String(pick(o, c, p) ?? "");
const strOrNull = (o: Raw, c: string, p: string) => {
  const v = pick<unknown>(o, c, p);
  return v == null ? null : String(v);
};
const num = (o: Raw, c: string, p: string) => Number(pick(o, c, p) ?? 0);

function normalizeReturn(raw: Raw): SaleReturn {
  const lines = (pick<unknown[]>(raw, "lines", "Lines") ?? []) as Raw[];
  return {
    id: num(raw, "id", "Id"),
    returnNumber: str(raw, "returnNumber", "ReturnNumber"),
    orderId: num(raw, "orderId", "OrderId"),
    orderNumber: strOrNull(raw, "orderNumber", "OrderNumber"),
    paymentMethod: str(raw, "paymentMethod", "PaymentMethod"),
    totalAmount: num(raw, "totalAmount", "TotalAmount"),
    restockItems: Boolean(pick(raw, "restockItems", "RestockItems") ?? false),
    reason: strOrNull(raw, "reason", "Reason"),
    createdByUserName: strOrNull(raw, "createdByUserName", "CreatedByUserName"),
    createdAtUtc: str(raw, "createdAtUtc", "CreatedAtUtc"),
    lines: lines.map((l) => ({
      orderLineId: num(l, "orderLineId", "OrderLineId"),
      menuItemName: str(l, "menuItemName", "MenuItemName"),
      quantity: num(l, "quantity", "Quantity"),
      amount: num(l, "amount", "Amount"),
    })),
  };
}

function normalizeOrder(raw: Raw): ReturnableOrder {
  const lines = (pick<unknown[]>(raw, "lines", "Lines") ?? []) as Raw[];
  const returns = (pick<unknown[]>(raw, "returns", "Returns") ?? []) as Raw[];
  return {
    orderId: num(raw, "orderId", "OrderId"),
    orderNumber: str(raw, "orderNumber", "OrderNumber"),
    receiptNumber: strOrNull(raw, "receiptNumber", "ReceiptNumber"),
    restaurantId: num(raw, "restaurantId", "RestaurantId"),
    restaurantName: strOrNull(raw, "restaurantName", "RestaurantName"),
    tableName: strOrNull(raw, "tableName", "TableName"),
    waiterName: strOrNull(raw, "waiterName", "WaiterName"),
    counterpartyName: strOrNull(raw, "counterpartyName", "CounterpartyName"),
    paidAt: strOrNull(raw, "paidAt", "PaidAt"),
    paymentMethod: strOrNull(raw, "paymentMethod", "PaymentMethod"),
    totalAmount: num(raw, "totalAmount", "TotalAmount"),
    returnedAmount: num(raw, "returnedAmount", "ReturnedAmount"),
    lines: lines.map((l) => ({
      orderLineId: num(l, "orderLineId", "OrderLineId"),
      menuItemId: num(l, "menuItemId", "MenuItemId"),
      menuItemName: str(l, "menuItemName", "MenuItemName"),
      isWeightBased: Boolean(pick(l, "isWeightBased", "IsWeightBased") ?? false),
      quantity: num(l, "quantity", "Quantity"),
      returnedQuantity: num(l, "returnedQuantity", "ReturnedQuantity"),
      returnableQuantity: num(l, "returnableQuantity", "ReturnableQuantity"),
      lineTotal: num(l, "lineTotal", "LineTotal"),
      refundUnitAmount: num(l, "refundUnitAmount", "RefundUnitAmount"),
    })),
    returns: returns.map(normalizeReturn),
  };
}

export async function findReturnableOrder(code: string): Promise<ReturnableOrder> {
  try {
    const response = await api.get<unknown>("/SaleReturns/lookup", { params: { code } });
    const raw = unwrap(response.data);
    if (!raw) throw new Error("Satış tapılmadı.");
    return normalizeOrder(raw);
  } catch (error) {
    throw toApiFormError(error, "Satış tapılmadı");
  }
}

export async function createSaleReturn(input: CreateSaleReturnInput): Promise<SaleReturn> {
  try {
    const response = await api.post<unknown>("/SaleReturns", input);
    const raw = unwrap(response.data);
    if (!raw) throw new Error("Qaytarma qeydə alınmadı.");
    return normalizeReturn(raw);
  } catch (error) {
    throw toApiFormError(error, "Qaytarma qeydə alınmadı");
  }
}

export async function getSaleReturns(restaurantId: number, from: Date, to: Date): Promise<SaleReturn[]> {
  try {
    const response = await api.get<unknown>("/SaleReturns", {
      params: { restaurantId, from: from.toISOString(), to: to.toISOString() },
    });
    const data = readBaseResponseData<unknown>(response.data) ?? response.data;
    return Array.isArray(data) ? (data as Raw[]).map(normalizeReturn) : [];
  } catch (error) {
    throw toApiFormError(error, "Qaytarmalar yüklənmədi");
  }
}
