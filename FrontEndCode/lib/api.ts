import axios, { type InternalAxiosRequestConfig } from "axios";
import { clearStoredAuth, getStoredRefreshToken, getStoredToken, persistAuth } from "@/lib/auth-client";
import { readBaseResponseData } from "@/lib/api-base-response";

export const api = axios.create({
  baseURL: "https://localhost:7145/api",
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
