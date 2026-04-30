"use client";

import { useCallback, useEffect, useMemo, useState } from "react";
import { useParams, useRouter, useSearchParams } from "next/navigation";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Badge } from "@/components/ui/badge";
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogDescription,
} from "@/components/ui/dialog";
import { toast } from "sonner";
import { getRestaurants } from "@/lib/services/restaurant-service";
import { getRestaurantTables } from "@/lib/services/restaurant-table-service";
import { getEmployeesByPosition, type Employee } from "@/lib/services/employee-service";
import { getMenuItems } from "@/lib/services/menu-item-service";
import { toApiFormError } from "@/lib/api-error";
import { useSelectedCompany } from "@/contexts/selected-company-context";
import {
  addOrderLine,
  createOrder,
  deleteOrderLine,
  getOrderById,
  getOrderReceipt,
  getOrderStatusValue,
  isOrderPaid,
  payOrder,
  serveOrder,
  submitOrder,
  updateOrder,
  updateOrderLine,
  type OrderDto,
  type OrderLineDto,
  type OrderReceiptDto,
  type PaymentMethod,
} from "@/lib/services/order-service";
import { formatCurrency } from "@/lib/format-currency";

type Variant = "create" | "edit";

export function OrderFormPage({ variant }: { variant: Variant }) {
  const receiptEndpointAvailable = false;
  const router = useRouter();
  const params = useParams();
  const searchParams = useSearchParams();
  const { selectedCompanyId } = useSelectedCompany();
  const orderId = Number(params.id);
  const viewMode = (searchParams.get("mode") ?? "edit") === "view";

  const [order, setOrder] = useState<OrderDto | null>(null);
  const [loading, setLoading] = useState(variant === "edit");
  const [saving, setSaving] = useState(false);
  const [paymentModalOpen, setPaymentModalOpen] = useState(false);
  const [receiptOpen, setReceiptOpen] = useState(false);
  const [paymentMethod, setPaymentMethod] = useState<PaymentMethod>("Cash");
  const [paidAmount, setPaidAmount] = useState("0");
  const [receipt, setReceipt] = useState<OrderReceiptDto | null>(null);

  const [restaurants, setRestaurants] = useState<Array<{ id: number; name: string; companyId: number }>>([]);
  const [tables, setTables] = useState<Array<{ id: number; restaurantId: number; name: string }>>([]);
  const [waiters, setWaiters] = useState<Employee[]>([]);
  const [menuItems, setMenuItems] = useState<
    Array<{ id: number; name: string; price: number; preparationType: number; isActive: boolean }>
  >([]);

  const [restaurantId, setRestaurantId] = useState("");
  const [tableId, setTableId] = useState("");
  const [waiterId, setWaiterId] = useState("");
  const [note, setNote] = useState("");

  const [newLineMenuItemId, setNewLineMenuItemId] = useState("");
  const [newLineQty, setNewLineQty] = useState("1");
  const [newLineNote, setNewLineNote] = useState("");

  const statusValue = getOrderStatusValue(order?.statusRaw ?? order?.status);
  const isPaid = isOrderPaid(order);

  const readOnly = viewMode || (order != null && (isPaid || statusValue === 6));

  const loadDependencies = useCallback(async () => {
    const [restaurantRows, tableRows, menuRows] = await Promise.all([
      getRestaurants(),
      getRestaurantTables(),
      getMenuItems(),
    ]);
    setRestaurants(restaurantRows.map((x) => ({ id: x.id, name: x.name, companyId: x.companyId })));
    setTables(tableRows.map((x) => ({ id: x.id, restaurantId: x.restaurantId, name: x.name })));
    setMenuItems(menuRows.map((x) => ({ id: x.id, name: x.name, price: x.price, preparationType: x.preparationType, isActive: x.isActive })));
  }, []);

  const loadOrder = useCallback(async () => {
    if (variant !== "edit" || !Number.isFinite(orderId) || orderId <= 0) return;
    const data = await getOrderById(orderId);
    setOrder(data);
    setRestaurantId(String(data.restaurantId));
    setTableId(String(data.tableId));
    setWaiterId(String(data.waiterId));
    setNote(data.note ?? "");
  }, [variant, orderId]);

  useEffect(() => {
    (async () => {
      try {
        await loadDependencies();
        if (variant === "edit") {
          await loadOrder();
        }
      } catch (e) {
        toast.error(toApiFormError(e, "Failed to load order form").message);
      } finally {
        setLoading(false);
      }
    })();
  }, [variant, loadDependencies, loadOrder]);

  useEffect(() => {
    const onRefresh = () => {
      if (variant === "edit") void loadOrder();
    };
    window.addEventListener("orders:refresh", onRefresh);
    return () => window.removeEventListener("orders:refresh", onRefresh);
  }, [variant, loadOrder]);

  const visibleTables = useMemo(() => {
    const rid = Number(restaurantId);
    return tables.filter((t) => t.restaurantId === rid);
  }, [tables, restaurantId]);

  const effectiveSelectedCompanyId = useMemo(() => {
    if (selectedCompanyId != null && selectedCompanyId > 0) return selectedCompanyId;
    if (typeof window === "undefined") return null;
    const stored = Number(window.localStorage.getItem("dashboardSelectedCompanyId") ?? "");
    return Number.isFinite(stored) && stored > 0 ? stored : null;
  }, [selectedCompanyId]);

  const loadWaiters = useCallback(async () => {
    if (effectiveSelectedCompanyId == null || effectiveSelectedCompanyId <= 0) {
      setWaiters([]);
      return;
    }

    console.log("selectedCompanyId", effectiveSelectedCompanyId);
    console.log("waiter api url params", {
      positionName: "Waiter",
      companyId: effectiveSelectedCompanyId,
    });
    const data = await getEmployeesByPosition("Waiter", effectiveSelectedCompanyId);
    console.log("waiters result:", data);
    setWaiters(data);
  }, [effectiveSelectedCompanyId]);

  useEffect(() => {
    void loadWaiters().catch((e) => {
      toast.error(toApiFormError(e, "Failed to load waiters").message);
      setWaiters([]);
    });
  }, [loadWaiters]);

  const refresh = useCallback(async () => {
    if (variant === "edit") {
      const fresh = await getOrderById(orderId);
      setOrder(fresh);
      window.dispatchEvent(new Event("orders:refresh"));
    }
  }, [variant, orderId]);

  const saveMaster = useCallback(async () => {
    if (readOnly) return;
    const rid = Number(restaurantId);
    const tid = Number(tableId);
    const wid = Number(waiterId);
    if (!rid || !tid || !wid) {
      toast.error("Restaurant, table and waiter are required.");
      return;
    }

    try {
      setSaving(true);
      if (variant === "create") {
        const created = await createOrder({ restaurantId: rid, tableId: tid, waiterId: wid, note: note.trim() || null });
        toast.success("Order created");
        router.push(`/dashboard/orders/${created.id}?mode=edit`);
        return;
      }

      if (!order) return;
      await updateOrder({
        id: order.id,
        restaurantId: rid,
        tableId: tid,
        waiterId: wid,
        note: note.trim() || null,
        status: order.statusRaw,
      });
      toast.success("Order updated");
      await refresh();
    } catch (e) {
      toast.error(toApiFormError(e, "Failed to save order").message);
    } finally {
      setSaving(false);
    }
  }, [readOnly, restaurantId, tableId, waiterId, variant, note, router, order, refresh]);

  const addLine = useCallback(async () => {
    if (!order || readOnly) return;
    try {
      const menuItemId = Number(newLineMenuItemId);
      const quantity = Number(newLineQty);
      if (!menuItemId || quantity <= 0) {
        toast.error("Select menu item and quantity.");
        return;
      }
      await addOrderLine({ orderId: order.id, menuItemId, quantity, note: newLineNote.trim() || null });
      toast.success("Order line added");
      setNewLineMenuItemId("");
      setNewLineQty("1");
      setNewLineNote("");
      await refresh();
    } catch (e) {
      toast.error(toApiFormError(e, "Failed to add order line").message);
    }
  }, [order, readOnly, newLineMenuItemId, newLineQty, newLineNote, refresh]);

  const updateLine = useCallback(
    async (line: OrderLineDto) => {
      if (readOnly) return;
      try {
        await updateOrderLine({ id: line.id, quantity: line.quantity, note: line.note, status: line.status });
        toast.success("Order line updated");
        await refresh();
      } catch (e) {
        toast.error(toApiFormError(e, "Failed to update order line").message);
      }
    },
    [readOnly, refresh],
  );

  const removeLine = useCallback(
    async (lineId: number) => {
      if (readOnly) return;
      try {
        await deleteOrderLine(lineId);
        toast.success("Order line deleted");
        await refresh();
      } catch (e) {
        toast.error(toApiFormError(e, "Failed to delete order line").message);
      }
    },
    [readOnly, refresh],
  );

  const submitCurrentOrder = useCallback(async () => {
    if (!order || readOnly) return;
    try {
      await submitOrder(order.id);
      toast.success("Order submitted");
      await refresh();
    } catch (e) {
      toast.error(toApiFormError(e, "Failed to submit order").message);
    }
  }, [order, readOnly, refresh]);

  const isCreateMode = variant === "create" || !Number.isFinite(orderId) || orderId <= 0;
  const canServe = !!order?.id && !isPaid && statusValue === 3;
  const canShowPaidButton =
    !isCreateMode &&
    !!order?.id &&
    !isPaid &&
    (statusValue === 3 || statusValue === 4);

  useEffect(() => {
    if (!order) return;
    console.log("Order detail data", order);
  }, [order]);

  const confirmPayment = useCallback(async () => {
    if (!order) return;
    try {
      const paid = Number(paidAmount);
      if (!Number.isFinite(paid) || paid <= 0) {
        toast.error("Please enter a valid paid amount.");
        return;
      }
      const updated = await payOrder(order.id, {
        paymentMethod,
        paidAmount: paid,
      });
      if (updated) setOrder(updated);
      setPaymentModalOpen(false);
      toast.success("Order paid successfully.");
      await refresh();
      window.dispatchEvent(new Event("orders:refresh"));
    } catch (e) {
      toast.error(toApiFormError(e, "Failed to process payment").message);
    }
  }, [order, paidAmount, paymentMethod]);

  const markServed = useCallback(async () => {
    if (!order) return;
    try {
      await serveOrder(order.id);
      toast.success("Order marked as served.");
      await refresh();
    } catch (e) {
      toast.error(toApiFormError(e, "Failed to serve order").message);
    }
  }, [order, refresh]);

  const openReceipt = useCallback(async () => {
    if (!order || !receiptEndpointAvailable) return;
    try {
      const receiptData = await getOrderReceipt(order.id);
      setReceipt(receiptData);
      setReceiptOpen(true);
    } catch {
      // Receipt is optional; suppress error toast.
    }
  }, [order, receiptEndpointAvailable]);

  if (loading) return <p className="text-sm text-muted-foreground">Loading order...</p>;

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-semibold">{variant === "create" ? "Create Order" : `Order ${order?.orderNumber ?? ""}`}</h1>
        <div className="flex gap-2">
          <Button variant="outline" onClick={() => router.push("/dashboard/orders")}>
            Back
          </Button>
          {!readOnly ? (
            <Button onClick={() => void saveMaster()} disabled={saving}>
              Save Master
            </Button>
          ) : null}
          {order && order.status === "draft" && !readOnly ? (
            <Button variant="secondary" onClick={() => void submitCurrentOrder()}>
              Submit
            </Button>
          ) : null}
          {!isCreateMode && order?.id ? (
            <Badge className={isPaid ? "bg-emerald-100 text-emerald-800" : "bg-amber-100 text-amber-800"}>
              {isPaid ? "Paid / Odenilib" : "Unpaid"}
            </Badge>
          ) : null}
          {canShowPaidButton ? (
            <Button
              onClick={() => {
                if (isPaid) return;
                setPaidAmount(String(order.totalAmount));
                setPaymentMethod("Cash");
                setPaymentModalOpen(true);
              }}
            >
              Paid / Odenildi
            </Button>
          ) : null}
          {canServe ? (
            <Button variant="secondary" onClick={() => void markServed()}>
              Serve / Servis edildi
            </Button>
          ) : null}
          {receiptEndpointAvailable && order?.id ? (
            <Button variant="outline" onClick={() => void openReceipt()}>
              Receipt
            </Button>
          ) : null}
        </div>
      </div>

      <Card>
        <CardHeader>
          <CardTitle>Master</CardTitle>
        </CardHeader>
        <CardContent className="grid gap-4 md:grid-cols-3">
          <div>
            <label className="mb-1 block text-sm">Company</label>
            <Input value={order?.companyId ?? ""} readOnly />
          </div>
          <div>
            <label className="mb-1 block text-sm">Restaurant</label>
            <select
              className="h-10 w-full rounded-md border px-3"
              value={restaurantId}
              onChange={(e) => setRestaurantId(e.target.value)}
              disabled={readOnly}
            >
              <option value="">Select restaurant</option>
              {restaurants.map((x) => (
                <option key={x.id} value={x.id}>
                  {x.name}
                </option>
              ))}
            </select>
          </div>
          <div>
            <label className="mb-1 block text-sm">Table</label>
            <select className="h-10 w-full rounded-md border px-3" value={tableId} onChange={(e) => setTableId(e.target.value)} disabled={readOnly}>
              <option value="">Select table</option>
              {visibleTables.map((x) => (
                <option key={x.id} value={x.id}>
                  {x.name}
                </option>
              ))}
            </select>
          </div>
          <div>
            <label className="mb-1 block text-sm">Waiter</label>
            <select className="h-10 w-full rounded-md border px-3" value={waiterId} onChange={(e) => setWaiterId(e.target.value)} disabled={readOnly}>
              <option value="">Select waiter</option>
              {waiters.length === 0 ? (
                <option value="" disabled>
                  No waiter employees found
                </option>
              ) : (
                waiters
                  .map((x) => (
                    <option key={x.id} value={x.id}>
                      {x.fullName || `${x.firstName} ${x.lastName}`.trim()}
                    </option>
                  ))
              )}
            </select>
          </div>
          <div>
            <label className="mb-1 block text-sm">Status</label>
            <Input value={order?.statusRaw ?? "Draft"} readOnly />
          </div>
          <div>
            <label className="mb-1 block text-sm">Opened At</label>
            <Input value={order?.openedAt ? new Date(order.openedAt).toLocaleString() : "-"} readOnly />
          </div>
          <div className="md:col-span-2">
            <label className="mb-1 block text-sm">Note</label>
            <Input value={note} onChange={(e) => setNote(e.target.value)} disabled={readOnly} />
          </div>
          <div>
            <label className="mb-1 block text-sm">Total Amount</label>
            <Input value={formatCurrency(order?.totalAmount)} readOnly />
          </div>
        </CardContent>
      </Card>

      {variant === "edit" ? (
        <Card>
          <CardHeader>
            <CardTitle>Order Lines</CardTitle>
          </CardHeader>
          <CardContent className="space-y-4">
            {!readOnly ? (
              <div className="grid gap-2 md:grid-cols-5">
                <select className="h-10 rounded-md border px-3" value={newLineMenuItemId} onChange={(e) => setNewLineMenuItemId(e.target.value)}>
                  <option value="">Menu item</option>
                  {menuItems.filter((x) => x.isActive).map((x) => (
                    <option key={x.id} value={x.id}>
                      {x.name}
                    </option>
                  ))}
                </select>
                <Input type="number" min={1} value={newLineQty} onChange={(e) => setNewLineQty(e.target.value)} />
                <Input placeholder="Note" value={newLineNote} onChange={(e) => setNewLineNote(e.target.value)} />
                <div />
                <Button onClick={() => void addLine()}>Add line</Button>
              </div>
            ) : null}

            <div className="overflow-x-auto">
              <table className="w-full text-sm">
                <thead>
                  <tr className="border-b bg-muted/40">
                    <th className="px-3 py-2 text-left">MenuItem</th>
                    <th className="px-3 py-2 text-left">MenuItem Type</th>
                    <th className="px-3 py-2 text-left">Quantity</th>
                    <th className="px-3 py-2 text-left">UnitPrice</th>
                    <th className="px-3 py-2 text-left">LineTotal</th>
                    <th className="px-3 py-2 text-left">PreparationType</th>
                    <th className="px-3 py-2 text-left">OrderLineStatus</th>
                    <th className="px-3 py-2 text-left">Note</th>
                    <th className="px-3 py-2 text-left">Actions</th>
                  </tr>
                </thead>
                <tbody>
                  {(order?.lines ?? []).map((line) => {
                    const goesToKitchen = line.menuItemType.toLowerCase() === "kitchen" || line.preparationType.toLowerCase() === "kitchen";
                    const effectiveLineTotal = line.lineTotal > 0 ? line.lineTotal : line.quantity * line.unitPrice;
                    return (
                      <tr key={line.id} className="border-b">
                        <td className="px-3 py-2">{line.menuItemName}</td>
                        <td className="px-3 py-2">{line.menuItemType || "-"}</td>
                        <td className="px-3 py-2">{line.quantity}</td>
                        <td className="px-3 py-2">{formatCurrency(line.unitPrice)}</td>
                        <td className="px-3 py-2">{formatCurrency(effectiveLineTotal)}</td>
                        <td className="px-3 py-2">{line.preparationType}</td>
                        <td className="px-3 py-2">
                          <div className="flex gap-2 items-center">
                            <Badge variant="outline">{line.status}</Badge>
                            {goesToKitchen ? <span className="text-xs text-muted-foreground">Kitchen line</span> : null}
                          </div>
                        </td>
                        <td className="px-3 py-2">{line.note ?? "-"}</td>
                        <td className="px-3 py-2">
                          {!readOnly ? (
                            <div className="flex gap-2">
                              <Button
                                size="sm"
                                variant="outline"
                                onClick={() => {
                                  const quantity = Number(window.prompt("Quantity", String(line.quantity)) ?? line.quantity);
                                  const lineNote = window.prompt("Note", line.note ?? "") ?? line.note ?? "";
                                  if (!Number.isFinite(quantity) || quantity <= 0) return;
                                  void updateLine({ ...line, quantity, note: lineNote });
                                }}
                              >
                                Edit
                              </Button>
                              <Button size="sm" variant="destructive" onClick={() => void removeLine(line.id)}>
                                Delete
                              </Button>
                            </div>
                          ) : (
                            "-"
                          )}
                        </td>
                      </tr>
                    );
                  })}
                </tbody>
              </table>
            </div>
          </CardContent>
        </Card>
      ) : null}

      <Dialog
        open={paymentModalOpen && !isPaid}
        onOpenChange={(open) => {
          if (isPaid) {
            setPaymentModalOpen(false);
            return;
          }
          setPaymentModalOpen(open);
        }}
      >
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Order Payment</DialogTitle>
            <DialogDescription>Confirm payment and generate receipt.</DialogDescription>
          </DialogHeader>
          <div className="space-y-3">
            <div>
              <label className="mb-1 block text-sm">Payment Method</label>
              <select
                className="h-10 w-full rounded-md border px-3"
                value={paymentMethod}
                onChange={(e) => setPaymentMethod(e.target.value as PaymentMethod)}
              >
                <option value="Cash">Cash</option>
                <option value="Card">Card</option>
              </select>
            </div>
            <div>
              <label className="mb-1 block text-sm">Total Amount</label>
              <Input value={formatCurrency(order?.totalAmount)} readOnly />
            </div>
            <div>
              <label className="mb-1 block text-sm">Paid Amount</label>
              <Input type="number" min={0} value={paidAmount} onChange={(e) => setPaidAmount(e.target.value)} />
            </div>
            <div>
              <label className="mb-1 block text-sm">Change Amount</label>
              <Input
                value={formatCurrency(Math.max(0, Number(paidAmount || "0") - Number(order?.totalAmount ?? 0)))}
                readOnly
              />
            </div>
            <div className="flex justify-end gap-2">
              <Button variant="outline" onClick={() => setPaymentModalOpen(false)}>
                Cancel
              </Button>
              <Button onClick={() => void confirmPayment()}>Confirm Payment</Button>
            </div>
          </div>
        </DialogContent>
      </Dialog>

      <Dialog open={receiptOpen} onOpenChange={setReceiptOpen}>
        <DialogContent className="max-w-2xl">
          <DialogHeader>
            <DialogTitle>Receipt / Check</DialogTitle>
          </DialogHeader>
          {receipt ? (
            <div className="space-y-3" id="receipt-print-content">
              <div className="text-sm">
                <div className="font-semibold">{receipt.restaurantName}</div>
                <div>Receipt No: {receipt.receiptNumber}</div>
                <div>Order No: {receipt.orderNumber}</div>
                <div>Table: {receipt.tableName}</div>
                <div>Waiter: {receipt.waiterName}</div>
                <div>Date: {receipt.paidAt ? new Date(receipt.paidAt).toLocaleString() : "-"}</div>
              </div>
              <table className="w-full text-sm">
                <thead>
                  <tr className="border-b">
                    <th className="text-left py-1">Item</th>
                    <th className="text-left py-1">Qty</th>
                    <th className="text-left py-1">Price</th>
                    <th className="text-left py-1">Total</th>
                  </tr>
                </thead>
                <tbody>
                  {receipt.lines.map((line, idx) => (
                    <tr key={`${line.menuItemName}-${idx}`} className="border-b">
                      <td className="py-1">{line.menuItemName}</td>
                      <td className="py-1">{line.quantity}</td>
                      <td className="py-1">{formatCurrency(line.unitPrice)}</td>
                      <td className="py-1">{formatCurrency(line.lineTotal)}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
              <div className="text-sm">
                <div>Total: {formatCurrency(receipt.totalAmount)}</div>
                <div>Payment: {receipt.paymentMethod}</div>
                <div>Paid: {formatCurrency(receipt.paidAmount)}</div>
                <div>Change: {formatCurrency(receipt.changeAmount)}</div>
              </div>
              <div className="flex justify-end">
                <Button
                  onClick={() => {
                    const receiptHtml = document.getElementById("receipt-print-content")?.innerHTML;
                    if (!receiptHtml) return;
                    const printWindow = window.open("", "_blank", "width=500,height=700");
                    if (!printWindow) return;
                    printWindow.document.write(`<html><body>${receiptHtml}</body></html>`);
                    printWindow.document.close();
                    printWindow.focus();
                    printWindow.print();
                    printWindow.close();
                  }}
                >
                  Print
                </Button>
              </div>
            </div>
          ) : null}
        </DialogContent>
      </Dialog>
    </div>
  );
}
