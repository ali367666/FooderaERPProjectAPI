"use client";

import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import Link from "next/link";
import { format } from "date-fns";
import { toast } from "sonner";
import {
  BarChart3,
  CalendarCheck,
  Eye,
  EyeOff,
  FileBarChart,
  FileText,
  Percent,
  Printer,
  Wallet,
  type LucideIcon,
} from "lucide-react";
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
import { POS_REPORT_PERMISSION } from "@/lib/pos-reports";
import { PERIODS, computeRange, toDateInputValue, type PeriodKey } from "@/lib/report-period";
import { formatCurrency } from "@/lib/format-currency";
import { cn } from "@/lib/utils";
import { getPosTerminalContext } from "@/lib/pos-terminal-client";
import { isActiveOrderStatus } from "@/lib/pos-order-status";
import { getZReport, type ZReport } from "@/lib/services/analytics-service";
import { getOrders, type OrderDto } from "@/lib/services/order-service";
import { getRestaurantTables, RestaurantTableType, type RestaurantTable } from "@/lib/services/restaurant-table-service";
import { CashMovementType, getCashMovements, type CashMovement } from "@/lib/services/cash-movement-service";
import { getCounterparties, type Counterparty } from "@/lib/services/counterparty-service";


/** Name of the till's counter-sale table (EnsureStoreSaleTableCommandHandler.StoreSaleTableName). */
const STORE_SALE_TABLE_NAME = "Mağaza Satışı";

type OptionKey =
  | "products"
  | "categories"
  | "counterparties"
  | "expenses"
  | "openAccounts"
  | "gifts"
  | "cancellations"
  | "tables"
  | "warehouseOps"
  | "warehouseBalance";

const OPTIONS: { key: OptionKey; label: string; defaultOn: boolean }[] = [
  { key: "products", label: "Məhsul satışları", defaultOn: true },
  { key: "categories", label: "Məhsul satışları (kateqoriyalı)", defaultOn: true },
  { key: "counterparties", label: "Kontragent detalları", defaultOn: true },
  { key: "expenses", label: "Xərclər", defaultOn: true },
  { key: "openAccounts", label: "Açıq hesablar", defaultOn: true },
  { key: "gifts", label: "Hədiyyə", defaultOn: false },
  { key: "cancellations", label: "Ləğv olunanlar", defaultOn: false },
  { key: "tables", label: "Masalar üzrə satış", defaultOn: false },
  { key: "warehouseOps", label: "Anbar əməliyyatları", defaultOn: true },
  { key: "warehouseBalance", label: "Anbar qalığı", defaultOn: true },
];

/** No report endpoint returns these yet — the switches are shown but cannot be turned on. */
const UNSUPPORTED_OPTIONS = new Set<OptionKey>(["warehouseOps", "warehouseBalance"]);

const OPTIONS_STORAGE_KEY = "pos-day-report-options";

function loadOptions(): Record<OptionKey, boolean> {
  const defaults = Object.fromEntries(OPTIONS.map((o) => [o.key, o.defaultOn])) as Record<OptionKey, boolean>;
  try {
    const raw = localStorage.getItem(OPTIONS_STORAGE_KEY);
    if (raw) return { ...defaults, ...(JSON.parse(raw) as Partial<Record<OptionKey, boolean>>) };
  } catch {
    // Storage blocked or corrupt — fall back to the defaults.
  }
  return defaults;
}

const NO_BACKEND = "Bu göstərici üçün hələ backend məlumatı yoxdur.";

type Extras = {
  openOrders: OrderDto[] | null;
  tables: RestaurantTable[] | null;
  movements: CashMovement[] | null;
  debtors: Counterparty[] | null;
};

const EMPTY_EXTRAS: Extras = { openOrders: null, tables: null, movements: null, debtors: null };

/**
 * "Hesabat" — the end-of-day summary of the till (sales, income, cash, open bills), opened over the
 * tables screen. The figures come from the Z-report endpoint plus the open orders, cash movements
 * and counterparty debts the user may read; the switches pick the detail lists that go on the
 * preview / printout.
 */
export function PosDayReportModal({ onClose }: { onClose: () => void }) {
  const permissions = usePermissionSet();
  const canZReport = permissions.has(POS_REPORT_PERMISSION.zReport);
  const canOrders = permissions.has("Orders.View");
  const canCash = permissions.has("CashRegister.View");
  const canCounterparties = permissions.has("Counterparty.View");

  const [period, setPeriod] = useState<PeriodKey>("today");
  const [customFrom, setCustomFrom] = useState(toDateInputValue(new Date()));
  const [customTo, setCustomTo] = useState(toDateInputValue(new Date()));
  const range = useMemo(() => computeRange(period, customFrom, customTo), [period, customFrom, customTo]);

  const [report, setReport] = useState<ZReport | null>(null);
  const [extras, setExtras] = useState<Extras>(EMPTY_EXTRAS);
  const [loading, setLoading] = useState(true);
  const [preview, setPreview] = useState(false);
  const [options, setOptions] = useState<Record<OptionKey, boolean>>(() =>
    Object.fromEntries(OPTIONS.map((o) => [o.key, o.defaultOn])) as Record<OptionKey, boolean>,
  );
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

  const load = useCallback(async () => {
    if (!range || !canZReport) {
      setLoading(false);
      return;
    }
    setLoading(true);
    const restaurantId = getPosTerminalContext()?.restaurantId ?? null;
    try {
      const [reportData, ordersRes, tablesRes, movementsRes, counterpartiesRes] = await Promise.all([
        getZReport(range.from, range.to),
        canOrders ? getOrders().catch(() => null) : Promise.resolve(null),
        getRestaurantTables().catch(() => null),
        canCash && restaurantId ? getCashMovements(restaurantId, range.from, range.to).catch(() => null) : Promise.resolve(null),
        canCounterparties ? getCounterparties().catch(() => null) : Promise.resolve(null),
      ]);
      setReport(reportData);
      setExtras({
        // Same rule as the tables screen: the bills still open on this terminal's branch.
        openOrders:
          ordersRes?.filter((o) => isActiveOrderStatus(o.status) && (!restaurantId || o.restaurantId === restaurantId)) ?? null,
        tables: tablesRes,
        movements: movementsRes,
        debtors:
          counterpartiesRes
            ?.filter((c) => c.currentDebtAmount > 0)
            .sort((a, b) => b.currentDebtAmount - a.currentDebtAmount) ?? null,
      });
    } catch (e) {
      toast.error(e instanceof Error ? e.message : "Hesabat yüklənmədi.");
      setReport(null);
      setExtras(EMPTY_EXTRAS);
    } finally {
      setLoading(false);
    }
  }, [range, canZReport, canOrders, canCash, canCounterparties]);

  useEffect(() => {
    void load();
  }, [load]);

  const figures = useMemo(() => (report ? computeFigures(report, extras) : null), [report, extras]);

  const documentRef = useRef<HTMLDivElement>(null);
  const handlePrint = () => {
    if (!documentRef.current) return;
    printHtml(documentRef.current.innerHTML);
  };

  const rangeLabel = range
    ? `${format(range.from, "dd.MM.yyyy HH:mm")} – ${format(range.to, "dd.MM.yyyy HH:mm")}`
    : "Tarix aralığı seçin";

  return (
    <Dialog open onOpenChange={(open) => !open && onClose()}>
      <DialogContent className="flex max-h-[calc(100dvh-1rem)] w-full max-w-[calc(100%-1rem)] flex-col gap-0 overflow-hidden p-0 sm:max-h-[calc(100dvh-2rem)] sm:max-w-[calc(100%-2rem)] lg:max-w-6xl">
        <DialogHeader className="shrink-0 space-y-3 border-b px-4 py-4 text-left sm:px-6">
          <div>
            <DialogTitle className="text-xl">Hesabat</DialogTitle>
            <DialogDescription className="mt-1 font-medium tabular-nums text-foreground/80">{rangeLabel}</DialogDescription>
          </div>
          {canZReport && (
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
          {!canZReport ? (
            <p className="py-8 text-center text-sm text-muted-foreground">
              Gün yekunu üçün “Pos.ZReport” icazəsi lazımdır. Aşağıdakı hesabatlardan istifadə edin.
            </p>
          ) : loading ? (
            <p className="py-12 text-center text-sm text-muted-foreground">Yüklənir…</p>
          ) : !report || !figures ? (
            <p className="py-12 text-center text-sm text-muted-foreground">Hesabat yüklənmədi.</p>
          ) : preview ? (
            <div className="mx-auto max-w-3xl rounded-lg border bg-background p-6 shadow-sm">
              <ReportDocument
                rangeLabel={rangeLabel}
                report={report}
                extras={extras}
                figures={figures}
                options={options}
              />
            </div>
          ) : (
            <div className="space-y-4">
              <SummarySection figures={figures} />
              <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-[1fr_1fr_minmax(0,18rem)]">
                <Panel title="Satış">
                  <Row label="Dəzgah" value={figures.counterSales} />
                  <Row label="Masa" value={figures.tableSales} />
                  <Row label="Paket" value={figures.packageSales} />
                  <Row label="Endirim" value={-figures.discount} tone="negative" />
                  <Row label="Hədiyyə" value={-figures.giftValue} tone="negative" />
                  <Row label="Toplam" value={figures.salesTotal} strong />
                </Panel>
                <Panel title="Gəlir">
                  <Row label="Nağd" value={figures.cash} />
                  <Row label="Bank gəliri" value={figures.card} />
                  {figures.banks.map((b) => (
                    <Row key={b.bankName} label={`${b.bankName} (${b.paymentCount})`} value={b.totalAmount} sub />
                  ))}
                  <Row label="Toplam" value={figures.cash + figures.card} strong />
                  <Row label="Nağd gəlir" value={figures.cashIn} tone="positive" hint={canCash ? undefined : CASH_HINT} />
                  <Row label="Nağd xərc" value={figures.cashOut == null ? null : -figures.cashOut} tone="negative" hint={canCash ? undefined : CASH_HINT} />
                  {figures.cashReturns > 0 && <Row label="Nağd qaytarma" value={-figures.cashReturns} tone="negative" />}
                  <Row label="Nağd qalıq" value={figures.cashRemaining} strong hint={canCash ? undefined : CASH_HINT} />
                </Panel>
                <Panel title="Hesabata daxil et" className="md:col-span-2 xl:col-span-1">
                  <div className="grid gap-x-6 sm:grid-cols-2 xl:grid-cols-1">
                    {OPTIONS.map((o) => {
                      const unsupported = UNSUPPORTED_OPTIONS.has(o.key);
                      return (
                        <label
                          key={o.key}
                          className={cn(
                            "flex min-h-10 items-center justify-between gap-3 border-b py-1.5 text-sm last:border-0",
                            unsupported ? "cursor-not-allowed text-muted-foreground" : "cursor-pointer",
                          )}
                          title={unsupported ? NO_BACKEND : undefined}
                        >
                          <span>
                            {o.label}
                            {unsupported && <span className="ml-1.5 text-xs">(tezliklə)</span>}
                          </span>
                          <Switch
                            checked={!unsupported && options[o.key]}
                            disabled={unsupported}
                            onCheckedChange={(on) => toggleOption(o.key, on)}
                          />
                        </label>
                      );
                    })}
                  </div>
                </Panel>
              </div>
            </div>
          )}

          {/* Printed through an iframe (see printHtml); kept in the DOM so printing works without the preview open. */}
          {report && figures && (
            <div ref={documentRef} className="hidden">
              <ReportDocument rangeLabel={rangeLabel} report={report} extras={extras} figures={figures} options={options} />
            </div>
          )}
        </div>

        <DialogFooter className="shrink-0 flex-row flex-wrap gap-2 border-t px-4 py-3 sm:justify-between sm:px-6">
          <Button variant="outline" size="lg" onClick={onClose}>
            Bağla
          </Button>
          {canZReport && (
            <div className="flex flex-1 flex-wrap justify-end gap-2">
              <Button variant="outline" size="lg" disabled={!report} onClick={() => setPreview((p) => !p)}>
                {preview ? <EyeOff className="h-4 w-4" /> : <Eye className="h-4 w-4" />}
                {preview ? "Xülasəyə qayıt" : "Çap etmədən izlə"}
              </Button>
              <Button size="lg" disabled={!report} onClick={handlePrint}>
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

const CASH_HINT = "Kassa hərəkətləri üçün “CashRegister.View” icazəsi lazımdır.";

type Figures = ReturnType<typeof computeFigures>;

function computeFigures(report: ZReport, extras: Extras) {
  const tableById = new Map((extras.tables ?? []).map((t) => [t.id, t]));
  let counterSales = 0;
  let packageSales = 0;
  let tableSales = 0;
  let creditCount = 0;
  let creditTotal = 0;
  for (const r of report.receipts ?? []) {
    const table = tableById.get(r.tableId);
    if (table?.name === STORE_SALE_TABLE_NAME) counterSales += r.amount;
    else if (table && (table.type === RestaurantTableType.TakeAway || table.type === RestaurantTableType.Delivery))
      packageSales += r.amount;
    else tableSales += r.amount;
    if (r.paymentMethod === "Credit") {
      creditCount += 1;
      creditTotal += r.amount;
    }
  }

  const cancellations = report.cancellations ?? [];
  const gifts = report.gifts ?? [];
  const movements = extras.movements;
  const cashIn = movements?.filter((m) => m.type === CashMovementType.Deposit).reduce((s, m) => s + m.amount, 0) ?? null;
  const cashOut = movements?.filter((m) => m.type === CashMovementType.Withdrawal).reduce((s, m) => s + m.amount, 0) ?? null;
  const cashReturns = report.cashReturns ?? 0;

  return {
    orderCount: report.orderCount,
    cancelCount: cancellations.length,
    cancelTotal: cancellations.reduce((s, c) => s + c.amount, 0),
    openCount: extras.openOrders?.length ?? null,
    openTotal: extras.openOrders?.reduce((s, o) => s + o.totalAmount, 0) ?? null,
    creditCount,
    creditTotal,
    counterpartyDebt: extras.debtors?.reduce((s, c) => s + c.currentDebtAmount, 0) ?? null,
    giftCount: gifts.length,
    giftValue: gifts.reduce((s, g) => s + g.value, 0),
    expenses: cashOut,
    counterSales,
    tableSales,
    packageSales,
    discount: report.totalDiscount,
    salesTotal: report.totalRevenue,
    cash: report.cashTotal,
    card: report.cardTotal,
    banks: report.bankBreakdown ?? [],
    cashIn,
    cashOut,
    cashReturns,
    cashRemaining: cashIn == null || cashOut == null ? null : report.cashTotal - cashReturns + cashIn - cashOut,
  };
}

function SummarySection({ figures }: { figures: Figures }) {
  return (
    <div className="grid gap-4 rounded-xl border bg-card p-4 md:grid-cols-2">
      <div className="space-y-0.5">
        <Row label="Hesab sayı" value={figures.orderCount} count />
        <Row label={`Ləğv (${figures.cancelCount})`} value={figures.cancelTotal} tone="negative" />
        <Row
          label={`Açıq hesab (${figures.openCount ?? "—"})`}
          value={figures.openTotal}
          hint={figures.openTotal == null ? "Açıq sifarişlər üçün “Orders.View” icazəsi lazımdır." : undefined}
        />
        <Row label={`Borca yazılan (${figures.creditCount})`} value={figures.creditTotal} />
        <Row
          label="Kontragent borcu"
          value={figures.counterpartyDebt}
          tone="negative"
          hint={figures.counterpartyDebt == null ? "Kontragentlər üçün “Counterparty.View” icazəsi lazımdır." : undefined}
        />
      </div>
      <div className="space-y-0.5 md:border-l md:pl-4">
        <Row label={`Hədiyyə (${figures.giftCount})`} value={figures.giftValue} />
        <Row label="Xərclər" value={figures.expenses} hint={figures.expenses == null ? CASH_HINT : undefined} />
        <Row label="Cari gəlir — nağd" value={null} hint={NO_BACKEND} />
        <Row label="Cari gəlir — bank" value={null} hint={NO_BACKEND} />
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

/** One label / amount line. `value` null means the figure is not available (missing permission or backend). */
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

/** The preview / printout: the summary figures plus the detail lists switched on. Plain markup so it prints styled by printHtml. */
function ReportDocument({
  rangeLabel,
  report,
  extras,
  figures,
  options,
}: {
  rangeLabel: string;
  report: ZReport;
  extras: Extras;
  figures: Figures;
  options: Record<OptionKey, boolean>;
}) {
  const terminal = getPosTerminalContext();
  const money = (n: number | null) => (n == null ? "—" : formatCurrency(n));
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
          ["Hesab sayı", String(figures.orderCount)],
          [`Ləğv (${figures.cancelCount})`, money(figures.cancelTotal)],
          [`Açıq hesab (${figures.openCount ?? "—"})`, money(figures.openTotal)],
          [`Borca yazılan (${figures.creditCount})`, money(figures.creditTotal)],
          ["Kontragent borcu", money(figures.counterpartyDebt)],
          [`Hədiyyə (${figures.giftCount})`, money(figures.giftValue)],
          ["Xərclər", money(figures.expenses)],
        ]}
      />
      <DocTable
        title="Satış"
        head={["", "Məbləğ"]}
        rows={[
          ["Dəzgah", money(figures.counterSales)],
          ["Masa", money(figures.tableSales)],
          ["Paket", money(figures.packageSales)],
          ["Endirim", money(-figures.discount)],
          ["Hədiyyə", money(-figures.giftValue)],
          ["Toplam", money(figures.salesTotal)],
        ]}
      />
      <DocTable
        title="Gəlir"
        head={["", "Məbləğ"]}
        rows={[
          ["Nağd", money(figures.cash)],
          ["Bank gəliri", money(figures.card)],
          ...figures.banks.map((b) => [`  ${b.bankName} (${b.paymentCount})`, money(b.totalAmount)]),
          ["Toplam", money(figures.cash + figures.card)],
          ["Nağd gəlir", money(figures.cashIn)],
          ["Nağd xərc", money(figures.cashOut == null ? null : -figures.cashOut)],
          ...(figures.cashReturns > 0 ? [["Nağd qaytarma", money(-figures.cashReturns)]] : []),
          ["Nağd qalıq", money(figures.cashRemaining)],
        ]}
      />

      {options.products && (
        <DocTable
          title="Məhsul satışları"
          head={["Məhsul", "Miqdar", "Məbləğ"]}
          rows={report.products.map((p) => [p.name, String(p.quantity), money(p.revenue)])}
        />
      )}
      {options.categories && (
        <DocTable
          title="Kateqoriya üzrə satış"
          head={["Kateqoriya", "Miqdar", "Məbləğ"]}
          rows={report.categories.map((c) => [c.categoryName, String(c.quantity), money(c.revenue)])}
        />
      )}
      {options.counterparties && extras.debtors && (
        <DocTable
          title="Kontragent borcları"
          head={["Kontragent", "Borc"]}
          rows={extras.debtors.map((c) => [c.name, money(c.currentDebtAmount)])}
        />
      )}
      {options.expenses && extras.movements && (
        <DocTable
          title="Xərclər"
          head={["Vaxt", "Səbəb", "Kim", "Məbləğ"]}
          rows={extras.movements
            .filter((m) => m.type === CashMovementType.Withdrawal)
            .map((m) => [formatTime(m.createdAtUtc), m.reason ?? "—", m.createdByUserName ?? "—", money(m.amount)])}
        />
      )}
      {options.openAccounts && extras.openOrders && (
        <DocTable
          title="Açıq hesablar"
          head={["Masa", "Ofisiant", "Açılıb", "Məbləğ"]}
          rows={extras.openOrders.map((o) => [
            o.tableName ?? `#${o.tableId}`,
            o.waiterName ?? "—",
            formatTime(o.openedAt),
            money(o.totalAmount),
          ])}
        />
      )}
      {options.gifts && (
        <DocTable
          title="Hədiyyələr"
          head={["Masa", "Məhsul", "Miqdar", "Kim", "Dəyər"]}
          rows={(report.gifts ?? []).map((g) => [g.tableName, g.menuItemName, String(g.quantity), g.giftedBy, money(g.value)])}
        />
      )}
      {options.cancellations && (
        <DocTable
          title="Ləğv olunanlar"
          head={["Vaxt", "Masa", "Məhsul", "Səbəb", "Kim", "Məbləğ"]}
          rows={(report.cancellations ?? []).map((c) => [
            formatTime(c.cancelledAt),
            c.tableName,
            c.isWholeOrder ? "Bütün sifariş" : `${c.quantity} × ${c.menuItemName ?? ""}`,
            c.reason,
            c.cancelledBy,
            money(c.amount),
          ])}
        />
      )}
      {options.tables && (
        <DocTable
          title="Masalar üzrə satış"
          head={["Masa", "Çek sayı", "Məbləğ"]}
          rows={(report.tables ?? []).map((t) => [t.tableName, String(t.orderCount), money(t.revenue)])}
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
