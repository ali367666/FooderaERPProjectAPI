import { api } from "@/lib/api";
import { assertApiSuccess, readBaseResponseData, readBaseResponseList } from "@/lib/api-base-response";
import { ApiFormError, toApiFormError } from "@/lib/api-error";
import { fetchListsPerCompany } from "@/lib/company-scope-utils";

export type AppRole = {
  id: number;
  name: string;
  normalizedName: string | null;
  companyId: number | null;
};

function normalizeRole(item: unknown): AppRole | null {
  if (!item || typeof item !== "object") return null;
  const raw = item as Record<string, unknown>;
  const id = Number(raw.id ?? raw.Id);
  if (!Number.isFinite(id) || id <= 0) return null;
  const companyIdRaw = raw.companyId ?? raw.CompanyId;
  const companyId = Number(companyIdRaw);
  return {
    id,
    name: String(raw.name ?? raw.Name ?? ""),
    normalizedName: (raw.normalizedName ?? raw.NormalizedName ?? null) as string | null,
    companyId: Number.isFinite(companyId) && companyId > 0 ? companyId : null,
  };
}

function parseFieldErrors(data: unknown): Record<string, string[]> | undefined {
  if (!data || typeof data !== "object") return undefined;
  const err = (data as { errors?: unknown; Errors?: unknown }).errors ?? (data as { Errors?: unknown }).Errors;
  if (!err || typeof err !== "object") return undefined;
  const out: Record<string, string[]> = {};
  for (const [k, v] of Object.entries(err as Record<string, unknown>)) {
    if (Array.isArray(v)) out[k] = v.filter((x): x is string => typeof x === "string");
    else if (typeof v === "string") out[k] = [v];
  }
  return Object.keys(out).length ? out : undefined;
}

export async function getRoles(companyId?: number): Promise<AppRole[]> {
  try {
    const response = await api.get<unknown>("/roles", {
      params: companyId ? { companyId } : undefined,
    });
    assertApiSuccess(response.data);
    return readBaseResponseList<unknown>(response.data)
      .map((r) => normalizeRole(r))
      .filter((r): r is AppRole => r !== null);
  } catch (e) {
    throw toApiFormError(e, "Failed to load roles.");
  }
}

/** Merge GET /roles for each company id — used when "All Companies" is selected. */
export async function getRolesForAllCompanies(companyIds: number[]): Promise<AppRole[]> {
  return fetchListsPerCompany(companyIds, (id) => getRoles(id));
}

export async function getRoleById(id: number): Promise<AppRole> {
  try {
    const response = await api.get<unknown>(`/roles/${id}`);
    assertApiSuccess(response.data);
    const data = readBaseResponseData<unknown>(response.data);
    const r = normalizeRole(data);
    if (!r) throw new Error("Role not found.");
    return r;
  } catch (e) {
    if (e instanceof Error && e.message === "Role not found.") throw e;
    throw toApiFormError(e, "Failed to load role.");
  }
}

export async function createRole(name: string, companyId?: number): Promise<number> {
  try {
    const response = await api.post<unknown>("/roles", {
      name: name.trim(),
      companyId,
    });
    if (response.data && typeof response.data === "object" && (response.data as { success?: boolean }).success === false) {
      const o = response.data as { message?: string };
      throw new ApiFormError(o.message || "Could not create role.", parseFieldErrors(response.data) ?? {});
    }
    assertApiSuccess(response.data);
    const id = readBaseResponseData<number>(response.data);
    if (id == null) throw new Error("Invalid response.");
    return Number(id);
  } catch (e) {
    if (e instanceof ApiFormError) throw e;
    throw toApiFormError(e, "Failed to create role.");
  }
}

export async function updateRole(id: number, name: string): Promise<void> {
  try {
    const response = await api.put<unknown>(`/roles/${id}`, { name: name.trim() });
    if (response.data && typeof response.data === "object" && (response.data as { success?: boolean }).success === false) {
      const o = response.data as { message?: string };
      throw new ApiFormError(o.message || "Could not update role.", parseFieldErrors(response.data) ?? {});
    }
    assertApiSuccess(response.data);
  } catch (e) {
    if (e instanceof ApiFormError) throw e;
    throw toApiFormError(e, "Failed to update role.");
  }
}

export async function deleteRoleApi(id: number): Promise<void> {
  try {
    const response = await api.delete<unknown>(`/roles/${id}`);
    assertApiSuccess(response.data);
  } catch (e) {
    throw toApiFormError(e, "Failed to delete role.");
  }
}
