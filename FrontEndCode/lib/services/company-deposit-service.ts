import { api } from "@/lib/api";
import { assertApiSuccess } from "@/lib/api-base-response";

export type CompanyDeposit = {
  id: number;
  amount: number;
  /** True for the amount the company started working with. */
  isOpening: boolean;
  note: string | null;
  createdAtUtc: string;
  createdByUserName: string | null;
};

export type CompanyDepositSummary = {
  openingAmount: number | null;
  openingAtUtc: string | null;
  totalAmount: number;
  entries: CompanyDeposit[];
};

export async function getCompanyDeposits(): Promise<CompanyDepositSummary> {
  const res = await api.get<CompanyDepositSummary>("/company-deposits");
  return res.data;
}

/** Records a deposit; the company's first one is its opening deposit. */
export async function addCompanyDeposit(amount: number, note?: string | null): Promise<string | null> {
  const res = await api.post("/company-deposits", { amount, note: note ?? null });
  assertApiSuccess(res.data);
  return (res.data as { message?: string } | undefined)?.message ?? null;
}
