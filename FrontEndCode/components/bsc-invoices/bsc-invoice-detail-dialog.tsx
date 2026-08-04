"use client";

import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import type { BscInvoiceMDto } from "@/lib/services/bsc-invoice-service";

function formatDate(iso: string): string {
  if (!iso) return "—";
  const d = new Date(iso);
  return Number.isNaN(d.getTime()) ? iso : d.toLocaleDateString("az-AZ", { year: "numeric", month: "2-digit", day: "2-digit" });
}

function formatAzn(val: number): string {
  return val.toLocaleString("az-AZ", { minimumFractionDigits: 2, maximumFractionDigits: 2 }) + " ₼";
}

type Props = {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  invoice: BscInvoiceMDto | null;
};

export function BscInvoiceDetailDialog({ open, onOpenChange, invoice }: Props) {
  if (!invoice) return null;

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-w-4xl max-h-[85vh] overflow-y-auto">
        <DialogHeader>
          <DialogTitle>
            Faktura Detalları — {invoice.docNo ?? `BSC#${invoice.bscInvoiceMId}`}
          </DialogTitle>
        </DialogHeader>

        <div className="grid grid-cols-2 sm:grid-cols-3 gap-3 text-sm mb-4">
          <div><span className="text-muted-foreground">BSC ID:</span> <span className="font-mono">{invoice.bscInvoiceMId}</span></div>
          <div><span className="text-muted-foreground">Sənəd №:</span> {invoice.docNo ?? "—"}</div>
          <div><span className="text-muted-foreground">Tarix:</span> {formatDate(invoice.docDate)}</div>
          <div><span className="text-muted-foreground">Entity ID:</span> {invoice.entityId ?? "—"}</div>
          <div><span className="text-muted-foreground">Filial ID:</span> {invoice.branchId ?? "—"}</div>
          <div><span className="text-muted-foreground">Şirkət ID:</span> {invoice.coId ?? "—"}</div>
          <div><span className="text-muted-foreground">Məbləğ:</span> {formatAzn(invoice.amt)}</div>
          <div><span className="text-muted-foreground">ƏDV:</span> {formatAzn(invoice.amtVat)}</div>
          <div><span className="text-muted-foreground">Cəmi:</span> <span className="font-semibold">{formatAzn(invoice.amt + invoice.amtVat)}</span></div>
        </div>

        <div className="border rounded-lg overflow-x-auto">
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b bg-muted/40">
                <th className="text-left py-2 px-3 font-medium">Sətir</th>
                <th className="text-left py-2 px-3 font-medium">Item ID</th>
                <th className="text-right py-2 px-3 font-medium">Miqdar</th>
                <th className="text-right py-2 px-3 font-medium">Vahid qiymət</th>
                <th className="text-right py-2 px-3 font-medium">Məbləğ</th>
                <th className="text-right py-2 px-3 font-medium">ƏDV %</th>
                <th className="text-right py-2 px-3 font-medium">ƏDV</th>
              </tr>
            </thead>
            <tbody>
              {invoice.lines.length === 0 ? (
                <tr>
                  <td colSpan={7} className="py-6 text-center text-muted-foreground">Sətir yoxdur.</td>
                </tr>
              ) : (
                invoice.lines.map((line) => (
                  <tr key={line.id} className="border-b last:border-0">
                    <td className="py-2 px-3">{line.lineNo}</td>
                    <td className="py-2 px-3 font-mono text-xs">{line.itemId ?? "—"}</td>
                    <td className="py-2 px-3 text-right tabular-nums">{line.qty.toLocaleString("az-AZ", { maximumFractionDigits: 3 })}</td>
                    <td className="py-2 px-3 text-right tabular-nums">{formatAzn(line.unitPrice)}</td>
                    <td className="py-2 px-3 text-right tabular-nums">{formatAzn(line.amt)}</td>
                    <td className="py-2 px-3 text-right tabular-nums">{line.vatRate}%</td>
                    <td className="py-2 px-3 text-right tabular-nums">{formatAzn(line.amtVat)}</td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>
      </DialogContent>
    </Dialog>
  );
}
