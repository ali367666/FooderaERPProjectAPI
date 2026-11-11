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

/** The company's own SuperAdmin account, created together with the company. */
export type CompanyOwnerInput = {
  ownerFullName: string;
  ownerUserName: string;
  ownerEmail: string;
  ownerPassword: string;
  /** Optional: the amount (AZN) the company starts working with; recorded as its opening deposit. */
  openingDepositAmount?: number | null;
};

export async function createCompany(data: CompanyMutationInput & CompanyOwnerInput): Promise<number> {
  try {
    const response = await api.post<ApiResponse<{ id?: number; Id?: number }>>("/companies", data);
    const payload = response.data;
    if (payload?.success === false) {
      throw new ApiFormError(payload.message || "Failed to create company");
    }
    const id = payload?.data?.id ?? payload?.data?.Id;
    if (!Number.isFinite(Number(id)) || Number(id) <= 0) {
      throw new ApiFormError("Invalid create company response");
    }
    return Number(id);
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

export type CompanyModules = {
  moduleFilial: boolean;
  moduleAnbar: boolean;
  moduleRezervasyon: boolean;
  moduleMasaBolge: boolean;
  modulePaket: boolean;
  moduleOtel: boolean;
  moduleFitnes: boolean;
  moduleDataSecimi: boolean;
  moduleQiymetSor: boolean;
  moduleKompleks: boolean;
  moduleDepartment: boolean;
};

export async function setCompanyModules(companyId: number, modules: CompanyModules): Promise<void> {
  try {
    const response = await api.put<ApiResponse<unknown>>(
      "/company-settings/modules",
      modules,
      { params: { companyId } },
    );
    const payload = response.data;
    if (payload?.success === false) {
      throw new ApiFormError(payload.message || "Failed to update company modules");
    }
  } catch (error) {
    throw toApiFormError(error, "Failed to update company modules");
  }
}

export type PaymentBankStatus = { name: string; enabled: boolean };

/** SuperAdmin only: the catalog of payment banks and which of them a company has. */
export async function getCompanyPaymentBanks(companyId: number): Promise<PaymentBankStatus[]> {
  try {
    const response = await api.get<unknown>("/company-settings/payment-banks", { params: { companyId } });
    const list = Array.isArray(response.data) ? response.data : [];
    return list.map((row) => {
      const r = row as Record<string, unknown>;
      return { name: String(r.name ?? r.Name ?? ""), enabled: Boolean(r.enabled ?? r.Enabled) };
    });
  } catch (error) {
    throw toApiFormError(error, "Failed to load the payment banks");
  }
}

/** SuperAdmin only: gives a company exactly these banks; the others are switched off. */
export async function setCompanyPaymentBanks(companyId: number, names: string[]): Promise<void> {
  try {
    const response = await api.put<ApiResponse<unknown>>("/company-settings/payment-banks", names, { params: { companyId } });
    if (response.data?.success === false) {
      throw new ApiFormError(response.data.message || "Failed to update the payment banks");
    }
  } catch (error) {
    throw toApiFormError(error, "Failed to update the payment banks");
  }
}

/** SuperAdmin only: whether staff with the receipt-edit permissions may correct a company's paid receipts. */
export async function setCompanyReceiptEdit(companyId: number, allowed: boolean): Promise<void> {
  try {
    const response = await api.put<ApiResponse<unknown>>("/company-settings/receipt-edit", null, { params: { companyId, allowed } });
    if (response.data?.success === false) {
      throw new ApiFormError(response.data.message || "Failed to update the receipt edit switch");
    }
  } catch (error) {
    throw toApiFormError(error, "Failed to update the receipt edit switch");
  }
}

/** SuperAdmin only: whether a restaurant's orders wait for the kitchen to accept them / mark them ready before payment. */
export async function setCompanyKitchenApproval(companyId: number, required: boolean): Promise<void> {
  try {
    const response = await api.put<ApiResponse<unknown>>("/company-settings/kitchen-approval", null, { params: { companyId, required } });
    if (response.data?.success === false) {
      throw new ApiFormError(response.data.message || "Failed to update the kitchen approval");
    }
  } catch (error) {
    throw toApiFormError(error, "Failed to update the kitchen approval");
  }
}

/** SuperAdmin only: opens or closes changing a company's store sales (cancelling them). */
export async function setCompanySalesLock(companyId: number, locked: boolean): Promise<void> {
  try {
    const response = await api.put<ApiResponse<unknown>>("/company-settings/sales-lock", null, { params: { companyId, locked } });
    if (response.data?.success === false) {
      throw new ApiFormError(response.data.message || "Failed to update the sales lock");
    }
  } catch (error) {
    throw toApiFormError(error, "Failed to update the sales lock");
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
