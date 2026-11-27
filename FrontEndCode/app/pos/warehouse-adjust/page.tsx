"use client";

import { useCallback, useEffect, useMemo, useState } from "react";
import { toast } from "sonner";
import { Pencil, Search } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { Label } from "@/components/ui/label";
import {
  getPosWarehouseBalances,
  posAdjustWarehouseStock,
  type PosWarehouseBalance,
} from "@/lib/services/warehouse-stock-service";
import { unitLabel, type UnitOfMeasureValue } from "@/lib/services/stock-item-service";
import { getPosTerminalContext, type PosTerminalContext } from "@/lib/pos-terminal-client";
import { PosEmpty, PosPage, PosPageLoading, PosTileGrid, posTileButtonClass } from "@/components/pos/pos-page";

export default function PosWarehouseAdjustPage() {
  const [terminal, setTerminal] = useState<PosTerminalContext | null | undefined>(undefined);
  const [balances, setBalances] = useState<PosWarehouseBalance[]>([]);
  const [loading, setLoading] = useState(true);
  const [search, setSearch] = useState("");
  const [editing, setEditing] = useState<PosWarehouseBalance | null>(null);
  const [newQuantityInput, setNewQuantityInput] = useState("");
  const [saving, setSaving] = useState(false);

  useEffect(() => {
    setTerminal(getPosTerminalContext());
  }, []);

  const load = useCallback(async () => {
    if (!terminal?.restaurantId) return;
    setLoading(true);
    try {
      setBalances(await getPosWarehouseBalances(terminal.restaurantId));
    } catch (err) {
      toast.error(err instanceof Error ? err.message : "Anbar qalıqları yüklənə bilmədi");
      setBalances([]);
    } finally {
      setLoading(false);
    }
  }, [terminal]);

  useEffect(() => {
    if (terminal === undefined) return;
    void load();
  }, [terminal, load]);

  const filtered = useMemo(() => {
    const q = search.trim().toLowerCase();
    if (!q) return balances;
    return balances.filter((b) => b.stockItemName.toLowerCase().includes(q));
  }, [balances, search]);

  const openEdit = (row: PosWarehouseBalance) => {
    setEditing(row);
    setNewQuantityInput(String(row.quantity));
  };

  const handleSave = async () => {
    if (!editing) return;
    const newQuantity = Number(newQuantityInput);
    if (!Number.isFinite(newQuantity) || newQuantity < 0) {
      toast.error("Miqdar düzgün deyil.");
      return;
    }
    setSaving(true);
    try {
      await posAdjustWarehouseStock({
        warehouseId: editing.warehouseId,
        stockItemId: editing.stockItemId,
        newQuantity,
        unitId: editing.unitId,
      });
      toast.success("Anbar miqdarı yeniləndi.");
      setEditing(null);
      await load();
    } catch (err) {
      toast.error(err instanceof Error ? err.message : "Yadda saxlanılmadı.");
    } finally {
      setSaving(false);
    }
  };

  if (loading || terminal === undefined) return <PosPageLoading />;

  return (
    <PosPage
      title="Anbar düzəlişi"
      description="Filialın anbar qalığını əl ilə düzəldin."
      actions={
        <div className="relative w-64">
          <Search className="absolute left-2 top-2.5 h-4 w-4 text-muted-foreground" />
          <Input className="pl-8" placeholder="Xammal axtar…" value={search} onChange={(e) => setSearch(e.target.value)} />
        </div>
      }
    >
      {filtered.length === 0 && <PosEmpty>Anbar qalığı tapılmadı</PosEmpty>}

      <PosTileGrid>
        {filtered.map((b) => (
          <button key={b.id} type="button" onClick={() => openEdit(b)} className={posTileButtonClass()}>
            <span className="line-clamp-2 text-base font-bold leading-tight">{b.stockItemName}</span>
            <span className="text-lg font-semibold tabular-nums">{b.quantity}</span>
            <span className="text-xs text-muted-foreground">{unitLabel(b.unitId as UnitOfMeasureValue)}</span>
            <span className="mt-1 flex items-center gap-1 text-[11px] text-muted-foreground">
              <Pencil className="h-3 w-3" />
              Düzəlt
            </span>
          </button>
        ))}
      </PosTileGrid>

      <Dialog open={editing !== null} onOpenChange={(o) => !o && setEditing(null)}>
        <DialogContent className="sm:max-w-sm">
          <DialogHeader>
            <DialogTitle>Anbar miqdarını düzəlt</DialogTitle>
            <DialogDescription>{editing?.stockItemName}</DialogDescription>
          </DialogHeader>
          <div className="space-y-2">
            <Label htmlFor="new-quantity">
              Yeni miqdar {editing && `(${unitLabel(editing.unitId as UnitOfMeasureValue)})`}
            </Label>
            <Input
              id="new-quantity"
              type="number"
              step="0.001"
              min={0}
              value={newQuantityInput}
              onChange={(e) => setNewQuantityInput(e.target.value)}
              onFocus={(e) => e.target.select()}
            />
          </div>
          <DialogFooter>
            <Button variant="outline" onClick={() => setEditing(null)} disabled={saving}>
              Ləğv et
            </Button>
            <Button onClick={() => void handleSave()} disabled={saving}>
              {saving ? "Saxlanılır…" : "Saxla"}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </PosPage>
  );
}
