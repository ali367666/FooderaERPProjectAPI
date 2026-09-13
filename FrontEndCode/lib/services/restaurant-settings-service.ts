import { api } from "@/lib/api";
import { toApiFormError } from "@/lib/api-error";

export type RestaurantModules = {
  moduleAnbar: boolean;
  moduleRezervasyon: boolean;
  moduleMasaBolge: boolean;
  modulePaket: boolean;
  moduleOtel: boolean;
  moduleFitnes: boolean;
  moduleDataSecimi: boolean;
  moduleQiymetSor: boolean;
};

type ApiResponse<T> = {
  success?: boolean;
  message?: string;
  data?: T;
};

function normalize(raw: Record<string, unknown>): RestaurantModules {
  const bool = (camel: string, pascal: string) => Boolean(raw[camel] ?? raw[pascal] ?? false);
  return {
    moduleAnbar: bool("moduleAnbar", "ModuleAnbar"),
    moduleRezervasyon: bool("moduleRezervasyon", "ModuleRezervasyon"),
    moduleMasaBolge: bool("moduleMasaBolge", "ModuleMasaBolge"),
    modulePaket: bool("modulePaket", "ModulePaket"),
    moduleOtel: bool("moduleOtel", "ModuleOtel"),
    moduleFitnes: bool("moduleFitnes", "ModuleFitnes"),
    moduleDataSecimi: bool("moduleDataSecimi", "ModuleDataSecimi"),
    moduleQiymetSor: bool("moduleQiymetSor", "ModuleQiymetSor"),
  };
}

export async function getRestaurantModules(restaurantId: number): Promise<RestaurantModules> {
  try {
    const response = await api.get<ApiResponse<unknown>>(`/Restaurant/${restaurantId}/modules`);
    const payload = response.data;
    if (payload?.success === false) {
      throw new Error(payload?.message || "Failed to fetch branch modules");
    }
    return normalize((payload?.data ?? {}) as Record<string, unknown>);
  } catch (error) {
    throw toApiFormError(error, "Failed to fetch branch modules");
  }
}

export async function setRestaurantModules(
  restaurantId: number,
  modules: RestaurantModules,
): Promise<void> {
  try {
    const response = await api.put<ApiResponse<unknown>>(`/Restaurant/${restaurantId}/modules`, modules);
    const payload = response.data;
    if (payload?.success === false) {
      throw new Error(payload?.message || "Failed to update branch modules");
    }
  } catch (error) {
    throw toApiFormError(error, "Failed to update branch modules");
  }
}
