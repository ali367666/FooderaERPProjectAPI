import { api } from "@/lib/api";
import { ApiFormError, toApiFormError } from "@/lib/api-error";

/** Matches Domain.Enums.PreparationType */
export const PreparationType = {
  None: 1,
  Kitchen: 2,
  Bar: 3,
} as const;

export type PreparationTypeValue = (typeof PreparationType)[keyof typeof PreparationType];

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
};

export type MenuItemCreateInput = {
  name: string;
  description?: string | null;
  price: number;
  portion?: string | null;
  menuCategoryId: number;
  preparationType: PreparationTypeValue;
};

export type MenuItemUpdateInput = {
  name: string;
  description?: string | null;
  price: number;
  portion?: string | null;
  menuCategoryId: number;
  preparationType: PreparationTypeValue;
  isActive: boolean;
};

function normalizePreparationType(raw: unknown): PreparationTypeValue {
  const n = Number(raw);
  if (n === PreparationType.None || n === PreparationType.Kitchen || n === PreparationType.Bar) {
    return n;
  }
  return PreparationType.Kitchen;
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

export async function createMenuItem(data: MenuItemCreateInput): Promise<void> {
  try {
    await api.post("/MenuItems", {
      name: data.name.trim(),
      description: data.description?.trim() || null,
      price: data.price,
      portion: data.portion?.trim() || null,
      menuCategoryId: data.menuCategoryId,
      preparationType: data.preparationType,
    });
  } catch (error) {
    throw toApiFormError(error, "Failed to create menu item");
  }
}

export async function updateMenuItem(id: number, data: MenuItemUpdateInput): Promise<void> {
  try {
    await api.put(`/MenuItems/${id}`, {
      name: data.name.trim(),
      description: data.description?.trim() || null,
      price: data.price,
      portion: data.portion?.trim() || null,
      menuCategoryId: data.menuCategoryId,
      preparationType: data.preparationType,
      isActive: data.isActive,
    });
  } catch (error) {
    throw toApiFormError(error, "Failed to update menu item");
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
