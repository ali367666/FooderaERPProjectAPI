"use client";

import { useCallback, useEffect, useState } from "react";
import { Plus } from "lucide-react";
import { toast } from "sonner";
import { Button } from "@/components/ui/button";
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from "@/components/ui/dialog";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { toApiFormError } from "@/lib/api-error";
import { formatCurrency } from "@/lib/format-currency";
import { toDateInputValue } from "@/lib/report-period";
import { getStockItems, type StockItem } from "@/lib/services/stock-item-service";
import {
  StockWriteOffReason,
  WRITE_OFF_REASON_LABELS,
  createStockWriteOff,
  getStockWriteOffs,
  type StockWriteOffList,
  type StockWriteOffReasonValue,
} from "@/lib/services/stock-writeoff-service";
import { getWarehouses, type Warehouse } from "@/lib/services/warehouse-service";

function formatDateTime(value: string): string {
  const d = new Date(value);
  return Number.isNaN(d.getTime()) ? value : d.toLocaleString("az-AZ");
}

const SELECT_CLASS = "h-9 w-full rounded-md border border-input bg-background px-3 text-sm";

export default function StockWriteOffsPage() {
  const today = toDateInputValue(new Date());
  const monthStart = toDateInputValue(new Date(new Date().getFullYear(), new Date().getMonth(), 1));

  const [from, setFrom] = useState(monthStart);
  const [to, setTo] = useState(today);
  const [warehouseFilter, setWarehouseFilter] = useState<number | "">("");
  const [list, setList] = useState<StockWriteOffList | null>(null);
  const [loading, setLoading] = useState(true);

  const [warehouses, setWarehouses] = useState<Warehouse[]>([]);
  const [stockItems, setStockItems] = useState<StockItem[]>([]);

  const [open, setOpen] = useState(false);
  const [saving, setSaving] = useState(false);
  const [warehouseId, setWarehouseId] = useState<number | "">("");
  const [stockItemId, setStockItemId] = useState<number | "">("");
  const [quantity, setQuantity] = useState("");
  const [reason, setReason] = useState<StockWriteOffReasonValue>(StockWriteOffReason.Waste);
  const [note, setNote] = useState("");

  useEffect(() => {
    getWarehouses().then(setWarehouses).catch(() => setWarehouses([]));
    getStockItems().then(setStockItems).catch(() => setStockItems([]));
  }, []);

  const load = useCallback(async () => {
    setLoading(true);
    try {
      setList(await getStockWriteOffs({ from, to, warehouseId: warehouseFilter === "" ? null : warehouseFilter }));
    } catch (e) {
      toast.error(e instanceof Error ? e.message : "Siyahı yüklənmədi.");
      setList(null);
    } finally {
      setLoading(false);
    }
  }, [from, to, warehouseFilter]);

  useEffect(() => {
    void load();
  }, [load]);

  const openAdd = () => {
    setWarehouseId(warehouseFilter);
    setStockItemId("");
    setQuantity("");
    setReason(StockWriteOffReason.Waste);
    setNote("");
    setOpen(true);
  };

  const submit = async (e: React.FormEvent) => {
    e.preventDefault();
    const qty = Number(quantity.replace(",", "."));
    if (warehouseId === "" || stockItemId === "") {
      toast.error("Anbar və məhsulu seçin.");
      return;
    }
    if (!Number.isFinite(qty) || qty <= 0) {
      toast.error("Miqdar düzgün deyil.");
      return;
    }
    setSaving(true);
    try {
      const message = await createStockWriteOff({
        warehouseId,
        stockItemId,
        quantity: qty,
        reason,
        note: note.trim() || null,
      });
      toast.success(message ?? "Mal silindi");
      setOpen(false);
      void load();
    } catch (err) {
      toast.error(toApiFormError(err, "Silmə yazılmadı").message);
    } finally {
      setSaving(false);
    }
  };

  return (
    <div className="space-y-6">
      <div className="flex flex-wrap items-start justify-between gap-3">
        <div>
          <h1 className="text-3xl font-bold text-foreground">Stok tənzimləmə</h1>
          <p className="mt-1 text-muted-foreground">
            İstifadəyə yararsız, xarab olan, sınan və ya müddəti bitən mal anbardan silinir. Alışın borcu dəyişmir.
          </p>
        </div>
        <Button onClick={openAdd}>
          <Plus className="mr-1 h-4 w-4" />
          Mal sil
        </Button>
      </div>

      <div className="flex flex-wrap items-end gap-3 rounded-lg border bg-card p-4">
        <div className="space-y-1">
          <Label htmlFor="wo-from">Başlanğıc</Label>
          <Input id="wo-from" type="date" value={from} max={to} onChange={(e) => setFrom(e.target.value)} />
        </div>
        <div className="space-y-1">
          <Label htmlFor="wo-to">Son</Label>
          <Input id="wo-to" type="date" value={to} min={from} onChange={(e) => setTo(e.target.value)} />
        </div>
        <div className="space-y-1">
          <Label htmlFor="wo-wh">Anbar</Label>
          <select
            id="wo-wh"
            className="h-9 rounded-md border border-input bg-background px-3 text-sm"
            value={warehouseFilter}
            onChange={(e) => setWarehouseFilter(e.target.value ? Number(e.target.value) : "")}
          >
            <option value="">Hamısı</option>
            {warehouses.map((w) => (
              <option key={w.id} value={w.id}>
                {w.name}
              </option>
            ))}
          </select>
        </div>
      </div>

      {loading ? (
        <p className="text-sm text-muted-foreground">Yüklənir…</p>
      ) : !list ? (
        <p className="text-sm text-muted-foreground">Siyahı yüklənmədi.</p>
      ) : (
        <div className="rounded-lg border bg-card p-4">
          <div className="mb-3 flex items-center justify-between">
            <h2 className="font-semibold">Silinənlər ({list.items.length})</h2>
            <p className="text-sm">
              Ümumi itki: <span className="font-semibold text-red-600">{formatCurrency(list.totalLossAzn)}</span>
            </p>
          </div>
          {list.items.length === 0 ? (
            <p className="text-sm text-muted-foreground">Bu dövr üçün qeyd yoxdur.</p>
          ) : (
            <div className="overflow-x-auto">
              <table className="w-full text-sm">
                <thead>
                  <tr className="text-left text-xs text-muted-foreground">
                    <th className="py-1 font-medium">Tarix</th>
                    <th className="py-1 font-medium">Kim</th>
                    <th className="py-1 font-medium">Anbar</th>
                    <th className="py-1 font-medium">Məhsul</th>
                    <th className="py-1 text-right font-medium">Miqdar</th>
                    <th className="py-1 font-medium">Səbəb</th>
                    <th className="py-1 text-right font-medium">İtki</th>
                  </tr>
                </thead>
                <tbody>
                  {list.items.map((r) => (
                    <tr key={r.id} className="border-t">
                      <td className="py-1.5">{formatDateTime(r.writtenOffAtUtc)}</td>
                      <td className="py-1.5">{r.writtenOffByUserName ?? "—"}</td>
                      <td className="py-1.5">{r.warehouseName}</td>
                      <td className="py-1.5">{r.stockItemName}</td>
                      <td className="py-1.5 text-right">{r.quantity}</td>
                      <td className="py-1.5">
                        {WRITE_OFF_REASON_LABELS[r.reason] ?? r.reason}
                        {r.note && <span className="block text-xs text-muted-foreground">{r.note}</span>}
                      </td>
                      <td className="py-1.5 text-right font-medium">{formatCurrency(r.lossAzn)}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </div>
      )}

      <Dialog open={open} onOpenChange={(o) => !o && !saving && setOpen(false)}>
        <DialogContent className="sm:max-w-md">
          <DialogHeader>
            <DialogTitle>Mal sil</DialogTitle>
            <DialogDescription>Mal anbardan çıxır. Alışın borcu dəyişmir.</DialogDescription>
          </DialogHeader>
          <form className="space-y-4" onSubmit={submit}>
            <div className="space-y-2">
              <Label htmlFor="wo-warehouse">Anbar</Label>
              <select
                id="wo-warehouse"
                className={SELECT_CLASS}
                value={warehouseId}
                onChange={(e) => setWarehouseId(e.target.value ? Number(e.target.value) : "")}
              >
                <option value="">Seçin…</option>
                {warehouses.map((w) => (
                  <option key={w.id} value={w.id}>
                    {w.name}
                  </option>
                ))}
              </select>
            </div>
            <div className="space-y-2">
              <Label htmlFor="wo-item">Məhsul</Label>
              <select
                id="wo-item"
                className={SELECT_CLASS}
                value={stockItemId}
                onChange={(e) => setStockItemId(e.target.value ? Number(e.target.value) : "")}
              >
                <option value="">Seçin…</option>
                {stockItems.map((i) => (
                  <option key={i.id} value={i.id}>
                    {i.name}
                  </option>
                ))}
              </select>
            </div>
            <div className="grid grid-cols-2 gap-3">
              <div className="space-y-2">
                <Label htmlFor="wo-qty">Miqdar</Label>
                <Input id="wo-qty" inputMode="decimal" value={quantity} onChange={(e) => setQuantity(e.target.value)} />
              </div>
              <div className="space-y-2">
                <Label htmlFor="wo-reason">Səbəb</Label>
                <select
                  id="wo-reason"
                  className={SELECT_CLASS}
                  value={reason}
                  onChange={(e) => setReason(Number(e.target.value) as StockWriteOffReasonValue)}
                >
                  {(Object.keys(WRITE_OFF_REASON_LABELS) as unknown as StockWriteOffReasonValue[]).map((k) => (
                    <option key={k} value={k}>
                      {WRITE_OFF_REASON_LABELS[k]}
                    </option>
                  ))}
                </select>
              </div>
            </div>
            <div className="space-y-2">
              <Label htmlFor="wo-note">Qeyd (istəyə bağlı)</Label>
              <Input id="wo-note" value={note} onChange={(e) => setNote(e.target.value)} />
            </div>
            <DialogFooter>
              <Button type="button" variant="outline" disabled={saving} onClick={() => setOpen(false)}>
                Ləğv et
              </Button>
              <Button type="submit" disabled={saving}>
                {saving ? "Yazılır…" : "Sil"}
              </Button>
            </DialogFooter>
          </form>
        </DialogContent>
      </Dialog>
    </div>
  );
}
