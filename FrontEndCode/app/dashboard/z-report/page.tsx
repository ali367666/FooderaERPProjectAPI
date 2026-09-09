"use client";

import { useCallback, useEffect, useMemo, useState } from "react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { toast } from "sonner";
import { getZReport, type ZReport } from "@/lib/services/analytics-service";
import { formatCurrency } from "@/lib/format-currency";
import { cn } from "@/lib/utils";

type PeriodKey = "today" | "yesterday" | "week" | "month" | "custom";

const PERIODS: { key: PeriodKey; label: string }[] = [
  { key: "today", label: "Bugün" },
  { key: "yesterday", label: "Dünən" },
  { key: "week", label: "Bu həftə" },
  { key: "month", label: "Bu ay" },
  { key: "custom", label: "Özəl tarix" },
];

function startOfDay(d: Date): Date {
  const x = new Date(d);
  x.setHours(0, 0, 0, 0);
  return x;
}

function endOfDay(d: Date): Date {
  const x = new Date(d);
  x.setHours(23, 59, 59, 999);
  return x;
}

function toDateInputValue(d: Date): string {
  const x = new Date(d);
  x.setMinutes(x.getMinutes() - x.getTimezoneOffset());
  return x.toISOString().slice(0, 10);
}

function computeRange(period: PeriodKey, customFrom: string, customTo: string): { from: Date; to: Date } | null {
  const now = new Date();
  switch (period) {
    case "today":
      return { from: startOfDay(now), to: endOfDay(now) };
    case "yesterday": {
      const y = new Date(now);
      y.setDate(y.getDate() - 1);
      return { from: startOfDay(y), to: endOfDay(y) };
    }
    case "week": {
      const from = new Date(now);
      from.setDate(from.getDate() - 6);
      return { from: startOfDay(from), to: endOfDay(now) };
    }
    case "month": {
      const from = new Date(now.getFullYear(), now.getMonth(), 1);
      return { from: startOfDay(from), to: endOfDay(now) };
    }
    case "custom": {
      if (!customFrom || !customTo) return null;
      return { from: startOfDay(new Date(customFrom)), to: endOfDay(new Date(customTo)) };
    }
    default:
      return null;
  }
}

export default function ZReportPage() {
  const [period, setPeriod] = useState<PeriodKey>("today");
  const [customFrom, setCustomFrom] = useState(toDateInputValue(new Date()));
  const [customTo, setCustomTo] = useState(toDateInputValue(new Date()));
  const [report, setReport] = useState<ZReport | null>(null);
  const [loading, setLoading] = useState(true);

  const range = useMemo(() => computeRange(period, customFrom, customTo), [period, customFrom, customTo]);

  const load = useCallback(async () => {
    if (!range) return;
    setLoading(true);
    try {
      const data = await getZReport(range.from, range.to);
      setReport(data);
    } catch (e) {
      toast.error(e instanceof Error ? e.message : "Z hesabatı yüklənmədi.");
      setReport(null);
    } finally {
      setLoading(false);
    }
  }, [range]);

  useEffect(() => {
    void load();
  }, [load]);

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-3xl font-bold text-foreground">Z Hesabatı</h1>
        <p className="text-muted-foreground mt-1">Seçilmiş dövr üzrə satış, ofisiant və kateqoriya hesabatı.</p>
      </div>

      <div className="flex flex-wrap items-center gap-2">
        {PERIODS.map((p) => (
          <Button
            key={p.key}
            variant={period === p.key ? "default" : "outline"}
            size="sm"
            onClick={() => setPeriod(p.key)}
          >
            {p.label}
          </Button>
        ))}
      </div>

      {period === "custom" && (
        <div className="flex flex-wrap items-end gap-3">
          <div>
            <Label htmlFor="zr-from">Başlanğıc</Label>
            <Input
              id="zr-from"
              type="date"
              className="mt-1"
              value={customFrom}
              onChange={(e) => setCustomFrom(e.target.value)}
            />
          </div>
          <div>
            <Label htmlFor="zr-to">Son</Label>
            <Input
              id="zr-to"
              type="date"
              className="mt-1"
              value={customTo}
              onChange={(e) => setCustomTo(e.target.value)}
            />
          </div>
        </div>
      )}

      {loading ? (
        <div className="text-sm text-muted-foreground">Yüklənir…</div>
      ) : !report ? (
        <div className="text-sm text-muted-foreground">Tarix aralığı seçin.</div>
      ) : (
        <>
          <div className="grid grid-cols-2 gap-4 lg:grid-cols-5">
            <SummaryCard label="Ümumi satış" value={formatCurrency(report.totalRevenue)} />
            <SummaryCard label="Endirim" value={formatCurrency(report.totalDiscount)} />
            <SummaryCard label="Nağd" value={formatCurrency(report.cashTotal)} />
            <SummaryCard label="Pos (kart)" value={formatCurrency(report.cardTotal)} />
            <SummaryCard label="Çek sayı" value={String(report.orderCount)} />
          </div>

          <div className="grid grid-cols-1 gap-4 lg:grid-cols-2">
            <ReportTable
              title="Məhsul satışları"
              rows={report.products}
              columns={[
                { key: "name", label: "Məhsul" },
                { key: "quantity", label: "Miqdar", align: "right" },
                { key: "revenue", label: "Məbləğ", align: "right", money: true },
              ]}
              emptyText="Bu dövrdə satış yoxdur."
            />
            <ReportTable
              title="Ofisiantların satışı"
              rows={report.waiters}
              columns={[
                { key: "waiterName", label: "Ofisiant" },
                { key: "orderCount", label: "Çek sayı", align: "right" },
                { key: "revenue", label: "Məbləğ", align: "right", money: true },
              ]}
              emptyText="Bu dövrdə satış yoxdur."
            />
          </div>

          <ReportTable
            title="Kateqoriya üzrə satış"
            rows={report.categories}
            columns={[
              { key: "categoryName", label: "Kateqoriya" },
              { key: "quantity", label: "Miqdar", align: "right" },
              { key: "revenue", label: "Məbləğ", align: "right", money: true },
            ]}
            emptyText="Bu dövrdə satış yoxdur."
          />
        </>
      )}
    </div>
  );
}

function SummaryCard({ label, value }: { label: string; value: string }) {
  return (
    <div className="rounded-lg border bg-card p-4">
      <p className="text-xs text-muted-foreground">{label}</p>
      <p className="mt-1 text-xl font-semibold text-foreground">{value}</p>
    </div>
  );
}

function ReportTable<T extends Record<string, unknown>>({
  title,
  rows,
  columns,
  emptyText,
}: {
  title: string;
  rows: T[];
  columns: { key: keyof T; label: string; align?: "left" | "right"; money?: boolean }[];
  emptyText: string;
}) {
  return (
    <div className="rounded-lg border bg-card">
      <div className="border-b px-4 py-3">
        <h2 className="text-sm font-semibold text-foreground">{title}</h2>
      </div>
      <div className="overflow-x-auto">
        <table className="w-full text-sm">
          <thead>
            <tr className="border-b bg-muted/50">
              {columns.map((c) => (
                <th
                  key={String(c.key)}
                  className={cn("px-4 py-2 font-medium text-muted-foreground", c.align === "right" ? "text-right" : "text-left")}
                >
                  {c.label}
                </th>
              ))}
            </tr>
          </thead>
          <tbody>
            {rows.length === 0 ? (
              <tr>
                <td colSpan={columns.length} className="px-4 py-6 text-center text-muted-foreground">
                  {emptyText}
                </td>
              </tr>
            ) : (
              rows.map((row, i) => (
                <tr key={i} className="border-b last:border-0">
                  {columns.map((c) => (
                    <td
                      key={String(c.key)}
                      className={cn("px-4 py-2", c.align === "right" ? "text-right" : "text-left")}
                    >
                      {c.money ? formatCurrency(Number(row[c.key])) : String(row[c.key])}
                    </td>
                  ))}
                </tr>
              ))
            )}
          </tbody>
        </table>
      </div>
    </div>
  );
}
