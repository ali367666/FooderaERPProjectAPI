"use client";

import { useCallback, useEffect, useMemo, useState } from "react";
import { useParams, useRouter } from "next/navigation";
import { toast } from "sonner";
import { ArrowLeft, Minus, Plus, Printer, Receipt, Trash2 } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { cn } from "@/lib/utils";
import {
  getOrderById,
  addOrderLine,
  updateOrderLine,
  deleteOrderLine,
  payOrder,
  getOrderReceipt,
  type OrderDto,
  type OrderReceiptDto,
  type PaymentMethod,
} from "@/lib/services/order-service";
import { getMenuCategories, type MenuCategory } from "@/lib/services/menu-category-service";
import { getMenuItems, type MenuItem } from "@/lib/services/menu-item-service";
import { getPosTerminalContext } from "@/lib/pos-terminal-client";
import { getStoredAuthUser } from "@/lib/auth-client";
import {
  getCompanySettingsBranding,
  type CompanySettingsBranding,
} from "@/lib/services/company-settings-service";

function isWaiterRole(): boolean {
  const roles = getStoredAuthUser()?.roles ?? [];
  return roles.some((r) => r.trim().toLowerCase() === "waiter");
}

export default function PosOrderPage() {
  const params = useParams<{ id: string }>();
  const router = useRouter();
  const orderId = Number(params.id);

  const [order, setOrder] = useState<OrderDto | null>(null);
  const [categories, setCategories] = useState<MenuCategory[]>([]);
  const [items, setItems] = useState<MenuItem[]>([]);
  const [activeCategoryId, setActiveCategoryId] = useState<number | null>(null);
  const [loading, setLoading] = useState(true);
  const [busy, setBusy] = useState(false);

  const [payOpen, setPayOpen] = useState(false);
  const [paymentMethod, setPaymentMethod] = useState<PaymentMethod>("Cash");
  const [paidAmountInput, setPaidAmountInput] = useState("");
  const [receipt, setReceipt] = useState<OrderReceiptDto | null>(null);
  const [branding, setBranding] = useState<CompanySettingsBranding | null>(null);

  const load = useCallback(async () => {
    setLoading(true);
    try {
      const [o, cats, mi] = await Promise.all([
        getOrderById(orderId),
        getMenuCategories(),
        getMenuItems(),
      ]);
      setOrder(o);
      setCategories(cats.filter((c) => c.isActive));
      setItems(mi.filter((i) => i.isActive));
      setActiveCategoryId((prev) => prev ?? cats.find((c) => c.isActive)?.id ?? null);
    } catch (err) {
      toast.error(err instanceof Error ? err.message : "Sifariş yüklənə bilmədi");
      router.replace("/pos");
    } finally {
      setLoading(false);
    }
  }, [orderId, router]);

  useEffect(() => {
    void load();
  }, [load]);

  useEffect(() => {
    const terminal = getPosTerminalContext();
    if (!terminal) return;
    getCompanySettingsBranding(terminal.companyId)
      .then(setBranding)
      .catch(() => setBranding(null));
  }, []);

  const itemsInCategory = useMemo(
    () => items.filter((i) => i.menuCategoryId === activeCategoryId),
    [items, activeCategoryId],
  );

  const handleAddItem = async (menuItemId: number) => {
    if (!order || busy) return;
    setBusy(true);
    try {
      const updated = await addOrderLine({ orderId: order.id, menuItemId, quantity: 1 });
      setOrder(updated);
    } catch (err) {
      toast.error(err instanceof Error ? err.message : "Məhsul əlavə olunmadı");
    } finally {
      setBusy(false);
    }
  };

  const handleQuantityChange = async (lineId: number, currentQty: number, delta: number) => {
    if (!order || busy) return;
    const newQty = currentQty + delta;
    setBusy(true);
    try {
      if (newQty <= 0) {
        const updated = await deleteOrderLine(lineId);
        setOrder(updated);
      } else {
        const updated = await updateOrderLine({ id: lineId, quantity: newQty });
        setOrder(updated);
      }
    } catch (err) {
      toast.error(err instanceof Error ? err.message : "Sətir yenilənmədi");
    } finally {
      setBusy(false);
    }
  };

  const handleRemoveLine = async (lineId: number) => {
    if (!order || busy) return;
    setBusy(true);
    try {
      const updated = await deleteOrderLine(lineId);
      setOrder(updated);
    } catch (err) {
      toast.error(err instanceof Error ? err.message : "Sətir silinmədi");
    } finally {
      setBusy(false);
    }
  };

  const openPayDialog = () => {
    if (!order || order.lines.length === 0) return;
    setPaymentMethod("Cash");
    setPaidAmountInput(order.totalAmount.toFixed(2));
    setPayOpen(true);
  };

  const paidAmount = Number(paidAmountInput) || 0;
  const changeAmount = paymentMethod === "Cash" ? Math.max(0, paidAmount - (order?.totalAmount ?? 0)) : 0;

  const handleConfirmPayment = async () => {
    if (!order) return;
    if (paymentMethod === "Cash" && paidAmount < order.totalAmount) {
      toast.error("Ödənilən məbləğ cəmdən az ola bilməz");
      return;
    }
    setBusy(true);
    try {
      await payOrder(order.id, {
        paymentMethod,
        paidAmount: paymentMethod === "Cash" ? paidAmount : order.totalAmount,
      });
      const r = await getOrderReceipt(order.id);
      setReceipt(r);
      setPayOpen(false);
    } catch (err) {
      toast.error(err instanceof Error ? err.message : "Ödəniş uğursuz oldu");
    } finally {
      setBusy(false);
    }
  };

  if (loading || !order) {
    return <div className="flex h-full items-center justify-center text-muted-foreground">Yüklənir...</div>;
  }

  const isPaid = order.status === "paid";
  const editingLocked = isPaid && branding?.allowReceiptEditAfterPrint !== true;

  if (editingLocked) {
    return (
      <div className="flex h-full flex-col items-center justify-center gap-4 p-6 text-center">
        <p className="text-lg font-medium">Bu sifariş artıq ödənilib.</p>
        <Button onClick={() => router.replace("/pos")}>Masalara qayıt</Button>
      </div>
    );
  }

  const canPrintReceipt = !(isWaiterRole() && branding?.waiterCanPrintCustomerReceipt === false);

  return (
    <div className="flex h-full flex-col md:flex-row">
      {/* Menu */}
      <div className="flex flex-1 flex-col overflow-hidden">
        <div className="flex items-center gap-2 border-b bg-background px-3 py-2">
          <Button variant="ghost" size="icon-sm" onClick={() => router.push("/pos")}>
            <ArrowLeft className="h-4 w-4" />
          </Button>
          <span className="font-semibold">{order.tableName}</span>
        </div>
        <div className="flex gap-2 overflow-x-auto border-b bg-background px-3 py-2">
          {categories.map((cat) => (
            <button
              key={cat.id}
              type="button"
              onClick={() => setActiveCategoryId(cat.id)}
              style={branding?.categoryFontSize ? { fontSize: `${branding.categoryFontSize}px` } : undefined}
              className={cn(
                "shrink-0 rounded-full px-4 py-2 text-sm font-medium transition-colors",
                cat.id === activeCategoryId
                  ? "bg-primary text-primary-foreground"
                  : "bg-muted text-muted-foreground hover:bg-muted/70",
              )}
            >
              {cat.name}
            </button>
          ))}
        </div>
        <div className="flex-1 overflow-y-auto p-3">
          <div className="grid grid-cols-2 gap-3 sm:grid-cols-3 lg:grid-cols-4">
            {itemsInCategory.map((item) => (
              <button
                key={item.id}
                type="button"
                disabled={busy}
                onClick={() => void handleAddItem(item.id)}
                className="flex h-24 flex-col items-center justify-center gap-1 rounded-xl border bg-card p-2 text-center shadow-sm transition-transform active:scale-95 disabled:opacity-50"
              >
                <span className="text-sm font-semibold leading-tight">{item.name}</span>
                <span className="text-xs text-muted-foreground">{item.price.toFixed(2)} ₼</span>
              </button>
            ))}
            {itemsInCategory.length === 0 && (
              <p className="col-span-full py-8 text-center text-sm text-muted-foreground">
                Bu kateqoriyada məhsul yoxdur
              </p>
            )}
          </div>
        </div>
      </div>

      {/* Cart */}
      <div className="flex w-full flex-col border-t bg-background md:w-[360px] md:border-l md:border-t-0">
        <div className="flex-1 overflow-y-auto p-3">
          {order.lines.length === 0 && (
            <p className="py-8 text-center text-sm text-muted-foreground">Sifariş boşdur</p>
          )}
          <div className="space-y-2">
            {order.lines.map((line) => (
              <div key={line.id} className="rounded-lg border p-2">
                <div className="flex items-start justify-between gap-2">
                  <span className="text-sm font-medium">{line.menuItemName}</span>
                  <button
                    type="button"
                    onClick={() => void handleRemoveLine(line.id)}
                    className="text-muted-foreground hover:text-destructive"
                  >
                    <Trash2 className="h-4 w-4" />
                  </button>
                </div>
                <div className="mt-1 flex items-center justify-between">
                  <div className="flex items-center gap-2">
                    <button
                      type="button"
                      disabled={busy}
                      onClick={() => void handleQuantityChange(line.id, line.quantity, -1)}
                      className="flex h-7 w-7 items-center justify-center rounded-md border disabled:opacity-50"
                    >
                      <Minus className="h-3 w-3" />
                    </button>
                    <span className="w-6 text-center text-sm font-semibold">{line.quantity}</span>
                    <button
                      type="button"
                      disabled={busy}
                      onClick={() => void handleQuantityChange(line.id, line.quantity, 1)}
                      className="flex h-7 w-7 items-center justify-center rounded-md border disabled:opacity-50"
                    >
                      <Plus className="h-3 w-3" />
                    </button>
                  </div>
                  <span className="text-sm font-semibold">{line.lineTotal.toFixed(2)} ₼</span>
                </div>
              </div>
            ))}
          </div>
        </div>
        <div className="border-t p-3">
          <div className="mb-3 flex items-center justify-between text-lg font-bold">
            <span>Cəm</span>
            <span>{order.totalAmount.toFixed(2)} ₼</span>
          </div>
          {isPaid ? (
            <div className="space-y-2">
              <p className="text-center text-sm text-muted-foreground">
                Sifariş ödənilib, düzəlişlər avtomatik saxlanılır.
              </p>
              <Button className="h-12 w-full text-base font-semibold" onClick={() => router.replace("/pos")}>
                Masalara qayıt
              </Button>
            </div>
          ) : (
            <Button
              className="h-12 w-full text-base font-semibold"
              disabled={order.lines.length === 0 || busy}
              onClick={openPayDialog}
            >
              Ödə
            </Button>
          )}
        </div>
      </div>

      {/* Payment dialog */}
      <Dialog open={payOpen} onOpenChange={setPayOpen}>
        <DialogContent className="sm:max-w-sm">
          <DialogHeader>
            <DialogTitle>Ödəniş</DialogTitle>
            <DialogDescription>Cəm: {order.totalAmount.toFixed(2)} ₼</DialogDescription>
          </DialogHeader>
          <div className="space-y-3">
            <div className="grid grid-cols-2 gap-2">
              <Button
                type="button"
                variant={paymentMethod === "Cash" ? "default" : "outline"}
                onClick={() => setPaymentMethod("Cash")}
              >
                Nəğd
              </Button>
              <Button
                type="button"
                variant={paymentMethod === "Card" ? "default" : "outline"}
                onClick={() => {
                  setPaymentMethod("Card");
                  setPaidAmountInput(order.totalAmount.toFixed(2));
                }}
              >
                Kart
              </Button>
            </div>
            {paymentMethod === "Cash" && (
              <div className="space-y-2">
                <Label htmlFor="paid-amount">Alınan məbləğ</Label>
                <Input
                  id="paid-amount"
                  type="number"
                  step="0.01"
                  value={paidAmountInput}
                  onChange={(e) => setPaidAmountInput(e.target.value)}
                />
                <p className="text-sm text-muted-foreground">Qalıq: {changeAmount.toFixed(2)} ₼</p>
              </div>
            )}
          </div>
          <DialogFooter>
            <Button variant="outline" onClick={() => setPayOpen(false)} disabled={busy}>
              Ləğv et
            </Button>
            <Button onClick={() => void handleConfirmPayment()} disabled={busy}>
              Təsdiqlə
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* Receipt dialog */}
      <Dialog
        open={receipt !== null}
        onOpenChange={(open) => {
          if (!open) {
            setReceipt(null);
            router.replace("/pos");
          }
        }}
      >
        <DialogContent className="sm:max-w-sm">
          <div
            id="receipt-print-area"
            style={branding?.receiptFontSize ? { fontSize: `${branding.receiptFontSize}px` } : undefined}
          >
            <DialogHeader>
              {branding?.reportLogoUrl && (
                // eslint-disable-next-line @next/next/no-img-element
                <img
                  src={branding.reportLogoUrl}
                  alt=""
                  className="mx-auto mb-1 h-12 w-auto object-contain"
                />
              )}
              <DialogTitle
                className="flex items-center gap-2"
                style={branding?.productColor ? { color: branding.productColor } : undefined}
              >
                <Receipt className="h-5 w-5" />
                Qəbz #{receipt?.receiptNumber}
              </DialogTitle>
              <DialogDescription>
                {receipt?.restaurantName} — {receipt?.tableName}
                {branding?.floorLabel && ` — ${branding.floorLabel}`}
              </DialogDescription>
            </DialogHeader>
            <div className="space-y-1 text-sm">
              {receipt?.lines.map((line, i) => (
                <div key={i} className="flex justify-between">
                  <span>
                    {line.quantity} × {line.menuItemName}
                  </span>
                  <span>{line.lineTotal.toFixed(2)} ₼</span>
                </div>
              ))}
              <div
                className="mt-2 border-t pt-2 font-semibold"
                style={branding?.productColor ? { color: branding.productColor } : undefined}
              >
                <div className="flex justify-between">
                  <span>Cəm</span>
                  <span>{receipt?.totalAmount.toFixed(2)} ₼</span>
                </div>
                {receipt && receipt.paymentMethod === "Cash" && (
                  <>
                    <div className="flex justify-between font-normal text-muted-foreground">
                      <span>Alınan</span>
                      <span>{receipt.paidAmount.toFixed(2)} ₼</span>
                    </div>
                    <div className="flex justify-between font-normal text-muted-foreground">
                      <span>Qalıq</span>
                      <span>{receipt.changeAmount.toFixed(2)} ₼</span>
                    </div>
                  </>
                )}
              </div>
              {(branding?.slogan || branding?.contactPhoneNumber || branding?.socialLinks) && (
                <div className="mt-3 border-t pt-2 text-center text-xs text-muted-foreground">
                  {branding?.slogan && <p>{branding.slogan}</p>}
                  {branding?.contactPhoneNumber && <p>{branding.contactPhoneNumber}</p>}
                  {branding?.socialLinks && <p>{branding.socialLinks}</p>}
                </div>
              )}
            </div>
          </div>
          <DialogFooter className="flex-col gap-2 sm:flex-col">
            {canPrintReceipt && (
              <Button
                variant="outline"
                className="w-full"
                onClick={() => window.print()}
              >
                <Printer className="mr-2 h-4 w-4" />
                Qəbzi çap et
              </Button>
            )}
            <Button
              className="w-full"
              onClick={() => {
                setReceipt(null);
                router.replace("/pos");
              }}
            >
              Bağla
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      <style>{`
        @media print {
          body * { visibility: hidden; }
          #receipt-print-area, #receipt-print-area * { visibility: visible; }
          #receipt-print-area { position: fixed; top: 0; left: 0; width: 100%; padding: 16px; }
        }
      `}</style>
    </div>
  );
}
