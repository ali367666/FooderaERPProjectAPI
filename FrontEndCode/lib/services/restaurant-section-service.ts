import { api } from "@/lib/api";
import { readBaseResponseData, readBaseResponseList } from "@/lib/api-base-response";
import { toApiFormError } from "@/lib/api-error";
import { RestaurantTableType, type RestaurantTableTypeValue } from "@/lib/services/restaurant-table-service";

export type RestaurantSection = {
  id: number;
  restaurantId: number;
  name: string;
  isActive: boolean;
  type: RestaurantTableTypeValue;
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

function normalize(item: unknown): RestaurantSection | null {
  if (!item || typeof item !== "object") return null;
  const raw = item as Record<string, unknown>;
  const id = Number(pick(raw, "id", "Id"));
  if (!Number.isFinite(id) || id <= 0) return null;
  return {
    id,
    restaurantId: Number(pick(raw, "restaurantId", "RestaurantId") ?? 0),
    name: String(pick(raw, "name", "Name") ?? ""),
    isActive: Boolean(pick(raw, "isActive", "IsActive") ?? true),
    type: (() => {
      const n = Number(pick(raw, "type", "Type"));
      if (n === RestaurantTableType.Kabinet) return n;
      if (n === RestaurantTableType.Masa) return n;
      return RestaurantTableType.Kabinet;
    })(),
  };
}

export async function getRestaurantSections(restaurantId: number): Promise<RestaurantSection[]> {
  try {
    const response = await api.get<unknown>("/RestaurantSections", { params: { restaurantId } });
    return unwrapList<unknown>(response.data).map(normalize).filter((x): x is RestaurantSection => x !== null);
  } catch (error) {
    throw toApiFormError(error, "Failed to fetch restaurant sections");
  }
}

export async function createRestaurantSection(payload: { restaurantId: number; name: string; isActive: boolean; type: RestaurantTableTypeValue }): Promise<RestaurantSection> {
  try {
    const response = await api.post<unknown>("/RestaurantSections", payload);
    const data = unwrapData<unknown>(response.data);
    const section = normalize(data);
    if (!section) throw new Error("Invalid response from server.");
    return section;
  } catch (error) {
    throw toApiFormError(error, "Failed to create section");
  }
}

export async function updateRestaurantSection(payload: { id: number; name: string; isActive: boolean; type: RestaurantTableTypeValue }): Promise<RestaurantSection> {
  try {
    const response = await api.put<unknown>("/RestaurantSections", payload);
    const data = unwrapData<unknown>(response.data);
    const section = normalize(data);
    if (!section) throw new Error("Invalid response from server.");
    return section;
  } catch (error) {
    throw toApiFormError(error, "Failed to update section");
  }
}

export async function deleteRestaurantSection(id: number): Promise<void> {
  try {
    await api.delete<unknown>(`/RestaurantSections/${id}`);
  } catch (error) {
    throw toApiFormError(error, "Failed to delete section");
  }
}
