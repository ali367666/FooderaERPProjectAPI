import { getStoredToken } from "@/lib/auth-client";
import { getEmployees, type Employee } from "@/lib/services/employee-service";

function base64UrlToUtf8(segment: string): string {
  const padded = segment + "=".repeat((4 - (segment.length % 4)) % 4);
  const base64 = padded.replace(/-/g, "+").replace(/_/g, "/");
  const binary = atob(base64);
  const bytes = new Uint8Array(binary.length);
  for (let i = 0; i < binary.length; i++) bytes[i] = binary.charCodeAt(i);
  return new TextDecoder("utf8").decode(bytes);
}

function readTokenPayload(): Record<string, unknown> | null {
  const token = getStoredToken();
  if (!token) return null;
  const parts = token.split(".");
  if (parts.length < 2) return null;
  try {
    return JSON.parse(base64UrlToUtf8(parts[1])) as Record<string, unknown>;
  } catch {
    return null;
  }
}

/** Reads the current User.Id (uid/sub claim) from the stored access token. */
export function getCurrentUserIdFromToken(): number | null {
  const payload = readTokenPayload();
  if (!payload) return null;
  const id = Number(payload.uid ?? payload.sub);
  return Number.isFinite(id) && id > 0 ? id : null;
}

/** The signed-in user's UserName (ClaimTypes.Name, under whichever key the token handler wrote it). */
export function getCurrentUserNameFromToken(): string | null {
  const payload = readTokenPayload();
  if (!payload) return null;
  const raw =
    payload["http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name"] ?? payload.unique_name ?? payload.name;
  return typeof raw === "string" && raw.trim() ? raw.trim() : null;
}

/** The Employee record linked to the signed-in User, if any. */
export async function getCurrentEmployee(): Promise<Employee | null> {
  const userId = getCurrentUserIdFromToken();
  if (!userId) return null;
  const employees = await getEmployees();
  return employees.find((e) => e.userId === userId) ?? null;
}

/**
 * Orders are created against an Employee.Id (CreateOrderCommandHandler looks the
 * waiter up in the Employee table, not Users), so the logged-in User must be
 * linked to an Employee record before they can open an order.
 */
export async function getCurrentEmployeeId(): Promise<number | null> {
  return (await getCurrentEmployee())?.id ?? null;
}
