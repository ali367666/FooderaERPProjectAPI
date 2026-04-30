import { ApiFormError } from "@/lib/api-error";

/** Backend `BaseResponse<T>` / `BaseResponse` shape (camelCase JSON). */
export function assertApiSuccess(body: unknown): void {
  if (body && typeof body === "object" && "success" in body) {
    const o = body as { success?: boolean; message?: string; Message?: string };
    if (o.success === false) {
      throw new ApiFormError(String(o.message ?? o.Message ?? "Request failed"));
    }
  }
}

export function readBaseResponseData<T>(body: unknown): T | null {
  if (body == null) return null;
  if (typeof body !== "object") return null;
  const o = body as { data?: T; Data?: T; success?: boolean };
  if ("success" in o && o.success === false) return null;
  if ("data" in o) return (o.data ?? null) as T | null;
  if ("Data" in o) return (o.Data ?? null) as T | null;
  return null;
}

export function readBaseResponseList<T>(body: unknown): T[] {
  const data = readBaseResponseData<unknown>(body);
  if (Array.isArray(data)) return data as T[];
  return [];
}
