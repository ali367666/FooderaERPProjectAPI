"use client";

import { useCallback, useEffect, useMemo, useState } from "react";
import { useParams, useRouter } from "next/navigation";
import { toast } from "sonner";
import { ArrowLeft, ArrowLeftRight, Clock, Gift, Minus, Package, Pencil, Play, Plus, Printer, Receipt, Search, Square, StickyNote, Tag, Trash2, User, UserCog, X } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
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
  setOrderLineHold,
  setOrderHold,
  deleteOrderLine,
  payOrder,
  getOrderReceipt,
  cancelOrder,
  discardEmptyOrder,
  moveOrderTable,
  reassignOrderWaiter,
  setOrderDeliveryDriver,
  setOrderCounterparty,
  printKitchenTicket,
  startTimeBasedLine,
  stopTimeBasedLine,
  startTableRental,
  stopTableRental,
  applyDiscountToOrder,
  removeDiscountFromOrder,
  updateOrder,
  type OrderDto,
  type OrderLineDto,
  type OrderReceiptDto,
  type OrderReceiptLineDto,
  type PaymentMethod,
} from "@/lib/services/order-service";
import { getCounterparties, type Counterparty } from "@/lib/services/counterparty-service";
import { getMenuCategories, type MenuCategory } from "@/lib/services/menu-category-service";
import { getMenuItems, getMenuItemAvailability, UnitOfMeasure, type MenuItem } from "@/lib/services/menu-item-service";
import {
  getRestaurantTables,
  getRestaurantTableById,
  updateRestaurantTable,
  type RestaurantTable,
} from "@/lib/services/restaurant-table-service";
import { getEmployees, type Employee } from "@/lib/services/employee-service";
import { getPrinters, printToPrinter, type Printer as PrinterProfile } from "@/lib/services/printer-service";
import { getPosTerminalContext } from "@/lib/pos-terminal-client";
import { getCurrentEmployeeId } from "@/lib/pos-session";
import { getStoredAuthUser } from "@/lib/auth-client";
import { useHasPermission } from "@/hooks/use-auth-permissions";
import {
  getCompanySettingsBranding,
  type CompanySettingsBranding,
} from "@/lib/services/company-settings-service";

function isWaiterRole(): boolean {
  const roles = getStoredAuthUser()?.roles ?? [];
  return roles.some((r) => r.trim().toLowerCase() === "waiter");
}

function formatEmployeeName(e: Employee): string {
  const n = e.fullName?.trim();
  if (n) return n;
  return `${e.firstName} ${e.lastName}`.trim() || `Employee #${e.id}`;
}

function groupReceiptLines(lines: OrderReceiptLineDto[], group: boolean): OrderReceiptLineDto[] {
  if (!group) return lines;
  const grouped: OrderReceiptLineDto[] = [];
  const indexByKey = new Map<string, number>();
  for (const line of lines) {
    const key = `${line.menuItemName}__${line.unitPrice}`;
    const existingIndex = indexByKey.get(key);
    if (existingIndex === undefined) {
      indexByKey.set(key, grouped.length);
      grouped.push({ ...line });
    } else {
      grouped[existingIndex].quantity += line.quantity;
      grouped[existingIndex].lineTotal += line.lineTotal;
      grouped[existingIndex].vatAmount += line.vatAmount;
    }
  }
  return grouped;
}

function formatDuration(ms: number): string {
  const totalSeconds = Math.max(0, Math.floor(ms / 1000));
  const h = Math.floor(totalSeconds / 3600);
  const m = Math.floor((totalSeconds % 3600) / 60);
  const s = totalSeconds % 60;
  const pad = (n: number) => String(n).padStart(2, "0");
  return h > 0 ? `${h}:${pad(m)}:${pad(s)}` : `${m}:${pad(s)}`;
}

export default function PosOrderPage() {
  const params = useParams<{ id: string }>();
  const router = useRouter();
  const orderId = Number(params.id);

  const [order, setOrder] = useState<OrderDto | null>(null);
  const [categories, setCategories] = useState<MenuCategory[]>([]);
  const [items, setItems] = useState<MenuItem[]>([]);
  const [activeCategoryId, setActiveCategoryId] = useState<number | null>(null);
  const [activeSubCategoryId, setActiveSubCategoryId] = useState<number | null>(null);
  const [productSearch, setProductSearch] = useState("");
  const [weightDialogItem, setWeightDialogItem] = useState<MenuItem | null>(null);
  const [weightDialogLineId, setWeightDialogLineId] = useState<number | null>(null);
  const [weightKgInput, setWeightKgInput] = useState("");
  const [weightBusy, setWeightBusy] = useState(false);
  const [noteDialogOpen, setNoteDialogOpen] = useState(false);
  const [noteInput, setNoteInput] = useState("");
  const [noteBusy, setNoteBusy] = useState(false);
  const [rateEditing, setRateEditing] = useState(false);
  const [rateInput, setRateInput] = useState("");
  const [rateBusy, setRateBusy] = useState(false);
  const [loading, setLoading] = useState(true);
  const [busy, setBusy] = useState(false);
  const [orderHoldBusy, setOrderHoldBusy] = useState(false);
  const [confirmPrintOpen, setConfirmPrintOpen] = useState(false);

  const [payOpen, setPayOpen] = useState(false);
  const [paymentMethod, setPaymentMethod] = useState<PaymentMethod>("Cash");
  const [paidAmountInput, setPaidAmountInput] = useState("");
  const [receipt, setReceipt] = useState<OrderReceiptDto | null>(null);
  const [branding, setBranding] = useState<CompanySettingsBranding | null>(null);
  const [serviceChargeInput, setServiceChargeInput] = useState("");

  const [moveTableOpen, setMoveTableOpen] = useState(false);
  const [availableTables, setAvailableTables] = useState<RestaurantTable[]>([]);
  const [reassignOpen, setReassignOpen] = useState(false);
  const [driverDialogOpen, setDriverDialogOpen] = useState(false);
  const [employeesList, setEmployeesList] = useState<Employee[]>([]);
  const [currentEmployeeId, setCurrentEmployeeId] = useState<number | null>(null);
  const [priceEditingLineId, setPriceEditingLineId] = useState<number | null>(null);
  const [discountEditingLineId, setDiscountEditingLineId] = useState<number | null>(null);
  const [discountAmountInput, setDiscountAmountInput] = useState("");
  const [priceInput, setPriceInput] = useState("");
  const [holdLineId, setHoldLineId] = useState<number | null>(null);
  const [holdMinutesInput, setHoldMinutesInput] = useState("");
  const [now, setNow] = useState(() => Date.now());
  const [counterpartyDialogOpen, setCounterpartyDialogOpen] = useState(false);
  const [counterparties, setCounterparties] = useState<Counterparty[]>([]);
  const [counterpartySearch, setCounterpartySearch] = useState("");
  const [counterpartyBusy, setCounterpartyBusy] = useState(false);

  const [discountDialogOpen, setDiscountDialogOpen] = useState(false);
  const [discountCodeInput, setDiscountCodeInput] = useState("");
  const [discountBusy, setDiscountBusy] = useState(false);

  const isStoreMode = branding?.moduleDataSecimi === true;
  const canEditProduct = useHasPermission("Pos.EditProductInSale");
  const canDeleteProduct = useHasPermission("Pos.DeleteProductInSale");
  const canDeleteOrder = useHasPermission("Pos.DeleteOrder");
  const canMoveTable = useHasPermission("Pos.MoveTable");
  const canRedirectUser = useHasPermission("Pos.RedirectUser");
  const canChangePrice = useHasPermission("Pos.ChangePrice");
  const canApplyDiscount = useHasPermission("Discount.Apply");
  const canTableServiceCharge = useHasPermission("Pos.TableServiceCharge");
  const canPrint = useHasPermission("Printer.Print");
  const canPay = useHasPermission("Orders.Pay");
  const canPrintBill = useHasPermission("Pos.PrintReceipt");

  const [printers, setPrinters] = useState<PrinterProfile[]>([]);
  const [printingId, setPrintingId] = useState<number | null>(null);
  const [outOfStockIds, setOutOfStockIds] = useState<Set<number>>(new Set());

  const loadAvailability = useCallback(async (restaurantId: number) => {
    try {
      setOutOfStockIds(await getMenuItemAvailability(restaurantId));
    } catch {
      setOutOfStockIds(new Set());
    }
  }, []);

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
      setActiveCategoryId(
        (prev) => prev ?? cats.find((c) => c.isActive && c.parentCategoryId == null)?.id ?? null,
      );
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
    void getCurrentEmployeeId().then(setCurrentEmployeeId);
  }, []);

  // Single-waiter mode: only the owning waiter may view/edit this order — no manager override,
  // even for a user who otherwise holds the "view all tables" permission.
  useEffect(() => {
    if (!order || branding?.singleWaiterMode !== true) return;
    if (currentEmployeeId == null) return;
    if (order.waiterId !== currentEmployeeId) {
      toast.error("Bu masa başqa ofisiantə aiddir, baxa bilməzsiniz.");
      router.replace("/pos");
    }
  }, [order, branding, currentEmployeeId, router]);

  useEffect(() => {
    const id = window.setInterval(() => setNow(Date.now()), 1000);
    return () => window.clearInterval(id);
  }, []);

  const openHoldDialog = (line: OrderLineDto) => {
    const remainingMs = line.holdUntilUtc ? new Date(line.holdUntilUtc).getTime() - now : 0;
    const remainingMinutes = remainingMs > 0 ? Math.ceil(remainingMs / 60000) : 0;
    setHoldMinutesInput(remainingMinutes > 0 ? String(remainingMinutes) : "");
    setHoldLineId(line.id);
  };

  const submitHold = async (lineId: number, minutes: number | null) => {
    setHoldLineId(null);
    setBusy(true);
    try {
      const updated = await setOrderLineHold(lineId, minutes);
      setOrder(updated);
    } catch (err) {
      toast.error(err instanceof Error ? err.message : "Gözlətmə təyin edilmədi");
    } finally {
      setBusy(false);
    }
  };

  const handleConfirmHold = async () => {
    if (holdLineId == null) return;
    const parsed = Number(holdMinutesInput);
    const minutes = Number.isFinite(parsed) && parsed > 0 ? Math.floor(parsed) : null;
    await submitHold(holdLineId, minutes);
  };

  const handleToggleOrderHold = async () => {
    if (!order) return;
    setOrderHoldBusy(true);
    try {
      const updated = await setOrderHold(order.id, !order.holdUntilUtc);
      setOrder(updated);
    } catch (err) {
      toast.error(err instanceof Error ? err.message : "Sifariş gözlətməsi dəyişdirilmədi");
    } finally {
      setOrderHoldBusy(false);
    }
  };

  useEffect(() => {
    const terminal = getPosTerminalContext();
    if (!terminal) return;
    getCompanySettingsBranding(terminal.companyId)
      .then(setBranding)
      .catch(() => setBranding(null));
    if (terminal.restaurantId) {
      getPrinters(terminal.restaurantId)
        .then((p) => setPrinters(p.filter((x) => x.isActive)))
        .catch(() => setPrinters([]));
      void loadAvailability(terminal.restaurantId);
    }
  }, [loadAvailability]);

  const orderedLines = useMemo(() => {
    const lines = order?.lines ?? [];
    const childrenByParent = new Map<number, OrderLineDto[]>();
    for (const line of lines) {
      if (line.parentLineId == null) continue;
      const siblings = childrenByParent.get(line.parentLineId) ?? [];
      siblings.push(line);
      childrenByParent.set(line.parentLineId, siblings);
    }
    const result: OrderLineDto[] = [];
    for (const line of lines) {
      if (line.parentLineId != null) continue;
      result.push(line);
      const children = childrenByParent.get(line.id);
      if (children) result.push(...children);
    }
    return result;
  }, [order]);

  const printerGroups = useMemo(() => {
    const itemById = new Map(items.map((i) => [i.id, i]));
    const groups = new Map<number, { printer: PrinterProfile; lines: OrderLineDto[] }>();
    for (const line of order?.lines ?? []) {
      if (line.status === "Cancelled" || line.kitchenPrintedAt != null) continue;
      const item = itemById.get(line.menuItemId);
      const printerId = item?.isSet ? item?.setPrinterId ?? item?.printerId : item?.printerId;
      if (!printerId) continue;
      const printer = printers.find((p) => p.id === printerId);
      if (!printer) continue;
      const existing = groups.get(printerId);
      if (existing) {
        existing.lines.push(line);
      } else {
        groups.set(printerId, { printer, lines: [line] });
      }
    }
    return Array.from(groups.values());
  }, [order, items, printers]);

  const handlePrintGroup = async (printer: PrinterProfile) => {
    if (!order) return;
    setPrintingId(printer.id);
    try {
      const count = await printKitchenTicket(order.id, printer.id);
      toast.success(count > 0 ? `${printer.name}-ə ${count} məhsul göndərildi` : "Yeni məhsul yoxdur");
      await load();
    } catch (err) {
      toast.error(err instanceof Error ? err.message : "Printerə qoşulmaq mümkün olmadı");
    } finally {
      setPrintingId(null);
    }
  };

  const groupedReceiptLines = useMemo<OrderReceiptLineDto[]>(
    () => groupReceiptLines(receipt?.lines ?? [], branding?.printGroupQuantities !== false),
    [receipt, branding?.printGroupQuantities],
  );

  const buildReceiptContent = (r: OrderReceiptDto | null = receipt) => {
    if (!r) return "";
    const lines: string[] = [];
    if (branding?.receiptShowBusinessName !== false) {
      lines.push((order?.restaurantName ?? r.restaurantName ?? "").toUpperCase());
      lines.push("");
    }
    if (branding?.receiptShowOrderNumber !== false) lines.push(`Sifariş: ${r.orderNumber}`);
    if (branding?.receiptShowTableName !== false) lines.push(`Masa: ${r.tableName}`);
    if (branding?.receiptShowWaiterName !== false) lines.push(`Ofisiant: ${r.waiterName}`);
    if (branding?.receiptShowTime !== false) {
      lines.push(`Vaxt: ${new Date(r.paidAt ?? r.openedAt).toLocaleString("az-AZ")}`);
    }
    if (branding?.receiptShowPaymentMethod !== false) lines.push(`Ödəniş: ${r.paymentMethod}`);
    lines.push("-".repeat(32));
    for (const line of groupReceiptLines(r.lines, branding?.printGroupQuantities !== false)) {
      lines.push(`${line.quantity} x ${line.menuItemName}`.padEnd(24) + `${line.lineTotal.toFixed(2)} ₼`);
    }
    lines.push("-".repeat(32));
    lines.push(`Cəm: ${r.totalAmount.toFixed(2)} ₼`);
    if (r.vatAmount > 0) lines.push(`ƏDV daxildir: ${r.vatAmount.toFixed(2)} ₼`);
    if (r.paymentMethod === "Cash") {
      lines.push(`Alınan: ${r.paidAmount.toFixed(2)} ₼`);
      lines.push(`Qalıq: ${r.changeAmount.toFixed(2)} ₼`);
    }
    if (branding?.slogan) lines.push(branding.slogan);
    if (branding?.contactPhoneNumber) lines.push(branding.contactPhoneNumber);
    return lines.join("\n");
  };

  const handlePrintReceiptToPrinter = async (printer: PrinterProfile, r?: OrderReceiptDto) => {
    setPrintingId(printer.id);
    try {
      await printToPrinter(printer.id, buildReceiptContent(r ?? receipt));
      toast.success(`${printer.name}-ə göndərildi`);
    } catch (err) {
      toast.error(err instanceof Error ? err.message : "Printerə qoşulmaq mümkün olmadı");
    } finally {
      setPrintingId(null);
    }
  };

  const sortedPrinters = useMemo(
    () => [...printers].sort((a, b) => Number(b.isPrimary) - Number(a.isPrimary)),
    [printers],
  );

  const topLevelCategories = useMemo(
    () => categories.filter((c) => c.parentCategoryId == null),
    [categories],
  );

  const subCategoriesWithItems = useMemo(() => {
    if (activeCategoryId == null) return [];
    return categories
      .filter((c) => c.parentCategoryId === activeCategoryId)
      .filter((c) => items.some((i) => i.menuCategoryId === c.id));
  }, [categories, items, activeCategoryId]);

  const ownItems = useMemo(
    () => (activeCategoryId == null ? [] : items.filter((i) => i.menuCategoryId === activeCategoryId)),
    [items, activeCategoryId],
  );

  const itemsInCategory = useMemo(() => {
    if (activeSubCategoryId != null) {
      return items.filter((i) => i.menuCategoryId === activeSubCategoryId);
    }
    return ownItems;
  }, [items, ownItems, activeSubCategoryId]);

  const productSearchTrimmed = productSearch.trim();
  const searchResults = useMemo(() => {
    if (!productSearchTrimmed) return [];
    const term = productSearchTrimmed.toLowerCase();
    return items.filter((i) => {
      if (i.hideFromPosSearch) return false;
      const nameMatch = i.name.toLowerCase().includes(term);
      const codeMatch =
        !i.hideBarcode &&
        ((i.barcode != null && i.barcode === productSearchTrimmed) ||
          (i.weightCode != null && i.weightCode === productSearchTrimmed));
      return nameMatch || codeMatch;
    });
  }, [items, productSearchTrimmed]);

  const handleBarcodeEnter = (e: React.KeyboardEvent<HTMLInputElement>) => {
    if (e.key !== "Enter") return;
    const exact = items.find(
      (i) => !i.hideBarcode && (i.barcode === productSearchTrimmed || i.weightCode === productSearchTrimmed),
    );
    if (exact) {
      void handleAddItem(exact.id);
      setProductSearch("");
    }
  };

  const handleAddItem = async (menuItemId: number) => {
    if (!order || busy || outOfStockIds.has(menuItemId)) return;
    setBusy(true);
    try {
      const updated = await addOrderLine({ orderId: order.id, menuItemId, quantity: 1 });
      setOrder(updated);
      void loadAvailability(order.restaurantId);
    } catch (err) {
      toast.error(err instanceof Error ? err.message : "Məhsul əlavə olunmadı");
    } finally {
      setBusy(false);
    }
  };

  const isWeightBasedItem = (item: MenuItem) =>
    item.unitId === UnitOfMeasure.Kg || item.unitId === UnitOfMeasure.Gram;

  const handleItemClick = (item: MenuItem) => {
    if (isWeightBasedItem(item)) {
      setWeightDialogItem(item);
      setWeightDialogLineId(null);
      setWeightKgInput("");
      return;
    }
    void handleAddItem(item.id);
  };

  const openReweighDialog = (line: OrderLineDto) => {
    const menuItem = items.find((i) => i.id === line.menuItemId);
    if (!menuItem) return;
    setWeightDialogItem(menuItem);
    setWeightDialogLineId(line.id);
    setWeightKgInput((line.quantity / 1000).toFixed(3));
  };

  const handleConfirmWeight = async () => {
    if (!order || !weightDialogItem) return;
    const kg = Number(weightKgInput);
    if (!Number.isFinite(kg) || kg <= 0) {
      toast.error("Çəki düzgün deyil");
      return;
    }
    const grams = Math.round(kg * 1000);
    setWeightBusy(true);
    try {
      const updated = weightDialogLineId != null
        ? await updateOrderLine({ id: weightDialogLineId, quantity: grams })
        : await addOrderLine({ orderId: order.id, menuItemId: weightDialogItem.id, quantity: grams });
      setOrder(updated);
      void loadAvailability(order.restaurantId);
      setWeightDialogItem(null);
      setWeightDialogLineId(null);
      setWeightKgInput("");
    } catch (err) {
      toast.error(err instanceof Error ? err.message : "Çəki tətbiq edilmədi");
    } finally {
      setWeightBusy(false);
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

  const handleStartTimer = async (lineId: number) => {
    if (!order || busy) return;
    setBusy(true);
    try {
      const updated = await startTimeBasedLine(lineId);
      setOrder(updated);
    } catch (err) {
      toast.error(err instanceof Error ? err.message : "Taymer başladılmadı");
    } finally {
      setBusy(false);
    }
  };

  const handleStopTimer = async (lineId: number) => {
    if (!order || busy) return;
    setBusy(true);
    try {
      const updated = await stopTimeBasedLine(lineId);
      setOrder(updated);
    } catch (err) {
      toast.error(err instanceof Error ? err.message : "Taymer dayandırılmadı");
    } finally {
      setBusy(false);
    }
  };

  const handleStartRental = async () => {
    if (!order || busy) return;
    setBusy(true);
    try {
      const updated = await startTableRental(order.id);
      setOrder(updated);
    } catch (err) {
      toast.error(err instanceof Error ? err.message : "İcarə başladılmadı");
    } finally {
      setBusy(false);
    }
  };

  const handleStopRental = async () => {
    if (!order || busy) return;
    setBusy(true);
    try {
      const updated = await stopTableRental(order.id);
      setOrder(updated);
    } catch (err) {
      toast.error(err instanceof Error ? err.message : "İcarə dayandırılmadı");
    } finally {
      setBusy(false);
    }
  };

  const openPayDialog = () => {
    if (!order) return;
    const hasRental = order.tableHourlyRate != null;
    if (order.lines.length === 0 && !hasRental) return;
    if (hasRental && order.tableRentalStartedAt != null && order.tableRentalStoppedAt == null) {
      toast.error("Əvvəlcə icarə taymerini dayandırın");
      return;
    }
    setPaymentMethod("Cash");
    setServiceChargeInput("");
    setPaidAmountInput((order.totalAmount + (order.tableRentalAmount ?? 0)).toFixed(2));
    setPayOpen(true);
  };

  const maybeAutoPrint = (isFromPayment: boolean) => {
    const shouldAutoPrint =
      (isFromPayment && branding?.printAutoOnPayment === true) || branding?.printShowPreview === false;
    if (!shouldAutoPrint) return;
    if (branding?.printAskBeforeAutoPrint === true) {
      setConfirmPrintOpen(true);
      return;
    }
    setTimeout(() => window.print(), 150);
  };

  const handleConfirmAutoPrint = () => {
    setConfirmPrintOpen(false);
    setTimeout(() => window.print(), 150);
  };

  const handleBack = async () => {
    if (order && order.lines.length === 0 && order.status === "draft") {
      try {
        await discardEmptyOrder(order.id);
      } catch (err) {
        toast.error(err instanceof Error ? err.message : "Boş sifariş ləğv edilmədi");
      }
    }
    router.push("/pos");
  };

  const handlePrintBill = async () => {
    if (!order || order.lines.length === 0) return;
    setBusy(true);
    try {
      const r = await getOrderReceipt(order.id);
      setReceipt(r);
      maybeAutoPrint(false);
    } catch (err) {
      toast.error(err instanceof Error ? err.message : "Qəbz alına bilmədi");
    } finally {
      setBusy(false);
    }
  };

  const serviceCharge = canTableServiceCharge ? Number(serviceChargeInput) || 0 : 0;
  const paidAmount = Number(paidAmountInput) || 0;
  const totalWithService = (order?.totalAmount ?? 0) + (order?.tableRentalAmount ?? 0) + serviceCharge;
  const changeAmount = paymentMethod === "Cash" ? Math.max(0, paidAmount - totalWithService) : 0;

  const handleConfirmPayment = async () => {
    if (!order) return;
    if (paymentMethod === "Cash" && paidAmount < totalWithService) {
      toast.error("Ödənilən məbləğ cəmdən az ola bilməz");
      return;
    }
    setBusy(true);
    try {
      await payOrder(order.id, {
        paymentMethod,
        paidAmount: paymentMethod === "Cash" ? paidAmount : totalWithService,
        serviceChargeAmount: canTableServiceCharge && serviceCharge > 0 ? serviceCharge : null,
      });
      const r = await getOrderReceipt(order.id);
      setReceipt(r);
      setPayOpen(false);
      maybeAutoPrint(true);
      if (branding?.printAutoOnPayment === true) {
        const primaryPrinter = printers.find((p) => p.isPrimary);
        if (primaryPrinter) void handlePrintReceiptToPrinter(primaryPrinter, r);
      }
      if (branding?.printKitchenOnPayment === true && printerGroups.length > 0) {
        for (const group of printerGroups) {
          try {
            await printKitchenTicket(order.id, group.printer.id);
          } catch (err) {
            toast.error(
              err instanceof Error
                ? `${group.printer.name}: ${err.message}`
                : `${group.printer.name}-ə mətbəx çapı göndərilmədi`,
            );
          }
        }
        await load();
      }
    } catch (err) {
      toast.error(err instanceof Error ? err.message : "Ödəniş uğursuz oldu");
    } finally {
      setBusy(false);
    }
  };

  const handleDeleteOrder = async () => {
    if (!order || busy) return;
    if (!window.confirm("Sifarişi ləğv etmək istədiyinizə əminsiniz?")) return;
    setBusy(true);
    try {
      await cancelOrder(order.id);
      toast.success("Sifariş ləğv edildi");
      router.replace("/pos");
    } catch (err) {
      toast.error(err instanceof Error ? err.message : "Sifariş ləğv edilmədi");
      setBusy(false);
    }
  };

  const openMoveTableDialog = async () => {
    if (!order) return;
    setMoveTableOpen(true);
    try {
      const tables = await getRestaurantTables();
      setAvailableTables(
        tables.filter((t) => t.restaurantId === order.restaurantId && t.isActive && !t.isOccupied),
      );
    } catch {
      setAvailableTables([]);
    }
  };

  const handleMoveTable = async (newTableId: number) => {
    if (!order || busy) return;
    setBusy(true);
    try {
      const updated = await moveOrderTable(order.id, newTableId);
      setOrder(updated);
      toast.success("Masa dəyişdirildi");
      setMoveTableOpen(false);
    } catch (err) {
      toast.error(err instanceof Error ? err.message : "Masa dəyişdirilmədi");
    } finally {
      setBusy(false);
    }
  };

  const openReassignDialog = async () => {
    setReassignOpen(true);
    try {
      const emp = await getEmployees();
      setEmployeesList(emp);
    } catch {
      setEmployeesList([]);
    }
  };

  const handleReassignWaiter = async (newEmployeeId: number) => {
    if (!order || busy) return;
    setBusy(true);
    try {
      const updated = await reassignOrderWaiter(order.id, newEmployeeId);
      setOrder(updated);
      toast.success("Ofisiant dəyişdirildi");
      setReassignOpen(false);
    } catch (err) {
      toast.error(err instanceof Error ? err.message : "Ofisiant dəyişdirilmədi");
    } finally {
      setBusy(false);
    }
  };

  const openDriverDialog = async () => {
    setDriverDialogOpen(true);
    try {
      const emp = await getEmployees();
      setEmployeesList(emp);
    } catch {
      setEmployeesList([]);
    }
  };

  const handleSetDriver = async (driverEmployeeId: number | null) => {
    if (!order || busy) return;
    setBusy(true);
    try {
      const updated = await setOrderDeliveryDriver(order.id, driverEmployeeId);
      setOrder(updated);
      toast.success(driverEmployeeId ? "Kuryer təyin edildi" : "Kuryer götürüldü");
      setDriverDialogOpen(false);
    } catch (err) {
      toast.error(err instanceof Error ? err.message : "Kuryer dəyişdirilmədi");
    } finally {
      setBusy(false);
    }
  };

  const openCounterpartyDialog = async () => {
    setCounterpartySearch("");
    setCounterpartyDialogOpen(true);
    if (counterparties.length > 0) return;
    try {
      const list = await getCounterparties();
      setCounterparties(list.filter((c) => c.isActive));
    } catch {
      setCounterparties([]);
    }
  };

  const handleSelectCounterparty = async (counterpartyId: number | null) => {
    if (!order) return;
    setCounterpartyBusy(true);
    try {
      const updated = await setOrderCounterparty(order.id, counterpartyId);
      setOrder(updated);
      setCounterpartyDialogOpen(false);
    } catch (err) {
      toast.error(err instanceof Error ? err.message : "Müştəri təyin edilmədi");
    } finally {
      setCounterpartyBusy(false);
    }
  };

  const handleApplyDiscount = async () => {
    if (!order || !discountCodeInput.trim()) return;
    setDiscountBusy(true);
    try {
      const updated = await applyDiscountToOrder(order.id, discountCodeInput.trim());
      setOrder(updated);
      setDiscountDialogOpen(false);
      setDiscountCodeInput("");
    } catch (err) {
      toast.error(err instanceof Error ? err.message : "Endirim tətbiq edilmədi");
    } finally {
      setDiscountBusy(false);
    }
  };

  const handleRemoveDiscount = async () => {
    if (!order) return;
    setDiscountBusy(true);
    try {
      const updated = await removeDiscountFromOrder(order.id);
      setOrder(updated);
    } catch (err) {
      toast.error(err instanceof Error ? err.message : "Endirim silinmədi");
    } finally {
      setDiscountBusy(false);
    }
  };

  const openNoteDialog = () => {
    if (!order) return;
    setNoteInput(order.note ?? "");
    setNoteDialogOpen(true);
  };

  const handleSaveNote = async () => {
    if (!order) return;
    setNoteBusy(true);
    try {
      const updated = await updateOrder({
        id: order.id,
        restaurantId: order.restaurantId,
        tableId: order.tableId,
        waiterId: order.waiterId,
        note: noteInput.trim() || null,
      });
      setOrder(updated);
      setNoteDialogOpen(false);
    } catch (err) {
      toast.error(err instanceof Error ? err.message : "Qeyd saxlanılmadı");
    } finally {
      setNoteBusy(false);
    }
  };

  const startRateEdit = () => {
    if (!order?.tableHourlyRate) return;
    setRateInput(order.tableHourlyRate.toFixed(2));
    setRateEditing(true);
  };

  const handleSaveRate = async () => {
    if (!order) return;
    const rate = Number(rateInput);
    if (!Number.isFinite(rate) || rate <= 0) {
      toast.error("Saatlıq qiymət düzgün deyil");
      return;
    }
    setRateBusy(true);
    try {
      const table = await getRestaurantTableById(order.tableId);
      await updateRestaurantTable(order.tableId, {
        restaurantId: table.restaurantId,
        name: table.name,
        capacity: table.capacity,
        isActive: table.isActive,
        hourlyRate: rate,
        note: table.note,
        type: table.type,
      });
      await load();
      setRateEditing(false);
    } catch (err) {
      toast.error(err instanceof Error ? err.message : "Saatlıq qiymət dəyişdirilmədi");
    } finally {
      setRateBusy(false);
    }
  };

  const filteredCounterparties = useMemo(() => {
    const term = counterpartySearch.trim().toLowerCase();
    if (!term) return counterparties;
    return counterparties.filter((c) => c.name.toLowerCase().includes(term));
  }, [counterparties, counterpartySearch]);

  const startPriceEdit = (lineId: number, currentPrice: number) => {
    setPriceEditingLineId(lineId);
    setPriceInput(currentPrice.toFixed(2));
  };

  const handleSavePrice = async (lineId: number, quantity: number) => {
    const newPrice = Number(priceInput);
    if (!Number.isFinite(newPrice) || newPrice < 0) {
      toast.error("Qiymət düzgün deyil");
      return;
    }
    setBusy(true);
    try {
      const updated = await updateOrderLine({ id: lineId, quantity, unitPrice: newPrice });
      setOrder(updated);
      setPriceEditingLineId(null);
    } catch (err) {
      toast.error(err instanceof Error ? err.message : "Qiymət dəyişdirilmədi");
    } finally {
      setBusy(false);
    }
  };

  const applyQuickPrice = async (lineId: number, quantity: number, price: number) => {
    setBusy(true);
    try {
      const updated = await updateOrderLine({ id: lineId, quantity, unitPrice: price });
      setOrder(updated);
    } catch (err) {
      toast.error(err instanceof Error ? err.message : "Qiymət tətbiq edilmədi");
    } finally {
      setBusy(false);
    }
  };

  const startDiscountEdit = (lineId: number, currentDiscount: number) => {
    setDiscountEditingLineId(lineId);
    setDiscountAmountInput(currentDiscount > 0 ? currentDiscount.toFixed(2) : "");
  };

  const handleSaveDiscount = async (lineId: number, quantity: number) => {
    const amount = Number(discountAmountInput) || 0;
    if (amount < 0) {
      toast.error("Endirim məbləği düzgün deyil");
      return;
    }
    setBusy(true);
    try {
      const updated = await updateOrderLine({ id: lineId, quantity, discountAmount: amount });
      setOrder(updated);
      setDiscountEditingLineId(null);
    } catch (err) {
      toast.error(err instanceof Error ? err.message : "Endirim tətbiq edilmədi");
    } finally {
      setBusy(false);
    }
  };

  const handleToggleGift = async (lineId: number, quantity: number, isGift: boolean) => {
    setBusy(true);
    try {
      const updated = await updateOrderLine({ id: lineId, quantity, isGift });
      setOrder(updated);
    } catch (err) {
      toast.error(err instanceof Error ? err.message : "Hədiyyə statusu dəyişdirilmədi");
    } finally {
      setBusy(false);
    }
  };

  if (loading || !order) {
    return <div className="flex h-full items-center justify-center text-muted-foreground">Yüklənir...</div>;
  }

  const isPaid = order.status === "paid";
  const canCancelOrderStatus = order.status === "draft" || order.status === "open";
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

  // Simple-receipt mode uses its own independently-configured field toggles instead of the
  // normal receipt's — each company decides separately what appears in each mode.
  const receiptSimpleMode = branding?.receiptSimpleMode === true;
  const showReceiptOrderNumber = receiptSimpleMode
    ? branding?.receiptSimpleShowOrderNumber === true
    : branding?.receiptShowOrderNumber !== false;
  const showReceiptWaiterName = receiptSimpleMode
    ? branding?.receiptSimpleShowWaiterName === true
    : branding?.receiptShowWaiterName !== false;
  const showReceiptTime = receiptSimpleMode
    ? branding?.receiptSimpleShowTime === true
    : branding?.receiptShowTime !== false;
  const showReceiptPaymentMethod = receiptSimpleMode
    ? branding?.receiptSimpleShowPaymentMethod === true
    : branding?.receiptShowPaymentMethod !== false;
  const showReceiptVat = receiptSimpleMode ? branding?.receiptSimpleShowVat === true : true;
  const showReceiptFooter = receiptSimpleMode ? branding?.receiptSimpleShowFooter === true : true;

  const isRentalRunning =
    order.tableHourlyRate != null && order.tableRentalStartedAt != null && order.tableRentalStoppedAt == null;
  const rentalElapsedMs = isRentalRunning ? now - new Date(order.tableRentalStartedAt!).getTime() : 0;
  const rentalLiveTotal = isRentalRunning
    ? (order.tableHourlyRate ?? 0) * (rentalElapsedMs / 3_600_000)
    : order.tableRentalAmount ?? 0;

  return (
    <div className="flex h-full flex-col md:flex-row">
      {/* Menu */}
      <div className="flex flex-1 flex-col overflow-hidden">
        <div className="flex items-center gap-2 border-b bg-background px-3 py-2">
          <Button variant="ghost" size="icon-sm" onClick={() => void handleBack()}>
            <ArrowLeft className="h-4 w-4" />
          </Button>
          <span className="font-semibold">{order.tableName}</span>
          {order.counterpartyName && (
            <span className="flex items-center gap-1 rounded-full bg-muted px-2 py-0.5 text-xs text-muted-foreground">
              <User className="h-3 w-3" />
              {order.counterpartyName}
              {order.counterpartyDebtAmount != null && order.counterpartyDebtAmount > 0 && (
                <span className="text-destructive">— borc: {order.counterpartyDebtAmount.toFixed(2)} ₼</span>
              )}
            </span>
          )}
          {order.discountCode && (
            <span className="flex items-center gap-1 rounded-full bg-blue-100 px-2 py-0.5 text-xs text-blue-800">
              <Tag className="h-3 w-3" />
              {order.discountCode} (-{order.discountAmount.toFixed(2)} ₼)
              {!isPaid && (
                <button
                  type="button"
                  onClick={() => void handleRemoveDiscount()}
                  disabled={discountBusy}
                  className="ml-0.5 hover:text-blue-950"
                  title="Endirimi sil"
                >
                  <X className="h-3 w-3" />
                </button>
              )}
            </span>
          )}
          {order.note && (
            <span
              className="flex max-w-[160px] items-center gap-1 truncate rounded-full bg-amber-100 px-2 py-0.5 text-xs text-amber-800"
              title={order.note}
            >
              <StickyNote className="h-3 w-3 shrink-0" />
              {order.note}
            </span>
          )}
          <div className="ml-auto flex items-center gap-1">
            {!isPaid && (
              <Button variant="outline" size="sm" onClick={() => void openCounterpartyDialog()} disabled={busy}>
                <User className="mr-1 h-3.5 w-3.5" />
                Müştəri seç
              </Button>
            )}
            {!isPaid && canApplyDiscount && !order.discountCode && (
              <Button
                variant="outline"
                size="sm"
                onClick={() => {
                  setDiscountCodeInput("");
                  setDiscountDialogOpen(true);
                }}
                disabled={busy}
              >
                <Tag className="mr-1 h-3.5 w-3.5" />
                Endirim tətbiq et
              </Button>
            )}
            <Button variant="outline" size="sm" onClick={openNoteDialog} disabled={busy}>
              <StickyNote className="mr-1 h-3.5 w-3.5" />
              Qeyd
            </Button>
            {!isPaid && !isStoreMode && canMoveTable && (
              <Button variant="outline" size="sm" onClick={() => void openMoveTableDialog()} disabled={busy}>
                <ArrowLeftRight className="mr-1 h-3.5 w-3.5" />
                Masanı dəyiş
              </Button>
            )}
            {!isPaid && !isStoreMode && canRedirectUser && (
              <Button variant="outline" size="sm" onClick={() => void openReassignDialog()} disabled={busy}>
                <UserCog className="mr-1 h-3.5 w-3.5" />
                Ofisiantı dəyiş
              </Button>
            )}
            {!isPaid && canEditProduct && (
              <Button
                variant="outline"
                size="sm"
                className={cn(order.holdUntilUtc && "border-amber-300 bg-amber-50 text-amber-700 hover:bg-amber-100")}
                onClick={() => void handleToggleOrderHold()}
                disabled={orderHoldBusy}
              >
                <Clock className="mr-1 h-3.5 w-3.5" />
                {order.holdUntilUtc ? "Gözləməni ləğv et" : "Bütöv sifarişi gözlət"}
              </Button>
            )}
            {!isPaid && order.isDelivery && canRedirectUser && (
              <Button variant="outline" size="sm" onClick={() => void openDriverDialog()} disabled={busy}>
                <UserCog className="mr-1 h-3.5 w-3.5" />
                {order.deliveryDriverName ? `Kuryer: ${order.deliveryDriverName}` : "Kuryer təyin et"}
              </Button>
            )}
            {canCancelOrderStatus && canDeleteOrder && (
              <Button variant="outline" size="sm" className="text-destructive" onClick={() => void handleDeleteOrder()} disabled={busy}>
                <X className="mr-1 h-3.5 w-3.5" />
                Sifarişi ləğv et
              </Button>
            )}
          </div>
        </div>
        {order.holdUntilUtc && (
          <div className="flex items-center gap-2 border-b bg-amber-50 px-3 py-2 text-sm font-medium text-amber-800">
            <Clock className="h-4 w-4" />
            Sifariş gözlədədir — mətbəxə göndərilmir, "Gözləməni ləğv et" ilə buraxın.
          </div>
        )}
        {order.isDelivery && (
          <div className="flex flex-wrap items-center gap-x-4 gap-y-1 border-b bg-sky-50 px-3 py-2 text-sm text-sky-900">
            <span className="font-medium">Çatdırılma sifarişi</span>
            {order.deliveryPhone && <span>Tel: {order.deliveryPhone}</span>}
            {order.deliveryAddress && <span>Ünvan: {order.deliveryAddress}</span>}
            <span>{order.deliveryDriverName ? `Kuryer: ${order.deliveryDriverName}` : "Kuryer təyin edilməyib"}</span>
          </div>
        )}
        {order.tableHourlyRate != null && (
          <div
            className={cn(
              "flex flex-wrap items-center gap-3 border-b px-3 py-2",
              isRentalRunning ? "bg-emerald-50" : "bg-muted/40",
            )}
          >
            {rateEditing ? (
              <div className="flex items-center gap-1">
                <span className="text-sm font-medium">Saatlıq icarə —</span>
                <Input
                  type="number"
                  step="0.01"
                  min={0}
                  autoFocus
                  value={rateInput}
                  onChange={(e) => setRateInput(e.target.value)}
                  className="h-7 w-20 text-sm"
                />
                <span className="text-sm text-muted-foreground">₼/saat</span>
                <Button size="sm" className="h-7 px-2" disabled={rateBusy} onClick={() => void handleSaveRate()}>
                  OK
                </Button>
              </div>
            ) : (
              <span className="flex items-center gap-1 text-sm font-medium">
                Saatlıq icarə — {order.tableHourlyRate.toFixed(2)} ₼/saat
                {canChangePrice && !isRentalRunning && !isPaid && (
                  <button type="button" onClick={startRateEdit} className="text-muted-foreground hover:text-primary">
                    <Pencil className="h-3 w-3" />
                  </button>
                )}
              </span>
            )}
            {isRentalRunning ? (
              <>
                <span className="flex items-center gap-1 text-sm font-semibold text-emerald-700">
                  <Clock className="h-3.5 w-3.5" />
                  {formatDuration(rentalElapsedMs)} — {rentalLiveTotal.toFixed(2)} ₼
                </span>
                {!isPaid && (
                  <Button
                    size="sm"
                    variant="outline"
                    className="ml-auto gap-1 text-emerald-700"
                    disabled={busy}
                    onClick={() => void handleStopRental()}
                  >
                    <Square className="h-3.5 w-3.5" />
                    Dayandır
                  </Button>
                )}
              </>
            ) : order.tableRentalStoppedAt != null ? (
              <span className="ml-auto text-sm text-muted-foreground">
                Dayandırılıb — {(order.tableRentalAmount ?? 0).toFixed(2)} ₼
              </span>
            ) : (
              !isPaid && (
                <Button
                  size="sm"
                  variant="outline"
                  className="ml-auto gap-1"
                  disabled={busy}
                  onClick={() => void handleStartRental()}
                >
                  <Play className="h-3.5 w-3.5" />
                  Başlat
                </Button>
              )
            )}
          </div>
        )}
        <div className="flex items-center gap-2 border-b bg-background px-3 py-2">
          <Search className="h-4 w-4 shrink-0 text-muted-foreground" />
          <Input
            value={productSearch}
            onChange={(e) => setProductSearch(e.target.value)}
            onKeyDown={handleBarcodeEnter}
            placeholder="Məhsul axtar və ya barkod skan et..."
            className="h-8"
          />
          {productSearch && (
            <button
              type="button"
              onClick={() => setProductSearch("")}
              className="shrink-0 text-muted-foreground hover:text-foreground"
            >
              <X className="h-4 w-4" />
            </button>
          )}
        </div>
        <div className={cn("flex gap-2 overflow-x-auto border-b bg-background px-3 py-2", productSearchTrimmed && "hidden")}>
          {topLevelCategories.map((cat) => (
            <button
              key={cat.id}
              type="button"
              onClick={() => {
                setActiveCategoryId(cat.id);
                setActiveSubCategoryId(null);
              }}
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
          {productSearchTrimmed ? (
            <div className="grid grid-cols-2 gap-3 sm:grid-cols-3 lg:grid-cols-4">
              {searchResults.map((item) => {
                const isOutOfStock = outOfStockIds.has(item.id);
                return (
                  <button
                    key={item.id}
                    type="button"
                    disabled={busy || isOutOfStock}
                    onClick={() => handleItemClick(item)}
                    className={cn(
                      "relative flex h-24 flex-col items-center justify-center gap-1 rounded-xl border bg-card p-2 text-center shadow-sm transition-transform active:scale-95 disabled:opacity-50",
                      isOutOfStock && "bg-muted grayscale",
                    )}
                  >
                    {isOutOfStock && (
                      <span className="absolute right-1 top-1 rounded-full bg-destructive px-1.5 py-0.5 text-[10px] font-semibold text-destructive-foreground">
                        Bitib
                      </span>
                    )}
                    <span className="text-sm font-semibold leading-tight">{item.name}</span>
                    <span className="text-xs text-muted-foreground">
                      {(item.stationPrice ?? item.price).toFixed(2)} ₼
                    </span>
                  </button>
                );
              })}
              {searchResults.length === 0 && (
                <p className="col-span-full py-8 text-center text-sm text-muted-foreground">
                  Nəticə tapılmadı
                </p>
              )}
            </div>
          ) : (
          <>
          {activeSubCategoryId != null && (
            <button
              type="button"
              onClick={() => setActiveSubCategoryId(null)}
              className="mb-3 flex items-center gap-1 text-sm font-medium text-muted-foreground hover:text-foreground"
            >
              <ArrowLeft className="h-4 w-4" />
              {categories.find((c) => c.id === activeSubCategoryId)?.name}
            </button>
          )}

          {activeSubCategoryId == null && subCategoriesWithItems.length > 0 && (
            <div className="mb-3 grid grid-cols-2 gap-3 sm:grid-cols-3 lg:grid-cols-4">
              {subCategoriesWithItems.map((sub) => (
                <button
                  key={sub.id}
                  type="button"
                  onClick={() => setActiveSubCategoryId(sub.id)}
                  className="flex h-24 flex-col items-center justify-center gap-1 rounded-xl border-2 border-primary/30 bg-primary/5 p-2 text-center shadow-sm transition-transform active:scale-95 hover:bg-primary/10"
                >
                  <span className="text-sm font-semibold leading-tight text-foreground">{sub.name}</span>
                  <span className="text-xs text-muted-foreground">Bölmə</span>
                </button>
              ))}
            </div>
          )}

          <div className="grid grid-cols-2 gap-3 sm:grid-cols-3 lg:grid-cols-4">
            {itemsInCategory.map((item) => {
              const isOutOfStock = outOfStockIds.has(item.id);
              return (
                <button
                  key={item.id}
                  type="button"
                  disabled={busy || isOutOfStock}
                  onClick={() => handleItemClick(item)}
                  className={cn(
                    "relative flex h-24 flex-col items-center justify-center gap-1 rounded-xl border bg-card p-2 text-center shadow-sm transition-transform active:scale-95 disabled:opacity-50",
                    isOutOfStock && "bg-muted grayscale",
                  )}
                >
                  {isOutOfStock && (
                    <span className="absolute right-1 top-1 rounded-full bg-destructive px-1.5 py-0.5 text-[10px] font-semibold text-destructive-foreground">
                      Bitib
                    </span>
                  )}
                  <span className="text-sm font-semibold leading-tight">{item.name}</span>
                  <span className="text-xs text-muted-foreground">
                    {(item.stationPrice ?? item.price).toFixed(2)} ₼
                  </span>
                </button>
              );
            })}
            {itemsInCategory.length === 0 && subCategoriesWithItems.length === 0 && (
              <p className="col-span-full py-8 text-center text-sm text-muted-foreground">
                Bu kateqoriyada məhsul yoxdur
              </p>
            )}
          </div>
          </>
          )}
        </div>
      </div>

      {/* Cart */}
      <div className="flex w-full flex-col border-t bg-background md:w-[360px] md:border-l md:border-t-0">
        <div className="flex-1 overflow-y-auto p-3">
          {order.lines.length === 0 && (
            <p className="py-8 text-center text-sm text-muted-foreground">Sifariş boşdur</p>
          )}
          <div className="space-y-2">
            {orderedLines.map((line) => {
              const isChild = line.parentLineId != null;
              if (isChild) {
                return (
                  <div key={line.id} className="ml-4 flex items-center justify-between gap-2 border-l-2 pl-2 text-sm text-muted-foreground">
                    <span>↳ {line.menuItemName}</span>
                    <span>x{line.quantity}</span>
                  </div>
                );
              }
              const isHeld = line.holdUntilUtc != null && new Date(line.holdUntilUtc).getTime() > now;
              const holdRemainingMinutes = isHeld
                ? Math.ceil((new Date(line.holdUntilUtc!).getTime() - now) / 60000)
                : 0;
              const isTimerRunning = line.isTimeBased && line.timeBasedStartedAt != null && line.timeBasedStoppedAt == null;
              const timerElapsedMs = isTimerRunning ? now - new Date(line.timeBasedStartedAt!).getTime() : 0;
              const timerLiveTotal = isTimerRunning ? line.unitPrice * (timerElapsedMs / 3_600_000) : line.lineTotal;
              return (
                <div key={line.id} className="rounded-lg border p-2">
                  <div className="flex items-start justify-between gap-2">
                    <span className="text-sm font-medium">
                      {line.menuItemName}
                      {line.isGift && (
                        <Badge className="ml-1.5 bg-pink-100 text-pink-800 hover:bg-pink-100">
                          <Gift className="mr-1 h-3 w-3" />
                          Hədiyyə
                        </Badge>
                      )}
                      {!line.isGift && line.discountAmount > 0 && (
                        <Badge className="ml-1.5 bg-blue-100 text-blue-800 hover:bg-blue-100">
                          -{line.discountAmount.toFixed(2)} ₼
                        </Badge>
                      )}
                    </span>
                    <div className="flex items-center gap-2">
                      {canApplyDiscount && !line.isTimeBased && (
                        <button
                          type="button"
                          onClick={() => void handleToggleGift(line.id, line.quantity, !line.isGift)}
                          className={cn(
                            "text-muted-foreground hover:text-pink-600",
                            line.isGift && "text-pink-600 hover:text-pink-700",
                          )}
                          title={line.isGift ? "Hədiyyədən çıxar" : "Hədiyyə et"}
                        >
                          <Gift className="h-4 w-4" />
                        </button>
                      )}
                      {canApplyDiscount && !line.isTimeBased && !line.isGift && (
                        <button
                          type="button"
                          onClick={() => startDiscountEdit(line.id, line.discountAmount)}
                          className={cn(
                            "text-muted-foreground hover:text-blue-600",
                            line.discountAmount > 0 && "text-blue-600 hover:text-blue-700",
                          )}
                          title="Endirim tətbiq et"
                        >
                          <Tag className="h-4 w-4" />
                        </button>
                      )}
                      {canEditProduct &&
                        line.status !== "InPreparation" &&
                        line.status !== "Ready" &&
                        line.status !== "Served" && (
                          <button
                            type="button"
                            onClick={() => openHoldDialog(line)}
                            className={cn(
                              "text-muted-foreground hover:text-foreground",
                              isHeld && "text-amber-600 hover:text-amber-700",
                            )}
                            title="Gözlət"
                          >
                            <Clock className="h-4 w-4" />
                          </button>
                        )}
                      {canDeleteProduct &&
                        line.status !== "InPreparation" &&
                        line.status !== "Ready" &&
                        line.status !== "Served" && (
                          <button
                            type="button"
                            onClick={() => void handleRemoveLine(line.id)}
                            className="text-muted-foreground hover:text-destructive"
                          >
                            <Trash2 className="h-4 w-4" />
                          </button>
                        )}
                    </div>
                  </div>
                  {isHeld && (
                    <p className="mt-0.5 flex items-center gap-1 text-[11px] font-medium text-amber-600">
                      <Clock className="h-3 w-3" />
                      Gözlədə — {holdRemainingMinutes} dəq sonra
                    </p>
                  )}
                  {isTimerRunning && (
                    <p className="mt-0.5 flex items-center gap-1 text-[11px] font-medium text-emerald-600">
                      <Clock className="h-3 w-3" />
                      İşləyir — {formatDuration(timerElapsedMs)} — {timerLiveTotal.toFixed(2)} ₼
                    </p>
                  )}
                  <div className="mt-1 flex items-center justify-between">
                    {line.isTimeBased ? (
                      <div className="flex items-center gap-2">
                        {isTimerRunning ? (
                          <Button
                            size="sm"
                            variant="outline"
                            className="h-7 gap-1 px-2 text-emerald-700"
                            disabled={busy || !canEditProduct}
                            onClick={() => void handleStopTimer(line.id)}
                          >
                            <Square className="h-3 w-3" />
                            Dayandır
                          </Button>
                        ) : line.timeBasedStoppedAt != null ? (
                          <span className="text-xs text-muted-foreground">Dayandırılıb</span>
                        ) : (
                          <Button
                            size="sm"
                            variant="outline"
                            className="h-7 gap-1 px-2"
                            disabled={busy || !canEditProduct}
                            onClick={() => void handleStartTimer(line.id)}
                          >
                            <Play className="h-3 w-3" />
                            Başlat
                          </Button>
                        )}
                      </div>
                    ) : line.isWeightBased ? (
                      <div className="flex items-center gap-2">
                        <span className="text-sm font-semibold">{(line.quantity / 1000).toFixed(3)} kq</span>
                        {canEditProduct && (
                          <button
                            type="button"
                            disabled={busy}
                            onClick={() => openReweighDialog(line)}
                            className="text-muted-foreground hover:text-primary"
                            title="Çəkini dəyiş"
                          >
                            <Pencil className="h-3 w-3" />
                          </button>
                        )}
                      </div>
                    ) : (
                      <div className="flex items-center gap-2">
                        <button
                          type="button"
                          disabled={busy || !canEditProduct}
                          onClick={() => void handleQuantityChange(line.id, line.quantity, -1)}
                          className="flex h-7 w-7 items-center justify-center rounded-md border disabled:opacity-50"
                        >
                          <Minus className="h-3 w-3" />
                        </button>
                        <span className="w-6 text-center text-sm font-semibold">{line.quantity}</span>
                        <button
                          type="button"
                          disabled={busy || !canEditProduct}
                          onClick={() => void handleQuantityChange(line.id, line.quantity, 1)}
                          className="flex h-7 w-7 items-center justify-center rounded-md border disabled:opacity-50"
                        >
                          <Plus className="h-3 w-3" />
                        </button>
                      </div>
                    )}
                    {priceEditingLineId === line.id ? (
                      <div className="flex items-center gap-1">
                        <Input
                          type="number"
                          step="0.01"
                          autoFocus
                          value={priceInput}
                          onChange={(e) => setPriceInput(e.target.value)}
                          className="h-7 w-20 text-right text-sm"
                        />
                        <Button size="sm" className="h-7 px-2" disabled={busy} onClick={() => void handleSavePrice(line.id, line.quantity)}>
                          OK
                        </Button>
                      </div>
                    ) : discountEditingLineId === line.id ? (
                      <div className="flex items-center gap-1">
                        <Input
                          type="number"
                          step="0.01"
                          min={0}
                          autoFocus
                          value={discountAmountInput}
                          onChange={(e) => setDiscountAmountInput(e.target.value)}
                          placeholder="Endirim ₼"
                          className="h-7 w-24 text-right text-sm"
                        />
                        <Button size="sm" className="h-7 px-2" disabled={busy} onClick={() => void handleSaveDiscount(line.id, line.quantity)}>
                          OK
                        </Button>
                      </div>
                    ) : (
                      <div className="flex items-center gap-1">
                        {canChangePrice && !line.isTimeBased && (
                          <button
                            type="button"
                            onClick={() => startPriceEdit(line.id, line.unitPrice)}
                            className="text-muted-foreground hover:text-primary"
                          >
                            <Pencil className="h-3 w-3" />
                          </button>
                        )}
                        {canChangePrice &&
                          !line.isTimeBased &&
                          !busy &&
                          (() => {
                            const packagePrice = items.find((i) => i.id === line.menuItemId)?.packagePrice;
                            if (packagePrice == null || packagePrice === line.unitPrice) return null;
                            return (
                              <button
                                type="button"
                                onClick={() => void applyQuickPrice(line.id, line.quantity, packagePrice)}
                                className="text-muted-foreground hover:text-primary"
                                title={`Paket qiyməti tətbiq et (${packagePrice.toFixed(2)} ₼)`}
                              >
                                <Package className="h-3 w-3" />
                              </button>
                            );
                          })()}
                        {canChangePrice &&
                          !line.isTimeBased &&
                          !busy &&
                          ([1, 2, 3] as const).map((n) => {
                            const menuItem = items.find((i) => i.id === line.menuItemId);
                            const price =
                              n === 1 ? menuItem?.specialPrice1 : n === 2 ? menuItem?.specialPrice2 : menuItem?.specialPrice3;
                            if (price == null || price === line.unitPrice) return null;
                            return (
                              <button
                                key={n}
                                type="button"
                                onClick={() => void applyQuickPrice(line.id, line.quantity, price)}
                                className="text-[10px] font-bold text-muted-foreground hover:text-primary"
                                title={`Qiymət ${n} tətbiq et (${price.toFixed(2)} ₼)`}
                              >
                                Q{n}
                              </button>
                            );
                          })}
                        <span className="text-sm font-semibold">{timerLiveTotal.toFixed(2)} ₼</span>
                      </div>
                    )}
                  </div>
                </div>
              );
            })}
          </div>
        </div>
        {canPrint && printerGroups.length > 0 && !isPaid && (
          <div className="flex flex-wrap gap-2 border-t p-3">
            {printerGroups.map(({ printer, lines }) => (
              <Button
                key={printer.id}
                size="sm"
                variant="outline"
                disabled={printingId === printer.id}
                onClick={() => void handlePrintGroup(printer)}
              >
                <Printer className="mr-1 h-3.5 w-3.5" />
                {printer.name}-ə göndər ({lines.length})
              </Button>
            ))}
          </div>
        )}
        <div className="border-t p-3">
          <div className="mb-3 flex items-center justify-between text-lg font-bold">
            <span>Cəm</span>
            <span>{(order.totalAmount + (isRentalRunning ? rentalLiveTotal : order.tableRentalAmount ?? 0)).toFixed(2)} ₼</span>
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
            <div className="space-y-2">
              {canPrintBill && (
                <Button
                  variant="outline"
                  className="h-12 w-full text-base font-semibold"
                  disabled={order.lines.length === 0 || busy}
                  onClick={() => void handlePrintBill()}
                >
                  <Receipt className="mr-2 h-4 w-4" />
                  Qəbz çap et
                </Button>
              )}
              {canPay && (
                <Button
                  className="h-12 w-full text-base font-semibold"
                  disabled={order.lines.length === 0 || busy}
                  onClick={openPayDialog}
                >
                  Ödə
                </Button>
              )}
            </div>
          )}
        </div>
      </div>

      {/* Payment dialog */}
      <Dialog open={payOpen} onOpenChange={setPayOpen}>
        <DialogContent className="sm:max-w-sm">
          <DialogHeader>
            <DialogTitle>Ödəniş</DialogTitle>
            <DialogDescription>Cəm: {totalWithService.toFixed(2)} ₼</DialogDescription>
          </DialogHeader>
          <div className="space-y-3">
            {canTableServiceCharge && (
              <div className="space-y-1">
                <Label htmlFor="service-charge">Servis haqqı (opsional)</Label>
                <Input
                  id="service-charge"
                  type="number"
                  step="0.01"
                  value={serviceChargeInput}
                  onChange={(e) => {
                    setServiceChargeInput(e.target.value);
                    if (paymentMethod === "Card") {
                      setPaidAmountInput(
                        (order.totalAmount + (order.tableRentalAmount ?? 0) + (Number(e.target.value) || 0)).toFixed(2),
                      );
                    }
                  }}
                  placeholder="0.00"
                />
              </div>
            )}
            <div className={cn("grid gap-2", order.counterpartyId != null ? "grid-cols-3" : "grid-cols-2")}>
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
                  setPaidAmountInput(totalWithService.toFixed(2));
                }}
              >
                Kart
              </Button>
              {order.counterpartyId != null && (
                <Button
                  type="button"
                  variant={paymentMethod === "Credit" ? "default" : "outline"}
                  onClick={() => {
                    setPaymentMethod("Credit");
                    setPaidAmountInput(totalWithService.toFixed(2));
                  }}
                >
                  Borca yaz
                </Button>
              )}
            </div>
            {paymentMethod === "Credit" && (
              <p className="text-sm text-muted-foreground">
                {order.counterpartyName} adına {totalWithService.toFixed(2)} ₼ borc yazılacaq
                {order.counterpartyDebtAmount != null && order.counterpartyDebtAmount > 0 && (
                  <> (əvvəlki borc: {order.counterpartyDebtAmount.toFixed(2)} ₼)</>
                )}
                .
              </p>
            )}
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

      {/* Confirm auto-print dialog */}
      <Dialog open={confirmPrintOpen} onOpenChange={(o) => !o && setConfirmPrintOpen(false)}>
        <DialogContent className="sm:max-w-sm">
          <DialogHeader>
            <DialogTitle>Çap et?</DialogTitle>
            <DialogDescription>Qəbzi indi çap etmək istəyirsiniz?</DialogDescription>
          </DialogHeader>
          <DialogFooter>
            <Button variant="outline" onClick={() => setConfirmPrintOpen(false)}>
              İmtina et
            </Button>
            <Button onClick={handleConfirmAutoPrint}>
              <Printer className="mr-2 h-4 w-4" />
              Çap et
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
              {branding?.receiptShowBusinessName !== false && (
                <p
                  className="text-center font-bold uppercase tracking-wide"
                  style={{
                    fontSize: branding?.receiptRestaurantNameFontSize
                      ? `${branding.receiptRestaurantNameFontSize}px`
                      : "18px",
                    color: branding?.productColor || undefined,
                  }}
                >
                  {receipt?.restaurantName}
                </p>
              )}
              <DialogTitle className="sr-only">Qəbz</DialogTitle>
              {(branding?.receiptShowTableName !== false && receipt?.tableName) || branding?.floorLabel ? (
                <DialogDescription className="text-center">
                  {branding?.receiptShowTableName !== false && receipt?.tableName}
                  {branding?.floorLabel &&
                    (branding?.receiptShowTableName !== false && receipt?.tableName
                      ? ` — ${branding.floorLabel}`
                      : branding.floorLabel)}
                </DialogDescription>
              ) : null}
              {receipt &&
                (showReceiptOrderNumber || showReceiptWaiterName || showReceiptTime || showReceiptPaymentMethod) && (
                  <div className="space-y-0.5 text-xs text-muted-foreground">
                    {showReceiptOrderNumber && <p>Sifariş: {receipt.orderNumber}</p>}
                    {showReceiptWaiterName && <p>Ofisiant: {receipt.waiterName}</p>}
                    {showReceiptTime && (
                      <p>Vaxt: {new Date(receipt.paidAt ?? receipt.openedAt).toLocaleString("az-AZ")}</p>
                    )}
                    {showReceiptPaymentMethod && <p>Ödəniş: {receipt.paymentMethod}</p>}
                  </div>
                )}
            </DialogHeader>
            <div className="space-y-1 text-sm">
              {groupedReceiptLines.map((line, i) => (
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
                {(order.serviceChargeAmount ?? 0) > 0 && (
                  <div className="flex justify-between font-normal text-muted-foreground">
                    <span>Servis haqqı</span>
                    <span>{(order.serviceChargeAmount ?? 0).toFixed(2)} ₼</span>
                  </div>
                )}
                <div className="flex justify-between">
                  <span>Cəm</span>
                  <span>{receipt?.totalAmount.toFixed(2)} ₼</span>
                </div>
                {receipt && receipt.vatAmount > 0 && showReceiptVat && (
                  <div className="flex justify-between font-normal text-muted-foreground">
                    <span>ƏDV daxildir</span>
                    <span>{receipt.vatAmount.toFixed(2)} ₼</span>
                  </div>
                )}
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
              {showReceiptFooter && (branding?.slogan || branding?.contactPhoneNumber || branding?.socialLinks) && (
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
            {canPrintReceipt &&
              sortedPrinters.map((printer) => (
                <Button
                  key={printer.id}
                  variant={printer.isPrimary ? "default" : "outline"}
                  className="w-full"
                  disabled={printingId === printer.id}
                  onClick={() => void handlePrintReceiptToPrinter(printer)}
                >
                  <Printer className="mr-2 h-4 w-4" />
                  {printer.name}-ə göndər
                  {printer.isPrimary && " (əsas)"}
                </Button>
              ))}
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

      {/* Move table dialog */}
      <Dialog open={moveTableOpen} onOpenChange={setMoveTableOpen}>
        <DialogContent className="sm:max-w-sm">
          <DialogHeader>
            <DialogTitle>Masanı dəyiş</DialogTitle>
            <DialogDescription>Boş masalardan birini seçin.</DialogDescription>
          </DialogHeader>
          <div className="grid grid-cols-3 gap-2">
            {availableTables.map((t) => (
              <Button key={t.id} variant="outline" disabled={busy} onClick={() => void handleMoveTable(t.id)}>
                {t.name}
              </Button>
            ))}
            {availableTables.length === 0 && (
              <p className="col-span-3 py-4 text-center text-sm text-muted-foreground">Boş masa yoxdur</p>
            )}
          </div>
        </DialogContent>
      </Dialog>

      {/* Reassign waiter dialog */}
      <Dialog open={reassignOpen} onOpenChange={setReassignOpen}>
        <DialogContent className="sm:max-w-sm">
          <DialogHeader>
            <DialogTitle>Ofisiantı dəyiş</DialogTitle>
            <DialogDescription>Yeni ofisiantı seçin.</DialogDescription>
          </DialogHeader>
          <div className="max-h-64 space-y-1 overflow-y-auto">
            {employeesList.map((e) => (
              <button
                key={e.id}
                type="button"
                disabled={busy}
                onClick={() => void handleReassignWaiter(e.id)}
                className="flex w-full items-center rounded-md border px-3 py-2 text-left text-sm hover:bg-muted disabled:opacity-50"
              >
                {formatEmployeeName(e)}
              </button>
            ))}
            {employeesList.length === 0 && (
              <p className="py-4 text-center text-sm text-muted-foreground">İşçi tapılmadı</p>
            )}
          </div>
        </DialogContent>
      </Dialog>

      {/* Set delivery driver dialog */}
      <Dialog open={driverDialogOpen} onOpenChange={setDriverDialogOpen}>
        <DialogContent className="sm:max-w-sm">
          <DialogHeader>
            <DialogTitle>Kuryer təyin et</DialogTitle>
            <DialogDescription>Bu çatdırılma sifarişi üçün kuryer seçin.</DialogDescription>
          </DialogHeader>
          <div className="max-h-64 space-y-1 overflow-y-auto">
            {order?.deliveryDriverEmployeeId != null && (
              <button
                type="button"
                disabled={busy}
                onClick={() => void handleSetDriver(null)}
                className="flex w-full items-center rounded-md border px-3 py-2 text-left text-sm text-destructive hover:bg-muted disabled:opacity-50"
              >
                Kuryeri götür
              </button>
            )}
            {employeesList.map((e) => (
              <button
                key={e.id}
                type="button"
                disabled={busy}
                onClick={() => void handleSetDriver(e.id)}
                className="flex w-full items-center rounded-md border px-3 py-2 text-left text-sm hover:bg-muted disabled:opacity-50"
              >
                {formatEmployeeName(e)}
              </button>
            ))}
            {employeesList.length === 0 && (
              <p className="py-4 text-center text-sm text-muted-foreground">İşçi tapılmadı</p>
            )}
          </div>
        </DialogContent>
      </Dialog>

      <Dialog open={noteDialogOpen} onOpenChange={setNoteDialogOpen}>
        <DialogContent className="sm:max-w-sm">
          <DialogHeader>
            <DialogTitle>Sifariş qeydi</DialogTitle>
            <DialogDescription>Bu qeyd sifarişlə birlikdə saxlanılır.</DialogDescription>
          </DialogHeader>
          <Input
            autoFocus
            value={noteInput}
            onChange={(e) => setNoteInput(e.target.value)}
            placeholder="məs. Müştəri allergiyası var"
          />
          <DialogFooter>
            <Button variant="outline" onClick={() => setNoteDialogOpen(false)} disabled={noteBusy}>
              Ləğv et
            </Button>
            <Button onClick={() => void handleSaveNote()} disabled={noteBusy}>
              Saxla
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      <Dialog open={weightDialogItem != null} onOpenChange={(o) => !o && setWeightDialogItem(null)}>
        <DialogContent className="sm:max-w-sm">
          <DialogHeader>
            <DialogTitle>{weightDialogItem?.name}</DialogTitle>
            <DialogDescription>
              Tərəzidə göstərilən çəkini kq olaraq daxil edin
              {weightDialogItem && (
                <> — {(weightDialogItem.stationPrice ?? weightDialogItem.price).toFixed(2)} ₼/kq</>
              )}
              .
            </DialogDescription>
          </DialogHeader>
          <Input
            type="number"
            min={0}
            step="0.001"
            autoFocus
            placeholder="0.000"
            value={weightKgInput}
            onChange={(e) => setWeightKgInput(e.target.value)}
            onKeyDown={(e) => {
              if (e.key === "Enter") void handleConfirmWeight();
            }}
          />
          {weightDialogItem && Number(weightKgInput) > 0 && (
            <p className="text-sm text-muted-foreground">
              Cəm: {((weightDialogItem.stationPrice ?? weightDialogItem.price) * Number(weightKgInput)).toFixed(2)} ₼
            </p>
          )}
          <DialogFooter>
            <Button variant="outline" onClick={() => setWeightDialogItem(null)} disabled={weightBusy}>
              Ləğv et
            </Button>
            <Button onClick={() => void handleConfirmWeight()} disabled={weightBusy || !(Number(weightKgInput) > 0)}>
              {weightDialogLineId != null ? "Yenilə" : "Əlavə et"}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      <Dialog open={discountDialogOpen} onOpenChange={setDiscountDialogOpen}>
        <DialogContent className="sm:max-w-sm">
          <DialogHeader>
            <DialogTitle>Endirim tətbiq et</DialogTitle>
            <DialogDescription>Endirim kodunu daxil edin.</DialogDescription>
          </DialogHeader>
          <Input
            placeholder="Endirim kodu"
            autoFocus
            value={discountCodeInput}
            onChange={(e) => setDiscountCodeInput(e.target.value)}
            onKeyDown={(e) => {
              if (e.key === "Enter") void handleApplyDiscount();
            }}
          />
          <DialogFooter>
            <Button variant="outline" onClick={() => setDiscountDialogOpen(false)} disabled={discountBusy}>
              Ləğv et
            </Button>
            <Button onClick={() => void handleApplyDiscount()} disabled={discountBusy || !discountCodeInput.trim()}>
              Tətbiq et
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      <Dialog open={counterpartyDialogOpen} onOpenChange={setCounterpartyDialogOpen}>
        <DialogContent className="sm:max-w-sm">
          <DialogHeader>
            <DialogTitle>Müştəri seç</DialogTitle>
            <DialogDescription>Bu sifarişə müştəri bağlayın.</DialogDescription>
          </DialogHeader>
          <Input
            placeholder="Axtar..."
            autoFocus
            value={counterpartySearch}
            onChange={(e) => setCounterpartySearch(e.target.value)}
          />
          <div className="max-h-64 space-y-1 overflow-y-auto">
            {order.counterpartyId != null && (
              <button
                type="button"
                disabled={counterpartyBusy}
                onClick={() => void handleSelectCounterparty(null)}
                className="flex w-full items-center rounded-md border px-3 py-2 text-left text-sm text-destructive hover:bg-muted disabled:opacity-50"
              >
                Müştərini götür
              </button>
            )}
            {filteredCounterparties.map((c) => (
              <button
                key={c.id}
                type="button"
                disabled={counterpartyBusy}
                onClick={() => void handleSelectCounterparty(c.id)}
                className={cn(
                  "flex w-full items-center justify-between gap-2 rounded-md border px-3 py-2 text-left text-sm hover:bg-muted disabled:opacity-50",
                  order.counterpartyId === c.id && "border-primary bg-primary/5",
                )}
              >
                <span>{c.name}</span>
                <span className="text-xs text-muted-foreground">{c.categoryName}</span>
              </button>
            ))}
            {filteredCounterparties.length === 0 && (
              <p className="py-4 text-center text-sm text-muted-foreground">Kontragent tapılmadı</p>
            )}
          </div>
        </DialogContent>
      </Dialog>

      <Dialog open={holdLineId !== null} onOpenChange={(open) => !open && setHoldLineId(null)}>
        <DialogContent className="sm:max-w-xs">
          <DialogHeader>
            <DialogTitle>Gözlət</DialogTitle>
            <DialogDescription>Neçə dəqiqə sonra mətbəxə göndərilsin?</DialogDescription>
          </DialogHeader>
          <div className="space-y-2">
            <Label htmlFor="hold-minutes-input">Dəqiqə</Label>
            <Input
              id="hold-minutes-input"
              type="number"
              min={1}
              autoFocus
              placeholder="məs. 20"
              value={holdMinutesInput}
              onChange={(e) => setHoldMinutesInput(e.target.value)}
              onKeyDown={(e) => {
                if (e.key === "Enter") void handleConfirmHold();
              }}
            />
          </div>
          <DialogFooter>
            {holdLineId != null && orderedLines.find((l) => l.id === holdLineId)?.holdUntilUtc && (
              <Button
                variant="ghost"
                className="mr-auto text-muted-foreground"
                onClick={() => void submitHold(holdLineId, null)}
              >
                Gözlətməni ləğv et
              </Button>
            )}
            <Button variant="outline" onClick={() => setHoldLineId(null)}>
              Bağla
            </Button>
            <Button onClick={() => void handleConfirmHold()}>Təsdiqlə</Button>
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
