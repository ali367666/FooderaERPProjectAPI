import { api } from "@/lib/api";
import { assertApiSuccess, readBaseResponseData, readBaseResponseList } from "@/lib/api-base-response";
import { ApiFormError, toApiFormError } from "@/lib/api-error";
import { resolveCompanyId } from "@/lib/resolve-company-id";
import { fetchListsPerCompany } from "@/lib/company-scope-utils";

/** Domain.Enums.WarehouseStockDocumentStatus */
export const WarehouseStockDocumentStatus = {
  Draft: 0,
  Approved: 1,
} as const;
export type WarehouseStockDocumentStatusValue =
  (typeof WarehouseStockDocumentStatus)[keyof typeof WarehouseStockDocumentStatus];

export type WarehouseStockDocumentLineInput = {
  stockItemId: number;
  quantity: number;
  unitId: number;
};

export type WarehouseStockDocumentSummary = {
  id: number;
  documentNo: string;
  warehouseId: number;
  warehouseName: string;
  companyId: number;
  createdAtUtc: string;
  status: WarehouseStockDocumentStatusValue;
  lineCount: number;
};

export type WarehouseStockDocumentLineDto = {
  id: number;
  stockItemId: number;
  stockItemName: string;
  quantity: number;
  unitId: number;
};

export type WarehouseStockDocumentDetail = {
  id: number;
  documentNo: string;
  warehouseId: number;
  warehouseName: string;
  companyId: number;
  createdAtUtc: string;
  status: WarehouseStockDocumentStatusValue;
  lines: WarehouseStockDocumentLineDto[];
};

function normalizeSummary(item: unknown): WarehouseStockDocumentSummary | null {
  if (!item || typeof item !== "object") return null;
  const raw = item as Record<string, unknown>;
  const id = Number(raw.id ?? raw.Id);
  if (!Number.isFinite(id) || id <= 0) return null;
  const created = raw.createdAtUtc ?? raw.CreatedAtUtc;
  const status = Number(raw.status ?? raw.Status ?? WarehouseStockDocumentStatus.Draft);
  return {
    id,
    documentNo: String(raw.documentNo ?? raw.DocumentNo ?? ""),
    warehouseId: Number(raw.warehouseId ?? raw.WarehouseId ?? 0),
    warehouseName: String(raw.warehouseName ?? raw.WarehouseName ?? ""),
    companyId: Number(raw.companyId ?? raw.CompanyId ?? 0),
    createdAtUtc: created instanceof Date ? created.toISOString() : String(created ?? ""),
    status: status as WarehouseStockDocumentStatusValue,
    lineCount: Number(raw.lineCount ?? raw.LineCount ?? 0),
  };
}

function normalizeDetail(data: unknown): WarehouseStockDocumentDetail | null {
  if (!data || typeof data !== "object") return null;
  const raw = data as Record<string, unknown>;
  const id = Number(raw.id ?? raw.Id);
  if (!Number.isFinite(id) || id <= 0) return null;
  const linesRaw = raw.lines ?? raw.Lines;
  const lines: WarehouseStockDocumentLineDto[] = Array.isArray(linesRaw)
    ? linesRaw
        .map((line) => {
          if (!line || typeof line !== "object") return null;
          const l = line as Record<string, unknown>;
          const lid = Number(l.id ?? l.Id);
          if (!Number.isFinite(lid)) return null;
          const unitId = Number(l.unitId ?? l.UnitId ?? l.unit ?? l.Unit ?? 0);
          return {
            id: lid,
            stockItemId: Number(l.stockItemId ?? l.StockItemId ?? 0),
            stockItemName: String(l.stockItemName ?? l.StockItemName ?? ""),
            quantity: Number(l.quantity ?? l.Quantity ?? 0),
            unitId,
          };
        })
        .filter((row): row is WarehouseStockDocumentLineDto => row !== null)
    : [];
  const created = raw.createdAtUtc ?? raw.CreatedAtUtc;
  const status = Number(raw.status ?? raw.Status ?? WarehouseStockDocumentStatus.Draft);
  return {
    id,
    documentNo: String(raw.documentNo ?? raw.DocumentNo ?? ""),
    warehouseId: Number(raw.warehouseId ?? raw.WarehouseId ?? 0),
    warehouseName: String(raw.warehouseName ?? raw.WarehouseName ?? ""),
    companyId: Number(raw.companyId ?? raw.CompanyId ?? 0),
    createdAtUtc: created instanceof Date ? created.toISOString() : String(created ?? ""),
    status: status as WarehouseStockDocumentStatusValue,
    lines,
  };
}

export function warehouseStockDocumentStatusLabel(s: WarehouseStockDocumentStatusValue): string {
  switch (s) {
    case WarehouseStockDocumentStatus.Draft:
      return "Draft";
    case WarehouseStockDocumentStatus.Approved:
      return "Approved";
    default:
      return String(s);
  }
}

export async function searchWarehouseStockDocuments(
  search?: string,
  companyId?: number,
): Promise<WarehouseStockDocumentSummary[]> {
  try {
    const cid = companyId ?? resolveCompanyId();
    const response = await api.get<unknown>("/WarehouseStock/search", {
      params: { companyId: cid, search: search?.trim() || undefined },
    });
    assertApiSuccess(response.data);
    const list = readBaseResponseList<unknown>(response.data);
    return list
      .map((row) => normalizeSummary(row))
      .filter((row): row is WarehouseStockDocumentSummary => row !== null);
  } catch (error) {
    throw toApiFormError(error, "Failed to fetch warehouse stock documents");
  }
}

export async function searchWarehouseStockDocumentsForAllCompanies(
  companyIds: number[],
  search?: string,
): Promise<WarehouseStockDocumentSummary[]> {
  return fetchListsPerCompany(companyIds, (id) => searchWarehouseStockDocuments(search, id));
}

export async function getWarehouseStockDocumentById(id: number): Promise<WarehouseStockDocumentDetail> {
  try {
    const response = await api.get<unknown>(`/WarehouseStock/${id}`);
    assertApiSuccess(response.data);
    const data = readBaseResponseData<unknown>(response.data);
    const row = normalizeDetail(data);
    if (!row) throw new ApiFormError("Warehouse stock document not found");
    return row;
  } catch (error) {
    throw toApiFormError(error, "Failed to load warehouse stock document");
  }
}

export async function createWarehouseStockDocument(payload: {
  warehouseId: number;
  lines: WarehouseStockDocumentLineInput[];
}): Promise<number> {
  try {
    const response = await api.post<unknown>("/WarehouseStock", {
      warehouseId: payload.warehouseId,
      lines: payload.lines.map((l) => ({
        stockItemId: l.stockItemId,
        quantity: l.quantity,
        unitId: l.unitId,
      })),
    });
    assertApiSuccess(response.data);
    const id = readBaseResponseData<number>(response.data);
    if (id == null || !Number.isFinite(Number(id))) {
      throw new ApiFormError("Invalid create response");
    }
    return Number(id);
  } catch (error) {
    throw toApiFormError(error, "Failed to create warehouse stock document");
  }
}

export async function updateWarehouseStockDocument(
  id: number,
  payload: { warehouseId: number; lines: WarehouseStockDocumentLineInput[] },
): Promise<void> {
  try {
    const response = await api.patch<unknown>(`/WarehouseStock/${id}`, {
      warehouseId: payload.warehouseId,
      lines: payload.lines.map((l) => ({
        stockItemId: l.stockItemId,
        quantity: l.quantity,
        unitId: l.unitId,
      })),
    });
    assertApiSuccess(response.data);
  } catch (error) {
    throw toApiFormError(error, "Failed to update warehouse stock document");
  }
}

export async function approveWarehouseStockDocument(id: number): Promise<void> {
  try {
    const response = await api.post<unknown>(`/WarehouseStock/${id}/approve`);
    assertApiSuccess(response.data);
  } catch (error) {
    throw toApiFormError(error, "Failed to approve warehouse stock document");
  }
}

export async function deleteWarehouseStockDocument(id: number): Promise<void> {
  try {
    const response = await api.delete<unknown>(`/WarehouseStock/${id}`);
    assertApiSuccess(response.data);
  } catch (error) {
    throw toApiFormError(error, "Failed to delete warehouse stock document");
  }
}
