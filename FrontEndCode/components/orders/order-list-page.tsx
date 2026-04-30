"use client";

import { useCallback, useEffect, useMemo, useState } from "react";
import { useRouter } from "next/navigation";
import { Eye, Pencil, Printer } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { toApiFormError } from "@/lib/api-error";
import {
  cancelOrder,
  getOrderReceipt,
  getOrderStatusValue,
  getOrders,
  isOrderPaid,
  payOrder,
  serveOrder,
  type OrderDto,
} from "@/lib/services/order-service";
import { toast } from "sonner";
import { hasPermission, usePermissionSet } from "@/hooks/use-auth-permissions";
import { formatCurrency } from "@/lib/format-currency";

export function OrderListPage() {
  const router = useRouter();
  const permissions = usePermissionSet();
  console.log("Current user permissions:", Array.from(permissions));
  console.log("Can create order:", hasPermission("Permissions.Orders.Create", permissions));
  const canCreateOrder = hasPermission("Permissions.Orders.Create", permissions);
  const [orders, setOrders] = useState<OrderDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [pendingById, setPendingById] = useState<Record<number, boolean>>({});
  const [selectedOrderId, setSelectedOrderId] = useState<number | null>(null);

  const loadOrders = useCallback(async () => {
    try {
      setLoading(true);
      setError(null);
      setOrders(await getOrders());
    } catch (e) {
      setError(toApiFormError(e, "Failed to load orders").message);
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    void loadOrders();
    const onRefresh = () => void loadOrders();
    window.addEventListener("orders:refresh", onRefresh);
    return () => window.removeEventListener("orders:refresh", onRefresh);
  }, [loadOrders]);

  const statusClass = useMemo(
    () => ({
      draft: "bg-slate-100 text-slate-800",
      open: "bg-amber-100 text-amber-800",
      in_preparation: "bg-blue-100 text-blue-800",
      ready: "bg-lime-100 text-lime-800",
      served: "bg-purple-100 text-purple-800",
      paid: "bg-emerald-100 text-emerald-800",
      cancelled: "bg-rose-100 text-rose-800",
    }),
    [],
  );

  const runAction = useCallback(
    async (orderId: number, action: "serve" | "cancel" | "pay") => {
      try {
        setPendingById((prev) => ({ ...prev, [orderId]: true }));
        if (action === "serve") {
          await serveOrder(orderId);
          toast.success("Order served");
        } else if (action === "cancel") {
          const updated = await cancelOrder(orderId);
          setOrders((prev) => prev.map((x) => (x.id === updated.id ? updated : x)));
          toast.success("Order cancelled");
        } else {
          const selected = orders.find((x) => x.id === orderId);
          const total = selected?.totalAmount ?? 0;
          const paidAmountRaw = window.prompt("Paid amount", String(total));
          if (!paidAmountRaw) return;
          const paidAmount = Number(paidAmountRaw);
          if (!Number.isFinite(paidAmount) || paidAmount <= 0) {
            toast.error("Please enter a valid paid amount.");
            return;
          }
          await payOrder(orderId, { paymentMethod: "Cash", paidAmount });
          toast.success("Order paid");
        }
        await loadOrders();
      } catch (e) {
        toast.error(toApiFormError(e, `Failed to ${action} order`).message);
      } finally {
        setPendingById((prev) => ({ ...prev, [orderId]: false }));
      }
    },
    [loadOrders, orders],
  );

  const selectedOrder = useMemo(
    () => orders.find((x) => x.id === selectedOrderId) ?? null,
    [orders, selectedOrderId],
  );

  const selectedStatusValue = getOrderStatusValue(selectedOrder?.statusRaw ?? selectedOrder?.status);
  const selectedIsPaid = isOrderPaid(selectedOrder);
  const selectedIsCancelled = selectedStatusValue === 6;
  const canEditSelected = !!selectedOrder && !selectedIsPaid && !selectedIsCancelled;
  const canServeSelected = !!selectedOrder && !selectedIsPaid && selectedStatusValue === 3;
  const canPaySelected =
    !!selectedOrder && !selectedIsPaid && (selectedStatusValue === 3 || selectedStatusValue === 4);
  const canCancelSelected = !!selectedOrder && !selectedIsPaid && !selectedIsCancelled;
  const canPrintSelected = !!selectedOrder && selectedIsPaid;

  const printReceipt = useCallback(async () => {
    if (!selectedOrder) return;
    try {
      const receipt = await getOrderReceipt(selectedOrder.id);
      const html = `
        <html><body>
        <h3>${receipt.restaurantName}</h3>
        <p>Receipt: ${receipt.receiptNumber}</p>
        <p>Order: ${receipt.orderNumber}</p>
        <p>Table: ${receipt.tableName}</p>
        <p>Waiter: ${receipt.waiterName}</p>
        <hr />
        ${receipt.lines.map((x) => `<div>${x.menuItemName} x${x.quantity} = ${formatCurrency(x.lineTotal)}</div>`).join("")}
        <hr />
        <p>Total: ${formatCurrency(receipt.totalAmount)}</p>
        <p>Paid: ${formatCurrency(receipt.paidAmount)}</p>
        <p>Change: ${formatCurrency(receipt.changeAmount)}</p>
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
      toast.error(toApiFormError(e, "Failed to print receipt").message);
    }
  }, [selectedOrder]);

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold">Orders</h1>
          <p className="text-muted-foreground mt-1">Master list of restaurant orders</p>
        </div>
        <div className="flex gap-2">
          <Button onClick={() => router.push("/dashboard/orders/create")} disabled={!canCreateOrder}>
            Create Order
          </Button>
          <Button variant="outline" onClick={() => void loadOrders()} disabled={loading}>
            Refresh
          </Button>
        </div>
      </div>

      {error ? (
        <div className="rounded-md border border-destructive/40 bg-destructive/10 px-4 py-3 text-sm text-destructive">
          {error}
        </div>
      ) : null}

      <Card>
        <CardHeader>
          <CardTitle>Order List</CardTitle>
          <CardDescription>{orders.length} orders</CardDescription>
        </CardHeader>
        <CardContent className="p-0">
          {selectedOrder ? (
            <div className="flex flex-wrap items-center gap-2 border-b px-4 py-3">
              <span className="text-sm font-medium">Selected Order: {selectedOrder.orderNumber}</span>
              <Button size="sm" variant="outline" onClick={() => router.push(`/dashboard/orders/${selectedOrder.id}?mode=view`)}>
                <Eye className="mr-1 h-4 w-4" />
                View
              </Button>
              <Button size="sm" disabled={!canEditSelected} onClick={() => router.push(`/dashboard/orders/${selectedOrder.id}?mode=edit`)}>
                <Pencil className="mr-1 h-4 w-4" />
                Edit
              </Button>
              <Button size="sm" variant="secondary" disabled={!canServeSelected || pendingById[selectedOrder.id]} onClick={() => void runAction(selectedOrder.id, "serve")}>
                Serve
              </Button>
              <Button size="sm" disabled={!canPaySelected || pendingById[selectedOrder.id]} onClick={() => void runAction(selectedOrder.id, "pay")}>
                Paid / Odenildi
              </Button>
              <Button size="sm" variant="destructive" disabled={!canCancelSelected || pendingById[selectedOrder.id]} onClick={() => void runAction(selectedOrder.id, "cancel")}>
                Cancel
              </Button>
              <Button size="sm" variant="outline" disabled={!canPrintSelected} onClick={() => void printReceipt()}>
                <Printer className="mr-1 h-4 w-4" />
                Print Receipt
              </Button>
            </div>
          ) : null}
          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead>
                <tr className="border-b bg-muted/50">
                  <th className="w-10 px-3 py-2 text-left">Sel</th>
                  <th className="px-4 py-3 text-left">Order Number</th>
                  <th className="px-4 py-3 text-left">Restaurant</th>
                  <th className="px-4 py-3 text-left">Table</th>
                  <th className="px-4 py-3 text-left">Waiter</th>
                  <th className="px-4 py-3 text-left">Total Amount</th>
                  <th className="px-4 py-3 text-left">Status</th>
                  <th className="px-4 py-3 text-left">Payment</th>
                  <th className="px-4 py-3 text-left">Date</th>
                  <th className="w-20 px-3 py-2 text-left">Action</th>
                </tr>
              </thead>
              <tbody>
                {loading ? (
                  <tr>
                    <td colSpan={10} className="px-4 py-8 text-center text-muted-foreground">
                      Loading orders...
                    </td>
                  </tr>
                ) : orders.length === 0 ? (
                  <tr>
                    <td colSpan={10} className="px-4 py-8 text-center text-muted-foreground">
                      No orders found.
                    </td>
                  </tr>
                ) : (
                  orders.map((order) => (
                    <tr key={order.id} className="border-b">
                      {(() => {
                        const paid = isOrderPaid(order);
                        return (
                          <>
                      <td className="px-3 py-2">
                        <input
                          type="radio"
                          name="selectedOrder"
                          checked={selectedOrderId === order.id}
                          onChange={() => setSelectedOrderId(order.id)}
                        />
                      </td>
                      <td className="px-4 py-3 font-medium">{order.orderNumber}</td>
                      <td className="px-4 py-3">{order.restaurantName || "-"}</td>
                      <td className="px-4 py-3">{order.tableName || `#${order.tableId}`}</td>
                      <td className="px-4 py-3">{order.waiterName || "-"}</td>
                      <td className="px-4 py-3">{formatCurrency(order.totalAmount)}</td>
                      <td className="px-4 py-3">
                        <Badge className={`${statusClass[order.status]} px-2 py-0.5 text-xs`}>{order.statusRaw}</Badge>
                      </td>
                      <td className="px-4 py-3">
                        <Badge className={`${paid ? "bg-emerald-100 text-emerald-800" : "bg-amber-100 text-amber-800"} px-2 py-0.5 text-xs`}>
                          {paid ? "Paid" : "Unpaid"}
                        </Badge>
                      </td>
                      <td className="px-4 py-3">
                        {order.openedAt ? new Date(order.openedAt).toLocaleString() : "-"}
                      </td>
                      <td className="px-3 py-2">
                        <Button variant="ghost" size="icon" onClick={() => router.push(`/dashboard/orders/${order.id}?mode=view`)}>
                          <Eye className="h-4 w-4" />
                        </Button>
                      </td>
                          </>
                        );
                      })()}
                    </tr>
                  ))
                )}
              </tbody>
            </table>
          </div>
        </CardContent>
      </Card>
    </div>
  );
}
