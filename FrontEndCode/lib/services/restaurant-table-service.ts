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
  posX: number;
  posY: number;
  width: number;
  height: number;
  shape: "square" | "round" | "rectangle";
  rotation: number;
  sectionId: number | null;
};

export type TableLayoutUpdate = {
  posX: number;
  posY: number;
  width: number;
  height: number;
  shape: "square" | "round" | "rectangle";
  rotation: number;
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
    posX: Number(raw.posX ?? raw.PosX ?? 0),
    posY: Number(raw.posY ?? raw.PosY ?? 0),
    width: Number(raw.width ?? raw.Width ?? 80),
    height: Number(raw.height ?? raw.Height ?? 80),
    shape: (String(raw.shape ?? raw.Shape ?? "square")) as "square" | "round" | "rectangle",
    rotation: Number(raw.rotation ?? raw.Rotation ?? 0),
    sectionId: (() => {
      const v = raw.sectionId ?? raw.SectionId;
      return v == null ? null : Number(v);
    })(),
  };
}

export async function updateTableSection(id: number, sectionId: number | null): Promise<void> {
  try {
    await api.put<unknown>(`/RestaurantTables/${id}/section`, null, { params: { sectionId: sectionId ?? undefined } });
  } catch (error) {
    throw toApiFormError(error, "Failed to update table section");
  }
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

export async function updateTableLayout(
  id: number,
  data: TableLayoutUpdate,
): Promise<void> {
  try {
    await api.put(`/RestaurantTables/${id}/layout`, data);
  } catch (error) {
    throw toApiFormError(error, "Failed to update table layout");
  }
}
