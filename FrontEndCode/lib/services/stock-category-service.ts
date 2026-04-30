import { api } from "@/lib/api";
import {
  assertApiSuccess,
  readBaseResponseData,
  readBaseResponseList,
} from "@/lib/api-base-response";
import { ApiFormError, toApiFormError } from "@/lib/api-error";
import { resolveCompanyId } from "@/lib/resolve-company-id";
import { fetchListsPerCompany } from "@/lib/company-scope-utils";

export type StockCategory = {
  id: number;
  name: string;
  description: string | null;
  isActive: boolean;
  companyId: number;
  companyName: string | null;
  parentId: number | null;
  parentName: string | null;
};

export type StockCategoryMutationInput = {
  name: string;
  description?: string | null;
  isActive: boolean;
  companyId: number;
  parentId?: number | null;
};

function normalizeCategory(item: unknown): StockCategory | null {
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
    companyId: Number(raw.companyId ?? raw.CompanyId ?? 0),
    companyName:
      raw.companyName !== undefined || raw.CompanyName !== undefined
        ? String(raw.companyName ?? raw.CompanyName ?? "") || null
        : null,
    parentId:
      raw.parentId === undefined && raw.ParentId === undefined
        ? null
        : Number(raw.parentId ?? raw.ParentId) || null,
    parentName:
      raw.parentName !== undefined || raw.ParentName !== undefined
        ? String(raw.parentName ?? raw.ParentName ?? "") || null
        : null,
  };
}

/** List categories for a company (legacy endpoint). Returns [] if API reports no data for company. */
export async function getStockCategoriesByCompany(companyId?: number): Promise<StockCategory[]> {
  try {
    const cid = companyId ?? resolveCompanyId();
    const response = await api.get<unknown>(`/StockCategory/company/${cid}`);
    const body = response.data;
    if (body && typeof body === "object") {
      const o = body as { success?: boolean };
      if (o.success === false) {
        return [];
      }
    }
    assertApiSuccess(body);
    const list = readBaseResponseList<unknown>(body);
    return list.map((row) => normalizeCategory(row)).filter((row): row is StockCategory => row !== null);
  } catch (error) {
    throw toApiFormError(error, "Failed to fetch stock categories");
  }
}

/** GET /StockCategory — optional server-side filters; defaults to current company unless `allCompanies`. */
export async function getAllStockCategories(options?: {
  companyId?: number;
  isActive?: boolean;
  searchTerm?: string;
  /** When true, no companyId is sent so the API returns categories across companies. */
  allCompanies?: boolean;
}): Promise<StockCategory[]> {
  try {
    const params: Record<string, string | number | boolean> = {};
    if (!options?.allCompanies) {
      const companyId = options?.companyId ?? resolveCompanyId();
      if (companyId) params.companyId = companyId;
    }
    if (options?.isActive !== undefined) params.isActive = options.isActive;
    if (options?.searchTerm?.trim()) params.searchTerm = options.searchTerm.trim();

    const response = await api.get<unknown>("/StockCategory", { params });
    assertApiSuccess(response.data);
    const list = readBaseResponseList<unknown>(response.data);
    return list.map((row) => normalizeCategory(row)).filter((row): row is StockCategory => row !== null);
  } catch (error) {
    throw toApiFormError(error, "Failed to fetch stock categories");
  }
}

/** Merge stock categories for each company (uses scoped GET per id if a global list is not available). */
export async function getAllStockCategoriesForAllCompanies(companyIds: number[]): Promise<StockCategory[]> {
  return fetchListsPerCompany(companyIds, (id) => getAllStockCategories({ companyId: id }));
}

export async function getStockCategoryById(id: number): Promise<StockCategory> {
  try {
    const response = await api.get<unknown>(`/StockCategory/${id}`);
    assertApiSuccess(response.data);
    const data = readBaseResponseData<unknown>(response.data);
    const row = normalizeCategory(data);
    if (!row) throw new ApiFormError("Stock category not found");
    return row;
  } catch (error) {
    throw toApiFormError(error, "Failed to load stock category");
  }
}

export async function createStockCategory(payload: StockCategoryMutationInput): Promise<void> {
  try {
    const response = await api.post<unknown>("/StockCategory", {
      name: payload.name.trim(),
      description: payload.description?.trim() || null,
      isActive: payload.isActive,
      companyId: payload.companyId,
      parentId: payload.parentId ?? null,
    });
    assertApiSuccess(response.data);
  } catch (error) {
    throw toApiFormError(error, "Failed to create stock category");
  }
}

export async function updateStockCategory(id: number, payload: StockCategoryMutationInput): Promise<void> {
  try {
    const response = await api.put<unknown>(`/StockCategory/${id}`, {
      name: payload.name.trim(),
      description: payload.description?.trim() || null,
      isActive: payload.isActive,
      companyId: payload.companyId,
      parentId: payload.parentId ?? null,
    });
    assertApiSuccess(response.data);
  } catch (error) {
    throw toApiFormError(error, "Failed to update stock category");
  }
}

export async function deleteStockCategory(id: number): Promise<void> {
  try {
    const response = await api.delete<unknown>(`/StockCategory/${id}`);
    assertApiSuccess(response.data);
  } catch (error) {
    throw toApiFormError(error, "Failed to delete stock category");
  }
}
