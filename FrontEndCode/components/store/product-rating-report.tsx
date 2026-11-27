"use client";

import { useCallback, useEffect, useState } from "react";
import { toast } from "sonner";
import { Button } from "@/components/ui/button";
import { cn } from "@/lib/utils";
import { downloadCsv } from "@/lib/store/csv";
import { getStoreProductRating, type StoreProductRating } from "@/lib/store/store-service";

const qty = (v: number) => v.toLocaleString("az-AZ", { minimumFractionDigits: 0, maximumFractionDigits: 3 });
const money = (v: number) => v.toLocaleString("az-AZ", { minimumFractionDigits: 2, maximumFractionDigits: 2 });

const TAKES = [50, 100, 500, 1000];

/** "Reytinq": sold products ranked by quantity, most sold first or least sold first. */
export function ProductRatingReport({ from, to }: { from: string; to: string }) {
  const [least, setLeast] = useState(false);
  const [take, setTake] = useState(100);
  const [report, setReport] = useState<StoreProductRating | null>(null);
  const [loading, setLoading] = useState(false);

  const load = useCallback(async () => {
    setLoading(true);
    try {
      setReport(await getStoreProductRating({ from, to, least, take }));
    } catch (e) {
      toast.error(e instanceof Error ? e.message : "Reytinq yüklənmədi");
    } finally {
      setLoading(false);
    }
  }, [from, to, least, take]);

  useEffect(() => {
    void load();
  }, [load]);

  const exportCsv = () => {
    if (!report) return;
    downloadCsv(`reytinq-${from}-${to}.csv`, [
      ["№", "Xüsusi kod", "Barkod", "Mal", "Satış qiyməti", "Miqdar", "Məbləğ"],
      ...report.rows.map((r) => [r.rank, r.sku ?? "", r.barcode ?? "", r.productName, r.salePrice, r.quantity, r.amount]),
    ]);
  };

  return (
    <div className="space-y-2">
      <div className="flex flex-wrap items-center gap-3">
        <div className="flex rounded-md border">
          <button type="button" onClick={() => setLeast(false)} className={cn("px-3 py-1.5 text-sm", !least ? "bg-primary text-primary-foreground" : "hover:bg-muted")}>
            Ən çox satılan
          </button>
          <button type="button" onClick={() => setLeast(true)} className={cn("px-3 py-1.5 text-sm", least ? "bg-primary text-primary-foreground" : "hover:bg-muted")}>
            Ən az satılan
          </button>
        </div>
        <label className="flex items-center gap-2 text-sm">
          Say
          <select className="h-8 rounded-md border border-input bg-background px-2 text-sm" value={take} onChange={(e) => setTake(Number(e.target.value))}>
            {TAKES.map((t) => (
              <option key={t} value={t}>
                {t}
              </option>
            ))}
          </select>
        </label>
        <Button size="sm" variant="outline" onClick={exportCsv} disabled={!report}>
          Excel/CSV ixrac
        </Button>
        {report && (
          <span className="text-sm text-muted-foreground">
            {report.productCount} məhsul satılıb · cəmi {qty(report.totalQuantity)} ədəd · {money(report.totalAmount)} ₼
          </span>
        )}
      </div>

      {loading && <p className="text-sm text-muted-foreground">Yüklənir…</p>}

      {report && !loading && (
        <div className="overflow-x-auto rounded-lg border bg-card">
          <table className="w-full text-sm">
            <thead>
              <tr className="bg-muted/40 text-left text-xs text-muted-foreground">
                <th className="w-12 px-3 py-2 font-medium">№</th>
                <th className="px-3 py-2 font-medium">Xüsusi kod</th>
                <th className="px-3 py-2 font-medium">Barkod</th>
                <th className="px-3 py-2 font-medium">Malın adı</th>
                <th className="px-3 py-2 text-right font-medium">Satış qiyməti</th>
                <th className="px-3 py-2 text-right font-medium">Miqdar</th>
                <th className="px-3 py-2 text-right font-medium">Məbləğ</th>
              </tr>
            </thead>
            <tbody>
              {report.rows.length === 0 ? (
                <tr>
                  <td colSpan={7} className="px-3 py-8 text-center text-muted-foreground">
                    Bu dövrdə satış yoxdur.
                  </td>
                </tr>
              ) : (
                report.rows.map((r) => (
                  <tr key={r.productId} className="border-t">
                    <td className="px-3 py-1.5 text-muted-foreground">{r.rank}</td>
                    <td className="px-3 py-1.5">{r.sku ?? "—"}</td>
                    <td className="px-3 py-1.5">{r.barcode ?? "—"}</td>
                    <td className="px-3 py-1.5 font-medium">{r.productName}</td>
                    <td className="px-3 py-1.5 text-right tabular-nums">{money(r.salePrice)}</td>
                    <td className="px-3 py-1.5 text-right tabular-nums font-semibold">{qty(r.quantity)}</td>
                    <td className="px-3 py-1.5 text-right tabular-nums">{money(r.amount)}</td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}
