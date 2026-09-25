import { api } from "@/lib/api";
import { ApiFormError, toApiFormError } from "@/lib/api-error";

/** Matches Domain.Enums.RestaurantTableType */
export const RestaurantTableType = {
  Masa: 1,
  Kabinet: 2,
  Delivery: 3,
} as const;

export type RestaurantTableTypeValue = (typeof RestaurantTableType)[keyof typeof RestaurantTableType];

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
  hourlyRate: number | null;
  note: string | null;
  type: RestaurantTableTypeValue;
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
  hourlyRate?: number | null;
  note?: string | null;
  type?: RestaurantTableTypeValue;
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
    hourlyRate: (() => {
      const v = raw.hourlyRate ?? raw.HourlyRate;
      return v == null ? null : Number(v);
    })(),
    note: (raw.note ?? raw.Note) != null ? String(raw.note ?? raw.Note) : null,
    type: (() => {
      const n = Number(raw.type ?? raw.Type);
      if (n === RestaurantTableType.Kabinet) return n;
      if (n === RestaurantTableType.Masa) return n;
      if (n === RestaurantTableType.Delivery) return n;
      return RestaurantTableType.Kabinet;
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
    throw toApiFormError(error, "Failed to fetch branch tables");
  }
}

export async function createRestaurantTable(
  data: RestaurantTableMutationInput,
): Promise<void> {
  try {
    const response = await api.post<unknown>("/RestaurantTables", { request: data });
    if (!response.data) {
      throw new ApiFormError("Failed to create branch table");
    }
  } catch (error) {
    throw toApiFormError(error, "Failed to create branch table");
  }
}

export async function updateRestaurantTable(
  id: number,
  data: RestaurantTableMutationInput,
): Promise<RestaurantTable> {
  try {
    const response = await api.put<unknown>(`/RestaurantTables/${id}`, data);
    const table = normalizeRestaurantTable(response.data);
    if (!table) {
      throw new ApiFormError("Failed to update branch table");
    }
    return table;
  } catch (error) {
    throw toApiFormError(error, "Failed to update branch table");
  }
}

export async function getRestaurantTableById(id: number): Promise<RestaurantTable> {
  try {
    const response = await api.get<unknown>(`/RestaurantTables/${id}`);
    const table = normalizeRestaurantTable(response.data);
    if (!table) throw new ApiFormError("Branch table not found");
    return table;
  } catch (error) {
    throw toApiFormError(error, "Failed to fetch branch table");
  }
}

export async function ensureStoreSaleTable(restaurantId: number): Promise<number> {
  try {
    const response = await api.post<{ success?: boolean; message?: string; data?: number }>(
      "/RestaurantTables/ensure-store-sale-table",
      null,
      { params: { restaurantId } },
    );
    const payload = response.data;
    if (payload?.success === false || payload?.data == null) {
      throw new ApiFormError(payload?.message || "Failed to ensure store sale table");
    }
    return Number(payload.data);
  } catch (error) {
    throw toApiFormError(error, "Failed to ensure store sale table");
  }
}

export async function deleteRestaurantTable(id: number): Promise<void> {
  try {
    await api.delete(`/RestaurantTables/${id}`);
  } catch (error) {
    throw toApiFormError(error, "Failed to delete branch table");
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
