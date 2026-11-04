"use client";

import { useCallback, useEffect, useState } from "react";
import { Printer } from "lucide-react";
import { toast } from "sonner";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Dialog, DialogContent, DialogFooter, DialogHeader, DialogTitle } from "@/components/ui/dialog";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { useStoreApproval } from "@/components/store/approval";
import { formatCurrency } from "@/lib/format-currency";
import { toDateInputValue } from "@/lib/report-period";
import { cn } from "@/lib/utils";
import { printStoreSale } from "@/lib/store/store-receipt";
import {
  PAYMENT_LABELS,
  StoreSaleStatus,
  cancelStoreSale,
  getStoreSale,
  searchStoreSales,
  type StoreSale,
  type StoreSaleListItem,
  FISCAL_LABELS,
  retryStoreFiscal,
} from "@/lib/store/store-service";

const PAGE_SIZE = 25;

export default function StoreSalesPage() {
  const approval = useStoreApproval();
  const today = toDateInputValue(new Date());

  const [from, setFrom] = useState(today);
  const [to, setTo] = useState(today);
  const [documentNo, setDocumentNo] = useState("");
  const [status, setStatus] = useState<number | "">("");
  const [page, setPage] = useState(1);
  const [items, setItems] = useState<StoreSaleListItem[]>([]);
  const [total, setTotal] = useState(0);
  const [loading, setLoading] = useState(true);

  const [sale, setSale] = useState<StoreSale | null>(null);
  const [cancelReason, setCancelReason] = useState("");
  const [cancelling, setCancelling] = useState(false);

  const load = useCallback(async () => {
    setLoading(true);
    try {
      const res = await searchStoreSales({
        from,
        to,
        documentNo: documentNo.trim() || undefined,
        status: status === "" ? null : status,
        page,
        pageSize: PAGE_SIZE,
      });
      setItems(res.items);
      setTotal(res.total);
    } catch (e) {
      toast.error(e instanceof Error ? e.message : "Satışlar yüklənmədi.");
    } finally {
      setLoading(false);
    }
  }, [from, to, documentNo, status, page]);

  useEffect(() => {
    const t = setTimeout(() => void load(), 250);
    return () => clearTimeout(t);
  }, [load]);

  const open = async (id: number) => {
    try {
      setSale(await getStoreSale(id));
      setCancelReason("");
    } catch (e) {
      toast.error(e instanceof Error ? e.message : "Satış yüklənmədi.");
    }
  };

  const cancel = async () => {
    if (!sale) return;
    if (!cancelReason.trim()) {
      toast.error("Ləğv səbəbini yazın.");
      return;
    }
    setCancelling(true);
    try {
      // A cashier without the cancel permission is asked for a manager's name and password.
      setSale(await approval.run((appr) => cancelStoreSale(sale.id, cancelReason.trim(), appr ?? null)));
      toast.success("Satış ləğv edildi, mallar stoka qaytarıldı");
      void load();
    } catch (e) {
      toast.error(e instanceof Error ? e.message : "Ləğv edilmədi.");
    } finally {
      setCancelling(false);
    }
  };

  const pages = Math.max(1, Math.ceil(total / PAGE_SIZE));
  const dayTotal = items.filter((i) => i.status === StoreSaleStatus.Completed).reduce((s, i) => s + i.total, 0);

  return (
    <div className="mx-auto max-w-5xl space-y-4 p-4">
      {approval.dialog}
      <h1 className="text-2xl font-bold">Satışlar</h1>

      <div className="flex flex-wrap items-end gap-3 rounded-lg border bg-card p-3">
        <div className="space-y-1">
          <Label htmlFor="s-from">Başlanğıc</Label>
          <Input id="s-from" type="date" value={from} max={to} onChange={(e) => { setFrom(e.target.value); setPage(1); }} />
        </div>
        <div className="space-y-1">
          <Label htmlFor="s-to">Son</Label>
          <Input id="s-to" type="date" value={to} min={from} onChange={(e) => { setTo(e.target.value); setPage(1); }} />
        </div>
        <div className="space-y-1">
          <Label htmlFor="s-no">Çek №</Label>
          <Input id="s-no" value={documentNo} onChange={(e) => { setDocumentNo(e.target.value); setPage(1); }} placeholder="S-0000123" />
        </div>
        <div className="space-y-1">
          <Label htmlFor="s-status">Status</Label>
          <select
            id="s-status"
            className="h-9 rounded-md border border-input bg-background px-3 text-sm"
            value={status}
            onChange={(e) => { setStatus(e.target.value ? Number(e.target.value) : ""); setPage(1); }}
          >
            <option value="">Hamısı</option>
            <option value={StoreSaleStatus.Completed}>Tamamlanıb</option>
            <option value={StoreSaleStatus.Cancelled}>Ləğv edilib</option>
          </select>
        </div>
      </div>

      <div className="overflow-x-auto rounded-lg border bg-card">
        <table className="w-full text-sm">
          <thead>
            <tr className="bg-muted/40 text-left text-xs text-muted-foreground">
              <th className="px-3 py-2 font-medium">Çek №</th>
              <th className="px-3 py-2 font-medium">Tarix</th>
              <th className="px-3 py-2 font-medium">Kassa</th>
              <th className="px-3 py-2 font-medium">Kassir</th>
              <th className="px-3 py-2 text-right font-medium">Sətir</th>
              <th className="px-3 py-2 text-right font-medium">Məbləğ</th>
              <th className="px-3 py-2 font-medium">Status</th>
            </tr>
          </thead>
          <tbody>
            {loading ? (
              <tr>
                <td colSpan={7} className="px-3 py-8 text-center text-muted-foreground">
                  Yüklənir…
                </td>
              </tr>
            ) : items.length === 0 ? (
              <tr>
                <td colSpan={7} className="px-3 py-8 text-center text-muted-foreground">
                  Satış tapılmadı.
                </td>
              </tr>
            ) : (
              items.map((s) => (
                <tr key={s.id} className="cursor-pointer border-t hover:bg-muted/40" onClick={() => void open(s.id)}>
                  <td className="px-3 py-2 font-medium">{s.documentNo}</td>
                  <td className="px-3 py-2">{new Date(s.createdAtUtc).toLocaleString("az-AZ")}</td>
                  <td className="px-3 py-2">{s.registerName ?? "—"}</td>
                  <td className="px-3 py-2">{s.cashierName ?? "—"}</td>
                  <td className="px-3 py-2 text-right">{s.lineCount}</td>
                  <td className={cn("px-3 py-2 text-right font-medium", s.status === StoreSaleStatus.Cancelled && "text-muted-foreground line-through")}>
                    {formatCurrency(s.total)}
                  </td>
                  <td className="px-3 py-2">
                    {s.status === StoreSaleStatus.Cancelled ? <Badge variant="destructive">Ləğv</Badge> : <Badge variant="secondary">Tamamlanıb</Badge>}
                  </td>
                </tr>
              ))
            )}
          </tbody>
        </table>
      </div>

      <div className="flex items-center justify-between text-sm text-muted-foreground">
        <span>
          Cəmi: {total} — bu səhifədə tamamlanan: <span className="font-semibold text-foreground">{formatCurrency(dayTotal)}</span>
        </span>
        <div className="flex items-center gap-2">
          <Button size="sm" variant="outline" disabled={page <= 1} onClick={() => setPage((p) => p - 1)}>
            Əvvəlki
          </Button>
          <span>
            {page} / {pages}
          </span>
          <Button size="sm" variant="outline" disabled={page >= pages} onClick={() => setPage((p) => p + 1)}>
            Növbəti
          </Button>
        </div>
      </div>

      <Dialog open={sale != null} onOpenChange={(o) => !o && setSale(null)}>
        <DialogContent className="max-h-[90vh] overflow-y-auto sm:max-w-md">
          <DialogHeader>
            <DialogTitle>
              {sale?.documentNo} {sale?.status === StoreSaleStatus.Cancelled && <Badge variant="destructive">Ləğv</Badge>}
            </DialogTitle>
          </DialogHeader>
          {sale && (
            <div className="space-y-3 text-sm">
              <p className="text-xs text-muted-foreground">
                {new Date(sale.createdAtUtc).toLocaleString("az-AZ")} — {sale.registerName} — {sale.cashierName ?? "kassir"}
              </p>
              <div className="rounded-md border">
                {sale.lines.map((l, i) => (
                  <div key={i} className="flex justify-between border-b px-3 py-2 last:border-b-0">
                    <span>
                      {l.productName}
                      <span className="block text-xs text-muted-foreground">
                        {l.quantity} × {formatCurrency(l.unitPrice)}
                        {l.discountAmount > 0 && ` − ${formatCurrency(l.discountAmount)}`}
                        {l.unitPrice !== l.listPrice && ` (qiymət dəyişib: ${formatCurrency(l.listPrice)} → ${formatCurrency(l.unitPrice)}${l.priceChangeReason ? `, ${l.priceChangeReason}` : ""})`}
                      </span>
                      {l.serialNumbers.length > 0 && (
                        <span className="block text-xs text-muted-foreground">IMEI/serial: {l.serialNumbers.join(", ")}</span>
                      )}
                    </span>
                    <span>{formatCurrency(l.lineTotal)}</span>
                  </div>
                ))}
              </div>
              <div className="space-y-1">
                {sale.discountTotal > 0 && (
                  <div className="flex justify-between">
                    <span>Endirim</span>
                    <span>−{formatCurrency(sale.discountTotal)}</span>
                  </div>
                )}
                <div className="flex justify-between font-semibold">
                  <span>Yekun</span>
                  <span>{formatCurrency(sale.total)}</span>
                </div>
                {sale.payments.map((p, i) => (
                  <div key={i} className="flex justify-between text-muted-foreground">
                    <span>{PAYMENT_LABELS[p.method] ?? p.method}</span>
                    <span>{formatCurrency(p.amount)}</span>
                  </div>
                ))}
                {sale.changeAmount > 0 && (
                  <div className="flex justify-between text-muted-foreground">
                    <span>Qaytarılan pul</span>
                    <span>{formatCurrency(sale.changeAmount)}</span>
                  </div>
                )}
              </div>
              {sale.fiscalStatus !== 0 && (
                <div className="flex items-center justify-between rounded-md border p-2">
                  <span>
                    Fiskal: {FISCAL_LABELS[sale.fiscalStatus]}
                    {sale.fiscalReceiptNo ? ` (${sale.fiscalReceiptNo})` : ""}
                    {sale.fiscalError ? <span className="block text-xs text-red-600">{sale.fiscalError}</span> : null}
                  </span>
                  {sale.fiscalStatus === 3 && sale.status === StoreSaleStatus.Completed && (
                    <Button
                      size="sm"
                      variant="outline"
                      onClick={() =>
                        void retryStoreFiscal(sale.id)
                          .then(setSale)
                          .catch((e) => toast.error(e instanceof Error ? e.message : "Göndərilmədi."))
                      }
                    >
                      Yenidən göndər
                    </Button>
                  )}
                </div>
              )}
              {sale.status === StoreSaleStatus.Cancelled && (
                <p className="rounded-md bg-red-50 p-2 text-red-700">
                  Ləğv: {sale.cancelReason} ({sale.cancelledAtUtc ? new Date(sale.cancelledAtUtc).toLocaleString("az-AZ") : ""})
                </p>
              )}
              {sale.status === StoreSaleStatus.Completed && (
                <div className="space-y-2 border-t pt-3">
                  <Label htmlFor="cancel-reason">Satışı ləğv et</Label>
                  <Input id="cancel-reason" placeholder="Səbəb" value={cancelReason} onChange={(e) => setCancelReason(e.target.value)} />
                  <p className="text-xs text-muted-foreground">Yalnız növbəsi hələ açıq olan satış ləğv edilə bilər. Mallar stoka qayıdır.</p>
                  <Button variant="destructive" size="sm" disabled={cancelling} onClick={() => void cancel()}>
                    {cancelling ? "Ləğv edilir…" : "Ləğv et"}
                  </Button>
                </div>
              )}
            </div>
          )}
          <DialogFooter>
            <Button
              variant="outline"
              onClick={() => {
                if (sale && !printStoreSale(sale, null, { duplicate: true })) toast.error("Çap pəncərəsi açılmadı — popup bloklamasını söndürün.");
              }}
            >
              <Printer className="mr-1 h-4 w-4" />
              Çap et
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}
