import { api } from "@/lib/api";
import { assertApiSuccess, readBaseResponseData, readBaseResponseList } from "@/lib/api-base-response";
import { ApiFormError, toApiFormError } from "@/lib/api-error";

/** Domain.Enums.WarehouseType */
export const WarehouseType = {
  HeadOffice: 1,
  Restaurant: 2,
  Vehicle: 3,
} as const;
export type WarehouseTypeValue = (typeof WarehouseType)[keyof typeof WarehouseType];

export type Warehouse = {
  id: number;
  name: string;
  type: WarehouseTypeValue;
  companyId: number;
  restaurantId: number | null;
  restaurantName: string | null;
  responsibleEmployeeId: number | null;
  responsibleEmployeeFullName: string | null;
  driverUserId: number | null;
  driverFullName: string | null;
};

export type WarehouseMutationInput = {
  name: string;
  type: WarehouseTypeValue;
  companyId: number;
  restaurantId?: number | null;
  responsibleEmployeeId?: number | null;
  /** Linked application user id for vehicle warehouse driver */
  driverUserId?: number | null;
};

/** Add/Edit warehouse form — all optional IDs follow API null rules */
export type WarehouseFormValues = {
  name: string;
  type: WarehouseTypeValue;
  companyId: number;
  restaurantId: number | null;
  responsibleEmployeeId: number | null;
  driverUserId: number | null;
};

function normalizeWarehouse(item: unknown): Warehouse | null {
  if (!item || typeof item !== "object") return null;
  const raw = item as Record<string, unknown>;
  const id = Number(raw.id ?? raw.Id);
  if (!Number.isFinite(id) || id <= 0) return null;
  const type = Number(raw.type ?? raw.Type ?? WarehouseType.HeadOffice);
  return {
    id,
    name: String(raw.name ?? raw.Name ?? ""),
    type: type as WarehouseTypeValue,
    companyId: Number(raw.companyId ?? raw.CompanyId ?? 0),
    restaurantId:
      raw.restaurantId === undefined && raw.RestaurantId === undefined
        ? null
        : Number(raw.restaurantId ?? raw.RestaurantId) || null,
    restaurantName:
      raw.restaurantName !== undefined || raw.RestaurantName !== undefined
        ? String(raw.restaurantName ?? raw.RestaurantName ?? "") || null
        : null,
    responsibleEmployeeId:
      raw.responsibleEmployeeId === undefined && raw.ResponsibleEmployeeId === undefined
        ? null
        : Number(raw.responsibleEmployeeId ?? raw.ResponsibleEmployeeId) || null,
    responsibleEmployeeFullName:
      raw.responsibleEmployeeFullName !== undefined || raw.ResponsibleEmployeeFullName !== undefined
        ? String(raw.responsibleEmployeeFullName ?? raw.ResponsibleEmployeeFullName ?? "") || null
        : null,
    driverUserId:
      raw.driverUserId === undefined && raw.DriverUserId === undefined
        ? null
        : Number(raw.driverUserId ?? raw.DriverUserId) || null,
    driverFullName:
      raw.driverFullName !== undefined || raw.DriverFullName !== undefined
        ? String(raw.driverFullName ?? raw.DriverFullName ?? "") || null
        : null,
  };
}

export function warehouseTypeLabel(type: WarehouseTypeValue): string {
  switch (type) {
    case WarehouseType.HeadOffice:
      return "Head office";
    case WarehouseType.Restaurant:
      return "Restaurant";
    case WarehouseType.Vehicle:
      return "Vehicle";
    default:
      return String(type);
  }
}

export async function getWarehouses(): Promise<Warehouse[]> {
  try {
    const response = await api.get<unknown>("/Warehouses");
    assertApiSuccess(response.data);
    const list = readBaseResponseList<unknown>(response.data);
    return list.map((row) => normalizeWarehouse(row)).filter((row): row is Warehouse => row !== null);
  } catch (error) {
    throw toApiFormError(error, "Failed to fetch warehouses");
  }
}

export async function getWarehouseById(id: number): Promise<Warehouse> {
  try {
    const response = await api.get<unknown>(`/Warehouses/${id}`);
    assertApiSuccess(response.data);
    const data = readBaseResponseData<unknown>(response.data);
    const row = normalizeWarehouse(data);
    if (!row) throw new ApiFormError("Warehouse not found");
    return row;
  } catch (error) {
    throw toApiFormError(error, "Failed to load warehouse");
  }
}

export async function createWarehouse(payload: WarehouseMutationInput): Promise<void> {
  try {
    const response = await api.post<unknown>("/Warehouses", {
      name: payload.name.trim(),
      type: payload.type,
      companyId: payload.companyId,
      restaurantId: payload.restaurantId ?? null,
      responsibleEmployeeId: payload.responsibleEmployeeId ?? null,
      driverUserId: payload.driverUserId ?? null,
    });
    assertApiSuccess(response.data);
  } catch (error) {
    throw toApiFormError(error, "Failed to create warehouse");
  }
}

export async function updateWarehouse(id: number, payload: WarehouseMutationInput): Promise<void> {
  try {
    const response = await api.put<unknown>(`/Warehouses/${id}`, {
      name: payload.name.trim(),
      type: payload.type,
      companyId: payload.companyId,
      restaurantId: payload.restaurantId ?? null,
      responsibleEmployeeId: payload.responsibleEmployeeId ?? null,
      driverUserId: payload.driverUserId ?? null,
    });
    assertApiSuccess(response.data);
  } catch (error) {
    throw toApiFormError(error, "Failed to update warehouse");
  }
}

export async function deleteWarehouse(id: number): Promise<void> {
  try {
    const response = await api.delete<unknown>(`/Warehouses/${id}`);
    assertApiSuccess(response.data);
  } catch (error) {
    throw toApiFormError(error, "Failed to delete warehouse");
  }
}
