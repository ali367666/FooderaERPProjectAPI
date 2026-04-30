import { api } from "@/lib/api";
import { assertApiSuccess, readBaseResponseData, readBaseResponseList } from "@/lib/api-base-response";
import { ApiFormError, toApiFormError } from "@/lib/api-error";

/** Domain.Enums.TransferStatus */
export const TransferStatus = {
  Draft: 0,
  Pending: 1,
  Approved: 2,
  InTransit: 3,
  Completed: 4,
  Rejected: 5,
  Cancelled: 6,
} as const;
export type TransferStatusValue = (typeof TransferStatus)[keyof typeof TransferStatus];

export type WarehouseTransferLineDto = {
  id: number;
  stockItemId: number;
  stockItemName: string;
  quantity: number;
};

export type WarehouseTransferDto = {
  id: number;
  companyId: number;
  stockRequestId: number | null;
  fromWarehouseId: number;
  fromWarehouseName: string;
  toWarehouseId: number;
  toWarehouseName: string;
  vehicleWarehouseId: number | null;
  vehicleWarehouseName: string | null;
  status: TransferStatusValue;
  note: string | null;
  createdAtUtc: string | null;
  transferDate: string;
  lines: WarehouseTransferLineDto[];
};

export type CreateWarehouseTransferPayload = {
  companyId: number;
  stockRequestId?: number | null;
  fromWarehouseId: number;
  toWarehouseId: number;
  vehicleWarehouseId?: number | null;
  note?: string | null;
  lines: { stockItemId: number; quantity: number }[];
};

export type UpdateWarehouseTransferPayload = {
  fromWarehouseId: number;
  toWarehouseId: number;
  vehicleWarehouseId?: number | null;
  note?: string | null;
  lines: { id?: number | null; stockItemId: number; quantity: number }[];
};

function pick<T>(raw: Record<string, unknown>, camel: string, pascal: string): T | undefined {
  if (camel in raw) return raw[camel] as T;
  if (pascal in raw) return raw[pascal] as T;
  return undefined;
}

function normalizeTransferLine(raw: unknown): WarehouseTransferLineDto | null {
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

function normalizeTransferDate(raw: unknown): string {
  if (raw == null) return "";
  const s = String(raw);
  return s;
}

export function normalizeWarehouseTransfer(raw: unknown): WarehouseTransferDto | null {
  if (!raw || typeof raw !== "object") return null;
  const o = raw as Record<string, unknown>;
  const id = Number(pick(o, "id", "Id"));
  if (!Number.isFinite(id)) return null;
  const linesRaw = pick<unknown[]>(o, "lines", "Lines") ?? [];
  const stockRequestRaw = pick(o, "stockRequestId", "StockRequestId");
  return {
    id,
    companyId: Number(pick(o, "companyId", "CompanyId") ?? 0),
    stockRequestId:
      stockRequestRaw === undefined || stockRequestRaw === null
        ? null
        : Number(stockRequestRaw) || null,
    fromWarehouseId: Number(pick(o, "fromWarehouseId", "FromWarehouseId") ?? 0),
    fromWarehouseName: String(pick(o, "fromWarehouseName", "FromWarehouseName") ?? ""),
    toWarehouseId: Number(pick(o, "toWarehouseId", "ToWarehouseId") ?? 0),
    toWarehouseName: String(pick(o, "toWarehouseName", "ToWarehouseName") ?? ""),
    vehicleWarehouseId: (() => {
      const v = pick<unknown>(o, "vehicleWarehouseId", "VehicleWarehouseId");
      if (v === undefined || v === null || v === "") return null;
      const n = Number(v);
      return Number.isFinite(n) && n > 0 ? n : null;
    })(),
    vehicleWarehouseName:
      pick(o, "vehicleWarehouseName", "VehicleWarehouseName") === undefined
        ? null
        : String(pick(o, "vehicleWarehouseName", "VehicleWarehouseName") ?? "") || null,
    status: Number(pick(o, "status", "Status") ?? 0) as TransferStatusValue,
    note:
      pick<string | null>(o, "note", "Note") === undefined
        ? null
        : (pick(o, "note", "Note") as string | null),
    createdAtUtc:
      pick<string | null>(o, "createdAtUtc", "CreatedAtUtc") === undefined
        ? null
        : (pick(o, "createdAtUtc", "CreatedAtUtc") as string | null),
    transferDate: normalizeTransferDate(pick(o, "transferDate", "TransferDate")),
    lines: linesRaw.map(normalizeTransferLine).filter((x): x is WarehouseTransferLineDto => x !== null),
  };
}

export function transferStatusLabel(status: TransferStatusValue): string {
  switch (status) {
    case TransferStatus.Draft:
      return "Draft";
    case TransferStatus.Pending:
      return "Pending";
    case TransferStatus.Approved:
      return "Approved";
    case TransferStatus.InTransit:
      return "In transit";
    case TransferStatus.Completed:
      return "Completed";
    case TransferStatus.Rejected:
      return "Rejected";
    case TransferStatus.Cancelled:
      return "Cancelled";
    default:
      return String(status);
  }
}

export async function getWarehouseTransfers(): Promise<WarehouseTransferDto[]> {
  try {
    const response = await api.get<unknown>("/WarehouseTransfers");
    assertApiSuccess(response.data);
    const list = readBaseResponseList<unknown>(response.data);
    return list.map(normalizeWarehouseTransfer).filter((x): x is WarehouseTransferDto => x !== null);
  } catch (error) {
    throw toApiFormError(error, "Failed to fetch warehouse transfers");
  }
}

export async function getWarehouseTransferById(id: number): Promise<WarehouseTransferDto> {
  try {
    const response = await api.get<unknown>(`/WarehouseTransfers/${id}`);
    assertApiSuccess(response.data);
    const data = readBaseResponseData<unknown>(response.data);
    const row = normalizeWarehouseTransfer(data);
    if (!row) throw new ApiFormError("Warehouse transfer not found");
    return row;
  } catch (error) {
    throw toApiFormError(error, "Failed to load warehouse transfer");
  }
}

export async function createWarehouseTransfer(
  payload: CreateWarehouseTransferPayload,
): Promise<number> {
  try {
    const response = await api.post<unknown>("/WarehouseTransfers", {
      companyId: payload.companyId,
      stockRequestId: payload.stockRequestId ?? null,
      fromWarehouseId: payload.fromWarehouseId,
      toWarehouseId: payload.toWarehouseId,
      vehicleWarehouseId: payload.vehicleWarehouseId ?? null,
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
    throw toApiFormError(error, "Failed to create warehouse transfer");
  }
}

export async function updateWarehouseTransfer(
  id: number,
  payload: UpdateWarehouseTransferPayload,
): Promise<void> {
  try {
    const response = await api.put<unknown>(`/WarehouseTransfers/${id}`, {
      fromWarehouseId: payload.fromWarehouseId,
      toWarehouseId: payload.toWarehouseId,
      vehicleWarehouseId: payload.vehicleWarehouseId ?? null,
      note: payload.note ?? null,
      lines: payload.lines.map((l) => ({
        id: l.id ?? null,
        stockItemId: l.stockItemId,
        quantity: l.quantity,
      })),
    });
    assertApiSuccess(response.data);
  } catch (error) {
    throw toApiFormError(error, "Failed to update warehouse transfer");
  }
}

export async function deleteWarehouseTransfer(id: number): Promise<void> {
  try {
    const response = await api.delete<unknown>(`/WarehouseTransfers/${id}`);
    assertApiSuccess(response.data);
  } catch (error) {
    throw toApiFormError(error, "Failed to delete warehouse transfer");
  }
}

export async function submitWarehouseTransfer(id: number): Promise<void> {
  try {
    const response = await api.post<unknown>(`/WarehouseTransfers/${id}/submit`);
    assertApiSuccess(response.data);
  } catch (error) {
    throw toApiFormError(error, "Failed to submit warehouse transfer");
  }
}

export async function approveWarehouseTransfer(id: number): Promise<void> {
  try {
    const response = await api.post<unknown>(`/WarehouseTransfers/${id}/approve`);
    assertApiSuccess(response.data);
  } catch (error) {
    throw toApiFormError(error, "Failed to approve warehouse transfer");
  }
}

export async function rejectWarehouseTransfer(id: number): Promise<void> {
  try {
    const response = await api.post<unknown>(`/WarehouseTransfers/${id}/reject`);
    assertApiSuccess(response.data);
  } catch (error) {
    throw toApiFormError(error, "Failed to reject warehouse transfer");
  }
}

export async function dispatchWarehouseTransfer(id: number): Promise<void> {
  try {
    const response = await api.post<unknown>(`/WarehouseTransfers/${id}/dispatch`);
    assertApiSuccess(response.data);
  } catch (error) {
    throw toApiFormError(error, "Failed to dispatch warehouse transfer");
  }
}

export async function receiveWarehouseTransfer(id: number): Promise<void> {
  try {
    const response = await api.post<unknown>(`/WarehouseTransfers/${id}/receive`);
    assertApiSuccess(response.data);
  } catch (error) {
    throw toApiFormError(error, "Failed to receive warehouse transfer");
  }
}

export async function cancelWarehouseTransfer(id: number): Promise<void> {
  try {
    const response = await api.post<unknown>(`/WarehouseTransfers/${id}/cancel`);
    assertApiSuccess(response.data);
  } catch (error) {
    throw toApiFormError(error, "Failed to cancel warehouse transfer");
  }
}
