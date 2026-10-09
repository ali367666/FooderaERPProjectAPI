import { api } from "@/lib/api";
import { readBaseResponseData, readBaseResponseList } from "@/lib/api-base-response";
import { toApiFormError } from "@/lib/api-error";

export type WorkstationItem = {
  id: number;
  restaurantId: number | null;
  restaurantName: string | null;
  name: string;
  isActive: boolean;
  lastSeenAtUtc: string | null;
};

export type WorkstationInput = {
  restaurantId: number | null;
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

function normalize(item: unknown): WorkstationItem | null {
  if (!item || typeof item !== "object") return null;
  const raw = item as Record<string, unknown>;
  const id = Number(pick(raw, "id", "Id"));
  if (!Number.isFinite(id) || id <= 0) return null;
  const restaurantId = pick<number | null>(raw, "restaurantId", "RestaurantId");
  return {
    id,
    restaurantId: typeof restaurantId === "number" ? restaurantId : null,
    restaurantName: pick<string | null>(raw, "restaurantName", "RestaurantName") ?? null,
    name: String(pick(raw, "name", "Name") ?? ""),
    isActive: Boolean(pick(raw, "isActive", "IsActive") ?? true),
    lastSeenAtUtc: pick<string | null>(raw, "lastSeenAtUtc", "LastSeenAtUtc") ?? null,
  };
}

export async function getWorkstations(restaurantId?: number): Promise<WorkstationItem[]> {
  try {
    const response = await api.get<unknown>("/workstations", {
      params: restaurantId ? { restaurantId } : undefined,
    });
    return unwrapList<unknown>(response.data)
      .map(normalize)
      .filter((x): x is WorkstationItem => x !== null);
  } catch (error) {
    throw toApiFormError(error, "Failed to fetch terminals");
  }
}

export async function createWorkstation(payload: WorkstationInput): Promise<WorkstationItem> {
  try {
    const response = await api.post<unknown>("/workstations", payload);
    const item = normalize(unwrapData<unknown>(response.data));
    if (!item) throw new Error("Invalid response from server.");
    return item;
  } catch (error) {
    throw toApiFormError(error, "Failed to create terminal");
  }
}

export async function updateWorkstation(id: number, payload: WorkstationInput): Promise<WorkstationItem> {
  try {
    const response = await api.put<unknown>(`/workstations/${id}`, payload);
    const item = normalize(unwrapData<unknown>(response.data));
    if (!item) throw new Error("Invalid response from server.");
    return item;
  } catch (error) {
    throw toApiFormError(error, "Failed to update terminal");
  }
}

export async function deleteWorkstation(id: number): Promise<void> {
  try {
    await api.delete<unknown>(`/workstations/${id}`);
  } catch (error) {
    throw toApiFormError(error, "Failed to delete terminal");
  }
}
