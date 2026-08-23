import { api } from "@/lib/api";
import { toApiFormError } from "@/lib/api-error";

export type RestaurantLookupItem = {
  id: number;
  name: string;
};

export type CompanyLookup = {
  companyId: number;
  companyName: string;
  restaurants: RestaurantLookupItem[];
};

type ApiResponse<T> = {
  success?: boolean;
  message?: string;
  data?: T;
};

function pickFirst<T>(...values: Array<T | undefined | null>): T | undefined {
  for (const value of values) {
    if (value !== undefined && value !== null) return value;
  }
  return undefined;
}

function normalizeCompanyLookup(item: unknown): CompanyLookup | null {
  if (!item || typeof item !== "object") return null;
  const raw = item as Record<string, unknown>;

  const companyIdRaw = pickFirst(raw.companyId, raw.CompanyId);
  const companyId = Number(companyIdRaw);
  if (!Number.isFinite(companyId) || companyId <= 0) return null;

  const companyName = pickFirst(raw.companyName, raw.CompanyName);
  if (!companyName) return null;

  const restaurantsRaw = pickFirst(raw.restaurants, raw.Restaurants);
  const restaurants: RestaurantLookupItem[] = Array.isArray(restaurantsRaw)
    ? restaurantsRaw
        .map((r): RestaurantLookupItem | null => {
          if (!r || typeof r !== "object") return null;
          const rr = r as Record<string, unknown>;
          const id = Number(pickFirst(rr.id, rr.Id));
          const name = pickFirst(rr.name, rr.Name);
          if (!Number.isFinite(id) || id <= 0 || !name) return null;
          return { id, name: String(name) };
        })
        .filter((r): r is RestaurantLookupItem => r !== null)
    : [];

  return { companyId, companyName: String(companyName), restaurants };
}

export async function lookupCompanyByCode(companyCode: string): Promise<CompanyLookup> {
  try {
    const response = await api.get<ApiResponse<unknown>>(
      `/companies/lookup/${encodeURIComponent(companyCode)}`,
    );
    const payload = response.data;
    if (payload?.success === false || !payload?.data) {
      throw new Error(payload?.message || "Company was not found");
    }
    const normalized = normalizeCompanyLookup(payload.data);
    if (!normalized) {
      throw new Error("Invalid company lookup response");
    }
    return normalized;
  } catch (error) {
    throw toApiFormError(error, "Company was not found");
  }
}

export type PosLoginPayload = {
  companyId: number;
  restaurantId?: number | null;
  code?: string;
  rfidCardId?: string;
};

export type PosLoginResult = {
  accessToken: string;
  refreshToken?: string;
  permissions: string[];
  roles: string[];
};

function normalizePosLoginResult(item: unknown): PosLoginResult | null {
  if (!item || typeof item !== "object") return null;
  const raw = item as Record<string, unknown>;

  const accessToken = pickFirst(raw.accessToken, raw.AccessToken);
  if (typeof accessToken !== "string" || !accessToken) return null;

  const refreshTokenRaw = pickFirst(raw.refreshToken, raw.RefreshToken);
  const permissionsRaw = pickFirst(raw.permissions, raw.Permissions);
  const rolesRaw = pickFirst(raw.roles, raw.Roles);

  return {
    accessToken,
    refreshToken: typeof refreshTokenRaw === "string" ? refreshTokenRaw : undefined,
    permissions: Array.isArray(permissionsRaw)
      ? permissionsRaw.filter((x): x is string => typeof x === "string")
      : [],
    roles: Array.isArray(rolesRaw) ? rolesRaw.filter((x): x is string => typeof x === "string") : [],
  };
}

export async function posLogin(payload: PosLoginPayload): Promise<PosLoginResult> {
  try {
    const response = await api.post<ApiResponse<unknown>>("/Auth/pos-login", payload);
    const data = response.data;
    if (data?.success === false || !data?.data) {
      throw new Error(data?.message || "Invalid code or card");
    }
    const normalized = normalizePosLoginResult(data.data);
    if (!normalized) {
      throw new Error("Invalid response from server");
    }
    return normalized;
  } catch (error) {
    throw toApiFormError(error, "Invalid code or card");
  }
}
