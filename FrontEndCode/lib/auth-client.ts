/** Browser-only helpers for JWT storage (no backend calls). */

export const TOKEN_KEY = "token";
export const REFRESH_TOKEN_KEY = "refreshToken";

export function getStoredToken(): string | null {
  if (typeof window === "undefined") return null;
  return localStorage.getItem(TOKEN_KEY);
}

export function clearStoredAuth(): void {
  localStorage.removeItem(TOKEN_KEY);
  localStorage.removeItem(REFRESH_TOKEN_KEY);
}

export function persistAuth(accessToken: string, refreshToken?: string): void {
  localStorage.setItem(TOKEN_KEY, accessToken);
  if (refreshToken) {
    localStorage.setItem(REFRESH_TOKEN_KEY, refreshToken);
  }
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
