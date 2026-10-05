"use client";

import { Fragment, useCallback, useEffect, useMemo, useState } from "react";
import Link from "next/link";
import {
  Ban,
  ChevronDown,
  ChevronRight,
  Gift,
  Info,
  LayoutGrid,
  type LucideIcon,
  Package,
  PieChart,
  Receipt,
  TrendingUp,
  UserRound,
  FileBarChart,
} from "lucide-react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { toast } from "sonner";
import { getZReport, type ZReport, type ZReportReceipt } from "@/lib/services/analytics-service";
import { getOrderById, type OrderDto } from "@/lib/services/order-service";
import { ReceiptLinesTable } from "@/components/receipt-lines-table";
import { formatCurrency } from "@/lib/format-currency";
import { PERIODS, computeRange, toDateInputValue, type PeriodKey } from "@/lib/report-period";
import { cn } from "@/lib/utils";

type ReportKey = "sales" | "products" | "tables" | "waiters" | "categories" | "receipts" | "cancellations" | "gifts";

const REPORTS: { key: ReportKey; label: string; icon: LucideIcon }[] = [
  { key: "sales", label: "Satış hesabatı", icon: TrendingUp },
  { key: "products", label: "Məhsullar üzrə satış", icon: Package },
  { key: "tables", label: "Masalar üzrə satış", icon: LayoutGrid },
  { key: "waiters", label: "Ofisiantlar üzrə satış", icon: UserRound },
  { key: "categories", label: "Kateqoriya üzrə satış", icon: PieChart },
  { key: "receipts", label: "Satış çekləri", icon: Receipt },
  { key: "cancellations", label: "Ləğv etmələr", icon: Ban },
  { key: "gifts", label: "Hədiyyələr", icon: Gift },
];

const PAYMENT_LABELS: Record<string, string> = { Cash: "Nağd", Card: "Kart", Credit: "Borc" };

function clock(value: string | null | undefined): string {
  return value ? new Date(value).toLocaleTimeString("az-AZ", { hour: "2-digit", minute: "2-digit" }) : "—";
}

function dateTime(value: string | null | undefined): string {
  return value
    ? new Date(value).toLocaleString("az-AZ", { day: "2-digit", month: "2-digit", hour: "2-digit", minute: "2-digit" })
    : "—";
}

function SummaryCard({ label, value, strong }: { label: string; value: string; strong?: boolean }) {
  return (
    <div className="rounded-lg border bg-card p-4">
      <p className="text-xs text-muted-foreground">{label}</p>
      <p className={cn("mt-1 font-semibold text-foreground tabular-nums", strong ? "text-2xl" : "text-xl")}>{value}</p>
    </div>
  );
}

function Th({ children, right }: { children?: React.ReactNode; right?: boolean }) {
  return (
    <th className={`px-4 py-2 font-medium text-muted-foreground ${right ? "text-right" : "text-left"}`}>{children}</th>
  );
}

function Td({ children, right, className = "" }: { children: React.ReactNode; right?: boolean; className?: string }) {
  return <td className={`px-4 py-2 ${right ? "text-right tabular-nums" : ""} ${className}`}>{children}</td>;
}

function ReportTable({ head, children, empty }: { head: React.ReactNode; children: React.ReactNode; empty: boolean }) {
  return (
    <div className="overflow-x-auto rounded-lg border bg-card">
      <table className="w-full text-sm">
        <thead>
          <tr className="border-b bg-muted/50">{head}</tr>
        </thead>
        <tbody>
          {empty ? (
            <tr>
              <td colSpan={20} className="px-4 py-6 text-center text-muted-foreground">
                Bu dövr üçün məlumat yoxdur.
              </td>
            </tr>
          ) : (
            children
          )}
        </tbody>
      </table>
    </div>
  );
}

function PercentBar({ percent }: { percent: number }) {
  return (
    <div className="flex items-center justify-end gap-2">
      <div className="hidden h-2 w-28 overflow-hidden rounded bg-muted sm:block">
        <div className="h-full bg-primary" style={{ width: `${Math.min(100, Math.max(0, percent))}%` }} />
      </div>
      <span className="w-12 text-right tabular-nums">{percent.toFixed(1)}%</span>
    </div>
  );
}

function Segmented<T extends string>({
  value,
  options,
  onChange,
}: {
  value: T;
  options: { value: T; label: string }[];
  onChange: (v: T) => void;
}) {
  return (
    <div className="inline-flex rounded-md border p-0.5">
      {options.map((o) => (
        <button
          key={o.value}
          type="button"
          onClick={() => onChange(o.value)}
          className={cn(
            "rounded px-3 py-1 text-sm",
            value === o.value ? "bg-primary text-primary-foreground" : "text-muted-foreground hover:text-foreground",
          )}
        >
          {o.label}
        </button>
      ))}
    </div>
  );
}

export default function ReportsPage() {
  const [period, setPeriod] = useState<PeriodKey>("today");
  const [customFrom, setCustomFrom] = useState(toDateInputValue(new Date()));
  const [customTo, setCustomTo] = useState(toDateInputValue(new Date()));
  const [report, setReport] = useState<ZReport | null>(null);
  const [loading, setLoading] = useState(true);
  const [active, setActive] = useState<ReportKey>("sales");
  const [tableView, setTableView] = useState<"total" | "separate">("total");
  const [openTables, setOpenTables] = useState<Set<number>>(new Set());
  const [detail, setDetail] = useState<OrderDto | null>(null);

  const range = useMemo(() => computeRange(period, customFrom, customTo), [period, customFrom, customTo]);

  const load = useCallback(async () => {
    if (!range) return;
    setLoading(true);
    try {
      setReport(await getZReport(range.from, range.to));
    } catch (e) {
      toast.error(e instanceof Error ? e.message : "Hesabat yüklənmədi.");
      setReport(null);
    } finally {
      setLoading(false);
    }
  }, [range]);

  useEffect(() => {
    void load();
  }, [load]);

  const openReceipt = useCallback(async (orderId: number) => {
    try {
      setDetail(await getOrderById(orderId));
    } catch (e) {
      toast.error(e instanceof Error ? e.message : "Çek yüklənmədi.");
    }
  }, []);

  const toggleTable = (id: number) =>
    setOpenTables((prev) => {
      const next = new Set(prev);
      if (next.has(id)) next.delete(id);
      else next.add(id);
      return next;
    });

  const tables = report?.tables ?? [];
  const receipts = report?.receipts ?? [];
  const cancellations = report?.cancellations ?? [];
  const gifts = report?.gifts ?? [];
  const detailCancellations = useMemo(
    () => (detail ? cancellations.filter((c) => c.orderId === detail.id) : []),
    [detail, cancellations],
  );
  const activeLabel = REPORTS.find((r) => r.key === active)?.label;

  const sessionsRow = (s: ZReportReceipt) => (
    <tr key={s.orderId} className="border-b last:border-0">
      <Td className="tabular-nums">{clock(s.openedAt)}</Td>
      <Td className="tabular-nums">{clock(s.paidAt)}</Td>
      <Td>{s.receiptNumber ?? s.orderNumber}</Td>
      <Td>{s.waiterName}</Td>
      <Td>{PAYMENT_LABELS[s.paymentMethod ?? ""] ?? "—"}</Td>
      <Td right>{formatCurrency(s.amount)}</Td>
      <Td right>
        <Button variant="ghost" size="icon" title="Çekin içi" onClick={() => void openReceipt(s.orderId)}>
          <Info className="h-4 w-4" />
        </Button>
      </Td>
    </tr>
  );

  return (
    <div className="space-y-6">
      <div className="flex flex-wrap items-end justify-between gap-3">
        <div>
          <h1 className="text-3xl font-bold text-foreground">Hesabatlar</h1>
          <p className="mt-1 text-muted-foreground">Hesabatı seçin — seçilmiş dövr üzrə aşağıda açılır.</p>
        </div>
        <Button variant="outline" size="sm" asChild>
          <Link href="/dashboard/z-report">
            <FileBarChart className="mr-1 h-4 w-4" />Z Hesabatı
          </Link>
        </Button>
      </div>

      <div className="flex flex-wrap items-center gap-2">
        {PERIODS.map((p) => (
          <Button key={p.key} variant={period === p.key ? "default" : "outline"} size="sm" onClick={() => setPeriod(p.key)}>
            {p.label}
          </Button>
        ))}
      </div>

      {period === "custom" && (
        <div className="flex flex-wrap items-end gap-3">
          <div>
            <Label htmlFor="rp-from">Başlanğıc</Label>
            <Input id="rp-from" type="date" className="mt-1" value={customFrom} onChange={(e) => setCustomFrom(e.target.value)} />
          </div>
          <div>
            <Label htmlFor="rp-to">Son</Label>
            <Input id="rp-to" type="date" className="mt-1" value={customTo} onChange={(e) => setCustomTo(e.target.value)} />
          </div>
        </div>
      )}

      <div className="grid grid-cols-2 gap-3 sm:grid-cols-4 lg:grid-cols-8">
        {REPORTS.map(({ key, label, icon: Icon }) => (
          <button
            key={key}
            type="button"
            onClick={() => setActive(key)}
            className={cn(
              "flex flex-col items-center justify-center gap-2 rounded-xl border p-3 text-center text-xs font-medium transition-colors",
              active === key
                ? "border-primary bg-primary text-primary-foreground"
                : "bg-card text-foreground hover:border-primary/60 hover:bg-muted/50",
            )}
          >
            <Icon className="h-7 w-7" />
            <span className="leading-tight">{label}</span>
          </button>
        ))}
      </div>

      <h2 className="text-lg font-semibold text-foreground">{activeLabel}</h2>

      {loading ? (
        <div className="text-sm text-muted-foreground">Yüklənir…</div>
      ) : !report ? (
        <div className="text-sm text-muted-foreground">Tarix aralığı seçin.</div>
      ) : (
        <>
          {active === "sales" && (
            <div className="space-y-4">
              <div className="grid grid-cols-1 gap-4 sm:grid-cols-3">
                <SummaryCard label="Ümumi dövriyyə" value={formatCurrency(report.totalRevenue)} strong />
                <SummaryCard label="Nağd" value={formatCurrency(report.cashTotal)} strong />
                <SummaryCard label="Kart" value={formatCurrency(report.cardTotal)} strong />
              </div>
              <div className="grid grid-cols-2 gap-4 lg:grid-cols-4">
                <SummaryCard
                  label="Borc (nisyə)"
                  value={formatCurrency(Math.max(0, report.totalRevenue - report.cashTotal - report.cardTotal))}
                />
                <SummaryCard label="Çek sayı" value={String(report.orderCount)} />
                <SummaryCard label="Endirim" value={formatCurrency(report.totalDiscount)} />
                <SummaryCard label="Servis haqqı" value={formatCurrency(report.totalServiceCharge ?? 0)} />
              </div>
            </div>
          )}

          {active === "products" && (
            <ReportTable
              empty={report.products.length === 0}
              head={
                <>
                  <Th>Məhsul</Th>
                  <Th right>Satılıb (ədəd)</Th>
                  <Th right>Məbləğ</Th>
                </>
              }
            >
              {report.products.map((p) => (
                <tr key={p.menuItemId} className="border-b last:border-0">
                  <Td>{p.name}</Td>
                  <Td right>{p.quantity}</Td>
                  <Td right>{formatCurrency(p.revenue)}</Td>
                </tr>
              ))}
            </ReportTable>
          )}

          {active === "tables" && (
            <div className="space-y-3">
              <Segmented
                value={tableView}
                onChange={setTableView}
                options={[
                  { value: "total", label: "Ümumi" },
                  { value: "separate", label: "Ayrı-ayrı (saat və məbləğ)" },
                ]}
              />
              {tableView === "total" ? (
                <ReportTable
                  empty={tables.length === 0}
                  head={
                    <>
                      <Th>Masa</Th>
                      <Th right>Müştəri sayı</Th>
                      <Th right>Ümumi məbləğ</Th>
                    </>
                  }
                >
                  {tables.map((t) => (
                    <tr key={t.tableId} className="border-b last:border-0">
                      <Td className="font-medium">{t.tableName}</Td>
                      <Td right>{t.orderCount}</Td>
                      <Td right>{formatCurrency(t.revenue)}</Td>
                    </tr>
                  ))}
                </ReportTable>
              ) : (
                <ReportTable
                  empty={tables.length === 0}
                  head={
                    <>
                      <Th>Oturdu</Th>
                      <Th>Ödədi</Th>
                      <Th>Çek</Th>
                      <Th>Ofisiant</Th>
                      <Th>Ödəniş</Th>
                      <Th right>Məbləğ</Th>
                      <Th />
                    </>
                  }
                >
                  {tables.map((t) => {
                    const open = openTables.has(t.tableId);
                    return (
                      <Fragment key={t.tableId}>
                        <tr className="cursor-pointer border-b bg-muted/30 hover:bg-muted/50" onClick={() => toggleTable(t.tableId)}>
                          <td colSpan={5} className="px-4 py-2 font-medium">
                            <span className="inline-flex items-center gap-1">
                              {open ? <ChevronDown className="h-4 w-4" /> : <ChevronRight className="h-4 w-4" />}
                              {t.tableName} · {t.orderCount} müştəri
                            </span>
                          </td>
                          <Td right className="font-medium">{formatCurrency(t.revenue)}</Td>
                          <td />
                        </tr>
                        {open && t.sessions.map(sessionsRow)}
                      </Fragment>
                    );
                  })}
                </ReportTable>
              )}
            </div>
          )}

          {active === "waiters" && (
            <div className="space-y-2">
              <p className="text-xs text-muted-foreground">
                Servis haqqı — ofisiantın satışlarında müştəridən alınan servis haqqı, yəni ofisianta veriləcək məbləğ.
              </p>
              <ReportTable
                empty={report.waiters.length === 0}
                head={
                  <>
                    <Th>Ofisiant</Th>
                    <Th right>Çek sayı</Th>
                    <Th right>Ümumi satış</Th>
                    <Th right>Ofisianta veriləcək (servis)</Th>
                  </>
                }
              >
                {report.waiters.map((w) => (
                  <tr key={w.waiterId} className="border-b last:border-0">
                    <Td>{w.waiterName}</Td>
                    <Td right>{w.orderCount}</Td>
                    <Td right>{formatCurrency(w.revenue)}</Td>
                    <Td right className="font-semibold">{formatCurrency(w.serviceCharge ?? 0)}</Td>
                  </tr>
                ))}
              </ReportTable>
            </div>
          )}

          {active === "categories" && (
            <div className="space-y-2">
              <p className="text-xs text-muted-foreground">
                Ümumi satış {formatCurrency(report.categories.reduce((s, c) => s + c.revenue, 0))} — kateqoriyalar üzrə
                bölgüsü.
              </p>
              <ReportTable
                empty={report.categories.length === 0}
                head={
                  <>
                    <Th>Kateqoriya</Th>
                    <Th right>Məbləğ</Th>
                    <Th right>Satışdan payı</Th>
                  </>
                }
              >
                {report.categories.map((c) => (
                  <tr key={c.categoryId} className="border-b last:border-0">
                    <Td>{c.categoryName}</Td>
                    <Td right>{formatCurrency(c.revenue)}</Td>
                    <Td right>
                      <PercentBar percent={c.percent ?? 0} />
                    </Td>
                  </tr>
                ))}
              </ReportTable>
            </div>
          )}

          {active === "receipts" && (
            <ReportTable
              empty={receipts.length === 0}
              head={
                <>
                  <Th>Saat</Th>
                  <Th>Masa</Th>
                  <Th>Çek</Th>
                  <Th>Ofisiant</Th>
                  <Th>Ödəniş</Th>
                  <Th right>Məbləğ</Th>
                  <Th />
                </>
              }
            >
              {receipts.map((r) => (
                <tr key={r.orderId} className="border-b last:border-0">
                  <Td className="tabular-nums">{dateTime(r.paidAt)}</Td>
                  <Td>{r.tableName}</Td>
                  <Td className="font-medium">{r.receiptNumber ?? r.orderNumber}</Td>
                  <Td>{r.waiterName}</Td>
                  <Td>{PAYMENT_LABELS[r.paymentMethod ?? ""] ?? "—"}</Td>
                  <Td right>{formatCurrency(r.amount)}</Td>
                  <Td right>
                    <Button variant="ghost" size="icon" title="Çekin içi" onClick={() => void openReceipt(r.orderId)}>
                      <Info className="h-4 w-4" />
                    </Button>
                  </Td>
                </tr>
              ))}
            </ReportTable>
          )}

          {active === "cancellations" && (
            <div className="space-y-2">
              <p className="text-xs text-muted-foreground">
                Cəmi: {formatCurrency(cancellations.reduce((sum, c) => sum + c.amount, 0))}.
              </p>
              <ReportTable
                empty={cancellations.length === 0}
                head={
                  <>
                    <Th>Saat</Th>
                    <Th>Masa</Th>
                    <Th>Sifariş</Th>
                    <Th>Nə</Th>
                    <Th right>Miqdar</Th>
                    <Th right>Məbləğ</Th>
                    <Th>Səbəb</Th>
                    <Th>Kim</Th>
                  </>
                }
              >
                {cancellations.map((c, i) => (
                  <tr key={i} className="border-b last:border-0">
                    <Td className="tabular-nums">{dateTime(c.cancelledAt)}</Td>
                    <Td>{c.tableName}</Td>
                    <Td>{c.orderNumber}</Td>
                    <Td>{c.isWholeOrder ? <span className="font-medium">Bütün sifariş</span> : c.menuItemName}</Td>
                    <Td right>{c.quantity}</Td>
                    <Td right>{formatCurrency(c.amount)}</Td>
                    <Td>
                      <span className={c.beforeKitchen ? "text-muted-foreground" : ""}>{c.reason}</span>
                      {c.note && <span className="block text-xs text-muted-foreground">{c.note}</span>}
                    </Td>
                    <Td>{c.cancelledBy}</Td>
                  </tr>
                ))}
              </ReportTable>
            </div>
          )}

          {active === "gifts" && (
            <div className="space-y-2">
              <p className="text-xs text-muted-foreground">
                Hədiyyələrin dəyəri: {formatCurrency(gifts.reduce((sum, g) => sum + g.value, 0))}.
              </p>
              <ReportTable
                empty={gifts.length === 0}
                head={
                  <>
                    <Th>Saat</Th>
                    <Th>Masa</Th>
                    <Th>Çek</Th>
                    <Th>Məhsul</Th>
                    <Th right>Miqdar</Th>
                    <Th right>Dəyəri</Th>
                    <Th>Kim</Th>
                  </>
                }
              >
                {gifts.map((g, i) => (
                  <tr key={i} className="border-b last:border-0">
                    <Td className="tabular-nums">{dateTime(g.giftedAt)}</Td>
                    <Td>{g.tableName}</Td>
                    <Td>{g.receiptNumber ?? g.orderNumber}</Td>
                    <Td>{g.menuItemName}</Td>
                    <Td right>{g.quantity}</Td>
                    <Td right>{formatCurrency(g.value)}</Td>
                    <Td>{g.giftedBy}</Td>
                  </tr>
                ))}
              </ReportTable>
            </div>
          )}
        </>
      )}

      <Dialog open={detail != null} onOpenChange={(o) => !o && setDetail(null)}>
        <DialogContent className="sm:max-w-2xl">
          <DialogHeader>
            <DialogTitle>{detail?.receiptNumber ?? detail?.orderNumber}</DialogTitle>
            <DialogDescription>
              Masa: {detail?.tableName ?? `#${detail?.tableId}`} · Ofisiant: {detail?.waiterName ?? "-"} · Oturdu:{" "}
              {clock(detail?.openedAt)}
            </DialogDescription>
          </DialogHeader>
          {detail && (
            <div className="space-y-3">
              <ReceiptLinesTable lines={detail.lines} cancellations={detailCancellations} />
              <div className="flex items-center justify-between text-sm">
                <span className="text-muted-foreground">
                  Ödəniş: {new Date(detail.paidAt ?? detail.closedAt ?? detail.openedAt).toLocaleString("az-AZ")}
                </span>
                <span className="text-base font-semibold">Toplam: {formatCurrency(detail.totalAmount)}</span>
              </div>
            </div>
          )}
        </DialogContent>
      </Dialog>
    </div>
  );
}
