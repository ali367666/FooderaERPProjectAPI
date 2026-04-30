import { api } from "@/lib/api";
import { ApiFormError, toApiFormError } from "@/lib/api-error";
import { fetchListsPerCompany } from "@/lib/company-scope-utils";

export type Department = {
  id: number;
  companyId: number;
  name: string;
  description?: string | null;
};

type ApiResponse<T> = {
  success?: boolean;
  message?: string;
  data?: T;
};

const TEMP_COMPANY_ID_FALLBACK = 5;

function resolveCompanyId(explicitCompanyId?: number): number {
  if (typeof explicitCompanyId === "number" && explicitCompanyId > 0) {
    return explicitCompanyId;
  }

  if (typeof window !== "undefined") {
    const storedCompanyId = localStorage.getItem("companyId");
    const parsedCompanyId = storedCompanyId ? Number(storedCompanyId) : NaN;

    if (Number.isFinite(parsedCompanyId) && parsedCompanyId > 0) {
      return parsedCompanyId;
    }
  }

  return TEMP_COMPANY_ID_FALLBACK;
}

type DepartmentMutationInput = {
  name: string;
  description?: string;
  companyId?: number;
};

/** Merge GET /Departments for each company id (API requires companyId). */
export async function getDepartmentsForAllCompanies(companyIds: number[]): Promise<Department[]> {
  return fetchListsPerCompany(companyIds, (id) => getDepartments(id));
}

export async function getDepartments(companyId?: number): Promise<Department[]> {
  const effectiveCompanyId = resolveCompanyId(companyId);

  try {
    const response = await api.get<ApiResponse<Department[]>>("/Departments", {
      params: { companyId: effectiveCompanyId },
    });

    const payload = response.data;

    if (!payload) return [];

    if (payload.success === false) {
      throw new Error(payload.message || "Failed to fetch departments");
    }

    if (Array.isArray(payload.data)) {
      return payload.data;
    }

    if (Array.isArray(payload)) {
      return payload;
    }

    const payloadWithItems = payload as { items?: Department[] };
    if (Array.isArray(payloadWithItems.items)) {
      return payloadWithItems.items;
    }

    return [];
  } catch (error) {
    throw toApiFormError(
      error,
      `Failed to fetch departments (companyId: ${effectiveCompanyId})`,
    );
  }
}

export async function createDepartment(
  data: DepartmentMutationInput,
): Promise<Department> {
  const effectiveCompanyId = resolveCompanyId(data.companyId);
  const payload = {
    name: data.name,
    description: data.description || "",
    companyId: effectiveCompanyId,
  };

  try {
    console.log("Create department payload:", payload);
    const response = await api.post<ApiResponse<Department>>("/Departments", payload);

    const responsePayload = response.data;
    if (responsePayload?.success === false) {
      throw new ApiFormError(responsePayload.message || "Failed to create department");
    }

    if (!responsePayload?.data) {
      throw new Error("Invalid create department response");
    }

    return responsePayload.data;
  } catch (error) {
    throw toApiFormError(error, "Failed to create department");
  }
}

export async function updateDepartment(
  id: number,
  data: DepartmentMutationInput,
): Promise<Department> {
  const effectiveCompanyId = resolveCompanyId(data.companyId);

  try {
    const response = await api.put<ApiResponse<Department>>(`/Departments/${id}`, {
      name: data.name,
      description: data.description || "",
      companyId: effectiveCompanyId,
    }, {
      params: { companyId: effectiveCompanyId },
    });

    const payload = response.data;
    if (payload?.success === false) {
      throw new ApiFormError(payload.message || "Failed to update department");
    }

    if (!payload?.data) {
      throw new Error("Invalid update department response");
    }

    return payload.data;
  } catch (error) {
    throw toApiFormError(error, "Failed to update department");
  }
}

export async function deleteDepartment(
  id: number,
  companyId?: number,
): Promise<void> {
  const effectiveCompanyId = resolveCompanyId(companyId);

  try {
    const response = await api.delete<ApiResponse<unknown>>(`/Departments/${id}`, {
      params: { companyId: effectiveCompanyId },
    });

    const payload = response.data;
    if (payload?.success === false) {
      throw new ApiFormError(payload.message || "Failed to delete department");
    }
  } catch (error) {
    throw toApiFormError(error, "Failed to delete department");
  }
}