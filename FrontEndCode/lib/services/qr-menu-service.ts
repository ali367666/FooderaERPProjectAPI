import { api } from "@/lib/api";
import { toApiFormError } from "@/lib/api-error";

export type QrMenuTable = {
  id: number;
  name: string;
  sectionId: number | null;
  sectionName: string | null;
  qrToken: string;
};

export type QrMenuStatus = {
  restaurantId: number;
  publishedAtUtc: string | null;
  hasPendingChanges: boolean;
  tables: QrMenuTable[];
};

function pick<T>(o: Record<string, unknown>, camel: string, pascal: string): T | undefined {
  if (o[camel] !== undefined) return o[camel] as T;
  if (o[pascal] !== undefined) return o[pascal] as T;
  return undefined;
}

function normalizeStatus(raw: Record<string, unknown>): QrMenuStatus {
  const tables = pick<unknown[]>(raw, "tables", "Tables") ?? [];
  return {
    restaurantId: Number(pick(raw, "restaurantId", "RestaurantId") ?? 0),
    publishedAtUtc: (pick(raw, "publishedAtUtc", "PublishedAtUtc") as string | null | undefined) ?? null,
    hasPendingChanges: Boolean(pick(raw, "hasPendingChanges", "HasPendingChanges") ?? false),
    tables: tables.map((t) => {
      const r = t as Record<string, unknown>;
      return {
        id: Number(pick(r, "id", "Id") ?? 0),
        name: String(pick(r, "name", "Name") ?? ""),
        sectionId: (pick(r, "sectionId", "SectionId") as number | null | undefined) ?? null,
        sectionName: (pick(r, "sectionName", "SectionName") as string | null | undefined) ?? null,
        qrToken: String(pick(r, "qrToken", "QrToken") ?? ""),
      };
    }),
  };
}

/** When the QR menu was last published, whether it is stale, and every table's own QR token. */
export async function getQrMenuStatus(restaurantId: number): Promise<QrMenuStatus> {
  try {
    const response = await api.get<unknown>(`/qr-menu/restaurants/${restaurantId}`);
    return normalizeStatus(response.data as Record<string, unknown>);
  } catch (error) {
    throw toApiFormError(error, "QR menyu məlumatı yüklənmədi");
  }
}

/** "QR-u yenilə": the QR codes start showing the current products and prices. */
export async function publishQrMenu(restaurantId: number): Promise<QrMenuStatus> {
  try {
    const response = await api.post<unknown>(`/qr-menu/restaurants/${restaurantId}/publish`);
    return normalizeStatus(response.data as Record<string, unknown>);
  } catch (error) {
    throw toApiFormError(error, "QR menyu yenilənmədi");
  }
}
