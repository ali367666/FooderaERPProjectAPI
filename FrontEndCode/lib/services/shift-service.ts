import { api } from "@/lib/api";
import { readBaseResponseData } from "@/lib/api-base-response";
import { toApiFormError } from "@/lib/api-error";

export type Shift = {
  id: number;
  restaurantId: number;
  openedByUserId: number;
  openedByUserName: string | null;
  openedAt: string;
  closedByUserId: number | null;
  closedAt: string | null;
  openingCashAmount: number;
  closingCashAmount: number | null;
  isOpen: boolean;
};

export type ZReportPaymentBreakdown = {
  paymentMethod: string;
  orderCount: number;
  totalAmount: number;
};

export type ZReport = {
  shiftId: number;
  restaurantId: number;
  openedAt: string;
  closedAt: string | null;
  openingCashAmount: number;
  closingCashAmount: number | null;
  orderCount: number;
  grossTotal: number;
  totalDiscount: number;
  totalServiceCharge: number;
  paymentBreakdown: ZReportPaymentBreakdown[];
};

function pick<T>(o: Record<string, unknown>, camel: string, pascal: string): T | undefined {
  if (o[camel] !== undefined) return o[camel] as T;
  if (o[pascal] !== undefined) return o[pascal] as T;
  return undefined;
}

function unwrapData<T>(body: unknown): T | null {
  const data = readBaseResponseData<T>(body);
  if (data != null) return data;
  if (body && typeof body === "object" && !("success" in (body as Record<string, unknown>))) {
    return body as T;
  }
  return null;
}

function normalizeShift(item: unknown): Shift | null {
  if (!item || typeof item !== "object") return null;
  const raw = item as Record<string, unknown>;
  const id = Number(pick(raw, "id", "Id"));
  if (!Number.isFinite(id) || id <= 0) return null;
  return {
    id,
    restaurantId: Number(pick(raw, "restaurantId", "RestaurantId") ?? 0),
    openedByUserId: Number(pick(raw, "openedByUserId", "OpenedByUserId") ?? 0),
    openedByUserName: (pick<string | null>(raw, "openedByUserName", "OpenedByUserName") ?? null) as string | null,
    openedAt: String(pick(raw, "openedAt", "OpenedAt") ?? ""),
    closedByUserId: (pick<number | null>(raw, "closedByUserId", "ClosedByUserId") ?? null) as number | null,
    closedAt: (pick<string | null>(raw, "closedAt", "ClosedAt") ?? null) as string | null,
    openingCashAmount: Number(pick(raw, "openingCashAmount", "OpeningCashAmount") ?? 0),
    closingCashAmount: (() => {
      const v = pick<number | null>(raw, "closingCashAmount", "ClosingCashAmount");
      return v == null ? null : Number(v);
    })(),
    isOpen: Boolean(pick(raw, "isOpen", "IsOpen") ?? false),
  };
}

function normalizeZReport(item: unknown): ZReport | null {
  if (!item || typeof item !== "object") return null;
  const raw = item as Record<string, unknown>;
  const breakdownRaw = pick<unknown[]>(raw, "paymentBreakdown", "PaymentBreakdown") ?? [];
  return {
    shiftId: Number(pick(raw, "shiftId", "ShiftId") ?? 0),
    restaurantId: Number(pick(raw, "restaurantId", "RestaurantId") ?? 0),
    openedAt: String(pick(raw, "openedAt", "OpenedAt") ?? ""),
    closedAt: (pick<string | null>(raw, "closedAt", "ClosedAt") ?? null) as string | null,
    openingCashAmount: Number(pick(raw, "openingCashAmount", "OpeningCashAmount") ?? 0),
    closingCashAmount: (() => {
      const v = pick<number | null>(raw, "closingCashAmount", "ClosingCashAmount");
      return v == null ? null : Number(v);
    })(),
    orderCount: Number(pick(raw, "orderCount", "OrderCount") ?? 0),
    grossTotal: Number(pick(raw, "grossTotal", "GrossTotal") ?? 0),
    totalDiscount: Number(pick(raw, "totalDiscount", "TotalDiscount") ?? 0),
    totalServiceCharge: Number(pick(raw, "totalServiceCharge", "TotalServiceCharge") ?? 0),
    paymentBreakdown: breakdownRaw.map((b) => {
      const r = b as Record<string, unknown>;
      return {
        paymentMethod: String(pick(r, "paymentMethod", "PaymentMethod") ?? ""),
        orderCount: Number(pick(r, "orderCount", "OrderCount") ?? 0),
        totalAmount: Number(pick(r, "totalAmount", "TotalAmount") ?? 0),
      };
    }),
  };
}

export async function getCurrentShift(restaurantId: number): Promise<Shift | null> {
  try {
    const response = await api.get<unknown>("/Shifts/current", { params: { restaurantId } });
    return normalizeShift(unwrapData<unknown>(response.data));
  } catch (error) {
    throw toApiFormError(error, "Failed to fetch current shift");
  }
}

export async function openShift(restaurantId: number, openingCashAmount: number): Promise<Shift> {
  try {
    const response = await api.post<unknown>("/Shifts/open", { restaurantId, openingCashAmount });
    const shift = normalizeShift(unwrapData<unknown>(response.data));
    if (!shift) throw new Error("Invalid response from server.");
    return shift;
  } catch (error) {
    throw toApiFormError(error, "Failed to open shift");
  }
}

export async function closeShift(shiftId: number, closingCashAmount: number): Promise<ZReport> {
  try {
    const response = await api.post<unknown>(`/Shifts/${shiftId}/close`, { closingCashAmount });
    const report = normalizeZReport(unwrapData<unknown>(response.data));
    if (!report) throw new Error("Invalid response from server.");
    return report;
  } catch (error) {
    throw toApiFormError(error, "Failed to close shift");
  }
}

export async function getZReport(shiftId: number): Promise<ZReport> {
  try {
    const response = await api.get<unknown>(`/Shifts/${shiftId}/z-report`);
    const report = normalizeZReport(unwrapData<unknown>(response.data));
    if (!report) throw new Error("Invalid response from server.");
    return report;
  } catch (error) {
    throw toApiFormError(error, "Failed to load Z-report");
  }
}
