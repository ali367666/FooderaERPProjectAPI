import { api } from "@/lib/api";
import { assertApiSuccess, readBaseResponseData, readBaseResponseList } from "@/lib/api-base-response";
import { ApiFormError, toApiFormError } from "@/lib/api-error";
import { resolveCompanyId } from "@/lib/resolve-company-id";
import { fetchListsPerCompany } from "@/lib/company-scope-utils";

/** Domain.Enums.StockItemType */
export const StockItemType = {
  RawMaterial: 1,
  FinishedGood: 2,
  NonFood: 3,
} as const;
export type StockItemTypeValue = (typeof StockItemType)[keyof typeof StockItemType];

/** Domain.Enums.UnitOfMeasure */
export const UnitOfMeasure = {
  Piece: 1,
  Kg: 2,
  Gram: 3,
  Liter: 4,
  Ml: 5,
} as const;
export type UnitOfMeasureValue = (typeof UnitOfMeasure)[keyof typeof UnitOfMeasure];

export type StockItem = {
  id: number;
  name: string;
  barcode: string | null;
  type: StockItemTypeValue;
  unit: UnitOfMeasureValue;
  categoryId: number;
  categoryName: string;
  companyId: number;
  restaurantId: number | null;
  restaurantName: string | null;
};

export type StockItemMutationInput = {
  name: string;
  barcode?: string | null;
  type: StockItemTypeValue;
  unit: UnitOfMeasureValue;
  categoryId: number;
  companyId: number;
  restaurantId?: number | null;
};

function normalizeStockItem(item: unknown): StockItem | null {
  if (!item || typeof item !== "object") return null;
  const raw = item as Record<string, unknown>;
  const id = Number(raw.id ?? raw.Id);
  if (!Number.isFinite(id) || id <= 0) return null;
  const type = Number(raw.type ?? raw.Type ?? StockItemType.RawMaterial);
  const unit = Number(raw.unit ?? raw.Unit ?? UnitOfMeasure.Piece);
  return {
    id,
    name: String(raw.name ?? raw.Name ?? ""),
    barcode:
      raw.barcode === undefined && raw.Barcode === undefined
        ? null
        : String(raw.barcode ?? raw.Barcode ?? "") || null,
    type: type as StockItemTypeValue,
    unit: unit as UnitOfMeasureValue,
    categoryId: Number(raw.categoryId ?? raw.CategoryId ?? 0),
    categoryName: String(raw.categoryName ?? raw.CategoryName ?? ""),
    companyId: Number(raw.companyId ?? raw.CompanyId ?? 0),
    restaurantId:
      raw.restaurantId === undefined && raw.RestaurantId === undefined
        ? null
        : Number(raw.restaurantId ?? raw.RestaurantId) || null,
    restaurantName:
      raw.restaurantName !== undefined || raw.RestaurantName !== undefined
        ? String(raw.restaurantName ?? raw.RestaurantName ?? "") || null
        : null,
  };
}

export function unitLabel(unit: UnitOfMeasureValue): string {
  switch (unit) {
    case UnitOfMeasure.Piece:
      return "Piece";
    case UnitOfMeasure.Kg:
      return "Kg";
    case UnitOfMeasure.Gram:
      return "Gram";
    case UnitOfMeasure.Liter:
      return "Liter";
    case UnitOfMeasure.Ml:
      return "Ml";
    default:
      return String(unit);
  }
}

export function stockItemTypeLabel(type: StockItemTypeValue): string {
  switch (type) {
    case StockItemType.RawMaterial:
      return "Raw material";
    case StockItemType.FinishedGood:
      return "Finished good";
    case StockItemType.NonFood:
      return "Non-food";
    default:
      return String(type);
  }
}

export async function getStockItemsForCompany(companyId: number): Promise<StockItem[]> {
  try {
    const response = await api.get<unknown>("/StockItem", {
      params: { companyId },
    });
    assertApiSuccess(response.data);
    const list = readBaseResponseList<unknown>(response.data);
    return list.map((row) => normalizeStockItem(row)).filter((row): row is StockItem => row !== null);
  } catch (error) {
    throw toApiFormError(error, "Failed to fetch stock items");
  }
}

export async function getStockItemsForAllCompanies(companyIds: number[]): Promise<StockItem[]> {
  return fetchListsPerCompany(companyIds, (id) => getStockItemsForCompany(id));
}

export async function getStockItems(): Promise<StockItem[]> {
  return getStockItemsForCompany(resolveCompanyId());
}

export async function getStockItemById(id: number): Promise<StockItem> {
  try {
    const response = await api.get<unknown>(`/StockItem/${id}`);
    assertApiSuccess(response.data);
    const data = readBaseResponseData<unknown>(response.data);
    const row = normalizeStockItem(data);
    if (!row) throw new ApiFormError("Stock item not found");
    return row;
  } catch (error) {
    throw toApiFormError(error, "Failed to load stock item");
  }
}

export async function createStockItem(payload: StockItemMutationInput): Promise<void> {
  try {
    const response = await api.post<unknown>("/StockItem", {
      name: payload.name.trim(),
      barcode: payload.barcode?.trim() || null,
      type: payload.type,
      unit: payload.unit,
      categoryId: payload.categoryId,
      companyId: payload.companyId,
      restaurantId: payload.restaurantId ?? null,
    });
    assertApiSuccess(response.data);
  } catch (error) {
    throw toApiFormError(error, "Failed to create stock item");
  }
}

export async function updateStockItem(id: number, payload: StockItemMutationInput): Promise<void> {
  try {
    const response = await api.put<unknown>(`/StockItem/${id}`, {
      name: payload.name.trim(),
      barcode: payload.barcode?.trim() || null,
      type: payload.type,
      unit: payload.unit,
      categoryId: payload.categoryId,
      companyId: payload.companyId,
      restaurantId: payload.restaurantId ?? null,
    });
    assertApiSuccess(response.data);
  } catch (error) {
    throw toApiFormError(error, "Failed to update stock item");
  }
}

export async function deleteStockItem(id: number): Promise<void> {
  try {
    const response = await api.delete<unknown>(`/StockItem/${id}`);
    assertApiSuccess(response.data);
  } catch (error) {
    throw toApiFormError(error, "Failed to delete stock item");
  }
}
