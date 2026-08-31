import { api } from "@/lib/api";
import { readBaseResponseData, readBaseResponseList } from "@/lib/api-base-response";
import { toApiFormError } from "@/lib/api-error";

export type CounterpartyCategory = {
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

function normalize(item: unknown): CounterpartyCategory | null {
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

export async function getCounterpartyCategories(): Promise<CounterpartyCategory[]> {
  try {
    const response = await api.get<unknown>("/CounterpartyCategories");
    return unwrapList<unknown>(response.data).map(normalize).filter((x): x is CounterpartyCategory => x !== null);
  } catch (error) {
    throw toApiFormError(error, "Failed to fetch counterparty categories");
  }
}

export async function createCounterpartyCategory(payload: { name: string; isActive: boolean }): Promise<CounterpartyCategory> {
  try {
    const response = await api.post<unknown>("/CounterpartyCategories", payload);
    const item = normalize(unwrapData<unknown>(response.data));
    if (!item) throw new Error("Invalid response from server.");
    return item;
  } catch (error) {
    throw toApiFormError(error, "Failed to create category");
  }
}

export async function updateCounterpartyCategory(id: number, payload: { name: string; isActive: boolean }): Promise<CounterpartyCategory> {
  try {
    const response = await api.put<unknown>("/CounterpartyCategories", { id, ...payload });
    const item = normalize(unwrapData<unknown>(response.data));
    if (!item) throw new Error("Invalid response from server.");
    return item;
  } catch (error) {
    throw toApiFormError(error, "Failed to update category");
  }
}

export async function deleteCounterpartyCategory(id: number): Promise<void> {
  try {
    await api.delete<unknown>(`/CounterpartyCategories/${id}`);
  } catch (error) {
    throw toApiFormError(error, "Failed to delete category");
  }
}
