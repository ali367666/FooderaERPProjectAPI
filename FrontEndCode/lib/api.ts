import axios, { type InternalAxiosRequestConfig } from "axios";
import { getStoredToken } from "@/lib/auth-client";

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