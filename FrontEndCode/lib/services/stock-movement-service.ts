import { api } from "@/lib/api";
import { assertApiSuccess, readBaseResponseList } from "@/lib/api-base-response";
import { ApiFormError, toApiFormError } from "@/lib/api-error";
import { resolveCompanyId } from "@/lib/resolve-company-id";
import { fetchListsPerCompany } from "@/lib/company-scope-utils";

export type StockMovementRow = {
  id: number;
  companyId: number;
  sourceDocumentNo: string | null;
  stockItemId: number;
  stockItemName: string;
  stockItemUnit: string | number | null;
  warehouseName: string;
  fromWarehouseName: string | null;
  toWarehouseName: string | null;
  quantity: number;
  movementType: string;
  sourceType: string;
  movementDate: string;
  note: string | null;
};

function normalizeRow(item: unknown): StockMovementRow | null {
  if (!item || typeof item !== "object") return null;
  const raw = item as Record<string, unknown>;
  const id = Number(raw.id ?? raw.Id);
  if (!Number.isFinite(id) || id <= 0) return null;
  const md = raw.movementDate ?? raw.MovementDate;
  return {
    id,
    companyId: Number(raw.companyId ?? raw.CompanyId ?? 0),
    sourceDocumentNo:
      raw.sourceDocumentNo != null || raw.SourceDocumentNo != null
        ? String(raw.sourceDocumentNo ?? raw.SourceDocumentNo ?? "")
        : null,
    stockItemId: Number(raw.stockItemId ?? raw.StockItemId ?? 0),
    stockItemName: String(raw.stockItemName ?? raw.StockItemName ?? ""),
    stockItemUnit: (raw.stockItemUnit ?? raw.StockItemUnit ?? null) as string | number | null,
    warehouseName: String(raw.warehouseName ?? raw.WarehouseName ?? ""),
    fromWarehouseName:
      raw.fromWarehouseName != null || raw.FromWarehouseName != null
        ? String(raw.fromWarehouseName ?? raw.FromWarehouseName ?? "")
        : null,
    toWarehouseName:
      raw.toWarehouseName != null || raw.ToWarehouseName != null
        ? String(raw.toWarehouseName ?? raw.ToWarehouseName ?? "")
        : null,
    quantity: Number(raw.quantity ?? raw.Quantity ?? 0),
    movementType: String(raw.movementType ?? raw.MovementType ?? ""),
    sourceType: String(raw.sourceType ?? raw.SourceType ?? ""),
    movementDate: md instanceof Date ? md.toISOString() : String(md ?? ""),
    note: raw.note != null || raw.Note != null ? String(raw.note ?? raw.Note ?? "") : null,
  };
}

export async function searchStockMovements(search: string | undefined, companyId?: number): Promise<StockMovementRow[]> {
  try {
    const cid = companyId ?? resolveCompanyId();
    const response = await api.get<unknown>("/StockMovements/search", {
      params: {
        companyId: cid,
        search: search?.trim() || undefined,
      },
    });
    assertApiSuccess(response.data);
    const list = readBaseResponseList<unknown>(response.data);
    return list
      .map((row) => normalizeRow(row))
      .filter((row): row is StockMovementRow => row !== null);
  } catch (error) {
    throw toApiFormError(error, "Failed to fetch stock movements");
  }
}

export async function searchStockMovementsForAllCompanies(
  companyIds: number[],
  search?: string,
): Promise<StockMovementRow[]> {
  return fetchListsPerCompany(companyIds, (id) => searchStockMovements(search, id));
}
