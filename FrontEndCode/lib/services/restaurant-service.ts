import { api } from "@/lib/api";
import { ApiFormError, toApiFormError } from "@/lib/api-error";

type ApiResponse<T> = {
  success?: boolean;
  message?: string;
  data?: T;
};

export type Restaurant = {
  id: number;
  name: string;
  description?: string | null;
  address?: string | null;
  phone?: string | null;
  email?: string | null;
  companyId: number;
  companyName?: string;
};

export type RestaurantMutationInput = {
  name: string;
  description?: string;
  address?: string;
  phone?: string;
  email?: string;
  companyId: number;
};

function normalizeRestaurant(item: unknown): Restaurant | null {
  if (!item || typeof item !== "object") return null;
  const raw = item as Record<string, unknown>;
  const id = Number(raw.id ?? raw.Id);
  if (!Number.isFinite(id) || id <= 0) return null;

  return {
    id,
    name: String(raw.name ?? raw.Name ?? ""),
    description: (raw.description ?? raw.Description ?? null) as string | null,
    address: (raw.address ?? raw.Address ?? null) as string | null,
    phone: (raw.phone ?? raw.Phone ?? null) as string | null,
    email: (raw.email ?? raw.Email ?? null) as string | null,
    companyId: Number(raw.companyId ?? raw.CompanyId ?? 0),
    companyName: String(raw.companyName ?? raw.CompanyName ?? ""),
  };
}

export async function getRestaurants(companyId?: number): Promise<Restaurant[]> {
  try {
    const response = await api.get<ApiResponse<Restaurant[]>>("/Restaurant", {
      params: companyId ? { companyId } : undefined,
    });
    const payload = response.data;
    if (payload?.success === false) {
      throw new ApiFormError(payload.message || "Failed to fetch branches");
    }

    const list = Array.isArray(payload?.data) ? payload.data : [];
    return list
      .map((restaurant) => normalizeRestaurant(restaurant))
      .filter((restaurant): restaurant is Restaurant => restaurant !== null);
  } catch (error) {
    throw toApiFormError(error, "Failed to fetch branches");
  }
}

export async function createRestaurant(data: RestaurantMutationInput): Promise<void> {
  try {
    const response = await api.post<ApiResponse<unknown>>("/Restaurant", data);
    const payload = response.data;
    if (payload?.success === false) {
      throw new ApiFormError(payload.message || "Failed to create branch");
    }
  } catch (error) {
    throw toApiFormError(error, "Failed to create branch");
  }
}

export async function updateRestaurant(
  id: number,
  data: RestaurantMutationInput,
): Promise<void> {
  try {
    const response = await api.put<ApiResponse<unknown>>(`/Restaurant/${id}`, data);
    const payload = response.data;
    if (payload?.success === false) {
      throw new ApiFormError(payload.message || "Failed to update branch");
    }
  } catch (error) {
    throw toApiFormError(error, "Failed to update branch");
  }
}

export async function deleteRestaurant(id: number): Promise<void> {
  try {
    const response = await api.delete<ApiResponse<unknown>>(`/Restaurant/${id}`);
    const payload = response.data;
    if (payload?.success === false) {
      throw new ApiFormError(payload.message || "Failed to delete branch");
    }
  } catch (error) {
    throw toApiFormError(error, "Failed to delete branch");
  }
}
