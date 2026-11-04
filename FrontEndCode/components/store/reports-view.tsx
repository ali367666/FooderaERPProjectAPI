"use client";

import { useCallback, useEffect, useState } from "react";
import { toast } from "sonner";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { downloadCsv } from "@/lib/store/csv";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { SalesMatrixReport } from "@/components/store/sales-matrix-report";
import { CashSummaryReport } from "@/components/store/cash-summary-report";
import { ProductRatingReport } from "@/components/store/product-rating-report";
import { StockSummaryReport } from "@/components/store/stock-summary-report";
import { useStorePermissions as usePermissionSet } from "@/lib/store/use-store-permissions";
import { formatCurrency } from "@/lib/format-currency";
import { toDateInputValue } from "@/lib/report-period";
import { cn } from "@/lib/utils";
import { getWarehouses, type Warehouse } from "@/lib/services/warehouse-service";
import {
  MOVEMENT_LABELS,
  PAYMENT_LABELS,
  getStoreBonusReport,
  getStoreCashReport,
  getStoreCategories,
  getStoreCustomerDebtReport,
  getStoreSalesReport,
  getStoreStockReport,
  searchStoreReturns,
  searchStoreSales,
  type StoreBonusReport,
  type StoreCashReport,
  type StoreCategory,
  type StoreCustomerDebtReport,
  type StoreReturn,
  type StoreSaleListItem,
  type StoreReportRow,
  type StoreSalesReport,
  type StoreStockReport,
} from "@/lib/store/store-service";

export type ReportTab = "matrix" | "cashsum" | "rating" | "stocksum" | "sales" | "stock" | "cash" | "debts" | "bonus" | "docs";

const fmtDate = (v: string) => new Date(v).toLocaleString("az-AZ");

function Stat({ label, value, tone }: { label: string; value: string; tone?: "bad" | "good" }) {
  return (
    <div className="rounded-lg border bg-card p-3">
      <p className="text-xs text-muted-foreground">{label}</p>
      <p className={cn("mt-1 text-lg font-semibold", tone === "bad" && "text-red-600", tone === "good" && "text-emerald-600")}>{value}</p>
    </div>
  );
}

/** A grouped list: name, receipts, quantity, amount, and optionally cost and profit. */
function RowsTable({ title, rows, withProfit, paymentNames }: { title: string; rows: StoreReportRow[]; withProfit: boolean; paymentNames?: boolean }) {
  return (
    <div className="rounded-lg border bg-card p-3">
      <h3 className="mb-2 text-sm font-semibold">{title}</h3>
      {rows.length === 0 ? (
        <p className="py-4 text-center text-sm text-muted-foreground">Məlumat yoxdur.</p>
      ) : (
        <div className="max-h-80 overflow-auto">
          <table className="w-full text-sm">
            <thead>
              <tr className="text-left text-xs text-muted-foreground">
                <th className="py-1 font-medium">Ad</th>
                <th className="py-1 text-right font-medium">Say</th>
                {!paymentNames && <th className="py-1 text-right font-medium">Miqdar</th>}
                <th className="py-1 text-right font-medium">Məbləğ</th>
                {withProfit && <th className="py-1 text-right font-medium">Maya</th>}
                {withProfit && <th className="py-1 text-right font-medium">Mənfəət</th>}
              </tr>
            </thead>
            <tbody>
              {rows.map((r) => (
                <tr key={r.key} className="border-t">
                  <td className="py-1">{paymentNames ? (PAYMENT_LABELS[Number(r.key)] ?? r.name) : r.name}</td>
                  <td className="py-1 text-right">{r.count}</td>
                  {!paymentNames && <td className="py-1 text-right">{r.quantity}</td>}
                  <td className="py-1 text-right font-medium">{formatCurrency(r.amount)}</td>
                  {withProfit && <td className="py-1 text-right text-muted-foreground">{formatCurrency(r.cost)}</td>}
                  {withProfit && <td className={cn("py-1 text-right", r.profit < 0 && "text-red-600")}>{formatCurrency(r.profit)}</td>}
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}

type Tab = ReportTab;

/** `fixedTab` shows that one report alone under its own heading (a sidebar section); without it all reports are tabs. */
export function StoreReportsView({ fixedTab, title }: { fixedTab?: ReportTab; title?: string } = {}) {
  const permissions = usePermissionSet();
  const canFinance = permissions.has("StoreReport.Finance");

  const today = toDateInputValue(new Date());
  const [tab, setTab] = useState<Tab>(fixedTab ?? (canFinance ? "matrix" : "stock"));
  const [from, setFrom] = useState(toDateInputValue(new Date(new Date().getFullYear(), new Date().getMonth(), 1)));
  const [to, setTo] = useState(today);
  const [warehouseId, setWarehouseId] = useState<number | "">("");
  const [categoryId, setCategoryId] = useState<number | "">("");
  const [warehouses, setWarehouses] = useState<Warehouse[]>([]);
  const [categories, setCategories] = useState<StoreCategory[]>([]);
  const [loading, setLoading] = useState(false);

  const [sales, setSales] = useState<StoreSalesReport | null>(null);
  const [stock, setStock] = useState<StoreStockReport | null>(null);
  const [cash, setCash] = useState<StoreCashReport | null>(null);
  const [debts, setDebts] = useState<StoreCustomerDebtReport | null>(null);
  const [bonus, setBonus] = useState<StoreBonusReport | null>(null);
  const [includeUnsold, setIncludeUnsold] = useState(false);
  const [docStatus, setDocStatus] = useState<number | "">("");
  const [docSales, setDocSales] = useState<StoreSaleListItem[]>([]);
  const [docReturns, setDocReturns] = useState<StoreReturn[]>([]);
  const [productSort, setProductSort] = useState<"amount" | "quantity" | "least">("amount");

  useEffect(() => {
    getWarehouses().then(setWarehouses).catch(() => setWarehouses([]));
    getStoreCategories().then(setCategories).catch(() => setCategories([]));
  }, []);

  const load = useCallback(async () => {
    setLoading(true);
    try {
      if (tab === "sales") {
        setSales(
          await getStoreSalesReport({
            from,
            to,
            warehouseId: warehouseId === "" ? null : warehouseId,
            categoryId: categoryId === "" ? null : categoryId,
            includeUnsold,
          }),
        );
      } else if (tab === "stock") {
        setStock(await getStoreStockReport({ from, to, warehouseId: warehouseId === "" ? null : warehouseId, categoryId: categoryId === "" ? null : categoryId }));
      } else if (tab === "cash") {
        setCash(await getStoreCashReport({ from, to }));
      } else if (tab === "debts") {
        setDebts(await getStoreCustomerDebtReport());
      } else if (tab === "docs") {
        const [sales, returns] = await Promise.all([
          searchStoreSales({ from, to, status: docStatus === "" ? null : docStatus, page: 1, pageSize: 200 }),
          searchStoreReturns({ from, to }),
        ]);
        setDocSales(sales.items);
        setDocReturns(returns);
      } else {
        setBonus(await getStoreBonusReport({ from, to }));
      }
    } catch (e) {
      toast.error(e instanceof Error ? e.message : "Hesabat yüklənmədi.");
    } finally {
      setLoading(false);
    }
  }, [tab, from, to, warehouseId, categoryId, includeUnsold, docStatus]);

  useEffect(() => {
    void load();
  }, [load]);

  const periods = [
    { label: "Bu gün", set: () => { setFrom(today); setTo(today); } },
    { label: "Dünən", set: () => { const d = toDateInputValue(new Date(Date.now() - 86_400_000)); setFrom(d); setTo(d); } },
    { label: "Bu həftə", set: () => { const n = new Date(); const monday = new Date(n); monday.setDate(n.getDate() - ((n.getDay() + 6) % 7)); setFrom(toDateInputValue(monday)); setTo(today); } },
    { label: "Bu ay", set: () => { setFrom(toDateInputValue(new Date(new Date().getFullYear(), new Date().getMonth(), 1))); setTo(today); } },
    { label: "Bu il", set: () => { setFrom(toDateInputValue(new Date(new Date().getFullYear(), 0, 1))); setTo(today); } },
    { label: "Bütün dövr", set: () => { setFrom("2020-01-01"); setTo(today); } },
  ];

  const tabs: { key: Tab; label: string; needsFinance: boolean }[] = [
    { key: "matrix", label: "Satış hesabatı", needsFinance: true },
    { key: "sales", label: "Satış və mənfəət", needsFinance: true },
    { key: "cashsum", label: "Kassa hesabatı", needsFinance: false },
    { key: "rating", label: "Reytinq", needsFinance: false },
    { key: "stocksum", label: "Ümumi stok", needsFinance: false },
    { key: "stock", label: "Stok", needsFinance: false },
    { key: "cash", label: "Kassa", needsFinance: false },
    { key: "debts", label: "Müştəri borcları", needsFinance: true },
    { key: "bonus", label: "Bonus", needsFinance: true },
    { key: "docs", label: "Çeklər, ləğvlər, qaytarmalar", needsFinance: false },
  ];

  const exportCsv = () => {
    const fmt = (d: string) => new Date(d).toLocaleString("az-AZ");
    const rowsOf = (title: string, rows: StoreReportRow[]) => [[title], ["Ad", "Say", "Miqdar", "Məbləğ", "Maya", "Mənfəət"], ...rows.map((r) => [r.name, r.count, r.quantity, r.amount, r.cost, r.profit]), []];
    if (tab === "sales" && sales) {
      downloadCsv(`satis-${from}-${to}.csv`, [
        ["Dövr", from, to],
        ["Ümumi satış", sales.grossSales],
        ["Endirim", sales.discounts],
        ["Xalis satış", sales.netSales],
        ["Qaytarma", sales.returnsTotal],
        ["Maya", sales.costOfGoods],
        ["Mənfəət", sales.profit],
        [],
        ...rowsOf("Günlər üzrə", sales.byDay),
        ...rowsOf("Kateqoriya üzrə", sales.byCategory),
        ...rowsOf("Kassir üzrə", sales.byCashier),
        ...rowsOf("Kassa üzrə", sales.byRegister),
        ...rowsOf("Məhsullar üzrə", sales.byProduct),
      ]);
    } else if (tab === "stock" && stock) {
      downloadCsv(`stok-${to}.csv`, [
        ["Anbar", "Məhsul", "SKU", "Qalıq", "Minimum"],
        ...stock.rows.map((r) => [r.warehouseName, r.productName, r.sku, r.quantity, r.minimumStock]),
      ]);
    } else if (tab === "cash" && cash) {
      downloadCsv(`kassa-${from}-${to}.csv`, [
        ["Açılış", "Kassa", "Kassir", "Gözlənilən", "Faktiki", "Fərq"],
        ...cash.shifts.map((s) => [fmt(s.openedAtUtc), s.registerName, s.cashierName, s.expectedCash, s.actualCash, s.cashDifference]),
      ]);
    } else if (tab === "debts" && debts) {
      downloadCsv("musteri-borclari.csv", [
        ["Müştəri", "Borc", "0–30", "31–60", "61–90", "90+"],
        ...debts.customers.map((c) => [c.customerName, c.debt, c.age0To30, c.age31To60, c.age61To90, c.age90Plus]),
      ]);
    } else if (tab === "bonus" && bonus) {
      downloadCsv(`bonus-${from}-${to}.csv`, [
        ["Kontragent", "İlkin məbləğ", "Məbləğin artması", "Verilən bonus məbləği", "Son məbləğ", "Bonus qalığı"],
        ...bonus.customers.map((c) => [c.customerName, c.opening, c.increase, c.decrease, c.closing, c.current]),
      ]);
    } else if (tab === "docs") {
      downloadCsv(`senedler-${from}-${to}.csv`, [
        ["Çek", "Tarix", "Status", "Kassir", "Kassa", "Məbləğ"],
        ...docSales.map((s) => [s.documentNo, fmt(s.createdAtUtc), s.status === 2 ? "Ləğv" : "Tamamlanıb", s.cashierName, s.registerName, s.total]),
        [],
        ["Qaytarma", "Tarix", "Çek", "Səbəb", "Məbləğ"],
        ...docReturns.map((r) => [r.documentNo, fmt(r.createdAtUtc), r.saleDocumentNo, r.reason, r.totalAmount]),
      ]);
    }
  };

  const products = (() => {
    const rows = [...(sales?.byProduct ?? [])];
    if (productSort === "quantity") rows.sort((a, b) => b.quantity - a.quantity);
    else if (productSort === "least") rows.sort((a, b) => a.amount - b.amount);
    return rows.slice(0, 50);
  })();

  return (
    <div className="mx-auto max-w-6xl space-y-4 p-4">
      <h1 className="text-2xl font-bold">{title ?? "Hesabatlar"}</h1>

      <div className={cn("flex flex-wrap rounded-md border", fixedTab && "hidden")}>
        {tabs
          .filter((t) => !t.needsFinance || canFinance)
          .map((t) => (
            <button
              key={t.key}
              onClick={() => setTab(t.key)}
              className={cn("px-3 py-1.5 text-sm", tab === t.key ? "bg-primary text-primary-foreground" : "hover:bg-muted")}
            >
              {t.label}
            </button>
          ))}
      </div>

      {tab !== "matrix" && tab !== "stocksum" && tab !== "rating" && tab !== "cashsum" && (
        <div className="flex justify-end">
          <Button variant="outline" size="sm" onClick={exportCsv}>
            Excel/CSV ixrac
          </Button>
        </div>
      )}

      {tab !== "debts" && (
        <div className="flex flex-wrap items-end gap-3 rounded-lg border bg-card p-3">
          <div className="flex flex-wrap gap-1">
            {periods.map((p) => (
              <button key={p.label} onClick={p.set} className="rounded-full border px-3 py-1 text-xs hover:bg-muted">
                {p.label}
              </button>
            ))}
          </div>
          <div className="space-y-1">
            <Label htmlFor="rp-from">Başlanğıc</Label>
            <Input id="rp-from" type="date" value={from} max={to} onChange={(e) => setFrom(e.target.value)} />
          </div>
          <div className="space-y-1">
            <Label htmlFor="rp-to">Son</Label>
            <Input id="rp-to" type="date" value={to} min={from} onChange={(e) => setTo(e.target.value)} />
          </div>
          {(tab === "sales" || tab === "stock" || tab === "stocksum") && (
            <>
              <div className="space-y-1">
                <Label htmlFor="rp-wh">Anbar</Label>
                <select id="rp-wh" className="h-9 rounded-md border border-input bg-background px-3 text-sm" value={warehouseId} onChange={(e) => setWarehouseId(e.target.value ? Number(e.target.value) : "")}>
                  <option value="">Hamısı</option>
                  {warehouses.map((w) => (
                    <option key={w.id} value={w.id}>
                      {w.name}
                    </option>
                  ))}
                </select>
              </div>
              <div className="space-y-1">
                <Label htmlFor="rp-cat">Kateqoriya</Label>
                <select id="rp-cat" className="h-9 rounded-md border border-input bg-background px-3 text-sm" value={categoryId} onChange={(e) => setCategoryId(e.target.value ? Number(e.target.value) : "")}>
                  <option value="">Hamısı</option>
                  {categories.map((c) => (
                    <option key={c.id} value={c.id}>
                      {c.name}
                    </option>
                  ))}
                </select>
              </div>
            </>
          )}
          {tab === "sales" && (
            <label className="flex items-center gap-2 pb-2 text-sm">
              <input type="checkbox" checked={includeUnsold} onChange={(e) => setIncludeUnsold(e.target.checked)} />
              Satılmayan məhsullar
            </label>
          )}
        </div>
      )}

      {tab === "matrix" && canFinance && <SalesMatrixReport from={from} to={to} />}
      {tab === "cashsum" && <CashSummaryReport from={from} to={to} />}
      {tab === "rating" && <ProductRatingReport from={from} to={to} />}
      {tab === "stocksum" && <StockSummaryReport from={from} to={to} warehouseId={warehouseId} categoryId={categoryId} />}

      {loading && <p className="text-sm text-muted-foreground">Yüklənir…</p>}

      {/* Sales and profit */}
      {tab === "sales" && sales && !loading && (
        <div className="space-y-4">
          <div className="grid grid-cols-2 gap-3 md:grid-cols-4">
            <Stat label="Satış sayı" value={String(sales.salesCount)} />
            <Stat label="Ümumi satış" value={formatCurrency(sales.grossSales)} />
            <Stat label="Endirim" value={formatCurrency(sales.discounts)} />
            <Stat label="Xalis satış" value={formatCurrency(sales.netSales)} />
            <Stat label="Qaytarma" value={`${formatCurrency(sales.returnsTotal)} (${sales.returnsCount})`} />
            <Stat label="Maya dəyəri" value={formatCurrency(sales.costOfGoods)} />
            <Stat label="Mənfəət" value={formatCurrency(sales.profit)} tone={sales.profit < 0 ? "bad" : "good"} />
            <Stat label="Orta çek" value={formatCurrency(sales.averageReceipt)} />
          </div>
          <p className="text-xs text-muted-foreground">
            Mənfəət = xalis satış − qaytarma − satılan malın maya dəyəri (satış anındakı orta maya). Ləğv edilən: {sales.cancelledCount} çek,{" "}
            {formatCurrency(sales.cancelledTotal)}. ƏDV (daxil): {formatCurrency(sales.vat)}.
          </p>

          <div className="grid gap-4 lg:grid-cols-2">
            <RowsTable title="Günlər üzrə" rows={sales.byDay} withProfit />
            <RowsTable title="Ödəniş üsulu üzrə" rows={sales.byPayment} withProfit={false} paymentNames />
            <RowsTable title="Kateqoriya üzrə" rows={sales.byCategory} withProfit />
            <RowsTable title="Kassir üzrə" rows={sales.byCashier} withProfit />
            <RowsTable title="Kassa üzrə" rows={sales.byRegister} withProfit />
            <div>
              <div className="mb-2 flex items-center gap-2 text-xs">
                <span className="text-muted-foreground">Məhsullar üzrə sıralama:</span>
                {(
                  [
                    ["amount", "Məbləğə görə"],
                    ["quantity", "Miqdara görə"],
                    ["least", "Ən az satılan"],
                  ] as const
                ).map(([k, label]) => (
                  <button key={k} onClick={() => setProductSort(k)} className={cn("rounded-full border px-2 py-0.5", productSort === k && "bg-primary text-primary-foreground")}>
                    {label}
                  </button>
                ))}
              </div>
              <RowsTable title="Məhsullar üzrə (satılanlar)" rows={products} withProfit />
            </div>
          </div>

          {includeUnsold && (
            <div className="rounded-lg border bg-card p-3">
              <h3 className="mb-2 text-sm font-semibold">Bu dövrdə heç satılmayan aktiv məhsullar</h3>
              {sales.unsold.length === 0 ? (
                <p className="text-sm text-muted-foreground">Hamısı satılıb.</p>
              ) : (
                <div className="max-h-72 overflow-auto">
                  <table className="w-full text-sm">
                    <tbody>
                      {sales.unsold.map((u) => (
                        <tr key={u.productId} className="border-t">
                          <td className="py-1">{u.productName}</td>
                          <td className="py-1 text-muted-foreground">{u.categoryName ?? "—"}</td>
                          <td className="py-1 text-right">stok: {u.stock}</td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              )}
            </div>
          )}
        </div>
      )}

      {/* Stock */}
      {tab === "stock" && stock && !loading && (
        <div className="space-y-4">
          <div className="grid grid-cols-3 gap-3">
            <Stat label="Stokun dəyəri" value={formatCurrency(stock.totalValue)} />
            <Stat label="Məhsul sayı" value={String(stock.rows.length)} />
            <Stat label="Minimumdan aşağı" value={String(stock.lowStockCount)} tone={stock.lowStockCount > 0 ? "bad" : undefined} />
          </div>
          <div className="grid gap-4 lg:grid-cols-2">
            <RowsTable title="Anbar üzrə (dəyər)" rows={stock.byWarehouse} withProfit={false} />
            <div className="rounded-lg border bg-card p-3">
              <h3 className="mb-2 text-sm font-semibold">Dövr üzrə hərəkətlər (növə görə)</h3>
              <table className="w-full text-sm">
                <thead>
                  <tr className="text-left text-xs text-muted-foreground">
                    <th className="py-1 font-medium">Növ</th>
                    <th className="py-1 text-right font-medium">Say</th>
                    <th className="py-1 text-right font-medium">Miqdar</th>
                    <th className="py-1 text-right font-medium">Dəyər</th>
                  </tr>
                </thead>
                <tbody>
                  {stock.movementsByType.map((m) => (
                    <tr key={m.key} className="border-t">
                      <td className="py-1">{MOVEMENT_LABELS[Number(m.key)] ?? m.name}</td>
                      <td className="py-1 text-right">{m.count}</td>
                      <td className={cn("py-1 text-right", m.quantity < 0 ? "text-red-600" : "text-emerald-600")}>{m.quantity}</td>
                      <td className="py-1 text-right">{formatCurrency(Math.abs(m.amount))}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </div>
          <div className="rounded-lg border bg-card p-3">
            <h3 className="mb-2 text-sm font-semibold">Minimum stokdan aşağı məhsullar</h3>
            {stock.belowMinimum.length === 0 ? (
              <p className="text-sm text-muted-foreground">Yoxdur.</p>
            ) : (
              <table className="w-full text-sm">
                <tbody>
                  {stock.belowMinimum.map((b) => (
                    <tr key={`${b.warehouseId}-${b.productId}`} className="border-t">
                      <td className="py-1">{b.productName}</td>
                      <td className="py-1 text-muted-foreground">{b.warehouseName}</td>
                      <td className="py-1 text-right font-medium text-red-600">{b.quantity}</td>
                      <td className="py-1 text-right text-muted-foreground">minimum {b.minimumStock}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            )}
          </div>
        </div>
      )}

      {/* Cash */}
      {tab === "cash" && cash && !loading && (
        <div className="space-y-4">
          <div className="grid grid-cols-3 gap-3">
            <Stat label="Mədaxil" value={formatCurrency(cash.totalIn)} tone="good" />
            <Stat label="Məxaric" value={formatCurrency(cash.totalOut)} />
            <Stat label="Sayım fərqi (cəmi)" value={formatCurrency(cash.totalDifference)} tone={cash.totalDifference < 0 ? "bad" : undefined} />
          </div>
          <div className="rounded-lg border bg-card p-3">
            <h3 className="mb-2 text-sm font-semibold">Növbələr: gözlənilən və faktiki nağd</h3>
            <div className="overflow-x-auto">
              <table className="w-full text-sm">
                <thead>
                  <tr className="text-left text-xs text-muted-foreground">
                    <th className="py-1 font-medium">Açılış</th>
                    <th className="py-1 font-medium">Kassa</th>
                    <th className="py-1 font-medium">Kassir</th>
                    <th className="py-1 text-right font-medium">Gözlənilən</th>
                    <th className="py-1 text-right font-medium">Faktiki</th>
                    <th className="py-1 text-right font-medium">Fərq</th>
                  </tr>
                </thead>
                <tbody>
                  {cash.shifts.map((s) => (
                    <tr key={s.shiftId} className="border-t">
                      <td className="py-1">{fmtDate(s.openedAtUtc)}</td>
                      <td className="py-1">{s.registerName}</td>
                      <td className="py-1">{s.cashierName ?? "—"}</td>
                      <td className="py-1 text-right">{s.expectedCash == null ? <Badge>açıq</Badge> : formatCurrency(s.expectedCash)}</td>
                      <td className="py-1 text-right">{s.actualCash == null ? "—" : formatCurrency(s.actualCash)}</td>
                      <td className={cn("py-1 text-right font-medium", (s.cashDifference ?? 0) < 0 && "text-red-600", (s.cashDifference ?? 0) > 0 && "text-amber-600")}>
                        {s.cashDifference == null ? "—" : formatCurrency(s.cashDifference)}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </div>
          <div className="rounded-lg border bg-card p-3">
            <h3 className="mb-2 text-sm font-semibold">Kassa mədaxil / məxaric</h3>
            {cash.movements.length === 0 ? (
              <p className="text-sm text-muted-foreground">Hərəkət yoxdur.</p>
            ) : (
              cash.movements.map((m) => (
                <div key={m.id} className="flex justify-between border-t py-1.5 text-sm">
                  <span>
                    {fmtDate(m.createdAtUtc)} — {m.registerName} — {m.reason}
                    <span className="ml-1 text-xs text-muted-foreground">{m.userName}</span>
                  </span>
                  <span className={m.type === 1 ? "text-emerald-600" : "text-red-600"}>
                    {m.type === 1 ? "+" : "−"}
                    {formatCurrency(m.amount)}
                  </span>
                </div>
              ))
            )}
          </div>
        </div>
      )}

      {/* Customer debts */}
      {tab === "debts" && debts && !loading && (
        <div className="space-y-4">
          <div className="grid grid-cols-2 gap-3">
            <Stat label="Cəmi müştəri borcu" value={formatCurrency(debts.totalDebt)} tone={debts.totalDebt > 0 ? "bad" : undefined} />
            <Stat label="30 gündən köhnə" value={formatCurrency(debts.totalOver30)} tone={debts.totalOver30 > 0 ? "bad" : undefined} />
          </div>
          <div className="overflow-x-auto rounded-lg border bg-card">
            <table className="w-full text-sm">
              <thead>
                <tr className="bg-muted/40 text-left text-xs text-muted-foreground">
                  <th className="px-3 py-2 font-medium">Müştəri</th>
                  <th className="px-3 py-2 text-right font-medium">Borc</th>
                  <th className="px-3 py-2 text-right font-medium">0–30 gün</th>
                  <th className="px-3 py-2 text-right font-medium">31–60</th>
                  <th className="px-3 py-2 text-right font-medium">61–90</th>
                  <th className="px-3 py-2 text-right font-medium">90+</th>
                  <th className="px-3 py-2 font-medium">Ən köhnə</th>
                </tr>
              </thead>
              <tbody>
                {debts.customers.length === 0 ? (
                  <tr>
                    <td colSpan={7} className="px-3 py-8 text-center text-muted-foreground">
                      Borclu müştəri yoxdur.
                    </td>
                  </tr>
                ) : (
                  debts.customers.map((c) => (
                    <tr key={c.customerId} className="border-t">
                      <td className="px-3 py-2 font-medium">{c.customerName}</td>
                      <td className="px-3 py-2 text-right font-semibold">{formatCurrency(c.debt)}</td>
                      <td className="px-3 py-2 text-right">{formatCurrency(c.age0To30)}</td>
                      <td className={cn("px-3 py-2 text-right", c.age31To60 > 0 && "text-amber-600")}>{formatCurrency(c.age31To60)}</td>
                      <td className={cn("px-3 py-2 text-right", c.age61To90 > 0 && "text-orange-600")}>{formatCurrency(c.age61To90)}</td>
                      <td className={cn("px-3 py-2 text-right", c.age90Plus > 0 && "font-medium text-red-600")}>{formatCurrency(c.age90Plus)}</td>
                      <td className="px-3 py-2">{c.oldestOpenDate ? new Date(c.oldestOpenDate).toLocaleDateString("az-AZ") : "—"}</td>
                    </tr>
                  ))
                )}
              </tbody>
            </table>
          </div>
        </div>
      )}

      {/* Bonus */}
      {tab === "docs" && !loading && (
        <div className="space-y-4">
          <div className="flex items-center gap-2 text-sm">
            <Label htmlFor="doc-status">Çeklər</Label>
            <select id="doc-status" className="h-9 rounded-md border border-input bg-background px-3 text-sm" value={docStatus} onChange={(e) => setDocStatus(e.target.value ? Number(e.target.value) : "")}>
              <option value="">Hamısı</option>
              <option value={1}>Tamamlanıb</option>
              <option value={2}>Ləğv edilib</option>
            </select>
            <span className="text-muted-foreground">
              Ləğv: {docSales.filter((s) => s.status === 2).length} · Qaytarma: {docReturns.length}
            </span>
          </div>
          <div className="overflow-x-auto rounded-lg border bg-card">
            <table className="w-full text-sm">
              <thead>
                <tr className="bg-muted/40 text-left text-xs text-muted-foreground">
                  <th className="px-3 py-2 font-medium">Çek №</th>
                  <th className="px-3 py-2 font-medium">Tarix</th>
                  <th className="px-3 py-2 font-medium">Kassir</th>
                  <th className="px-3 py-2 font-medium">Kassa</th>
                  <th className="px-3 py-2 font-medium">Status</th>
                  <th className="px-3 py-2 text-right font-medium">Məbləğ</th>
                </tr>
              </thead>
              <tbody>
                {docSales.map((s) => (
                  <tr key={s.id} className="border-t">
                    <td className="px-3 py-2">{s.documentNo}</td>
                    <td className="px-3 py-2">{fmtDate(s.createdAtUtc)}</td>
                    <td className="px-3 py-2">{s.cashierName ?? "—"}</td>
                    <td className="px-3 py-2">{s.registerName ?? "—"}</td>
                    <td className="px-3 py-2">{s.status === 2 ? <Badge variant="destructive">Ləğv</Badge> : <Badge variant="secondary">Tamamlanıb</Badge>}</td>
                    <td className="px-3 py-2 text-right">{formatCurrency(s.total)}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
          <div className="rounded-lg border bg-card p-3">
            <h3 className="mb-2 text-sm font-semibold">Qaytarmalar</h3>
            {docReturns.length === 0 ? (
              <p className="text-sm text-muted-foreground">Qaytarma yoxdur.</p>
            ) : (
              docReturns.map((r) => (
                <div key={r.id} className="flex justify-between border-t py-1.5 text-sm">
                  <span>
                    {fmtDate(r.createdAtUtc)} — {r.documentNo} ({r.saleDocumentNo}) — {r.reason}
                  </span>
                  <span className="font-medium">{formatCurrency(r.totalAmount)}</span>
                </div>
              ))
            )}
          </div>
        </div>
      )}

      {tab === "bonus" && bonus && !loading && (
        <div className="space-y-2">
          <div>
            <p className="font-bold">Bonus hesabatı</p>
            <p className="text-sm text-muted-foreground">
              {from} – {to}
            </p>
          </div>
          <div className="overflow-x-auto rounded-lg border bg-card">
            <table className="w-full text-sm">
              <thead>
                <tr className="bg-muted/40 text-left text-xs text-muted-foreground">
                  <th className="px-3 py-2 font-medium">Kontragent</th>
                  <th className="px-3 py-2 text-right font-medium">İlkin məbləğ</th>
                  <th className="px-3 py-2 text-right font-medium">Məbləğin artması</th>
                  <th className="px-3 py-2 text-right font-medium">Verilən bonus məbləği</th>
                  <th className="px-3 py-2 text-right font-medium">Son məbləğ</th>
                  <th className="px-3 py-2 text-right font-medium">Bonus qalığı</th>
                </tr>
              </thead>
              <tbody>
                {bonus.customers.length === 0 ? (
                  <tr>
                    <td colSpan={6} className="px-3 py-8 text-center text-muted-foreground">
                      Bonusu olan müştəri yoxdur.
                    </td>
                  </tr>
                ) : (
                  <>
                    <tr className="border-t bg-muted/30 font-bold">
                      <td className="px-3 py-2">Cəmi</td>
                      {(["opening", "increase", "decrease", "closing", "current"] as const).map((k) => (
                        <td key={k} className="px-3 py-2 text-right tabular-nums">
                          {bonus.customers.reduce((sum, c) => sum + c[k], 0).toFixed(2)}
                        </td>
                      ))}
                    </tr>
                    {bonus.customers.map((c) => (
                      <tr key={c.customerId} className="border-t">
                        <td className="px-3 py-2 font-medium">{c.customerName}</td>
                        <td className="px-3 py-2 text-right tabular-nums">{c.opening.toFixed(2)}</td>
                        <td className="px-3 py-2 text-right tabular-nums">{c.increase.toFixed(2)}</td>
                        <td className="px-3 py-2 text-right tabular-nums">{c.decrease.toFixed(2)}</td>
                        <td className="px-3 py-2 text-right tabular-nums">{c.closing.toFixed(2)}</td>
                        <td className="px-3 py-2 text-right font-semibold tabular-nums">{c.current.toFixed(2)}</td>
                      </tr>
                    ))}
                  </>
                )}
              </tbody>
            </table>
          </div>
        </div>
      )}
    </div>
  );
}
