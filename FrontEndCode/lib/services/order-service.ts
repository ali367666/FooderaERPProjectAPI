import { api } from "@/lib/api";
import { assertApiSuccess, readBaseResponseData, readBaseResponseList } from "@/lib/api-base-response";
import { toApiFormError } from "@/lib/api-error";

export type OrderWorkflowStatus = "draft" | "open" | "in_preparation" | "ready" | "served" | "paid" | "cancelled";

export type OrderLineDto = {
  id: number;
  menuItemId: number;
  menuItemName: string;
  menuItemType: string;
  quantity: number;
  unitPrice: number;
  lineTotal: number;
  preparationType: string;
  status: string;
  note: string | null;
};

export type PaymentMethod = "Cash" | "Card";

export type OrderReceiptLineDto = {
  menuItemName: string;
  quantity: number;
  unitPrice: number;
  lineTotal: number;
};

export type OrderReceiptDto = {
  receiptNumber: string;
  orderNumber: string;
  restaurantName: string;
  tableName: string;
  waiterName: string;
  openedAt: string;
  paidAt: string | null;
  paymentMethod: string;
  lines: OrderReceiptLineDto[];
  totalAmount: number;
  paidAmount: number;
  changeAmount: number;
};

export type OrderDto = {
  id: number;
  companyId: number;
  orderNumber: string;
  restaurantId: number;
  restaurantName: string | null;
  tableId: number;
  tableName: string | null;
  waiterId: number;
  waiterName: string | null;
  statusRaw: string;
  status: OrderWorkflowStatus;
  note: string | null;
  openedAt: string;
  closedAt: string | null;
  totalAmount: number;
  processedByUserId: number | null;
  processedByUserName: string | null;
  processedAt: string | null;
  isPaid: boolean;
  paidAt: string | null;
  paymentMethod: string | null;
  paidAmount: number;
  changeAmount: number;
  receiptNumber: string | null;
  lines: OrderLineDto[];
};

export type CreateOrderPayload = {
  restaurantId: number;
  tableId: number;
  waiterId: number;
  note?: string | null;
};

export type AddOrderLinePayload = {
  orderId: number;
  menuItemId: number;
  quantity: number;
  note?: string | null;
};

export type UpdateOrderPayload = {
  id: number;
  restaurantId: number;
  tableId: number;
  waiterId: number;
  note?: string | null;
  status?: string | null;
};

export type UpdateOrderLinePayload = {
  id: number;
  quantity: number;
  note?: string | null;
  status?: string | null;
};

export function getOrderStatusValue(status: unknown): number {
  if (typeof status === "number") return status;
  if (typeof status === "string") {
    if (status === "Open") return 1;
    if (status === "InPreparation") return 2;
    if (status === "Ready") return 3;
    if (status === "Served") return 4;
    if (status === "Paid") return 5;
    if (status === "Cancelled") return 6;
    const normalized = status.trim().toLowerCase();
    if (normalized === "open") return 1;
    if (normalized === "inpreparation" || normalized === "in_preparation") return 2;
    if (normalized === "ready") return 3;
    if (normalized === "served") return 4;
    if (normalized === "paid" || normalized === "completed") return 5;
    if (normalized === "cancelled") return 6;
    const numeric = Number(status);
    if (Number.isFinite(numeric)) return numeric;
  }
  return 0;
}

export function isOrderPaid(order: Pick<OrderDto, "status" | "statusRaw" | "isPaid"> | null | undefined): boolean {
  if (!order) return false;
  const statusValue = getOrderStatusValue(order.statusRaw ?? order.status);
  return statusValue === 5 || order.isPaid === true;
}

function pick<T>(raw: Record<string, unknown>, camel: string, pascal: string): T | undefined {
  if (camel in raw) return raw[camel] as T;
  if (pascal in raw) return raw[pascal] as T;
  return undefined;
}

function normalizeOrderStatus(raw: string): OrderWorkflowStatus {
  switch ((raw || "").toLowerCase()) {
    case "draft":
      return "draft";
    case "open":
      return "open";
    case "inpreparation":
      return "in_preparation";
    case "ready":
      return "ready";
    case "served":
      return "served";
    case "paid":
    case "completed":
      return "paid";
    case "cancelled":
      return "cancelled";
    default:
      return "draft";
  }
}

function normalizeOrder(raw: unknown): OrderDto | null {
  if (!raw || typeof raw !== "object") return null;
  const o = raw as Record<string, unknown>;
  const id = Number(pick(o, "id", "Id"));
  if (!Number.isFinite(id)) return null;
  const statusRaw = String(pick(o, "status", "Status") ?? "");
  const rawLines = pick<unknown[]>(o, "lines", "Lines");
  const lines = Array.isArray(rawLines)
    ? rawLines
        .map((line) => {
          if (!line || typeof line !== "object") return null;
          const l = line as Record<string, unknown>;
          const lineId = Number(pick(l, "id", "Id"));
          if (!Number.isFinite(lineId)) return null;
          return {
            id: lineId,
            menuItemId: Number(pick(l, "menuItemId", "MenuItemId") ?? 0),
            menuItemName: String(pick(l, "menuItemName", "MenuItemName") ?? ""),
            menuItemType: String(pick(l, "menuItemType", "MenuItemType") ?? ""),
            quantity: Number(pick(l, "quantity", "Quantity") ?? 0),
            unitPrice: Number(pick(l, "unitPrice", "UnitPrice") ?? 0),
            lineTotal: Number(pick(l, "lineTotal", "LineTotal") ?? 0),
            preparationType: String(pick(l, "preparationType", "PreparationType") ?? ""),
            status: String(pick(l, "status", "Status") ?? ""),
            note: (pick(l, "note", "Note") as string | null | undefined) ?? null,
          } satisfies OrderLineDto;
        })
        .filter((line): line is OrderLineDto => line !== null)
    : [];

  return {
    id,
    companyId: Number(pick(o, "companyId", "CompanyId") ?? 0),
    orderNumber: String(pick(o, "orderNumber", "OrderNumber") ?? ""),
    restaurantId: Number(pick(o, "restaurantId", "RestaurantId") ?? 0),
    restaurantName: (pick(o, "restaurantName", "RestaurantName") as string | null | undefined) ?? null,
    tableId: Number(pick(o, "tableId", "TableId") ?? 0),
    tableName: (pick(o, "tableName", "TableName") as string | null | undefined) ?? null,
    waiterId: Number(pick(o, "waiterId", "WaiterId") ?? 0),
    waiterName: (pick(o, "waiterName", "WaiterName") as string | null | undefined) ?? null,
    statusRaw,
    status: normalizeOrderStatus(statusRaw),
    note: (pick(o, "note", "Note") as string | null | undefined) ?? null,
    openedAt: String(pick(o, "openedAt", "OpenedAt") ?? ""),
    closedAt: (pick(o, "closedAt", "ClosedAt") as string | null | undefined) ?? null,
    totalAmount: Number(pick(o, "totalAmount", "TotalAmount") ?? 0),
    processedByUserId: Number(pick(o, "processedByUserId", "ProcessedByUserId") ?? 0) || null,
    processedByUserName:
      (pick(o, "processedByUserName", "ProcessedByUserName") as string | null | undefined) ?? null,
    processedAt: (pick(o, "processedAt", "ProcessedAt") as string | null | undefined) ?? null,
    isPaid: Boolean(pick(o, "isPaid", "IsPaid") ?? false),
    paidAt: (pick(o, "paidAt", "PaidAt") as string | null | undefined) ?? null,
    paymentMethod: (pick(o, "paymentMethod", "PaymentMethod") as string | null | undefined) ?? null,
    paidAmount: Number(pick(o, "paidAmount", "PaidAmount") ?? 0),
    changeAmount: Number(pick(o, "changeAmount", "ChangeAmount") ?? 0),
    receiptNumber: (pick(o, "receiptNumber", "ReceiptNumber") as string | null | undefined) ?? null,
    lines,
  };
}

function normalizeReceipt(raw: unknown): OrderReceiptDto | null {
  if (!raw || typeof raw !== "object") return null;
  const o = raw as Record<string, unknown>;
  const rawLines = pick<unknown[]>(o, "lines", "Lines");
  return {
    receiptNumber: String(pick(o, "receiptNumber", "ReceiptNumber") ?? ""),
    orderNumber: String(pick(o, "orderNumber", "OrderNumber") ?? ""),
    restaurantName: String(pick(o, "restaurantName", "RestaurantName") ?? ""),
    tableName: String(pick(o, "tableName", "TableName") ?? ""),
    waiterName: String(pick(o, "waiterName", "WaiterName") ?? ""),
    openedAt: String(pick(o, "openedAt", "OpenedAt") ?? ""),
    paidAt: (pick(o, "paidAt", "PaidAt") as string | null | undefined) ?? null,
    paymentMethod: String(pick(o, "paymentMethod", "PaymentMethod") ?? ""),
    lines: Array.isArray(rawLines)
      ? rawLines
          .map((line) => {
            if (!line || typeof line !== "object") return null;
            const l = line as Record<string, unknown>;
            return {
              menuItemName: String(pick(l, "menuItemName", "MenuItemName") ?? ""),
              quantity: Number(pick(l, "quantity", "Quantity") ?? 0),
              unitPrice: Number(pick(l, "unitPrice", "UnitPrice") ?? 0),
              lineTotal: Number(pick(l, "lineTotal", "LineTotal") ?? 0),
            } satisfies OrderReceiptLineDto;
          })
          .filter((x): x is OrderReceiptLineDto => x !== null)
      : [],
    totalAmount: Number(pick(o, "totalAmount", "TotalAmount") ?? 0),
    paidAmount: Number(pick(o, "paidAmount", "PaidAmount") ?? 0),
    changeAmount: Number(pick(o, "changeAmount", "ChangeAmount") ?? 0),
  };
}

function unwrapList<T>(body: unknown): T[] {
  const list = readBaseResponseList<T>(body);
  if (list.length > 0) return list;
  if (Array.isArray(body)) return body as T[];
  return [];
}

function unwrapData<T>(body: unknown): T | null {
  const data = readBaseResponseData<T>(body);
  if (data != null) return data;
  if (body && typeof body === "object" && !("success" in (body as Record<string, unknown>))) {
    return body as T;
  }
  return null;
}

export async function getOrders(): Promise<OrderDto[]> {
  try {
    const response = await api.get<unknown>("/Orders");
    assertApiSuccess(response.data);
    return unwrapList<unknown>(response.data).map(normalizeOrder).filter((x): x is OrderDto => x !== null);
  } catch (error) {
    throw toApiFormError(error, "Failed to fetch orders");
  }
}

export async function createOrder(payload: CreateOrderPayload): Promise<OrderDto> {
  try {
    const response = await api.post<unknown>("/Orders", {
      restaurantId: payload.restaurantId,
      tableId: payload.tableId,
      waiterId: payload.waiterId,
      note: payload.note ?? null,
    });
    assertApiSuccess(response.data);
    const row = normalizeOrder(unwrapData<unknown>(response.data));
    if (!row) throw new Error("Invalid create order response");
    return row;
  } catch (error) {
    throw toApiFormError(error, "Failed to create order");
  }
}

export async function updateOrder(payload: UpdateOrderPayload): Promise<OrderDto> {
  try {
    const response = await api.put<unknown>("/Orders", {
      id: payload.id,
      restaurantId: payload.restaurantId,
      tableId: payload.tableId,
      waiterId: payload.waiterId,
      note: payload.note ?? null,
      status: payload.status ?? null,
    });
    assertApiSuccess(response.data);
    const row = normalizeOrder(unwrapData<unknown>(response.data));
    if (!row) throw new Error("Invalid update order response");
    return row;
  } catch (error) {
    throw toApiFormError(error, "Failed to update order");
  }
}

export async function getOrderById(id: number): Promise<OrderDto> {
  try {
    const response = await api.get<unknown>(`/Orders/${id}`);
    assertApiSuccess(response.data);
    const row = normalizeOrder(unwrapData<unknown>(response.data));
    if (!row) throw new Error("Invalid order response");
    return row;
  } catch (error) {
    throw toApiFormError(error, "Failed to load order");
  }
}

export async function addOrderLine(payload: AddOrderLinePayload): Promise<OrderDto> {
  try {
    const response = await api.post<unknown>("/Orders/lines", {
      orderId: payload.orderId,
      menuItemId: payload.menuItemId,
      quantity: payload.quantity,
      note: payload.note ?? null,
    });
    assertApiSuccess(response.data);
    const row = normalizeOrder(unwrapData<unknown>(response.data));
    if (!row) throw new Error("Invalid add line response");
    return row;
  } catch (error) {
    throw toApiFormError(error, "Failed to add order line");
  }
}

export async function updateOrderLine(payload: UpdateOrderLinePayload): Promise<OrderDto> {
  try {
    const response = await api.put<unknown>("/Orders/lines", {
      id: payload.id,
      quantity: payload.quantity,
      note: payload.note ?? null,
      status: payload.status ?? null,
    });
    assertApiSuccess(response.data);
    const row = normalizeOrder(unwrapData<unknown>(response.data));
    if (!row) throw new Error("Invalid update line response");
    return row;
  } catch (error) {
    throw toApiFormError(error, "Failed to update order line");
  }
}

export async function deleteOrderLine(id: number): Promise<OrderDto> {
  try {
    const response = await api.delete<unknown>(`/Orders/lines/${id}`);
    assertApiSuccess(response.data);
    const row = normalizeOrder(unwrapData<unknown>(response.data));
    if (!row) throw new Error("Invalid delete line response");
    return row;
  } catch (error) {
    throw toApiFormError(error, "Failed to delete order line");
  }
}

async function runWorkflow(
  id: number,
  action: "start" | "complete" | "cancel" | "submit",
): Promise<OrderDto> {
  try {
    const response = await api.post<unknown>(`/Orders/${id}/${action}`);
    assertApiSuccess(response.data);
    const row = normalizeOrder(unwrapData<unknown>(response.data));
    if (!row) {
      throw new Error("Invalid order response");
    }
    return row;
  } catch (error) {
    throw toApiFormError(error, `Failed to ${action} order`);
  }
}

export function startOrder(id: number): Promise<OrderDto> {
  return runWorkflow(id, "start");
}

export function completeOrder(id: number): Promise<OrderDto> {
  return runWorkflow(id, "complete");
}

export function cancelOrder(id: number): Promise<OrderDto> {
  return runWorkflow(id, "cancel");
}

export function submitOrder(id: number): Promise<OrderDto> {
  return runWorkflow(id, "submit");
}

export function serveOrder(id: number): Promise<void> {
  return (async () => {
    try {
      const response = await api.put<unknown>(`/Orders/${id}/serve`);
      const payload = response.data as { success?: boolean; message?: string } | undefined;
      if (payload?.success === false) {
        throw new Error(payload.message || "Failed to serve order");
      }
      // Treat any HTTP 200 with success !== false as successful for BaseResponse contracts.
      return;
    } catch (error) {
      throw toApiFormError(error, "Failed to serve order");
    }
  })();
}

export async function payOrder(id: number, payload: { paymentMethod: PaymentMethod; paidAmount: number }): Promise<OrderDto | null> {
  try {
    const response = await api.put<unknown>(`/Orders/${id}/pay`, payload);
    assertApiSuccess(response.data);
    const row = normalizeOrder(unwrapData<unknown>(response.data));
    if (!row) return null;
    return row;
  } catch (error) {
    throw toApiFormError(error, "Failed to pay order");
  }
}

export async function getOrderReceipt(id: number): Promise<OrderReceiptDto> {
  try {
    const response = await api.get<unknown>(`/Orders/${id}/receipt`);
    assertApiSuccess(response.data);
    const row = normalizeReceipt(unwrapData<unknown>(response.data));
    if (!row) throw new Error("Invalid receipt response");
    return row;
  } catch (error) {
    throw toApiFormError(error, "Failed to load receipt");
  }
}
