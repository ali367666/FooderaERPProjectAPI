"use client";

import { useCallback, useEffect, useMemo, useState } from "react";
import Link from "next/link";
import { Download } from "lucide-react";
import { toast } from "sonner";
import { TimeRangeInputs } from "@/components/store/time-range-inputs";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { formatCurrency } from "@/lib/format-currency";
import { toDateInputValue, withTime } from "@/lib/report-period";
import { downloadCsv } from "@/lib/store/csv";
import { cn } from "@/lib/utils";
import { getStoreProfitLoss, type StoreProfitLoss } from "@/lib/store/store-service";

const PRESETS: { label: string; range: () => [string, string] }[] = [
  { label: "Bu gün", range: () => [toDateInputValue(new Date()), toDateInputValue(new Date())] },
  { label: "Son 7 gün", range: () => [toDateInputValue(new Date(Date.now() - 6 * 86_400_000)), toDateInputValue(new Date())] },
  { label: "Bu ay", range: () => [toDateInputValue(new Date(new Date().getFullYear(), new Date().getMonth(), 1)), toDateInputValue(new Date())] },
  {
    label: "Keçən ay",
    range: () => {
      const n = new Date();
      return [toDateInputValue(new Date(n.getFullYear(), n.getMonth() - 1, 1)), toDateInputValue(new Date(n.getFullYear(), n.getMonth(), 0))];
    },
  },
];

const th = "px-3 py-2 font-medium";
const pct = (a: number, b: number) => (b === 0 ? "—" : `${Math.round((a / b) * 1000) / 10}%`);

/**
 * The result of a period: the turnover, what the goods cost, the gross profit, the other expenses and the net income, laid out as a step-by-step
 * chain with bars, then the expenses by item and the days.
 */
export function StoreProfitLossReport() {
  const [from, setFrom] = useState(toDateInputValue(new Date(new Date().getFullYear(), new Date().getMonth(), 1)));
  const [to, setTo] = useState(toDateInputValue(new Date()));
  const [fromTime, setFromTime] = useState("");
  const [toTime, setToTime] = useState("");
  const [data, setData] = useState<StoreProfitLoss | null>(null);
  const [loading, setLoading] = useState(true);

  const load = useCallback(async () => {
    setLoading(true);
    try {
      setData(await getStoreProfitLoss({ from: withTime(from, fromTime), to: withTime(to, toTime) }));
    } catch (e) {
      toast.error(e instanceof Error ? e.message : "Hesabat yüklənmədi.");
      setData(null);
    } finally {
      setLoading(false);
    }
  }, [from, to, fromTime, toTime]);

  useEffect(() => {
    void load();
  }, [load]);

  // The chain, every step drawn against the turnover.
  const steps = useMemo(() => {
    if (!data) return [];
    return [
      { key: "sales", label: "Satış dövriyyəsi", hint: "Satışlar, qaytarmalar çıxılmaqla", value: data.sales, sign: "", tone: "bg-sky-500", result: false },
      { key: "cost", label: "Satılan malların maya dəyəri", hint: "Satış anındakı maya (orta maya, FIFO və ya LIFO)", value: data.costOfGoods, sign: "−", tone: "bg-amber-500", result: false },
      { key: "gross", label: "Ümumi mənfəət", hint: "Dövriyyə − maya dəyəri", value: data.grossProfit, sign: "=", tone: data.grossProfit >= 0 ? "bg-emerald-500" : "bg-red-500", result: true },
      { key: "exp", label: "Digər xərclər", hint: "Kassadan ödənilən xərc sənədləri", value: data.expenses, sign: "−", tone: "bg-orange-500", result: false },
      { key: "net", label: "Təmiz gəlir", hint: "Ümumi mənfəət − digər xərclər", value: data.netIncome, sign: "=", tone: data.netIncome >= 0 ? "bg-emerald-600" : "bg-red-600", result: true },
    ];
  }, [data]);

  const scale = Math.max(1, ...steps.map((s) => Math.abs(s.value)));
  const maxDay = Math.max(1, ...(data?.days ?? []).map((d) => Math.max(d.sales, Math.abs(d.netIncome))));

  const exportCsv = () => {
    if (!data) return;
    downloadCsv(`netice-${from}-${to}.csv`, [
      ["Göstərici", "Məbləğ"],
      ["Satış dövriyyəsi", data.sales],
      ["Satılan malların maya dəyəri", data.costOfGoods],
      ["Ümumi mənfəət", data.grossProfit],
      ["Digər xərclər", data.expenses],
      ["Təmiz gəlir", data.netIncome],
      [],
      ["Xərc maddəsi", "Məbləğ"],
      ...data.expensesByItem.map((e) => [e.name, e.amount]),
      [],
      ["Gün", "Satış", "Ümumi mənfəət", "Xərc", "Təmiz gəlir"],
      ...data.days.map((d) => [d.day, d.sales, d.grossProfit, d.expenses, d.netIncome]),
    ]);
  };

  return (
    <div className="space-y-4 p-4">
      <div className="flex flex-wrap items-start justify-between gap-2">
        <div>
          <h1 className="text-2xl font-bold">Dövr nəticəsi</h1>
          <p className="text-sm text-muted-foreground">Satışdan təmiz gəlirə qədər addım-addım: dövriyyə, maya dəyəri, mənfəət, xərclər və nəticə.</p>
        </div>
        <Button variant="outline" size="sm" disabled={!data} onClick={exportCsv}>
          <Download className="mr-1 h-4 w-4" />
          Excel (CSV)
        </Button>
      </div>

      <div className="space-y-3 rounded-lg border bg-card p-3">
        <div className="flex flex-wrap items-center gap-1.5">
          {PRESETS.map((p) => (
            <button
              key={p.label}
              type="button"
              onClick={() => {
                const [f, t] = p.range();
                setFrom(f);
                setTo(t);
              }}
              className="rounded-full border px-3 py-1 text-xs hover:bg-muted"
            >
              {p.label}
            </button>
          ))}
        </div>
        <div className="flex flex-wrap items-end gap-3">
          <div className="space-y-1">
            <Label htmlFor="pl-from">Başlanğıc</Label>
            <Input id="pl-from" type="date" value={from} max={to} onChange={(e) => setFrom(e.target.value)} />
          </div>
          <div className="space-y-1">
            <Label htmlFor="pl-to">Son</Label>
            <Input id="pl-to" type="date" value={to} min={from} onChange={(e) => setTo(e.target.value)} />
          </div>
          <TimeRangeInputs idPrefix="pl" fromTime={fromTime} toTime={toTime} onFromTime={setFromTime} onToTime={setToTime} />
        </div>
      </div>

      {loading ? (
        <p className="py-8 text-center text-sm text-muted-foreground">Yüklənir…</p>
      ) : !data ? (
        <p className="py-8 text-center text-sm text-muted-foreground">Məlumat yoxdur.</p>
      ) : (
        <>
          {/* the headline */}
          <div className={cn("rounded-2xl border p-5", data.netIncome >= 0 ? "border-emerald-500/40 bg-emerald-500/5" : "border-red-500/40 bg-red-500/5")}>
            <p className="text-sm text-muted-foreground">
              {data.from.slice(0, 10)} — {data.to.slice(0, 10)} · {data.salesCount} satış
            </p>
            <p className={cn("mt-1 text-3xl font-bold", data.netIncome >= 0 ? "text-emerald-600" : "text-red-600")}>
              {data.netIncome >= 0 ? "Təmiz gəlir " : "Zərər "}
              {formatCurrency(Math.abs(data.netIncome))}
            </p>
            <p className="mt-1 text-sm text-muted-foreground">
              Ümumi mənfəət marjası: <b>{pct(data.grossProfit, data.sales)}</b> · Xərclərdən sonra: <b>{pct(data.netIncome, data.sales)}</b>
            </p>
          </div>

          {/* the chain */}
          <div className="space-y-2 rounded-lg border bg-card p-4">
            {steps.map((s) => (
              <div key={s.key} className={cn("grid grid-cols-[minmax(0,14rem)_1fr_8rem] items-center gap-3", s.result && "border-t pt-2")}>
                <div>
                  <p className={cn("text-sm", s.result ? "font-semibold" : "font-medium")}>{s.label}</p>
                  <p className="hidden text-[11px] text-muted-foreground sm:block">{s.hint}</p>
                </div>
                <div className="h-3 overflow-hidden rounded-full bg-muted">
                  <div className={cn("h-full rounded-full", s.tone)} style={{ width: `${(Math.abs(s.value) / scale) * 100}%` }} />
                </div>
                <p className={cn("text-right text-sm tabular-nums", s.result ? "font-semibold" : "", s.value < 0 && "text-red-600")}>
                  {s.sign} {formatCurrency(Math.abs(s.value))}
                </p>
              </div>
            ))}
          </div>

          <div className="grid gap-4 lg:grid-cols-2">
            <div className="overflow-hidden rounded-lg border bg-card">
              <div className="flex items-center justify-between border-b px-3 py-2">
                <h2 className="text-sm font-semibold">Digər xərclər maddələr üzrə</h2>
                <Link href="/dashboard/store/expenses" className="text-xs text-primary hover:underline">
                  Xərc sənədlərinə bax
                </Link>
              </div>
              {data.expensesByItem.length === 0 ? (
                <p className="px-3 py-6 text-center text-sm text-muted-foreground">Bu dövrdə xərc yoxdur.</p>
              ) : (
                <table className="w-full text-sm">
                  <tbody>
                    {data.expensesByItem.map((e) => (
                      <tr key={e.name} className="border-t first:border-t-0">
                        <td className="px-3 py-2">{e.name}</td>
                        <td className="w-1/3 px-3 py-2">
                          <span className="block h-1.5 overflow-hidden rounded-full bg-muted">
                            <span className="block h-full rounded-full bg-orange-500" style={{ width: `${(e.amount / Math.max(1, data.expenses)) * 100}%` }} />
                          </span>
                        </td>
                        <td className="px-3 py-2 text-right tabular-nums">{formatCurrency(e.amount)}</td>
                        <td className="px-3 py-2 text-right text-xs text-muted-foreground">{pct(e.amount, data.expenses)}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              )}
            </div>

            <div className="overflow-hidden rounded-lg border bg-card">
              <div className="border-b px-3 py-2">
                <h2 className="text-sm font-semibold">Günlər üzrə</h2>
                <p className="text-[11px] text-muted-foreground">Günlük satış və mənfəət satışlara görədir; qaytarmalar yalnız yuxarıdakı yekun nəticədə nəzərə alınır.</p>
              </div>
              <div className="max-h-96 overflow-y-auto">
                <table className="w-full text-sm">
                  <thead>
                    <tr className="bg-muted/40 text-left text-xs text-muted-foreground">
                      <th className={th}>Gün</th>
                      <th className={cn(th, "text-right")}>Satış</th>
                      <th className={cn(th, "text-right")}>Mənfəət</th>
                      <th className={cn(th, "text-right")}>Xərc</th>
                      <th className={cn(th, "text-right")}>Nəticə</th>
                    </tr>
                  </thead>
                  <tbody>
                    {data.days.length === 0 ? (
                      <tr>
                        <td colSpan={5} className="px-3 py-6 text-center text-muted-foreground">
                          Bu dövrdə hərəkət yoxdur.
                        </td>
                      </tr>
                    ) : (
                      data.days.map((d) => (
                        <tr key={d.day} className="border-t">
                          <td className="px-3 py-1.5 whitespace-nowrap">{new Date(d.day).toLocaleDateString("az-AZ")}</td>
                          <td className="px-3 py-1.5 text-right tabular-nums">{formatCurrency(d.sales)}</td>
                          <td className="px-3 py-1.5 text-right tabular-nums">{formatCurrency(d.grossProfit)}</td>
                          <td className="px-3 py-1.5 text-right tabular-nums text-orange-600">{d.expenses ? formatCurrency(d.expenses) : "—"}</td>
                          <td className="px-3 py-1.5">
                            <div className="flex items-center justify-end gap-2">
                              <span className="hidden h-1.5 w-16 overflow-hidden rounded-full bg-muted sm:block">
                                <span className={cn("block h-full rounded-full", d.netIncome >= 0 ? "bg-emerald-500" : "bg-red-500")} style={{ width: `${(Math.abs(d.netIncome) / maxDay) * 100}%` }} />
                              </span>
                              <span className={cn("tabular-nums", d.netIncome < 0 && "text-red-600")}>{formatCurrency(d.netIncome)}</span>
                            </div>
                          </td>
                        </tr>
                      ))
                    )}
                  </tbody>
                </table>
              </div>
            </div>
          </div>
        </>
      )}
    </div>
  );
}
