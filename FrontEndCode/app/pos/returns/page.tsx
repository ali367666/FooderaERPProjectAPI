"use client";

import { useEffect, useMemo, useRef, useState } from "react";
import { toast } from "sonner";
import { Minus, Plus, Printer, ScanBarcode, Undo2 } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Checkbox } from "@/components/ui/checkbox";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import {
  createSaleReturn,
  findReturnableOrder,
  type ReturnableOrder,
  type ReturnableOrderLine,
  type SaleReturn,
} from "@/lib/services/sale-return-service";
import { getPrinters, printToPrinter, type Printer as PrinterProfile } from "@/lib/services/printer-service";
import { getPosTerminalContext } from "@/lib/pos-terminal-client";
import { useHasPermission } from "@/hooks/use-auth-permissions";

const PAYMENT_LABELS: Record<string, string> = {
  Cash: "Nağd",
  Card: "Kart",
  Credit: "Borc",
};

const REFUND_HINTS: Record<string, string> = {
  Cash: "Məbləğ kassadan nağd qaytarılacaq (kassa məxarici yazılır).",
  Card: "Məbləğ karta qaytarılmalıdır.",
  Credit: "Məbləğ müştərinin borcundan silinəcək.",
};

function formatQuantity(line: ReturnableOrderLine, quantity: number): string {
  return line.isWeightBased ? `${quantity} q` : String(quantity);
}

function buildReturnSlip(order: ReturnableOrder, saleReturn: SaleReturn): string {
  const lines: string[] = [];
  if (order.restaurantName) lines.push(order.restaurantName.toUpperCase());
  lines.push("GERİ QAYTARMA");
  lines.push("-".repeat(32));
  lines.push(`Qaytarma: ${saleReturn.returnNumber}`);
  lines.push(`Sifariş: ${order.orderNumber}`);
  lines.push(`Vaxt: ${new Date(saleReturn.createdAtUtc).toLocaleString("az-AZ")}`);
  if (saleReturn.createdByUserName) lines.push(`İşçi: ${saleReturn.createdByUserName}`);
  lines.push("-".repeat(32));
  for (const l of saleReturn.lines) {
    lines.push(`${l.quantity} x ${l.menuItemName}`.padEnd(24) + `${l.amount.toFixed(2)} ₼`);
  }
  lines.push("-".repeat(32));
  lines.push(`Qaytarılan: ${saleReturn.totalAmount.toFixed(2)} ₼`);
  lines.push(`Üsul: ${PAYMENT_LABELS[saleReturn.paymentMethod] ?? saleReturn.paymentMethod}`);
  if (saleReturn.reason) lines.push(`Səbəb: ${saleReturn.reason}`);
  return lines.join("\n");
}

export default function PosReturnsPage() {
  const canReturn = useHasPermission("Pos.ReturnSale");
  const scanRef = useRef<HTMLInputElement>(null);

  const [code, setCode] = useState("");
  const [loading, setLoading] = useState(false);
  const [order, setOrder] = useState<ReturnableOrder | null>(null);
  const [quantities, setQuantities] = useState<Record<number, number>>({});
  const [reason, setReason] = useState("");
  const [restock, setRestock] = useState(true);
  const [confirmOpen, setConfirmOpen] = useState(false);
  const [saving, setSaving] = useState(false);
  const [lastReturn, setLastReturn] = useState<SaleReturn | null>(null);
  const [printers, setPrinters] = useState<PrinterProfile[]>([]);

  useEffect(() => {
    scanRef.current?.focus();
    const terminal = getPosTerminalContext();
    if (terminal?.restaurantId) {
      getPrinters(terminal.restaurantId)
        .then((p) => setPrinters(p.filter((x) => x.isActive)))
        .catch(() => setPrinters([]));
    }
  }, []);

  const primaryPrinter = useMemo(() => printers.find((p) => p.isPrimary) ?? null, [printers]);

  const lookup = async (value: string) => {
    const trimmed = value.trim();
    if (!trimmed) return;
    setLoading(true);
    try {
      const found = await findReturnableOrder(trimmed);
      setOrder(found);
      setQuantities({});
      setReason("");
      setRestock(true);
      setLastReturn(null);
    } catch (err) {
      setOrder(null);
      toast.error(err instanceof Error ? err.message : "Satış tapılmadı");
    } finally {
      setLoading(false);
      setCode("");
      scanRef.current?.focus();
    }
  };

  const setQuantity = (line: ReturnableOrderLine, value: number) => {
    const clamped = Math.max(0, Math.min(line.returnableQuantity, Math.floor(value) || 0));
    setQuantities((prev) => ({ ...prev, [line.orderLineId]: clamped }));
  };

  const selectAll = () => {
    if (!order) return;
    setQuantities(Object.fromEntries(order.lines.map((l) => [l.orderLineId, l.returnableQuantity])));
  };

  const selectedLines = useMemo(
    () => (order?.lines ?? []).filter((l) => (quantities[l.orderLineId] ?? 0) > 0),
    [order, quantities],
  );

  const refundTotal = useMemo(
    () => selectedLines.reduce((sum, l) => sum + l.refundUnitAmount * (quantities[l.orderLineId] ?? 0), 0),
    [selectedLines, quantities],
  );

  const handleReturn = async () => {
    if (!order || selectedLines.length === 0) return;
    setSaving(true);
    try {
      const result = await createSaleReturn({
        orderId: order.orderId,
        reason: reason.trim() || null,
        restockItems: restock,
        lines: selectedLines.map((l) => ({ orderLineId: l.orderLineId, quantity: quantities[l.orderLineId] ?? 0 })),
      });
      toast.success(`Qaytarıldı: ${result.totalAmount.toFixed(2)} ₼`);
      setConfirmOpen(false);
      const refreshed = await findReturnableOrder(String(order.orderId));
      setOrder(refreshed);
      setQuantities({});
      setReason("");
      setLastReturn(result);
    } catch (err) {
      toast.error(err instanceof Error ? err.message : "Qaytarma qeydə alınmadı");
    } finally {
      setSaving(false);
    }
  };

  const handlePrintSlip = async (saleReturn: SaleReturn) => {
    if (!order || !primaryPrinter) return;
    try {
      await printToPrinter(primaryPrinter.id, buildReturnSlip(order, saleReturn));
      toast.success(`${primaryPrinter.name}-ə göndərildi`);
    } catch (err) {
      toast.error(err instanceof Error ? err.message : "Printerə qoşulmaq mümkün olmadı");
    }
  };

  if (!canReturn) {
    return (
      <div className="p-6 text-sm text-muted-foreground">
        Satışı geri qaytarmaq üçün icazəniz yoxdur.
      </div>
    );
  }

  const paymentMethod = order?.paymentMethod ?? "";

  return (
    <div className="mx-auto max-w-4xl space-y-4 p-4">
      <div className="flex items-center gap-2">
        <Undo2 className="h-5 w-5" />
        <h1 className="text-xl font-semibold">Geri qaytarma</h1>
      </div>

      <form
        className="flex gap-2"
        onSubmit={(e) => {
          e.preventDefault();
          void lookup(code);
        }}
      >
        <div className="relative flex-1">
          <ScanBarcode className="pointer-events-none absolute left-3 top-1/2 h-5 w-5 -translate-y-1/2 text-muted-foreground" />
          <Input
            ref={scanRef}
            value={code}
            onChange={(e) => setCode(e.target.value)}
            placeholder="Qəbzin barkodunu oxudun və ya qəbz nömrəsini yazın"
            className="h-12 pl-10 text-base"
            autoComplete="off"
            disabled={loading}
          />
        </div>
        <Button type="submit" className="h-12 px-6" disabled={loading || !code.trim()}>
          {loading ? "Axtarılır…" : "Tap"}
        </Button>
      </form>

      {order && (
        <div className="space-y-4 rounded-xl border bg-card p-4">
          <div className="flex flex-wrap items-start justify-between gap-2">
            <div className="space-y-0.5 text-sm">
              <p className="text-base font-semibold">Sifariş {order.orderNumber}</p>
              {order.receiptNumber && <p className="text-muted-foreground">Qəbz: {order.receiptNumber}</p>}
              <p className="text-muted-foreground">
                {[order.tableName && `Masa: ${order.tableName}`, order.waiterName && `Ofisiant: ${order.waiterName}`]
                  .filter(Boolean)
                  .join(" · ")}
              </p>
              {order.paidAt && (
                <p className="text-muted-foreground">Ödənib: {new Date(order.paidAt).toLocaleString("az-AZ")}</p>
              )}
            </div>
            <div className="text-right text-sm">
              <p>
                Ödəniş: <span className="font-medium">{PAYMENT_LABELS[paymentMethod] ?? paymentMethod}</span>
                {order.counterpartyName && ` (${order.counterpartyName})`}
              </p>
              <p>
                Cəm: <span className="font-semibold">{order.totalAmount.toFixed(2)} ₼</span>
              </p>
              {order.returnedAmount > 0 && (
                <p className="text-red-600">Artıq qaytarılıb: {order.returnedAmount.toFixed(2)} ₼</p>
              )}
            </div>
          </div>

          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead>
                <tr className="border-b text-left text-muted-foreground">
                  <th className="py-2 pr-2 font-medium">Məhsul</th>
                  <th className="px-2 py-2 text-right font-medium">Satılıb</th>
                  <th className="px-2 py-2 text-right font-medium">Qaytarılıb</th>
                  <th className="px-2 py-2 text-center font-medium">Qaytarılacaq</th>
                  <th className="py-2 pl-2 text-right font-medium">Məbləğ</th>
                </tr>
              </thead>
              <tbody>
                {order.lines.map((line) => {
                  const qty = quantities[line.orderLineId] ?? 0;
                  const exhausted = line.returnableQuantity <= 0;
                  return (
                    <tr key={line.orderLineId} className={exhausted ? "border-b text-muted-foreground" : "border-b"}>
                      <td className="py-2 pr-2">{line.menuItemName}</td>
                      <td className="px-2 py-2 text-right">{formatQuantity(line, line.quantity)}</td>
                      <td className="px-2 py-2 text-right">{formatQuantity(line, line.returnedQuantity)}</td>
                      <td className="px-2 py-2">
                        {exhausted ? (
                          <p className="text-center text-xs">Tam qaytarılıb</p>
                        ) : (
                          <div className="flex items-center justify-center gap-1">
                            <Button
                              type="button"
                              size="icon"
                              variant="outline"
                              className="h-8 w-8"
                              disabled={qty <= 0}
                              onClick={() => setQuantity(line, qty - 1)}
                            >
                              <Minus className="h-3.5 w-3.5" />
                            </Button>
                            <Input
                              type="number"
                              min={0}
                              max={line.returnableQuantity}
                              value={qty}
                              onChange={(e) => setQuantity(line, Number(e.target.value))}
                              className="h-8 w-20 text-center"
                            />
                            <Button
                              type="button"
                              size="icon"
                              variant="outline"
                              className="h-8 w-8"
                              disabled={qty >= line.returnableQuantity}
                              onClick={() => setQuantity(line, qty + 1)}
                            >
                              <Plus className="h-3.5 w-3.5" />
                            </Button>
                          </div>
                        )}
                      </td>
                      <td className="py-2 pl-2 text-right">
                        {qty > 0 ? `${(line.refundUnitAmount * qty).toFixed(2)} ₼` : "—"}
                      </td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          </div>

          <div className="grid gap-3 sm:grid-cols-2">
            <div className="space-y-1">
              <Label htmlFor="return-reason">Səbəb (opsional)</Label>
              <Input
                id="return-reason"
                value={reason}
                onChange={(e) => setReason(e.target.value)}
                placeholder="məs. Müştəri bəyənmədi"
              />
            </div>
            <div className="flex items-center gap-2 sm:pt-6">
              <Checkbox id="return-restock" checked={restock} onCheckedChange={(v) => setRestock(v === true)} />
              <Label htmlFor="return-restock" className="text-sm font-normal">
                Məhsulları anbara geri qaytar (stok artsın)
              </Label>
            </div>
          </div>

          <div className="flex flex-wrap items-center justify-between gap-2 border-t pt-3">
            <Button type="button" variant="outline" onClick={selectAll}>
              Hamısını seç
            </Button>
            <div className="flex items-center gap-3">
              <span className="text-lg font-bold">{refundTotal.toFixed(2)} ₼</span>
              <Button
                type="button"
                variant="destructive"
                className="h-11 px-6"
                disabled={selectedLines.length === 0}
                onClick={() => setConfirmOpen(true)}
              >
                <Undo2 className="mr-2 h-4 w-4" />
                Geri qaytar
              </Button>
            </div>
          </div>

          {lastReturn && (
            <div className="flex flex-wrap items-center justify-between gap-2 rounded-lg border border-green-200 bg-green-50 px-3 py-2 text-sm text-green-800">
              <span>
                {lastReturn.returnNumber}: {lastReturn.totalAmount.toFixed(2)} ₼ qaytarıldı
              </span>
              {primaryPrinter && (
                <Button size="sm" variant="outline" onClick={() => void handlePrintSlip(lastReturn)}>
                  <Printer className="mr-1 h-3.5 w-3.5" />
                  Qaytarma çekini çap et
                </Button>
              )}
            </div>
          )}

          {order.returns.length > 0 && (
            <div className="space-y-2 border-t pt-3">
              <p className="text-sm font-semibold">Bu satış üzrə qaytarmalar</p>
              {order.returns.map((r) => (
                <div key={r.id} className="rounded-lg border px-3 py-2 text-sm">
                  <div className="flex flex-wrap justify-between gap-2">
                    <span className="font-medium">{r.returnNumber}</span>
                    <span>{r.totalAmount.toFixed(2)} ₼</span>
                  </div>
                  <p className="text-muted-foreground">
                    {new Date(r.createdAtUtc).toLocaleString("az-AZ")}
                    {r.createdByUserName && ` · ${r.createdByUserName}`}
                    {r.reason && ` · ${r.reason}`}
                  </p>
                  <p className="text-muted-foreground">
                    {r.lines.map((l) => `${l.quantity} x ${l.menuItemName}`).join(", ")}
                  </p>
                </div>
              ))}
            </div>
          )}
        </div>
      )}

      <Dialog open={confirmOpen} onOpenChange={(o) => !saving && setConfirmOpen(o)}>
        <DialogContent className="sm:max-w-md">
          <DialogHeader>
            <DialogTitle>Geri qaytarmanı təsdiqləyin</DialogTitle>
            <DialogDescription>{REFUND_HINTS[paymentMethod] ?? "Məbləğ müştəriyə qaytarılacaq."}</DialogDescription>
          </DialogHeader>
          <div className="space-y-1 text-sm">
            {selectedLines.map((l) => (
              <div key={l.orderLineId} className="flex justify-between">
                <span>
                  {formatQuantity(l, quantities[l.orderLineId] ?? 0)} × {l.menuItemName}
                </span>
                <span>{(l.refundUnitAmount * (quantities[l.orderLineId] ?? 0)).toFixed(2)} ₼</span>
              </div>
            ))}
            <div className="flex justify-between border-t pt-2 font-semibold">
              <span>Qaytarılacaq</span>
              <span>{refundTotal.toFixed(2)} ₼</span>
            </div>
            {!restock && <p className="text-xs text-muted-foreground">Məhsullar anbara qaytarılmayacaq.</p>}
          </div>
          <DialogFooter>
            <Button variant="outline" disabled={saving} onClick={() => setConfirmOpen(false)}>
              İmtina et
            </Button>
            <Button variant="destructive" disabled={saving} onClick={() => void handleReturn()}>
              {saving ? "Qeydə alınır…" : "Təsdiqlə"}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}
