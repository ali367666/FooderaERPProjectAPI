import axios, { type InternalAxiosRequestConfig } from "axios";
import { clearStoredAuth, getStoredRefreshToken, getStoredToken, persistAuth } from "@/lib/auth-client";
import { readBaseResponseData } from "@/lib/api-base-response";

function resolveApiBaseUrl(): string {
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
