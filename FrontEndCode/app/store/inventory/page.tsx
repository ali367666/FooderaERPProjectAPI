"use client";

import { useCallback, useEffect, useMemo, useState } from "react";
import { Plus } from "lucide-react";
import { toast } from "sonner";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from "@/components/ui/dialog";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { useStorePermissions as usePermissionSet } from "@/lib/store/use-store-permissions";
import { formatCurrency } from "@/lib/format-currency";
import { cn } from "@/lib/utils";
import { getWarehouses, type Warehouse } from "@/lib/services/warehouse-service";
import {
  INVENTORY_STATUS_LABELS,
  StoreInventoryStatus,
  UNIT_LABELS,
  cancelStoreInventory,
  confirmStoreInventory,
  getStoreCategories,
  getStoreInventories,
  getStoreInventory,
  saveStoreInventoryCounts,
  startStoreInventory,
  type StoreCategory,
  type StoreInventory,
} from "@/lib/store/store-service";

const SELECT = "h-9 w-full rounded-md border border-input bg-background px-3 text-sm";
const fmtDate = (v: string) => new Date(v).toLocaleDateString("az-AZ");

function statusBadge(status: number) {
  const variant = status === StoreInventoryStatus.Confirmed ? "default" : status === StoreInventoryStatus.Cancelled ? "destructive" : "secondary";
  return <Badge variant={variant}>{INVENTORY_STATUS_LABELS[status] ?? status}</Badge>;
}

export default function StoreInventoryPage() {
  const permissions = usePermissionSet();
  const canManage = permissions.has("StoreInventory.Manage");
  const canConfirm = permissions.has("StoreInventory.Confirm");

  const [warehouses, setWarehouses] = useState<Warehouse[]>([]);
  const [categories, setCategories] = useState<StoreCategory[]>([]);
  const [warehouseFilter, setWarehouseFilter] = useState<number | "">("");
  const [list, setList] = useState<StoreInventory[]>([]);
  const [loading, setLoading] = useState(true);

  const [startOpen, setStartOpen] = useState(false);
  const [startWarehouse, setStartWarehouse] = useState<number | "">("");
  const [startCategory, setStartCategory] = useState<number | "">("");
  const [startNote, setStartNote] = useState("");
  const [starting, setStarting] = useState(false);

  const [current, setCurrent] = useState<StoreInventory | null>(null);
  const [counts, setCounts] = useState<Record<number, string>>({});
  const [reasons, setReasons] = useState<Record<number, string>>({});
  const [term, setTerm] = useState("");
  const [onlyDiff, setOnlyDiff] = useState(false);
  const [busy, setBusy] = useState(false);

  useEffect(() => {
    getWarehouses().then(setWarehouses).catch(() => setWarehouses([]));
    getStoreCategories().then(setCategories).catch(() => setCategories([]));
  }, []);

  const load = useCallback(async () => {
    setLoading(true);
    try {
      setList(await getStoreInventories({ warehouseId: warehouseFilter === "" ? null : warehouseFilter }));
    } catch (e) {
      toast.error(e instanceof Error ? e.message : "Yüklənmədi.");
    } finally {
      setLoading(false);
    }
  }, [warehouseFilter]);

  useEffect(() => {
    void load();
  }, [load]);

  const show = (inv: StoreInventory) => {
    setCurrent(inv);
    const c: Record<number, string> = {};
    const r: Record<number, string> = {};
    for (const l of inv.lines) {
      if (l.countedQuantity != null) c[l.id] = String(l.countedQuantity);
      if (l.reason) r[l.id] = l.reason;
    }
    setCounts(c);
    setReasons(r);
    setTerm("");
    setOnlyDiff(false);
  };

  const open = async (id: number) => {
    try {
      show(await getStoreInventory(id));
    } catch (e) {
      toast.error(e instanceof Error ? e.message : "Yüklənmədi.");
    }
  };

  const start = async () => {
    if (startWarehouse === "") {
      toast.error("Anbarı seçin.");
      return;
    }
    setStarting(true);
    try {
      const inv = await startStoreInventory({
        warehouseId: startWarehouse,
        categoryId: startCategory === "" ? null : startCategory,
        note: startNote.trim() || null,
      });
      setStartOpen(false);
      show(inv);
      void load();
      toast.success(`İnventarizasiya açıldı: ${inv.documentNo}`);
    } catch (e) {
      toast.error(e instanceof Error ? e.message : "Açılmadı.");
    } finally {
      setStarting(false);
    }
  };

  const draft = current?.status === StoreInventoryStatus.Draft;

  const liveDiff = (lineId: number, expected: number): number | null => {
    const raw = counts[lineId];
    if (raw === undefined || raw.trim() === "") return null;
    return Number(raw.replace(",", ".")) - expected;
  };

  const payload = () =>
    (current?.lines ?? []).map((l) => {
      const raw = counts[l.id];
      return {
        lineId: l.id,
        countedQuantity: raw === undefined || raw.trim() === "" ? null : Number(raw.replace(",", ".")),
        reason: reasons[l.id]?.trim() || null,
      };
    });

  const saveCounts = async (): Promise<StoreInventory | null> => {
    if (!current) return null;
    try {
      const inv = await saveStoreInventoryCounts(current.id, payload());
      setCurrent(inv);
      return inv;
    } catch (e) {
      toast.error(e instanceof Error ? e.message : "Say yazılmadı.");
      return null;
    }
  };

  const confirm = async () => {
    if (!current) return;
    setBusy(true);
    try {
      if (!(await saveCounts())) return;
      const inv = await confirmStoreInventory(current.id);
      setCurrent(inv);
      toast.success("İnventarizasiya təsdiqləndi, fərqlər stok düzəlişi kimi yazıldı");
      void load();
    } catch (e) {
      toast.error(e instanceof Error ? e.message : "Təsdiqlənmədi.");
    } finally {
      setBusy(false);
    }
  };

  const lines = useMemo(() => {
    if (!current) return [];
    const t = term.trim().toLowerCase();
    return current.lines.filter((l) => {
      if (t && !l.productName.toLowerCase().includes(t) && !(l.barcode ?? "").includes(t)) return false;
      if (onlyDiff) {
        const d = draft ? liveDiff(l.id, l.expectedQuantity) : l.difference;
        return d != null && d !== 0;
      }
      return true;
    });
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [current, term, onlyDiff, counts, draft]);

  return (
    <div className="mx-auto max-w-6xl space-y-4 p-4">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div>
          <h1 className="text-2xl font-bold">İnventarizasiya</h1>
          <p className="text-sm text-muted-foreground">Sistemin gözlədiyi qalıq dondurulur, faktiki say yazılır. Stok yalnız təsdiqdən sonra dəyişir.</p>
        </div>
        {canManage && (
          <Button
            onClick={() => {
              setStartWarehouse(warehouseFilter);
              setStartCategory("");
              setStartNote("");
              setStartOpen(true);
            }}
          >
            <Plus className="mr-1 h-4 w-4" />
            Yeni inventarizasiya
          </Button>
        )}
      </div>

      <select
        className="h-9 rounded-md border border-input bg-background px-3 text-sm"
        value={warehouseFilter}
        onChange={(e) => setWarehouseFilter(e.target.value ? Number(e.target.value) : "")}
      >
        <option value="">Bütün anbarlar</option>
        {warehouses.map((w) => (
          <option key={w.id} value={w.id}>
            {w.name}
          </option>
        ))}
      </select>

      <div className="overflow-x-auto rounded-lg border bg-card">
        <table className="w-full text-sm">
          <thead>
            <tr className="bg-muted/40 text-left text-xs text-muted-foreground">
              <th className="px-3 py-2 font-medium">Sənəd</th>
              <th className="px-3 py-2 font-medium">Tarix</th>
              <th className="px-3 py-2 font-medium">Anbar</th>
              <th className="px-3 py-2 font-medium">Status</th>
              <th className="px-3 py-2 text-right font-medium">Sayılıb</th>
              <th className="px-3 py-2 text-right font-medium">Fərq sayı</th>
              <th className="px-3 py-2 text-right font-medium">Artıq / əskik (AZN)</th>
            </tr>
          </thead>
          <tbody>
            {loading ? (
              <tr>
                <td colSpan={7} className="px-3 py-8 text-center text-muted-foreground">
                  Yüklənir…
                </td>
              </tr>
            ) : list.length === 0 ? (
              <tr>
                <td colSpan={7} className="px-3 py-8 text-center text-muted-foreground">
                  İnventarizasiya yoxdur.
                </td>
              </tr>
            ) : (
              list.map((i) => (
                <tr key={i.id} className="cursor-pointer border-t hover:bg-muted/40" onClick={() => void open(i.id)}>
                  <td className="px-3 py-2 font-medium">
                    {i.documentNo}
                    {i.categoryName && <span className="block text-xs text-muted-foreground">{i.categoryName}</span>}
                  </td>
                  <td className="px-3 py-2">{fmtDate(i.createdAtUtc)}</td>
                  <td className="px-3 py-2">{i.warehouseName}</td>
                  <td className="px-3 py-2">{statusBadge(i.status)}</td>
                  <td className="px-3 py-2 text-right">
                    {i.countedCount} / {i.lineCount}
                  </td>
                  <td className="px-3 py-2 text-right">{i.differenceCount}</td>
                  <td className="px-3 py-2 text-right">
                    <span className="text-emerald-600">+{formatCurrency(i.surplusValue)}</span> /{" "}
                    <span className="text-red-600">{formatCurrency(i.shortageValue)}</span>
                  </td>
                </tr>
              ))
            )}
          </tbody>
        </table>
      </div>

      <Dialog open={startOpen} onOpenChange={(o) => !o && !starting && setStartOpen(false)}>
        <DialogContent className="sm:max-w-sm">
          <DialogHeader>
            <DialogTitle>Yeni inventarizasiya</DialogTitle>
            <DialogDescription>Anbarın bütün məhsulları və ya yalnız bir kateqoriya sayılır.</DialogDescription>
          </DialogHeader>
          <div className="space-y-3">
            <div className="space-y-1">
              <Label htmlFor="inv-wh">Anbar</Label>
              <select id="inv-wh" className={SELECT} value={startWarehouse} onChange={(e) => setStartWarehouse(e.target.value ? Number(e.target.value) : "")}>
                <option value="">Seçin…</option>
                {warehouses.map((w) => (
                  <option key={w.id} value={w.id}>
                    {w.name}
                  </option>
                ))}
              </select>
            </div>
            <div className="space-y-1">
              <Label htmlFor="inv-cat">Kateqoriya (istəyə bağlı)</Label>
              <select id="inv-cat" className={SELECT} value={startCategory} onChange={(e) => setStartCategory(e.target.value ? Number(e.target.value) : "")}>
                <option value="">Hamısı</option>
                {categories.map((c) => (
                  <option key={c.id} value={c.id}>
                    {c.name}
                  </option>
                ))}
              </select>
            </div>
            <div className="space-y-1">
              <Label htmlFor="inv-note">Qeyd</Label>
              <Input id="inv-note" value={startNote} onChange={(e) => setStartNote(e.target.value)} />
            </div>
            <p className="text-xs text-muted-foreground">IMEI/serial ilə izlənən məhsullar bu sayıma düşmür; onlar nömrə ilə izlənir.</p>
          </div>
          <DialogFooter>
            <Button variant="outline" disabled={starting} onClick={() => setStartOpen(false)}>
              Ləğv et
            </Button>
            <Button disabled={starting} onClick={() => void start()}>
              {starting ? "Açılır…" : "Aç"}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      <Dialog open={current != null} onOpenChange={(o) => !o && setCurrent(null)}>
        <DialogContent className="max-h-[92vh] overflow-y-auto sm:max-w-5xl">
          <DialogHeader>
            <DialogTitle className="flex items-center gap-2">
              {current?.documentNo} {current && statusBadge(current.status)}
            </DialogTitle>
            <DialogDescription>
              {current?.warehouseName}
              {current?.categoryName && ` — ${current.categoryName}`}
              {current?.confirmedAtUtc && ` — təsdiq: ${current.confirmedByName ?? "—"}, ${fmtDate(current.confirmedAtUtc)}`}
            </DialogDescription>
          </DialogHeader>
          {current && (
            <div className="space-y-3">
              <div className="flex flex-wrap items-center gap-3">
                <Input className="max-w-60" placeholder="Ad və ya barkod…" value={term} onChange={(e) => setTerm(e.target.value)} />
                <label className="flex items-center gap-2 text-sm">
                  <input type="checkbox" checked={onlyDiff} onChange={(e) => setOnlyDiff(e.target.checked)} />
                  Yalnız fərqlər
                </label>
                <span className="ml-auto text-sm text-muted-foreground">
                  Artıq: <span className="text-emerald-600">+{formatCurrency(current.surplusValue)}</span> · Əskik:{" "}
                  <span className="text-red-600">{formatCurrency(current.shortageValue)}</span>
                </span>
              </div>
              <div className="max-h-[55vh] overflow-auto rounded-md border">
                <table className="w-full text-sm">
                  <thead className="sticky top-0 bg-muted">
                    <tr className="text-left text-xs text-muted-foreground">
                      <th className="px-2 py-2 font-medium">Məhsul</th>
                      <th className="px-2 py-2 text-right font-medium">Sistemdə</th>
                      <th className="px-2 py-2 text-right font-medium">Sayılan</th>
                      <th className="px-2 py-2 text-right font-medium">Fərq</th>
                      <th className="px-2 py-2 font-medium">Səbəb</th>
                    </tr>
                  </thead>
                  <tbody>
                    {lines.map((l) => {
                      const diff = draft ? liveDiff(l.id, l.expectedQuantity) : l.countedQuantity == null ? null : l.difference;
                      return (
                        <tr key={l.id} className="border-t">
                          <td className="px-2 py-1.5">
                            {l.productName}
                            {l.barcode && <span className="block text-xs text-muted-foreground">{l.barcode}</span>}
                          </td>
                          <td className="px-2 py-1.5 text-right">
                            {l.expectedQuantity} <span className="text-xs text-muted-foreground">{UNIT_LABELS[l.unit]}</span>
                          </td>
                          <td className="px-2 py-1.5 text-right">
                            {draft ? (
                              <Input
                                className="ml-auto h-8 w-24 text-right"
                                inputMode="decimal"
                                value={counts[l.id] ?? ""}
                                onChange={(e) => setCounts((p) => ({ ...p, [l.id]: e.target.value }))}
                              />
                            ) : (
                              (l.countedQuantity ?? "—")
                            )}
                          </td>
                          <td className={cn("px-2 py-1.5 text-right font-medium", diff != null && diff < 0 && "text-red-600", diff != null && diff > 0 && "text-emerald-600")}>
                            {diff == null ? "—" : diff > 0 ? `+${diff}` : diff}
                          </td>
                          <td className="px-2 py-1.5">
                            {draft ? (
                              diff != null && diff !== 0 ? (
                                <Input
                                  className="h-8"
                                  placeholder="Fərqin səbəbi"
                                  value={reasons[l.id] ?? ""}
                                  onChange={(e) => setReasons((p) => ({ ...p, [l.id]: e.target.value }))}
                                />
                              ) : null
                            ) : (
                              <span className="text-xs text-muted-foreground">{l.reason}</span>
                            )}
                          </td>
                        </tr>
                      );
                    })}
                  </tbody>
                </table>
              </div>
            </div>
          )}
          {draft && (
            <DialogFooter className="gap-2">
              {canManage && (
                <>
                  <Button
                    variant="outline"
                    disabled={busy}
                    onClick={async () => {
                      if (!current) return;
                      try {
                        await cancelStoreInventory(current.id);
                        setCurrent(null);
                        void load();
                        toast.success("İnventarizasiya ləğv edildi");
                      } catch (e) {
                        toast.error(e instanceof Error ? e.message : "Ləğv edilmədi.");
                      }
                    }}
                  >
                    Ləğv et
                  </Button>
                  <Button
                    variant="outline"
                    disabled={busy}
                    onClick={async () => {
                      if (await saveCounts()) toast.success("Say yadda saxlanıldı");
                    }}
                  >
                    Sayı saxla
                  </Button>
                </>
              )}
              {canConfirm && (
                <Button disabled={busy} onClick={() => void confirm()}>
                  Təsdiq et (stoku düzəlt)
                </Button>
              )}
            </DialogFooter>
          )}
        </DialogContent>
      </Dialog>
    </div>
  );
}
