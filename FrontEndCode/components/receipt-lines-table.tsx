"use client";

import { useEffect, useState } from "react";
import { Checkbox } from "@/components/ui/checkbox";
import { formatCurrency } from "@/lib/format-currency";
import type { OrderLineDto } from "@/lib/services/order-service";

const SHOW_TIMES_KEY = "receiptShowLineTimes";

/** A product removed from the order (from the cancellations report) — shown struck through. */
export type ReceiptCancelledLine = {
  menuItemName: string | null;
  isWholeOrder: boolean;
  quantity: number;
  amount: number;
  cancelledAt: string;
  reason: string;
  note: string | null;
  cancelledBy: string;
};

function readShowTimes(): boolean {
  try {
    return localStorage.getItem(SHOW_TIMES_KEY) !== "0";
  } catch {
    return true;
  }
}

function clock(value: string | null | undefined): string {
  return value ? new Date(value).toLocaleTimeString("az-AZ", { hour: "2-digit", minute: "2-digit" }) : "—";
}

type Row =
  | { kind: "line"; at: string | null; line: OrderLineDto }
  | { kind: "cancelled"; at: string; c: ReceiptCancelledLine };

/**
 * What was ordered on a receipt, each item with the time it was rung up; removed products appear
 * in red with the time, reason and who removed them; a gift says "(Hədiyyə)" in the price column.
 * The time column can be hidden — the choice is remembered in this browser.
 */
export function ReceiptLinesTable({
  lines,
  cancellations = [],
}: {
  lines: OrderLineDto[];
  cancellations?: ReceiptCancelledLine[];
}) {
  const [showTimes, setShowTimes] = useState(true);

  useEffect(() => {
    setShowTimes(readShowTimes());
  }, []);

  const toggle = (value: boolean) => {
    setShowTimes(value);
    try {
      localStorage.setItem(SHOW_TIMES_KEY, value ? "1" : "0");
    } catch {
      // storage unavailable — keep the choice for this session only
    }
  };

  const rows: Row[] = [
    ...lines
      .filter((l) => l.status.toLowerCase() !== "cancelled")
      .map((line): Row => ({ kind: "line", at: line.createdAtUtc, line })),
    ...cancellations.filter((c) => !c.isWholeOrder).map((c): Row => ({ kind: "cancelled", at: c.cancelledAt, c })),
  ].sort((a, b) => new Date(a.at ?? 0).getTime() - new Date(b.at ?? 0).getTime());

  return (
    <div className="space-y-2">
      <label className="flex items-center gap-2 text-sm text-muted-foreground">
        <Checkbox checked={showTimes} onCheckedChange={(v) => toggle(v === true)} />
        Sifariş saatlarını göstər
      </label>
      <div className="max-h-80 overflow-y-auto rounded-md border">
        <table className="w-full text-sm">
          <thead>
            <tr className="border-b bg-muted/50">
              {showTimes && <th className="px-3 py-2 text-left font-medium text-muted-foreground">Saat</th>}
              <th className="px-3 py-2 text-left font-medium text-muted-foreground">Məhsul</th>
              <th className="px-3 py-2 text-right font-medium text-muted-foreground">Miqdar</th>
              <th className="px-3 py-2 text-right font-medium text-muted-foreground">Qiymət</th>
              <th className="px-3 py-2 text-right font-medium text-muted-foreground">Məbləğ</th>
            </tr>
          </thead>
          <tbody>
            {rows.map((row, i) =>
              row.kind === "line" ? (
                <tr key={`l${row.line.id}`} className="border-b last:border-0">
                  {showTimes && <td className="px-3 py-2 tabular-nums text-muted-foreground">{clock(row.at)}</td>}
                  <td className="px-3 py-2">
                    {row.line.menuItemName}
                    {row.line.note && <span className="block text-xs text-muted-foreground">{row.line.note}</span>}
                  </td>
                  <td className="px-3 py-2 text-right">{row.line.quantity}</td>
                  <td className="px-3 py-2 text-right">
                    {row.line.isGift ? (
                      <span className="font-medium text-amber-700">(Hədiyyə)</span>
                    ) : (
                      formatCurrency(row.line.unitPrice)
                    )}
                  </td>
                  <td className="px-3 py-2 text-right">{formatCurrency(row.line.lineTotal)}</td>
                </tr>
              ) : (
                <tr key={`c${i}`} className="border-b bg-rose-50/60 last:border-0 dark:bg-rose-950/20">
                  {showTimes && <td className="px-3 py-2 tabular-nums text-rose-700">{clock(row.at)}</td>}
                  <td className="px-3 py-2 text-rose-700">
                    <span className="line-through">{row.c.menuItemName}</span>
                    <span className="block text-xs">
                      Ləğv edildi — {row.c.reason}
                      {row.c.note ? ` (${row.c.note})` : ""} · {row.c.cancelledBy}
                    </span>
                  </td>
                  <td className="px-3 py-2 text-right text-rose-700 line-through">{row.c.quantity}</td>
                  <td className="px-3 py-2 text-right text-rose-700">—</td>
                  <td className="px-3 py-2 text-right text-rose-700 line-through">{formatCurrency(row.c.amount)}</td>
                </tr>
              ),
            )}
          </tbody>
        </table>
      </div>
    </div>
  );
}
