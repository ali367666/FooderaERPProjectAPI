import axios, { type InternalAxiosRequestConfig } from "axios";
import { getDeviceKey } from "@/lib/device-key";
import { clearStoredAuth, getStoredRefreshToken, getStoredToken, persistAuth } from "@/lib/auth-client";
import { readBaseResponseData } from "@/lib/api-base-response";

export function resolveApiBaseUrl(): string {
  // Installations set this at build time, e.g. "http://{host}:5167/api" — {host} becomes the
  // address the browser used, so the server PC (localhost) and every terminal (LAN IP) work alike.
  const configured = process.env.NEXT_PUBLIC_API_URL;
  if (configured) {
    return typeof window === "undefined"
      ? configured.replace("{host}", "localhost")
      : configured.replace("{host}", window.location.hostname);
  }

  if (typeof window === "undefined") return "https://localhost:7145/api";
  const host = window.location.hostname;
  if (host === "localhost" || host === "127.0.0.1") return "https://localhost:7145/api";
  // Accessed via a LAN IP (e.g. scanning a QR code from a phone) — the backend's
  // self-signed HTTPS dev cert isn't trusted off the dev machine, so fall back to
  // its plain-HTTP profile on the same host.
  return `http://${host}:5167/api`;
}

export const api = axios.create({
  baseURL: resolveApiBaseUrl(),
});

api.interceptors.request.use((config: InternalAxiosRequestConfig) => {
  const token = getStoredToken();

  if (token) {
    config.headers = config.headers ?? {};
    config.headers.Authorization = `Bearer ${token}`;
  }

  // Registered-device key for the Online/Offline licence gate.
  const deviceKey = getDeviceKey();
  if (deviceKey) {
    config.headers = config.headers ?? {};
    config.headers["X-Device-Key"] = deviceKey;
  }

  // Dashboard company filter: tells the API which company to show. Only honoured server-side
  // for the SuperAdmin — for every other user the company always comes from their own token.
  if (typeof window !== "undefined" && window.location.pathname.startsWith("/dashboard")) {
    try {
      const selected = Number(localStorage.getItem("dashboardSelectedCompanyId"));
      if (Number.isFinite(selected) && selected > 0) {
        config.headers = config.headers ?? {};
        config.headers["X-Company-Id"] = String(selected);
      }
    } catch {
      // storage unavailable — fall back to the token's company
    }
  }

  return config;
});

let redirectingToLogin = false;
let refreshPromise: Promise<string | null> | null = null;

function isAuthPath(url: string): boolean {
  return /\/Auth\/(login|register|refresh-token)/i.test(url);
}

function forceLogout(): void {
  clearStoredAuth();

  if (!redirectingToLogin && typeof window !== "undefined" && window.location.pathname !== "/login") {
    redirectingToLogin = true;
    window.location.href = "/login?expired=1";
  }
}

async function refreshAccessToken(): Promise<string | null> {
  const refreshToken = getStoredRefreshToken();
  if (!refreshToken) return null;

  try {
    const response = await axios.post(`${api.defaults.baseURL}/Auth/refresh-token`, { refreshToken });
    const data = readBaseResponseData<{ accessToken?: string; AccessToken?: string; refreshToken?: string; RefreshToken?: string }>(
      response.data,
    );
    const newAccessToken = data?.accessToken ?? data?.AccessToken;
    if (!newAccessToken) return null;

    persistAuth(newAccessToken, data?.refreshToken ?? data?.RefreshToken);
    return newAccessToken;
  } catch {
    return null;
  }
}

api.interceptors.response.use(
  (response) => response,
  async (error) => {
    if (!axios.isAxiosError(error) || typeof window === "undefined") {
      return Promise.reject(error);
    }

    const status = error.response?.status;
    const requestUrl = error.config?.url ?? "";

    // Offline licence: this device isn't one of the company's registered devices.
    const body = error.response?.data as { code?: string } | undefined;
    if (status === 403 && body?.code === "LICENSE_REQUIRED") {
      if (!window.location.pathname.startsWith("/license")) {
        const next = encodeURIComponent(window.location.pathname + window.location.search);
        window.location.href = `/license?next=${next}`;
      }
      return Promise.reject(error);
    }

    if (status === 403 && body?.code === "DEVICE_NOT_REGISTERED") {
      if (!window.location.pathname.startsWith("/device-register")) {
        const next = encodeURIComponent(window.location.pathname + window.location.search);
        window.location.href = `/device-register?next=${next}`;
      }
      return Promise.reject(error);
    }

    if (status !== 401 || isAuthPath(requestUrl)) {
      return Promise.reject(error);
    }

    const originalRequest = error.config as (InternalAxiosRequestConfig & { _retried?: boolean }) | undefined;
    if (!originalRequest || originalRequest._retried) {
      forceLogout();
      return Promise.reject(error);
    }
    originalRequest._retried = true;

    if (!refreshPromise) {
      refreshPromise = refreshAccessToken().finally(() => {
        refreshPromise = null;
      });
    }

    const newToken = await refreshPromise;
    if (!newToken) {
      forceLogout();
      return Promise.reject(error);
    }

    originalRequest.headers = originalRequest.headers ?? {};
    originalRequest.headers.Authorization = `Bearer ${newToken}`;
    return api(originalRequest);
  },
);
