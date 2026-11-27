"use client";

import { Fragment, useCallback, useEffect, useMemo, useState } from "react";
import Link from "next/link";
import { ChevronDown, ChevronRight, Minus, Plus } from "lucide-react";
import { toast } from "sonner";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from "@/components/ui/dialog";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { toApiFormError } from "@/lib/api-error";
import { formatCurrency } from "@/lib/format-currency";
import { toDateInputValue } from "@/lib/report-period";
import {
  UNIT_SHORT,
  getStockAssetHistory,
  getStockAssets,
  increaseStockAsset,
  type StockAsset,
  type StockAssetList,
  type StockAssetOperation,
} from "@/lib/services/stock-asset-service";
import { StockWriteOffReason, createStockWriteOff, type StockWriteOffReasonValue } from "@/lib/services/stock-writeoff-service";
import { getWarehouses, type Warehouse } from "@/lib/services/warehouse-service";
import { cn } from "@/lib/utils";

const SELECT_CLASS = "h-9 w-full rounded-md border border-input bg-background px-3 text-sm";
const th = "px-3 py-2 font-medium";

type OpKind = "increase" | "broken" | "expense" | "other";

const OPERATIONS: { kind: OpKind; label: string; hint: string; increase: boolean; reason?: StockWriteOffReasonValue }[] = [
  { kind: "increase", label: "Artım", hint: "Alış sənədi olmadan stoka əlavə (hədiyyə, tapılan, düzəliş).", increase: true },
  { kind: "broken", label: "Sınma", hint: "Sındı, itdi, zədələndi — stokdan azalır.", increase: false, reason: StockWriteOffReason.Broken },
  { kind: "expense", label: "Xərcə silinmə", hint: "İstifadə olundu və ya xərcə yazıldı — stokdan azalır.", increase: false, reason: StockWriteOffReason.Expense },
  { kind: "other", label: "Digər azalma", hint: "Başqa səbəbdən azalma.", increase: false, reason: StockWriteOffReason.Other },
];

const dateTime = (iso: string) => {
  const d = new Date(iso);
  return `${d.toLocaleDateString("az-AZ")} ${d.toLocaleTimeString("az-AZ", { hour: "2-digit", minute: "2-digit" })}`;
};
const qty = (n: number) => String(Math.round(n * 1000) / 1000);

/** Company inventory of non-food goods (plates, glasses, cutlery, detergent): the stock, and every increase and decrease. */
export default function StockAssetsPage() {
  const today = toDateInputValue(new Date());
  const monthStart = toDateInputValue(new Date(new Date().getFullYear(), new Date().getMonth(), 1));

  const [tab, setTab] = useState<"stock" | "history">("stock");
  const [list, setList] = useState<StockAssetList | null>(null);
  const [loading, setLoading] = useState(true);
  const [term, setTerm] = useState("");
  const [open, setOpen] = useState<Set<number>>(new Set());
  const [warehouses, setWarehouses] = useState<Warehouse[]>([]);

  // history
  const [from, setFrom] = useState(monthStart);
  const [to, setTo] = useState(today);
  const [itemFilter, setItemFilter] = useState<number | "">("");
  const [opFilter, setOpFilter] = useState("");
  const [history, setHistory] = useState<StockAssetOperation[]>([]);
  const [historyLoading, setHistoryLoading] = useState(false);

  // operation dialog
  const [target, setTarget] = useState<StockAsset | null>(null);
  const [kind, setKind] = useState<OpKind>("increase");
  const [warehouseId, setWarehouseId] = useState<number | "">("");
  const [quantity, setQuantity] = useState("");
  const [note, setNote] = useState("");
  const [saving, setSaving] = useState(false);

  const loadStock = useCallback(async () => {
    setLoading(true);
    try {
      setList(await getStockAssets());
    } catch (e) {
      toast.error(toApiFormError(e, "Ləvazimat yüklənmədi").message);
    } finally {
      setLoading(false);
    }
  }, []);

  const loadHistory = useCallback(async () => {
    setHistoryLoading(true);
    try {
      setHistory(await getStockAssetHistory({ from, to, stockItemId: itemFilter === "" ? null : itemFilter }));
    } catch (e) {
      toast.error(toApiFormError(e, "Əməliyyatlar yüklənmədi").message);
    } finally {
      setHistoryLoading(false);
    }
  }, [from, to, itemFilter]);

  useEffect(() => {
    getWarehouses().then(setWarehouses).catch(() => setWarehouses([]));
    void loadStock();
  }, [loadStock]);

  useEffect(() => {
    if (tab === "history") void loadHistory();
  }, [tab, loadHistory]);

  const items = useMemo(() => {
    const q = term.trim().toLowerCase();
    return (list?.items ?? []).filter((i) => !q || i.name.toLowerCase().includes(q) || (i.barcode ?? "").includes(q) || i.categoryName.toLowerCase().includes(q));
  }, [list, term]);

  const operations = useMemo(() => [...new Set(history.map((h) => h.operation))].sort(), [history]);
  const shownHistory = useMemo(() => history.filter((h) => !opFilter || h.operation === opFilter), [history, opFilter]);

  const toggle = (id: number) =>
    setOpen((prev) => {
      const next = new Set(prev);
      if (next.has(id)) next.delete(id);
      else next.add(id);
      return next;
    });

  const startOperation = (item: StockAsset, k: OpKind) => {
    setTarget(item);
    setKind(k);
    setQuantity("");
    setNote("");
    // A decrease starts from the warehouse that holds the item; an increase from the first warehouse.
    const holder = item.warehouses[0]?.warehouseId ?? warehouses[0]?.id ?? "";
    setWarehouseId(k === "increase" ? (warehouses[0]?.id ?? holder) : holder);
  };

  const op = OPERATIONS.find((o) => o.kind === kind)!;
  const available = target?.warehouses.find((w) => w.warehouseId === warehouseId)?.quantity ?? 0;

  const submit = async () => {
    if (!target || warehouseId === "") {
      toast.error("Anbarı seçin.");
      return;
    }
    const amount = Number(quantity.replace(",", "."));
    if (!Number.isFinite(amount) || amount <= 0) {
      toast.error("Miqdarı yazın.");
      return;
    }
    if (!op.increase && amount > available) {
      toast.error(`Bu anbarda ${qty(available)} ${UNIT_SHORT[target.unit]} var.`);
      return;
    }
    setSaving(true);
    try {
      if (op.increase) {
        await increaseStockAsset({ warehouseId, stockItemId: target.stockItemId, quantity: amount, note: note.trim() || null });
      } else {
        await createStockWriteOff({
          warehouseId,
          stockItemId: target.stockItemId,
          quantity: amount,
          reason: op.reason!,
          note: note.trim() || null,
        });
      }
      toast.success(`${op.label}: ${target.name} — ${qty(amount)} ${UNIT_SHORT[target.unit]}`);
      setTarget(null);
      void loadStock();
      if (tab === "history") void loadHistory();
    } catch (e) {
      toast.error(toApiFormError(e, "Əməliyyat yazılmadı").message);
    } finally {
      setSaving(false);
    }
  };

  return (
    <div className="space-y-4 p-6">
      <div className="flex flex-wrap items-start justify-between gap-3">
        <div>
          <h1 className="text-2xl font-bold">Ləvazimat və inventar</h1>
          <p className="text-sm text-muted-foreground">
            Qab-qacaq, stəkan, çəngəl-bıçaq, təmizlik vasitələri kimi qeyri-ərzaq məhsulların stoku. Artım, sınma və xərcə silinmə burada yazılır, bütün əməliyyatlar tarixçədə görünür.
          </p>
        </div>
        <Button asChild variant="outline">
          <Link href="/dashboard/stock-items">
            <Plus className="mr-1 h-4 w-4" />
            Yeni ləvazimat qeydiyyatı
          </Link>
        </Button>
      </div>

      <p className="text-xs text-muted-foreground">
        Bura “Qeyri-ərzaq” növlü stok məhsulları düşür. Yeni məhsulu Stok məhsulları səhifəsində növünü “Qeyri-ərzaq” seçərək yaradın; alışı Stok alışlarından edə bilərsiniz.
      </p>

      <div className="flex rounded-md border w-fit">
        {(
          [
            ["stock", "Stok"],
            ["history", "Əməliyyatlar"],
          ] as const
        ).map(([k, label]) => (
          <button key={k} onClick={() => setTab(k)} className={cn("px-4 py-1.5 text-sm", tab === k ? "bg-primary text-primary-foreground" : "hover:bg-muted")}>
            {label}
          </button>
        ))}
      </div>

      {tab === "stock" ? (
        <div className="space-y-3">
          <div className="flex flex-wrap items-end justify-between gap-3">
            <div className="space-y-1">
              <Label htmlFor="sa-q">Axtar</Label>
              <Input id="sa-q" className="w-72" placeholder="Ad, barkod və ya kateqoriya…" value={term} onChange={(e) => setTerm(e.target.value)} />
            </div>
            <div className="rounded-lg border bg-card px-4 py-2 text-sm">
              <span className="text-muted-foreground">Cəmi dəyər: </span>
              <b>{formatCurrency(list?.totalValueAzn ?? 0)}</b>
              <span className="ml-3 text-muted-foreground">Məhsul: </span>
              <b>{list?.items.length ?? 0}</b>
            </div>
          </div>

          <div className="overflow-x-auto rounded-lg border bg-card">
            <table className="w-full text-sm">
              <thead>
                <tr className="bg-muted/40 text-left text-xs text-muted-foreground">
                  <th className={th}>Məhsul</th>
                  <th className={th}>Kateqoriya</th>
                  <th className={cn(th, "text-right")}>Stok</th>
                  <th className={cn(th, "text-right")}>Orta maya</th>
                  <th className={cn(th, "text-right")}>Dəyər</th>
                  <th className={th} />
                </tr>
              </thead>
              <tbody>
                {loading ? (
                  <tr>
                    <td colSpan={6} className="px-3 py-8 text-center text-muted-foreground">
                      Yüklənir…
                    </td>
                  </tr>
                ) : items.length === 0 ? (
                  <tr>
                    <td colSpan={6} className="px-3 py-8 text-center text-muted-foreground">
                      Ləvazimat tapılmadı. Stok məhsulunu “Qeyri-ərzaq” növü ilə yaradın.
                    </td>
                  </tr>
                ) : (
                  items.map((i) => {
                    const expanded = open.has(i.stockItemId);
                    return (
                      <Fragment key={i.stockItemId}>
                        <tr className="border-t hover:bg-muted/30">
                          <td className="px-3 py-2 font-medium">
                            <button type="button" className="flex items-center gap-1.5" onClick={() => toggle(i.stockItemId)}>
                              {expanded ? <ChevronDown className="h-4 w-4" /> : <ChevronRight className="h-4 w-4" />}
                              {i.name}
                              {i.barcode && <span className="text-xs font-normal text-muted-foreground">{i.barcode}</span>}
                            </button>
                          </td>
                          <td className="px-3 py-2 text-muted-foreground">{i.categoryName}</td>
                          <td className={cn("px-3 py-2 text-right font-semibold", i.totalQuantity <= 0 && "text-red-600")}>
                            {qty(i.totalQuantity)} {UNIT_SHORT[i.unit]}
                          </td>
                          <td className="px-3 py-2 text-right">{i.averageCostAzn == null ? "—" : formatCurrency(i.averageCostAzn)}</td>
                          <td className="px-3 py-2 text-right">{formatCurrency(i.valueAzn)}</td>
                          <td className="px-3 py-2 text-right whitespace-nowrap">
                            <Button size="sm" variant="outline" onClick={() => startOperation(i, "increase")}>
                              <Plus className="mr-1 h-3.5 w-3.5" />
                              Artım
                            </Button>
                            <Button size="sm" variant="outline" className="ml-1" disabled={i.totalQuantity <= 0} onClick={() => startOperation(i, "broken")}>
                              <Minus className="mr-1 h-3.5 w-3.5" />
                              Azalma
                            </Button>
                          </td>
                        </tr>
                        {expanded && (
                          <tr className="bg-muted/10 text-xs">
                            <td colSpan={6} className="px-9 py-2">
                              {i.warehouses.length === 0 ? (
                                <span className="text-muted-foreground">Heç bir anbarda stok yoxdur.</span>
                              ) : (
                                <div className="flex flex-wrap gap-x-6 gap-y-1">
                                  {i.warehouses.map((w) => (
                                    <span key={w.warehouseId}>
                                      <span className="text-muted-foreground">{w.warehouseName}: </span>
                                      <b>
                                        {qty(w.quantity)} {UNIT_SHORT[i.unit]}
                                      </b>
                                    </span>
                                  ))}
                                </div>
                              )}
                              <Button
                                size="sm"
                                variant="link"
                                className="h-auto p-0 text-xs"
                                onClick={() => {
                                  setItemFilter(i.stockItemId);
                                  setTab("history");
                                }}
                              >
                                Bu məhsulun əməliyyatlarına bax
                              </Button>
                            </td>
                          </tr>
                        )}
                      </Fragment>
                    );
                  })
                )}
              </tbody>
            </table>
          </div>
        </div>
      ) : (
        <div className="space-y-3">
          <div className="flex flex-wrap items-end gap-3 rounded-lg border bg-card p-3">
            <div className="space-y-1">
              <Label htmlFor="sh-from">Başlanğıc</Label>
              <Input id="sh-from" type="date" value={from} max={to} onChange={(e) => setFrom(e.target.value)} />
            </div>
            <div className="space-y-1">
              <Label htmlFor="sh-to">Son</Label>
              <Input id="sh-to" type="date" value={to} min={from} onChange={(e) => setTo(e.target.value)} />
            </div>
            <div className="space-y-1">
              <Label htmlFor="sh-item">Məhsul</Label>
              <select id="sh-item" className={cn(SELECT_CLASS, "w-56")} value={itemFilter} onChange={(e) => setItemFilter(e.target.value ? Number(e.target.value) : "")}>
                <option value="">Hamısı</option>
                {(list?.items ?? []).map((i) => (
                  <option key={i.stockItemId} value={i.stockItemId}>
                    {i.name}
                  </option>
                ))}
              </select>
            </div>
            <div className="space-y-1">
              <Label htmlFor="sh-op">Əməliyyat</Label>
              <select id="sh-op" className={cn(SELECT_CLASS, "w-48")} value={opFilter} onChange={(e) => setOpFilter(e.target.value)}>
                <option value="">Hamısı</option>
                {operations.map((o) => (
                  <option key={o} value={o}>
                    {o}
                  </option>
                ))}
              </select>
            </div>
          </div>

          <div className="overflow-x-auto rounded-lg border bg-card">
            <table className="w-full text-sm">
              <thead>
                <tr className="bg-muted/40 text-left text-xs text-muted-foreground">
                  <th className={th}>Tarix</th>
                  <th className={th}>Məhsul</th>
                  <th className={th}>Anbar</th>
                  <th className={th}>Əməliyyat</th>
                  <th className={cn(th, "text-right")}>Say</th>
                  <th className={th}>İstifadəçi</th>
                  <th className={th}>Qeyd</th>
                </tr>
              </thead>
              <tbody>
                {historyLoading ? (
                  <tr>
                    <td colSpan={7} className="px-3 py-8 text-center text-muted-foreground">
                      Yüklənir…
                    </td>
                  </tr>
                ) : shownHistory.length === 0 ? (
                  <tr>
                    <td colSpan={7} className="px-3 py-8 text-center text-muted-foreground">
                      Bu dövrdə əməliyyat yoxdur.
                    </td>
                  </tr>
                ) : (
                  shownHistory.map((h) => (
                    <tr key={h.id} className="border-t">
                      <td className="px-3 py-2 whitespace-nowrap">{dateTime(h.date)}</td>
                      <td className="px-3 py-2 font-medium">{h.stockItemName}</td>
                      <td className="px-3 py-2 text-muted-foreground">{h.warehouseName}</td>
                      <td className="px-3 py-2">
                        <Badge variant={h.isIncrease ? "default" : "secondary"} className={cn(!h.isIncrease && "bg-red-100 text-red-700 dark:bg-red-950 dark:text-red-300")}>
                          {h.operation}
                        </Badge>
                        {h.documentNo && <span className="ml-1 font-mono text-xs text-muted-foreground">{h.documentNo}</span>}
                      </td>
                      <td className={cn("px-3 py-2 text-right font-semibold", h.isIncrease ? "text-emerald-600" : "text-red-600")}>
                        {h.isIncrease ? "+" : "−"}
                        {qty(h.quantity)}
                      </td>
                      <td className="px-3 py-2 text-muted-foreground">{h.userName ?? "—"}</td>
                      <td className="px-3 py-2 text-xs text-muted-foreground">{h.note ?? ""}</td>
                    </tr>
                  ))
                )}
              </tbody>
            </table>
          </div>
        </div>
      )}

      <Dialog open={target != null} onOpenChange={(o) => !o && !saving && setTarget(null)}>
        <DialogContent className="sm:max-w-md">
          <DialogHeader>
            <DialogTitle>{target?.name}</DialogTitle>
            <DialogDescription>{op.hint}</DialogDescription>
          </DialogHeader>
          <div className="space-y-3">
            <div className="flex flex-wrap gap-1.5">
              {OPERATIONS.map((o) => (
                <button
                  key={o.kind}
                  type="button"
                  onClick={() => {
                    setKind(o.kind);
                    // moving from an increase to a decrease: pick a warehouse that has the item
                    if (!o.increase && target && !target.warehouses.some((w) => w.warehouseId === warehouseId)) {
                      setWarehouseId(target.warehouses[0]?.warehouseId ?? "");
                    }
                  }}
                  className={cn("rounded-full border px-3 py-1 text-xs", kind === o.kind ? "border-primary bg-primary text-primary-foreground" : "hover:bg-muted")}
                >
                  {o.label}
                </button>
              ))}
            </div>
            <div className="space-y-1">
              <Label htmlFor="op-wh">Anbar</Label>
              <select id="op-wh" className={SELECT_CLASS} value={warehouseId} onChange={(e) => setWarehouseId(e.target.value ? Number(e.target.value) : "")}>
                <option value="">Seçin</option>
                {(op.increase ? warehouses.map((w) => ({ id: w.id, name: w.name })) : (target?.warehouses ?? []).map((w) => ({ id: w.warehouseId, name: `${w.warehouseName} (${qty(w.quantity)})` }))).map((w) => (
                  <option key={w.id} value={w.id}>
                    {w.name}
                  </option>
                ))}
              </select>
            </div>
            <div className="space-y-1">
              <Label htmlFor="op-qty">Miqdar {target ? `(${UNIT_SHORT[target.unit]})` : ""}</Label>
              <Input id="op-qty" inputMode="decimal" value={quantity} onChange={(e) => setQuantity(e.target.value)} />
            </div>
            <div className="space-y-1">
              <Label htmlFor="op-note">Qeyd</Label>
              <Input id="op-note" value={note} onChange={(e) => setNote(e.target.value)} placeholder="Məsələn: 3 stəkan sındı" />
            </div>
          </div>
          <DialogFooter>
            <Button variant="outline" disabled={saving} onClick={() => setTarget(null)}>
              Ləğv et
            </Button>
            <Button disabled={saving} onClick={() => void submit()}>
              {op.label} — yaz
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}
