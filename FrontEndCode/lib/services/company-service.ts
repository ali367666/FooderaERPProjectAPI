import { api } from "@/lib/api";
import { ApiFormError, toApiFormError } from "@/lib/api-error";

export type Company = {
  id: number;
  name: string;
  companyCode: string;
  email?: string | null;
  primaryPhoneNumber?: string | null;
  secondaryPhoneNumber?: string | null;
  address?: string | null;
  description?: string | null;
  countryCode?: string | null;
  countryName?: string | null;
  taxNumber?: string | null;
  taxOfficeCode?: string | null;
};

type ApiResponse<T> = {
  success?: boolean;
  message?: string;
  data?: T;
};

export type CompanyMutationInput = {
  companyCode: string;
  name: string;
  description?: string;
  address?: string;
  taxNumber?: string;
  taxOfficeCode?: string;
  country?: number;
  email?: string;
  primaryPhoneNumber?: string;
  secondaryPhoneNumber?: string;
};

function pickFirst<T>(...values: Array<T | undefined | null>): T | undefined {
  for (const value of values) {
    if (value !== undefined && value !== null) return value;
  }
  return undefined;
}

function normalizeCompany(item: unknown): Company | null {
  if (!item || typeof item !== "object") return null;
  const raw = item as Record<string, unknown>;

  const idRaw = pickFirst(raw.id, raw.Id);
  const id = Number(idRaw);
  if (!Number.isFinite(id) || id <= 0) return null;

  const companyCode = pickFirst(raw.companyCode, raw.CompanyCode);
  const name = pickFirst(raw.name, raw.Name);
  if (!companyCode || !name) return null;

  return {
    id,
    companyCode: String(companyCode),
    name: String(name),
    email: (pickFirst(raw.email, raw.Email) as string | null | undefined) ?? null,
    primaryPhoneNumber:
      (pickFirst(
        raw.primaryPhoneNumber,
        raw.PrimaryPhoneNumber,
      ) as string | null | undefined) ?? null,
    secondaryPhoneNumber:
      (pickFirst(
        raw.secondaryPhoneNumber,
        raw.SecondaryPhoneNumber,
      ) as string | null | undefined) ?? null,
    address: (pickFirst(raw.address, raw.Address) as string | null | undefined) ?? null,
    description:
      (pickFirst(raw.description, raw.Description) as string | null | undefined) ?? null,
    countryCode:
      (pickFirst(raw.countryCode, raw.CountryCode) as string | null | undefined) ?? null,
    countryName:
      (pickFirst(raw.countryName, raw.CountryName) as string | null | undefined) ?? null,
    taxNumber:
      (pickFirst(raw.taxNumber, raw.TaxNumber) as string | null | undefined) ?? null,
    taxOfficeCode:
      (pickFirst(raw.taxOfficeCode, raw.TaxOfficeCode) as string | null | undefined) ?? null,
  };
}

export async function getCompanies(): Promise<Company[]> {
  try {
    const response = await api.get<Company[] | ApiResponse<Company[]>>("/companies");
    const payload = response.data;
    if (Array.isArray(payload)) {
      return payload
        .map((company) => normalizeCompany(company))
        .filter((company): company is Company => company !== null);
    }
    if (payload?.success === false) {
      throw new Error(payload.message || "Failed to fetch companies");
    }
    if (!Array.isArray(payload?.data)) return [];
    return payload.data
      .map((company) => normalizeCompany(company))
      .filter((company): company is Company => company !== null);
  } catch (error) {
    throw toApiFormError(error, "Failed to fetch companies");
  }
}

export async function getCompanyById(id: number): Promise<Company> {
  try {
    const response = await api.get<ApiResponse<Company>>(`/companies/${id}`);
    const payload = response.data;
    if (payload?.success === false || !payload?.data) {
      throw new Error(payload?.message || "Failed to fetch company");
    }
    const normalized = normalizeCompany(payload.data);
    if (!normalized) {
      throw new Error("Invalid company detail response");
    }
    return normalized;
  } catch (error) {
    throw toApiFormError(error, "Failed to fetch company");
  }
}

export async function createCompany(data: CompanyMutationInput): Promise<void> {
  try {
    const response = await api.post<ApiResponse<unknown>>("/companies", data);
    const payload = response.data;
    if (payload?.success === false) {
      throw new ApiFormError(payload.message || "Failed to create company");
    }
  } catch (error) {
    throw toApiFormError(error, "Failed to create company");
  }
}

export async function updateCompany(
  id: number,
  data: CompanyMutationInput,
): Promise<void> {
  try {
    const response = await api.put<ApiResponse<unknown>>(`/companies/${id}`, data);
    const payload = response.data;
    if (payload?.success === false) {
      throw new ApiFormError(payload.message || "Failed to update company");
    }
  } catch (error) {
    throw toApiFormError(error, "Failed to update company");
  }
}

export async function deleteCompany(id: number): Promise<void> {
  try {
    const response = await api.delete<ApiResponse<unknown>>(`/companies/${id}`);
    const payload = response.data;
    if (payload?.success === false) {
      throw new ApiFormError(payload.message || "Failed to delete company");
    }
  } catch (error) {
    throw toApiFormError(error, "Failed to delete company");
  }
}
