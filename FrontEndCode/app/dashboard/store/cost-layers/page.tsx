"use client";

import { Fragment, useCallback, useEffect, useMemo, useState } from "react";
import { ChevronDown, ChevronRight } from "lucide-react";
import { toast } from "sonner";
import { Badge } from "@/components/ui/badge";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { formatCurrency } from "@/lib/format-currency";
import { cn } from "@/lib/utils";
import { getStoreCostLayers, type StoreCostLayer, type StoreCostLayers } from "@/lib/store/store-service";
import { getWarehouses, type Warehouse } from "@/lib/services/warehouse-service";

const METHOD_LABEL: Record<number, string> = { 0: "Orta maya", 1: "FIFO", 2: "LIFO" };
const th = "px-3 py-2 font-medium";
const num = (n: number) => String(Math.round(n * 1000) / 1000);
const dateTime = (iso: string) => {
  const d = new Date(iso);
  return `${d.toLocaleDateString("az-AZ")} ${d.toLocaleTimeString("az-AZ", { hour: "2-digit", minute: "2-digit" })}`;
};

/** Admin: the cost layers (deliveries still held) of the store's warehouse goods; filled while the SuperAdmin has the company on FIFO or LIFO. */
export default function StoreCostLayersPage() {
  const [data, setData] = useState<StoreCostLayers | null>(null);
  const [loading, setLoading] = useState(true);
  const [warehouseId, setWarehouseId] = useState<number | "">("");
  const [warehouses, setWarehouses] = useState<Warehouse[]>([]);
  const [term, setTerm] = useState("");
  const [onlyRemaining, setOnlyRemaining] = useState(true);
  const [open, setOpen] = useState<Set<number>>(new Set());

  useEffect(() => {
    getWarehouses().then(setWarehouses).catch(() => setWarehouses([]));
  }, []);

  const load = useCallback(async () => {
    setLoading(true);
    try {
      setData(await getStoreCostLayers({ warehouseId: warehouseId === "" ? null : warehouseId, onlyRemaining }));
    } catch (e) {
      toast.error(e instanceof Error ? e.message : "Maya qatları yüklənmədi.");
      setData(null);
    } finally {
      setLoading(false);
    }
  }, [warehouseId, onlyRemaining]);

  useEffect(() => {
    void load();
  }, [load]);

  // One row per product and warehouse, its layers underneath in the order they are used up.
  const groups = useMemo(() => {
    const q = term.trim().toLowerCase();
    const map = new Map<string, { key: string; product: string; warehouse: string; layers: StoreCostLayer[] }>();
    for (const l of data?.layers ?? []) {
      if (q && !l.productName.toLowerCase().includes(q)) continue;
      const key = `${l.warehouseId}-${l.productId}`;
      const g = map.get(key) ?? { key, product: l.productName, warehouse: l.warehouseName, layers: [] };
      g.layers.push(l);
      map.set(key, g);
    }
    return [...map.values()];
  }, [data, term]);

  const method = data?.method ?? 0;
  const order = method === 2 ? "en yenidən köhnəyə" : "ən köhnədən yeniyə";
  const toggle = (key: string, idx: number) =>
    setOpen((prev) => {
      const next = new Set(prev);
      if (next.has(idx)) next.delete(idx);
      else next.add(idx);
      void key;
      return next;
    });

  return (
    <div className="space-y-4 p-4">
      <div>
        <h1 className="text-2xl font-bold">Maya qatları</h1>
        <p className="text-sm text-muted-foreground">
          Anbara daxil olan hər alış ayrı qatdır. FIFO-da satış ən köhnə qatdan, LIFO-da ən yeni qatdan düşür və satışın maya dəyəri həmin qatın qiymətidir.
        </p>
      </div>

      <div className={cn("flex flex-wrap items-center justify-between gap-2 rounded-lg border p-3 text-sm", method === 0 ? "bg-muted/40" : "border-emerald-500/50 bg-emerald-500/5")}>
        <span>
          Maya metodu: <Badge variant={method === 0 ? "secondary" : "default"}>{METHOD_LABEL[method] ?? method}</Badge>
          {method === 0 ? (
            <span className="ml-2 text-muted-foreground">FIFO / LIFO bu şirkət üçün açıq deyil, maya orta qiymətlə hesablanır. Açmaq üçün SuperAdmin-ə müraciət edin.</span>
          ) : (
            <span className="ml-2 text-muted-foreground">Qatlar {order} istifadə olunur.</span>
          )}
        </span>
        {method !== 0 && data && (
          <span>
            Qatlardakı mal: <b>{num(data.totalQuantity)}</b> · dəyəri: <b>{formatCurrency(data.totalValue)}</b>
          </span>
        )}
      </div>

      {method !== 0 && (
        <>
          <div className="flex flex-wrap items-end gap-3 rounded-lg border bg-card p-3">
            <div className="space-y-1">
              <Label htmlFor="cl-wh">Anbar</Label>
              <select id="cl-wh" className="h-9 w-48 rounded-md border border-input bg-background px-3 text-sm" value={warehouseId} onChange={(e) => setWarehouseId(e.target.value ? Number(e.target.value) : "")}>
                <option value="">Hamısı</option>
                {warehouses.map((w) => (
                  <option key={w.id} value={w.id}>
                    {w.name}
                  </option>
                ))}
              </select>
            </div>
            <div className="space-y-1">
              <Label htmlFor="cl-q">Məhsul</Label>
              <Input id="cl-q" className="w-56" placeholder="Ad…" value={term} onChange={(e) => setTerm(e.target.value)} />
            </div>
            <label className="flex h-9 items-center gap-2 text-sm">
              <input type="checkbox" checked={onlyRemaining} onChange={(e) => setOnlyRemaining(e.target.checked)} />
              Yalnız qalığı olan qatlar
            </label>
          </div>

          <div className="overflow-x-auto rounded-lg border bg-card">
            <table className="w-full text-sm">
              <thead>
                <tr className="bg-muted/40 text-left text-xs text-muted-foreground">
                  <th className={th}>Məhsul</th>
                  <th className={th}>Anbar</th>
                  <th className={cn(th, "text-right")}>Qat sayı</th>
                  <th className={cn(th, "text-right")}>Qalıq</th>
                  <th className={cn(th, "text-right")}>Növbəti maya</th>
                  <th className={cn(th, "text-right")}>Dəyər</th>
                </tr>
              </thead>
              <tbody>
                {loading ? (
                  <tr>
                    <td colSpan={6} className="px-3 py-8 text-center text-muted-foreground">
                      Yüklənir…
                    </td>
                  </tr>
                ) : groups.length === 0 ? (
                  <tr>
                    <td colSpan={6} className="px-3 py-8 text-center text-muted-foreground">
                      Maya qatı tapılmadı.
                    </td>
                  </tr>
                ) : (
                  groups.map((g, idx) => {
                    const expanded = open.has(idx);
                    // layers come oldest first; the next one to be used up is the first (FIFO) or the last (LIFO)
                    const next = method === 2 ? g.layers[g.layers.length - 1] : g.layers[0];
                    const qty = g.layers.reduce((s, l) => s + l.remainingQuantity, 0);
                    const value = g.layers.reduce((s, l) => s + l.value, 0);
                    return (
                      <Fragment key={g.key}>
                        <tr className="cursor-pointer border-t hover:bg-muted/40" onClick={() => toggle(g.key, idx)}>
                          <td className="px-3 py-2 font-medium">
                            <span className="flex items-center gap-1.5">
                              {expanded ? <ChevronDown className="h-4 w-4" /> : <ChevronRight className="h-4 w-4" />}
                              {g.product}
                            </span>
                          </td>
                          <td className="px-3 py-2 text-muted-foreground">{g.warehouse}</td>
                          <td className="px-3 py-2 text-right">{g.layers.length}</td>
                          <td className="px-3 py-2 text-right">{num(qty)}</td>
                          <td className="px-3 py-2 text-right">{next ? formatCurrency(next.unitCost) : "—"}</td>
                          <td className="px-3 py-2 text-right font-medium">{formatCurrency(value)}</td>
                        </tr>
                        {expanded &&
                          g.layers.map((l, i) => {
                            const isNext = l === next;
                            return (
                              <tr key={l.id} className={cn("bg-muted/10 text-xs", isNext && "font-medium")}>
                                <td className="py-1.5 pl-9 pr-3">
                                  {l.isOpeningBalance ? "İlkin qalıq" : `${l.documentType ?? "Daxilolma"} ${l.documentNo ?? ""}`}
                                  <span className="ml-2 text-muted-foreground">{l.isOpeningBalance ? "" : dateTime(l.receivedAtUtc)}</span>
                                  {isNext && l.remainingQuantity > 0 && <Badge className="ml-2" variant="outline">növbəti</Badge>}
                                </td>
                                <td />
                                <td className="px-3 py-1.5 text-right text-muted-foreground">
                                  #{i + 1}
                                </td>
                                <td className="px-3 py-1.5 text-right">
                                  {num(l.remainingQuantity)} <span className="text-muted-foreground">/ {num(l.originalQuantity)}</span>
                                </td>
                                <td className="px-3 py-1.5 text-right">{formatCurrency(l.unitCost)}</td>
                                <td className="px-3 py-1.5 text-right">{formatCurrency(l.value)}</td>
                              </tr>
                            );
                          })}
                      </Fragment>
                    );
                  })
                )}
              </tbody>
            </table>
          </div>
        </>
      )}
    </div>
  );
}
