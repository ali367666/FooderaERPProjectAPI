import { api } from "@/lib/api";
import { ApiFormError, toApiFormError } from "@/lib/api-error";

const SELECTED_COMPANY_KEY = "dashboardSelectedCompanyId";
const TOKEN_KEY = "token";

function readCompanyIdFromToken(): number | null {
  if (typeof window === "undefined") return null;
  const token = localStorage.getItem(TOKEN_KEY);
  if (!token) return null;
  const parts = token.split(".");
  if (parts.length < 2) return null;
  try {
    const base64 = parts[1].replace(/-/g, "+").replace(/_/g, "/");
    const padded = base64 + "=".repeat((4 - (base64.length % 4)) % 4);
    const payload = JSON.parse(atob(padded)) as Record<string, unknown>;
    const raw =
      payload.companyId ?? payload.CompanyId ?? payload.company_id ?? payload.Company_id;
    const companyId = Number(raw);
    return Number.isFinite(companyId) && companyId > 0 ? companyId : null;
  } catch {
    return null;
  }
}

function resolveMenuCompanyId(explicitCompanyId?: number): number {
  if (Number.isFinite(explicitCompanyId) && Number(explicitCompanyId) > 0) {
    return Number(explicitCompanyId);
  }
  if (typeof window !== "undefined") {
    const selected = Number(localStorage.getItem(SELECTED_COMPANY_KEY) || "");
    if (Number.isFinite(selected) && selected > 0) return selected;
  }
  const fromToken = readCompanyIdFromToken();
  if (fromToken && fromToken > 0) return fromToken;
  throw new ApiFormError("Unable to resolve companyId from selected company or token.");
}

export type MenuCategory = {
  id: number;
  name: string;
  description: string | null;
  isActive: boolean;
};

export type MenuCategoryCreateInput = {
  name: string;
  description?: string | null;
};

export type MenuCategoryUpdateInput = {
  name: string;
  description?: string | null;
  isActive: boolean;
};

function normalizeMenuCategory(item: unknown): MenuCategory | null {
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
    isActive: Boolean(raw.isActive ?? raw.IsActive ?? true),
  };
}

export async function getMenuCategories(): Promise<MenuCategory[]> {
  try {
    const response = await api.get<unknown[]>("/MenuCategories");
    const list = Array.isArray(response.data) ? response.data : [];
    return list
      .map((row) => normalizeMenuCategory(row))
      .filter((row): row is MenuCategory => row !== null);
  } catch (error) {
    throw toApiFormError(error, "Failed to fetch menu categories");
  }
}

export async function getMenuCategoryById(id: number, companyId?: number): Promise<MenuCategory> {
  try {
    const resolvedCompanyId = resolveMenuCompanyId(companyId);
    console.debug("[MenuCategories] GET by id", { id, companyId: resolvedCompanyId });
    const response = await api.get<unknown>(`/MenuCategories/${id}`, {
      params: { companyId: resolvedCompanyId },
    });
    const row = normalizeMenuCategory(response.data);
    if (!row) throw new ApiFormError("Menu category not found");
    return row;
  } catch (error) {
    throw toApiFormError(error, "Failed to load menu category");
  }
}

export async function createMenuCategory(data: MenuCategoryCreateInput): Promise<void> {
  try {
    const companyId = resolveMenuCompanyId();
    console.debug("[MenuCategories] CREATE", { companyId, name: data.name });
    await api.post("/MenuCategories", {
      name: data.name.trim(),
      description: data.description?.trim() || null,
    });
  } catch (error) {
    throw toApiFormError(error, "Failed to create menu category");
  }
}

export async function updateMenuCategory(
  id: number,
  data: MenuCategoryUpdateInput,
  companyId?: number,
): Promise<void> {
  try {
    const resolvedCompanyId = resolveMenuCompanyId(companyId);
    console.debug("[MenuCategories] UPDATE", { id, companyId: resolvedCompanyId });
    await api.put(
      `/MenuCategories/${id}`,
      {
        name: data.name.trim(),
        description: data.description?.trim() || null,
        isActive: data.isActive,
      },
      { params: { companyId: resolvedCompanyId } },
    );
  } catch (error) {
    throw toApiFormError(error, "Failed to update menu category");
  }
}

export async function deleteMenuCategory(id: number, companyId?: number): Promise<void> {
  try {
    const resolvedCompanyId = resolveMenuCompanyId(companyId);
    console.debug("[MenuCategories] DELETE", { id, companyId: resolvedCompanyId });
    await api.delete(`/MenuCategories/${id}`, { params: { companyId: resolvedCompanyId } });
  } catch (error) {
    throw toApiFormError(error, "Failed to delete menu category");
  }
}
