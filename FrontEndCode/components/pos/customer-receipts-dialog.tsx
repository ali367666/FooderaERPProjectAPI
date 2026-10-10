"use client";

import { Printer, Receipt } from "lucide-react";
import { Button } from "@/components/ui/button";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import type { OrderPaymentsDto } from "@/lib/services/order-service";
import { paymentMethodLabel } from "@/lib/payment-labels";

type Props = {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  tableName: string | null;
  payments: OrderPaymentsDto | null;
  /** Prints one guest's receipt (the items they paid for; earlier guests' items marked "paid"). */
  onPrintPayment: (paymentId: number) => void;
  /** Prints the receipt of the whole table. */
  onPrintWhole: () => void;
};

/** "Müştəri qəbzi" — print the receipt of any guest who has already paid their part of the bill. */
export function CustomerReceiptsDialog({ open, onOpenChange, tableName, payments, onPrintPayment, onPrintWhole }: Props) {
  const list = payments?.payments ?? [];
  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-h-[90vh] overflow-y-auto sm:max-w-md">
        <DialogHeader>
          <DialogTitle>Müştəri qəbzi</DialogTitle>
          <DialogDescription>{tableName}: qəbzini çap etmək istədiyiniz ödənişi seçin.</DialogDescription>
        </DialogHeader>

        {list.length === 0 ? (
          <p className="py-2 text-center text-sm text-muted-foreground">Hələ ödəniş olmayıb.</p>
        ) : (
          <div className="space-y-1.5">
            {list.map((p, i) => (
              <div key={p.id} className="flex items-center justify-between gap-2 rounded-lg border p-2 text-sm">
                <div className="min-w-0">
                  <p className="font-medium">
                    Hesab {i + 1} · {paymentMethodLabel(p.method)} · {(p.amount + p.serviceChargeAmount).toFixed(2)} ₼
                  </p>
                  <p className="truncate text-[11px] text-muted-foreground">
                    {p.lines.length > 0
                      ? p.lines.map((l) => `${l.quantity}× ${l.menuItemName}`).join(", ")
                      : "Hesabdan ödəniş"}
                  </p>
                </div>
                <Button type="button" size="icon-sm" variant="outline" onClick={() => onPrintPayment(p.id)} title="Qəbzi çap et">
                  <Printer className="h-4 w-4" />
                </Button>
              </div>
            ))}
          </div>
        )}

        <Button type="button" variant="outline" className="w-full" onClick={onPrintWhole}>
          <Receipt className="mr-2 h-4 w-4" />
          Ön çek (bütün masa)
        </Button>
      </DialogContent>
    </Dialog>
  );
}
