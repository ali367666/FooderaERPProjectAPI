import { api } from "@/lib/api";
import { toApiFormError } from "@/lib/api-error";

export type DiscountType = "Percentage" | "FixedAmount";

export type DiscountDto = {
  id: number;
  code: string;
  name: string;
  type: DiscountType;
  value: number;
  minOrderAmount: number | null;
  maxDiscountAmount: number | null;
  startDate: string;
  endDate: string;
  startTime: string | null;
  endTime: string | null;
  maxUsageCount: number | null;
  usedCount: number;
  isActive: boolean;
  isCurrentlyValid: boolean;
};

export type DiscountInput = {
  code: string;
  name: string;
  type: DiscountType;
  value: number;
  minOrderAmount?: number | null;
  maxDiscountAmount?: number | null;
  startDate: string;
  endDate: string;
  startTime?: string | null;
  endTime?: string | null;
  maxUsageCount?: number | null;
  isActive?: boolean;
};

export async function getDiscounts(): Promise<DiscountDto[]> {
  try {
    const res = await api.get<DiscountDto[]>("/Discounts");
    return Array.isArray(res.data) ? res.data : [];
  } catch (e) {
    throw toApiFormError(e, "Endirimlər yüklənmədi");
  }
}

export async function createDiscount(data: DiscountInput): Promise<DiscountDto> {
  try {
    const res = await api.post<DiscountDto>("/Discounts", data);
    return res.data;
  } catch (e) {
    throw toApiFormError(e, "Endirim yaradılmadı");
  }
}

export async function updateDiscount(id: number, data: DiscountInput): Promise<DiscountDto> {
  try {
    const res = await api.put<DiscountDto>(`/Discounts/${id}`, data);
    return res.data;
  } catch (e) {
    throw toApiFormError(e, "Endirim yenilənmədi");
  }
}

export async function deleteDiscount(id: number): Promise<void> {
  try {
    await api.delete(`/Discounts/${id}`);
  } catch (e) {
    throw toApiFormError(e, "Endirim silinmədi");
  }
}
