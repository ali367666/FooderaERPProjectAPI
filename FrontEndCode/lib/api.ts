import axios, { type InternalAxiosRequestConfig } from "axios";
import { clearStoredAuth, getStoredToken } from "@/lib/auth-client";

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

api.interceptors.response.use(
  (response) => response,
  (error) => {
    const status = axios.isAxiosError(error) ? error.response?.status : undefined;
    const requestUrl = axios.isAxiosError(error) ? error.config?.url ?? "" : "";
    const isAuthEndpoint = /\/Auth\/(login|register)/i.test(requestUrl);

    if (status === 401 && !isAuthEndpoint && typeof window !== "undefined") {
      clearStoredAuth();

      if (!redirectingToLogin && window.location.pathname !== "/login") {
        redirectingToLogin = true;
        window.location.href = "/login?expired=1";
      }
    }

    return Promise.reject(error);
  },
);