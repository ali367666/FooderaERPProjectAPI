"use client";

import { useCallback, useEffect, useMemo, useState } from "react";
import { Plus, Trash2 } from "lucide-react";
import { toast } from "sonner";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from "@/components/ui/dialog";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Textarea } from "@/components/ui/textarea";
import { useStorePermissions as usePermissionSet } from "@/lib/store/use-store-permissions";
import { formatCurrency } from "@/lib/format-currency";
import { toDateInputValue } from "@/lib/report-period";
import { cn } from "@/lib/utils";
import { getWarehouses, type Warehouse } from "@/lib/services/warehouse-service";
import {
  PURCHASE_STATUS_LABELS,
  PAYMENT_LABELS,
  StorePaymentMethod,
  StorePurchaseStatus,
  cancelStorePurchase,
  confirmStorePurchase,
  getStorePurchase,
  getStoreSuppliers,
  returnStorePurchase,
  saveStorePurchase,
  searchStorePurchases,
  searchStoreProducts,
  type StoreProduct,
  type StorePurchase,
  type StorePurchaseListItem,
  type StoreSupplier,
} from "@/lib/store/store-service";

const SELECT = "h-9 w-full rounded-md border border-input bg-background px-3 text-sm";
const num = (s: string) => Number(s.replace(",", ".")) || 0;
const r2 = (n: number) => Math.round((n + Number.EPSILON) * 100) / 100;
const fmtDate = (v: string) => new Date(v).toLocaleDateString("az-AZ");
const PAGE_SIZE = 25;

type EditLine = {
  product: StoreProduct;
  quantity: string;
  unitPrice: string;
  discount: string;
  vat: string;
  serials: string;
};

const lineTotal = (l: EditLine) => r2(num(l.quantity) * num(l.unitPrice) * (1 - num(l.discount) / 100) * (1 + num(l.vat) / 100));

function statusBadge(status: number) {
  const variant = status === StorePurchaseStatus.Confirmed ? "default" : status === StorePurchaseStatus.Cancelled ? "destructive" : "secondary";
  return <Badge variant={variant}>{PURCHASE_STATUS_LABELS[status] ?? status}</Badge>;
}

export default function StorePurchasesPage() {
  const permissions = usePermissionSet();
  const canManage = permissions.has("StorePurchase.Manage");
  const canConfirm = permissions.has("StorePurchase.Confirm");
  // Returning goods to the supplier is its own permission, apart from confirming / cancelling a purchase.
  const canReturn = permissions.has("StorePurchase.Return");

  const [from, setFrom] = useState(toDateInputValue(new Date(new Date().getFullYear(), new Date().getMonth(), 1)));
  const [to, setTo] = useState(toDateInputValue(new Date()));
  const [supplierFilter, setSupplierFilter] = useState<number | "">("");
  const [statusFilter, setStatusFilter] = useState<number | "">("");
  const [q, setQ] = useState("");
  const [page, setPage] = useState(1);
  const [items, setItems] = useState<StorePurchaseListItem[]>([]);
  const [total, setTotal] = useState(0);
  const [loading, setLoading] = useState(true);
  const [suppliers, setSuppliers] = useState<StoreSupplier[]>([]);
  const [warehouses, setWarehouses] = useState<Warehouse[]>([]);

  // Editor
  const [editOpen, setEditOpen] = useState(false);
  const [editingId, setEditingId] = useState<number | null>(null);
  const [supplierId, setSupplierId] = useState<number | "">("");
  const [warehouseId, setWarehouseId] = useState<number | "">("");
  const [invoiceNo, setInvoiceNo] = useState("");
  const [purchaseDate, setPurchaseDate] = useState(toDateInputValue(new Date()));
  const [dueDate, setDueDate] = useState("");
  const [note, setNote] = useState("");
  const [lines, setLines] = useState<EditLine[]>([]);
  const [term, setTerm] = useState("");
  const [options, setOptions] = useState<StoreProduct[]>([]);
  const [saving, setSaving] = useState(false);

  // Detail
  const [detail, setDetail] = useState<StorePurchase | null>(null);
  const [confirmPay, setConfirmPay] = useState("");
  const [confirmMethod, setConfirmMethod] = useState<number>(StorePaymentMethod.Cash);
  const [cancelReason, setCancelReason] = useState("");
  const [retQty, setRetQty] = useState<Record<number, string>>({});
  const [retSerials, setRetSerials] = useState<Record<number, string>>({});
  const [retReason, setRetReason] = useState("");
  const [busy, setBusy] = useState(false);

  useEffect(() => {
    getStoreSuppliers().then(setSuppliers).catch(() => setSuppliers([]));
    getWarehouses().then(setWarehouses).catch(() => setWarehouses([]));
  }, []);

  const load = useCallback(async () => {
    setLoading(true);
    try {
      const res = await searchStorePurchases({
        supplierId: supplierFilter === "" ? null : supplierFilter,
        status: statusFilter === "" ? null : statusFilter,
        from,
        to,
        q: q.trim() || undefined,
        page,
        pageSize: PAGE_SIZE,
      });
      setItems(res.items);
      setTotal(res.total);
    } catch (e) {
      toast.error(e instanceof Error ? e.message : "Alışlar yüklənmədi.");
    } finally {
      setLoading(false);
    }
  }, [supplierFilter, statusFilter, from, to, q, page]);

  useEffect(() => {
    const t = setTimeout(() => void load(), 250);
    return () => clearTimeout(t);
  }, [load]);

  useEffect(() => {
    if (!editOpen || term.trim().length < 1) {
      setOptions([]);
      return;
    }
    const t = setTimeout(() => {
      searchStoreProducts({ q: term.trim(), pageSize: 8, isActive: true })
        .then((r) => setOptions(r.items))
        .catch(() => setOptions([]));
    }, 200);
    return () => clearTimeout(t);
  }, [editOpen, term]);

  const openNew = () => {
    setEditingId(null);
    setSupplierId(supplierFilter);
    setWarehouseId("");
    setInvoiceNo("");
    setPurchaseDate(toDateInputValue(new Date()));
    setDueDate("");
    setNote("");
    setLines([]);
    setTerm("");
    setEditOpen(true);
  };

  const openEdit = (p: StorePurchase) => {
    setEditingId(p.id);
    setSupplierId(p.supplierId);
    setWarehouseId(p.warehouseId);
    setInvoiceNo(p.supplierInvoiceNo ?? "");
    setPurchaseDate(p.purchaseDate.slice(0, 10));
    setDueDate(p.dueDate ? p.dueDate.slice(0, 10) : "");
    setNote(p.note ?? "");
    setLines(
      p.lines.map((l) => ({
        product: {
          id: l.productId,
          name: l.productName,
          unit: 1,
          tracksSerial: l.tracksSerial,
        } as StoreProduct,
        quantity: String(l.quantity),
        unitPrice: String(l.unitPrice),
        discount: String(l.discountPercent),
        vat: String(l.vatPercent),
        serials: l.serialNumbers.join("\n"),
      })),
    );
    setTerm("");
    setDetail(null);
    setEditOpen(true);
  };

  const addProduct = (p: StoreProduct) => {
    setLines((prev) =>
      prev.some((l) => l.product.id === p.id)
        ? prev
        : [
            ...prev,
            {
              product: p,
              quantity: "1",
              unitPrice: String(p.purchasePrice ?? 0),
              discount: "0",
              vat: String(p.vatPercent ?? 0),
              serials: "",
            },
          ],
    );
    setTerm("");
  };

  const updateLine = (id: number, patch: Partial<EditLine>) => setLines((prev) => prev.map((l) => (l.product.id === id ? { ...l, ...patch } : l)));

  const editTotal = useMemo(() => r2(lines.reduce((s, l) => s + lineTotal(l), 0)), [lines]);

  const submitDraft = async () => {
    if (supplierId === "" || warehouseId === "") {
      toast.error("Təchizatçı və anbarı seçin.");
      return;
    }
    if (lines.length === 0) {
      toast.error("Ən azı bir məhsul əlavə edin.");
      return;
    }
    setSaving(true);
    try {
      const saved = await saveStorePurchase(editingId, {
        supplierId,
        warehouseId,
        supplierInvoiceNo: invoiceNo.trim() || null,
        purchaseDate: purchaseDate || null,
        dueDate: dueDate || null,
        note: note.trim() || null,
        lines: lines.map((l) => ({
          productId: l.product.id,
          quantity: l.product.tracksSerial
            ? l.serials.split(/[\n,;]+/).map((x) => x.trim()).filter(Boolean).length
            : num(l.quantity),
          unitPrice: num(l.unitPrice),
          discountPercent: num(l.discount),
          vatPercent: num(l.vat),
          serialNumbers: l.product.tracksSerial ? l.serials.split(/[\n,;]+/).map((x) => x.trim()).filter(Boolean) : null,
        })),
      });
      toast.success(`Qaralama yazıldı: ${saved.documentNo}`);
      setEditOpen(false);
      void load();
      setDetail(saved);
    } catch (e) {
      toast.error(e instanceof Error ? e.message : "Yazılmadı.");
    } finally {
      setSaving(false);
    }
  };

  const openDetail = async (id: number) => {
    try {
      const p = await getStorePurchase(id);
      setDetail(p);
      setConfirmPay("");
      setCancelReason("");
      setRetQty({});
      setRetSerials({});
      setRetReason("");
    } catch (e) {
      toast.error(e instanceof Error ? e.message : "Alış yüklənmədi.");
    }
  };

  const run = async (fn: () => Promise<StorePurchase>, success: string) => {
    setBusy(true);
    try {
      setDetail(await fn());
      toast.success(success);
      void load();
    } catch (e) {
      toast.error(e instanceof Error ? e.message : "Əməliyyat alınmadı.");
    } finally {
      setBusy(false);
    }
  };

  const doConfirm = () =>
    run(
      () =>
        confirmStorePurchase(
          detail!.id,
          num(confirmPay) > 0 ? { amount: num(confirmPay), method: confirmMethod } : null,
        ),
      "Alış təsdiqləndi: mal anbara daxil oldu, təchizatçının balansı artdı",
    );

  const doReturn = () => {
    const lns = (detail?.lines ?? [])
      .map((l) => ({
        purchaseLineId: l.id,
        quantity: l.tracksSerial ? (retSerials[l.id] ?? "").split(/[\n,;]+/).map((x) => x.trim()).filter(Boolean).length : num(retQty[l.id] ?? ""),
        serialNumbers: l.tracksSerial ? (retSerials[l.id] ?? "").split(/[\n,;]+/).map((x) => x.trim()).filter(Boolean) : null,
      }))
      .filter((l) => l.quantity > 0);
    if (lns.length === 0 || !retReason.trim()) {
      toast.error("Qaytarılacaq miqdarı və səbəbi yazın.");
      return Promise.resolve();
    }
    return run(() => returnStorePurchase(detail!.id, { reason: retReason.trim(), lines: lns }), "Mal təchizatçıya qaytarıldı");
  };

  const pages = Math.max(1, Math.ceil(total / PAGE_SIZE));

  return (
    <div className="mx-auto max-w-6xl space-y-4 p-4">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div>
          <h1 className="text-2xl font-bold">Alışlar (mal qəbulu)</h1>
          <p className="text-sm text-muted-foreground">Qaralama stoka və borca təsir etmir; təsdiqdən sonra mal anbara düşür, təchizatçı borcu yaranır.</p>
        </div>
        {canManage && (
          <Button onClick={openNew}>
            <Plus className="mr-1 h-4 w-4" />
            Yeni alış
          </Button>
        )}
      </div>

      <div className="flex flex-wrap items-end gap-3 rounded-lg border bg-card p-3">
        <div className="space-y-1">
          <Label htmlFor="pu-from">Başlanğıc</Label>
          <Input id="pu-from" type="date" value={from} max={to} onChange={(e) => { setFrom(e.target.value); setPage(1); }} />
        </div>
        <div className="space-y-1">
          <Label htmlFor="pu-to">Son</Label>
          <Input id="pu-to" type="date" value={to} min={from} onChange={(e) => { setTo(e.target.value); setPage(1); }} />
        </div>
        <div className="space-y-1">
          <Label htmlFor="pu-sup">Təchizatçı</Label>
          <select id="pu-sup" className="h-9 rounded-md border border-input bg-background px-3 text-sm" value={supplierFilter} onChange={(e) => { setSupplierFilter(e.target.value ? Number(e.target.value) : ""); setPage(1); }}>
            <option value="">Hamısı</option>
            {suppliers.map((s) => (
              <option key={s.id} value={s.id}>
                {s.name}
              </option>
            ))}
          </select>
        </div>
        <div className="space-y-1">
          <Label htmlFor="pu-status">Status</Label>
          <select id="pu-status" className="h-9 rounded-md border border-input bg-background px-3 text-sm" value={statusFilter} onChange={(e) => { setStatusFilter(e.target.value === "" ? "" : Number(e.target.value)); setPage(1); }}>
            <option value="">Hamısı</option>
            {Object.entries(PURCHASE_STATUS_LABELS).map(([k, label]) => (
              <option key={k} value={k}>
                {label}
              </option>
            ))}
          </select>
        </div>
        <div className="space-y-1">
          <Label htmlFor="pu-q">Sənəd №</Label>
          <Input id="pu-q" value={q} onChange={(e) => { setQ(e.target.value); setPage(1); }} placeholder="P-… / qaimə" />
        </div>
      </div>

      <div className="overflow-x-auto rounded-lg border bg-card">
        <table className="w-full text-sm">
          <thead>
            <tr className="bg-muted/40 text-left text-xs text-muted-foreground">
              <th className="px-3 py-2 font-medium">Sənəd</th>
              <th className="px-3 py-2 font-medium">Tarix</th>
              <th className="px-3 py-2 font-medium">Təchizatçı</th>
              <th className="px-3 py-2 font-medium">Status</th>
              <th className="px-3 py-2 text-right font-medium">Məbləğ</th>
              <th className="px-3 py-2 text-right font-medium">Ödənilib</th>
              <th className="px-3 py-2 text-right font-medium">Qalan</th>
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
                  Alış tapılmadı.
                </td>
              </tr>
            ) : (
              items.map((p) => (
                <tr key={p.id} className="cursor-pointer border-t hover:bg-muted/40" onClick={() => void openDetail(p.id)}>
                  <td className="px-3 py-2 font-medium">
                    {p.documentNo}
                    {p.supplierInvoiceNo && <span className="block text-xs text-muted-foreground">Qaimə: {p.supplierInvoiceNo}</span>}
                  </td>
                  <td className="px-3 py-2">{fmtDate(p.purchaseDate)}</td>
                  <td className="px-3 py-2">{p.supplierName}</td>
                  <td className="px-3 py-2">{statusBadge(p.status)}</td>
                  <td className="px-3 py-2 text-right">{formatCurrency(p.total)}</td>
                  <td className="px-3 py-2 text-right">{formatCurrency(p.paid)}</td>
                  <td className={cn("px-3 py-2 text-right font-medium", p.outstanding > 0 && "text-red-600")}>{formatCurrency(p.outstanding)}</td>
                </tr>
              ))
            )}
          </tbody>
        </table>
      </div>

      <div className="flex items-center justify-between text-sm text-muted-foreground">
        <span>Cəmi: {total}</span>
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

      {/* Editor */}
      <Dialog open={editOpen} onOpenChange={(o) => !o && !saving && setEditOpen(false)}>
        <DialogContent className="max-h-[92vh] overflow-y-auto sm:max-w-4xl">
          <DialogHeader>
            <DialogTitle>{editingId ? "Qaralamanı dəyiş" : "Yeni alış"}</DialogTitle>
            <DialogDescription>Qiymətlər qaimədəki kimi, ƏDV-siz yazılır; ƏDV üstünə əlavə olunur.</DialogDescription>
          </DialogHeader>
          <div className="space-y-4">
            <div className="grid gap-3 sm:grid-cols-3">
              <div className="space-y-1">
                <Label htmlFor="pe-sup">Təchizatçı</Label>
                <select id="pe-sup" className={SELECT} value={supplierId} onChange={(e) => setSupplierId(e.target.value ? Number(e.target.value) : "")}>
                  <option value="">Seçin…</option>
                  {suppliers.filter((s) => s.isActive).map((s) => (
                    <option key={s.id} value={s.id}>
                      {s.name}
                    </option>
                  ))}
                </select>
              </div>
              <div className="space-y-1">
                <Label htmlFor="pe-wh">Anbar</Label>
                <select id="pe-wh" className={SELECT} value={warehouseId} onChange={(e) => setWarehouseId(e.target.value ? Number(e.target.value) : "")}>
                  <option value="">Seçin…</option>
                  {warehouses.map((w) => (
                    <option key={w.id} value={w.id}>
                      {w.name}
                    </option>
                  ))}
                </select>
              </div>
              <div className="space-y-1">
                <Label htmlFor="pe-inv">Təchizatçının qaimə №</Label>
                <Input id="pe-inv" value={invoiceNo} onChange={(e) => setInvoiceNo(e.target.value)} />
              </div>
              <div className="space-y-1">
                <Label htmlFor="pe-date">Tarix</Label>
                <Input id="pe-date" type="date" value={purchaseDate} onChange={(e) => setPurchaseDate(e.target.value)} />
              </div>
              <div className="space-y-1">
                <Label htmlFor="pe-due">Ödəniş tarixi (boşdursa təchizatçının müddəti)</Label>
                <Input id="pe-due" type="date" value={dueDate} onChange={(e) => setDueDate(e.target.value)} />
              </div>
              <div className="space-y-1">
                <Label htmlFor="pe-note">Qeyd</Label>
                <Input id="pe-note" value={note} onChange={(e) => setNote(e.target.value)} />
              </div>
            </div>

            <div className="space-y-2">
              <Input placeholder="Məhsul əlavə et: ad, SKU və ya barkod…" value={term} onChange={(e) => setTerm(e.target.value)} />
              {options.length > 0 && (
                <div className="max-h-44 overflow-y-auto rounded-md border">
                  {options.map((p) => (
                    <button type="button" key={p.id} className="flex w-full justify-between px-3 py-1.5 text-left text-sm hover:bg-muted" onClick={() => addProduct(p)}>
                      <span>{p.name}</span>
                      <span className="text-xs text-muted-foreground">son alış: {formatCurrency(p.purchasePrice)}</span>
                    </button>
                  ))}
                </div>
              )}
            </div>

            {lines.length > 0 && (
              <div className="overflow-x-auto rounded-md border">
                <table className="w-full text-sm">
                  <thead>
                    <tr className="bg-muted/40 text-left text-xs text-muted-foreground">
                      <th className="px-2 py-2 font-medium">Məhsul</th>
                      <th className="px-2 py-2 font-medium">Miqdar</th>
                      <th className="px-2 py-2 font-medium">Qiymət</th>
                      <th className="px-2 py-2 font-medium">Endirim %</th>
                      <th className="px-2 py-2 font-medium">ƏDV %</th>
                      <th className="px-2 py-2 text-right font-medium">Cəmi</th>
                      <th className="px-2 py-2" />
                    </tr>
                  </thead>
                  <tbody>
                    {lines.map((l) => (
                      <tr key={l.product.id} className="border-t align-top">
                        <td className="px-2 py-2">
                          {l.product.name}
                          {l.product.tracksSerial && (
                            <Textarea
                              className="mt-1 text-xs"
                              rows={2}
                              placeholder="IMEI / serial (hər sətirdə bir)"
                              value={l.serials}
                              onChange={(e) => updateLine(l.product.id, { serials: e.target.value })}
                            />
                          )}
                        </td>
                        <td className="px-2 py-2">
                          {l.product.tracksSerial ? (
                            <span className="text-xs text-muted-foreground">{l.serials.split(/[\n,;]+/).map((x) => x.trim()).filter(Boolean).length} ədəd</span>
                          ) : (
                            <Input className="h-8 w-20" inputMode="decimal" value={l.quantity} onChange={(e) => updateLine(l.product.id, { quantity: e.target.value })} />
                          )}
                        </td>
                        <td className="px-2 py-2">
                          <Input className="h-8 w-24" inputMode="decimal" value={l.unitPrice} onChange={(e) => updateLine(l.product.id, { unitPrice: e.target.value })} />
                        </td>
                        <td className="px-2 py-2">
                          <Input className="h-8 w-16" inputMode="decimal" value={l.discount} onChange={(e) => updateLine(l.product.id, { discount: e.target.value })} />
                        </td>
                        <td className="px-2 py-2">
                          <Input className="h-8 w-16" inputMode="decimal" value={l.vat} onChange={(e) => updateLine(l.product.id, { vat: e.target.value })} />
                        </td>
                        <td className="px-2 py-2 text-right font-medium">{formatCurrency(lineTotal(l))}</td>
                        <td className="px-2 py-2">
                          <Button size="icon" variant="ghost" className="h-8 w-8" onClick={() => setLines((prev) => prev.filter((x) => x.product.id !== l.product.id))}>
                            <Trash2 className="h-4 w-4" />
                          </Button>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}
            <p className="text-right text-lg font-semibold">Yekun: {formatCurrency(editTotal)}</p>
          </div>
          <DialogFooter>
            <Button variant="outline" disabled={saving} onClick={() => setEditOpen(false)}>
              Ləğv et
            </Button>
            <Button disabled={saving} onClick={() => void submitDraft()}>
              {saving ? "Yazılır…" : "Qaralamanı yaz"}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* Detail */}
      <Dialog open={detail != null} onOpenChange={(o) => !o && setDetail(null)}>
        <DialogContent className="max-h-[92vh] overflow-y-auto sm:max-w-3xl">
          <DialogHeader>
            <DialogTitle className="flex items-center gap-2">
              {detail?.documentNo} {detail && statusBadge(detail.status)}
            </DialogTitle>
            <DialogDescription>
              {detail?.supplierName} — {detail?.warehouseName} — {detail ? fmtDate(detail.purchaseDate) : ""}
              {detail?.supplierInvoiceNo && ` — qaimə ${detail.supplierInvoiceNo}`}
            </DialogDescription>
          </DialogHeader>
          {detail && (
            <div className="space-y-4">
              <div className="overflow-x-auto rounded-md border">
                <table className="w-full text-sm">
                  <thead>
                    <tr className="bg-muted/40 text-left text-xs text-muted-foreground">
                      <th className="px-2 py-2 font-medium">Məhsul</th>
                      <th className="px-2 py-2 text-right font-medium">Miqdar</th>
                      <th className="px-2 py-2 text-right font-medium">Qiymət</th>
                      <th className="px-2 py-2 text-right font-medium">Endirim / ƏDV</th>
                      <th className="px-2 py-2 text-right font-medium">Cəmi</th>
                      {detail.status === StorePurchaseStatus.Confirmed && canReturn && (
                        <th className="px-2 py-2 font-medium">Qaytar</th>
                      )}
                    </tr>
                  </thead>
                  <tbody>
                    {detail.lines.map((l) => (
                      <tr key={l.id} className="border-t align-top">
                        <td className="px-2 py-2">
                          {l.productName}
                          {l.serialNumbers.length > 0 && <span className="block text-xs text-muted-foreground">IMEI: {l.serialNumbers.join(", ")}</span>}
                          {l.returnedQuantity > 0 && <span className="block text-xs text-amber-600">qaytarılıb: {l.returnedQuantity}</span>}
                        </td>
                        <td className="px-2 py-2 text-right">{l.quantity}</td>
                        <td className="px-2 py-2 text-right">{formatCurrency(l.unitPrice)}</td>
                        <td className="px-2 py-2 text-right text-xs text-muted-foreground">
                          {l.discountPercent}% / {l.vatPercent}%
                        </td>
                        <td className="px-2 py-2 text-right font-medium">{formatCurrency(l.lineTotal)}</td>
                        {detail.status === StorePurchaseStatus.Confirmed && canReturn && (
                          <td className="px-2 py-2">
                            {l.returnableQuantity > 0 &&
                              (l.tracksSerial ? (
                                <Textarea
                                  className="w-40 text-xs"
                                  rows={2}
                                  placeholder="Qaytarılan IMEI-lər"
                                  value={retSerials[l.id] ?? ""}
                                  onChange={(e) => setRetSerials((prev) => ({ ...prev, [l.id]: e.target.value }))}
                                />
                              ) : (
                                <Input
                                  className="h-8 w-20"
                                  inputMode="decimal"
                                  placeholder={`≤ ${l.returnableQuantity}`}
                                  value={retQty[l.id] ?? ""}
                                  onChange={(e) => setRetQty((prev) => ({ ...prev, [l.id]: e.target.value }))}
                                />
                              ))}
                          </td>
                        )}
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>

              <div className="grid grid-cols-4 gap-2 text-sm">
                <div className="rounded-md border p-2">
                  <p className="text-xs text-muted-foreground">Yekun</p>
                  <p className="font-semibold">{formatCurrency(detail.total)}</p>
                </div>
                <div className="rounded-md border p-2">
                  <p className="text-xs text-muted-foreground">Qaytarılan</p>
                  <p className="font-semibold">{formatCurrency(detail.returned)}</p>
                </div>
                <div className="rounded-md border p-2">
                  <p className="text-xs text-muted-foreground">Ödənilən</p>
                  <p className="font-semibold">{formatCurrency(detail.paid)}</p>
                </div>
                <div className="rounded-md border p-2">
                  <p className="text-xs text-muted-foreground">Qalan borc</p>
                  <p className={cn("font-semibold", detail.outstanding > 0 && "text-red-600")}>{formatCurrency(detail.outstanding)}</p>
                </div>
              </div>

              {detail.returns.length > 0 && (
                <div className="text-sm">
                  <h3 className="mb-1 font-semibold">Təchizatçıya qaytarmalar</h3>
                  {detail.returns.map((r) => (
                    <div key={r.id} className="border-t py-1.5">
                      <div className="flex justify-between">
                        <span>
                          {r.documentNo} — {fmtDate(r.createdAtUtc)} — {r.reason}
                        </span>
                        <span className="font-medium">{formatCurrency(r.total)}</span>
                      </div>
                      <p className="text-xs text-muted-foreground">{r.lines.map((l) => `${l.productName} × ${l.quantity}`).join(", ")}</p>
                    </div>
                  ))}
                </div>
              )}

              {detail.status === StorePurchaseStatus.Cancelled && (
                <p className="rounded-md bg-red-50 p-2 text-sm text-red-700">Ləğv: {detail.cancelReason}</p>
              )}

              {detail.status === StorePurchaseStatus.Draft && (
                <div className="space-y-3 border-t pt-3">
                  {canConfirm && (
                    <div className="space-y-2">
                      <p className="text-sm font-medium">Təsdiq (mal qəbulu)</p>
                      <div className="flex flex-wrap items-end gap-2">
                        <div className="space-y-1">
                          <Label htmlFor="cf-pay">Dərhal ödəniş (istəyə bağlı)</Label>
                          <Input id="cf-pay" className="w-32" inputMode="decimal" value={confirmPay} onChange={(e) => setConfirmPay(e.target.value)} />
                        </div>
                        <select className="h-9 rounded-md border border-input bg-background px-3 text-sm" value={confirmMethod} onChange={(e) => setConfirmMethod(Number(e.target.value))}>
                          <option value={StorePaymentMethod.Cash}>{PAYMENT_LABELS[1]}</option>
                          <option value={StorePaymentMethod.Card}>{PAYMENT_LABELS[2]}</option>
                          <option value={StorePaymentMethod.BankTransfer}>{PAYMENT_LABELS[7]}</option>
                        </select>
                        <Button disabled={busy} onClick={() => void doConfirm()}>
                          Təsdiq et
                        </Button>
                      </div>
                    </div>
                  )}
                  <div className="flex flex-wrap gap-2">
                    {canManage && (
                      <Button variant="outline" onClick={() => openEdit(detail)}>
                        Dəyiş
                      </Button>
                    )}
                  </div>
                </div>
              )}

              {detail.status === StorePurchaseStatus.Confirmed && canReturn && (
                <div className="space-y-2 border-t pt-3">
                  <div className="flex flex-wrap items-end gap-2">
                    <div className="min-w-48 flex-1 space-y-1">
                      <Label htmlFor="rt-reason">Təchizatçıya qaytarma səbəbi</Label>
                      <Input id="rt-reason" value={retReason} onChange={(e) => setRetReason(e.target.value)} />
                    </div>
                    <Button variant="outline" disabled={busy} onClick={() => void doReturn()}>
                      Qaytar
                    </Button>
                  </div>
                </div>
              )}

              {detail.status !== StorePurchaseStatus.Cancelled && canConfirm && (
                <div className="flex flex-wrap items-end gap-2 border-t pt-3">
                  <div className="min-w-48 flex-1 space-y-1">
                    <Label htmlFor="cn-reason">Alışı ləğv et</Label>
                    <Input id="cn-reason" placeholder="Səbəb" value={cancelReason} onChange={(e) => setCancelReason(e.target.value)} />
                  </div>
                  <Button
                    variant="destructive"
                    disabled={busy}
                    onClick={() => {
                      if (!cancelReason.trim()) {
                        toast.error("Ləğv səbəbini yazın.");
                        return;
                      }
                      void run(() => cancelStorePurchase(detail.id, cancelReason.trim()), "Alış ləğv edildi");
                    }}
                  >
                    Ləğv et
                  </Button>
                </div>
              )}

              <p className="text-xs text-muted-foreground">
                Yaradan: {detail.createdByName ?? "—"} — {fmtDate(detail.createdAtUtc)}
                {detail.confirmedAtUtc && ` · təsdiq: ${detail.confirmedByName ?? "—"} — ${fmtDate(detail.confirmedAtUtc)}`}
              </p>
            </div>
          )}
        </DialogContent>
      </Dialog>
    </div>
  );
}
