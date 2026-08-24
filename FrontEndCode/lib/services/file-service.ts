import { api } from "@/lib/api";
import { toApiFormError } from "@/lib/api-error";

type ApiResponse<T> = {
  success?: boolean;
  message?: string;
  data?: T;
};

export async function uploadFile(file: File): Promise<string> {
  try {
    const formData = new FormData();
    formData.append("file", file);

    const response = await api.post<ApiResponse<{ url: string }>>("/files/upload", formData, {
      headers: { "Content-Type": "multipart/form-data" },
    });

    const payload = response.data;
    if (payload?.success === false || !payload?.data?.url) {
      throw new Error(payload?.message || "Fayl yüklənmədi");
    }
    return payload.data.url;
  } catch (error) {
    throw toApiFormError(error, "Fayl yüklənmədi");
  }
}
