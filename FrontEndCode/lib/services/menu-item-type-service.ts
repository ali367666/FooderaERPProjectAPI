import { api } from "@/lib/api";
import { readBaseResponseData, readBaseResponseList } from "@/lib/api-base-response";
import { toApiFormError } from "@/lib/api-error";

export type MenuItemTypeOption = {
  id: number;
  name: string;
  isActive: boolean;
};

function pick<T>(o: Record<string, unknown>, camel: string, pascal: string): T | undefined {
  if (o[camel] !== undefined) return o[camel] as T;
  if (o[pascal] !== undefined) return o[pascal] as T;
  return undefined;
}

function unwrapList<T>(body: unknown): T[] {
  const list = readBaseResponseList<T>(body);
  if (list.length > 0) return list;
  if (Array.isArray(body)) return body as T[];
  return [];
}

function unwrapData<T>(body: unknown): T | null {
  const data = readBaseResponseData<T>(body);
  if (data != null) return data;
  if (body && typeof body === "object" && !("success" in (body as Record<string, unknown>))) {
    return body as T;
  }
  return null;
}

function normalize(item: unknown): MenuItemTypeOption | null {
  if (!item || typeof item !== "object") return null;
  const raw = item as Record<string, unknown>;
  const id = Number(pick(raw, "id", "Id"));
  if (!Number.isFinite(id) || id <= 0) return null;
  return {
    id,
    name: String(pick(raw, "name", "Name") ?? ""),
    isActive: Boolean(pick(raw, "isActive", "IsActive") ?? true),
  };
}

export async function getMenuItemTypes(): Promise<MenuItemTypeOption[]> {
  try {
    const response = await api.get<unknown>("/MenuItemTypes");
    return unwrapList<unknown>(response.data).map(normalize).filter((x): x is MenuItemTypeOption => x !== null);
  } catch (error) {
    throw toApiFormError(error, "Failed to fetch menu item types");
  }
}

export async function createMenuItemType(payload: { name: string; isActive: boolean }): Promise<MenuItemTypeOption> {
  try {
    const response = await api.post<unknown>("/MenuItemTypes", payload);
    const item = normalize(unwrapData<unknown>(response.data));
    if (!item) throw new Error("Invalid response from server.");
    return item;
  } catch (error) {
    throw toApiFormError(error, "Failed to create item type");
  }
}

export async function updateMenuItemType(id: number, payload: { name: string; isActive: boolean }): Promise<MenuItemTypeOption> {
  try {
    const response = await api.put<unknown>("/MenuItemTypes", { id, ...payload });
    const item = normalize(unwrapData<unknown>(response.data));
    if (!item) throw new Error("Invalid response from server.");
    return item;
  } catch (error) {
    throw toApiFormError(error, "Failed to update item type");
  }
}

export async function deleteMenuItemType(id: number): Promise<void> {
  try {
    await api.delete<unknown>(`/MenuItemTypes/${id}`);
  } catch (error) {
    throw toApiFormError(error, "Failed to delete item type");
  }
}
