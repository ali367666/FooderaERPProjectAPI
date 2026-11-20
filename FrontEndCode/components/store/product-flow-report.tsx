"use client";

import { Fragment, useCallback, useEffect, useMemo, useState } from "react";
import { ChevronDown, ChevronRight, Download, RotateCcw } from "lucide-react";
import { toast } from "sonner";
import { TimeRangeInputs } from "@/components/store/time-range-inputs";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { formatCurrency } from "@/lib/format-currency";
import { toDateInputValue, withTime } from "@/lib/report-period";
import { downloadCsv } from "@/lib/store/csv";
import { cn } from "@/lib/utils";
import {
  getStoreCategories,
  getStoreProductFlow,
  getStoreProductFlowLines,
  type StoreCategory,
  type StoreFlowFigure,
  type StoreProductFlow,
  type StoreProductFlowLine,
  type StoreProductFlowRow,
} from "@/lib/store/store-service";
import { getWarehouses, type Warehouse } from "@/lib/services/warehouse-service";

const BUCKETS: { key: string; label: string; incoming: boolean }[] = [
  { key: "purchase", label: "Alış", incoming: true },
  { key: "saleReturn", label: "Alıcı qaytarması", incoming: true },
  { key: "transferIn", label: "Transfer (daxil)", incoming: true },
  { key: "adjustUp", label: "Artım", incoming: true },
  { key: "sale", label: "Satış", incoming: false },
  { key: "purchaseReturn", label: "Təchizatçıya qaytarma", incoming: false },
  { key: "transferOut", label: "Transfer (çıxış)", incoming: false },
  { key: "adjustDown", label: "Azalma", incoming: false },
  { key: "waste", label: "Silinmə", incoming: false },
  { key: "internalUse", label: "Daxili istifadə", incoming: false },
];
const BUCKET_LABEL = Object.fromEntries(BUCKETS.map((b) => [b.key, b.label]));

type Measure = "qty" | "value";
type GroupKey = "warehouse-product" | "category-product" | "product" | "warehouse" | "category";

const GROUPS: { key: GroupKey; label: string; hint: string }[] = [
  { key: "warehouse-product", label: "Anbar → məhsul", hint: "Hər anbarın məhsulları" },
  { key: "category-product", label: "Kateqoriya → məhsul", hint: "Hər kateqoriyanın məhsulları" },
  { key: "product", label: "Məhsul (anbarlar cəmi)", hint: "Bütün anbarlar birləşdirilir" },
  { key: "warehouse", label: "Yalnız anbar", hint: "Anbar üzrə yekun" },
  { key: "category", label: "Yalnız kateqoriya", hint: "Kateqoriya üzrə yekun" },
];

type ViewState = {
  measure: Measure;
  group: GroupKey;
  detailed: boolean;
  hidden: string[]; // hidden movement kinds
  onlyMoved: boolean;
  onlyNegative: boolean;
  onlyStock: boolean;
};

const DEFAULT_VIEW: ViewState = {
  measure: "qty",
  group: "warehouse-product",
  detailed: false,
  hidden: [],
  onlyMoved: false,
  onlyNegative: false,
  onlyStock: false,
};

const PRESETS: { label: string; apply: Partial<ViewState> }[] = [
  { label: "Tam görünüş", apply: { ...DEFAULT_VIEW } },
  { label: "Yalnız satılanlar", apply: { detailed: true, hidden: BUCKETS.filter((b) => b.key !== "sale").map((b) => b.key), onlyMoved: true } },
  { label: "İtki və silinmə", apply: { detailed: true, hidden: BUCKETS.filter((b) => !["waste", "adjustDown", "internalUse"].includes(b.key)).map((b) => b.key), onlyMoved: true } },
  { label: "Dəyər yekunu", apply: { measure: "value", group: "category", detailed: false } },
  { label: "Mənfi qalıqlar", apply: { onlyNegative: true } },
];

const th = "px-3 py-2 font-medium";
const r3 = (n: number) => Math.round(n * 1000) / 1000;
const num = (n: number) => String(r3(n));
const dateTime = (iso: string) => {
  const d = new Date(iso);
  return `${d.toLocaleDateString("az-AZ")} ${d.toLocaleTimeString("az-AZ", { hour: "2-digit", minute: "2-digit" })}`;
};

type Agg = { opening: StoreFlowFigure; moves: Record<string, StoreFlowFigure>; closing: StoreFlowFigure };
const emptyAgg = (): Agg => ({
  opening: { qty: 0, value: 0 },
  moves: Object.fromEntries(BUCKETS.map((b) => [b.key, { qty: 0, value: 0 }])),
  closing: { qty: 0, value: 0 },
});
const addTo = (acc: Agg, r: Pick<StoreProductFlowRow, "opening" | "moves" | "closing">) => {
  acc.opening.qty += r.opening.qty;
  acc.opening.value += r.opening.value;
  acc.closing.qty += r.closing.qty;
  acc.closing.value += r.closing.value;
  for (const b of BUCKETS) {
    acc.moves[b.key].qty += r.moves[b.key]?.qty ?? 0;
    acc.moves[b.key].value += r.moves[b.key]?.value ?? 0;
  }
  return acc;
};

type Row = StoreProductFlowRow & { key: string; label: string };
type Group = { key: string; label: string; rows: Row[]; agg: Agg };

/**
 * The goods flow (mal axını): per product the stock at the start, what each kind of movement did and the stock at the end.
 * Movement kinds are switched on and off with the chips, the grouping with the pills, conditions narrow the rows; a row opens into its movement lines.
 */
export function StoreProductFlowReport() {
  const today = toDateInputValue(new Date());
  const monthStart = toDateInputValue(new Date(new Date().getFullYear(), new Date().getMonth(), 1));
  const storageKey = "store-product-flow-view";

  const [from, setFrom] = useState(monthStart);
  const [to, setTo] = useState(today);
  const [fromTime, setFromTime] = useState("");
  const [toTime, setToTime] = useState("");
  const [warehouseId, setWarehouseId] = useState<number | "">("");
  const [categoryId, setCategoryId] = useState<number | "">("");
  const [term, setTerm] = useState("");
  const [warehouses, setWarehouses] = useState<Warehouse[]>([]);
  const [categories, setCategories] = useState<StoreCategory[]>([]);
  const [data, setData] = useState<StoreProductFlow | null>(null);
  const [loading, setLoading] = useState(true);
  const [view, setView] = useState<ViewState>(DEFAULT_VIEW);
  const [open, setOpen] = useState<Set<string>>(new Set());
  const [lines, setLines] = useState<Record<string, StoreProductFlowLine[] | "loading">>({});

  useEffect(() => {
    try {
      const saved = localStorage.getItem(storageKey);
      if (saved) setView({ ...DEFAULT_VIEW, ...(JSON.parse(saved) as Partial<ViewState>) });
    } catch {
      // no saved view
    }
    getWarehouses().then(setWarehouses).catch(() => setWarehouses([]));
    getStoreCategories().then(setCategories).catch(() => setCategories([]));
  }, []);

  const patchView = (patch: Partial<ViewState>) =>
    setView((prev) => {
      const next = { ...prev, ...patch };
      try {
        localStorage.setItem(storageKey, JSON.stringify(next));
      } catch {
        // storage unavailable
      }
      return next;
    });

  const fromQ = withTime(from, fromTime);
  const toQ = withTime(to, toTime);

  const load = useCallback(async () => {
    setLoading(true);
    try {
      setData(await getStoreProductFlow({ from: fromQ, to: toQ, warehouseId: warehouseId === "" ? null : warehouseId, categoryId: categoryId === "" ? null : categoryId }));
      setOpen(new Set());
      setLines({});
    } catch (e) {
      toast.error(e instanceof Error ? e.message : "Mal axını yüklənmədi.");
      setData(null);
    } finally {
      setLoading(false);
    }
  }, [fromQ, toQ, warehouseId, categoryId]);

  useEffect(() => {
    void load();
  }, [load]);

  const visible = useMemo(() => BUCKETS.filter((b) => !view.hidden.includes(b.key)), [view.hidden]);
  const val = (f: StoreFlowFigure) => (view.measure === "qty" ? f.qty : f.value);
  const show = (n: number) => (view.measure === "qty" ? num(n) : formatCurrency(n));

  // ---- rows after the conditions ----------------------------------------------------------
  const rows = useMemo<Row[]>(() => {
    const q = term.trim().toLowerCase();
    let list = (data?.rows ?? []).filter((r) => !q || r.productName.toLowerCase().includes(q) || (r.barcode ?? "").includes(q));
    if (view.onlyMoved) list = list.filter((r) => BUCKETS.some((b) => visible.includes(b) && Math.abs(r.moves[b.key]?.qty ?? 0) > 0));
    if (view.onlyNegative) list = list.filter((r) => r.closing.qty < 0);
    if (view.onlyStock) list = list.filter((r) => r.closing.qty !== 0 || r.opening.qty !== 0);

    if (view.group === "product") {
      const byProduct = new Map<number, Row>();
      for (const r of list) {
        const existing = byProduct.get(r.productId);
        if (!existing) {
          byProduct.set(r.productId, { ...r, moves: Object.fromEntries(Object.entries(r.moves).map(([k, v]) => [k, { ...v }])), opening: { ...r.opening }, closing: { ...r.closing }, key: `p${r.productId}`, label: r.productName });
        } else {
          addTo(existing as unknown as Agg, r);
        }
      }
      return [...byProduct.values()];
    }
    return list.map((r) => ({ ...r, key: `${r.warehouseId}-${r.productId}`, label: r.productName }));
  }, [data, term, view, visible]);

  const groups = useMemo<Group[]>(() => {
    const keyOf = (r: Row) =>
      view.group === "warehouse-product" || view.group === "warehouse"
        ? [String(r.warehouseId), r.warehouseName]
        : view.group === "category-product" || view.group === "category"
          ? [String(r.categoryId ?? 0), r.categoryName ?? "Kateqoriyasız"]
          : ["all", "Bütün anbarlar"];
    const map = new Map<string, Group>();
    for (const r of rows) {
      const [k, label] = keyOf(r);
      const g = map.get(k) ?? { key: k, label, rows: [], agg: emptyAgg() };
      g.rows.push(r);
      addTo(g.agg, r);
      map.set(k, g);
    }
    return [...map.values()].sort((a, b) => a.label.localeCompare(b.label, "az"));
  }, [rows, view.group]);

  const grand = useMemo(() => rows.reduce((acc, r) => addTo(acc, r), emptyAgg()), [rows]);

  // ---- the chain of figures on top ---------------------------------------------------------
  const incomingTotal = (a: Agg) => BUCKETS.filter((b) => b.incoming && visible.includes(b)).reduce((s, b) => s + val(a.moves[b.key]), 0);
  const outgoingTotal = (a: Agg) => BUCKETS.filter((b) => !b.incoming && visible.includes(b)).reduce((s, b) => s + val(a.moves[b.key]), 0);

  const toggleBucket = (key: string) =>
    patchView({ hidden: view.hidden.includes(key) ? view.hidden.filter((k) => k !== key) : [...view.hidden, key] });

  const toggleRow = async (r: Row) => {
    const key = r.key;
    setOpen((prev) => {
      const next = new Set(prev);
      if (next.has(key)) next.delete(key);
      else next.add(key);
      return next;
    });
    if (lines[key] || open.has(key)) return;
    setLines((prev) => ({ ...prev, [key]: "loading" }));
    try {
      const wh = view.group === "product" ? (warehouseId === "" ? null : warehouseId) : r.warehouseId;
      setLines((prev) => ({ ...prev, [key]: [] }));
      const result = await getStoreProductFlowLines(r.productId, { from: fromQ, to: toQ, warehouseId: wh });
      setLines((prev) => ({ ...prev, [key]: result }));
    } catch (e) {
      toast.error(e instanceof Error ? e.message : "Hərəkətlər yüklənmədi.");
      setLines((prev) => ({ ...prev, [key]: [] }));
    }
  };

  const resetAll = () => {
    patchView({ ...DEFAULT_VIEW });
    setTerm("");
    setWarehouseId("");
    setCategoryId("");
  };

  const exportCsv = () => {
    const head = ["Anbar", "Məhsul", "Barkod", "Başlanğıc", ...visible.map((b) => b.label), "Son qalıq"];
    downloadCsv(`mal-axini-${from}-${to}.csv`, [
      head,
      ...rows.map((r) => [r.warehouseName, r.productName, r.barcode ?? "", val(r.opening), ...visible.map((b) => val(r.moves[b.key] ?? { qty: 0, value: 0 })), val(r.closing)]),
    ]);
  };

  const flowBar = (r: Pick<Agg, "moves">) => {
    const inn = BUCKETS.filter((b) => b.incoming).reduce((s, b) => s + Math.abs(r.moves[b.key].qty), 0);
    const out = BUCKETS.filter((b) => !b.incoming).reduce((s, b) => s + Math.abs(r.moves[b.key].qty), 0);
    const total = inn + out;
    if (total === 0) return <span className="text-muted-foreground">—</span>;
    return (
      <span className="flex h-1.5 w-24 overflow-hidden rounded-full bg-muted" title={`Giriş ${num(inn)} · Çıxış ${num(out)}`}>
        <span className="bg-emerald-500" style={{ width: `${(inn / total) * 100}%` }} />
        <span className="bg-red-400" style={{ width: `${(out / total) * 100}%` }} />
      </span>
    );
  };

  const Move = ({ f, incoming }: { f: StoreFlowFigure; incoming: boolean }) => {
    const v = val(f);
    if (v === 0) return <span className="text-muted-foreground">—</span>;
    return <span className={incoming ? "text-emerald-600" : "text-red-600"}>{incoming ? "+" : "−"}{show(Math.abs(v))}</span>;
  };

  const colCount = 3 + (view.detailed ? visible.length : 2);

  const renderRowCells = (a: Pick<Agg, "opening" | "moves" | "closing">, bold = false) => (
    <>
      <td className={cn("px-3 py-2 text-right", bold && "font-semibold")}>{show(val(a.opening))}</td>
      {view.detailed ? (
        visible.map((b) => (
          <td key={b.key} className={cn("px-3 py-2 text-right", bold && "font-semibold")}>
            <Move f={a.moves[b.key] ?? { qty: 0, value: 0 }} incoming={b.incoming} />
          </td>
        ))
      ) : (
        <>
          <td className={cn("px-3 py-2 text-right", bold && "font-semibold")} title={visible.filter((b) => b.incoming).map((b) => `${b.label}: ${show(val(a.moves[b.key]))}`).join("\n")}>
            <Move f={{ qty: incomingTotal(a as Agg), value: incomingTotal(a as Agg) }} incoming />
          </td>
          <td className={cn("px-3 py-2 text-right", bold && "font-semibold")} title={visible.filter((b) => !b.incoming).map((b) => `${b.label}: ${show(-val(a.moves[b.key]))}`).join("\n")}>
            <Move f={{ qty: -outgoingTotal(a as Agg), value: -outgoingTotal(a as Agg) }} incoming={false} />
          </td>
        </>
      )}
      <td className={cn("px-3 py-2 text-right font-semibold", val(a.closing) < 0 && "text-red-600")}>{show(val(a.closing))}</td>
      <td className="px-3 py-2">{flowBar(a)}</td>
    </>
  );

  return (
    <div className="space-y-4 p-4">
      <div className="flex flex-wrap items-start justify-between gap-2">
        <div>
          <h1 className="text-2xl font-bold">Mal axını</h1>
          <p className="text-sm text-muted-foreground">
            Hər məhsulun dövrün əvvəlindəki qalığı, nə qədər daxil olub-çıxdığı və son qalığı. Hərəkət növlərini düymələrlə seçin, sətrə basanda hərəkət sənədləri açılır.
          </p>
        </div>
        <div className="flex gap-2">
          <Button variant="ghost" size="sm" onClick={resetAll}>
            <RotateCcw className="mr-1 h-4 w-4" />
            Sıfırla
          </Button>
          <Button variant="outline" size="sm" disabled={rows.length === 0} onClick={exportCsv}>
            <Download className="mr-1 h-4 w-4" />
            Excel (CSV)
          </Button>
        </div>
      </div>

      <div className="flex flex-wrap items-end gap-3 rounded-lg border bg-card p-3">
        <div className="space-y-1">
          <Label htmlFor="pf-from">Başlanğıc</Label>
          <Input id="pf-from" type="date" value={from} max={to} onChange={(e) => setFrom(e.target.value)} />
        </div>
        <div className="space-y-1">
          <Label htmlFor="pf-to">Son</Label>
          <Input id="pf-to" type="date" value={to} min={from} onChange={(e) => setTo(e.target.value)} />
        </div>
        <TimeRangeInputs idPrefix="pf" fromTime={fromTime} toTime={toTime} onFromTime={setFromTime} onToTime={setToTime} />
        <div className="space-y-1">
          <Label htmlFor="pf-wh">Anbar</Label>
          <select id="pf-wh" className="h-9 w-44 rounded-md border border-input bg-background px-3 text-sm" value={warehouseId} onChange={(e) => setWarehouseId(e.target.value ? Number(e.target.value) : "")}>
            <option value="">Hamısı</option>
            {warehouses.map((w) => (
              <option key={w.id} value={w.id}>
                {w.name}
              </option>
            ))}
          </select>
        </div>
        <div className="space-y-1">
          <Label htmlFor="pf-cat">Kateqoriya</Label>
          <select id="pf-cat" className="h-9 w-44 rounded-md border border-input bg-background px-3 text-sm" value={categoryId} onChange={(e) => setCategoryId(e.target.value ? Number(e.target.value) : "")}>
            <option value="">Hamısı</option>
            {categories.map((c) => (
              <option key={c.id} value={c.id}>
                {c.name}
              </option>
            ))}
          </select>
        </div>
        <div className="space-y-1">
          <Label htmlFor="pf-q">Məhsul</Label>
          <Input id="pf-q" className="w-52" placeholder="Ad və ya barkod…" value={term} onChange={(e) => setTerm(e.target.value)} />
        </div>
      </div>

      {/* the chain: opening, what came in, what went out, closing */}
      <div className="grid grid-cols-2 gap-2 lg:grid-cols-4">
        {[
          { label: "Başlanğıc qalıq", value: val(grand.opening), tone: "" },
          { label: "Daxil olan", value: incomingTotal(grand), tone: "text-emerald-600", sign: "+" },
          { label: "Çıxan", value: -outgoingTotal(grand), tone: "text-red-600", sign: "−" },
          { label: "Son qalıq", value: val(grand.closing), tone: "" },
        ].map((c) => (
          <div key={c.label} className="rounded-xl border p-3">
            <p className="text-xs text-muted-foreground">{c.label}</p>
            <p className={cn("text-lg font-semibold", c.tone)}>
              {c.sign}
              {show(Math.abs(c.value))}
            </p>
          </div>
        ))}
      </div>

      <div className="space-y-3 rounded-lg border bg-card p-3">
        <div className="flex flex-wrap items-center gap-1.5">
          <span className="mr-1 text-xs font-medium text-muted-foreground">Hazır görünüş:</span>
          {PRESETS.map((p) => (
            <button key={p.label} type="button" onClick={() => patchView({ ...DEFAULT_VIEW, ...p.apply })} className="rounded-full border px-3 py-1 text-xs hover:bg-muted">
              {p.label}
            </button>
          ))}
        </div>

        <div className="flex flex-wrap items-center gap-1.5">
          <span className="mr-1 text-xs font-medium text-muted-foreground">Göstər:</span>
          {(
            [
              ["qty", "Say"],
              ["value", "Dəyər (maya)"],
            ] as const
          ).map(([k, label]) => (
            <button key={k} type="button" onClick={() => patchView({ measure: k })} className={cn("rounded-full border px-3 py-1 text-xs", view.measure === k ? "border-primary bg-primary text-primary-foreground" : "hover:bg-muted")}>
              {label}
            </button>
          ))}
          <span className="mx-2 text-muted-foreground">|</span>
          <span className="mr-1 text-xs font-medium text-muted-foreground">Qruplaşdır:</span>
          {GROUPS.map((g) => (
            <button key={g.key} type="button" title={g.hint} onClick={() => patchView({ group: g.key })} className={cn("rounded-full border px-3 py-1 text-xs", view.group === g.key ? "border-primary bg-primary text-primary-foreground" : "hover:bg-muted")}>
              {g.label}
            </button>
          ))}
        </div>

        <div className="flex flex-wrap items-center gap-1.5">
          <span className="mr-1 text-xs font-medium text-muted-foreground">Hərəkət növü:</span>
          {BUCKETS.map((b) => {
            const on = !view.hidden.includes(b.key);
            return (
              <button
                key={b.key}
                type="button"
                onClick={() => toggleBucket(b.key)}
                className={cn(
                  "rounded-full border px-2.5 py-0.5 text-xs",
                  on ? (b.incoming ? "border-emerald-500 bg-emerald-500/10 text-emerald-700 dark:text-emerald-300" : "border-red-400 bg-red-500/10 text-red-700 dark:text-red-300") : "border-dashed text-muted-foreground opacity-60",
                )}
              >
                {b.label}
              </button>
            );
          })}
          <label className="ml-2 flex items-center gap-1.5 text-xs">
            <input type="checkbox" checked={view.detailed} onChange={(e) => patchView({ detailed: e.target.checked })} />
            Hər növ ayrı sütunda
          </label>
        </div>

        <div className="flex flex-wrap items-center gap-4 border-t pt-3 text-sm">
          <span className="text-xs font-medium text-muted-foreground">Şərtlər:</span>
          <label className="flex items-center gap-1.5">
            <input type="checkbox" checked={view.onlyMoved} onChange={(e) => patchView({ onlyMoved: e.target.checked })} />
            Yalnız hərəkəti olanlar
          </label>
          <label className="flex items-center gap-1.5">
            <input type="checkbox" checked={view.onlyStock} onChange={(e) => patchView({ onlyStock: e.target.checked })} />
            Qalığı olanlar
          </label>
          <label className="flex items-center gap-1.5">
            <input type="checkbox" checked={view.onlyNegative} onChange={(e) => patchView({ onlyNegative: e.target.checked })} />
            Mənfi qalıq
          </label>
        </div>
      </div>

      <div className="text-xs text-muted-foreground">{loading ? "Yüklənir…" : `${rows.length} sətir, ${groups.length} qrup`}</div>

      <div className="overflow-x-auto rounded-lg border bg-card">
        <table className="w-full text-sm">
          <thead>
            <tr className="bg-muted/40 text-left text-xs text-muted-foreground">
              <th className={th}>{view.group.endsWith("product") ? "Məhsul" : view.group === "warehouse" ? "Anbar" : "Kateqoriya"}</th>
              <th className={cn(th, "text-right")}>Başlanğıc</th>
              {view.detailed ? (
                visible.map((b) => (
                  <th key={b.key} className={cn(th, "text-right")}>
                    {b.label}
                  </th>
                ))
              ) : (
                <>
                  <th className={cn(th, "text-right")}>Daxil olan</th>
                  <th className={cn(th, "text-right")}>Çıxan</th>
                </>
              )}
              <th className={cn(th, "text-right")}>Son qalıq</th>
              <th className={th}>Axın</th>
            </tr>
          </thead>
          <tbody>
            {loading ? (
              <tr>
                <td colSpan={colCount + 1} className="px-3 py-8 text-center text-muted-foreground">
                  Yüklənir…
                </td>
              </tr>
            ) : groups.length === 0 ? (
              <tr>
                <td colSpan={colCount + 1} className="px-3 py-8 text-center text-muted-foreground">
                  Bu şərtlərlə hərəkət tapılmadı.
                </td>
              </tr>
            ) : (
              <>
                <tr className="border-t bg-muted/30 font-semibold">
                  <td className="px-3 py-2">Cəmi</td>
                  {renderRowCells(grand, true)}
                </tr>
                {groups.map((g) => (
                  <Fragment key={g.key}>
                    {(view.group === "warehouse-product" || view.group === "category-product" || view.group === "warehouse" || view.group === "category") && (
                      <tr className="border-t bg-muted/15">
                        <td className="px-3 py-2 font-semibold">{g.label}</td>
                        {renderRowCells(g.agg, true)}
                      </tr>
                    )}
                    {(view.group === "warehouse" || view.group === "category") ? null : g.rows.map((r) => {
                      const expanded = open.has(r.key);
                      const l = lines[r.key];
                      return (
                        <Fragment key={r.key}>
                          <tr className="cursor-pointer border-t hover:bg-muted/40" onClick={() => void toggleRow(r)}>
                            <td className="px-3 py-2">
                              <span className="flex items-center gap-1.5 pl-3">
                                {expanded ? <ChevronDown className="h-4 w-4" /> : <ChevronRight className="h-4 w-4" />}
                                {r.productName}
                                {r.barcode && <span className="text-xs text-muted-foreground">{r.barcode}</span>}
                              </span>
                            </td>
                            {renderRowCells(r)}
                          </tr>
                          {expanded && (
                            <tr className="bg-muted/10 text-xs">
                              <td colSpan={colCount + 1} className="px-9 py-2">
                                {l === "loading" || l === undefined ? (
                                  <span className="text-muted-foreground">Yüklənir…</span>
                                ) : l.length === 0 ? (
                                  <span className="text-muted-foreground">Bu dövrdə hərəkət yoxdur.</span>
                                ) : (
                                  <table className="w-full">
                                    <thead>
                                      <tr className="text-left text-muted-foreground">
                                        <th className="py-1 font-medium">Tarix</th>
                                        <th className="py-1 font-medium">Hərəkət</th>
                                        <th className="py-1 font-medium">Anbar</th>
                                        <th className="py-1 text-right font-medium">Say</th>
                                        <th className="py-1 text-right font-medium">Dəyər</th>
                                        <th className="py-1 text-right font-medium">Qalıq</th>
                                      </tr>
                                    </thead>
                                    <tbody>
                                      {l.map((x, i) => (
                                        <tr key={i} className="border-t">
                                          <td className="py-1 whitespace-nowrap">{dateTime(x.createdAtUtc)}</td>
                                          <td className="py-1">
                                            {BUCKET_LABEL[x.bucket] ?? x.bucket}
                                            {x.documentNo && <span className="ml-1 font-mono">{x.documentNo}</span>}
                                            {x.reason && <span className="ml-2 text-muted-foreground">· {x.reason}</span>}
                                          </td>
                                          <td className="py-1 text-muted-foreground">{x.warehouseName}</td>
                                          <td className={cn("py-1 text-right", x.quantity >= 0 ? "text-emerald-600" : "text-red-600")}>
                                            {x.quantity >= 0 ? "+" : ""}
                                            {num(x.quantity)}
                                          </td>
                                          <td className="py-1 text-right">{formatCurrency(x.value)}</td>
                                          <td className="py-1 text-right">{num(x.balanceAfter)}</td>
                                        </tr>
                                      ))}
                                    </tbody>
                                  </table>
                                )}
                              </td>
                            </tr>
                          )}
                        </Fragment>
                      );
                    })}
                  </Fragment>
                ))}
              </>
            )}
          </tbody>
        </table>
      </div>
    </div>
  );
}
