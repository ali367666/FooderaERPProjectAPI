"use client";

import { useEffect, useState } from "react";
import { Banknote, CreditCard, Users } from "lucide-react";
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
import {
  payOrderPart,
  type OrderDto,
  type OrderPaymentsDto,
  type PartPaymentResultDto,
} from "@/lib/services/order-service";
import { useFiscalMode } from "@/lib/pos-fiscal-mode";
import { paymentMethodLabel } from "@/lib/payment-labels";

const r2 = (n: number) => Math.round((n + Number.EPSILON) * 100) / 100;
type Method = "Cash" | "Card" | "Credit";

type Props = {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  order: OrderDto;
  payments: OrderPaymentsDto | null;
  loadError?: string | null;
  onRetry?: () => void;
  cashEnabled: boolean;
  cardEnabled: boolean;
  creditEnabled: boolean;
  touchScreen: boolean;
  /** "Hesab bölüşdür" — pay item by item, guest by guest. */
  onSplit: () => void;
  /** Called after a payment; `orderClosed` tells whether it settled the whole bill. */
  onPaid: (result: PartPaymentResultDto) => void | Promise<void>;
};

/**
 * "Hesab" — how does the guest pay: cash, card, or split the bill between guests. A cash/card
 * amount smaller than the bill is deducted from it (the rest can go by the other method); an amount
 * that covers the whole bill closes the table and gives back the change.
 */
export function PayDialog({
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
  onSplit,
  onPaid,
}: Props) {
  const [view, setView] = useState<"choose" | Method>("choose");
  const [amountInput, setAmountInput] = useState("");
  const fiscal = useFiscalMode();
  const [busy, setBusy] = useState(false);
  // A toast disappears in seconds — the server's refusal ("only ready orders can be paid"...) stays here.
  const [error, setError] = useState<string | null>(null);

  const remaining = payments?.remainingAmount ?? 0;
  const amount = r2(Number(amountInput) || 0);
  const isFinal = amount >= r2(remaining) && remaining > 0;
  // The service charge recorded on the table ("Servis haqqı qeyd etmək") goes on the payment that closes it.
  const orderService = r2(order.serviceChargeAmount ?? 0);
  const service = isFinal ? orderService : 0;
  const due = r2((isFinal ? remaining : amount) + service);
  const change = view === "Cash" && isFinal ? Math.max(0, r2(amount - due)) : 0;
  const leftAfter = isFinal ? 0 : r2(remaining - amount);

  // Back to the chooser whenever the dialog opens or a payment changes what is left.
  useEffect(() => {
    if (open) {
      setView("choose");
      setError(null);
    }
  }, [open, payments?.paidAmount]);

  const pick = (method: Method) => {
    setAmountInput(remaining > 0 ? r2(remaining + orderService).toFixed(2) : "");
    setView(method);
  };

  const handlePay = async () => {
    if (view === "choose") return;
    if (amount <= 0) {
      toast.error("Məbləği daxil edin.");
      return;
    }
    if (view === "Cash" && isFinal && amount < due) {
      toast.error(`Alınan məbləğ servis haqqı ilə birlikdə ${due.toFixed(2)} ₼ olmalıdır.`);
      return;
    }
    if (view === "Credit" && order.counterpartyId == null) {
      toast.error("Borca yazmaq üçün əvvəlcə müştəri seçin.");
      return;
    }
    setBusy(true);
    setError(null);
    try {
      const result = await payOrderPart(order.id, {
        paymentMethod: view,
        // Cash: the money handed over. Card/credit: the server settles exactly what is due.
        paidAmount: amount,
        amount,
        isFiscal: fiscal,
        lines: [],
      });
      // Part of the bill is paid: leave the amount form straight away — the chooser shows what is
      // still to pay. (A settled bill closes the whole dialog from the page.)
      if (!result.orderClosed) {
        setView("choose");
        setAmountInput("");
      }
      await onPaid(result);
    } catch (err) {
      const message = err instanceof Error ? err.message : "Ödəniş uğursuz oldu";
      setError(message);
      toast.error(message);
    } finally {
      setBusy(false);
    }
  };

  const hasParts = (payments?.payments.length ?? 0) > 0;

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-h-[90vh] overflow-y-auto sm:max-w-sm">
        <DialogHeader>
          <DialogTitle>Hesab</DialogTitle>
          <DialogDescription>
            {order.tableName}
            {payments && ` — ödəniləcək: ${r2(remaining + orderService).toFixed(2)} ₼`}
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
            {hasParts && (
              <div className="space-y-1 rounded-lg bg-muted/50 p-3 text-sm">
                {payments.payments.map((p) => (
                  <div key={p.id} className="flex justify-between text-muted-foreground">
                    <span>{paymentMethodLabel(p.method)}</span>
                    <span>{(p.amount + p.serviceChargeAmount).toFixed(2)} ₼</span>
                  </div>
                ))}
                <div className="flex justify-between border-t pt-1 font-semibold">
                  <span>Qalıq</span>
                  <span>{remaining.toFixed(2)} ₼</span>
                </div>
              </div>
            )}

            {error && (
              <div className="rounded-lg border border-destructive/40 bg-destructive/10 p-3 text-sm text-destructive">
                {error}
              </div>
            )}

            {view === "choose" ? (
              <div className="space-y-2">
                {cashEnabled && (
                  <Button className="h-14 w-full justify-start text-base" onClick={() => pick("Cash")}>
                    <Banknote className="mr-3 h-5 w-5" />
                    Nağd
                  </Button>
                )}
                {cardEnabled && (
                  <Button className="h-14 w-full justify-start text-base" variant="outline" onClick={() => pick("Card")}>
                    <CreditCard className="mr-3 h-5 w-5" />
                    Kart
                  </Button>
                )}
                <Button
                  className="h-14 w-full justify-start text-base"
                  variant="outline"
                  onClick={onSplit}
                  disabled={order.lines.length === 0}
                >
                  <Users className="mr-3 h-5 w-5" />
                  Hesab bölüşdür
                </Button>
                {creditEnabled && order.counterpartyId != null && (
                  <Button className="w-full" variant="ghost" size="sm" onClick={() => pick("Credit")}>
                    Borca yaz ({order.counterpartyName})
                  </Button>
                )}
              </div>
            ) : (
              <div className="space-y-3">
                <div className="space-y-1">
                  <Label htmlFor="pay-amount">
                    {view === "Cash" ? "Alınan məbləğ" : view === "Card" ? "Kartla ödənilən məbləğ" : "Borca yazılan məbləğ"}
                  </Label>
                  <Input
                    id="pay-amount"
                    type="number"
                    step="0.01"
                    autoFocus
                    value={amountInput}
                    onChange={(e) => setAmountInput(e.target.value)}
                  />
                  {touchScreen && <TouchNumpad value={amountInput} onChange={setAmountInput} allowDecimal />}
                </div>

                <div className="rounded-lg bg-muted/50 p-3 text-sm">
                  {isFinal ? (
                    <>
                      <p className="font-medium">Hesab tam ödəniləcək və bağlanacaq.</p>
                      {service > 0 && (
                        <p className="text-muted-foreground">Servis haqqı daxildir: {service.toFixed(2)} ₼</p>
                      )}
                      {view === "Cash" && (
                        <p className="text-muted-foreground">Qaytarılan: {change.toFixed(2)} ₼</p>
                      )}
                    </>
                  ) : (
                    <>
                      <p className="font-medium">{amount.toFixed(2)} ₼ hesabdan çıxılacaq.</p>
                      <p className="text-muted-foreground">
                        Qalıq: {leftAfter.toFixed(2)} ₼ — {view === "Cash" ? "kartla" : "nağdla"} ödəyə bilərsiniz.
                      </p>
                    </>
                  )}
                </div>

                <div className="flex gap-2">
                  <Button variant="outline" className="flex-1" onClick={() => setView("choose")} disabled={busy}>
                    Geri
                  </Button>
                  <Button className="flex-1" onClick={() => void handlePay()} disabled={busy || amount <= 0}>
                    {busy ? "Gözləyin…" : "Ödə"}
                  </Button>
                </div>
              </div>
            )}
          </div>
        )}
      </DialogContent>
    </Dialog>
  );
}
