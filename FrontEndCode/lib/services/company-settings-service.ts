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
  modulePaket: boolean;
  moduleOtel: boolean;
  moduleFitnes: boolean;
  moduleDataSecimi: boolean;
  moduleQiymetSor: boolean;

  integrationWolt: boolean;
  integrationBolt: boolean;
  integration189Delivery: boolean;

  alertMilliseconds: number | null;
  alertRingCount: number | null;
  alertRingIntervalSeconds: number | null;
  tableTimeWarningMinutes: number | null;

  loginLogoUrl: string | null;
  reportLogoUrl: string | null;
  wallpaperUrl: string | null;
  loginLocation: string | null;
  transparencyLevel: number | null;
  productColor: string | null;
  floorLabel: string | null;
  logoSize: number | null;
  slogan: string | null;
  socialLinks: string | null;
  contactPhoneNumber: string | null;
  receiptFontSize: number | null;
  categoryFontSize: number | null;
  receiptRestaurantNameFontSize: number | null;
  allowReceiptEditAfterPrint: boolean;
  waiterCanPrintCustomerReceipt: boolean;

  printAutoOnPayment: boolean;
  printKitchenOnPayment: boolean;
  printShowPreview: boolean;
  printGroupQuantities: boolean;
  printKitchenGroupQuantities: boolean;
  receiptShowTime: boolean;
  receiptShowWaiterName: boolean;
  receiptShowTableName: boolean;
  receiptShowOrderNumber: boolean;
  receiptShowPaymentMethod: boolean;
  printAskBeforeAutoPrint: boolean;
  receiptSimpleMode: boolean;
  receiptSimpleShowOrderNumber: boolean;
  receiptSimpleShowWaiterName: boolean;
  receiptSimpleShowTime: boolean;
  receiptSimpleShowPaymentMethod: boolean;
  receiptSimpleShowVat: boolean;
  receiptSimpleShowFooter: boolean;
  printKitchenShowBusinessName: boolean;
  receiptShowBusinessName: boolean;
  printKitchenOnHold: boolean;
  printTransferDocAuto: boolean;
  printTransferDocDouble: boolean;
  printChiefCopy: boolean;

  askGuestCountOnOpen: boolean;
  singleWaiterMode: boolean;
  defaultVatPercent: number | null;
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
    modulePaket: bool("modulePaket", "ModulePaket"),
    moduleOtel: bool("moduleOtel", "ModuleOtel"),
    moduleFitnes: bool("moduleFitnes", "ModuleFitnes"),
    moduleDataSecimi: bool("moduleDataSecimi", "ModuleDataSecimi"),
    moduleQiymetSor: bool("moduleQiymetSor", "ModuleQiymetSor"),

    integrationWolt: bool("integrationWolt", "IntegrationWolt"),
    integrationBolt: bool("integrationBolt", "IntegrationBolt"),
    integration189Delivery: bool("integration189Delivery", "Integration189Delivery"),

    alertMilliseconds: numOrNull("alertMilliseconds", "AlertMilliseconds"),
    alertRingCount: numOrNull("alertRingCount", "AlertRingCount"),
    alertRingIntervalSeconds: numOrNull("alertRingIntervalSeconds", "AlertRingIntervalSeconds"),
    tableTimeWarningMinutes: numOrNull("tableTimeWarningMinutes", "TableTimeWarningMinutes"),

    loginLogoUrl: strOrNull("loginLogoUrl", "LoginLogoUrl"),
    reportLogoUrl: strOrNull("reportLogoUrl", "ReportLogoUrl"),
    wallpaperUrl: strOrNull("wallpaperUrl", "WallpaperUrl"),
    loginLocation: strOrNull("loginLocation", "LoginLocation"),
    transparencyLevel: numOrNull("transparencyLevel", "TransparencyLevel"),
    productColor: strOrNull("productColor", "ProductColor"),
    floorLabel: strOrNull("floorLabel", "FloorLabel"),
    logoSize: numOrNull("logoSize", "LogoSize"),
    slogan: strOrNull("slogan", "Slogan"),
    socialLinks: strOrNull("socialLinks", "SocialLinks"),
    contactPhoneNumber: strOrNull("contactPhoneNumber", "ContactPhoneNumber"),
    receiptFontSize: numOrNull("receiptFontSize", "ReceiptFontSize"),
    categoryFontSize: numOrNull("categoryFontSize", "CategoryFontSize"),
    receiptRestaurantNameFontSize: numOrNull("receiptRestaurantNameFontSize", "ReceiptRestaurantNameFontSize"),
    allowReceiptEditAfterPrint: bool("allowReceiptEditAfterPrint", "AllowReceiptEditAfterPrint", true),
    waiterCanPrintCustomerReceipt: bool("waiterCanPrintCustomerReceipt", "WaiterCanPrintCustomerReceipt", true),

    printAutoOnPayment: bool("printAutoOnPayment", "PrintAutoOnPayment"),
    printKitchenOnPayment: bool("printKitchenOnPayment", "PrintKitchenOnPayment"),
    printShowPreview: bool("printShowPreview", "PrintShowPreview", true),
    printGroupQuantities: bool("printGroupQuantities", "PrintGroupQuantities", true),
    printKitchenGroupQuantities: bool("printKitchenGroupQuantities", "PrintKitchenGroupQuantities"),
    receiptShowTime: bool("receiptShowTime", "ReceiptShowTime", true),
    receiptShowWaiterName: bool("receiptShowWaiterName", "ReceiptShowWaiterName", true),
    receiptShowTableName: bool("receiptShowTableName", "ReceiptShowTableName", true),
    receiptShowOrderNumber: bool("receiptShowOrderNumber", "ReceiptShowOrderNumber", true),
    receiptShowPaymentMethod: bool("receiptShowPaymentMethod", "ReceiptShowPaymentMethod", true),
    printAskBeforeAutoPrint: bool("printAskBeforeAutoPrint", "PrintAskBeforeAutoPrint"),
    receiptSimpleMode: bool("receiptSimpleMode", "ReceiptSimpleMode"),
    receiptSimpleShowOrderNumber: bool("receiptSimpleShowOrderNumber", "ReceiptSimpleShowOrderNumber"),
    receiptSimpleShowWaiterName: bool("receiptSimpleShowWaiterName", "ReceiptSimpleShowWaiterName"),
    receiptSimpleShowTime: bool("receiptSimpleShowTime", "ReceiptSimpleShowTime"),
    receiptSimpleShowPaymentMethod: bool("receiptSimpleShowPaymentMethod", "ReceiptSimpleShowPaymentMethod"),
    receiptSimpleShowVat: bool("receiptSimpleShowVat", "ReceiptSimpleShowVat"),
    receiptSimpleShowFooter: bool("receiptSimpleShowFooter", "ReceiptSimpleShowFooter"),
    printKitchenShowBusinessName: bool("printKitchenShowBusinessName", "PrintKitchenShowBusinessName", true),
    receiptShowBusinessName: bool("receiptShowBusinessName", "ReceiptShowBusinessName", true),
    printKitchenOnHold: bool("printKitchenOnHold", "PrintKitchenOnHold"),
    printTransferDocAuto: bool("printTransferDocAuto", "PrintTransferDocAuto"),
    printTransferDocDouble: bool("printTransferDocDouble", "PrintTransferDocDouble"),
    printChiefCopy: bool("printChiefCopy", "PrintChiefCopy"),
    askGuestCountOnOpen: bool("askGuestCountOnOpen", "AskGuestCountOnOpen"),
    singleWaiterMode: bool("singleWaiterMode", "SingleWaiterMode"),
    defaultVatPercent: numOrNull("defaultVatPercent", "DefaultVatPercent"),
  };
}

export async function getCompanySettings(companyId?: number): Promise<CompanySettings> {
  try {
    const response = await api.get<ApiResponse<unknown>>("/company-settings", {
      params: companyId ? { companyId } : undefined,
    });
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

export async function updateCompanySettings(
  data: CompanySettingsInput,
  companyId?: number,
): Promise<CompanySettings> {
  try {
    const response = await api.put<ApiResponse<unknown>>("/company-settings", data, {
      params: companyId ? { companyId } : undefined,
    });
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
  openingTime: string | null;
  loginLogoUrl: string | null;
  reportLogoUrl: string | null;
  wallpaperUrl: string | null;
  loginLocation: string | null;
  transparencyLevel: number | null;
  floorLabel: string | null;
  logoSize: number | null;
  socialLinks: string | null;
  slogan: string | null;
  productColor: string | null;
  contactPhoneNumber: string | null;
  receiptFontSize: number | null;
  categoryFontSize: number | null;
  receiptRestaurantNameFontSize: number | null;
  allowReceiptEditAfterPrint: boolean;
  waiterCanPrintCustomerReceipt: boolean;
  alertMilliseconds: number | null;
  alertRingCount: number | null;
  alertRingIntervalSeconds: number | null;
  tableTimeWarningMinutes: number | null;
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
  printAutoOnPayment: boolean;
  printKitchenOnPayment: boolean;
  printShowPreview: boolean;
  printGroupQuantities: boolean;
  printKitchenGroupQuantities: boolean;
  receiptShowTime: boolean;
  receiptShowWaiterName: boolean;
  receiptShowTableName: boolean;
  receiptShowOrderNumber: boolean;
  receiptShowPaymentMethod: boolean;
  printAskBeforeAutoPrint: boolean;
  receiptSimpleMode: boolean;
  receiptSimpleShowOrderNumber: boolean;
  receiptSimpleShowWaiterName: boolean;
  receiptSimpleShowTime: boolean;
  receiptSimpleShowPaymentMethod: boolean;
  receiptSimpleShowVat: boolean;
  receiptSimpleShowFooter: boolean;
  printKitchenShowBusinessName: boolean;
  receiptShowBusinessName: boolean;
  printKitchenOnHold: boolean;
  printTransferDocAuto: boolean;
  printTransferDocDouble: boolean;
  printChiefCopy: boolean;
  askGuestCountOnOpen: boolean;
  singleWaiterMode: boolean;
  defaultVatPercent: number | null;
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
    openingTime: strOrNull("openingTime", "OpeningTime"),
    loginLogoUrl: strOrNull("loginLogoUrl", "LoginLogoUrl"),
    reportLogoUrl: strOrNull("reportLogoUrl", "ReportLogoUrl"),
    wallpaperUrl: strOrNull("wallpaperUrl", "WallpaperUrl"),
    loginLocation: strOrNull("loginLocation", "LoginLocation"),
    transparencyLevel: numOrNull("transparencyLevel", "TransparencyLevel"),
    floorLabel: strOrNull("floorLabel", "FloorLabel"),
    logoSize: numOrNull("logoSize", "LogoSize"),
    socialLinks: strOrNull("socialLinks", "SocialLinks"),
    slogan: strOrNull("slogan", "Slogan"),
    productColor: strOrNull("productColor", "ProductColor"),
    contactPhoneNumber: strOrNull("contactPhoneNumber", "ContactPhoneNumber"),
    receiptFontSize: numOrNull("receiptFontSize", "ReceiptFontSize"),
    categoryFontSize: numOrNull("categoryFontSize", "CategoryFontSize"),
    receiptRestaurantNameFontSize: numOrNull("receiptRestaurantNameFontSize", "ReceiptRestaurantNameFontSize"),
    allowReceiptEditAfterPrint: bool("allowReceiptEditAfterPrint", "AllowReceiptEditAfterPrint", true),
    waiterCanPrintCustomerReceipt: bool("waiterCanPrintCustomerReceipt", "WaiterCanPrintCustomerReceipt", true),
    alertMilliseconds: numOrNull("alertMilliseconds", "AlertMilliseconds"),
    alertRingCount: numOrNull("alertRingCount", "AlertRingCount"),
    alertRingIntervalSeconds: numOrNull("alertRingIntervalSeconds", "AlertRingIntervalSeconds"),
    tableTimeWarningMinutes: numOrNull("tableTimeWarningMinutes", "TableTimeWarningMinutes"),
    moduleFilial: bool("moduleFilial", "ModuleFilial", true),
    moduleAnbar: bool("moduleAnbar", "ModuleAnbar", true),
    moduleRezervasyon: bool("moduleRezervasyon", "ModuleRezervasyon", true),
    moduleMasaBolge: bool("moduleMasaBolge", "ModuleMasaBolge", true),
    modulePaket: bool("modulePaket", "ModulePaket"),
    moduleOtel: bool("moduleOtel", "ModuleOtel"),
    moduleFitnes: bool("moduleFitnes", "ModuleFitnes"),
    moduleDataSecimi: bool("moduleDataSecimi", "ModuleDataSecimi"),
    moduleQiymetSor: bool("moduleQiymetSor", "ModuleQiymetSor"),
    moduleKompleks: bool("moduleKompleks", "ModuleKompleks"),
    printAutoOnPayment: bool("printAutoOnPayment", "PrintAutoOnPayment"),
    printKitchenOnPayment: bool("printKitchenOnPayment", "PrintKitchenOnPayment"),
    printShowPreview: bool("printShowPreview", "PrintShowPreview", true),
    printGroupQuantities: bool("printGroupQuantities", "PrintGroupQuantities", true),
    printKitchenGroupQuantities: bool("printKitchenGroupQuantities", "PrintKitchenGroupQuantities"),
    receiptShowTime: bool("receiptShowTime", "ReceiptShowTime", true),
    receiptShowWaiterName: bool("receiptShowWaiterName", "ReceiptShowWaiterName", true),
    receiptShowTableName: bool("receiptShowTableName", "ReceiptShowTableName", true),
    receiptShowOrderNumber: bool("receiptShowOrderNumber", "ReceiptShowOrderNumber", true),
    receiptShowPaymentMethod: bool("receiptShowPaymentMethod", "ReceiptShowPaymentMethod", true),
    printAskBeforeAutoPrint: bool("printAskBeforeAutoPrint", "PrintAskBeforeAutoPrint"),
    receiptSimpleMode: bool("receiptSimpleMode", "ReceiptSimpleMode"),
    receiptSimpleShowOrderNumber: bool("receiptSimpleShowOrderNumber", "ReceiptSimpleShowOrderNumber"),
    receiptSimpleShowWaiterName: bool("receiptSimpleShowWaiterName", "ReceiptSimpleShowWaiterName"),
    receiptSimpleShowTime: bool("receiptSimpleShowTime", "ReceiptSimpleShowTime"),
    receiptSimpleShowPaymentMethod: bool("receiptSimpleShowPaymentMethod", "ReceiptSimpleShowPaymentMethod"),
    receiptSimpleShowVat: bool("receiptSimpleShowVat", "ReceiptSimpleShowVat"),
    receiptSimpleShowFooter: bool("receiptSimpleShowFooter", "ReceiptSimpleShowFooter"),
    printKitchenShowBusinessName: bool("printKitchenShowBusinessName", "PrintKitchenShowBusinessName", true),
    receiptShowBusinessName: bool("receiptShowBusinessName", "ReceiptShowBusinessName", true),
    printKitchenOnHold: bool("printKitchenOnHold", "PrintKitchenOnHold"),
    printTransferDocAuto: bool("printTransferDocAuto", "PrintTransferDocAuto"),
    printTransferDocDouble: bool("printTransferDocDouble", "PrintTransferDocDouble"),
    printChiefCopy: bool("printChiefCopy", "PrintChiefCopy"),
    askGuestCountOnOpen: bool("askGuestCountOnOpen", "AskGuestCountOnOpen"),
    singleWaiterMode: bool("singleWaiterMode", "SingleWaiterMode"),
    defaultVatPercent: numOrNull("defaultVatPercent", "DefaultVatPercent"),
  };
}

export async function getCompanySettingsBranding(
  companyId: number,
  restaurantId?: number,
): Promise<CompanySettingsBranding> {
  try {
    const response = await api.get<ApiResponse<unknown>>("/company-settings/branding", {
      params: restaurantId ? { companyId, restaurantId } : { companyId },
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
