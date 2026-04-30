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

export type WarehouseTransferLineDraft = {
  key: string;
  lineId?: number;
  stockItemId: number | null;
  quantity: string;
};

type WarehouseTransferLinesTableProps = {
  lines: WarehouseTransferLineDraft[];
  stockItems: StockItem[];
  onChange: (lines: WarehouseTransferLineDraft[]) => void;
  readOnly: boolean;
  rowErrors: Record<string, string | undefined>;
  duplicateMessage?: string;
};

export function WarehouseTransferLinesTable({
  lines,
  stockItems,
  onChange,
  readOnly,
  rowErrors,
  duplicateMessage,
}: WarehouseTransferLinesTableProps) {
  const addLine = () => {
    onChange([
      ...lines,
      { key: `nl-${Date.now()}-${Math.random().toString(36).slice(2)}`, stockItemId: null, quantity: "" },
    ]);
  };

  const removeLine = (key: string) => {
    onChange(lines.filter((l) => l.key !== key));
  };

  const patchLine = (key: string, patch: Partial<WarehouseTransferLineDraft>) => {
    onChange(lines.map((l) => (l.key === key ? { ...l, ...patch } : l)));
  };

  return (
    <div className="space-y-3">
      {duplicateMessage ? <p className="text-sm text-destructive">{duplicateMessage}</p> : null}
      <div className="rounded-md border border-border overflow-x-auto">
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead className="min-w-[200px]">Stock item</TableHead>
              <TableHead className="w-[120px]">Quantity</TableHead>
              <TableHead className="w-[100px]">Unit</TableHead>
              <TableHead className="w-[56px]" />
            </TableRow>
          </TableHeader>
          <TableBody>
            {lines.length === 0 ? (
              <TableRow>
                <TableCell colSpan={4} className="text-center text-muted-foreground py-10">
                  No lines. Add at least one line before saving.
                </TableCell>
              </TableRow>
            ) : (
              lines.map((line) => {
                const item = line.stockItemId != null ? stockItems.find((s) => s.id === line.stockItemId) : null;
                const err = rowErrors[line.key];
                return (
                  <TableRow key={line.key}>
                    <TableCell className="align-top">
                      <StockItemCombobox
                        items={stockItems}
                        value={line.stockItemId}
                        onChange={(id) => patchLine(line.key, { stockItemId: id })}
                        disabled={readOnly}
                      />
                      {err ? <p className="text-xs text-destructive mt-1">{err}</p> : null}
                    </TableCell>
                    <TableCell className="align-top">
                      <Input
                        type="number"
                        min={0}
                        step="any"
                        className="h-10"
                        value={line.quantity}
                        onChange={(e) => patchLine(line.key, { quantity: e.target.value })}
                        disabled={readOnly}
                      />
                    </TableCell>
                    <TableCell className="align-top text-muted-foreground text-sm">
                      {item ? unitLabel(item.unit) : "—"}
                    </TableCell>
                    <TableCell className="align-top">
                      <Button
                        type="button"
                        variant="ghost"
                        size="icon"
                        className="text-destructive hover:text-destructive"
                        disabled={readOnly}
                        onClick={() => removeLine(line.key)}
                        aria-label="Remove line"
                      >
                        <Trash2 className="h-4 w-4" />
                      </Button>
                    </TableCell>
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
