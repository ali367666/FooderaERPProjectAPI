import { api } from "@/lib/api";
import { readBaseResponseData, readBaseResponseList } from "@/lib/api-base-response";
import { toApiFormError } from "@/lib/api-error";

export type ScaleDevice = {
  id: number;
  restaurantId: number;
  name: string;
  brand: string | null;
  connectionInfo: string | null;
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

function normalize(item: unknown): ScaleDevice | null {
  if (!item || typeof item !== "object") return null;
  const raw = item as Record<string, unknown>;
  const id = Number(pick(raw, "id", "Id"));
  if (!Number.isFinite(id) || id <= 0) return null;
  return {
    id,
    restaurantId: Number(pick(raw, "restaurantId", "RestaurantId") ?? 0),
    name: String(pick(raw, "name", "Name") ?? ""),
    brand: (pick(raw, "brand", "Brand") as string | null | undefined) ?? null,
    connectionInfo:
      (pick(raw, "connectionInfo", "ConnectionInfo") as string | null | undefined) ?? null,
    isActive: Boolean(pick(raw, "isActive", "IsActive") ?? true),
  };
}

export type ScaleDeviceInput = {
  restaurantId: number;
  name: string;
  brand: string | null;
  connectionInfo: string | null;
  isActive: boolean;
};

export async function getScaleDevices(restaurantId: number): Promise<ScaleDevice[]> {
  try {
    const response = await api.get<unknown>("/ScaleDevices", { params: { restaurantId } });
    return unwrapList<unknown>(response.data).map(normalize).filter((x): x is ScaleDevice => x !== null);
  } catch (error) {
    throw toApiFormError(error, "Failed to fetch scale devices");
  }
}

export async function createScaleDevice(payload: ScaleDeviceInput): Promise<ScaleDevice> {
  try {
    const response = await api.post<unknown>("/ScaleDevices", payload);
    const device = normalize(unwrapData<unknown>(response.data));
    if (!device) throw new Error("Invalid response from server.");
    return device;
  } catch (error) {
    throw toApiFormError(error, "Failed to create scale device");
  }
}

export async function updateScaleDevice(id: number, payload: ScaleDeviceInput): Promise<ScaleDevice> {
  try {
    const response = await api.put<unknown>("/ScaleDevices", { id, ...payload });
    const device = normalize(unwrapData<unknown>(response.data));
    if (!device) throw new Error("Invalid response from server.");
    return device;
  } catch (error) {
    throw toApiFormError(error, "Failed to update scale device");
  }
}

export async function deleteScaleDevice(id: number): Promise<void> {
  try {
    await api.delete<unknown>(`/ScaleDevices/${id}`);
  } catch (error) {
    throw toApiFormError(error, "Failed to delete scale device");
  }
}
