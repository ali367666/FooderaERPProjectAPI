import { api } from "@/lib/api";
import { toApiFormError } from "@/lib/api-error";

export const FinanceEntryType = { Income: 1, Expense: 2 } as const;
export type FinanceEntryTypeValue = (typeof FinanceEntryType)[keyof typeof FinanceEntryType];

export const FinanceGranularity = { Week: 1, Month: 2, Year: 3 } as const;
export type FinanceGranularityValue = (typeof FinanceGranularity)[keyof typeof FinanceGranularity];

export type FinanceLine = {
  entryId: number | null;
  name: string;
  amount: number;
  source: string;
  note: string | null;
};

export type FinanceDay = {
  date: string;
  income: number;
  expense: number;
  profit: number;
  incomeLines: FinanceLine[];
  expenseLines: FinanceLine[];
};

export type FinanceBucket = {
  label: string;
  fromDate: string;
  toDate: string;
  income: number;
  expense: number;
  profit: number;
};

export type FinanceNameTotal = { name: string; count: number; total: number };

export type FinanceReport = {
  granularity: FinanceGranularityValue;
  fromDate: string;
  toDate: string;
  totalIncome: number;
  totalExpense: number;
  profit: number;
  buckets: FinanceBucket[];
  days: FinanceDay[];
  incomeByName: FinanceNameTotal[];
  expenseByName: FinanceNameTotal[];
};

export type FinanceEntryPayload = {
  restaurantId?: number | null;
  type: FinanceEntryTypeValue;
  name: string;
  amount: number;
  entryDate: string;
  note?: string | null;
};

export type FinanceEntry = FinanceEntryPayload & { id: number; createdByUserName: string | null };

export async function getFinanceReport(params: {
  granularity: FinanceGranularityValue;
  date: string;
  restaurantId?: number | null;
}): Promise<FinanceReport> {
  try {
    const response = await api.get<FinanceReport>("/Finance/report", {
      params: {
        granularity: params.granularity,
        date: params.date,
        restaurantId: params.restaurantId ?? undefined,
      },
    });
    return response.data;
  } catch (error) {
    throw toApiFormError(error, "Maliyyə hesabatı yüklənmədi.");
  }
}

export async function getFinanceEntry(id: number, date: string): Promise<FinanceEntry | null> {
  try {
    const response = await api.get<FinanceEntry[]>("/Finance/entries", { params: { from: date, to: date } });
    return response.data.find((e) => e.id === id) ?? null;
  } catch (error) {
    throw toApiFormError(error, "Qeyd yüklənmədi.");
  }
}

export async function createFinanceEntry(payload: FinanceEntryPayload): Promise<FinanceEntry> {
  try {
    const response = await api.post<FinanceEntry>("/Finance/entries", payload);
    return response.data;
  } catch (error) {
    throw toApiFormError(error, "Qeyd əlavə edilmədi.");
  }
}

export async function updateFinanceEntry(id: number, payload: FinanceEntryPayload): Promise<FinanceEntry> {
  try {
    const response = await api.put<FinanceEntry>(`/Finance/entries/${id}`, payload);
    return response.data;
  } catch (error) {
    throw toApiFormError(error, "Qeyd yenilənmədi.");
  }
}

export async function deleteFinanceEntry(id: number): Promise<void> {
  try {
    await api.delete(`/Finance/entries/${id}`);
  } catch (error) {
    throw toApiFormError(error, "Qeyd silinmədi.");
  }
}
