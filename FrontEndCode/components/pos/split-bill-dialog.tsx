"use client";

import { useEffect, useMemo, useState } from "react";
import { Minus, Plus, Printer } from "lucide-react";
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
import { TouchNumpad } from "@/components/pos/touch-numpad";
import { cn } from "@/lib/utils";
import {
  payOrderPart,
  type OrderDto,
  type OrderLineDto,
  type OrderPaymentsDto,
  type PartPaymentResultDto,
  type PaymentMethod,
} from "@/lib/services/order-service";
import { useFiscalMode } from "@/lib/pos-fiscal-mode";
import { paymentMethodLabel } from "@/lib/payment-labels";

const r2 = (n: number) => Math.round((n + Number.EPSILON) * 100) / 100;

type Props = {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  order: OrderDto;
  payments: OrderPaymentsDto | null;
  /** Set when the part-payment data could not be loaded. */
  loadError?: string | null;
  onRetry?: () => void;
  cashEnabled: boolean;
  cardEnabled: boolean;
  creditEnabled: boolean;
  touchScreen: boolean;
  /** Called after a share was paid — the page reloads the order and prints the guest's receipt. */
  onPaid: (result: PartPaymentResultDto) => void | Promise<void>;
  onPrintPayment: (paymentId: number) => void;
};

/**
 * "Hesab" — split the bill. Pick the items (and how many of each) one guest pays for, take their
 * payment, repeat for the next guest. The table closes with the last share.
 */
export function SplitBillDialog({
  open,
  onOpenChange,
  order,
  payments,
  loadError,
  onRetry,
  cashEnabled,
  cardEnabled,
  creditEnabled,
  touchScreen,
  onPaid,
  onPrintPayment,
}: Props) {
  const [selection, setSelection] = useState<Record<number, number>>({});
  const [method, setMethod] = useState<PaymentMethod>("Cash");
  const [receivedInput, setReceivedInput] = useState("");
  const fiscal = useFiscalMode();
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const paidByLine = useMemo(() => {
    const map = new Map<number, number>();
    for (const p of payments?.paidLines ?? []) map.set(p.orderLineId, p.paidQuantity);
    return map;
  }, [payments]);

  const lines = useMemo(
    () => order.lines.filter((l) => l.parentLineId == null && l.status !== "Cancelled"),
    [order.lines],
  );
  const remainingOf = (l: OrderLineDto) => l.quantity - (paidByLine.get(l.id) ?? 0);
  const isAtomic = (l: OrderLineDto) => l.isWeightBased || l.isTimeBased;

  const factor = payments?.discountFactor ?? 1;
  const portion = (l: OrderLineDto, qty: number) =>
    l.quantity <= 0 ? 0 : r2(((l.lineTotal * qty) / l.quantity) * factor);

  const selectedLines = lines.filter((l) => (selection[l.id] ?? 0) > 0);
  // Gifts (0 ₼) never hold the table open.
  const isFinal =
    lines.length > 0 &&
    lines.filter((l) => l.lineTotal > 0).every((l) => remainingOf(l) - (selection[l.id] ?? 0) <= 0);
  const portionsSum = r2(selectedLines.reduce((s, l) => s + portion(l, selection[l.id] ?? 0), 0));
  const remainingAmount = payments?.remainingAmount ?? 0;
  const amount = isFinal ? remainingAmount : Math.min(portionsSum, remainingAmount);
  // The table's recorded service charge ("Servis haqqı qeyd etmək") is added to the last share.
  const service = isFinal ? r2(order.serviceChargeAmount ?? 0) : 0;
  const due = r2(amount + service);
  const received = Number(receivedInput) || 0;
  const change = method === "Cash" ? Math.max(0, r2(received - due)) : 0;

  // Reset whenever the dialog opens or a payment changes what is left.
  useEffect(() => {
    if (!open) return;
    setSelection({});
    setError(null);
    setMethod(cashEnabled ? "Cash" : cardEnabled ? "Card" : "Credit");
  }, [open, payments?.paidAmount, cashEnabled, cardEnabled]);

  useEffect(() => {
    setReceivedInput(due > 0 ? due.toFixed(2) : "");
  }, [due, method]);

  const setQty = (l: OrderLineDto, qty: number) => {
    const clamped = Math.max(0, Math.min(remainingOf(l), qty));
    setSelection((prev) => ({ ...prev, [l.id]: clamped }));
  };

  const selectAllRemaining = () => {
    const next: Record<number, number> = {};
    for (const l of lines) if (remainingOf(l) > 0) next[l.id] = remainingOf(l);
    setSelection(next);
  };

  const handlePay = async () => {
    if (selectedLines.length === 0 && !isFinal) {
      toast.error("Ödəniş üçün məhsul seçin.");
      return;
    }
    if (method === "Cash" && received < due) {
      toast.error("Alınan məbləğ ödənilməli məbləğdən az ola bilməz.");
      return;
    }
    if (method === "Credit" && order.counterpartyId == null) {
      toast.error("Borca yazmaq üçün əvvəlcə müştəri seçin.");
      return;
    }
    setBusy(true);
    setError(null);
    try {
      const result = await payOrderPart(order.id, {
        paymentMethod: method,
        paidAmount: method === "Cash" ? received : due,
        isFiscal: fiscal,
        lines: selectedLines.map((l) => ({ orderLineId: l.id, quantity: selection[l.id] ?? 0 })),
      });
      setSelection({});
      await onPaid(result);
    } catch (err) {
      const message = err instanceof Error ? err.message : "Ödəniş uğursuz oldu";
      setError(message);
      toast.error(message);
    } finally {
      setBusy(false);
    }
  };

  const qtyLabel = (l: OrderLineDto, qty: number) =>
    l.isWeightBased ? `${(qty / 1000).toFixed(3)} kq` : String(qty);

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-h-[90vh] overflow-y-auto sm:max-w-lg">
        <DialogHeader>
          <DialogTitle>Hesab — hissə-hissə ödəniş</DialogTitle>
          <DialogDescription>
            {order.tableName}: hər qonaq üçün öz məhsullarını seçin. Qalıq: {remainingAmount.toFixed(2)} ₼
          </DialogDescription>
        </DialogHeader>

        {payments === null ? (
          loadError ? (
            <div className="space-y-3 py-4 text-center">
              <p className="text-sm text-destructive">{loadError}</p>
              <Button type="button" variant="outline" size="sm" onClick={onRetry}>
                Yenidən cəhd et
              </Button>
            </div>
          ) : (
            <p className="py-6 text-center text-sm text-muted-foreground">Yüklənir…</p>
          )
        ) : (
          <div className="space-y-4">
            <div className="flex items-center justify-between">
              <p className="text-sm font-medium">Məhsullar</p>
              <Button type="button" size="sm" variant="ghost" onClick={selectAllRemaining} disabled={busy}>
                Qalanların hamısı
              </Button>
            </div>

            <div className="space-y-1.5">
              {lines.map((l) => {
                const remaining = remainingOf(l);
                const qty = selection[l.id] ?? 0;
                const fullyPaid = remaining <= 0;
                return (
                  <div
                    key={l.id}
                    className={cn(
                      "flex items-center justify-between gap-2 rounded-lg border p-2 text-sm",
                      qty > 0 && "border-primary bg-primary/5",
                      fullyPaid && "opacity-50",
                    )}
                  >
                    <div className="min-w-0">
                      <p className="truncate font-medium">{l.menuItemName}</p>
                      <p className="text-[11px] text-muted-foreground">
                        {fullyPaid
                          ? "Ödənilib"
                          : `Qalan ${qtyLabel(l, remaining)} / ${qtyLabel(l, l.quantity)} · ${portion(l, remaining).toFixed(2)} ₼`}
                      </p>
                    </div>
                    {!fullyPaid &&
                      (isAtomic(l) ? (
                        <Button
                          type="button"
                          size="sm"
                          variant={qty > 0 ? "default" : "outline"}
                          disabled={busy}
                          onClick={() => setQty(l, qty > 0 ? 0 : remaining)}
                        >
                          {qty > 0 ? "Seçildi" : "Seç"}
                        </Button>
                      ) : (
                        <div className="flex shrink-0 items-center gap-2">
                          <button
                            type="button"
                            disabled={busy || qty <= 0}
                            onClick={() => setQty(l, qty - 1)}
                            className="flex h-8 w-8 items-center justify-center rounded-md border disabled:opacity-40"
                          >
                            <Minus className="h-3.5 w-3.5" />
                          </button>
                          <span className="w-6 text-center font-semibold">{qty}</span>
                          <button
                            type="button"
                            disabled={busy || qty >= remaining}
                            onClick={() => setQty(l, qty + 1)}
                            className="flex h-8 w-8 items-center justify-center rounded-md border disabled:opacity-40"
                          >
                            <Plus className="h-3.5 w-3.5" />
                          </button>
                        </div>
                      ))}
                  </div>
                );
              })}
            </div>

            <div className="space-y-3 rounded-lg border p-3">
              <div className="flex items-center justify-between text-sm">
                <span>Bu hesab{isFinal && " (son — masa bağlanacaq)"}</span>
                <span className="text-lg font-bold">{amount.toFixed(2)} ₼</span>
              </div>

              {service > 0 && (
                <div className="flex items-center justify-between text-sm text-muted-foreground">
                  <span>Servis haqqı</span>
                  <span>{service.toFixed(2)} ₼</span>
                </div>
              )}
              <div className="grid auto-cols-fr grid-flow-col gap-2">
                {cashEnabled && (
                  <Button type="button" variant={method === "Cash" ? "default" : "outline"} onClick={() => setMethod("Cash")}>
                    Nağd
                  </Button>
                )}
                {cardEnabled && (
                  <Button type="button" variant={method === "Card" ? "default" : "outline"} onClick={() => setMethod("Card")}>
                    Kart
                  </Button>
                )}
                {creditEnabled && order.counterpartyId != null && (
                  <Button type="button" variant={method === "Credit" ? "default" : "outline"} onClick={() => setMethod("Credit")}>
                    Borca yaz
                  </Button>
                )}
              </div>

              {method === "Cash" && (
                <div className="space-y-2">
                  <Label htmlFor="split-received">Alınan məbləğ</Label>
                  <Input
                    id="split-received"
                    type="number"
                    step="0.01"
                    value={receivedInput}
                    onChange={(e) => setReceivedInput(e.target.value)}
                  />
                  {touchScreen && <TouchNumpad value={receivedInput} onChange={setReceivedInput} allowDecimal />}
                  <p className="text-sm text-muted-foreground">Qaytarılan: {change.toFixed(2)} ₼</p>
                </div>
              )}

              {error && (
                <div className="rounded-lg border border-destructive/40 bg-destructive/10 p-3 text-sm text-destructive">
                  {error}
                </div>
              )}

              <Button
                className="h-12 w-full text-base font-semibold"
                disabled={busy || (selectedLines.length === 0 && !isFinal) || due < 0}
                onClick={() => void handlePay()}
              >
                {busy ? "Gözləyin…" : `Ödə — ${due.toFixed(2)} ₼`}
              </Button>
            </div>

            {payments.payments.length > 0 && (
              <div className="space-y-1.5">
                <p className="text-sm font-medium">Ödənilənlər — {payments.paidAmount.toFixed(2)} ₼</p>
                {payments.payments.map((p, i) => (
                  <div key={p.id} className="flex items-center justify-between gap-2 rounded-lg border p-2 text-sm">
                    <div className="min-w-0">
                      <p className="font-medium">
                        Hesab {i + 1} · {paymentMethodLabel(p.method)} · {(p.amount + p.serviceChargeAmount).toFixed(2)} ₼
                      </p>
                      <p className="truncate text-[11px] text-muted-foreground">
                        {p.lines.map((l) => `${l.quantity}× ${l.menuItemName}`).join(", ")}
                      </p>
                    </div>
                    <Button type="button" size="icon-sm" variant="ghost" onClick={() => onPrintPayment(p.id)} title="Çek çap et">
                      <Printer className="h-4 w-4" />
                    </Button>
                  </div>
                ))}
              </div>
            )}
          </div>
        )}
      </DialogContent>
    </Dialog>
  );
}
