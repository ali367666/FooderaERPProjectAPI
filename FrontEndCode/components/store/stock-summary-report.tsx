"use client";

import { Fragment, useCallback, useEffect, useState } from "react";
import { ChevronDown, ChevronRight } from "lucide-react";
import { toast } from "sonner";
import { Button } from "@/components/ui/button";
import { downloadCsv } from "@/lib/store/csv";
import { getStoreStockSummary, type StoreStockSummary, type StoreStockSummaryRow } from "@/lib/store/store-service";

const qty = (v: number) => v.toLocaleString("az-AZ", { minimumFractionDigits: 0, maximumFractionDigits: 3 });
const money = (v: number) => v.toLocaleString("az-AZ", { minimumFractionDigits: 2, maximumFractionDigits: 2 });

function Cells({ r, bold }: { r: StoreStockSummaryRow; bold?: boolean }) {
  const cls = bold ? "px-2 py-1.5 text-right tabular-nums font-semibold" : "px-2 py-1.5 text-right tabular-nums";
  return (
    <>
      <td className={cls}>{qty(r.openingQty)}</td>
      <td className={cls}>{money(r.openingValue)}</td>
      <td className={cls}>{qty(r.inQty)}</td>
      <td className={cls}>{money(r.inValue)}</td>
      <td className={cls}>{qty(r.outQty)}</td>
      <td className={cls}>{money(r.outValue)}</td>
      <td className={cls}>{qty(r.closingQty)}</td>
      <td className={cls}>{money(r.closingValue)}</td>
      <td className={cls}>{r.productId ? money(r.purchasePrice) : ""}</td>
      <td className={cls}>{r.productId ? money(r.salePrice) : ""}</td>
      <td className={cls}>{money(r.saleAmount)}</td>
    </>
  );
}

/** "Ümumi stok": every warehouse with its products — opening balance, income, outcome and closing balance. */
export function StockSummaryReport({
  from,
  to,
  warehouseId,
  categoryId,
}: {
  from: string;
  to: string;
  warehouseId: number | "";
  categoryId: number | "";
}) {
  const [report, setReport] = useState<StoreStockSummary | null>(null);
  const [loading, setLoading] = useState(false);
  const [collapsed, setCollapsed] = useState<Set<number>>(new Set());

  const load = useCallback(async () => {
    setLoading(true);
    try {
      setReport(await getStoreStockSummary({ from, to, warehouseId: warehouseId || null, categoryId: categoryId || null }));
      setCollapsed(new Set());
    } catch (e) {
      toast.error(e instanceof Error ? e.message : "Hesabat yüklənmədi");
    } finally {
      setLoading(false);
    }
  }, [from, to, warehouseId, categoryId]);

  useEffect(() => {
    void load();
  }, [load]);

  const toggle = (id: number) =>
    setCollapsed((prev) => {
      const next = new Set(prev);
      if (next.has(id)) next.delete(id);
      else next.add(id);
      return next;
    });

  const exportCsv = () => {
    if (!report) return;
    const line = (name: string, barcode: string, r: StoreStockSummaryRow, withPrices: boolean) => [
      name, barcode, r.openingQty, r.openingValue, r.inQty, r.inValue, r.outQty, r.outValue, r.closingQty, r.closingValue,
      withPrices ? r.purchasePrice : "", withPrices ? r.salePrice : "", r.saleAmount,
    ];
    downloadCsv(`umumi-stok-${from}-${to}.csv`, [
      ["Mal", "Barkod", "İlkin qalıq miqdar", "İlkin qalıq dəyər", "Mədaxil miqdar", "Mədaxil dəyər", "Məxaric miqdar", "Məxaric dəyər", "Son qalıq miqdar", "Son qalıq dəyər", "Alış qiyməti", "Satış qiyməti", "Məbləğ"],
      line("Cəmi", "", report.total, false),
      ...report.warehouses.flatMap((w) => [
        line(w.warehouseName, "", w.total, false),
        ...w.rows.map((r) => line(`  ${r.productName}`, r.barcode ?? "", r, true)),
      ]),
    ]);
  };

  return (
    <div className="space-y-2">
      <div className="flex flex-wrap items-center gap-2">
        <Button size="sm" onClick={() => void load()} disabled={loading}>
          Tərtib et
        </Button>
        <Button size="sm" variant="outline" onClick={exportCsv} disabled={!report}>
          Excel/CSV ixrac
        </Button>
        <span className="text-sm text-muted-foreground">
          {from} – {to}
        </span>
      </div>

      {loading && <p className="text-sm text-muted-foreground">Yüklənir…</p>}

      {report && !loading && (
        <div className="overflow-x-auto rounded-lg border bg-card">
          <table className="w-full text-sm">
            <thead>
              <tr className="bg-muted/40 text-xs text-muted-foreground">
                <th rowSpan={2} className="px-3 py-2 text-left font-medium">
                  Mal / Barkod
                </th>
                <th colSpan={2} className="border-l px-2 py-1 text-center font-medium">
                  İlkin qalıq
                </th>
                <th colSpan={2} className="border-l px-2 py-1 text-center font-medium">
                  Mədaxil
                </th>
                <th colSpan={2} className="border-l px-2 py-1 text-center font-medium">
                  Məxaric
                </th>
                <th colSpan={2} className="border-l px-2 py-1 text-center font-medium">
                  Son qalıq
                </th>
                <th rowSpan={2} className="border-l px-2 py-2 text-right font-medium">
                  Alış qiyməti
                </th>
                <th rowSpan={2} className="px-2 py-2 text-right font-medium">
                  Satış qiyməti
                </th>
                <th rowSpan={2} className="px-2 py-2 text-right font-medium">
                  Məbləğ
                </th>
              </tr>
              <tr className="bg-muted/40 text-xs text-muted-foreground">
                {["Miqdar", "Dəyər", "Miqdar", "Dəyər", "Miqdar", "Dəyər", "Miqdar", "Dəyər"].map((h, i) => (
                  <th key={i} className={`px-2 py-1 text-right font-medium ${i % 2 === 0 ? "border-l" : ""}`}>
                    {h}
                  </th>
                ))}
              </tr>
            </thead>
            <tbody>
              <tr className="border-t bg-muted/30 font-bold">
                <td className="px-3 py-2 text-base">Cəmi</td>
                <Cells r={report.total} bold />
              </tr>
              {report.warehouses.length === 0 && (
                <tr>
                  <td colSpan={12} className="px-3 py-8 text-center text-muted-foreground">
                    Bu dövr üçün stok hərəkəti yoxdur.
                  </td>
                </tr>
              )}
              {report.warehouses.map((w) => (
                <Fragment key={w.warehouseId}>
                  <tr className="border-t bg-muted/10 font-semibold">
                    <td className="px-3 py-1.5">
                      <button type="button" className="mr-1 inline-flex align-middle" onClick={() => toggle(w.warehouseId)} aria-label="Aç / bağla">
                        {collapsed.has(w.warehouseId) ? <ChevronRight className="h-4 w-4" /> : <ChevronDown className="h-4 w-4" />}
                      </button>
                      {w.warehouseName}
                    </td>
                    <Cells r={w.total} bold />
                  </tr>
                  {!collapsed.has(w.warehouseId) &&
                    w.rows.map((r) => (
                      <tr key={r.productId} className="border-t">
                        <td className="px-3 py-1.5 pl-9">
                          {r.productName}
                          {r.barcode && <span className="block text-xs text-muted-foreground">{r.barcode}</span>}
                        </td>
                        <Cells r={r} />
                      </tr>
                    ))}
                </Fragment>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}
