import { api } from "@/lib/api";
import { readBaseResponseData, readBaseResponseList } from "@/lib/api-base-response";
import { toApiFormError } from "@/lib/api-error";

/** Matches Domain.Enums.FiscalDeviceProvider */
export const FiscalDeviceProvider = {
  Other: 1,
  TotalOmitech: 2,
  Smartfon: 3,
  Caspos: 4,
} as const;

export type FiscalDeviceProviderValue = (typeof FiscalDeviceProvider)[keyof typeof FiscalDeviceProvider];

export type FiscalDevice = {
  id: number;
  restaurantId: number;
  name: string;
  provider: FiscalDeviceProviderValue;
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

function normalizeProvider(raw: unknown): FiscalDeviceProviderValue {
  const n = Number(raw);
  if (
    n === FiscalDeviceProvider.Other ||
    n === FiscalDeviceProvider.TotalOmitech ||
    n === FiscalDeviceProvider.Smartfon ||
    n === FiscalDeviceProvider.Caspos
  ) {
    return n;
  }
  return FiscalDeviceProvider.Other;
}

function normalize(item: unknown): FiscalDevice | null {
  if (!item || typeof item !== "object") return null;
  const raw = item as Record<string, unknown>;
  const id = Number(pick(raw, "id", "Id"));
  if (!Number.isFinite(id) || id <= 0) return null;
  return {
    id,
    restaurantId: Number(pick(raw, "restaurantId", "RestaurantId") ?? 0),
    name: String(pick(raw, "name", "Name") ?? ""),
    provider: normalizeProvider(pick(raw, "provider", "Provider")),
    connectionInfo:
      (pick(raw, "connectionInfo", "ConnectionInfo") as string | null | undefined) ?? null,
    isActive: Boolean(pick(raw, "isActive", "IsActive") ?? true),
  };
}

export type FiscalDeviceInput = {
  restaurantId: number;
  name: string;
  provider: FiscalDeviceProviderValue;
  connectionInfo: string | null;
  isActive: boolean;
};

export async function getFiscalDevices(restaurantId: number): Promise<FiscalDevice[]> {
  try {
    const response = await api.get<unknown>("/FiscalDevices", { params: { restaurantId } });
    return unwrapList<unknown>(response.data).map(normalize).filter((x): x is FiscalDevice => x !== null);
  } catch (error) {
    throw toApiFormError(error, "Failed to fetch fiscal devices");
  }
}

export async function createFiscalDevice(payload: FiscalDeviceInput): Promise<FiscalDevice> {
  try {
    const response = await api.post<unknown>("/FiscalDevices", payload);
    const device = normalize(unwrapData<unknown>(response.data));
    if (!device) throw new Error("Invalid response from server.");
    return device;
  } catch (error) {
    throw toApiFormError(error, "Failed to create fiscal device");
  }
}

export async function updateFiscalDevice(id: number, payload: FiscalDeviceInput): Promise<FiscalDevice> {
  try {
    const response = await api.put<unknown>("/FiscalDevices", { id, ...payload });
    const device = normalize(unwrapData<unknown>(response.data));
    if (!device) throw new Error("Invalid response from server.");
    return device;
  } catch (error) {
    throw toApiFormError(error, "Failed to update fiscal device");
  }
}

export async function deleteFiscalDevice(id: number): Promise<void> {
  try {
    await api.delete<unknown>(`/FiscalDevices/${id}`);
  } catch (error) {
    throw toApiFormError(error, "Failed to delete fiscal device");
  }
}

export function fiscalDeviceProviderLabel(value: FiscalDeviceProviderValue): string {
  switch (value) {
    case FiscalDeviceProvider.TotalOmitech:
      return "Total Omitech";
    case FiscalDeviceProvider.Smartfon:
      return "Smartfon";
    case FiscalDeviceProvider.Caspos:
      return "Caspos";
    default:
      return "Digər";
  }
}
