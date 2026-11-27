"use client";

import { useEffect, useState } from "react";
import { toast } from "sonner";
import { Button } from "@/components/ui/button";
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from "@/components/ui/dialog";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { toApiFormError } from "@/lib/api-error";
import { increaseStockAsset, type StockAssetWarehouse } from "@/lib/services/stock-asset-service";
import { StockWriteOffReason, createStockWriteOff, type StockWriteOffReasonValue } from "@/lib/services/stock-writeoff-service";
import type { Warehouse } from "@/lib/services/warehouse-service";
import { cn } from "@/lib/utils";
import { UNIT_LABELS } from "@/components/stock/item-dialog";

const SELECT_CLASS = "h-9 w-full rounded-md border border-input bg-background px-3 text-sm";

export type OperationKind = "increase" | "broken" | "expense" | "spoiled" | "other";

const OPERATIONS: { kind: OperationKind; label: string; hint: string; reason?: StockWriteOffReasonValue }[] = [
  { kind: "increase", label: "Artım", hint: "Alış sənədi olmadan stoka əlavə (hədiyyə, tapılan, düzəliş). Pulla alınan mal Alışlardan daxil edilir." },
  { kind: "broken", label: "Sınma", hint: "Sındı, zədələndi — stokdan azalır.", reason: StockWriteOffReason.Broken },
  { kind: "expense", label: "Xərcə silinmə", hint: "İstifadə olundu və ya xərcə yazıldı — stokdan azalır.", reason: StockWriteOffReason.Expense },
  { kind: "spoiled", label: "Xarab olma", hint: "Xarab oldu, istifadəyə yaramır — stokdan azalır.", reason: StockWriteOffReason.Spoiled },
  { kind: "other", label: "Digər azalma", hint: "Başqa səbəbdən azalma.", reason: StockWriteOffReason.Other },
];

const qty = (n: number) => String(Math.round(n * 1000) / 1000);

export type OperationTarget = { stockItemId: number; name: string; unit: number; warehouses: StockAssetWarehouse[]; start: OperationKind };

/** One window for every change of an item's stock: an increase, breakage, expense, spoilage or another decrease. */
export function StockOperationDialog({
  target,
  warehouses,
  preferredWarehouseId,
  onClose,
  onDone,
}: {
  target: OperationTarget | null;
  warehouses: Warehouse[];
  preferredWarehouseId: number | null;
  onClose: () => void;
  onDone: () => void;
}) {
  const [kind, setKind] = useState<OperationKind>("increase");
  const [warehouseId, setWarehouseId] = useState<number | "">("");
  const [quantity, setQuantity] = useState("");
  const [note, setNote] = useState("");
  const [saving, setSaving] = useState(false);

  /** A decrease starts from a warehouse that holds the item; an increase from the chosen (or first) warehouse. */
  const pickWarehouse = (k: OperationKind, current: number | ""): number | "" => {
    if (!target) return "";
    const holders = target.warehouses.filter((w) => w.quantity > 0).map((w) => w.warehouseId);
    const candidates = k === "increase" ? warehouses.map((w) => w.id) : holders;
    if (current !== "" && candidates.includes(current)) return current;
    if (preferredWarehouseId != null && candidates.includes(preferredWarehouseId)) return preferredWarehouseId;
    return candidates[0] ?? "";
  };

  useEffect(() => {
    if (!target) return;
    setKind(target.start);
    setQuantity("");
    setNote("");
    setWarehouseId(pickWarehouse(target.start, ""));
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [target]);

  const op = OPERATIONS.find((o) => o.kind === kind)!;
  const increase = kind === "increase";
  const available = target?.warehouses.find((w) => w.warehouseId === warehouseId)?.quantity ?? 0;
  const unit = target ? (UNIT_LABELS[target.unit] ?? "") : "";
  const options = increase
    ? warehouses.map((w) => ({ id: w.id, name: w.name }))
    : (target?.warehouses ?? []).filter((w) => w.quantity > 0).map((w) => ({ id: w.warehouseId, name: `${w.warehouseName} (${qty(w.quantity)})` }));

  const submit = async () => {
    if (!target || warehouseId === "") return void toast.error("Anbarı seçin.");
    const amount = Number(quantity.replace(",", "."));
    if (!Number.isFinite(amount) || amount <= 0) return void toast.error("Miqdarı yazın.");
    if (!increase && amount > available) return void toast.error(`Bu anbarda ${qty(available)} ${unit} var.`);
    setSaving(true);
    try {
      if (increase) await increaseStockAsset({ warehouseId, stockItemId: target.stockItemId, quantity: amount, note: note.trim() || null });
      else await createStockWriteOff({ warehouseId, stockItemId: target.stockItemId, quantity: amount, reason: op.reason!, note: note.trim() || null });
      toast.success(`${op.label}: ${target.name} — ${qty(amount)} ${unit}`);
      onDone();
      onClose();
    } catch (e) {
      toast.error(toApiFormError(e, "Əməliyyat yazılmadı").message);
    } finally {
      setSaving(false);
    }
  };

  return (
    <Dialog open={target != null} onOpenChange={(o) => !o && !saving && onClose()}>
      <DialogContent className="sm:max-w-md">
        <DialogHeader>
          <DialogTitle>{target?.name}</DialogTitle>
          <DialogDescription>{op.hint}</DialogDescription>
        </DialogHeader>
        <div className="space-y-3">
          <div className="flex flex-wrap gap-1.5">
            {OPERATIONS.map((o) => (
              <button
                key={o.kind}
                type="button"
                onClick={() => {
                  setKind(o.kind);
                  setWarehouseId((current) => pickWarehouse(o.kind, current));
                }}
                className={cn("rounded-full border px-3 py-1 text-xs", kind === o.kind ? "border-primary bg-primary text-primary-foreground" : "hover:bg-muted")}
              >
                {o.label}
              </button>
            ))}
          </div>
          <div className="space-y-1">
            <Label htmlFor="so-wh">Anbar</Label>
            <select id="so-wh" className={SELECT_CLASS} value={warehouseId} onChange={(e) => setWarehouseId(e.target.value ? Number(e.target.value) : "")}>
              <option value="">Seçin</option>
              {options.map((w) => (
                <option key={w.id} value={w.id}>
                  {w.name}
                </option>
              ))}
            </select>
            {!increase && options.length === 0 && <p className="text-xs text-amber-600">Bu məhsul heç bir anbarda yoxdur.</p>}
          </div>
          <div className="space-y-1">
            <Label htmlFor="so-qty">Miqdar ({unit})</Label>
            <Input id="so-qty" autoFocus inputMode="decimal" value={quantity} onChange={(e) => setQuantity(e.target.value)} onKeyDown={(e) => e.key === "Enter" && void submit()} />
          </div>
          <div className="space-y-1">
            <Label htmlFor="so-note">Qeyd</Label>
            <Input id="so-note" value={note} onChange={(e) => setNote(e.target.value)} />
          </div>
        </div>
        <DialogFooter>
          <Button variant="outline" disabled={saving} onClick={onClose}>
            Ləğv et
          </Button>
          <Button disabled={saving} onClick={() => void submit()}>
            {op.label} — yaz
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
