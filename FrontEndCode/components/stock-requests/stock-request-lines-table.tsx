"use client";

import { Trash2 } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import { StockItemCombobox } from "@/components/documents/stock-item-combobox";
import type { StockItem } from "@/lib/services/stock-item-service";
import { unitLabel } from "@/lib/services/stock-item-service";

export type StockRequestLineDraft = {
  key: string;
  lineId?: number;
  stockItemId: number | null;
  /** Set when hydrating from API; used for read-only display without combobox. */
  stockItemName?: string;
  quantity: string;
};

type StockRequestLinesTableProps = {
  lines: StockRequestLineDraft[];
  stockItems: StockItem[];
  onChange: (lines: StockRequestLineDraft[]) => void;
  readOnly: boolean;
  rowErrors: Record<string, string | undefined>;
};

export function StockRequestLinesTable({
  lines,
  stockItems,
  onChange,
  readOnly,
  rowErrors,
}: StockRequestLinesTableProps) {
  const addLine = () => {
    onChange([
      ...lines,
      {
        key: `nl-${Date.now()}-${Math.random().toString(36).slice(2)}`,
        stockItemId: null,
        quantity: "",
      },
    ]);
  };

  const removeLine = (key: string) => {
    onChange(lines.filter((l) => l.key !== key));
  };

  const patchLine = (key: string, patch: Partial<StockRequestLineDraft>) => {
    onChange(lines.map((l) => (l.key === key ? { ...l, ...patch } : l)));
  };

  return (
    <div className="space-y-3">
      <div className="rounded-md border border-border overflow-x-auto">
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead className="min-w-[200px]">Stock item</TableHead>
              <TableHead className="w-[120px]">Quantity</TableHead>
              <TableHead className="w-[100px]">Unit</TableHead>
              {!readOnly ? <TableHead className="w-[56px]" /> : null}
            </TableRow>
          </TableHeader>
          <TableBody>
            {lines.length === 0 ? (
              <TableRow>
                <TableCell
                  colSpan={readOnly ? 3 : 4}
                  className="text-center text-muted-foreground py-10"
                >
                  {readOnly ? "No line items on this document." : "No lines. Add at least one line before saving."}
                </TableCell>
              </TableRow>
            ) : (
              lines.map((line) => {
                const item = line.stockItemId != null ? stockItems.find((s) => s.id === line.stockItemId) : null;
                const displayName =
                  line.stockItemName?.trim() ||
                  item?.name?.trim() ||
                  (line.stockItemId != null ? "—" : "");
                const err = rowErrors[line.key];
                return (
                  <TableRow key={line.key}>
                    <TableCell className="align-top">
                      {readOnly ? (
                        <span className="text-sm font-medium text-foreground">{displayName || "—"}</span>
                      ) : (
                        <>
                          <StockItemCombobox
                            items={stockItems}
                            value={line.stockItemId}
                            onChange={(id) => patchLine(line.key, { stockItemId: id, stockItemName: undefined })}
                          />
                          {err ? <p className="text-xs text-destructive mt-1">{err}</p> : null}
                        </>
                      )}
                    </TableCell>
                    <TableCell className="align-top">
                      {readOnly ? (
                        <span className="text-sm tabular-nums">{line.quantity || "—"}</span>
                      ) : (
                        <Input
                          type="number"
                          min={0}
                          step="any"
                          className="h-10"
                          value={line.quantity}
                          onChange={(e) => patchLine(line.key, { quantity: e.target.value })}
                        />
                      )}
                    </TableCell>
                    <TableCell className="align-top text-muted-foreground text-sm">
                      {item ? unitLabel(item.unit) : "—"}
                    </TableCell>
                    {!readOnly ? (
                      <TableCell className="align-top">
                        <Button
                          type="button"
                          variant="ghost"
                          size="icon"
                          className="text-destructive hover:text-destructive"
                          onClick={() => removeLine(line.key)}
                          aria-label="Remove line"
                        >
                          <Trash2 className="h-4 w-4" />
                        </Button>
                      </TableCell>
                    ) : null}
                  </TableRow>
                );
              })
            )}
          </TableBody>
        </Table>
      </div>
      {!readOnly ? (
        <Button type="button" variant="outline" size="sm" onClick={addLine}>
          Add line
        </Button>
      ) : null}
    </div>
  );
}
