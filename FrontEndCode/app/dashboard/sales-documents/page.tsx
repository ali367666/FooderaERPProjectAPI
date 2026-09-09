"use client";

import { useCallback, useEffect, useMemo, useState } from "react";
import { Info, Printer, X } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { toast } from "sonner";
import { hasPermission, usePermissionSet } from "@/hooks/use-auth-permissions";
import { formatCurrency } from "@/lib/format-currency";
import {
  deleteOrder,
  getOrderReceipt,
  getOrders,
  type OrderDto,
} from "@/lib/services/order-service";

function isClosedSale(order: OrderDto): boolean {
  return order.isPaid || order.status === "cancelled";
}

export default function SalesDocumentsPage() {
  const permissions = usePermissionSet();
  const canDelete = hasPermission("Pos.DeleteReceipt", permissions);

  const [orders, setOrders] = useState<OrderDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [detailOrder, setDetailOrder] = useState<OrderDto | null>(null);

  const loadOrders = useCallback(async () => {
    setLoading(true);
    try {
      const data = await getOrders();
      setOrders(data.filter(isClosedSale));
    } catch (e) {
      toast.error(e instanceof Error ? e.message : "Satış sənədləri yüklənmədi.");
      setOrders([]);
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    void loadOrders();
  }, [loadOrders]);

  const rows = useMemo(
    () =>
      [...orders].sort((a, b) => {
        const ta = new Date(a.paidAt ?? a.closedAt ?? a.openedAt).getTime();
        const tb = new Date(b.paidAt ?? b.closedAt ?? b.openedAt).getTime();
        return tb - ta;
      }),
    [orders],
  );

  const discountPercent = (order: OrderDto): number => {
    const subtotal = order.totalAmount + order.discountAmount;
    if (subtotal <= 0) return 0;
    return Math.round((order.discountAmount / subtotal) * 1000) / 10;
  };

  const printOrder = useCallback(async (order: OrderDto) => {
    try {
      const receipt = await getOrderReceipt(order.id);
      const html = `
        <html><body>
        <h3>${receipt.restaurantName}</h3>
        <p>Çek: ${receipt.receiptNumber}</p>
        <p>Sifariş: ${receipt.orderNumber}</p>
        <p>Masa: ${receipt.tableName}</p>
        <p>Ofisiant: ${receipt.waiterName}</p>
        <hr />
        ${receipt.lines.map((x) => `<div>${x.menuItemName} x${x.quantity} = ${formatCurrency(x.lineTotal)}</div>`).join("")}
        <hr />
        <p>Cəm: ${formatCurrency(receipt.totalAmount)}</p>
        <p>Ödənilib: ${formatCurrency(receipt.paidAmount)}</p>
        <p>Qalıq: ${formatCurrency(receipt.changeAmount)}</p>
        </body></html>
      `;
      const printWindow = window.open("", "_blank", "width=500,height=700");
      if (!printWindow) return;
      printWindow.document.write(html);
      printWindow.document.close();
      printWindow.focus();
      printWindow.print();
      printWindow.close();
    } catch (e) {
      toast.error(e instanceof Error ? e.message : "Çek çap edilmədi.");
    }
  }, []);

  const handleDelete = useCallback(
    async (order: OrderDto) => {
      if (!canDelete) return;
      if (!window.confirm(`"${order.receiptNumber ?? order.orderNumber}" çekini silmək istəyirsiniz? Bu geri qaytarılmır.`)) return;
      try {
        await deleteOrder(order.id);
        toast.success("Çek silindi.");
        await loadOrders();
      } catch (e) {
        toast.error(e instanceof Error ? e.message : "Çek silinmədi.");
      }
    },
    [canDelete, loadOrders],
  );

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-3xl font-bold text-foreground">Satış Sənədləri</h1>
        <p className="text-muted-foreground mt-1">
          Bütün ödənilmiş və ləğv edilmiş çeklərin siyahısı — hər çekin məhsul detalını görmək üçün{" "}
          <Info className="inline h-3.5 w-3.5" /> düyməsinə basın.
        </p>
      </div>

      <div className="rounded-lg border bg-card">
        <div className="overflow-x-auto">
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b bg-muted/50">
                <th className="px-4 py-2 text-left font-medium text-muted-foreground">Çap Nömrəsi</th>
                <th className="px-4 py-2 text-left font-medium text-muted-foreground">Tarix</th>
                <th className="px-4 py-2 text-left font-medium text-muted-foreground">Saat</th>
                <th className="px-4 py-2 text-left font-medium text-muted-foreground">Masa</th>
                <th className="px-4 py-2 text-right font-medium text-muted-foreground">Toplam</th>
                <th className="px-4 py-2 text-right font-medium text-muted-foreground">Endirim</th>
                <th className="px-4 py-2 text-right font-medium text-muted-foreground">Endirim%</th>
                <th className="px-4 py-2 text-right font-medium text-muted-foreground">Nağd</th>
                <th className="px-4 py-2 text-right font-medium text-muted-foreground">Pos</th>
                <th className="px-4 py-2 text-left font-medium text-muted-foreground">Status</th>
                <th className="px-3 py-2 text-left font-medium text-muted-foreground">Əməliyyat</th>
              </tr>
            </thead>
            <tbody>
              {loading ? (
                <tr>
                  <td colSpan={11} className="px-4 py-8 text-center text-muted-foreground">
                    Yüklənir…
                  </td>
                </tr>
              ) : rows.length === 0 ? (
                <tr>
                  <td colSpan={11} className="px-4 py-8 text-center text-muted-foreground">
                    Satış sənədi tapılmadı.
                  </td>
                </tr>
              ) : (
                rows.map((order) => {
                  const dt = new Date(order.paidAt ?? order.closedAt ?? order.openedAt);
                  const isCancelled = order.status === "cancelled";
                  return (
                    <tr key={order.id} className="border-b last:border-0">
                      <td className="px-4 py-2 font-medium">{order.receiptNumber ?? order.orderNumber}</td>
                      <td className="px-4 py-2">{dt.toLocaleDateString("az-AZ")}</td>
                      <td className="px-4 py-2">{dt.toLocaleTimeString("az-AZ", { hour: "2-digit", minute: "2-digit" })}</td>
                      <td className="px-4 py-2">{order.tableName ?? `#${order.tableId}`}</td>
                      <td className="px-4 py-2 text-right">{formatCurrency(order.totalAmount)}</td>
                      <td className="px-4 py-2 text-right">{formatCurrency(order.discountAmount)}</td>
                      <td className="px-4 py-2 text-right">{discountPercent(order)}%</td>
                      <td className="px-4 py-2 text-right">
                        {order.paymentMethod === "Cash" ? formatCurrency(order.totalAmount) : "0,00"}
                      </td>
                      <td className="px-4 py-2 text-right">
                        {order.paymentMethod === "Card" ? formatCurrency(order.totalAmount) : "0,00"}
                      </td>
                      <td className="px-4 py-2">
                        {isCancelled ? (
                          <Badge className="bg-rose-100 text-rose-800 hover:bg-rose-100">
                            Ləğv edilib
                            {order.processedByUserName && (
                              <span className="ml-1 font-normal">
                                — {order.processedByUserName}, {dt.toLocaleString("az-AZ")}
                              </span>
                            )}
                          </Badge>
                        ) : (
                          <Badge className="bg-emerald-100 text-emerald-800 hover:bg-emerald-100">Ödənilib</Badge>
                        )}
                      </td>
                      <td className="px-3 py-2">
                        <div className="flex items-center gap-1">
                          <Button variant="ghost" size="icon" title="Detallar" onClick={() => setDetailOrder(order)}>
                            <Info className="h-4 w-4" />
                          </Button>
                          <Button variant="ghost" size="icon" title="Çap et" onClick={() => void printOrder(order)}>
                            <Printer className="h-4 w-4" />
                          </Button>
                          <Button
                            variant="ghost"
                            size="icon"
                            title={canDelete ? "Çeki sil" : "Bu əməliyyat üçün icazəniz yoxdur"}
                            disabled={!canDelete}
                            onClick={() => void handleDelete(order)}
                          >
                            <X className="h-4 w-4 text-destructive" />
                          </Button>
                        </div>
                      </td>
                    </tr>
                  );
                })
              )}
            </tbody>
          </table>
        </div>
      </div>

      <Dialog open={detailOrder != null} onOpenChange={(o) => !o && setDetailOrder(null)}>
        <DialogContent className="sm:max-w-lg">
          <DialogHeader>
            <DialogTitle>{detailOrder?.receiptNumber ?? detailOrder?.orderNumber}</DialogTitle>
            <DialogDescription>
              Masa: {detailOrder?.tableName ?? `#${detailOrder?.tableId}`} · Ofisiant: {detailOrder?.waiterName ?? "-"}
            </DialogDescription>
          </DialogHeader>
          {detailOrder && (
            <div className="space-y-3">
              <div className="max-h-72 overflow-y-auto rounded-md border">
                <table className="w-full text-sm">
                  <thead>
                    <tr className="border-b bg-muted/50">
                      <th className="px-3 py-2 text-left font-medium text-muted-foreground">Məhsul</th>
                      <th className="px-3 py-2 text-right font-medium text-muted-foreground">Miqdar</th>
                      <th className="px-3 py-2 text-right font-medium text-muted-foreground">Qiymət</th>
                      <th className="px-3 py-2 text-right font-medium text-muted-foreground">Məbləğ</th>
                    </tr>
                  </thead>
                  <tbody>
                    {detailOrder.lines
                      .filter((l) => l.status !== "cancelled")
                      .map((line) => (
                        <tr key={line.id} className="border-b last:border-0">
                          <td className="px-3 py-2">
                            {line.menuItemName}
                            {line.note && <span className="block text-xs text-muted-foreground">{line.note}</span>}
                          </td>
                          <td className="px-3 py-2 text-right">{line.quantity}</td>
                          <td className="px-3 py-2 text-right">{formatCurrency(line.unitPrice)}</td>
                          <td className="px-3 py-2 text-right">{formatCurrency(line.lineTotal)}</td>
                        </tr>
                      ))}
                  </tbody>
                </table>
              </div>
              <div className="flex items-center justify-between text-sm">
                <span className="text-muted-foreground">
                  {new Date(detailOrder.paidAt ?? detailOrder.closedAt ?? detailOrder.openedAt).toLocaleString("az-AZ")}
                </span>
                <span className="text-base font-semibold">Toplam: {formatCurrency(detailOrder.totalAmount)}</span>
              </div>
              <div className="flex justify-end gap-2">
                <Button variant="outline" onClick={() => setDetailOrder(null)}>
                  Bağla
                </Button>
                <Button onClick={() => void printOrder(detailOrder)}>
                  <Printer className="mr-1 h-4 w-4" />
                  Çap et
                </Button>
              </div>
            </div>
          )}
        </DialogContent>
      </Dialog>
    </div>
  );
}
