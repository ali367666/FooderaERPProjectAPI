import { api } from "@/lib/api";
import { readBaseResponseData, readBaseResponseList } from "@/lib/api-base-response";
import { toApiFormError } from "@/lib/api-error";

export type Printer = {
  id: number;
  restaurantId: number;
  name: string;
  stationTypeId: number;
  stationTypeName: string;
  ipAddress: string;
  port: number;
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

function normalize(item: unknown): Printer | null {
  if (!item || typeof item !== "object") return null;
  const raw = item as Record<string, unknown>;
  const id = Number(pick(raw, "id", "Id"));
  if (!Number.isFinite(id) || id <= 0) return null;
  return {
    id,
    restaurantId: Number(pick(raw, "restaurantId", "RestaurantId") ?? 0),
    name: String(pick(raw, "name", "Name") ?? ""),
    stationTypeId: Number(pick(raw, "stationTypeId", "StationTypeId") ?? 0),
    stationTypeName: String(pick(raw, "stationTypeName", "StationTypeName") ?? ""),
    ipAddress: String(pick(raw, "ipAddress", "IpAddress") ?? ""),
    port: Number(pick(raw, "port", "Port") ?? 9100),
    isActive: Boolean(pick(raw, "isActive", "IsActive") ?? true),
  };
}

export type PrinterInput = {
  restaurantId: number;
  name: string;
  stationTypeId: number;
  ipAddress: string;
  port: number;
  isActive: boolean;
};

export async function getPrinters(restaurantId: number): Promise<Printer[]> {
  try {
    const response = await api.get<unknown>("/Printers", { params: { restaurantId } });
    return unwrapList<unknown>(response.data).map(normalize).filter((x): x is Printer => x !== null);
  } catch (error) {
    throw toApiFormError(error, "Failed to fetch printers");
  }
}

export async function createPrinter(payload: PrinterInput): Promise<Printer> {
  try {
    const response = await api.post<unknown>("/Printers", payload);
    const printer = normalize(unwrapData<unknown>(response.data));
    if (!printer) throw new Error("Invalid response from server.");
    return printer;
  } catch (error) {
    throw toApiFormError(error, "Failed to create printer");
  }
}

export async function updatePrinter(id: number, payload: PrinterInput): Promise<Printer> {
  try {
    const response = await api.put<unknown>("/Printers", { id, ...payload });
    const printer = normalize(unwrapData<unknown>(response.data));
    if (!printer) throw new Error("Invalid response from server.");
    return printer;
  } catch (error) {
    throw toApiFormError(error, "Failed to update printer");
  }
}

export async function deletePrinter(id: number): Promise<void> {
  try {
    await api.delete<unknown>(`/Printers/${id}`);
  } catch (error) {
    throw toApiFormError(error, "Failed to delete printer");
  }
}

export async function printToPrinter(id: number, content: string): Promise<void> {
  try {
    await api.post<unknown>(`/Printers/${id}/print`, { content });
  } catch (error) {
    throw toApiFormError(error, "Printerə çap göndərilmədi");
  }
}
