import { api } from "@/lib/api";
import { readBaseResponseList } from "@/lib/api-base-response";

export type BscInvoiceDDto = {
  id: number;
  bscInvoiceDId: number;
  bscInvoiceMId: number;
  lineNo: number;
  itemId: number | null;
  qty: number;
  unitPrice: number;
  amt: number;
  amtVat: number;
  vatRate: number;
  branchId: number | null;
  coId: number | null;
  docDate: string;
  bscCreateDate: string | null;
};

export type BscInvoiceMDto = {
  id: number;
  bscInvoiceMId: number;
  docNo: string | null;
  docDate: string;
  entityId: number | null;
  branchId: number | null;
  coId: number | null;
  amt: number;
  amtVat: number;
  purchaseSales: number | null;
  bscCreateDate: string | null;
  lines: BscInvoiceDDto[];
};

export async function getBscInvoices(params?: { docDate?: string }): Promise<BscInvoiceMDto[]> {
  const query = params?.docDate ? `?docDate=${params.docDate}` : "";
  const res = await api.get(`/bsc-invoices${query}`);
  return readBaseResponseList<BscInvoiceMDto>(res.data);
}

export async function getBscInvoiceById(id: number): Promise<BscInvoiceMDto> {
  const res = await api.get(`/bsc-invoices/${id}`);
  return res.data?.data ?? res.data;
}
