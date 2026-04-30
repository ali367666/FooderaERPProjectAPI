import { api } from "@/lib/api";
import { assertApiSuccess, readBaseResponseData, readBaseResponseList } from "@/lib/api-base-response";
import { toApiFormError } from "@/lib/api-error";

export type PermissionDto = {
  id: number;
  name: string;
  displayName: string;
  module: string;
  action: string;
};

function normalizePermission(raw: unknown): PermissionDto | null {
  if (!raw || typeof raw !== "object") return null;
  const o = raw as Record<string, unknown>;
  const id = Number(o.id ?? o.Id ?? 0);
  if (!Number.isFinite(id) || id <= 0) return null;
  return {
    id,
    name: String(o.name ?? o.Name ?? ""),
    displayName: String(o.displayName ?? o.DisplayName ?? ""),
    module: String(o.module ?? o.Module ?? ""),
    action: String(o.action ?? o.Action ?? ""),
  };
}

function unwrapDataOrRaw<T>(body: unknown): T | null {
  const fromBase = readBaseResponseData<T>(body);
  if (fromBase != null) return fromBase;
  if (body && typeof body === "object") {
    const raw = body as Record<string, unknown>;
    const normalized = (raw.data ?? raw.Data ?? body) as T;
    return normalized ?? null;
  }
  return (body as T) ?? null;
}

export async function getPermissions(): Promise<PermissionDto[]> {
  try {
    const response = await api.get<unknown>("/permissions");
    try {
      assertApiSuccess(response.data);
    } catch {
      // Some APIs may return raw array without BaseResponse wrapper.
    }

    const raw = unwrapDataOrRaw<unknown>(response.data);
    const fromBase = readBaseResponseList<unknown>(response.data);
    const list = Array.isArray(raw) ? raw : fromBase;
    return list.map(normalizePermission).filter((x): x is PermissionDto => x !== null);
  } catch (error) {
    throw toApiFormError(error, "Failed to load permissions");
  }
}

export async function getRolePermissionIds(roleId: number): Promise<number[]> {
  try {
    const response = await api.get<unknown>(`/roles/${roleId}/permissions`);
    try {
      assertApiSuccess(response.data);
    } catch {
      // Some APIs may return raw array without BaseResponse wrapper.
    }

    const data = unwrapDataOrRaw<unknown>(response.data);
    return Array.isArray(data) ? data.map((x) => Number(x)).filter((x) => Number.isFinite(x)) : [];
  } catch (error) {
    throw toApiFormError(error, "Failed to load role permissions");
  }
}

export async function updateRolePermissions(roleId: number, permissionIds: number[]): Promise<void> {
  try {
    const response = await api.put<unknown>(`/roles/${roleId}/permissions`, { permissionIds });
    // Treat successful HTTP and success!==false as successful.
    if (response.data && typeof response.data === "object") {
      const payload = response.data as { success?: boolean; Success?: boolean; message?: string; Message?: string };
      const success = payload.success ?? payload.Success;
      if (success === false) {
        throw new Error(payload.message || payload.Message || "Failed to update role permissions");
      }
    }
  } catch (error) {
    throw toApiFormError(error, "Failed to update role permissions");
  }
}
