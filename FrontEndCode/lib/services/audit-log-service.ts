import { api } from "@/lib/api";
import { assertApiSuccess, readBaseResponseData, readBaseResponseList } from "@/lib/api-base-response";
import { ApiFormError, toApiFormError } from "@/lib/api-error";

export type AuditLogDto = {
  id: number;
  entityName: string;
  entityId: string;
  actionType: string;
  oldValues: string | null;
  newValues: string | null;
  message: string;
  userId: number | null;
  userFullName: string | null;
  userEmail: string | null;
  companyId: number | null;
  correlationId: string | null;
  isSuccess: boolean;
  createdAtUtc: string;
};

function pick<T>(raw: Record<string, unknown>, camel: string, pascal: string): T | undefined {
  if (camel in raw) return raw[camel] as T;
  if (pascal in raw) return raw[pascal] as T;
  return undefined;
}

export function normalizeAuditLog(raw: unknown): AuditLogDto | null {
  if (!raw || typeof raw !== "object") return null;
  const o = raw as Record<string, unknown>;
  const id = Number(pick(o, "id", "Id"));
  if (!Number.isFinite(id)) return null;
  return {
    id,
    entityName: String(pick(o, "entityName", "EntityName") ?? ""),
    entityId: String(pick(o, "entityId", "EntityId") ?? ""),
    actionType: String(pick(o, "actionType", "ActionType") ?? ""),
    oldValues: (pick<string | null>(o, "oldValues", "OldValues") ?? null) as string | null,
    newValues: (pick<string | null>(o, "newValues", "NewValues") ?? null) as string | null,
    message: String(pick(o, "message", "Message") ?? ""),
    userId:
      pick(o, "userId", "UserId") == null ? null : Number(pick(o, "userId", "UserId")),
    userFullName: (pick<string | null>(o, "userFullName", "UserFullName") ?? null) as string | null,
    userEmail: (pick<string | null>(o, "userEmail", "UserEmail") ?? null) as string | null,
    companyId:
      pick(o, "companyId", "CompanyId") == null ? null : Number(pick(o, "companyId", "CompanyId")),
    correlationId: (pick<string | null>(o, "correlationId", "CorrelationId") ?? null) as string | null,
    isSuccess: Boolean(pick(o, "isSuccess", "IsSuccess")),
    createdAtUtc: String(pick(o, "createdAtUtc", "CreatedAtUtc") ?? ""),
  };
}

export type AuditLogQueryParams = {
  entityName?: string;
  entityId?: string;
  actionType?: string;
  userId?: number;
  fromUtc?: string;
  toUtc?: string;
  search?: string;
};

export async function getAuditLogs(params?: AuditLogQueryParams): Promise<AuditLogDto[]> {
  try {
    const response = await api.get<unknown>("/AuditLogs", {
      params: {
        entityName: params?.entityName || undefined,
        entityId: params?.entityId || undefined,
        actionType: params?.actionType || undefined,
        userId: params?.userId,
        fromUtc: params?.fromUtc || undefined,
        toUtc: params?.toUtc || undefined,
        search: params?.search || undefined,
      },
    });
    assertApiSuccess(response.data);
    const list = readBaseResponseList<unknown>(response.data);
    return list.map(normalizeAuditLog).filter((x): x is AuditLogDto => x != null);
  } catch (e) {
    throw toApiFormError(e, "Failed to load audit logs");
  }
}

export async function getAuditLogById(id: number): Promise<AuditLogDto> {
  try {
    const response = await api.get<unknown>(`/AuditLogs/${id}`);
    assertApiSuccess(response.data);
    const data = readBaseResponseData<unknown>(response.data);
    const row = normalizeAuditLog(data);
    if (!row) throw new ApiFormError("Audit log not found");
    return row;
  } catch (e) {
    throw toApiFormError(e, "Failed to load audit log");
  }
}
