"use client";

import { useCallback, useEffect, useState } from "react";
import { toast } from "sonner";
import { Button } from "@/components/ui/button";
import { downloadCsv } from "@/lib/store/csv";
import { getStoreCashSummary, type StoreCashSummary } from "@/lib/store/store-service";

const money = (v: number) => v.toLocaleString("az-AZ", { minimumFractionDigits: 2, maximumFractionDigits: 2 });

/** "Kassa hesabatı": every till's cash — opening balance, money in, money out and closing balance. */
export function CashSummaryReport({ from, to }: { from: string; to: string }) {
  const [report, setReport] = useState<StoreCashSummary | null>(null);
  const [loading, setLoading] = useState(false);

  const load = useCallback(async () => {
    setLoading(true);
    try {
      setReport(await getStoreCashSummary({ from, to }));
    } catch (e) {
      toast.error(e instanceof Error ? e.message : "Kassa hesabatı yüklənmədi");
    } finally {
      setLoading(false);
    }
  }, [from, to]);

  useEffect(() => {
    void load();
  }, [load]);

  const exportCsv = () => {
    if (!report) return;
    downloadCsv(`kassa-hesabati-${from}-${to}.csv`, [
      ["Kassa", "İlkin qalıq", "Mədaxil", "Məxaric", "Son qalıq"],
      ...[report.total, ...report.registers].map((r) => [r.registerName, r.opening, r.income, r.outcome, r.closing]),
    ]);
  };

  return (
    <div className="space-y-2">
      <div className="flex flex-wrap items-center gap-2">
        <Button size="sm" variant="outline" onClick={exportCsv} disabled={!report}>
          Excel/CSV ixrac
        </Button>
        <span className="text-sm text-muted-foreground">
          {from} – {to} · yalnız nağd pul
        </span>
      </div>

      {loading && <p className="text-sm text-muted-foreground">Yüklənir…</p>}

      {report && !loading && (
        <div className="overflow-x-auto rounded-lg border bg-card">
          <table className="w-full text-sm">
            <thead>
              <tr className="bg-muted/40 text-left text-xs text-muted-foreground">
                <th className="px-3 py-2 font-medium">Kassa</th>
                <th className="px-3 py-2 text-right font-medium">İlkin qalıq</th>
                <th className="px-3 py-2 text-right font-medium">Mədaxil</th>
                <th className="px-3 py-2 text-right font-medium">Məxaric</th>
                <th className="px-3 py-2 text-right font-medium">Son qalıq</th>
              </tr>
            </thead>
            <tbody>
              {report.registers.length === 0 ? (
                <tr>
                  <td colSpan={5} className="px-3 py-8 text-center text-muted-foreground">
                    Kassa yoxdur.
                  </td>
                </tr>
              ) : (
                <>
                  <tr className="border-t bg-muted/30 font-bold">
                    <td className="px-3 py-2">{report.total.registerName}</td>
                    <td className="px-3 py-2 text-right tabular-nums">{money(report.total.opening)}</td>
                    <td className="px-3 py-2 text-right tabular-nums">{money(report.total.income)}</td>
                    <td className="px-3 py-2 text-right tabular-nums">{money(report.total.outcome)}</td>
                    <td className="px-3 py-2 text-right tabular-nums">{money(report.total.closing)}</td>
                  </tr>
                  {report.registers.map((r) => (
                    <tr key={r.registerId} className="border-t">
                      <td className="px-3 py-1.5 font-medium">{r.registerName}</td>
                      <td className="px-3 py-1.5 text-right tabular-nums">{money(r.opening)}</td>
                      <td className="px-3 py-1.5 text-right tabular-nums">{money(r.income)}</td>
                      <td className="px-3 py-1.5 text-right tabular-nums">{money(r.outcome)}</td>
                      <td className="px-3 py-1.5 text-right font-semibold tabular-nums">{money(r.closing)}</td>
                    </tr>
                  ))}
                </>
              )}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}
