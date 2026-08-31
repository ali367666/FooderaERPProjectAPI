import { api } from "@/lib/api";
import { ApiFormError, toApiFormError } from "@/lib/api-error";

/** Matches Domain.Enums.PreparationType */
export const PreparationType = {
  None: 1,
  Kitchen: 2,
  Bar: 3,
} as const;

export type PreparationTypeValue = (typeof PreparationType)[keyof typeof PreparationType];

/** Matches Domain.Enums.UnitOfMeasure */
export const UnitOfMeasure = {
  Piece: 1,
  Kg: 2,
  Gram: 3,
  Liter: 4,
  Ml: 5,
} as const;

export type UnitOfMeasureValue = (typeof UnitOfMeasure)[keyof typeof UnitOfMeasure];

export type MenuItem = {
  id: number;
  name: string;
  description: string | null;
  price: number;
  portion: string | null;
  isActive: boolean;
  menuCategoryId: number;
  menuCategoryName: string;
  preparationType: PreparationTypeValue;

  itemTypeId: number;
  itemTypeName: string;
  unitId: UnitOfMeasureValue;
  vatPercent: number | null;
  weightCode: string | null;
  barcode: string | null;

  stationPrice: number | null;
  purchasePrice: number | null;
  packagePrice: number | null;
  specialPrice1: number | null;
  specialPrice2: number | null;
  specialPrice3: number | null;
  specialPrice4: number | null;
  specialPrice5: number | null;

  hideFromPosSearch: boolean;
  hideBarcode: boolean;
  excludeFromDiscount: boolean;
  skipTaxCalculation: boolean;
  isTimeBased: boolean;
  allowQuantityPromptOverride: boolean;
  printerId: number | null;

  isSet: boolean;

  stockItemId: number | null;
  stockItemName: string | null;
};

export type SetComponent = {
  componentMenuItemId: number;
  componentMenuItemName: string;
  quantity: number;
};

export type SetComponentInput = {
  componentMenuItemId: number;
  quantity: number;
};

export type MenuItemCreateInput = {
  name: string;
  description?: string | null;
  price: number;
  portion?: string | null;
  menuCategoryId: number;
  preparationType: PreparationTypeValue;

  itemTypeId: number;
  unitId: UnitOfMeasureValue;
  vatPercent?: number | null;
  barcode?: string | null;

  stationPrice?: number | null;
  purchasePrice?: number | null;
  packagePrice?: number | null;
  specialPrice1?: number | null;
  specialPrice2?: number | null;
  specialPrice3?: number | null;
  specialPrice4?: number | null;
  specialPrice5?: number | null;

  hideFromPosSearch: boolean;
  hideBarcode: boolean;
  excludeFromDiscount: boolean;
  skipTaxCalculation: boolean;
  isTimeBased: boolean;
  allowQuantityPromptOverride: boolean;

  isSet: boolean;

  stockItemId?: number | null;
};

export type MenuItemUpdateInput = MenuItemCreateInput & {
  isActive: boolean;
  resetWeightCode?: boolean;
  setComponents: SetComponentInput[];
};

function normalizePreparationType(raw: unknown): PreparationTypeValue {
  const n = Number(raw);
  if (n === PreparationType.None || n === PreparationType.Kitchen || n === PreparationType.Bar) {
    return n;
  }
  return PreparationType.Kitchen;
}

function normalizeUnitId(raw: unknown): UnitOfMeasureValue {
  const n = Number(raw);
  if (
    n === UnitOfMeasure.Piece ||
    n === UnitOfMeasure.Kg ||
    n === UnitOfMeasure.Gram ||
    n === UnitOfMeasure.Liter ||
    n === UnitOfMeasure.Ml
  ) {
    return n;
  }
  return UnitOfMeasure.Piece;
}

function nullableNumber(v: unknown): number | null {
  return v == null ? null : Number(v);
}

function normalizeMenuItem(item: unknown): MenuItem | null {
  if (!item || typeof item !== "object") return null;
  const raw = item as Record<string, unknown>;
  const id = Number(raw.id ?? raw.Id);
  if (!Number.isFinite(id) || id <= 0) return null;

  return {
    id,
    name: String(raw.name ?? raw.Name ?? ""),
    description:
      raw.description === undefined && raw.Description === undefined
        ? null
        : String(raw.description ?? raw.Description ?? "") || null,
    price: Number(raw.price ?? raw.Price ?? 0),
    portion:
      raw.portion === undefined && raw.Portion === undefined
        ? null
        : String(raw.portion ?? raw.Portion ?? "") || null,
    isActive: Boolean(raw.isActive ?? raw.IsActive ?? true),
    menuCategoryId: Number(raw.menuCategoryId ?? raw.MenuCategoryId ?? 0),
    menuCategoryName: String(raw.menuCategoryName ?? raw.MenuCategoryName ?? ""),
    preparationType: normalizePreparationType(raw.preparationType ?? raw.PreparationType),

    itemTypeId: Number(raw.itemTypeId ?? raw.ItemTypeId ?? 0),
    itemTypeName: String(raw.itemTypeName ?? raw.ItemTypeName ?? ""),
    unitId: normalizeUnitId(raw.unitId ?? raw.UnitId),
    vatPercent: nullableNumber(raw.vatPercent ?? raw.VatPercent),
    weightCode: (raw.weightCode ?? raw.WeightCode) != null ? String(raw.weightCode ?? raw.WeightCode) : null,
    barcode: (raw.barcode ?? raw.Barcode) != null ? String(raw.barcode ?? raw.Barcode) : null,

    stationPrice: nullableNumber(raw.stationPrice ?? raw.StationPrice),
    purchasePrice: nullableNumber(raw.purchasePrice ?? raw.PurchasePrice),
    packagePrice: nullableNumber(raw.packagePrice ?? raw.PackagePrice),
    specialPrice1: nullableNumber(raw.specialPrice1 ?? raw.SpecialPrice1),
    specialPrice2: nullableNumber(raw.specialPrice2 ?? raw.SpecialPrice2),
    specialPrice3: nullableNumber(raw.specialPrice3 ?? raw.SpecialPrice3),
    specialPrice4: nullableNumber(raw.specialPrice4 ?? raw.SpecialPrice4),
    specialPrice5: nullableNumber(raw.specialPrice5 ?? raw.SpecialPrice5),

    hideFromPosSearch: Boolean(raw.hideFromPosSearch ?? raw.HideFromPosSearch ?? false),
    hideBarcode: Boolean(raw.hideBarcode ?? raw.HideBarcode ?? false),
    excludeFromDiscount: Boolean(raw.excludeFromDiscount ?? raw.ExcludeFromDiscount ?? false),
    skipTaxCalculation: Boolean(raw.skipTaxCalculation ?? raw.SkipTaxCalculation ?? false),
    isTimeBased: Boolean(raw.isTimeBased ?? raw.IsTimeBased ?? false),
    allowQuantityPromptOverride: Boolean(
      raw.allowQuantityPromptOverride ?? raw.AllowQuantityPromptOverride ?? false,
    ),
    printerId: nullableNumber(raw.printerId ?? raw.PrinterId),

    isSet: Boolean(raw.isSet ?? raw.IsSet ?? false),

    stockItemId: nullableNumber(raw.stockItemId ?? raw.StockItemId),
    stockItemName:
      (raw.stockItemName ?? raw.StockItemName) != null ? String(raw.stockItemName ?? raw.StockItemName) : null,
  };
}

function normalizeSetComponent(item: unknown): SetComponent | null {
  if (!item || typeof item !== "object") return null;
  const raw = item as Record<string, unknown>;
  const componentMenuItemId = Number(raw.componentMenuItemId ?? raw.ComponentMenuItemId);
  if (!Number.isFinite(componentMenuItemId) || componentMenuItemId <= 0) return null;
  return {
    componentMenuItemId,
    componentMenuItemName: String(raw.componentMenuItemName ?? raw.ComponentMenuItemName ?? ""),
    quantity: Number(raw.quantity ?? raw.Quantity ?? 1),
  };
}

export async function getMenuItems(): Promise<MenuItem[]> {
  try {
    const response = await api.get<unknown[]>("/MenuItems");
    const list = Array.isArray(response.data) ? response.data : [];
    return list
      .map((row) => normalizeMenuItem(row))
      .filter((row): row is MenuItem => row !== null);
  } catch (error) {
    throw toApiFormError(error, "Failed to fetch menu items");
  }
}

export async function getMenuItemById(id: number): Promise<MenuItem> {
  try {
    const response = await api.get<unknown>(`/MenuItems/${id}`);
    const row = normalizeMenuItem(response.data);
    if (!row) throw new ApiFormError("Menu item not found");
    return row;
  } catch (error) {
    throw toApiFormError(error, "Failed to load menu item");
  }
}

export async function getMenuItemSetComponents(setMenuItemId: number): Promise<SetComponent[]> {
  try {
    const response = await api.get<unknown[]>(`/MenuItems/${setMenuItemId}/set-components`);
    const list = Array.isArray(response.data) ? response.data : [];
    return list
      .map((row) => normalizeSetComponent(row))
      .filter((row): row is SetComponent => row !== null);
  } catch (error) {
    throw toApiFormError(error, "Failed to fetch SET components");
  }
}

function buildBasePayload(data: MenuItemCreateInput) {
  return {
    name: data.name.trim(),
    description: data.description?.trim() || null,
    price: data.price,
    portion: data.portion?.trim() || null,
    menuCategoryId: data.menuCategoryId,
    preparationType: data.preparationType,

    itemTypeId: data.itemTypeId,
    unitId: data.unitId,
    vatPercent: data.vatPercent ?? null,
    barcode: data.barcode?.trim() || null,

    stationPrice: data.stationPrice ?? null,
    purchasePrice: data.purchasePrice ?? null,
    packagePrice: data.packagePrice ?? null,
    specialPrice1: data.specialPrice1 ?? null,
    specialPrice2: data.specialPrice2 ?? null,
    specialPrice3: data.specialPrice3 ?? null,
    specialPrice4: data.specialPrice4 ?? null,
    specialPrice5: data.specialPrice5 ?? null,

    hideFromPosSearch: data.hideFromPosSearch,
    hideBarcode: data.hideBarcode,
    excludeFromDiscount: data.excludeFromDiscount,
    skipTaxCalculation: data.skipTaxCalculation,
    isTimeBased: data.isTimeBased,
    allowQuantityPromptOverride: data.allowQuantityPromptOverride,

    isSet: data.isSet,

    stockItemId: data.stockItemId ?? null,
  };
}

export async function createMenuItem(data: MenuItemCreateInput): Promise<void> {
  try {
    await api.post("/MenuItems", buildBasePayload(data));
  } catch (error) {
    throw toApiFormError(error, "Failed to create menu item");
  }
}

export async function updateMenuItem(id: number, data: MenuItemUpdateInput): Promise<void> {
  try {
    await api.put(`/MenuItems/${id}`, {
      ...buildBasePayload(data),
      isActive: data.isActive,
      resetWeightCode: data.resetWeightCode ?? false,
      setComponents: data.setComponents,
    });
  } catch (error) {
    throw toApiFormError(error, "Failed to update menu item");
  }
}

export type WarehouseQuantityLine = {
  warehouseId: number;
  warehouseName: string;
  quantity: number;
};

export type MenuItemStockInfo = {
  stockItemId: number | null;
  stockItemName: string | null;
  directBalances: WarehouseQuantityLine[];
  recipeMakeablePortions: WarehouseQuantityLine[];
};

function normalizeWarehouseQuantityLine(item: unknown): WarehouseQuantityLine | null {
  if (!item || typeof item !== "object") return null;
  const raw = item as Record<string, unknown>;
  const warehouseId = Number(raw.warehouseId ?? raw.WarehouseId);
  if (!Number.isFinite(warehouseId) || warehouseId <= 0) return null;
  return {
    warehouseId,
    warehouseName: String(raw.warehouseName ?? raw.WarehouseName ?? ""),
    quantity: Number(raw.quantity ?? raw.Quantity ?? 0),
  };
}

export async function getMenuItemStockInfo(menuItemId: number): Promise<MenuItemStockInfo> {
  try {
    const response = await api.get<unknown>(`/MenuItems/${menuItemId}/stock-info`);
    const raw = (response.data ?? {}) as Record<string, unknown>;
    const directRaw = raw.directBalances ?? raw.DirectBalances;
    const recipeRaw = raw.recipeMakeablePortions ?? raw.RecipeMakeablePortions;
    return {
      stockItemId: nullableNumber(raw.stockItemId ?? raw.StockItemId),
      stockItemName:
        (raw.stockItemName ?? raw.StockItemName) != null ? String(raw.stockItemName ?? raw.StockItemName) : null,
      directBalances: Array.isArray(directRaw)
        ? directRaw.map(normalizeWarehouseQuantityLine).filter((x): x is WarehouseQuantityLine => x !== null)
        : [],
      recipeMakeablePortions: Array.isArray(recipeRaw)
        ? recipeRaw.map(normalizeWarehouseQuantityLine).filter((x): x is WarehouseQuantityLine => x !== null)
        : [],
    };
  } catch (error) {
    throw toApiFormError(error, "Failed to fetch stock info");
  }
}

export async function deleteMenuItem(id: number): Promise<void> {
  try {
    await api.delete(`/MenuItems/${id}`);
  } catch (error) {
    throw toApiFormError(error, "Failed to delete menu item");
  }
}

export function preparationTypeLabel(value: PreparationTypeValue): string {
  switch (value) {
    case PreparationType.None:
      return "None";
    case PreparationType.Kitchen:
      return "Kitchen";
    case PreparationType.Bar:
      return "Bar";
    default:
      return "Kitchen";
  }
}

export function unitLabel(value: UnitOfMeasureValue): string {
  switch (value) {
    case UnitOfMeasure.Piece:
      return "Ədəd";
    case UnitOfMeasure.Kg:
      return "Kq";
    case UnitOfMeasure.Gram:
      return "Qram";
    case UnitOfMeasure.Liter:
      return "Litr";
    case UnitOfMeasure.Ml:
      return "Ml";
    default:
      return "Ədəd";
  }
}
