import { api } from "@/lib/api";
import { assertApiSuccess, readBaseResponseData } from "@/lib/api-base-response";
import { toApiFormError } from "@/lib/api-error";

export type CompanyEmailSettings = {
  isConfigured: boolean;
  email: string | null;
  senderName: string | null;
};

export type SaveCompanyEmailInput = {
  email: string;
  senderName: string | null;
  /** Empty keeps the saved password. */
  appPassword: string | null;
};

const URL = "/company-settings/email";

async function run<T>(request: () => Promise<{ data: unknown }>, failure: string): Promise<T | null> {
  try {
    const response = await request();
    assertApiSuccess(response.data);
    return readBaseResponseData<T>(response.data);
  } catch (error) {
    throw toApiFormError(error, failure);
  }
}

export async function getCompanyEmailSettings(): Promise<CompanyEmailSettings> {
  const data = await run<CompanyEmailSettings>(() => api.get(URL), "Poçt ayarları yüklənmədi");
  return data ?? { isConfigured: false, email: null, senderName: null };
}

export async function saveCompanyEmailSettings(input: SaveCompanyEmailInput): Promise<CompanyEmailSettings> {
  const data = await run<CompanyEmailSettings>(() => api.put(URL, input), "Poçt ayarları yadda saxlanmadı");
  return data ?? getCompanyEmailSettings();
}

export async function deleteCompanyEmailSettings(): Promise<void> {
  await run(() => api.delete(URL), "Poçt ayarları silinmədi");
}

export async function sendCompanyTestEmail(to: string): Promise<void> {
  await run(() => api.post(`${URL}/test`, { to }), "Test məktubu göndərilmədi");
}
