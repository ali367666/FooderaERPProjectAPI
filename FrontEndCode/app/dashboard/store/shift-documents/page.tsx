"use client";

import { useCallback, useEffect, useState } from "react";
import { toast } from "sonner";
import { ShiftReportView } from "@/components/store/shift-report-view";
import { TimeRangeInputs, useTimeRangeEnabled } from "@/components/store/time-range-inputs";
import { Badge } from "@/components/ui/badge";
import { Dialog, DialogContent, DialogHeader, DialogTitle } from "@/components/ui/dialog";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { formatCurrency } from "@/lib/format-currency";
import { toDateInputValue, withTime } from "@/lib/report-period";
import { cn } from "@/lib/utils";
import {
  PAYMENT_LABELS,
  StoreSaleStatus,
  StoreShiftStatus,
  getStoreRegisters,
  getStoreShiftDocument,
  getStoreShifts,
  type StoreCashRegister,
  type StoreShift,
  type StoreShiftDocument,
} from "@/lib/store/store-service";

const APPROVAL_LABELS: Record<string, string> = {
  CartLineRemoved: "Səbətdən məhsul silindi",
  CartCleared: "Səbət təmizləndi",
  CashIn: "Kassaya pul daxil edildi",
  CashOut: "Kassadan pul çıxarıldı",
  Discount: "Endirim",
  PriceChange: "Qiymət dəyişdirildi",
  SaleCancelled: "Satış ləğv edildi",
  Return: "Qaytarma",
};

const dateOf = (iso: string) => new Date(iso).toLocaleDateString("az-AZ");
const timeOf = (iso: string) => new Date(iso).toLocaleTimeString("az-AZ", { hour: "2-digit", minute: "2-digit" });

function Section({ title, count, children }: { title: string; count?: number; children: React.ReactNode }) {
  return (
    <section className="space-y-2">
      <h3 className="text-sm font-semibold">
        {title}
        {count != null && <span className="ml-1.5 font-normal text-muted-foreground">({count})</span>}
      </h3>
      {children}
    </section>
  );
}

const th = "px-3 py-2 font-medium";

/** One shift as a document: name and code on top, then the till, cashier, date and time; opened, it shows everything that happened in the shift. */
function ShiftDocumentDialog({ shiftId, onClose }: { shiftId: number | null; onClose: () => void }) {
  const [doc, setDoc] = useState<StoreShiftDocument | null>(null);
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    if (shiftId == null) {
      setDoc(null);
      return;
    }
    setLoading(true);
    getStoreShiftDocument(shiftId)
      .then(setDoc)
      .catch((e) => {
        toast.error(e instanceof Error ? e.message : "Növbə sənədi yüklənmədi.");
        onClose();
      })
      .finally(() => setLoading(false));
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [shiftId]);

  const shift = doc?.report.shift;

  return (
    <Dialog open={shiftId != null} onOpenChange={(o) => !o && onClose()}>
      <DialogContent className="max-h-[92vh] overflow-y-auto sm:max-w-5xl">
        <DialogHeader>
          <DialogTitle className="flex items-center justify-between gap-3 pr-6">
            <span>{doc?.name ?? "Növbə sənədi"}</span>
            <span className="font-mono text-base text-muted-foreground">{doc?.code ?? ""}</span>
          </DialogTitle>
        </DialogHeader>

        {loading || !doc || !shift ? (
          <p className="py-10 text-center text-sm text-muted-foreground">Yüklənir…</p>
        ) : (
          <div className="space-y-6">
            <div className="grid grid-cols-2 gap-3 rounded-lg border bg-muted/30 p-3 text-sm sm:grid-cols-4">
              <div>
                <p className="text-xs text-muted-foreground">Kassa</p>
                <p className="font-medium">{shift.registerName}</p>
              </div>
              <div>
                <p className="text-xs text-muted-foreground">Kassir</p>
                <p className="font-medium">{shift.cashierName ?? "—"}</p>
              </div>
              <div>
                <p className="text-xs text-muted-foreground">Tarix</p>
                <p className="font-medium">{dateOf(shift.openedAtUtc)}</p>
              </div>
              <div>
                <p className="text-xs text-muted-foreground">Saat</p>
                <p className="font-medium">
                  {timeOf(shift.openedAtUtc)}
                  {shift.closedAtUtc ? ` – ${timeOf(shift.closedAtUtc)}` : " – açıqdır"}
                </p>
              </div>
            </div>

            <div className="grid grid-cols-2 gap-3 sm:grid-cols-4">
              <div className="rounded-lg border p-3">
                <p className="text-xs text-muted-foreground">Satış sayı</p>
                <p className="text-lg font-semibold">{doc.report.salesCount}</p>
              </div>
              <div className="rounded-lg border p-3">
                <p className="text-xs text-muted-foreground">Satılan məhsul (növ / ədəd)</p>
                <p className="text-lg font-semibold">
                  {doc.productKinds} / {doc.soldQuantity}
                </p>
              </div>
              <div className="rounded-lg border p-3">
                <p className="text-xs text-muted-foreground">Qaytarılan ədəd</p>
                <p className="text-lg font-semibold">{doc.returnedQuantity}</p>
              </div>
              <div className="rounded-lg border p-3">
                <p className="text-xs text-muted-foreground">Ləğv edilən satış</p>
                <p className="text-lg font-semibold">{doc.report.cancelledCount}</p>
              </div>
            </div>

            <Section title="Satılan məhsullar" count={doc.products.length}>
              <div className="overflow-x-auto rounded-lg border">
                <table className="w-full text-sm">
                  <thead>
                    <tr className="bg-muted/40 text-left text-xs text-muted-foreground">
                      <th className={th}>Məhsul</th>
                      <th className={th}>Barkod</th>
                      <th className={cn(th, "text-right")}>Satılıb</th>
                      <th className={cn(th, "text-right")}>Məbləğ</th>
                      <th className={cn(th, "text-right")}>Qaytarılıb</th>
                    </tr>
                  </thead>
                  <tbody>
                    {doc.products.length === 0 ? (
                      <tr>
                        <td colSpan={5} className="px-3 py-6 text-center text-muted-foreground">
                          Bu növbədə məhsul satılmayıb.
                        </td>
                      </tr>
                    ) : (
                      doc.products.map((p) => (
                        <tr key={p.productId} className="border-t">
                          <td className="px-3 py-2">{p.productName}</td>
                          <td className="px-3 py-2 text-muted-foreground">{p.barcode ?? "—"}</td>
                          <td className="px-3 py-2 text-right">{p.soldQuantity}</td>
                          <td className="px-3 py-2 text-right">{formatCurrency(p.soldAmount)}</td>
                          <td className="px-3 py-2 text-right">
                            {p.returnedQuantity > 0 ? `${p.returnedQuantity} (${formatCurrency(p.returnedAmount)})` : "—"}
                          </td>
                        </tr>
                      ))
                    )}
                  </tbody>
                </table>
              </div>
            </Section>

            <Section title="Satışlar" count={doc.sales.length}>
              <div className="space-y-2">
                {doc.sales.length === 0 && <p className="text-sm text-muted-foreground">Satış yoxdur.</p>}
                {doc.sales.map((s) => {
                  const cancelled = s.status === StoreSaleStatus.Cancelled;
                  return (
                    <details key={s.id} className={cn("rounded-lg border", cancelled && "border-red-300 bg-red-50/40 dark:bg-red-950/10")}>
                      <summary className="flex cursor-pointer flex-wrap items-center justify-between gap-2 px-3 py-2 text-sm">
                        <span className="flex items-center gap-2">
                          <span className="font-mono">{s.documentNo}</span>
                          <span className="text-muted-foreground">{timeOf(s.createdAtUtc)}</span>
                          {s.customerName && <span className="text-muted-foreground">· {s.customerName}</span>}
                          {cancelled && <Badge variant="destructive">Ləğv edilib</Badge>}
                          {s.isReceiptless && <Badge variant="secondary">Çeksiz</Badge>}
                        </span>
                        <span className="flex items-center gap-3">
                          <span className="text-xs text-muted-foreground">
                            {s.payments.map((p) => `${PAYMENT_LABELS[p.method] ?? p.method}: ${formatCurrency(p.amount)}`).join(" · ")}
                          </span>
                          <b className={cn(cancelled && "line-through")}>{formatCurrency(s.total)}</b>
                        </span>
                      </summary>
                      <div className="border-t px-3 py-2">
                        <table className="w-full text-xs">
                          <tbody>
                            {s.lines.map((l) => (
                              <tr key={l.lineId}>
                                <td className="py-0.5">{l.productName}</td>
                                <td className="py-0.5 text-right">
                                  {l.quantity} × {formatCurrency(l.unitPrice)}
                                  {l.discountAmount > 0 && <span className="text-amber-600"> (−{formatCurrency(l.discountAmount)})</span>}
                                </td>
                                <td className="py-0.5 text-right font-medium">{formatCurrency(l.lineTotal)}</td>
                              </tr>
                            ))}
                          </tbody>
                        </table>
                        {cancelled && (
                          <p className="mt-2 text-xs text-red-600">
                            Ləğv: {s.cancelledAtUtc ? `${dateOf(s.cancelledAtUtc)} ${timeOf(s.cancelledAtUtc)}` : ""}
                            {s.cancelledByName ? ` · ${s.cancelledByName}` : ""}
                            {s.cancelReason ? ` · ${s.cancelReason}` : ""}
                          </p>
                        )}
                      </div>
                    </details>
                  );
                })}
              </div>
            </Section>

            <Section title="Qaytarmalar" count={doc.returns.length}>
              {doc.returns.length === 0 ? (
                <p className="text-sm text-muted-foreground">Bu növbədə qaytarma olmayıb.</p>
              ) : (
                <div className="space-y-2">
                  {doc.returns.map((r) => (
                    <div key={r.id} className="rounded-lg border px-3 py-2 text-sm">
                      <div className="flex flex-wrap items-center justify-between gap-2">
                        <span>
                          <span className="font-mono">{r.documentNo}</span>
                          <span className="ml-2 text-muted-foreground">
                            {timeOf(r.createdAtUtc)} · satış {r.saleDocumentNo}
                          </span>
                        </span>
                        <span>
                          <b>{formatCurrency(r.totalAmount)}</b>
                          <span className="ml-2 text-xs text-muted-foreground">
                            geri ödəniş: {formatCurrency(r.refundAmount)} ({PAYMENT_LABELS[r.refundMethod] ?? r.refundMethod})
                          </span>
                        </span>
                      </div>
                      <p className="text-xs text-muted-foreground">
                        {r.lines.map((l) => `${l.productName} × ${l.quantity}`).join(", ")} — {r.reason}
                      </p>
                    </div>
                  ))}
                </div>
              )}
            </Section>

            <Section title="Silinmə və menecer təsdiqləri" count={doc.approvals.length}>
              {doc.approvals.length === 0 ? (
                <p className="text-sm text-muted-foreground">Bu növbədə menecer təsdiqi tələb olunan əməliyyat olmayıb.</p>
              ) : (
                <div className="overflow-x-auto rounded-lg border">
                  <table className="w-full text-sm">
                    <thead>
                      <tr className="bg-muted/40 text-left text-xs text-muted-foreground">
                        <th className={th}>Saat</th>
                        <th className={th}>Əməliyyat</th>
                        <th className={th}>Sənəd / məlumat</th>
                        <th className={th}>Təsdiq edən</th>
                        <th className={th}>Səbəb</th>
                      </tr>
                    </thead>
                    <tbody>
                      {doc.approvals.map((a, i) => (
                        <tr key={i} className="border-t">
                          <td className="px-3 py-2">{timeOf(a.createdAtUtc)}</td>
                          <td className="px-3 py-2">{APPROVAL_LABELS[a.action] ?? a.action}</td>
                          <td className="px-3 py-2 text-muted-foreground">{a.documentNo ?? "—"}</td>
                          <td className="px-3 py-2">{a.approvedByName ?? "—"}</td>
                          <td className="px-3 py-2 text-muted-foreground">{a.reason ?? "—"}</td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              )}
            </Section>

            <Section title="Kassa və ödənişlər">
              <ShiftReportView report={doc.report} />
            </Section>
          </div>
        )}
      </DialogContent>
    </Dialog>
  );
}

/** Admin: the store's shifts as documents (Növbə sənədləri). */
export default function StoreShiftDocumentsPage() {
  const today = toDateInputValue(new Date());
  const weekAgo = toDateInputValue(new Date(Date.now() - 6 * 86_400_000));

  const [from, setFrom] = useState(weekAgo);
  const [to, setTo] = useState(today);
  const [fromTime, setFromTime] = useState("");
  const [toTime, setToTime] = useState("");
  const timesOn = useTimeRangeEnabled();
  const [registerId, setRegisterId] = useState<number | "">("");
  const [registers, setRegisters] = useState<StoreCashRegister[]>([]);
  const [cashierId, setCashierId] = useState<number | "">("");
  // Everyone who has had a shift in the periods looked at, so the cashier list does not shrink once one is picked.
  const [cashiers, setCashiers] = useState<Record<number, string>>({});
  const [codeInput, setCodeInput] = useState("");
  const [code, setCode] = useState("");
  const [shifts, setShifts] = useState<StoreShift[]>([]);
  const [loading, setLoading] = useState(true);
  const [openId, setOpenId] = useState<number | null>(null);

  useEffect(() => {
    getStoreRegisters().then(setRegisters).catch(() => setRegisters([]));
  }, []);

  // The code search waits for a pause in typing.
  useEffect(() => {
    const t = setTimeout(() => setCode(codeInput.trim()), 300);
    return () => clearTimeout(t);
  }, [codeInput]);

  const load = useCallback(async () => {
    setLoading(true);
    try {
      const list = await getStoreShifts({
        registerId: registerId === "" ? null : registerId,
        from: timesOn ? withTime(from, fromTime) : from,
        to: timesOn ? withTime(to, toTime) : to,
        cashierUserId: cashierId === "" ? null : cashierId,
        code: code || undefined,
      });
      setShifts(list);
      setCashiers((prev) => {
        const next = { ...prev };
        for (const s of list) if (s.cashierName) next[s.cashierUserId] = s.cashierName;
        return next;
      });
    } catch (e) {
      toast.error(e instanceof Error ? e.message : "Növbələr yüklənmədi.");
    } finally {
      setLoading(false);
    }
  }, [registerId, cashierId, code, from, to, fromTime, toTime, timesOn]);

  useEffect(() => {
    void load();
  }, [load]);

  return (
    <div className="space-y-4 p-4">
      <div>
        <h1 className="text-2xl font-bold">Növbə sənədləri</h1>
        <p className="text-sm text-muted-foreground">
          Hər növbə bir sənəddir: içinə girəndə satılan məhsullar, satışlar, ləğvlər, qaytarmalar, silinmələr və kassa hərəkətləri görünür.
          Tarix, saat aralığı, kassa və kassirə görə süzə bilərsiniz; sənəd kodu yazanda tarixdən asılı olmayaraq həmin sənəd tapılır.
        </p>
      </div>

      <div className="flex flex-wrap items-end gap-3 rounded-lg border bg-card p-3">
        <div className="space-y-1">
          <Label htmlFor="sd-from">Başlanğıc</Label>
          <Input id="sd-from" type="date" value={from} max={to} disabled={code !== ""} onChange={(e) => setFrom(e.target.value)} />
        </div>
        <div className="space-y-1">
          <Label htmlFor="sd-to">Son</Label>
          <Input id="sd-to" type="date" value={to} min={from} disabled={code !== ""} onChange={(e) => setTo(e.target.value)} />
        </div>
        <TimeRangeInputs idPrefix="sd" fromTime={fromTime} toTime={toTime} onFromTime={setFromTime} onToTime={setToTime} />
        <div className="space-y-1">
          <Label htmlFor="sd-cashier">Kassir</Label>
          <select
            id="sd-cashier"
            className="h-9 rounded-md border border-input bg-background px-3 text-sm"
            value={cashierId}
            onChange={(e) => setCashierId(e.target.value ? Number(e.target.value) : "")}
          >
            <option value="">Hamısı</option>
            {Object.entries(cashiers)
              .sort((a, b) => a[1].localeCompare(b[1]))
              .map(([id, name]) => (
                <option key={id} value={id}>
                  {name}
                </option>
              ))}
          </select>
        </div>
        <div className="space-y-1">
          <Label htmlFor="sd-code">Sənəd kodu</Label>
          <Input id="sd-code" className="w-40" placeholder="NV-000123" value={codeInput} onChange={(e) => setCodeInput(e.target.value)} />
        </div>
        <div className="space-y-1">
          <Label htmlFor="sd-reg">Kassa</Label>
          <select
            id="sd-reg"
            className="h-9 rounded-md border border-input bg-background px-3 text-sm"
            value={registerId}
            onChange={(e) => setRegisterId(e.target.value ? Number(e.target.value) : "")}
          >
            <option value="">Hamısı</option>
            {registers.map((r) => (
              <option key={r.id} value={r.id}>
                {r.name}
              </option>
            ))}
          </select>
        </div>
      </div>

      <div className="space-y-2">
        {loading ? (
          <p className="py-8 text-center text-sm text-muted-foreground">Yüklənir…</p>
        ) : shifts.length === 0 ? (
          <p className="py-8 text-center text-sm text-muted-foreground">Bu dövrdə növbə sənədi yoxdur.</p>
        ) : (
          shifts.map((s) => (
            <button
              key={s.id}
              type="button"
              onClick={() => setOpenId(s.id)}
              className="block w-full rounded-lg border bg-card px-4 py-3 text-left transition-colors hover:bg-muted/50"
            >
              <div className="flex items-center justify-between gap-3">
                <span className="font-semibold">Növbə sənədi</span>
                <span className="font-mono text-sm text-muted-foreground">{s.code}</span>
              </div>
              <div className="mt-1 flex flex-wrap items-center gap-x-6 gap-y-1 text-sm">
                <span>
                  <span className="text-muted-foreground">Kassa: </span>
                  {s.registerName}
                </span>
                <span>
                  <span className="text-muted-foreground">Kassir: </span>
                  {s.cashierName ?? "—"}
                </span>
                <span>
                  <span className="text-muted-foreground">Tarix: </span>
                  {dateOf(s.openedAtUtc)}
                </span>
                <span>
                  <span className="text-muted-foreground">Saat: </span>
                  {timeOf(s.openedAtUtc)}
                  {s.closedAtUtc ? ` – ${timeOf(s.closedAtUtc)}` : ""}
                </span>
                {s.status === StoreShiftStatus.Open ? <Badge>Açıq</Badge> : <Badge variant="secondary">Bağlı</Badge>}
              </div>
            </button>
          ))
        )}
      </div>

      <ShiftDocumentDialog shiftId={openId} onClose={() => setOpenId(null)} />
    </div>
  );
}
