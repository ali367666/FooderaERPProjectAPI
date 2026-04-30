import { api } from "@/lib/api";
import { assertApiSuccess, readBaseResponseData, readBaseResponseList } from "@/lib/api-base-response";
import { ApiFormError, toApiFormError } from "@/lib/api-error";

export type NotificationDto = {
  id: number;
  companyId: number;
  userId: number;
  title: string;
  message: string;
  isRead: boolean;
  type: string;
  referenceId: number | null;
  referenceType: string | null;
  createdAtUtc: string;
};

function pick<T>(raw: Record<string, unknown>, camel: string, pascal: string): T | undefined {
  if (camel in raw) return raw[camel] as T;
  if (pascal in raw) return raw[pascal] as T;
  return undefined;
}

export function normalizeNotification(raw: unknown): NotificationDto | null {
  if (!raw || typeof raw !== "object") return null;
  const o = raw as Record<string, unknown>;
  const id = Number(pick(o, "id", "Id"));
  if (!Number.isFinite(id)) return null;
  return {
    id,
    companyId: Number(pick(o, "companyId", "CompanyId") ?? 0),
    userId: Number(pick(o, "userId", "UserId") ?? 0),
    title: String(pick(o, "title", "Title") ?? ""),
    message: String(pick(o, "message", "Message") ?? ""),
    isRead: Boolean(pick(o, "isRead", "IsRead")),
    type: String(pick(o, "type", "Type") ?? ""),
    referenceId: (() => {
      const v = pick<unknown>(o, "referenceId", "ReferenceId");
      if (v == null || v === "") return null;
      const n = Number(v);
      return Number.isFinite(n) ? n : null;
    })(),
    referenceType: (pick<string | null>(o, "referenceType", "ReferenceType") ?? null) as string | null,
    createdAtUtc: String(pick(o, "createdAtUtc", "CreatedAtUtc") ?? ""),
  };
}

/** Omit or non-positive = all notifications for the current user (any company). */
function notificationParams(companyId?: number | null): { companyId: number } | undefined {
  if (typeof companyId === "number" && companyId > 0) return { companyId };
  return undefined;
}

export async function getNotifications(companyId?: number | null): Promise<NotificationDto[]> {
  try {
    const p = notificationParams(companyId);
    const response = await api.get<unknown>("/Notifications", p ? { params: p } : {});
    assertApiSuccess(response.data);
    const list = readBaseResponseList<unknown>(response.data);
    return list.map(normalizeNotification).filter((x): x is NotificationDto => x != null);
  } catch (e) {
    throw toApiFormError(e, "Failed to load notifications");
  }
}

export async function getNotificationById(id: number, companyId?: number | null): Promise<NotificationDto> {
  try {
    const p = notificationParams(companyId);
    const response = await api.get<unknown>(`/Notifications/${id}`, p ? { params: p } : {});
    assertApiSuccess(response.data);
    const data = readBaseResponseData<unknown>(response.data);
    const row = normalizeNotification(data);
    if (!row) throw new ApiFormError("Notification not found");
    return row;
  } catch (e) {
    throw toApiFormError(e, "Failed to load notification");
  }
}

export async function getUnreadNotificationCount(companyId?: number | null): Promise<number> {
  try {
    const p = notificationParams(companyId);
    const response = await api.get<unknown>("/Notifications/unread-count", p ? { params: p } : {});
    assertApiSuccess(response.data);
    const n = readBaseResponseData<number>(response.data);
    return typeof n === "number" && Number.isFinite(n) ? n : 0;
  } catch (e) {
    throw toApiFormError(e, "Failed to load unread count");
  }
}

export async function markNotificationRead(id: number, companyId?: number | null): Promise<void> {
  try {
    const p = notificationParams(companyId);
    const response = await api.post<unknown>(`/Notifications/${id}/read`, {}, p ? { params: p } : {});
    assertApiSuccess(response.data);
  } catch (e) {
    throw toApiFormError(e, "Failed to mark as read");
  }
}

export async function markNotificationUnread(id: number, companyId?: number | null): Promise<void> {
  try {
    const p = notificationParams(companyId);
    const response = await api.post<unknown>(`/Notifications/${id}/unread`, {}, p ? { params: p } : {});
    assertApiSuccess(response.data);
  } catch (e) {
    throw toApiFormError(e, "Failed to mark as unread");
  }
}

export async function deleteNotification(id: number, companyId?: number | null): Promise<void> {
  try {
    const p = notificationParams(companyId);
    const response = await api.delete<unknown>(`/Notifications/${id}`, p ? { params: p } : {});
    assertApiSuccess(response.data);
  } catch (e) {
    throw toApiFormError(e, "Failed to delete notification");
  }
}

/** Build dashboard link from backend reference metadata when possible. */
export function getNotificationDocumentHref(n: Pick<NotificationDto, "referenceType" | "referenceId">): string | null {
  const id = n.referenceId;
  if (id == null || !Number.isFinite(id)) return null;
  const t = (n.referenceType ?? "").toLowerCase().replace(/\s+/g, "");
  if (t.includes("stockrequest")) return `/dashboard/stock-requests/${id}?mode=view`;
  if (t.includes("warehousetransfer")) return `/dashboard/warehouse-transfers/${id}?mode=view`;
  if (t.includes("warehousestock")) return `/dashboard/warehouse-stock-documents`;
  if (t.includes("order")) return `/dashboard/orders`;
  return null;
}
