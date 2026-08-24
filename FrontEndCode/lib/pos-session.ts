import { getStoredToken } from "@/lib/auth-client";
import { getEmployees } from "@/lib/services/employee-service";

function base64UrlToUtf8(segment: string): string {
  const padded = segment + "=".repeat((4 - (segment.length % 4)) % 4);
  const base64 = padded.replace(/-/g, "+").replace(/_/g, "/");
  const binary = atob(base64);
  const bytes = new Uint8Array(binary.length);
  for (let i = 0; i < binary.length; i++) bytes[i] = binary.charCodeAt(i);
  return new TextDecoder("utf8").decode(bytes);
}

/** Reads the current User.Id (uid/sub claim) from the stored access token. */
export function getCurrentUserIdFromToken(): number | null {
  const token = getStoredToken();
  if (!token) return null;
  const parts = token.split(".");
  if (parts.length < 2) return null;
  try {
    const payload = JSON.parse(base64UrlToUtf8(parts[1])) as Record<string, unknown>;
    const raw = payload.uid ?? payload.sub;
    const id = Number(raw);
    return Number.isFinite(id) && id > 0 ? id : null;
  } catch {
    return null;
  }
}

/**
 * Orders are created against an Employee.Id (CreateOrderCommandHandler looks the
 * waiter up in the Employee table, not Users), so the logged-in User must be
 * linked to an Employee record before they can open an order.
 */
export async function getCurrentEmployeeId(): Promise<number | null> {
  const userId = getCurrentUserIdFromToken();
  if (!userId) return null;
  const employees = await getEmployees();
  const match = employees.find((e) => e.userId === userId);
  return match?.id ?? null;
}
