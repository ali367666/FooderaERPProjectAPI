import { api } from "@/lib/api";
import { assertApiSuccess, readBaseResponseList } from "@/lib/api-base-response";
import { ApiFormError, toApiFormError } from "@/lib/api-error";
import { resolveCompanyId } from "@/lib/resolve-company-id";
import { fetchListsPerCompany } from "@/lib/company-scope-utils";
import { unitLabel, type UnitOfMeasureValue } from "@/lib/services/stock-item-service";

export type WarehouseStockBalanceRow = {
  id: number;
  companyId: number;
  warehouseId: number;
  warehouseName: string;
  stockItemId: number;
  stockItemName: string;
  quantity: number;
  unitId: number;
  unitLabel: string;
  minimumQuantity: number | null;
  reorderLevel: number | null;
};

function normalizeRow(item: unknown): WarehouseStockBalanceRow | null {
  if (!item || typeof item !== "object") return null;
  const raw = item as Record<string, unknown>;
  const id = Number(raw.id ?? raw.Id);
  if (!Number.isFinite(id) || id <= 0) return null;
  const unitId = Number(raw.unitId ?? raw.UnitId ?? 0);
  return {
    id,
    companyId: Number(raw.companyId ?? raw.CompanyId ?? 0),
    warehouseId: Number(raw.warehouseId ?? raw.WarehouseId ?? 0),
    warehouseName: String(raw.warehouseName ?? raw.WarehouseName ?? ""),
    stockItemId: Number(raw.stockItemId ?? raw.StockItemId ?? 0),
    stockItemName: String(raw.stockItemName ?? raw.StockItemName ?? ""),
    quantity: Number(raw.quantity ?? raw.Quantity ?? 0),
    unitId,
    unitLabel: unitLabel(unitId as UnitOfMeasureValue),
    minimumQuantity:
      raw.minimumQuantity == null && raw.MinimumQuantity == null
        ? null
        : Number(raw.minimumQuantity ?? raw.MinimumQuantity ?? 0),
    reorderLevel:
      raw.reorderLevel == null && raw.ReorderLevel == null
        ? null
        : Number(raw.reorderLevel ?? raw.ReorderLevel ?? 0),
  };
}

export async function searchWarehouseStockBalances(
  options?: { warehouseId?: number; stockItemId?: number; search?: string },
  companyId?: number,
): Promise<WarehouseStockBalanceRow[]> {
  try {
    const cid = companyId ?? resolveCompanyId();
    const response = await api.get<unknown>("/WarehouseStockBalance/search", {
      params: {
        companyId: cid,
        warehouseId: options?.warehouseId,
        stockItemId: options?.stockItemId,
        search: options?.search?.trim() || undefined,
      },
    });
    assertApiSuccess(response.data);
    const list = readBaseResponseList<unknown>(response.data);
    return list
      .map((row) => normalizeRow(row))
      .filter((row): row is WarehouseStockBalanceRow => row !== null);
  } catch (error) {
    throw toApiFormError(error, "Failed to fetch warehouse stock balances");
  }
}

export async function searchWarehouseStockBalancesForAllCompanies(
  companyIds: number[],
  options?: { warehouseId?: number; stockItemId?: number; search?: string },
): Promise<WarehouseStockBalanceRow[]> {
  return fetchListsPerCompany(companyIds, (id) => searchWarehouseStockBalances(options, id));
}
