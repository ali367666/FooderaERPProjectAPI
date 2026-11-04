"use client";

import { Printer } from "lucide-react";
import { toast } from "sonner";
import { Button } from "@/components/ui/button";
import { formatCurrency } from "@/lib/format-currency";
import { cn } from "@/lib/utils";
import { printStoreShiftReport } from "@/lib/store/store-receipt";
import { PAYMENT_LABELS, StoreCashMovementType, type StoreShiftReport } from "@/lib/store/store-service";

function Row({ label, value, strong }: { label: string; value: string; strong?: boolean }) {
  return (
    <div className={cn("flex justify-between py-1 text-sm", strong && "font-semibold")}>
      <span>{label}</span>
      <span>{value}</span>
    </div>
  );
}

/** X-Report of an open shift or Z-Report of a closed one; the same numbers either way. */
export function ShiftReportView({ report }: { report: StoreShiftReport }) {
  const s = report.shift;
  return (
    <div className="space-y-3">
      <div className="flex items-start justify-between gap-2">
        <div>
          <p className="font-semibold">
            {report.isFinal ? "Z-Hesabat (növbə bağlanıb)" : "X-Hesabat (növbə açıqdır)"}
          </p>
          <p className="text-xs text-muted-foreground">
            {s.registerName} — {s.cashierName ?? "kassir"} — {new Date(s.openedAtUtc).toLocaleString("az-AZ")}
            {s.closedAtUtc && ` → ${new Date(s.closedAtUtc).toLocaleString("az-AZ")}`}
          </p>
        </div>
        <Button
          size="sm"
          variant="outline"
          onClick={() => {
            if (!printStoreShiftReport(report)) toast.error("Çap pəncərəsi açılmadı — popup bloklamasını söndürün.");
          }}
        >
          <Printer className="mr-1 h-4 w-4" />
          Çap et
        </Button>
      </div>

      <div className="rounded-md border p-3">
        <Row label="Satış sayı" value={String(report.salesCount)} />
        <Row label="Ümumi satış" value={formatCurrency(report.grossSales)} />
        <Row label="Endirim" value={formatCurrency(report.discounts)} />
        <Row label="Xalis satış" value={formatCurrency(report.netSales)} strong />
        <Row label="ƏDV (daxil)" value={formatCurrency(report.vat)} />
        <Row label={`Ləğv edilən (${report.cancelledCount})`} value={formatCurrency(report.cancelledTotal)} />
        <Row label={`Geri qaytarma (${report.returnsCount})`} value={formatCurrency(report.returnsTotal)} />
      </div>

      <div className="rounded-md border p-3">
        <p className="mb-1 text-xs font-medium text-muted-foreground">Ödəniş üsulları</p>
        {report.payments.length === 0 ? (
          <p className="text-sm text-muted-foreground">Satış yoxdur.</p>
        ) : (
          report.payments.map((p) => <Row key={p.method} label={PAYMENT_LABELS[p.method] ?? String(p.method)} value={formatCurrency(p.amount)} />)
        )}
      </div>

      <div className="rounded-md border p-3">
        <p className="mb-1 text-xs font-medium text-muted-foreground">Kassa nağdı</p>
        <Row label="İlkin nağd" value={formatCurrency(s.openingCash)} />
        <Row label="Müştəri borc ödənişləri (nağd)" value={formatCurrency(report.customerPaymentsCash)} />
        <Row label="Mədaxil" value={formatCurrency(report.cashIn)} />
        <Row label="Məxaric" value={formatCurrency(report.cashOut)} />
        <Row label="Nağd qaytarma" value={formatCurrency(report.cashRefunds)} />
        {report.cardRefunds > 0 && <Row label="Karta qaytarma" value={formatCurrency(report.cardRefunds)} />}
        <Row label="Gözlənilən nağd" value={formatCurrency(report.expectedCash)} strong />
        {s.actualCash != null && (
          <>
            <Row label="Faktiki nağd" value={formatCurrency(s.actualCash)} />
            <div
              className={cn(
                "flex justify-between py-1 text-sm font-semibold",
                (s.cashDifference ?? 0) < 0 ? "text-red-600" : (s.cashDifference ?? 0) > 0 ? "text-amber-600" : "text-emerald-600",
              )}
            >
              <span>Fərq</span>
              <span>{formatCurrency(s.cashDifference ?? 0)}</span>
            </div>
          </>
        )}
      </div>

      {report.cashMovements.length > 0 && (
        <div className="rounded-md border p-3">
          <p className="mb-1 text-xs font-medium text-muted-foreground">Mədaxil / məxaric</p>
          {report.cashMovements.map((m) => (
            <div key={m.id} className="flex justify-between py-1 text-sm">
              <span>
                {m.type === StoreCashMovementType.In ? "Mədaxil" : "Məxaric"} — {m.reason}
                <span className="block text-xs text-muted-foreground">
                  {new Date(m.createdAtUtc).toLocaleTimeString("az-AZ")} {m.createdByUserName ?? ""}
                </span>
              </span>
              <span className={m.type === StoreCashMovementType.In ? "text-emerald-600" : "text-red-600"}>
                {m.type === StoreCashMovementType.In ? "+" : "−"}
                {formatCurrency(m.amount)}
              </span>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}
