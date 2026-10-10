"use client";

import { useCallback, useEffect, useState } from "react";
import { toast } from "sonner";
import { Button } from "@/components/ui/button";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { cn } from "@/lib/utils";
import {
  addCounterpartyDebt,
  getCounterpartyDebtHistory,
  type Counterparty,
  type CounterpartyDebtEntry,
} from "@/lib/services/counterparty-service";

const TYPE_LABEL: Record<string, string> = {
  Added: "Borc əlavə edildi",
  Adjusted: "Borc düzəldildi",
  CreditSale: "Nisyə satış",
};

function formatWhen(iso: string): string {
  const d = new Date(iso);
  return Number.isNaN(d.getTime())
    ? "—"
    : d.toLocaleString("az-AZ", { day: "2-digit", month: "2-digit", year: "numeric", hour: "2-digit", minute: "2-digit" });
}

type Props = {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  counterparty: Counterparty | null;
  /** Called after the debt changed, so the list behind the dialog refreshes. */
  onChanged: () => void;
  /** Opens the "set the debt to a new figure" correction. */
  onAdjust: (counterparty: Counterparty) => void;
};

/** A counterparty's debt: add more debt, and see every change with its date and time. */
export function CounterpartyDebtDialog({ open, onOpenChange, counterparty, onChanged, onAdjust }: Props) {
  const [entries, setEntries] = useState<CounterpartyDebtEntry[]>([]);
  const [loading, setLoading] = useState(false);
  const [amount, setAmount] = useState("");
  const [note, setNote] = useState("");
  const [busy, setBusy] = useState(false);
  const [currentDebt, setCurrentDebt] = useState(0);

  const loadHistory = useCallback(async (id: number) => {
    setLoading(true);
    try {
      setEntries(await getCounterpartyDebtHistory(id));
    } catch (e) {
      toast.error(e instanceof Error ? e.message : "Tarixçə yüklənmədi.");
      setEntries([]);
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    if (!open || !counterparty) return;
    setAmount("");
    setNote("");
    setCurrentDebt(counterparty.currentDebtAmount);
    void loadHistory(counterparty.id);
  }, [open, counterparty, loadHistory]);

  const handleAdd = async () => {
    if (!counterparty) return;
    const value = Number(amount.replace(",", "."));
    if (!Number.isFinite(value) || value <= 0) {
      toast.error("Əlavə olunan borc 0-dan böyük olmalıdır.");
      return;
    }
    setBusy(true);
    try {
      const updated = await addCounterpartyDebt(counterparty.id, value, note.trim() || null);
      setCurrentDebt(updated.currentDebtAmount);
      setAmount("");
      setNote("");
      toast.success(`${value.toFixed(2)} ₼ borc əlavə edildi.`);
      await loadHistory(counterparty.id);
      onChanged();
    } catch (e) {
      toast.error(e instanceof Error ? e.message : "Borc əlavə edilmədi.");
    } finally {
      setBusy(false);
    }
  };

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-h-[90vh] overflow-y-auto sm:max-w-lg">
        <DialogHeader>
          <DialogTitle>{counterparty?.name} — borc</DialogTitle>
          <DialogDescription>
            Cari borc: <span className={cn("font-semibold", currentDebt > 0 && "text-destructive")}>{currentDebt.toFixed(2)} ₼</span>
          </DialogDescription>
        </DialogHeader>

        <div className="space-y-3 rounded-lg border p-3">
          <p className="text-sm font-medium">Borc əlavə et</p>
          <div className="grid grid-cols-3 gap-2">
            <div>
              <Label htmlFor="cd-amount">Məbləğ (₼)</Label>
              <Input
                id="cd-amount"
                type="number"
                step="0.01"
                min={0}
                className="mt-1"
                value={amount}
                onChange={(e) => setAmount(e.target.value)}
                placeholder="100"
              />
            </div>
            <div className="col-span-2">
              <Label htmlFor="cd-note">Qeyd</Label>
              <Input id="cd-note" className="mt-1" value={note} onChange={(e) => setNote(e.target.value)} placeholder="İstəyə görə" />
            </div>
          </div>
          <Button onClick={() => void handleAdd()} disabled={busy || !amount.trim()}>
            {busy ? "Əlavə olunur…" : "Əlavə et"}
          </Button>
        </div>

        <div>
          <p className="mb-2 text-sm font-medium">Tarixçə</p>
          {loading ? (
            <p className="text-sm text-muted-foreground">Yüklənir…</p>
          ) : entries.length === 0 ? (
            <p className="text-sm text-muted-foreground">Hələ qeyd yoxdur.</p>
          ) : (
            <div className="overflow-hidden rounded-lg border">
              <table className="w-full text-sm">
                <thead className="bg-muted/50 text-left text-xs text-muted-foreground">
                  <tr>
                    <th className="px-3 py-2">Tarix və saat</th>
                    <th className="px-3 py-2">Əməliyyat</th>
                    <th className="px-3 py-2 text-right">Məbləğ</th>
                    <th className="px-3 py-2 text-right">Qalıq</th>
                  </tr>
                </thead>
                <tbody>
                  {entries.map((e) => (
                    <tr key={e.id} className="border-t">
                      <td className="whitespace-nowrap px-3 py-2">{formatWhen(e.createdAtUtc)}</td>
                      <td className="px-3 py-2">
                        {TYPE_LABEL[e.type] ?? e.type}
                        {(e.note || e.orderNumber) && (
                          <span className="block text-xs text-muted-foreground">
                            {e.orderNumber ? `Sifariş ${e.orderNumber}` : e.note}
                            {e.orderNumber && e.note ? ` — ${e.note}` : ""}
                          </span>
                        )}
                      </td>
                      <td
                        className={cn(
                          "whitespace-nowrap px-3 py-2 text-right font-semibold tabular-nums",
                          e.amount >= 0 ? "text-destructive" : "text-emerald-700",
                        )}
                      >
                        {e.amount >= 0 ? "+" : "−"}
                        {Math.abs(e.amount).toFixed(2)} ₼
                      </td>
                      <td className="whitespace-nowrap px-3 py-2 text-right tabular-nums">{e.balanceAfter.toFixed(2)} ₼</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </div>

        {counterparty && (
          <Button
            type="button"
            variant="ghost"
            size="sm"
            className="self-start text-muted-foreground"
            onClick={() => {
              onOpenChange(false);
              onAdjust(counterparty);
            }}
          >
            Borcu düzəlt (yeni məbləğ yaz)
          </Button>
        )}
      </DialogContent>
    </Dialog>
  );
}
