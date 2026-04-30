import { api } from "@/lib/api";
import { assertApiSuccess, readBaseResponseData, readBaseResponseList } from "@/lib/api-base-response";
import { ApiFormError, toApiFormError } from "@/lib/api-error";

/** Domain.Enums.StockRequestStatus */
export const StockRequestStatus = {
  Draft: 0,
  Submitted: 1,
  Approved: 2,
  Rejected: 3,
  Cancelled: 4,
  Fulfilled: 5,
} as const;
export type StockRequestStatusValue = (typeof StockRequestStatus)[keyof typeof StockRequestStatus];

/** Mirrors `StockRequestLineResponse` from the API. */
export type StockRequestLineDto = {
  id: number;
  stockItemId: number;
  stockItemName: string;
  quantity: number;
};

/**
 * Normalized from `StockRequestResponse` (GET single/list).
 * Company name is not returned — resolve via `/companies` or `GET /companies/{id}`.
 */
export type StockRequestDto = {
  id: number;
  companyId: number;
  requestingWarehouseId: number;
  requestingWarehouseName: string;
  supplyingWarehouseId: number;
  supplyingWarehouseName: string;
  status: StockRequestStatusValue;
  note: string | null;
  createdAtUtc: string | null;
  lines: StockRequestLineDto[];
};

export type CreateStockRequestPayload = {
  companyId: number;
  requestingWarehouseId: number;
  supplyingWarehouseId: number;
  note?: string | null;
  lines: { stockItemId: number; quantity: number }[];
};

export type UpdateStockRequestPayload = {
  requestingWarehouseId: number;
  supplyingWarehouseId: number;
  note?: string | null;
  lines: { id?: number | null; stockItemId: number; quantity: number }[];
};

function pick<T>(raw: Record<string, unknown>, camel: string, pascal: string): T | undefined {
  if (camel in raw) return raw[camel] as T;
  if (pascal in raw) return raw[pascal] as T;
  return undefined;
}

function normalizeLine(raw: unknown): StockRequestLineDto | null {
  if (!raw || typeof raw !== "object") return null;
  const o = raw as Record<string, unknown>;
  const id = Number(pick(o, "id", "Id"));
  const stockItemId = Number(pick(o, "stockItemId", "StockItemId"));
  if (!Number.isFinite(id) || !Number.isFinite(stockItemId)) return null;
  return {
    id,
    stockItemId,
    stockItemName: String(pick(o, "stockItemName", "StockItemName") ?? ""),
    quantity: Number(pick(o, "quantity", "Quantity") ?? 0),
  };
}

export function normalizeStockRequest(raw: unknown): StockRequestDto | null {
  if (!raw || typeof raw !== "object") return null;
  const o = raw as Record<string, unknown>;
  const id = Number(pick(o, "id", "Id"));
  if (!Number.isFinite(id)) return null;
  const linesRaw = pick<unknown[]>(o, "lines", "Lines") ?? [];
  return {
    id,
    companyId: Number(pick(o, "companyId", "CompanyId") ?? 0),
    requestingWarehouseId: Number(pick(o, "requestingWarehouseId", "RequestingWarehouseId") ?? 0),
    requestingWarehouseName: String(pick(o, "requestingWarehouseName", "RequestingWarehouseName") ?? ""),
    supplyingWarehouseId: Number(pick(o, "supplyingWarehouseId", "SupplyingWarehouseId") ?? 0),
    supplyingWarehouseName: String(pick(o, "supplyingWarehouseName", "SupplyingWarehouseName") ?? ""),
    status: Number(pick(o, "status", "Status") ?? 0) as StockRequestStatusValue,
    note:
      pick<string | null>(o, "note", "Note") === undefined
        ? null
        : (pick(o, "note", "Note") as string | null),
    createdAtUtc:
      pick<string | null>(o, "createdAtUtc", "CreatedAtUtc") === undefined
        ? null
        : (pick(o, "createdAtUtc", "CreatedAtUtc") as string | null),
    lines: linesRaw.map(normalizeLine).filter((x): x is StockRequestLineDto => x !== null),
  };
}

export function stockRequestStatusLabel(status: StockRequestStatusValue): string {
  switch (status) {
    case StockRequestStatus.Draft:
      return "Draft";
    case StockRequestStatus.Submitted:
      return "Submitted";
    case StockRequestStatus.Approved:
      return "Approved";
    case StockRequestStatus.Rejected:
      return "Rejected";
    case StockRequestStatus.Cancelled:
      return "Cancelled";
    case StockRequestStatus.Fulfilled:
      return "Fulfilled";
    default:
      return String(status);
  }
}

export async function getStockRequests(): Promise<StockRequestDto[]> {
  try {
    const response = await api.get<unknown>("/StockRequests");
    assertApiSuccess(response.data);
    const list = readBaseResponseList<unknown>(response.data);
    return list.map(normalizeStockRequest).filter((x): x is StockRequestDto => x !== null);
  } catch (error) {
    throw toApiFormError(error, "Failed to fetch stock requests");
  }
}

export async function getStockRequestById(id: number): Promise<StockRequestDto> {
  try {
    const response = await api.get<unknown>(`/StockRequests/${id}`);
    assertApiSuccess(response.data);
    const data = readBaseResponseData<unknown>(response.data);
    const row = normalizeStockRequest(data);
    if (!row) throw new ApiFormError("Stock request not found");
    return row;
  } catch (error) {
    throw toApiFormError(error, "Failed to load stock request");
  }
}

export async function createStockRequest(payload: CreateStockRequestPayload): Promise<number> {
  try {
    const response = await api.post<unknown>("/StockRequests", {
      companyId: payload.companyId,
      requestingWarehouseId: payload.requestingWarehouseId,
      supplyingWarehouseId: payload.supplyingWarehouseId,
      note: payload.note ?? null,
      lines: payload.lines.map((l) => ({ stockItemId: l.stockItemId, quantity: l.quantity })),
    });
    assertApiSuccess(response.data);
    const id = readBaseResponseData<number>(response.data);
    if (id == null || !Number.isFinite(Number(id))) {
      throw new ApiFormError("Invalid create response");
    }
    return Number(id);
  } catch (error) {
    throw toApiFormError(error, "Failed to create stock request");
  }
}

export async function updateStockRequest(id: number, payload: UpdateStockRequestPayload): Promise<void> {
  try {
    const response = await api.put<unknown>(`/StockRequests/${id}`, {
      requestingWarehouseId: payload.requestingWarehouseId,
      supplyingWarehouseId: payload.supplyingWarehouseId,
      note: payload.note ?? null,
      lines: payload.lines.map((l) => ({
        id: l.id ?? null,
        stockItemId: l.stockItemId,
        quantity: l.quantity,
      })),
    });
    assertApiSuccess(response.data);
  } catch (error) {
    throw toApiFormError(error, "Failed to update stock request");
  }
}

export async function deleteStockRequest(id: number): Promise<void> {
  try {
    const response = await api.delete<unknown>(`/StockRequests/${id}`);
    assertApiSuccess(response.data);
  } catch (error) {
    throw toApiFormError(error, "Failed to delete stock request");
  }
}

export async function submitStockRequest(id: number): Promise<void> {
  try {
    const response = await api.post<unknown>(`/StockRequests/${id}/submit`);
    assertApiSuccess(response.data);
  } catch (error) {
    throw toApiFormError(error, "Failed to submit stock request");
  }
}

export async function approveStockRequest(id: number): Promise<void> {
  try {
    const response = await api.post<unknown>(`/StockRequests/${id}/approve`);
    assertApiSuccess(response.data);
  } catch (error) {
    throw toApiFormError(error, "Failed to approve stock request");
  }
}

export async function rejectStockRequest(id: number): Promise<void> {
  try {
    const response = await api.post<unknown>(`/StockRequests/${id}/reject`);
    assertApiSuccess(response.data);
  } catch (error) {
    throw toApiFormError(error, "Failed to reject stock request");
  }
}
