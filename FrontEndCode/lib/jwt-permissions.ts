/**
 * Reads Permission claims from the access JWT payload (client-side UI only; not a security boundary).
 * ASP.NET Core typically serializes repeated claims as a JSON array of strings.
 */

function base64UrlToUtf8(segment: string): string {
  const padded = segment + "=".repeat((4 - (segment.length % 4)) % 4);
  const base64 = padded.replace(/-/g, "+").replace(/_/g, "/");
  const binary = atob(base64);
  const bytes = new Uint8Array(binary.length);
  for (let i = 0; i < binary.length; i++) bytes[i] = binary.charCodeAt(i);
  return new TextDecoder("utf8").decode(bytes);
}

export function getPermissionClaimsFromToken(token: string | null | undefined): string[] {
  if (!token || typeof token !== "string") return [];
  const parts = token.split(".");
  if (parts.length < 2) return [];
  try {
    const json = base64UrlToUtf8(parts[1]);
    const payload = JSON.parse(json) as Record<string, unknown>;
    const raw = payload.Permission ?? payload.permission;
    if (Array.isArray(raw)) {
      return raw.filter((x): x is string => typeof x === "string");
    }
    if (typeof raw === "string") return [raw];
    return [];
  } catch {
    return [];
  }
}

export function getRoleClaimsFromToken(token: string | null | undefined): string[] {
  if (!token || typeof token !== "string") return [];
  const parts = token.split(".");
  if (parts.length < 2) return [];
  try {
    const json = base64UrlToUtf8(parts[1]);
    const payload = JSON.parse(json) as Record<string, unknown>;
    const raw = payload.role ?? payload.roles ?? payload.Role ?? payload.Roles;
    if (Array.isArray(raw)) return raw.filter((x): x is string => typeof x === "string");
    if (typeof raw === "string") return [raw];
    return [];
  } catch {
    return [];
  }
}
