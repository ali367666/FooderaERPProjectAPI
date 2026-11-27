import { api } from "@/lib/api";
import { assertApiSuccess, readBaseResponseData } from "@/lib/api-base-response";
import { toApiFormError } from "@/lib/api-error";

export type MyProfile = {
  id: number;
  fullName: string;
  userName: string | null;
  email: string | null;
  phoneNumber: string | null;
  code: string | null;
  avatarUrl: string | null;
  roles: string[];
  /** 1 = head office, 2 = restaurant. */
  workplaceType: number;
  restaurantName: string | null;
  warehouseName: string | null;
  departmentName: string | null;
  positionName: string | null;
  hireDate: string | null;
  companyId: number;
  companyName: string | null;
  companyCode: string | null;
  companyTaxNumber: string | null;
  companyAddress: string | null;
  companyPhoneNumber: string | null;
  companyEmail: string | null;
};

export async function getMyProfile(): Promise<MyProfile> {
  try {
    const response = await api.get("/profile");
    assertApiSuccess(response.data);
    const data = readBaseResponseData<MyProfile>(response.data);
    if (!data) throw new Error("Profil yüklənmədi");
    return data;
  } catch (error) {
    throw toApiFormError(error, "Profil yüklənmədi");
  }
}

export async function uploadMyAvatar(file: File): Promise<string> {
  try {
    const formData = new FormData();
    formData.append("file", file);
    const response = await api.post("/profile/avatar", formData, {
      headers: { "Content-Type": "multipart/form-data" },
    });
    assertApiSuccess(response.data);
    const url = readBaseResponseData<string>(response.data);
    if (!url) throw new Error("Şəkil yüklənmədi");
    return url;
  } catch (error) {
    throw toApiFormError(error, "Şəkil yüklənmədi");
  }
}

export async function removeMyAvatar(): Promise<void> {
  try {
    const response = await api.delete("/profile/avatar");
    assertApiSuccess(response.data);
  } catch (error) {
    throw toApiFormError(error, "Şəkil silinmədi");
  }
}
