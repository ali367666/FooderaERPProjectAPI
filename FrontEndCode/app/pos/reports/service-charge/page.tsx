"use client";

import { useCallback, useEffect, useState } from "react";
import { toast } from "sonner";
import { Button } from "@/components/ui/button";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { Input } from "@/components/ui/input";
import { TouchNumpad } from "@/components/pos/touch-numpad";
import { PosReportShell } from "@/components/pos/pos-report-shell";
import { getPosTerminalContext } from "@/lib/pos-terminal-client";
import { getOrders, setOrderServiceCharge, type OrderDto } from "@/lib/services/order-service";
import { cn } from "@/lib/utils";

const isOpen = (o: OrderDto) => o.status !== "paid" && o.status !== "cancelled";
const r2 = (n: number) => Math.round((n + Number.EPSILON) * 100) / 100;

/**
 * "Servis haqqı qeyd etmək" — pick a table with an open order and record its service charge, as a
 * percentage of the bill or as a sum in manat. It is added to the payment that closes the table.
 */
function ServiceChargeContent() {
  const [orders, setOrders] = useState<OrderDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [target, setTarget] = useState<OrderDto | null>(null);
  const [kind, setKind] = useState<"Percent" | "Amount">("Percent");
  const [input, setInput] = useState("");
  const [busy, setBusy] = useState(false);

  const load = useCallback(async () => {
    setLoading(true);
    try {
      const restaurantId = getPosTerminalContext()?.restaurantId ?? null;
      const all = await getOrders();
      setOrders(
        all.filter(
          (o) => isOpen(o) && o.lines.length > 0 && (restaurantId == null || o.restaurantId === restaurantId),
        ),
      );
    } catch (err) {
      toast.error(err instanceof Error ? err.message : "Sifarişlər yüklənmədi");
      setOrders([]);
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    void load();
  }, [load]);

  const openDialog = (order: OrderDto) => {
    setTarget(order);
    setKind(order.serviceChargeAmount ? "Amount" : "Percent");
    setInput(order.serviceChargeAmount ? String(order.serviceChargeAmount) : "");
  };

  const value = Number(input.replace(",", ".")) || 0;
  const amount = target ? (kind === "Percent" ? r2((target.totalAmount * value) / 100) : r2(value)) : 0;

  const save = async (clear = false) => {
    if (!target) return;
    if (!clear && (value <= 0 || (kind === "Percent" && value > 100))) {
      toast.error(kind === "Percent" ? "Faizi 0–100 arasında daxil edin" : "Məbləği düzgün daxil edin");
      return;
    }
    setBusy(true);
    try {
      await setOrderServiceCharge(target.id, clear ? 0 : amount);
      toast.success(clear ? "Servis haqqı silindi" : `${target.tableName}: servis haqqı ${amount.toFixed(2)} ₼`);
      setTarget(null);
      await load();
    } catch (err) {
      toast.error(err instanceof Error ? err.message : "Servis haqqı yazılmadı");
    } finally {
      setBusy(false);
    }
  };

  return (
    <div>
      <h1 className="mb-1 text-xl font-bold">Servis haqqı qeyd etmək</h1>
      <p className="mb-4 text-sm text-muted-foreground">Masanı seçin, servis haqqını faizlə və ya manatla yazın.</p>

      {loading ? (
        <p className="text-sm text-muted-foreground">Yüklənir…</p>
      ) : orders.length === 0 ? (
        <p className="text-sm text-muted-foreground">Açıq sifarişi olan masa yoxdur.</p>
      ) : (
        <div className="grid grid-cols-2 gap-3 sm:grid-cols-3 md:grid-cols-4">
          {orders.map((o) => (
            <button
              key={o.id}
              type="button"
              onClick={() => openDialog(o)}
              className="flex flex-col items-center rounded-xl border bg-card p-3 text-center transition-colors hover:border-primary/50"
            >
              <span className="text-lg font-bold">{o.tableName}</span>
              <span className="text-sm text-muted-foreground">{o.totalAmount.toFixed(2)} ₼</span>
              <span
                className={cn(
                  "mt-1 text-xs",
                  o.serviceChargeAmount ? "font-semibold text-emerald-700" : "text-muted-foreground",
                )}
              >
                {o.serviceChargeAmount ? `Servis: ${o.serviceChargeAmount.toFixed(2)} ₼` : "Servis yoxdur"}
              </span>
            </button>
          ))}
        </div>
      )}

      <Dialog open={target !== null} onOpenChange={(o) => !o && setTarget(null)}>
        <DialogContent className="sm:max-w-xs">
          <DialogHeader>
            <DialogTitle>{target?.tableName}: servis haqqı</DialogTitle>
            <DialogDescription>Sifarişin cəmi: {target?.totalAmount.toFixed(2)} ₼</DialogDescription>
          </DialogHeader>
          <div className="grid grid-cols-2 gap-2">
            <Button type="button" variant={kind === "Percent" ? "default" : "outline"} onClick={() => setKind("Percent")}>
              Faiz %
            </Button>
            <Button type="button" variant={kind === "Amount" ? "default" : "outline"} onClick={() => setKind("Amount")}>
              Məbləğ ₼
            </Button>
          </div>
          <Input
            type="number"
            step={kind === "Percent" ? "1" : "0.01"}
            min={0}
            autoFocus
            value={input}
            onChange={(e) => setInput(e.target.value)}
            placeholder={kind === "Percent" ? "10" : "0.00"}
          />
          <TouchNumpad value={input} onChange={setInput} allowDecimal />
          <p className="text-sm text-muted-foreground">Servis haqqı: {amount.toFixed(2)} ₼</p>
          <DialogFooter className="gap-2 sm:gap-2">
            {target?.serviceChargeAmount ? (
              <Button type="button" variant="outline" onClick={() => void save(true)} disabled={busy}>
                Sil
              </Button>
            ) : null}
            <Button type="button" onClick={() => void save()} disabled={busy || !input.trim()}>
              Yaz
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}

export default function PosServiceChargePage() {
  return (
    <PosReportShell>
      <ServiceChargeContent />
    </PosReportShell>
  );
}
