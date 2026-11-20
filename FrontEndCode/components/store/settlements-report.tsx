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
import { getStoreSettlements, type StoreSettlementEntry, type StoreSettlements } from "@/lib/store/store-service";

const ENTRY_LABELS: Record<string, string> = {
  OpeningBalance: "İlkin qalıq",
  Purchase: "Alış",
  PurchaseReturn: "Alış qaytarma",
  SupplierPayment: "Təchizatçıya ödəniş",
  Adjustment: "Düzəliş",
  PurchaseCancel: "Alış ləğvi",
  CreditSale: "Nisyə satış",
  CustomerPayment: "Müştəri ödənişi",
  SaleReturnCredit: "Satış qaytarma (borcdan)",
  PaymentReversal: "Ödənişin ləğvi",
};
const typeLabel = (t: string) => ENTRY_LABELS[t] ?? t;

type FieldKey = "opening" | "increase" | "decrease" | "closing";
type GroupKey = "party" | "partyOnly" | "type" | "day" | "flat";

const FIELDS: { key: FieldKey; label: string; hint: string }[] = [
  { key: "opening", label: "İlkin borc", hint: "Dövrün əvvəlinə qalıq" },
  { key: "increase", label: "Borcun artması", hint: "Dövrdə borcu artıran hərəkətlər" },
  { key: "decrease", label: "Borcun azalması", hint: "Dövrdə borcu azaldan hərəkətlər" },
  { key: "closing", label: "Son borc", hint: "Dövrün sonuna qalıq" },
];

const GROUPS: { key: GroupKey; label: string; hint: string }[] = [
  { key: "party", label: "Kontragent → sənəd", hint: "Hər kontragent açılır, içində sənədlər" },
  { key: "partyOnly", label: "Yalnız kontragent", hint: "Yığcam: sənədsiz" },
  { key: "type", label: "Hərəkət növü", hint: "Alış, ödəniş, qaytarma... üzrə" },
  { key: "day", label: "Gün üzrə", hint: "Hər günün dövriyyəsi" },
  { key: "flat", label: "Düz siyahı", hint: "Bütün hərəkətlər vaxt sırası ilə" },
];

const PRESETS: { label: string; apply: () => Partial<ViewState> }[] = [
  { label: "Tam", apply: () => ({ fields: { opening: true, increase: true, decrease: true, closing: true }, group: "party", onlyDebt: false }) },
  { label: "Yalnız borclular", apply: () => ({ fields: { opening: false, increase: false, decrease: false, closing: true }, group: "partyOnly", onlyDebt: true }) },
  { label: "Dövriyyə", apply: () => ({ fields: { opening: false, increase: true, decrease: true, closing: false }, group: "type", onlyDebt: false }) },
  { label: "Günlük axın", apply: () => ({ fields: { opening: false, increase: true, decrease: true, closing: false }, group: "day", onlyDebt: false }) },
];

type ViewState = {
  fields: Record<FieldKey, boolean>;
  group: GroupKey;
  onlyDebt: boolean;
  minDebt: string;
};

const DEFAULT_VIEW: ViewState = {
  fields: { opening: true, increase: true, decrease: true, closing: true },
  group: "party",
  onlyDebt: false,
  minDebt: "",
};

const th = "px-3 py-2 font-medium";
const dayOf = (iso: string) => new Date(iso).toLocaleDateString("az-AZ");
const dateTime = (iso: string) => {
  const d = new Date(iso);
  return `${d.toLocaleDateString("az-AZ")} ${d.toLocaleTimeString("az-AZ", { hour: "2-digit", minute: "2-digit" })}`;
};
const r2 = (n: number) => Math.round(n * 100) / 100;

type FlatEntry = StoreSettlementEntry & { partyId: number; partyName: string };

/**
 * Settlements with counterparties (qarşılıqlı hesablaşma): opening debt, increase, decrease and closing debt per customer or supplier.
 * The view is built on the page itself - clickable figure cards choose the fields, pills choose the grouping, conditions narrow the rows -
 * and is remembered per browser.
 */
export function StoreSettlementsReport({ partyType }: { partyType: "customers" | "suppliers" }) {
  const isCustomers = partyType === "customers";
  const title = isCustomers ? "Alıcılarla qarşılıqlı hesablaşma" : "Təchizatçılarla qarşılıqlı hesablaşma";
  const partyLabel = isCustomers ? "Alıcı" : "Təchizatçı";
  // A positive debt is the party's natural direction: a customer owes us, we owe a supplier.
  const positiveText = isCustomers ? "bizə borclu" : "biz borcluyuq";
  const negativeText = isCustomers ? "biz borcluyuq" : "bizə borclu";

  const storageKey = `store-settlements-view:${partyType}`;
  const today = toDateInputValue(new Date());
  const monthStart = toDateInputValue(new Date(new Date().getFullYear(), new Date().getMonth(), 1));

  const [from, setFrom] = useState(monthStart);
  const [to, setTo] = useState(today);
  const [fromTime, setFromTime] = useState("");
  const [toTime, setToTime] = useState("");
  const [data, setData] = useState<StoreSettlements | null>(null);
  const [loading, setLoading] = useState(true);
  const [open, setOpen] = useState<Set<string>>(new Set());

  const [view, setView] = useState<ViewState>(DEFAULT_VIEW);
  const [types, setTypes] = useState<Set<string>>(new Set());
  const [term, setTerm] = useState("");
  const [docTerm, setDocTerm] = useState("");

  // The chosen view is kept for the next visit.
  useEffect(() => {
    try {
      const saved = localStorage.getItem(storageKey);
      if (saved) setView({ ...DEFAULT_VIEW, ...(JSON.parse(saved) as Partial<ViewState>) });
    } catch {
      // no saved view
    }
  }, [storageKey]);
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

  const load = useCallback(async () => {
    setLoading(true);
    try {
      setData(await getStoreSettlements({ partyType, from: withTime(from, fromTime), to: withTime(to, toTime) }));
      setOpen(new Set());
    } catch (e) {
      toast.error(e instanceof Error ? e.message : "Hesabat yüklənmədi.");
      setData(null);
    } finally {
      setLoading(false);
    }
  }, [partyType, from, to, fromTime, toTime]);

  useEffect(() => {
    void load();
  }, [load]);

  const allTypes = useMemo(() => [...new Set((data?.rows ?? []).flatMap((r) => r.entries.map((e) => e.typeName)))].sort(), [data]);

  // ---- conditions -----------------------------------------------------------------------
  const rows = useMemo(() => {
    const q = term.trim().toLowerCase();
    const dq = docTerm.trim().toLowerCase();
    const min = Number(view.minDebt.replace(",", ".")) || 0;
    const entriesFiltered = types.size > 0 || dq !== "";

    return (data?.rows ?? [])
      .filter((r) => !q || r.name.toLowerCase().includes(q) || (r.phone ?? "").includes(q))
      .map((r) => {
        const entries = r.entries.filter(
          (e) => (types.size === 0 || types.has(e.typeName)) && (dq === "" || (e.documentNo ?? "").toLowerCase().includes(dq)),
        );
        return {
          ...r,
          entries,
          increase: r2(entries.reduce((s, e) => s + e.increase, 0)),
          decrease: r2(entries.reduce((s, e) => s + e.decrease, 0)),
          hidden: entriesFiltered && entries.length === 0,
        };
      })
      .filter((r) => !r.hidden)
      .filter((r) => !view.onlyDebt || Math.abs(r.closingDebt) >= 0.005)
      .filter((r) => min <= 0 || Math.abs(r.closingDebt) >= min);
  }, [data, term, docTerm, types, view.onlyDebt, view.minDebt]);

  const totals = useMemo(
    () => ({
      opening: r2(rows.reduce((s, r) => s + r.openingDebt, 0)),
      increase: r2(rows.reduce((s, r) => s + r.increase, 0)),
      decrease: r2(rows.reduce((s, r) => s + r.decrease, 0)),
      closing: r2(rows.reduce((s, r) => s + r.closingDebt, 0)),
    }),
    [rows],
  );

  const flat = useMemo<FlatEntry[]>(
    () =>
      rows
        .flatMap((r) => r.entries.map((e) => ({ ...e, partyId: r.partyId, partyName: r.name })))
        .sort((a, b) => b.entryDate.localeCompare(a.entryDate)),
    [rows],
  );

  const groupedBy = useMemo(() => {
    const keyOf = (e: FlatEntry) => (view.group === "type" ? e.typeName : new Date(e.entryDate).toISOString().slice(0, 10));
    const map = new Map<string, FlatEntry[]>();
    for (const e of flat) {
      const k = keyOf(e);
      map.set(k, [...(map.get(k) ?? []), e]);
    }
    return [...map.entries()]
      .map(([key, entries]) => ({
        key,
        label: view.group === "type" ? typeLabel(key) : dayOf(entries[0].entryDate),
        entries,
        increase: r2(entries.reduce((s, e) => s + e.increase, 0)),
        decrease: r2(entries.reduce((s, e) => s + e.decrease, 0)),
      }))
      .sort((a, b) => (view.group === "day" ? b.key.localeCompare(a.key) : b.increase + b.decrease - (a.increase + a.decrease)));
  }, [flat, view.group]);

  const maxAbsClosing = useMemo(() => Math.max(0, ...rows.map((r) => Math.abs(r.closingDebt))), [rows]);
  const filtersActive = view.onlyDebt || view.minDebt.trim() !== "" || types.size > 0 || docTerm.trim() !== "" || term.trim() !== "";

  const toggleOpen = (key: string) =>
    setOpen((prev) => {
      const next = new Set(prev);
      if (next.has(key)) next.delete(key);
      else next.add(key);
      return next;
    });

  const toggleType = (t: string) =>
    setTypes((prev) => {
      const next = new Set(prev);
      if (next.has(t)) next.delete(t);
      else next.add(t);
      return next;
    });

  const resetAll = () => {
    patchView({ ...DEFAULT_VIEW });
    setTypes(new Set());
    setTerm("");
    setDocTerm("");
  };

  /** A signed debt with the direction written out, so no column has to explain it. */
  const Debt = ({ value, bold }: { value: number; bold?: boolean }) =>
    Math.abs(value) < 0.005 ? (
      <span className="text-muted-foreground">—</span>
    ) : (
      <span className="inline-flex flex-col items-end leading-tight">
        <span className={cn(bold && "font-semibold")}>{formatCurrency(Math.abs(value))}</span>
        <span className={cn("text-[10px]", value > 0 ? "text-amber-600" : "text-emerald-600")}>{value > 0 ? positiveText : negativeText}</span>
      </span>
    );

  const money = (v: number) => (v === 0 ? <span className="text-muted-foreground">—</span> : formatCurrency(v));

  const exportCsv = () => {
    if (!data) return;
    if (view.group === "party" || view.group === "partyOnly") {
      const head = [partyLabel, ...FIELDS.filter((f) => view.fields[f.key]).map((f) => f.label)];
      downloadCsv(`${partyType}-hesablasma-${from}-${to}.csv`, [
        head,
        ...rows.map((r) => [
          r.name,
          ...(view.fields.opening ? [r.openingDebt] : []),
          ...(view.fields.increase ? [r.increase] : []),
          ...(view.fields.decrease ? [r.decrease] : []),
          ...(view.fields.closing ? [r.closingDebt] : []),
        ]),
      ]);
    } else if (view.group === "flat") {
      downloadCsv(`${partyType}-hereketler-${from}-${to}.csv`, [
        ["Tarix", partyLabel, "Növ", "Sənəd", "Artma", "Azalma"],
        ...flat.map((e) => [dateTime(e.entryDate), e.partyName, typeLabel(e.typeName), e.documentNo ?? "", e.increase, e.decrease]),
      ]);
    } else {
      downloadCsv(`${partyType}-${view.group}-${from}-${to}.csv`, [
        [view.group === "type" ? "Növ" : "Gün", "Say", "Artma", "Azalma"],
        ...groupedBy.map((g) => [g.label, g.entries.length, g.increase, g.decrease]),
      ]);
    }
  };

  const showParty = view.group === "party" || view.group === "partyOnly";
  const visibleFields = FIELDS.filter((f) => view.fields[f.key]);

  return (
    <div className="space-y-4 p-4">
      <div className="flex flex-wrap items-start justify-between gap-2">
        <div>
          <h1 className="text-2xl font-bold">{title}</h1>
          <p className="text-sm text-muted-foreground">
            Rəqəm kartlarına toxunub göstəriləcək sahələri seçin, qruplaşdırmanı və şərtləri ekranın üstündən verin. Seçiminiz yadda qalır.
          </p>
        </div>
        <div className="flex gap-2">
          <Button variant="ghost" size="sm" onClick={resetAll}>
            <RotateCcw className="mr-1 h-4 w-4" />
            Sıfırla
          </Button>
          <Button variant="outline" size="sm" disabled={!data || rows.length === 0} onClick={exportCsv}>
            <Download className="mr-1 h-4 w-4" />
            Excel (CSV)
          </Button>
        </div>
      </div>

      {/* period */}
      <div className="flex flex-wrap items-end gap-3 rounded-lg border bg-card p-3">
        <div className="space-y-1">
          <Label htmlFor="st-from">Başlanğıc</Label>
          <Input id="st-from" type="date" value={from} max={to} onChange={(e) => setFrom(e.target.value)} />
        </div>
        <div className="space-y-1">
          <Label htmlFor="st-to">Son</Label>
          <Input id="st-to" type="date" value={to} min={from} onChange={(e) => setTo(e.target.value)} />
        </div>
        <TimeRangeInputs idPrefix="st" fromTime={fromTime} toTime={toTime} onFromTime={setFromTime} onToTime={setToTime} />
      </div>

      {/* figure cards: they are also the field switches */}
      <div className="grid grid-cols-2 gap-2 lg:grid-cols-4">
        {FIELDS.map((f) => {
          const on = view.fields[f.key];
          const value = totals[f.key];
          return (
            <button
              key={f.key}
              type="button"
              title={f.hint}
              onClick={() => patchView({ fields: { ...view.fields, [f.key]: !on } })}
              className={cn("rounded-xl border p-3 text-left transition", on ? "border-primary/50 bg-primary/5" : "border-dashed opacity-60 hover:opacity-100")}
            >
              <div className="flex items-center justify-between text-xs text-muted-foreground">
                <span>{f.label}</span>
                <span className={cn("rounded-full px-1.5 py-0.5 text-[10px]", on ? "bg-primary text-primary-foreground" : "bg-muted")}>{on ? "görünür" : "gizli"}</span>
              </div>
              <div className="mt-1 text-lg font-semibold">
                {f.key === "increase" || f.key === "decrease" ? formatCurrency(value) : formatCurrency(Math.abs(value))}
              </div>
              {(f.key === "opening" || f.key === "closing") && Math.abs(value) >= 0.005 && (
                <div className={cn("text-[11px]", value > 0 ? "text-amber-600" : "text-emerald-600")}>{value > 0 ? positiveText : negativeText}</div>
              )}
            </button>
          );
        })}
      </div>

      {/* view: presets, grouping, conditions */}
      <div className="space-y-3 rounded-lg border bg-card p-3">
        <div className="flex flex-wrap items-center gap-1.5">
          <span className="mr-1 text-xs font-medium text-muted-foreground">Hazır görünüş:</span>
          {PRESETS.map((p) => (
            <button key={p.label} type="button" onClick={() => patchView(p.apply())} className="rounded-full border px-3 py-1 text-xs hover:bg-muted">
              {p.label}
            </button>
          ))}
        </div>

        <div className="flex flex-wrap items-center gap-1.5">
          <span className="mr-1 text-xs font-medium text-muted-foreground">Qruplaşdır:</span>
          {GROUPS.map((g) => (
            <button
              key={g.key}
              type="button"
              title={g.hint}
              onClick={() => patchView({ group: g.key })}
              className={cn("rounded-full border px-3 py-1 text-xs", view.group === g.key ? "border-primary bg-primary text-primary-foreground" : "hover:bg-muted")}
            >
              {g.label}
            </button>
          ))}
        </div>

        <div className="flex flex-wrap items-end gap-3 border-t pt-3">
          <div className="space-y-1">
            <Label htmlFor="st-q">{partyLabel}</Label>
            <Input id="st-q" className="w-48" placeholder="Ad və ya telefon…" value={term} onChange={(e) => setTerm(e.target.value)} />
          </div>
          <div className="space-y-1">
            <Label htmlFor="st-doc">Sənəd №</Label>
            <Input id="st-doc" className="w-40" placeholder="Sənəd nömrəsi…" value={docTerm} onChange={(e) => setDocTerm(e.target.value)} />
          </div>
          <div className="space-y-1">
            <Label htmlFor="st-min">Son borc (ən azı)</Label>
            <Input id="st-min" className="w-36" inputMode="decimal" placeholder="0.00" value={view.minDebt} onChange={(e) => patchView({ minDebt: e.target.value })} />
          </div>
          <label className="flex h-9 items-center gap-2 text-sm">
            <input type="checkbox" checked={view.onlyDebt} onChange={(e) => patchView({ onlyDebt: e.target.checked })} />
            Yalnız borcu olanlar
          </label>
        </div>

        {allTypes.length > 0 && (
          <div className="flex flex-wrap items-center gap-1.5">
            <span className="mr-1 text-xs font-medium text-muted-foreground">Hərəkət növü:</span>
            {allTypes.map((t) => (
              <button
                key={t}
                type="button"
                onClick={() => toggleType(t)}
                className={cn("rounded-full border px-2.5 py-0.5 text-xs", types.has(t) ? "border-primary bg-primary/10 text-primary" : "hover:bg-muted")}
              >
                {typeLabel(t)}
              </button>
            ))}
            {types.size > 0 && <span className="text-[11px] text-muted-foreground">Növ və sənəd süzgəci artma/azalma rəqəmlərini hesablayır; ilkin və son qalıq dəyişmir.</span>}
          </div>
        )}
      </div>

      <div className="text-xs text-muted-foreground">
        {loading ? "Yüklənir…" : `${rows.length} ${partyLabel.toLowerCase()}, ${flat.length} hərəkət${filtersActive ? " (süzgəclə)" : ""}`}
      </div>

      {/* result */}
      <div className="overflow-x-auto rounded-lg border bg-card">
        {showParty ? (
          <table className="w-full text-sm">
            <thead>
              <tr className="bg-muted/40 text-left text-xs text-muted-foreground">
                <th className={th}>{partyLabel}</th>
                {visibleFields.map((f) => (
                  <th key={f.key} className={cn(th, "text-right")}>
                    {f.label}
                  </th>
                ))}
              </tr>
            </thead>
            <tbody>
              {loading ? (
                <tr>
                  <td colSpan={1 + visibleFields.length} className="px-3 py-8 text-center text-muted-foreground">
                    Yüklənir…
                  </td>
                </tr>
              ) : rows.length === 0 ? (
                <tr>
                  <td colSpan={1 + visibleFields.length} className="px-3 py-8 text-center text-muted-foreground">
                    Bu şərtlərlə hesablaşma tapılmadı.
                  </td>
                </tr>
              ) : (
                rows.map((r) => {
                  const key = String(r.partyId);
                  const expanded = view.group === "party" && open.has(key);
                  return (
                    <Fragment key={r.partyId}>
                      <tr className={cn("border-t", view.group === "party" && "cursor-pointer hover:bg-muted/40")} onClick={() => view.group === "party" && toggleOpen(key)}>
                        <td className="px-3 py-2 font-medium">
                          <span className="flex items-center gap-1.5">
                            {view.group === "party" && (expanded ? <ChevronDown className="h-4 w-4" /> : <ChevronRight className="h-4 w-4" />)}
                            {r.name}
                            {r.phone && <span className="text-xs font-normal text-muted-foreground">{r.phone}</span>}
                          </span>
                        </td>
                        {view.fields.opening && (
                          <td className="px-3 py-2 text-right">
                            <Debt value={r.openingDebt} />
                          </td>
                        )}
                        {view.fields.increase && <td className="px-3 py-2 text-right">{money(r.increase)}</td>}
                        {view.fields.decrease && <td className="px-3 py-2 text-right">{money(r.decrease)}</td>}
                        {view.fields.closing && (
                          <td className="px-3 py-2 text-right">
                            <div className="flex items-center justify-end gap-2">
                              <span className="hidden h-1.5 w-24 overflow-hidden rounded-full bg-muted sm:block" aria-hidden>
                                <span
                                  className={cn("block h-full rounded-full", r.closingDebt > 0 ? "bg-amber-500" : "bg-emerald-500")}
                                  style={{ width: `${maxAbsClosing ? Math.round((Math.abs(r.closingDebt) / maxAbsClosing) * 100) : 0}%` }}
                                />
                              </span>
                              <Debt value={r.closingDebt} bold />
                            </div>
                          </td>
                        )}
                      </tr>
                      {expanded &&
                        (r.entries.length === 0 ? (
                          <tr className="bg-muted/10 text-xs text-muted-foreground">
                            <td colSpan={1 + visibleFields.length} className="px-9 py-2">
                              Bu şərtlərlə hərəkət yoxdur.
                            </td>
                          </tr>
                        ) : (
                          r.entries.map((e, i) => (
                            <tr key={i} className="bg-muted/10 text-xs">
                              <td className="py-1.5 pl-9 pr-3">
                                {typeLabel(e.typeName)}
                                {e.documentNo && <span className="ml-1 font-mono">{e.documentNo}</span>}
                                <span className="ml-2 text-muted-foreground">{dateTime(e.entryDate)}</span>
                                {e.note && <span className="ml-2 text-muted-foreground">· {e.note}</span>}
                              </td>
                              {view.fields.opening && <td />}
                              {view.fields.increase && <td className="px-3 py-1.5 text-right italic">{e.increase ? formatCurrency(e.increase) : ""}</td>}
                              {view.fields.decrease && <td className="px-3 py-1.5 text-right italic">{e.decrease ? formatCurrency(e.decrease) : ""}</td>}
                              {view.fields.closing && <td />}
                            </tr>
                          ))
                        ))}
                    </Fragment>
                  );
                })
              )}
            </tbody>
          </table>
        ) : view.group === "flat" ? (
          <table className="w-full text-sm">
            <thead>
              <tr className="bg-muted/40 text-left text-xs text-muted-foreground">
                <th className={th}>Tarix</th>
                <th className={th}>{partyLabel}</th>
                <th className={th}>Hərəkət</th>
                <th className={cn(th, "text-right")}>Artma</th>
                <th className={cn(th, "text-right")}>Azalma</th>
              </tr>
            </thead>
            <tbody>
              {flat.length === 0 ? (
                <tr>
                  <td colSpan={5} className="px-3 py-8 text-center text-muted-foreground">
                    {loading ? "Yüklənir…" : "Hərəkət tapılmadı."}
                  </td>
                </tr>
              ) : (
                flat.map((e, i) => (
                  <tr key={i} className="border-t">
                    <td className="px-3 py-2 whitespace-nowrap">{dateTime(e.entryDate)}</td>
                    <td className="px-3 py-2 font-medium">{e.partyName}</td>
                    <td className="px-3 py-2">
                      {typeLabel(e.typeName)}
                      {e.documentNo && <span className="ml-1 font-mono text-xs">{e.documentNo}</span>}
                    </td>
                    <td className="px-3 py-2 text-right">{e.increase ? formatCurrency(e.increase) : ""}</td>
                    <td className="px-3 py-2 text-right">{e.decrease ? formatCurrency(e.decrease) : ""}</td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        ) : (
          <table className="w-full text-sm">
            <thead>
              <tr className="bg-muted/40 text-left text-xs text-muted-foreground">
                <th className={th}>{view.group === "type" ? "Hərəkət növü" : "Gün"}</th>
                <th className={cn(th, "text-right")}>Say</th>
                <th className={cn(th, "text-right")}>Artma</th>
                <th className={cn(th, "text-right")}>Azalma</th>
              </tr>
            </thead>
            <tbody>
              {groupedBy.length === 0 ? (
                <tr>
                  <td colSpan={4} className="px-3 py-8 text-center text-muted-foreground">
                    {loading ? "Yüklənir…" : "Hərəkət tapılmadı."}
                  </td>
                </tr>
              ) : (
                groupedBy.map((g) => {
                  const expanded = open.has(g.key);
                  return (
                    <Fragment key={g.key}>
                      <tr className="cursor-pointer border-t hover:bg-muted/40" onClick={() => toggleOpen(g.key)}>
                        <td className="px-3 py-2 font-medium">
                          <span className="flex items-center gap-1.5">
                            {expanded ? <ChevronDown className="h-4 w-4" /> : <ChevronRight className="h-4 w-4" />}
                            {g.label}
                          </span>
                        </td>
                        <td className="px-3 py-2 text-right">{g.entries.length}</td>
                        <td className="px-3 py-2 text-right">{money(g.increase)}</td>
                        <td className="px-3 py-2 text-right">{money(g.decrease)}</td>
                      </tr>
                      {expanded &&
                        g.entries.map((e, i) => (
                          <tr key={i} className="bg-muted/10 text-xs">
                            <td className="py-1.5 pl-9 pr-3">
                              <span className="font-medium">{e.partyName}</span>
                              {view.group === "day" && <span className="ml-2">{typeLabel(e.typeName)}</span>}
                              {e.documentNo && <span className="ml-1 font-mono">{e.documentNo}</span>}
                              <span className="ml-2 text-muted-foreground">{dateTime(e.entryDate)}</span>
                            </td>
                            <td />
                            <td className="px-3 py-1.5 text-right italic">{e.increase ? formatCurrency(e.increase) : ""}</td>
                            <td className="px-3 py-1.5 text-right italic">{e.decrease ? formatCurrency(e.decrease) : ""}</td>
                          </tr>
                        ))}
                    </Fragment>
                  );
                })
              )}
            </tbody>
          </table>
        )}
      </div>
    </div>
  );
}
