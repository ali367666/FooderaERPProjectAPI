import { api } from "@/lib/api";
import { ApiFormError, toApiFormError } from "@/lib/api-error";

export type CompanySettings = {
  id: number;
  companyId: number;
  openingTime: string | null;

  moduleFilial: boolean;
  moduleAnbar: boolean;
  moduleRezervasyon: boolean;
  moduleMasaBolge: boolean;

  integrationWolt: boolean;
  integrationBolt: boolean;
  integration189Delivery: boolean;

  alertMilliseconds: number | null;
  alertRingCount: number | null;
  alertRingIntervalSeconds: number | null;

  loginLogoUrl: string | null;
  reportLogoUrl: string | null;
  wallpaperUrl: string | null;
  loginLocation: string | null;
  transparencyLevel: number | null;
  productColor: string | null;
  floorLabel: string | null;
  slogan: string | null;
  socialLinks: string | null;
  contactPhoneNumber: string | null;
  receiptFontSize: number | null;
  categoryFontSize: number | null;
  allowReceiptEditAfterPrint: boolean;
  waiterCanPrintCustomerReceipt: boolean;
};

export type CompanySettingsInput = Omit<CompanySettings, "id" | "companyId">;

type ApiResponse<T> = {
  success?: boolean;
  message?: string;
  data?: T;
};

function pick<T>(o: Record<string, unknown>, camel: string, pascal: string): T | undefined {
  if (o[camel] !== undefined) return o[camel] as T;
  if (o[pascal] !== undefined) return o[pascal] as T;
  return undefined;
}

function normalize(item: unknown): CompanySettings | null {
  if (!item || typeof item !== "object") return null;
  const raw = item as Record<string, unknown>;
  const id = Number(pick(raw, "id", "Id"));
  if (!Number.isFinite(id) || id <= 0) return null;

  const bool = (camel: string, pascal: string, def = false) =>
    Boolean(pick(raw, camel, pascal) ?? def);
  const numOrNull = (camel: string, pascal: string) => {
    const v = pick<number | null>(raw, camel, pascal);
    return v === null || v === undefined ? null : Number(v);
  };
  const strOrNull = (camel: string, pascal: string) => {
    const v = pick<string | null>(raw, camel, pascal);
    return v === null || v === undefined ? null : String(v);
  };

  return {
    id,
    companyId: Number(pick(raw, "companyId", "CompanyId") ?? 0),
    openingTime: strOrNull("openingTime", "OpeningTime"),

    moduleFilial: bool("moduleFilial", "ModuleFilial"),
    moduleAnbar: bool("moduleAnbar", "ModuleAnbar"),
    moduleRezervasyon: bool("moduleRezervasyon", "ModuleRezervasyon"),
    moduleMasaBolge: bool("moduleMasaBolge", "ModuleMasaBolge"),

    integrationWolt: bool("integrationWolt", "IntegrationWolt"),
    integrationBolt: bool("integrationBolt", "IntegrationBolt"),
    integration189Delivery: bool("integration189Delivery", "Integration189Delivery"),

    alertMilliseconds: numOrNull("alertMilliseconds", "AlertMilliseconds"),
    alertRingCount: numOrNull("alertRingCount", "AlertRingCount"),
    alertRingIntervalSeconds: numOrNull("alertRingIntervalSeconds", "AlertRingIntervalSeconds"),

    loginLogoUrl: strOrNull("loginLogoUrl", "LoginLogoUrl"),
    reportLogoUrl: strOrNull("reportLogoUrl", "ReportLogoUrl"),
    wallpaperUrl: strOrNull("wallpaperUrl", "WallpaperUrl"),
    loginLocation: strOrNull("loginLocation", "LoginLocation"),
    transparencyLevel: numOrNull("transparencyLevel", "TransparencyLevel"),
    productColor: strOrNull("productColor", "ProductColor"),
    floorLabel: strOrNull("floorLabel", "FloorLabel"),
    slogan: strOrNull("slogan", "Slogan"),
    socialLinks: strOrNull("socialLinks", "SocialLinks"),
    contactPhoneNumber: strOrNull("contactPhoneNumber", "ContactPhoneNumber"),
    receiptFontSize: numOrNull("receiptFontSize", "ReceiptFontSize"),
    categoryFontSize: numOrNull("categoryFontSize", "CategoryFontSize"),
    allowReceiptEditAfterPrint: bool("allowReceiptEditAfterPrint", "AllowReceiptEditAfterPrint", true),
    waiterCanPrintCustomerReceipt: bool("waiterCanPrintCustomerReceipt", "WaiterCanPrintCustomerReceipt", true),
  };
}

export async function getCompanySettings(): Promise<CompanySettings> {
  try {
    const response = await api.get<ApiResponse<unknown>>("/company-settings");
    const payload = response.data;
    if (payload?.success === false || !payload?.data) {
      throw new Error(payload?.message || "Failed to fetch settings");
    }
    const normalized = normalize(payload.data);
    if (!normalized) throw new Error("Invalid settings response");
    return normalized;
  } catch (error) {
    throw toApiFormError(error, "Failed to fetch settings");
  }
}

export async function updateCompanySettings(data: CompanySettingsInput): Promise<CompanySettings> {
  try {
    const response = await api.put<ApiResponse<unknown>>("/company-settings", data);
    const payload = response.data;
    if (payload?.success === false || !payload?.data) {
      throw new ApiFormError(payload?.message || "Failed to update settings");
    }
    const normalized = normalize(payload.data);
    if (!normalized) throw new Error("Invalid settings response");
    return normalized;
  } catch (error) {
    throw toApiFormError(error, "Failed to update settings");
  }
}

export type CompanySettingsBranding = {
  loginLogoUrl: string | null;
  reportLogoUrl: string | null;
  wallpaperUrl: string | null;
  loginLocation: string | null;
  transparencyLevel: number | null;
  floorLabel: string | null;
  socialLinks: string | null;
  slogan: string | null;
  productColor: string | null;
  contactPhoneNumber: string | null;
  receiptFontSize: number | null;
  categoryFontSize: number | null;
  allowReceiptEditAfterPrint: boolean;
  waiterCanPrintCustomerReceipt: boolean;
  alertMilliseconds: number | null;
  alertRingCount: number | null;
  alertRingIntervalSeconds: number | null;
  moduleFilial: boolean;
  moduleAnbar: boolean;
  moduleRezervasyon: boolean;
  moduleMasaBolge: boolean;
};

function normalizeBranding(item: unknown): CompanySettingsBranding {
  const raw = (item && typeof item === "object" ? item : {}) as Record<string, unknown>;

  const strOrNull = (camel: string, pascal: string) => {
    const v = pick<string | null>(raw, camel, pascal);
    return v === null || v === undefined ? null : String(v);
  };
  const numOrNull = (camel: string, pascal: string) => {
    const v = pick<number | null>(raw, camel, pascal);
    return v === null || v === undefined ? null : Number(v);
  };
  const bool = (camel: string, pascal: string, def = false) =>
    Boolean(pick(raw, camel, pascal) ?? def);

  return {
    loginLogoUrl: strOrNull("loginLogoUrl", "LoginLogoUrl"),
    reportLogoUrl: strOrNull("reportLogoUrl", "ReportLogoUrl"),
    wallpaperUrl: strOrNull("wallpaperUrl", "WallpaperUrl"),
    loginLocation: strOrNull("loginLocation", "LoginLocation"),
    transparencyLevel: numOrNull("transparencyLevel", "TransparencyLevel"),
    floorLabel: strOrNull("floorLabel", "FloorLabel"),
    socialLinks: strOrNull("socialLinks", "SocialLinks"),
    slogan: strOrNull("slogan", "Slogan"),
    productColor: strOrNull("productColor", "ProductColor"),
    contactPhoneNumber: strOrNull("contactPhoneNumber", "ContactPhoneNumber"),
    receiptFontSize: numOrNull("receiptFontSize", "ReceiptFontSize"),
    categoryFontSize: numOrNull("categoryFontSize", "CategoryFontSize"),
    allowReceiptEditAfterPrint: bool("allowReceiptEditAfterPrint", "AllowReceiptEditAfterPrint", true),
    waiterCanPrintCustomerReceipt: bool("waiterCanPrintCustomerReceipt", "WaiterCanPrintCustomerReceipt", true),
    alertMilliseconds: numOrNull("alertMilliseconds", "AlertMilliseconds"),
    alertRingCount: numOrNull("alertRingCount", "AlertRingCount"),
    alertRingIntervalSeconds: numOrNull("alertRingIntervalSeconds", "AlertRingIntervalSeconds"),
    moduleFilial: bool("moduleFilial", "ModuleFilial", true),
    moduleAnbar: bool("moduleAnbar", "ModuleAnbar", true),
    moduleRezervasyon: bool("moduleRezervasyon", "ModuleRezervasyon", true),
    moduleMasaBolge: bool("moduleMasaBolge", "ModuleMasaBolge", true),
  };
}

export async function getCompanySettingsBranding(companyId: number): Promise<CompanySettingsBranding> {
  try {
    const response = await api.get<ApiResponse<unknown>>("/company-settings/branding", {
      params: { companyId },
    });
    const payload = response.data;
    if (payload?.success === false) {
      throw new Error(payload?.message || "Failed to fetch branding");
    }
    return normalizeBranding(payload?.data);
  } catch (error) {
    throw toApiFormError(error, "Failed to fetch branding");
  }
}
