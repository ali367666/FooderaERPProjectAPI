import axios from "axios";
import { api } from "@/lib/api";
import { toApiFormError } from "@/lib/api-error";

export type LicenseStatus = {
  companyId: number;
  deploymentType: "Cloud" | "Local";
  remoteAccessEnabled: boolean;
  remoteAccessActive: boolean;
  remoteAccessExpiresAtUtc: string | null;
  daysLeft: number | null;
  offlineModeEnabled: boolean;
  deviceRegistered: boolean;
  isLocalInstallation: boolean;
  licenseKeyExpiresAtUtc: string | null;
  licenseKeyValid: boolean;
};

export type LicensePayment = {
  id: number;
  months: number;
  amount: number;
  periodFromUtc: string;
  periodToUtc: string;
  note: string | null;
  createdByUserName: string | null;
  createdAtUtc: string;
};

export type TrustedDevice = {
  id: number;
  name: string;
  isActive: boolean;
  createdAtUtc: string;
  lastSeenAtUtc: string | null;
};

export type CompanyLicenseDetail = LicenseStatus & {
  monthlyPrice: number | null;
  payments: LicensePayment[];
  devices: TrustedDevice[];
};

export type UpdateLicenseInput = {
  deploymentType: "Cloud" | "Local";
  remoteAccessEnabled: boolean;
  remoteAccessExpiresAtUtc: string | null;
  offlineModeEnabled: boolean;
  monthlyPrice: number | null;
};

type Raw = Record<string, unknown>;
const pick = (o: Raw, c: string, p: string) => (o[c] !== undefined ? o[c] : o[p]);

function normalizeStatus(raw: Raw): LicenseStatus {
  const days = pick(raw, "daysLeft", "DaysLeft");
  return {
    companyId: Number(pick(raw, "companyId", "CompanyId") ?? 0),
    deploymentType: pick(raw, "deploymentType", "DeploymentType") === "Local" ? "Local" : "Cloud",
    remoteAccessEnabled: Boolean(pick(raw, "remoteAccessEnabled", "RemoteAccessEnabled")),
    remoteAccessActive: Boolean(pick(raw, "remoteAccessActive", "RemoteAccessActive")),
    remoteAccessExpiresAtUtc: (pick(raw, "remoteAccessExpiresAtUtc", "RemoteAccessExpiresAtUtc") as string | null) ?? null,
    daysLeft: days == null ? null : Number(days),
    offlineModeEnabled: Boolean(pick(raw, "offlineModeEnabled", "OfflineModeEnabled")),
    deviceRegistered: Boolean(pick(raw, "deviceRegistered", "DeviceRegistered")),
    isLocalInstallation: Boolean(pick(raw, "isLocalInstallation", "IsLocalInstallation")),
    licenseKeyExpiresAtUtc: (pick(raw, "licenseKeyExpiresAtUtc", "LicenseKeyExpiresAtUtc") as string | null) ?? null,
    licenseKeyValid: Boolean(pick(raw, "licenseKeyValid", "LicenseKeyValid")),
  };
}

function normalizeDetail(raw: Raw): CompanyLicenseDetail {
  const payments = (pick(raw, "payments", "Payments") as Raw[] | undefined) ?? [];
  const devices = (pick(raw, "devices", "Devices") as Raw[] | undefined) ?? [];
  const price = pick(raw, "monthlyPrice", "MonthlyPrice");
  return {
    ...normalizeStatus(raw),
    monthlyPrice: price == null ? null : Number(price),
    payments: payments.map((p) => ({
      id: Number(pick(p, "id", "Id")),
      months: Number(pick(p, "months", "Months")),
      amount: Number(pick(p, "amount", "Amount")),
      periodFromUtc: String(pick(p, "periodFromUtc", "PeriodFromUtc")),
      periodToUtc: String(pick(p, "periodToUtc", "PeriodToUtc")),
      note: (pick(p, "note", "Note") as string | null) ?? null,
      createdByUserName: (pick(p, "createdByUserName", "CreatedByUserName") as string | null) ?? null,
      createdAtUtc: String(pick(p, "createdAtUtc", "CreatedAtUtc")),
    })),
    devices: devices.map((d) => ({
      id: Number(pick(d, "id", "Id")),
      name: String(pick(d, "name", "Name") ?? ""),
      isActive: Boolean(pick(d, "isActive", "IsActive")),
      createdAtUtc: String(pick(d, "createdAtUtc", "CreatedAtUtc")),
      lastSeenAtUtc: (pick(d, "lastSeenAtUtc", "LastSeenAtUtc") as string | null) ?? null,
    })),
  };
}

export async function getMyLicenseStatus(): Promise<LicenseStatus> {
  try {
    const res = await api.get<Raw>("/Licenses/me");
    return normalizeStatus(res.data);
  } catch (error) {
    throw toApiFormError(error, "Lisenziya vəziyyəti alınmadı");
  }
}

export async function getCompanyLicense(companyId: number): Promise<CompanyLicenseDetail> {
  try {
    const res = await api.get<Raw>(`/Licenses/${companyId}`);
    return normalizeDetail(res.data);
  } catch (error) {
    throw toApiFormError(error, "Lisenziya yüklənmədi");
  }
}

export async function updateCompanyLicense(companyId: number, input: UpdateLicenseInput): Promise<CompanyLicenseDetail> {
  try {
    const res = await api.put<Raw>(`/Licenses/${companyId}`, input);
    return normalizeDetail(res.data);
  } catch (error) {
    throw toApiFormError(error, "Lisenziya yadda saxlanmadı");
  }
}

export async function extendCompanyLicense(
  companyId: number,
  input: { months: number; amount: number; note: string | null },
): Promise<CompanyLicenseDetail> {
  try {
    const res = await api.post<Raw>(`/Licenses/${companyId}/extend`, input);
    return normalizeDetail(res.data);
  } catch (error) {
    throw toApiFormError(error, "Lisenziya uzadılmadı");
  }
}

export async function createRegistrationCode(companyId: number): Promise<{ code: string; expiresAtUtc: string }> {
  try {
    const res = await api.post<Raw>(`/Licenses/${companyId}/registration-code`);
    return {
      code: String(pick(res.data, "code", "Code")),
      expiresAtUtc: String(pick(res.data, "expiresAtUtc", "ExpiresAtUtc")),
    };
  } catch (error) {
    throw toApiFormError(error, "Kod yaradılmadı");
  }
}

export async function revokeDevice(deviceId: number): Promise<void> {
  try {
    await api.delete(`/Licenses/devices/${deviceId}`);
  } catch (error) {
    throw toApiFormError(error, "Cihaz ləğv edilmədi");
  }
}

function errorMessage(error: unknown): string | null {
  if (axios.isAxiosError(error)) {
    return (error.response?.data as { message?: string } | undefined)?.message ?? null;
  }
  return null;
}

/** Central server: signs a licence key for a Local installation. */
export async function issueLicenseKey(companyId: number): Promise<{ licenseKey: string; expiresAtUtc: string }> {
  try {
    const res = await api.post<Raw>(`/Licenses/${companyId}/license-key`);
    return {
      licenseKey: String(pick(res.data, "licenseKey", "LicenseKey")),
      expiresAtUtc: String(pick(res.data, "expiresAtUtc", "ExpiresAtUtc")),
    };
  } catch (error) {
    const message = errorMessage(error);
    if (message) throw new Error(message);
    throw toApiFormError(error, "Lisenziya açarı yaradılmadı");
  }
}

/** Local installation: activates the key received from the platform. */
export async function importLicenseKey(licenseKey: string): Promise<LicenseStatus> {
  try {
    const res = await api.post<Raw>("/Licenses/import", { licenseKey });
    return normalizeStatus(res.data);
  } catch (error) {
    const message = errorMessage(error);
    if (message) throw new Error(message);
    throw toApiFormError(error, "Lisenziya açarı qəbul edilmədi");
  }
}

/** Anonymous — the device has no account of its own, only the one-time code. */
export async function registerDevice(code: string, name: string): Promise<{ deviceKey: string; deviceName: string }> {
  try {
    const res = await api.post<Raw>("/Devices/register", { code, name });
    return {
      deviceKey: String(pick(res.data, "deviceKey", "DeviceKey")),
      deviceName: String(pick(res.data, "deviceName", "DeviceName")),
    };
  } catch (error) {
    if (axios.isAxiosError(error)) {
      const message = (error.response?.data as { message?: string } | undefined)?.message;
      if (message) throw new Error(message);
    }
    throw toApiFormError(error, "Cihaz qeydiyyatdan keçmədi");
  }
}
