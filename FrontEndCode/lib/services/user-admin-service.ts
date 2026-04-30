import axios from "axios";
import { api } from "@/lib/api";
import { assertApiSuccess, readBaseResponseData, readBaseResponseList } from "@/lib/api-base-response";
import { ApiFormError, toApiFormError } from "@/lib/api-error";

export type AppUser = {
  id: number;
  fullName: string;
  userName: string | null;
  email: string | null;
  phoneNumber: string | null;
  isActive: boolean;
  companyId: number;
  companyName: string | null;
  roles: string[];
  linkedEmployeeId: number | null;
};

export type AppUserInput = {
  fullName: string;
  userName: string;
  email: string;
  /** Only for create, or when changing password on update */
  password?: string;
  phoneNumber: string | null;
  isActive: boolean;
  companyId: number;
  employeeId: number | null;
};

function pick<T>(o: Record<string, unknown>, camel: string, pascal: string): T | undefined {
  if (o[camel] !== undefined) return o[camel] as T;
  if (o[pascal] !== undefined) return o[pascal] as T;
  return undefined;
}

function normalizeUser(item: unknown): AppUser | null {
  if (!item || typeof item !== "object") return null;
  const raw = item as Record<string, unknown>;
  const id = Number(pick(raw, "id", "Id"));
  if (!Number.isFinite(id) || id <= 0) return null;
  const rolesRaw = pick<unknown[] | undefined>(raw, "roles", "Roles");
  const roles = Array.isArray(rolesRaw) ? rolesRaw.map((r) => String(r)) : [];
  const le = pick<number | null | undefined>(raw, "linkedEmployeeId", "LinkedEmployeeId");
  return {
    id,
    fullName: String(pick(raw, "fullName", "FullName") ?? ""),
    userName: (pick<string | null>(raw, "userName", "UserName") ?? null) as string | null,
    email: (pick<string | null>(raw, "email", "Email") ?? null) as string | null,
    phoneNumber: (pick<string | null>(raw, "phoneNumber", "PhoneNumber") ?? null) as string | null,
    isActive: Boolean(pick(raw, "isActive", "IsActive") ?? true),
    companyId: Number(pick(raw, "companyId", "CompanyId") ?? 0),
    companyName: (pick<string | null>(raw, "companyName", "CompanyName") ?? null) as string | null,
    roles,
    linkedEmployeeId: le == null || le === 0 ? null : Number(le),
  };
}

function parseFieldErrors(data: unknown): Record<string, string[]> | undefined {
  if (!data || typeof data !== "object") return undefined;
  const err = (data as { errors?: unknown; Errors?: unknown }).errors ?? (data as { Errors?: unknown }).Errors;
  if (!err || typeof err !== "object") return undefined;
  const out: Record<string, string[]> = {};
  for (const [k, v] of Object.entries(err as Record<string, unknown>)) {
    if (Array.isArray(v)) {
      out[k] = v.filter((x): x is string => typeof x === "string");
    } else if (typeof v === "string") {
      out[k] = [v];
    }
  }
  return Object.keys(out).length ? out : undefined;
}

function friendlyRequestError(message: string, status?: number): string {
  if (status === 403) return "You do not have permission to perform this action.";
  if (status === 404) return "The requested user was not found.";
  if (!message || /^request failed with status code/i.test(message)) {
    return "The request could not be completed. Please try again.";
  }
  return message;
}

export async function getUsers(companyId?: number | null): Promise<AppUser[]> {
  try {
    const response = await api.get<unknown>("/Users", {
      params: companyId != null && companyId > 0 ? { companyId } : undefined,
    });
    assertApiSuccess(response.data);
    const list = readBaseResponseList<unknown>(response.data);
    return list.map((row) => normalizeUser(row)).filter((row): row is AppUser => row !== null);
  } catch (e) {
    throw toApiFormError(e, "Failed to load users.");
  }
}

export async function getUserById(id: number): Promise<AppUser> {
  try {
    const response = await api.get<unknown>(`/Users/${id}`);
    assertApiSuccess(response.data);
    const data = readBaseResponseData<unknown>(response.data);
    const u = normalizeUser(data);
    if (!u) throw new Error("User not found.");
    return u;
  } catch (e) {
    if (e instanceof Error && e.message === "User not found.") throw e;
    const err = toApiFormError(e, "Failed to load user.");
    throw new Error(friendlyRequestError(err.message));
  }
}

export async function createUser(input: AppUserInput & { password: string }): Promise<number> {
  try {
    const response = await api.post<unknown>("/Users", {
      fullName: input.fullName.trim(),
      userName: input.userName.trim(),
      email: input.email.trim(),
      password: input.password,
      phoneNumber: input.phoneNumber?.trim() || null,
      isActive: input.isActive,
      companyId: input.companyId,
      employeeId: input.employeeId,
    });
    if (response.data && typeof response.data === "object" && (response.data as { success?: boolean }).success === false) {
      const o = response.data as { message?: string; errors?: unknown };
      throw new ApiFormError(
        o.message || "Could not create user.",
        parseFieldErrors(response.data) ?? {},
      );
    }
    assertApiSuccess(response.data);
    const id = readBaseResponseData<number>(response.data);
    if (id == null || !Number.isFinite(Number(id))) throw new Error("Invalid response from server.");
    return Number(id);
  } catch (e) {
    if (e instanceof ApiFormError) throw e;
    if (axios.isAxiosError(e) && e.response?.status === 403) {
      throw new Error("You do not have permission to create users.");
    }
    const err = toApiFormError(e, "Failed to create user.");
    throw new Error(friendlyRequestError(err.message));
  }
}

export async function updateUser(id: number, input: AppUserInput): Promise<void> {
  try {
    const body: Record<string, unknown> = {
      fullName: input.fullName.trim(),
      userName: input.userName.trim(),
      email: input.email.trim(),
      phoneNumber: input.phoneNumber?.trim() || null,
      isActive: input.isActive,
      companyId: input.companyId,
      employeeId: input.employeeId,
    };
    if (input.password && input.password.trim().length > 0) {
      body.password = input.password;
    }
    const response = await api.put<unknown>(`/Users/${id}`, body);
    if (response.data && typeof response.data === "object" && (response.data as { success?: boolean }).success === false) {
      const o = response.data as { message?: string };
      throw new ApiFormError(o.message || "Could not update user.", parseFieldErrors(response.data) ?? {});
    }
    assertApiSuccess(response.data);
  } catch (e) {
    if (e instanceof ApiFormError) throw e;
    const err = toApiFormError(e, "Failed to update user.");
    throw new Error(friendlyRequestError(err.message));
  }
}

export async function deleteUserApi(id: number): Promise<void> {
  try {
    const response = await api.delete<unknown>(`/Users/${id}`);
    assertApiSuccess(response.data);
  } catch (e) {
    const err = toApiFormError(e, "Failed to delete user.");
    throw new Error(friendlyRequestError(err.message));
  }
}
