import { api } from "@/lib/api";
import { readBaseResponseData, readBaseResponseList } from "@/lib/api-base-response";
import { toApiFormError } from "@/lib/api-error";

export type Counterparty = {
  id: number;
  name: string;
  phoneNumber: string | null;
  categoryId: number;
  categoryName: string;
  isActive: boolean;
  currentDebtAmount: number;
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

function normalize(item: unknown): Counterparty | null {
  if (!item || typeof item !== "object") return null;
  const raw = item as Record<string, unknown>;
  const id = Number(pick(raw, "id", "Id"));
  if (!Number.isFinite(id) || id <= 0) return null;
  return {
    id,
    name: String(pick(raw, "name", "Name") ?? ""),
    phoneNumber: (pick<string | null>(raw, "phoneNumber", "PhoneNumber") ?? null) as string | null,
    categoryId: Number(pick(raw, "categoryId", "CategoryId") ?? 0),
    categoryName: String(pick(raw, "categoryName", "CategoryName") ?? ""),
    isActive: Boolean(pick(raw, "isActive", "IsActive") ?? true),
    currentDebtAmount: Number(pick(raw, "currentDebtAmount", "CurrentDebtAmount") ?? 0),
  };
}

export type CounterpartyInput = {
  name: string;
  phoneNumber: string | null;
  categoryId: number;
  isActive: boolean;
};

export async function getCounterparties(): Promise<Counterparty[]> {
  try {
    const response = await api.get<unknown>("/Counterparties");
    return unwrapList<unknown>(response.data).map(normalize).filter((x): x is Counterparty => x !== null);
  } catch (error) {
    throw toApiFormError(error, "Failed to fetch counterparties");
  }
}

export async function createCounterparty(payload: CounterpartyInput): Promise<Counterparty> {
  try {
    const response = await api.post<unknown>("/Counterparties", payload);
    const item = normalize(unwrapData<unknown>(response.data));
    if (!item) throw new Error("Invalid response from server.");
    return item;
  } catch (error) {
    throw toApiFormError(error, "Failed to create counterparty");
  }
}

export async function updateCounterparty(id: number, payload: CounterpartyInput): Promise<Counterparty> {
  try {
    const response = await api.put<unknown>("/Counterparties", { id, ...payload });
    const item = normalize(unwrapData<unknown>(response.data));
    if (!item) throw new Error("Invalid response from server.");
    return item;
  } catch (error) {
    throw toApiFormError(error, "Failed to update counterparty");
  }
}

export async function deleteCounterparty(id: number): Promise<void> {
  try {
    await api.delete<unknown>(`/Counterparties/${id}`);
  } catch (error) {
    throw toApiFormError(error, "Failed to delete counterparty");
  }
}

export async function adjustCounterpartyDebt(id: number, newDebtAmount: number): Promise<Counterparty> {
  try {
    const response = await api.post<unknown>(`/Counterparties/${id}/adjust-debt`, { newDebtAmount });
    const item = normalize(unwrapData<unknown>(response.data));
    if (!item) throw new Error("Invalid response from server.");
    return item;
  } catch (error) {
    throw toApiFormError(error, "Failed to adjust debt");
  }
}
