import { api } from "@/lib/api";
import { readBaseResponseList } from "@/lib/api-base-response";
import { toApiFormError } from "@/lib/api-error";
import axios from "axios";

export type MenuItemRecipeLine = {
  stockItemId: number;
  stockItemName: string;
  quantity: number;
  unit: string;
};

export type MenuItemRecipe = {
  id: number;
  menuItemId: number;
  menuItemName: string;
  lines: MenuItemRecipeLine[];
};

export type CreateOrUpdateMenuItemRecipeInput = {
  menuItemId: number;
  lines: Array<{
    stockItemId: number;
    quantity: number;
  }>;
};

function normalizeLine(row: unknown): MenuItemRecipeLine | null {
  if (!row || typeof row !== "object") return null;
  const raw = row as Record<string, unknown>;
  const stockItemId = Number(raw.stockItemId ?? raw.StockItemId ?? 0);
  if (!Number.isFinite(stockItemId) || stockItemId <= 0) return null;
  const stockItemName = String(raw.stockItemName ?? raw.StockItemName ?? "").trim();
  if (!stockItemName) return null;
  const quantity = Number(raw.quantity ?? raw.Quantity ?? raw.quantityPerPortion ?? raw.QuantityPerPortion ?? 0);
  if (!Number.isFinite(quantity) || quantity <= 0) return null;
  return {
    stockItemId,
    stockItemName,
    quantity,
    unit: String(raw.unit ?? raw.Unit ?? "").trim(),
  };
}

function readRecipeArray(body: unknown): unknown[] {
  if (Array.isArray(body)) return body;
  return readBaseResponseList<unknown>(body);
}

function normalizeRecipe(row: unknown): MenuItemRecipe | null {
  if (!row || typeof row !== "object") return null;
  const raw = row as Record<string, unknown>;
  const menuItemId = Number(raw.menuItemId ?? raw.MenuItemId ?? 0);
  if (!Number.isFinite(menuItemId) || menuItemId <= 0) return null;
  const id = Number(raw.id ?? raw.Id ?? menuItemId);
  const linesRaw = Array.isArray(raw.lines ?? raw.Lines) ? ((raw.lines ?? raw.Lines) as unknown[]) : [];
  const lines = linesRaw.map(normalizeLine).filter((x): x is MenuItemRecipeLine => x !== null);
  return {
    id: Number.isFinite(id) ? id : menuItemId,
    menuItemId,
    menuItemName: String(raw.menuItemName ?? raw.MenuItemName ?? `Menu Item #${menuItemId}`),
    lines,
  };
}

export async function getAllMenuItemRecipes(): Promise<MenuItemRecipe[]> {
  try {
    const res = await api.get<unknown>("/MenuItemRecipes");
    console.log("recipes api response", res.data);
    const list = readRecipeArray(res.data);
    console.log("recipes parsed", list);
    return list.map(normalizeRecipe).filter((x): x is MenuItemRecipe => x !== null);
  } catch (error) {
    throw toApiFormError(error, "Failed to fetch recipes");
  }
}

export async function getRecipeByMenuItemId(menuItemId: number): Promise<MenuItemRecipe | null> {
  try {
    const res = await api.get<unknown>(`/MenuItemRecipes/by-menu-item/${menuItemId}`);
    console.log("selected recipe response", res.data);
    const payload = (res.data && typeof res.data === "object" && "data" in (res.data as Record<string, unknown>))
      ? (res.data as Record<string, unknown>).data
      : res.data;
    return normalizeRecipe(payload);
  } catch (error) {
    if (axios.isAxiosError(error) && error.response?.status === 404) {
      return null;
    }
    throw toApiFormError(error, "Failed to fetch recipe");
  }
}

export async function createOrUpdateRecipe(
  input: CreateOrUpdateMenuItemRecipeInput,
): Promise<MenuItemRecipe | null> {
  return createRecipe(input);
}

export async function createRecipe(
  input: CreateOrUpdateMenuItemRecipeInput,
): Promise<MenuItemRecipe | null> {
  try {
    const res = await api.post<unknown>("/MenuItemRecipes", {
      menuItemId: input.menuItemId,
      lines: input.lines.map((x) => ({
        stockItemId: x.stockItemId,
        quantity: x.quantity,
      })),
    });
    console.log("save response", res.data);
    const payload = (res.data && typeof res.data === "object" && "data" in (res.data as Record<string, unknown>))
      ? (res.data as Record<string, unknown>).data
      : res.data;
    return normalizeRecipe(payload);
  } catch (error) {
    throw toApiFormError(error, "Failed to save recipe");
  }
}

export async function updateRecipe(
  input: CreateOrUpdateMenuItemRecipeInput,
): Promise<MenuItemRecipe | null> {
  try {
    const res = await api.put<unknown>(`/MenuItemRecipes/${input.menuItemId}`, {
      lines: input.lines.map((x) => ({
        stockItemId: x.stockItemId,
        quantity: x.quantity,
      })),
    });
    console.log("save response", res.data);
    const payload = (res.data && typeof res.data === "object" && "data" in (res.data as Record<string, unknown>))
      ? (res.data as Record<string, unknown>).data
      : res.data;
    return normalizeRecipe(payload);
  } catch (error) {
    throw toApiFormError(error, "Failed to update recipe");
  }
}
