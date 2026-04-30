import { api } from "@/lib/api";
import { ApiFormError, toApiFormError } from "@/lib/api-error";

export type RestaurantTable = {
  id: number;
  restaurantId: number;
  restaurantName?: string;
  name: string;
  capacity: number;
  isActive: boolean;
  isOccupied: boolean;
};

export type RestaurantTableMutationInput = {
  restaurantId: number;
  name: string;
  capacity: number;
  isActive?: boolean;
};

function normalizeRestaurantTable(item: unknown): RestaurantTable | null {
  if (!item || typeof item !== "object") return null;
  const raw = item as Record<string, unknown>;
  const id = Number(raw.id ?? raw.Id);
  if (!Number.isFinite(id) || id <= 0) return null;

  return {
    id,
    restaurantId: Number(raw.restaurantId ?? raw.RestaurantId ?? 0),
    restaurantName: String(raw.restaurantName ?? raw.RestaurantName ?? ""),
    name: String(raw.name ?? raw.Name ?? ""),
    capacity: Number(raw.capacity ?? raw.Capacity ?? 0),
    isActive: Boolean(raw.isActive ?? raw.IsActive ?? true),
    isOccupied: Boolean(raw.isOccupied ?? raw.IsOccupied ?? false),
  };
}

export async function getRestaurantTables(): Promise<RestaurantTable[]> {
  try {
    const response = await api.get<unknown[]>("/RestaurantTables");
    const list = Array.isArray(response.data) ? response.data : [];
    return list
      .map((table) => normalizeRestaurantTable(table))
      .filter((table): table is RestaurantTable => table !== null);
  } catch (error) {
    throw toApiFormError(error, "Failed to fetch restaurant tables");
  }
}

export async function createRestaurantTable(
  data: RestaurantTableMutationInput,
): Promise<void> {
  try {
    const response = await api.post<unknown>("/RestaurantTables", { request: data });
    if (!response.data) {
      throw new ApiFormError("Failed to create restaurant table");
    }
  } catch (error) {
    throw toApiFormError(error, "Failed to create restaurant table");
  }
}

export async function updateRestaurantTable(
  id: number,
  data: RestaurantTableMutationInput,
): Promise<void> {
  try {
    const response = await api.put<unknown>(`/RestaurantTables/${id}`, data);
    if (!response.data) {
      throw new ApiFormError("Failed to update restaurant table");
    }
  } catch (error) {
    throw toApiFormError(error, "Failed to update restaurant table");
  }
}

export async function deleteRestaurantTable(id: number): Promise<void> {
  try {
    await api.delete(`/RestaurantTables/${id}`);
  } catch (error) {
    throw toApiFormError(error, "Failed to delete restaurant table");
  }
}
