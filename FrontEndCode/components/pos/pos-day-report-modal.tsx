"use client";

import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import { format } from "date-fns";
import { toast } from "sonner";
import { Eye, EyeOff, Printer } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Switch } from "@/components/ui/switch";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { usePermissionSet } from "@/hooks/use-auth-permissions";
import { PERIODS, toDateInputValue, type PeriodKey } from "@/lib/report-period";
import { formatCurrency } from "@/lib/format-currency";
import { cn } from "@/lib/utils";
import { getPosTerminalContext } from "@/lib/pos-terminal-client";
import {
  getBusinessReport,
  ReportPeriod,
  type BusinessReport,
  type BusinessReportRequest,
  type BusinessReportSections,
} from "@/lib/services/report-service";

/** The /Reports/business endpoint requires this permission. */
const REPORTS_VIEW_PERMISSION = "Reports.View";

type OptionKey = keyof BusinessReportSections;

const OPTIONS: { key: OptionKey; label: string; defaultOn: boolean }[] = [
  { key: "productSales", label: "Məhsul satışları", defaultOn: true },
  { key: "productSalesByCategory", label: "Məhsul satışları (kateqoriyalı)", defaultOn: true },
  { key: "counterpartyDetails", label: "Kontragent detalları", defaultOn: true },
  { key: "expenses", label: "Xərclər", defaultOn: true },
  { key: "openChecks", label: "Açıq hesablar", defaultOn: true },
  { key: "gifts", label: "Hədiyyə", defaultOn: false },
  { key: "cancellations", label: "Ləğv olunanlar", defaultOn: false },
  { key: "tableSales", label: "Masalar üzrə satış", defaultOn: false },
  { key: "stockOperations", label: "Anbar əməliyyatları", defaultOn: true },
  { key: "stockBalance", label: "Anbar qalığı", defaultOn: true },
];

const OPTIONS_STORAGE_KEY = "pos-day-report-sections";

function defaultOptions(): BusinessReportSections {
  return Object.fromEntries(OPTIONS.map((o) => [o.key, o.defaultOn])) as BusinessReportSections;
}

function loadOptions(): BusinessReportSections {
  const defaults = defaultOptions();
  try {
    const raw = localStorage.getItem(OPTIONS_STORAGE_KEY);
    if (raw) return { ...defaults, ...(JSON.parse(raw) as Partial<BusinessReportSections>) };
  } catch {
    // Storage blocked or corrupt — fall back to the defaults.
  }
  return defaults;
}

/** Maps the period picker onto the backend periods; "yesterday" goes as a one-day custom period. */
function toPeriodRequest(
  period: PeriodKey,
  customFrom: string,
  customTo: string,
): Pick<BusinessReportRequest, "period" | "from" | "to"> | null {
  switch (period) {
    case "today":
      return { period: ReportPeriod.Today };
    case "yesterday": {
      // Business day starts at 06:00, so "yesterday" is relative to the current business date.
      const d = new Date(Date.now() - 6 * 60 * 60 * 1000);
      d.setDate(d.getDate() - 1);
      const day = toDateInputValue(d);
      return { period: ReportPeriod.Custom, from: day, to: day };
    }
    case "week":
      return { period: ReportPeriod.ThisWeek };
    case "month":
      return { period: ReportPeriod.ThisMonth };
    case "custom":
      if (!customFrom || !customTo) return null;
      return { period: ReportPeriod.Custom, from: customFrom, to: customTo };
    default:
      return null;
  }
}

/**
 * "Hesabat" — the end-of-day summary of the till (sales, income, cash, open bills), opened over the
 * tables screen. Everything comes from the business report endpoint; the switches pick the detail
 * sections it builds, which go on the preview / printout.
 */
export function PosDayReportModal({ onClose }: { onClose: () => void }) {
  const permissions = usePermissionSet();
  const canReport = permissions.has(REPORTS_VIEW_PERMISSION);

  const [period, setPeriod] = useState<PeriodKey>("today");
  const [customFrom, setCustomFrom] = useState(toDateInputValue(new Date()));
  const [customTo, setCustomTo] = useState(toDateInputValue(new Date()));
  const periodRequest = useMemo(() => toPeriodRequest(period, customFrom, customTo), [period, customFrom, customTo]);

  const [report, setReport] = useState<BusinessReport | null>(null);
  const [loading, setLoading] = useState(true);
  const [preview, setPreview] = useState(false);
  const [options, setOptions] = useState<BusinessReportSections>(defaultOptions);
  useEffect(() => setOptions(loadOptions()), []);

  const toggleOption = (key: OptionKey, on: boolean) => {
    setOptions((prev) => {
      const next = { ...prev, [key]: on };
      try {
        localStorage.setItem(OPTIONS_STORAGE_KEY, JSON.stringify(next));
      } catch {
        // Not remembered — harmless.
      }
      return next;
    });
  };

  // Only the latest request may set the report (period / switches can change while one is in flight).
  const requestSeq = useRef(0);
  const load = useCallback(async () => {
    if (!periodRequest || !canReport) {
      setLoading(false);
      return;
    }
    const seq = ++requestSeq.current;
    setLoading(true);
    try {
      const data = await getBusinessReport({
        ...periodRequest,
        ...options,
        restaurantId: getPosTerminalContext()?.restaurantId ?? null,
      });
      if (seq === requestSeq.current) setReport(data);
    } catch (e) {
      if (seq !== requestSeq.current) return;
      toast.error(e instanceof Error ? e.message : "Hesabat yüklənmədi.");
      setReport(null);
    } finally {
      if (seq === requestSeq.current) setLoading(false);
    }
  }, [periodRequest, options, canReport]);

  useEffect(() => {
    void load();
  }, [load]);

  const documentRef = useRef<HTMLDivElement>(null);
  const handlePrint = () => {
    if (!documentRef.current) return;
    printHtml(documentRef.current.innerHTML);
  };

  const rangeLabel = report
    ? `${formatDateTime(report.fromLocal)} – ${formatDateTime(report.toLocal)}`
    : periodRequest
      ? "—"
      : "Tarix aralığı seçin";

  return (
    <Dialog open onOpenChange={(open) => !open && onClose()}>
      <DialogContent className="flex max-h-[calc(100dvh-1rem)] w-full max-w-[calc(100%-1rem)] flex-col gap-0 overflow-hidden p-0 sm:max-h-[calc(100dvh-2rem)] sm:max-w-[calc(100%-2rem)] lg:max-w-6xl">
        <DialogHeader className="shrink-0 space-y-3 border-b px-4 py-4 text-left sm:px-6">
          <div>
            <DialogTitle className="text-xl">Hesabat</DialogTitle>
            <DialogDescription className="mt-1 font-medium tabular-nums text-foreground/80">{rangeLabel}</DialogDescription>
          </div>
          {canReport && (
            <div className="flex flex-wrap items-end gap-2">
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
              {period === "custom" && (
                <div className="flex flex-wrap items-end gap-2">
                  <div>
                    <Label htmlFor="dr-from" className="text-xs">Başlanğıc</Label>
                    <Input id="dr-from" type="date" className="mt-1 h-8" value={customFrom} onChange={(e) => setCustomFrom(e.target.value)} />
                  </div>
                  <div>
                    <Label htmlFor="dr-to" className="text-xs">Son</Label>
                    <Input id="dr-to" type="date" className="mt-1 h-8" value={customTo} onChange={(e) => setCustomTo(e.target.value)} />
                  </div>
                </div>
              )}
            </div>
          )}
        </DialogHeader>

        <div className="min-h-0 flex-1 overflow-y-auto bg-muted/30 px-4 py-4 sm:px-6">
          {!canReport ? (
            <p className="py-8 text-center text-sm text-muted-foreground">
              Hesabat üçün “{REPORTS_VIEW_PERMISSION}” icazəsi lazımdır.
            </p>
          ) : loading && !report ? (
            <p className="py-12 text-center text-sm text-muted-foreground">Yüklənir…</p>
          ) : !report ? (
            <p className="py-12 text-center text-sm text-muted-foreground">Hesabat yüklənmədi.</p>
          ) : preview ? (
            <div className={cn("mx-auto max-w-3xl rounded-lg border bg-background p-6 shadow-sm", loading && "opacity-60")}>
              <ReportDocument rangeLabel={rangeLabel} report={report} options={options} />
            </div>
          ) : (
            <div className={cn("space-y-4", loading && "opacity-60")}>
              <SummarySection report={report} />
              <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-[1fr_1fr_minmax(0,18rem)]">
                <Panel title="Satış">
                  <Row label="Dəzgah" value={report.sales.counter} />
                  <Row label="Masa" value={report.sales.table} />
                  <Row label="Paket" value={report.sales.package} />
                  <Row label="Endirim" value={-report.sales.discount} tone="negative" />
                  <Row label="Hədiyyə" value={-report.sales.gift} tone="negative" />
                  <Row label="Toplam" value={report.sales.total} strong />
                </Panel>
                <Panel title="Gəlir">
                  <Row label="Nağd" value={report.income.cash} />
                  <Row label="Bank gəliri" value={report.income.bank} />
                  {report.income.bankBreakdown.map((b) => (
                    <Row key={b.bankName} label={b.bankName} value={b.amount} sub />
                  ))}
                  <Row label="Toplam" value={report.income.total} strong />
                  <Row label="Nağd gəlir" value={report.income.cashIn} tone="positive" />
                  <Row label="Nağd xərc" value={-report.income.cashOut} tone="negative" />
                  {report.income.cashReturns > 0 && (
                    <Row label="Nağd qaytarma" value={-report.income.cashReturns} tone="negative" />
                  )}
                  <Row label="Nağd qalıq" value={report.income.cashRemaining} strong />
                </Panel>
                <Panel title="Hesabata daxil et" className="md:col-span-2 xl:col-span-1">
                  <div className="grid gap-x-6 sm:grid-cols-2 xl:grid-cols-1">
                    {OPTIONS.map((o) => (
                      <label
                        key={o.key}
                        className="flex min-h-10 cursor-pointer items-center justify-between gap-3 border-b py-1.5 text-sm last:border-0"
                      >
                        <span>{o.label}</span>
                        <Switch
                          checked={options[o.key]}
                          onCheckedChange={(on) => toggleOption(o.key, on)}
                          className="data-[state=unchecked]:border-muted-foreground/40 data-[state=unchecked]:bg-muted-foreground/25 [&>span]:shadow-sm"
                        />
                      </label>
                    ))}
                  </div>
                </Panel>
              </div>
            </div>
          )}

          {/* Printed through an iframe (see printHtml); kept in the DOM so printing works without the preview open. */}
          {report && (
            <div ref={documentRef} className="hidden">
              <ReportDocument rangeLabel={rangeLabel} report={report} options={options} />
            </div>
          )}
        </div>

        <DialogFooter className="shrink-0 flex-row flex-wrap gap-2 border-t px-4 py-3 sm:justify-between sm:px-6">
          <Button variant="outline" size="lg" onClick={onClose}>
            Bağla
          </Button>
          {canReport && (
            <div className="flex flex-1 flex-wrap justify-end gap-2">
              <Button variant="outline" size="lg" disabled={!report} onClick={() => setPreview((p) => !p)}>
                {preview ? <EyeOff className="h-4 w-4" /> : <Eye className="h-4 w-4" />}
                {preview ? "Xülasəyə qayıt" : "Çap etmədən izlə"}
              </Button>
              <Button size="lg" disabled={!report || loading} onClick={handlePrint}>
                <Printer className="h-4 w-4" />
                Çap et
              </Button>
            </div>
          )}
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}

/** Total current debt of the counterparties — only known when the counterparty section was requested. */
function counterpartyDebt(report: BusinessReport): number | null {
  return report.counterpartyDetails?.reduce((s, c) => s + c.currentDebt, 0) ?? null;
}

function SummarySection({ report }: { report: BusinessReport }) {
  const s = report.summary;
  const debt = counterpartyDebt(report);
  return (
    <div className="grid gap-4 rounded-xl border bg-card p-4 md:grid-cols-2">
      <div className="space-y-0.5">
        <Row label="Hesab sayı" value={s.checkCount} count />
        <Row label={`Ləğv (${s.cancelledCount})`} value={s.cancelledAmount} tone="negative" />
        <Row label={`Açıq hesab (${s.openCheckCount})`} value={s.openCheckAmount} />
        <Row label="Borca yazılan" value={s.counterpartyCharges} />
        <Row
          label="Kontragent borcu"
          value={debt}
          tone="negative"
          hint={debt == null ? "“Kontragent detalları” seçildikdə göstərilir." : undefined}
        />
      </div>
      <div className="space-y-0.5 md:border-l md:pl-4">
        <Row label={`Hədiyyə (${s.giftCount})`} value={s.giftAmount} />
        <Row label="Xərclər" value={s.expenses} />
        <Row label="Cari gəlir — nağd" value={s.currentIncomeCash} />
        <Row label="Cari gəlir — bank" value={s.currentIncomeBank} />
      </div>
    </div>
  );
}

function Panel({ title, className, children }: { title: string; className?: string; children: React.ReactNode }) {
  return (
    <section className={cn("rounded-xl border bg-card p-4", className)}>
      <h3 className="mb-2 text-sm font-semibold uppercase tracking-wide text-primary">{title}</h3>
      <div className="space-y-0.5">{children}</div>
    </section>
  );
}

/** One label / amount line. `value` null means the figure is not available. */
function Row({
  label,
  value,
  tone,
  strong,
  sub,
  count,
  hint,
}: {
  label: string;
  value: number | null;
  tone?: "positive" | "negative";
  strong?: boolean;
  sub?: boolean;
  count?: boolean;
  hint?: string;
}) {
  return (
    <div
      className={cn(
        "flex items-center justify-between gap-3 rounded-md px-2 py-1.5 text-sm",
        strong && "mt-1 border-t pt-2.5 font-semibold",
        sub && "py-1 pl-5 text-xs text-muted-foreground",
      )}
      title={hint}
    >
      <span className={cn(!sub && !strong && "text-muted-foreground", tone === "negative" && !sub && "text-destructive/90")}>
        {label}
      </span>
      <span
        className={cn(
          "shrink-0 text-right tabular-nums",
          strong ? "text-base" : "font-medium",
          value == null && "text-muted-foreground",
          tone === "positive" && value != null && "text-emerald-600",
        )}
      >
        {value == null ? "—" : count ? value : formatCurrency(value)}
      </span>
    </div>
  );
}

/** The preview / printout: the summary figures plus the detail sections switched on. Plain markup so it prints styled by printHtml. */
function ReportDocument({
  rangeLabel,
  report,
  options,
}: {
  rangeLabel: string;
  report: BusinessReport;
  options: BusinessReportSections;
}) {
  const terminal = getPosTerminalContext();
  const money = (n: number | null) => (n == null ? "—" : formatCurrency(n));
  const qty = (n: number) => String(Number(n.toFixed(3)));
  const s = report.summary;
  return (
    <div className="report-doc space-y-5 text-sm">
      <div>
        <h1 className="text-lg font-bold">Hesabat{terminal?.restaurantName ? ` — ${terminal.restaurantName}` : ""}</h1>
        <p className="text-muted-foreground">{rangeLabel}</p>
      </div>

      <DocTable
        title="Xülasə"
        head={["", "Məbləğ"]}
        rows={[
          ["Hesab sayı", String(s.checkCount)],
          [`Ləğv (${s.cancelledCount})`, money(s.cancelledAmount)],
          [`Açıq hesab (${s.openCheckCount})`, money(s.openCheckAmount)],
          ["Borca yazılan", money(s.counterpartyCharges)],
          ["Kontragent borcu", money(counterpartyDebt(report))],
          [`Hədiyyə (${s.giftCount})`, money(s.giftAmount)],
          ["Xərclər", money(s.expenses)],
          ["Cari gəlir — nağd", money(s.currentIncomeCash)],
          ["Cari gəlir — bank", money(s.currentIncomeBank)],
        ]}
      />
      <DocTable
        title="Satış"
        head={["", "Məbləğ"]}
        rows={[
          ["Dəzgah", money(report.sales.counter)],
          ["Masa", money(report.sales.table)],
          ["Paket", money(report.sales.package)],
          ["Endirim", money(-report.sales.discount)],
          ["Hədiyyə", money(-report.sales.gift)],
          ["Toplam", money(report.sales.total)],
        ]}
      />
      <DocTable
        title="Gəlir"
        head={["", "Məbləğ"]}
        rows={[
          ["Nağd", money(report.income.cash)],
          ["Bank gəliri", money(report.income.bank)],
          ...report.income.bankBreakdown.map((b) => [`  ${b.bankName}`, money(b.amount)]),
          ["Toplam", money(report.income.total)],
          ["Nağd gəlir", money(report.income.cashIn)],
          ["Nağd xərc", money(-report.income.cashOut)],
          ...(report.income.cashReturns > 0 ? [["Nağd qaytarma", money(-report.income.cashReturns)]] : []),
          ["Nağd qalıq", money(report.income.cashRemaining)],
        ]}
      />

      {options.productSales && report.productSales && (
        <DocTable
          title="Məhsul satışları"
          head={["Məhsul", "Miqdar", "Məbləğ"]}
          rows={report.productSales.map((p) => [p.name, String(p.quantity), money(p.revenue)])}
        />
      )}
      {options.productSalesByCategory && report.productSalesByCategory && (
        <DocTable
          title="Kateqoriya üzrə satış"
          head={["Kateqoriya / məhsul", "Miqdar", "Məbləğ"]}
          rows={report.productSalesByCategory.flatMap((c) => [
            [c.categoryName, String(c.quantity), money(c.revenue)],
            ...c.products.map((p) => [`  ${p.name}`, String(p.quantity), money(p.revenue)]),
          ])}
        />
      )}
      {options.counterpartyDetails && report.counterpartyDetails && (
        <DocTable
          title="Kontragent detalları"
          head={["Kontragent", "Borca satış", "Ödənilib", "Cari borc"]}
          rows={report.counterpartyDetails.map((c) => [c.name, money(c.creditSales), money(c.paid), money(c.currentDebt)])}
        />
      )}
      {options.expenses && report.expenses && (
        <DocTable
          title="Xərclər"
          head={["Vaxt", "Səbəb", "Kim", "Məbləğ"]}
          rows={report.expenses.map((m) => [formatTime(m.createdAt), m.reason ?? "—", m.createdBy ?? "—", money(m.amount)])}
        />
      )}
      {options.openChecks && report.openChecks && (
        <DocTable
          title="Açıq hesablar"
          head={["Masa", "Ofisiant", "Açılıb", "Məbləğ"]}
          rows={report.openChecks.map((o) => [o.tableName, o.waiterName || "—", formatTime(o.openedAt), money(o.amount)])}
        />
      )}
      {options.gifts && report.gifts && (
        <DocTable
          title="Hədiyyələr"
          head={["Masa", "Məhsul", "Miqdar", "Kim", "Dəyər"]}
          rows={report.gifts.map((g) => [g.tableName, g.menuItemName, String(g.quantity), g.giftedBy, money(g.value)])}
        />
      )}
      {options.cancellations && report.cancellations && (
        <DocTable
          title="Ləğv olunanlar"
          head={["Vaxt", "Masa", "Məhsul", "Səbəb", "Kim", "Məbləğ"]}
          rows={report.cancellations.map((c) => [
            formatTime(c.cancelledAt),
            c.tableName,
            c.isWholeOrder ? "Bütün sifariş" : `${c.quantity} × ${c.menuItemName ?? ""}`,
            c.reason,
            c.cancelledBy,
            money(c.amount),
          ])}
        />
      )}
      {options.tableSales && report.tableSales && (
        <DocTable
          title="Masalar üzrə satış"
          head={["Masa", "Çek sayı", "Məbləğ"]}
          rows={report.tableSales.map((t) => [t.tableName, String(t.orderCount), money(t.revenue)])}
        />
      )}
      {options.stockOperations && report.stockOperations && (
        <DocTable
          title="Anbar əməliyyatları"
          head={["Məhsul", "Daxil olan", "Transfer +", "Transfer −", "Sərf", "Qaytarma"]}
          rows={report.stockOperations.map((o) => [
            `${o.stockItemName} (${o.unit})`,
            qty(o.received),
            qty(o.transferIn),
            qty(o.transferOut),
            qty(o.consumed),
            qty(o.returned),
          ])}
        />
      )}
      {options.stockBalance && report.stockBalance && (
        <DocTable
          title="Anbar qalığı"
          head={["Anbar", "Məhsul", "Qalıq"]}
          rows={report.stockBalance.map((b) => [b.warehouseName, b.stockItemName, `${qty(b.quantity)} ${b.unit}`])}
        />
      )}
    </div>
  );
}

function DocTable({ title, head, rows }: { title: string; head: string[]; rows: string[][] }) {
  return (
    <section>
      <h2 className="mb-1.5 font-semibold">{title}</h2>
      {rows.length === 0 ? (
        <p className="text-muted-foreground">Məlumat yoxdur.</p>
      ) : (
        <table className="w-full border-collapse">
          <thead>
            <tr className="border-b text-muted-foreground">
              {head.map((h, i) => (
                <th key={i} className={cn("py-1 font-medium", i === head.length - 1 ? "num text-right" : "text-left")}>
                  {h}
                </th>
              ))}
            </tr>
          </thead>
          <tbody>
            {rows.map((r, ri) => (
              <tr key={ri} className="border-b last:border-0">
                {r.map((cell, ci) => (
                  <td key={ci} className={cn("py-1 pr-2", ci === r.length - 1 && "num whitespace-nowrap pr-0 text-right tabular-nums")}>
                    {cell}
                  </td>
                ))}
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </section>
  );
}

function formatDateTime(iso: string): string {
  const d = new Date(iso);
  return Number.isFinite(d.getTime()) ? format(d, "dd.MM.yyyy HH:mm") : "—";
}

function formatTime(iso: string): string {
  const d = new Date(iso);
  return Number.isFinite(d.getTime()) ? format(d, "dd.MM HH:mm") : "—";
}

/** Prints the report markup from a hidden iframe so the POS screen behind the modal stays off the paper. */
function printHtml(body: string) {
  const iframe = document.createElement("iframe");
  iframe.style.position = "fixed";
  iframe.style.width = "0";
  iframe.style.height = "0";
  iframe.style.border = "0";
  document.body.appendChild(iframe);
  const doc = iframe.contentDocument;
  const win = iframe.contentWindow;
  if (!doc || !win) {
    iframe.remove();
    return;
  }
  doc.open();
  doc.write(`<!doctype html><html lang="az"><head><meta charset="utf-8"><title>Hesabat</title><style>
    body{font-family:system-ui,-apple-system,"Segoe UI",Roboto,sans-serif;font-size:12px;color:#111;margin:16px}
    h1{font-size:16px;margin:0 0 2px} h2{font-size:13px;margin:14px 0 4px} p{margin:0;color:#555}
    table{width:100%;border-collapse:collapse} th,td{padding:3px 6px 3px 0;text-align:left;border-bottom:1px solid #ddd}
    th{color:#555;font-weight:600} .num{text-align:right;white-space:nowrap;font-variant-numeric:tabular-nums;padding-right:0}
    section{break-inside:avoid}
  </style></head><body>${body}</body></html>`);
  doc.close();
  const cleanup = () => window.setTimeout(() => iframe.remove(), 500);
  win.onafterprint = cleanup;
  win.focus();
  win.print();
  // Browsers without afterprint in iframes still get the frame removed eventually.
  window.setTimeout(() => iframe.isConnected && iframe.remove(), 60_000);
}
