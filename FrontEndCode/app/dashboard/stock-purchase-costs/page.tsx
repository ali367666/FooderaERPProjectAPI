"use client";

import { Fragment, useCallback, useEffect, useMemo, useState } from "react";
import { ChevronDown, ChevronRight } from "lucide-react";
import { toast } from "sonner";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { formatCurrency } from "@/lib/format-currency";
import { toDateInputValue } from "@/lib/report-period";
import { cn } from "@/lib/utils";
import { getCounterparties, type Counterparty } from "@/lib/services/counterparty-service";
import {
  getStockPurchaseDebtReport,
  type StockPurchaseDebtReport,
  type StockPurchaseDebtRow,
} from "@/lib/services/stock-purchase-service";

function formatDate(value: string): string {
  const d = new Date(value);
  return Number.isNaN(d.getTime()) ? value : d.toLocaleDateString("az-AZ");
}

function formatDateTime(value: string): string {
  const d = new Date(value);
  return Number.isNaN(d.getTime()) ? value : d.toLocaleString("az-AZ");
}

function Stat({ label, value, tone }: { label: string; value: number; tone?: "bad" | "good" }) {
  return (
    <div className="rounded-lg border bg-card p-4">
      <p className="text-xs text-muted-foreground">{label}</p>
      <p
        className={cn(
          "mt-1 text-xl font-semibold",
          tone === "bad" && value > 0 && "text-red-600",
          tone === "good" && "text-emerald-600",
        )}
      >
        {formatCurrency(value)}
      </p>
    </div>
  );
}

function PurchaseDetails({ row }: { row: StockPurchaseDebtRow }) {
  return (
    <div className="grid gap-4 p-4 md:grid-cols-2">
      <div>
        <h3 className="mb-2 text-sm font-semibold">Ödənişlər</h3>
        {row.payments.length === 0 ? (
          <p className="text-sm text-muted-foreground">Hələ ödəniş yoxdur.</p>
        ) : (
          <table className="w-full text-sm">
            <thead>
              <tr className="text-left text-xs text-muted-foreground">
                <th className="py-1 font-medium">Tarix</th>
                <th className="py-1 font-medium">Kim</th>
                <th className="py-1 text-right font-medium">Məbləğ</th>
                <th className="py-1 text-right font-medium">Qalan borc</th>
              </tr>
            </thead>
            <tbody>
              {row.payments.map((p) => (
                <tr key={p.id} className="border-t">
                  <td className="py-1.5">{formatDateTime(p.paidAtUtc)}</td>
                  <td className="py-1.5">
                    {p.paidByUserName ?? "—"}
                    {p.note && <span className="block text-xs text-muted-foreground">{p.note}</span>}
                  </td>
                  <td className="py-1.5 text-right font-medium">{formatCurrency(p.amount)}</td>
                  <td className="py-1.5 text-right">{formatCurrency(p.remainingAfterAzn)}</td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </div>
      <div>
        <h3 className="mb-2 text-sm font-semibold">Təchizatçıya qaytarmalar</h3>
        {row.returns.length === 0 ? (
          <p className="text-sm text-muted-foreground">Qaytarma yoxdur.</p>
        ) : (
          <div className="space-y-3">
            {row.returns.map((r) => (
              <div key={r.id} className="rounded border p-2 text-sm">
                <div className="flex justify-between text-xs text-muted-foreground">
                  <span>
                    {formatDateTime(r.returnedAtUtc)} — {r.returnedByUserName ?? "—"}
                  </span>
                  <span className="font-medium text-foreground">{formatCurrency(r.totalAzn)}</span>
                </div>
                {r.lines.map((l, i) => (
                  <div key={i} className="mt-1 flex justify-between">
                    <span>
                      {l.stockItemName} × {l.quantity}
                    </span>
                    <span>{formatCurrency(l.totalAzn)}</span>
                  </div>
                ))}
                {r.note && <p className="mt-1 text-xs text-muted-foreground">{r.note}</p>}
              </div>
            ))}
          </div>
        )}
      </div>
    </div>
  );
}

export default function StockPurchaseCostsPage() {
  const today = toDateInputValue(new Date());
  const monthStart = toDateInputValue(new Date(new Date().getFullYear(), new Date().getMonth(), 1));

  const [from, setFrom] = useState(monthStart);
  const [to, setTo] = useState(today);
  const [counterpartyId, setCounterpartyId] = useState<number | "">("");
  const [onlyOpen, setOnlyOpen] = useState(false);
  const [suppliers, setSuppliers] = useState<Counterparty[]>([]);
  const [report, setReport] = useState<StockPurchaseDebtReport | null>(null);
  const [loading, setLoading] = useState(true);
  const [expanded, setExpanded] = useState<Set<number>>(new Set());

  useEffect(() => {
    getCounterparties()
      .then((list) => setSuppliers(list.filter((c) => !c.isBank)))
      .catch(() => setSuppliers([]));
  }, []);

  const load = useCallback(async () => {
    setLoading(true);
    try {
      setReport(
        await getStockPurchaseDebtReport({
          from,
          to,
          counterpartyId: counterpartyId === "" ? null : counterpartyId,
          onlyOpen,
        }),
      );
    } catch (e) {
      toast.error(e instanceof Error ? e.message : "Hesabat yüklənmədi.");
      setReport(null);
    } finally {
      setLoading(false);
    }
  }, [from, to, counterpartyId, onlyOpen]);

  useEffect(() => {
    void load();
  }, [load]);

  const toggle = (id: number) =>
    setExpanded((prev) => {
      const next = new Set(prev);
      if (next.has(id)) next.delete(id);
      else next.add(id);
      return next;
    });

  const purchases = useMemo(() => report?.purchases ?? [], [report]);

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-3xl font-bold text-foreground">Mal alışı xərcləri</h1>
        <p className="mt-1 text-muted-foreground">
          Təsdiqlənmiş mal qəbulları: nə qədər xərc olub, kim nə vaxt nə qədər ödəyib, nə qədər qaytarılıb və qalan borc.
        </p>
      </div>

      <div className="flex flex-wrap items-end gap-3 rounded-lg border bg-card p-4">
        <div className="space-y-1">
          <Label htmlFor="cost-from">Başlanğıc</Label>
          <Input id="cost-from" type="date" value={from} max={to} onChange={(e) => setFrom(e.target.value)} />
        </div>
        <div className="space-y-1">
          <Label htmlFor="cost-to">Son</Label>
          <Input id="cost-to" type="date" value={to} min={from} onChange={(e) => setTo(e.target.value)} />
        </div>
        <div className="space-y-1">
          <Label htmlFor="cost-supplier">Təchizatçı</Label>
          <select
            id="cost-supplier"
            className="h-9 rounded-md border border-input bg-background px-3 text-sm"
            value={counterpartyId}
            onChange={(e) => setCounterpartyId(e.target.value ? Number(e.target.value) : "")}
          >
            <option value="">Hamısı</option>
            {suppliers.map((s) => (
              <option key={s.id} value={s.id}>
                {s.name}
              </option>
            ))}
          </select>
        </div>
        <label className="flex items-center gap-2 pb-2 text-sm">
          <input type="checkbox" checked={onlyOpen} onChange={(e) => setOnlyOpen(e.target.checked)} />
          Yalnız borcu olanlar
        </label>
      </div>

      {loading ? (
        <p className="text-sm text-muted-foreground">Yüklənir…</p>
      ) : !report ? (
        <p className="text-sm text-muted-foreground">Hesabat yüklənmədi.</p>
      ) : (
        <>
          <div className="grid grid-cols-2 gap-3 lg:grid-cols-5">
            <Stat label="Ümumi alış" value={report.totalAzn} />
            <Stat label="Qaytarılan" value={report.returnedAzn} />
            <Stat label="Xalis xərc" value={report.netTotalAzn} />
            <Stat label="Ödənilən" value={report.paidAzn} tone="good" />
            <Stat label="Qalan borc" value={report.remainingAzn} tone="bad" />
          </div>
          {report.overpaidAzn > 0 && (
            <p className="rounded-md border border-amber-300 bg-amber-50 p-3 text-sm text-amber-800">
              Təchizatçılar sizə {formatCurrency(report.overpaidAzn)} geri borcludur (qaytarmadan sonra artıq ödəniş).
            </p>
          )}

          <div className="rounded-lg border bg-card p-4">
            <h2 className="mb-3 font-semibold">Təchizatçılar üzrə</h2>
            {report.suppliers.length === 0 ? (
              <p className="text-sm text-muted-foreground">Qeyd yoxdur.</p>
            ) : (
              <div className="overflow-x-auto">
                <table className="w-full text-sm">
                  <thead>
                    <tr className="text-left text-xs text-muted-foreground">
                      <th className="py-1 font-medium">Təchizatçı</th>
                      <th className="py-1 text-right font-medium">Alış sayı</th>
                      <th className="py-1 text-right font-medium">Xalis xərc</th>
                      <th className="py-1 text-right font-medium">Ödənilən</th>
                      <th className="py-1 text-right font-medium">Qalan borc</th>
                    </tr>
                  </thead>
                  <tbody>
                    {report.suppliers.map((s) => (
                      <tr key={s.counterpartyId} className="border-t">
                        <td className="py-1.5">{s.counterpartyName}</td>
                        <td className="py-1.5 text-right">{s.purchaseCount}</td>
                        <td className="py-1.5 text-right">{formatCurrency(s.netTotalAzn)}</td>
                        <td className="py-1.5 text-right">{formatCurrency(s.paidAzn)}</td>
                        <td className={cn("py-1.5 text-right font-medium", s.remainingAzn > 0 && "text-red-600")}>
                          {formatCurrency(s.remainingAzn)}
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}
          </div>

          <div className="rounded-lg border bg-card p-4">
            <h2 className="mb-3 font-semibold">Alışlar</h2>
            {purchases.length === 0 ? (
              <p className="text-sm text-muted-foreground">Bu dövr üçün alış yoxdur.</p>
            ) : (
              <div className="overflow-x-auto">
                <table className="w-full text-sm">
                  <thead>
                    <tr className="text-left text-xs text-muted-foreground">
                      <th className="w-8 py-1" />
                      <th className="py-1 font-medium">Tarix</th>
                      <th className="py-1 font-medium">Sənəd</th>
                      <th className="py-1 font-medium">Təchizatçı</th>
                      <th className="py-1 font-medium">Kim yazıb</th>
                      <th className="py-1 text-right font-medium">Ümumi</th>
                      <th className="py-1 text-right font-medium">Qaytarılan</th>
                      <th className="py-1 text-right font-medium">Ödənilən</th>
                      <th className="py-1 text-right font-medium">Qalan borc</th>
                    </tr>
                  </thead>
                  <tbody>
                    {purchases.map((p) => {
                      const open = expanded.has(p.purchaseId);
                      return (
                        <Fragment key={p.purchaseId}>
                          <tr className="cursor-pointer border-t hover:bg-muted/40" onClick={() => toggle(p.purchaseId)}>
                            <td className="py-1.5">
                              {open ? <ChevronDown className="h-4 w-4" /> : <ChevronRight className="h-4 w-4" />}
                            </td>
                            <td className="py-1.5">{formatDate(p.purchaseDate)}</td>
                            <td className="py-1.5">
                              {p.documentNo}
                              {p.supplierInvoiceNo && (
                                <span className="block text-xs text-muted-foreground">Qaimə: {p.supplierInvoiceNo}</span>
                              )}
                            </td>
                            <td className="py-1.5">{p.counterpartyName}</td>
                            <td className="py-1.5">{p.createdByUserName ?? "—"}</td>
                            <td className="py-1.5 text-right">{formatCurrency(p.totalAzn)}</td>
                            <td className="py-1.5 text-right">{formatCurrency(p.returnedAzn)}</td>
                            <td className="py-1.5 text-right">{formatCurrency(p.paidAzn)}</td>
                            <td className={cn("py-1.5 text-right font-medium", p.remainingAzn > 0 && "text-red-600")}>
                              {formatCurrency(p.remainingAzn)}
                            </td>
                          </tr>
                          {open && (
                            <tr className="border-t bg-muted/20">
                              <td colSpan={9}>
                                <PurchaseDetails row={p} />
                              </td>
                            </tr>
                          )}
                        </Fragment>
                      );
                    })}
                  </tbody>
                </table>
              </div>
            )}
          </div>
        </>
      )}
    </div>
  );
}
