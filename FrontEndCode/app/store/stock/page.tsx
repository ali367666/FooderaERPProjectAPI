"use client";

import { useCallback, useEffect, useState } from "react";
import { Plus } from "lucide-react";
import { toast } from "sonner";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from "@/components/ui/dialog";
import { Input } from "@/components/ui/input";
import { Textarea } from "@/components/ui/textarea";
import { Label } from "@/components/ui/label";
import { useStorePermissions as usePermissionSet } from "@/lib/store/use-store-permissions";
import { formatCurrency } from "@/lib/format-currency";
import { cn } from "@/lib/utils";
import { getWarehouses, type Warehouse } from "@/lib/services/warehouse-service";
import {
  MOVEMENT_LABELS,
  SERIAL_EVENT_LABELS,
  SERIAL_STATUS_LABELS,
  StoreStockMovementType,
  UNIT_LABELS,
  adjustStoreStock,
  getStoreMovements,
  getStoreSerial,
  getStoreStock,
  searchStoreProducts,
  searchStoreSerials,
  type AdjustTypeValue,
  type StoreProduct,
  type StoreSerialUnit,
  type StoreStockBalance,
  type StoreStockMovement,
} from "@/lib/store/store-service";

const SELECT = "h-9 w-full rounded-md border border-input bg-background px-3 text-sm";

const ADJUST_LABELS: Record<number, string> = {
  [StoreStockMovementType.AdjustmentIncrease]: "Stok artır (sayımda artıq çıxdı, daxil oldu)",
  [StoreStockMovementType.AdjustmentDecrease]: "Stok azalt (sayımda əskik çıxdı)",
  [StoreStockMovementType.Waste]: "Zay (xarab, sınıq, istifadəyə yararsız)",
  [StoreStockMovementType.InternalUse]: "Daxili istifadə",
};

function formatDateTime(value: string): string {
  const d = new Date(value);
  return Number.isNaN(d.getTime()) ? value : d.toLocaleString("az-AZ");
}

export default function StoreStockPage() {
  const canAdjust = usePermissionSet().has("StoreStock.Adjust");

  const [tab, setTab] = useState<"balances" | "movements" | "serials">("balances");
  const [serials, setSerials] = useState<StoreSerialUnit[]>([]);
  const [serialStatus, setSerialStatus] = useState<number | "">("");
  const [serialDetail, setSerialDetail] = useState<StoreSerialUnit | null>(null);
  const [serialText, setSerialText] = useState("");
  const [warehouses, setWarehouses] = useState<Warehouse[]>([]);
  const [warehouseId, setWarehouseId] = useState<number | "">("");
  const [q, setQ] = useState("");
  const [lowOnly, setLowOnly] = useState(false);
  const [balances, setBalances] = useState<StoreStockBalance[]>([]);
  const [movements, setMovements] = useState<StoreStockMovement[]>([]);
  const [loading, setLoading] = useState(true);

  const [open, setOpen] = useState(false);
  const [saving, setSaving] = useState(false);
  const [adjWarehouse, setAdjWarehouse] = useState<number | "">("");
  const [adjType, setAdjType] = useState<AdjustTypeValue>(StoreStockMovementType.AdjustmentIncrease);
  const [productTerm, setProductTerm] = useState("");
  const [productOptions, setProductOptions] = useState<StoreProduct[]>([]);
  const [product, setProduct] = useState<StoreProduct | null>(null);
  const [quantity, setQuantity] = useState("");
  const [unitCost, setUnitCost] = useState("");
  const [reason, setReason] = useState("");

  useEffect(() => {
    getWarehouses().then(setWarehouses).catch(() => setWarehouses([]));
  }, []);

  const load = useCallback(async () => {
    setLoading(true);
    try {
      if (tab === "balances") {
        setBalances(await getStoreStock({ warehouseId: warehouseId === "" ? null : warehouseId, q: q.trim() || undefined, lowOnly }));
      } else if (tab === "serials") {
        setSerials(
          await searchStoreSerials({
            q: q.trim() || undefined,
            warehouseId: warehouseId === "" ? null : warehouseId,
            status: serialStatus === "" ? null : serialStatus,
          }),
        );
      } else {
        setMovements(await getStoreMovements({ warehouseId: warehouseId === "" ? null : warehouseId }));
      }
    } catch (e) {
      toast.error(e instanceof Error ? e.message : "Yüklənmədi.");
    } finally {
      setLoading(false);
    }
  }, [tab, warehouseId, q, lowOnly, serialStatus]);

  useEffect(() => {
    const t = setTimeout(() => void load(), 250);
    return () => clearTimeout(t);
  }, [load]);

  // Product picker inside the adjust dialog.
  useEffect(() => {
    if (!open || product || productTerm.trim().length < 1) {
      setProductOptions([]);
      return;
    }
    const t = setTimeout(() => {
      searchStoreProducts({ q: productTerm.trim(), pageSize: 8, isActive: true })
        .then((r) => setProductOptions(r.items))
        .catch(() => setProductOptions([]));
    }, 200);
    return () => clearTimeout(t);
  }, [open, productTerm, product]);

  const openAdjust = () => {
    setAdjWarehouse(warehouseId);
    setAdjType(StoreStockMovementType.AdjustmentIncrease);
    setProduct(null);
    setProductTerm("");
    setQuantity("");
    setUnitCost("");
    setReason("");
    setSerialText("");
    setOpen(true);
  };

  const submit = async (e: React.FormEvent) => {
    e.preventDefault();
    const serialList = serialText.split(/[\n,;]+/).map((x) => x.trim()).filter(Boolean);
    const qty = product?.tracksSerial ? serialList.length : Number(quantity.replace(",", "."));
    if (adjWarehouse === "" || !product) {
      toast.error("Anbar və məhsulu seçin.");
      return;
    }
    if (!Number.isFinite(qty) || qty <= 0) {
      toast.error("Miqdar düzgün deyil.");
      return;
    }
    if (!reason.trim()) {
      toast.error("Səbəbi yazın.");
      return;
    }
    setSaving(true);
    try {
      await adjustStoreStock({
        warehouseId: adjWarehouse,
        productId: product.id,
        type: adjType,
        quantity: qty,
        unitCost: adjType === StoreStockMovementType.AdjustmentIncrease && unitCost.trim() ? Number(unitCost.replace(",", ".")) : null,
        reason: reason.trim(),
        serialNumbers: product.tracksSerial ? serialList : null,
      });
      toast.success("Stok yeniləndi");
      setOpen(false);
      void load();
    } catch (err) {
      toast.error(err instanceof Error ? err.message : "Yazılmadı.");
    } finally {
      setSaving(false);
    }
  };

  const totalValue = balances.reduce((s, b) => s + b.value, 0);

  return (
    <div className="mx-auto max-w-6xl space-y-4 p-4">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <h1 className="text-2xl font-bold">Stok</h1>
        {canAdjust && (
          <Button onClick={openAdjust}>
            <Plus className="mr-1 h-4 w-4" />
            Stok düzəlişi
          </Button>
        )}
      </div>

      <div className="flex flex-wrap items-center gap-2">
        <div className="flex rounded-md border">
          {(["balances", "movements", "serials"] as const).map((t) => (
            <button
              key={t}
              onClick={() => setTab(t)}
              className={cn("px-3 py-1.5 text-sm", tab === t ? "bg-primary text-primary-foreground" : "hover:bg-muted")}
            >
              {t === "balances" ? "Qalıqlar" : t === "movements" ? "Hərəkətlər" : "IMEI / serial"}
            </button>
          ))}
        </div>
        <select
          className="h-9 rounded-md border border-input bg-background px-3 text-sm"
          value={warehouseId}
          onChange={(e) => setWarehouseId(e.target.value ? Number(e.target.value) : "")}
        >
          <option value="">Bütün anbarlar</option>
          {warehouses.map((w) => (
            <option key={w.id} value={w.id}>
              {w.name}
            </option>
          ))}
        </select>
        {tab === "serials" && (
          <>
            <Input className="max-w-60" placeholder="IMEI / serial…" value={q} onChange={(e) => setQ(e.target.value)} />
            <select
              className="h-9 rounded-md border border-input bg-background px-3 text-sm"
              value={serialStatus}
              onChange={(e) => setSerialStatus(e.target.value ? Number(e.target.value) : "")}
            >
              <option value="">Bütün statuslar</option>
              {Object.entries(SERIAL_STATUS_LABELS).map(([k, label]) => (
                <option key={k} value={k}>
                  {label}
                </option>
              ))}
            </select>
          </>
        )}
        {tab === "balances" && (
          <>
            <Input className="max-w-60" placeholder="Ad, SKU, barkod…" value={q} onChange={(e) => setQ(e.target.value)} />
            <label className="flex items-center gap-2 text-sm">
              <input type="checkbox" checked={lowOnly} onChange={(e) => setLowOnly(e.target.checked)} />
              Yalnız minimumdan aşağı
            </label>
          </>
        )}
      </div>

      {tab === "balances" ? (
        <>
          <div className="overflow-x-auto rounded-lg border bg-card">
            <table className="w-full text-sm">
              <thead>
                <tr className="bg-muted/40 text-left text-xs text-muted-foreground">
                  <th className="px-3 py-2 font-medium">Məhsul</th>
                  <th className="px-3 py-2 font-medium">Anbar</th>
                  <th className="px-3 py-2 text-right font-medium">Qalıq</th>
                  <th className="px-3 py-2 text-right font-medium">Minimum</th>
                  <th className="px-3 py-2 text-right font-medium">Orta maya</th>
                  <th className="px-3 py-2 text-right font-medium">Dəyər</th>
                </tr>
              </thead>
              <tbody>
                {loading ? (
                  <tr>
                    <td colSpan={6} className="px-3 py-8 text-center text-muted-foreground">
                      Yüklənir…
                    </td>
                  </tr>
                ) : balances.length === 0 ? (
                  <tr>
                    <td colSpan={6} className="px-3 py-8 text-center text-muted-foreground">
                      Qeyd yoxdur.
                    </td>
                  </tr>
                ) : (
                  balances.map((b) => (
                    <tr key={`${b.warehouseId}-${b.productId}`} className="border-t">
                      <td className="px-3 py-2 font-medium">
                        {b.productName}
                        {b.primaryBarcode && <span className="block text-xs text-muted-foreground">{b.primaryBarcode}</span>}
                      </td>
                      <td className="px-3 py-2">{b.warehouseName}</td>
                      <td className={cn("px-3 py-2 text-right font-medium", b.quantity < 0 && "text-red-600")}>
                        {b.quantity} {UNIT_LABELS[b.unit]} {b.isLow && <Badge variant="destructive">Az</Badge>}
                      </td>
                      <td className="px-3 py-2 text-right">{b.minimumStock}</td>
                      <td className="px-3 py-2 text-right">{formatCurrency(b.averageCost)}</td>
                      <td className="px-3 py-2 text-right">{formatCurrency(b.value)}</td>
                    </tr>
                  ))
                )}
              </tbody>
            </table>
          </div>
          {balances.length > 0 && (
            <p className="text-right text-sm">
              Stokun dəyəri: <span className="font-semibold">{formatCurrency(totalValue)}</span>
            </p>
          )}
        </>
      ) : tab === "serials" ? (
        <div className="overflow-x-auto rounded-lg border bg-card">
          <table className="w-full text-sm">
            <thead>
              <tr className="bg-muted/40 text-left text-xs text-muted-foreground">
                <th className="px-3 py-2 font-medium">IMEI / serial</th>
                <th className="px-3 py-2 font-medium">Məhsul</th>
                <th className="px-3 py-2 font-medium">Anbar</th>
                <th className="px-3 py-2 font-medium">Status</th>
                <th className="px-3 py-2 font-medium">Satılıb</th>
              </tr>
            </thead>
            <tbody>
              {loading ? (
                <tr>
                  <td colSpan={5} className="px-3 py-8 text-center text-muted-foreground">
                    Yüklənir…
                  </td>
                </tr>
              ) : serials.length === 0 ? (
                <tr>
                  <td colSpan={5} className="px-3 py-8 text-center text-muted-foreground">
                    Nömrə tapılmadı.
                  </td>
                </tr>
              ) : (
                serials.map((u) => (
                  <tr
                    key={u.id}
                    className="cursor-pointer border-t hover:bg-muted/40"
                    onClick={() => getStoreSerial(u.id).then(setSerialDetail).catch((e) => toast.error(e instanceof Error ? e.message : "Yüklənmədi."))}
                  >
                    <td className="px-3 py-2 font-medium">{u.serialNumber}</td>
                    <td className="px-3 py-2">{u.productName}</td>
                    <td className="px-3 py-2">{u.warehouseName ?? "—"}</td>
                    <td className="px-3 py-2">
                      <Badge variant={u.status === 1 ? "default" : "secondary"}>{SERIAL_STATUS_LABELS[u.status] ?? u.status}</Badge>
                    </td>
                    <td className="px-3 py-2">{u.soldAtUtc ? formatDateTime(u.soldAtUtc) : "—"}</td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>
      ) : (
        <div className="overflow-x-auto rounded-lg border bg-card">
          <table className="w-full text-sm">
            <thead>
              <tr className="bg-muted/40 text-left text-xs text-muted-foreground">
                <th className="px-3 py-2 font-medium">Tarix</th>
                <th className="px-3 py-2 font-medium">Məhsul</th>
                <th className="px-3 py-2 font-medium">Anbar</th>
                <th className="px-3 py-2 font-medium">Növ</th>
                <th className="px-3 py-2 text-right font-medium">Miqdar</th>
                <th className="px-3 py-2 text-right font-medium">Əvvəl → sonra</th>
                <th className="px-3 py-2 font-medium">Kim / səbəb</th>
              </tr>
            </thead>
            <tbody>
              {loading ? (
                <tr>
                  <td colSpan={7} className="px-3 py-8 text-center text-muted-foreground">
                    Yüklənir…
                  </td>
                </tr>
              ) : movements.length === 0 ? (
                <tr>
                  <td colSpan={7} className="px-3 py-8 text-center text-muted-foreground">
                    Hərəkət yoxdur.
                  </td>
                </tr>
              ) : (
                movements.map((m) => (
                  <tr key={m.id} className="border-t">
                    <td className="px-3 py-2">{formatDateTime(m.createdAtUtc)}</td>
                    <td className="px-3 py-2 font-medium">{m.productName}</td>
                    <td className="px-3 py-2">{m.warehouseName}</td>
                    <td className="px-3 py-2">
                      {MOVEMENT_LABELS[m.type] ?? m.typeName}
                      {m.documentNo && <span className="block text-xs text-muted-foreground">{m.documentNo}</span>}
                    </td>
                    <td className={cn("px-3 py-2 text-right font-medium", m.quantity < 0 ? "text-red-600" : "text-emerald-600")}>
                      {m.quantity > 0 ? "+" : ""}
                      {m.quantity}
                    </td>
                    <td className="px-3 py-2 text-right text-muted-foreground">
                      {m.balanceBefore} → {m.balanceAfter}
                    </td>
                    <td className="px-3 py-2">
                      {m.createdByUserName ?? "—"}
                      {m.reason && <span className="block text-xs text-muted-foreground">{m.reason}</span>}
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>
      )}

      <Dialog open={serialDetail != null} onOpenChange={(o) => !o && setSerialDetail(null)}>
        <DialogContent className="max-h-[90vh] overflow-y-auto sm:max-w-md">
          <DialogHeader>
            <DialogTitle>{serialDetail?.serialNumber}</DialogTitle>
            <DialogDescription>
              {serialDetail?.productName} — {serialDetail ? (SERIAL_STATUS_LABELS[serialDetail.status] ?? serialDetail.status) : ""}
            </DialogDescription>
          </DialogHeader>
          <div className="space-y-2">
            {(serialDetail?.history ?? []).map((h, i) => (
              <div key={i} className="rounded-md border p-2 text-sm">
                <div className="flex justify-between">
                  <span className="font-medium">{SERIAL_EVENT_LABELS[h.type] ?? h.type}</span>
                  <span className="text-xs text-muted-foreground">{formatDateTime(h.createdAtUtc)}</span>
                </div>
                {(h.documentNo || h.note) && (
                  <p className="text-xs text-muted-foreground">
                    {h.documentNo} {h.note}
                  </p>
                )}
              </div>
            ))}
          </div>
        </DialogContent>
      </Dialog>

      <Dialog open={open} onOpenChange={(o) => !o && !saving && setOpen(false)}>
        <DialogContent className="sm:max-w-md">
          <DialogHeader>
            <DialogTitle>Stok düzəlişi</DialogTitle>
            <DialogDescription>Hər düzəliş stok hərəkətlərində səbəblə qeyd olunur.</DialogDescription>
          </DialogHeader>
          <form className="space-y-4" onSubmit={submit}>
            <div className="space-y-2">
              <Label htmlFor="adj-wh">Anbar</Label>
              <select
                id="adj-wh"
                className={SELECT}
                value={adjWarehouse}
                onChange={(e) => setAdjWarehouse(e.target.value ? Number(e.target.value) : "")}
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
              <Label htmlFor="adj-type">Əməliyyat</Label>
              <select
                id="adj-type"
                className={SELECT}
                value={adjType}
                onChange={(e) => setAdjType(Number(e.target.value) as AdjustTypeValue)}
              >
                {Object.entries(ADJUST_LABELS).map(([k, label]) => (
                  <option key={k} value={k}>
                    {label}
                  </option>
                ))}
              </select>
            </div>
            <div className="space-y-2">
              <Label>Məhsul</Label>
              {product ? (
                <div className="flex items-center justify-between rounded-md border px-3 py-2 text-sm">
                  <span>{product.name}</span>
                  <Button type="button" size="sm" variant="ghost" onClick={() => setProduct(null)}>
                    Dəyiş
                  </Button>
                </div>
              ) : (
                <>
                  <Input placeholder="Ad, SKU və ya barkod…" value={productTerm} onChange={(e) => setProductTerm(e.target.value)} />
                  {productOptions.length > 0 && (
                    <div className="max-h-40 overflow-y-auto rounded-md border">
                      {productOptions.map((p) => (
                        <button
                          type="button"
                          key={p.id}
                          className="block w-full px-3 py-1.5 text-left text-sm hover:bg-muted"
                          onClick={() => setProduct(p)}
                        >
                          {p.name}
                          {p.primaryBarcode && <span className="ml-2 text-xs text-muted-foreground">{p.primaryBarcode}</span>}
                        </button>
                      ))}
                    </div>
                  )}
                </>
              )}
            </div>
            <div className="grid grid-cols-2 gap-3">
              {product?.tracksSerial ? (
                <div className="col-span-2 space-y-2">
                  <Label htmlFor="adj-serials">IMEI / serial nömrələri (hər sətirdə bir)</Label>
                  <Textarea id="adj-serials" rows={4} value={serialText} onChange={(e) => setSerialText(e.target.value)} />
                  <p className="text-xs text-muted-foreground">
                    Miqdar nömrələrin sayıdır: {serialText.split(/[\n,;]+/).map((x) => x.trim()).filter(Boolean).length}
                  </p>
                </div>
              ) : (
                <div className="space-y-2">
                  <Label htmlFor="adj-qty">Miqdar {product ? `(${UNIT_LABELS[product.unit]})` : ""}</Label>
                  <Input id="adj-qty" inputMode="decimal" value={quantity} onChange={(e) => setQuantity(e.target.value)} />
                </div>
              )}
              {adjType === StoreStockMovementType.AdjustmentIncrease && (
                <div className="space-y-2">
                  <Label htmlFor="adj-cost">Alış qiyməti (istəyə bağlı)</Label>
                  <Input id="adj-cost" inputMode="decimal" value={unitCost} onChange={(e) => setUnitCost(e.target.value)} />
                </div>
              )}
            </div>
            <div className="space-y-2">
              <Label htmlFor="adj-reason">Səbəb</Label>
              <Input id="adj-reason" value={reason} onChange={(e) => setReason(e.target.value)} placeholder="Məs.: sayım fərqi, sınıb, vitrində zədələnib" />
            </div>
            <DialogFooter>
              <Button type="button" variant="outline" disabled={saving} onClick={() => setOpen(false)}>
                Ləğv et
              </Button>
              <Button type="submit" disabled={saving}>
                {saving ? "Yazılır…" : "Yaz"}
              </Button>
            </DialogFooter>
          </form>
        </DialogContent>
      </Dialog>
    </div>
  );
}
