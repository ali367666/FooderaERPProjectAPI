/** Browser-only helpers for JWT storage (no backend calls). */

export const TOKEN_KEY = "token";
export const REFRESH_TOKEN_KEY = "refreshToken";
export const AUTH_USER_KEY = "authUser";

export type AuthUserSnapshot = {
  roles: string[];
  permissions: string[];
};

export function getStoredToken(): string | null {
  if (typeof window === "undefined") return null;
  return localStorage.getItem(TOKEN_KEY);
}

export function clearStoredAuth(): void {
  localStorage.removeItem(TOKEN_KEY);
  localStorage.removeItem(REFRESH_TOKEN_KEY);
  localStorage.removeItem(AUTH_USER_KEY);
}

export function persistAuth(accessToken: string, refreshToken?: string): void {
  localStorage.setItem(TOKEN_KEY, accessToken);
  if (refreshToken) {
    localStorage.setItem(REFRESH_TOKEN_KEY, refreshToken);
  }
}

export function getStoredAuthUser(): AuthUserSnapshot | null {
  if (typeof window === "undefined") return null;
  const raw = localStorage.getItem(AUTH_USER_KEY);
  if (!raw) return null;
  try {
    const parsed = JSON.parse(raw) as Partial<AuthUserSnapshot>;
    const roles = Array.isArray(parsed.roles) ? parsed.roles.filter((x): x is string => typeof x === "string") : [];
    const permissions = Array.isArray(parsed.permissions)
      ? parsed.permissions.filter((x): x is string => typeof x === "string")
      : [];
    return { roles, permissions };
  } catch {
    return null;
  }
}

export function persistAuthUser(user: AuthUserSnapshot): void {
  localStorage.setItem(AUTH_USER_KEY, JSON.stringify(user));
}

/**
 * Notifies when the stored token may have changed (polling, other tabs, focus).
 * Call `sync()` immediately and whenever the app window may have touched storage.
 */
export function subscribeToToken(
  onChange: (token: string | null) => void,
): () => void {
  const sync = () => onChange(getStoredToken());

  sync();
  const intervalId = window.setInterval(sync, 500);
  window.addEventListener("storage", sync);
  window.addEventListener("focus", sync);
  const onVisibility = () => {
    if (document.visibilityState === "visible") sync();
  };
  document.addEventListener("visibilitychange", onVisibility);

  return () => {
    window.clearInterval(intervalId);
    window.removeEventListener("storage", sync);
    window.removeEventListener("focus", sync);
    document.removeEventListener("visibilitychange", onVisibility);
  };
}
