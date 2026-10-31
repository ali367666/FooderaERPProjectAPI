"use client";

import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import { useParams, useRouter } from "next/navigation";
import { toast } from "sonner";
import { ArrowLeft, ArrowLeftRight, Banknote, CalendarCheck, CalendarPlus, ChevronDown, ChevronUp, Clock, CreditCard, Hash, Keyboard, LayoutGrid, LogOut, MoreHorizontal, Scale, Gift, Minus, Package, Pencil, Play, Plus, Printer, Receipt, Search, Square, StickyNote, Tag, Lightbulb, Percent, Trash2, User, UserCog, Users, UtensilsCrossed, X } from "lucide-react";
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
  setManualDiscount,
  getOrderPayments,
  getPaymentReceipt,
  type OrderPaymentsDto,
  setOrderDeliveryDriver,
  setOrderCounterparty,
  printKitchenTicket,
  submitOrder,
  sendMars,
  markBillPrinted,
  unlockBill,
  startTimeBasedLine,
  stopTimeBasedLine,
  startTableRental,
  stopTableRental,
  removeDiscountFromOrder,
  updateOrder,
  type OrderDto,
  type OrderLineDto,
  type OrderReceiptDto,
  type PaymentMethod,
} from "@/lib/services/order-service";
import { useCancelReason } from "@/components/cancel-reason-dialog";
import { getCounterparties, type Counterparty } from "@/lib/services/counterparty-service";
import { getMenuCategories, type MenuCategory } from "@/lib/services/menu-category-service";
import { getMenuItems, getMenuItemAvailability, MenuItemPriceType, UnitOfMeasure, type MenuItem } from "@/lib/services/menu-item-service";
import {
  getRestaurantTables,
  getRestaurantTableById,
  updateRestaurantTable,
  RestaurantTableType,
  type RestaurantTable,
} from "@/lib/services/restaurant-table-service";
import { getEmployees, type Employee } from "@/lib/services/employee-service";
import { getPrinters, printImageToPrinter, type Printer as PrinterProfile } from "@/lib/services/printer-service";
import { canvasToRaster, designFromBranding, renderReceipt } from "@/lib/receipt-render";
import { ReceiptPreview } from "@/components/receipt-preview";
import { getPosTerminalContext } from "@/lib/pos-terminal-client";
import { escPosBarcode, receiptBarcodeValue } from "@/lib/receipt-barcode";
import { BarcodeSvg } from "@/components/barcode-svg";
import { getFiscalDevices } from "@/lib/services/fiscal-device-service";
import { setFiscalMode, useFiscalMode } from "@/lib/pos-fiscal-mode";
import { CustomerReceiptsDialog } from "@/components/pos/customer-receipts-dialog";
import { PayDialog, type Method as PayMethod } from "@/components/pos/pay-dialog";
import { NewReservationDialog } from "@/components/pos/new-reservation-dialog";
import { VirtualKeyboard } from "@/components/pos/virtual-keyboard";
import { getRestaurantSections } from "@/lib/services/restaurant-section-service";
import { SplitBillDialog } from "@/components/pos/split-bill-dialog";
import { SetSelectionDialog, isSetPickAllowed, type SetPick } from "@/components/pos/set-selection-dialog";
import { localIsoDate } from "@/lib/reservation-rules";
import {
  changeReservationStatus,
  getReservations,
  type ReservationDto,
} from "@/lib/services/reservation-service";
import { TouchNumpad } from "@/components/pos/touch-numpad";
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

/** Lower-cases and folds Azerbaijani letters so typing "aci" finds "Acı", "qehve" finds "Qəhvə". */
function normalizeSearch(s: string): string {
  let lower: string;
  try {
    lower = s.toLocaleLowerCase("az");
  } catch {
    lower = s.toLowerCase();
  }
  return lower
    .replace(/ı/g, "i")
    .replace(/ə/g, "e")
    .replace(/ö/g, "o")
    .replace(/ü/g, "u")
    .replace(/ş/g, "s")
    .replace(/ç/g, "c")
    .replace(/ğ/g, "g");
}

function formatEmployeeName(e: Employee): string {
  const n = e.fullName?.trim();
  if (n) return n;
  return `${e.firstName} ${e.lastName}`.trim() || `Employee #${e.id}`;
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
  const [setDialogItem, setSetDialogItem] = useState<MenuItem | null>(null);
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
  const [reasonDialog, askCancelReason] = useCancelReason();
  const [orderHoldBusy, setOrderHoldBusy] = useState(false);
  const [confirmPrintOpen, setConfirmPrintOpen] = useState(false);

  const [payOpen, setPayOpen] = useState(false);
  const [receipt, setReceipt] = useState<OrderReceiptDto | null>(null);
  const [branding, setBranding] = useState<CompanySettingsBranding | null>(null);

  const [moveTableOpen, setMoveTableOpen] = useState(false);
  const [availableTables, setAvailableTables] = useState<RestaurantTable[]>([]);
  const [reassignOpen, setReassignOpen] = useState(false);
  // Double-tap on a line opens its note.
  const [lineNoteLine, setLineNoteLine] = useState<OrderLineDto | null>(null);
  const [lineNoteInput, setLineNoteInput] = useState("");
  // "Qiymət" mode: tap a product, then pick one of its price lists.
  const [priceMode, setPriceMode] = useState(false);
  // "Hədiyyə" mode: tap a product to make it a gift — or, if it is one already, to take it off the gifts.
  const [giftMode, setGiftMode] = useState(false);
  const fiscalOn = useFiscalMode();
  // "₼" / "%" buttons — a hand-typed order discount.
  const [manualDiscountKind, setManualDiscountKind] = useState<"Amount" | "Percent" | null>(null);
  const [manualDiscountInput, setManualDiscountInput] = useState("");
  const [manualDiscountBusy, setManualDiscountBusy] = useState(false);
  const [priceLine, setPriceLine] = useState<OrderLineDto | null>(null);
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

  const [discountBusy, setDiscountBusy] = useState(false);

  // The line the toolbar above the list ("Qiymət", "Miqdar", "+", "Silmə") acts on; it shows its own controls.
  const [selectedLineId, setSelectedLineId] = useState<number | null>(null);
  const [qtyLine, setQtyLine] = useState<OrderLineDto | null>(null);
  const [qtyInput, setQtyInput] = useState("");
  // Tapping a running minute-billed line asks "Dayandır" / "Davam et".
  const [timerLine, setTimerLine] = useState<OrderLineDto | null>(null);
  const [moreOpen, setMoreOpen] = useState(false);
  const [reservationOpen, setReservationOpen] = useState(false);
  // "KART" / "NƏĞD" open the payment straight on that method; "Hesab" opens the chooser.
  const [payMethod, setPayMethod] = useState<PayMethod | null>(null);
  const [searchOpen, setSearchOpen] = useState(false);
  const [searchKeyboardOpen, setSearchKeyboardOpen] = useState(false);
  const searchInputRef = useRef<HTMLInputElement | null>(null);
  const [hallName, setHallName] = useState<string | null>(null);
  // ↑ / ↓ next to the categories page through the products.
  const productScrollRef = useRef<HTMLDivElement | null>(null);
  const [productScroll, setProductScroll] = useState({ up: false, down: false });
  const syncProductScroll = useCallback(() => {
    const el = productScrollRef.current;
    if (!el) return;
    setProductScroll({ up: el.scrollTop > 4, down: el.scrollTop + el.clientHeight < el.scrollHeight - 4 });
  }, []);
  const pageProducts = (direction: 1 | -1) => {
    const el = productScrollRef.current;
    if (!el) return;
    el.scrollBy({ top: direction * Math.max(el.clientHeight - 80, 120), behavior: "smooth" });
  };
  const canCreateReservation = useHasPermission("Reservation.Create");

  const isStoreMode = branding?.moduleDataSecimi === true;
  const canAddProduct = useHasPermission("Orders.Add");
  const canEditProduct = useHasPermission("Pos.EditProductInSale");
  const canDeleteProduct = useHasPermission("Pos.DeleteProductInSale");
  const canDeleteOrder = useHasPermission("Pos.DeleteOrder");
  const canRedirectUser = useHasPermission("Pos.RedirectUser");
  // Every action button shows only for someone who may use it (the server enforces the same permissions).
  const canMoveTable = useHasPermission("Pos.MoveTable");
  const canChangeWaiter = useHasPermission("Pos.ChangeWaiter");
  const canPay = useHasPermission("Orders.Pay");
  const canViewReservations = useHasPermission("Reservation.View");
  const canConfirmReservation = useHasPermission("Reservation.Confirm");
  // The pending/confirmed reservation held on this order's table — the guest sat down, so it can be completed here.
  const [tableReservation, setTableReservation] = useState<ReservationDto | null>(null);
  const [reservationBusy, setReservationBusy] = useState(false);
  const orderTableId = order?.tableId;

  const loadTableReservation = useCallback(() => {
    if (!orderTableId || !canViewReservations) {
      setTableReservation(null);
      return;
    }
    getReservations(localIsoDate())
      .then((rows) =>
        setTableReservation(
          rows.find(
            (r) => r.tableId === orderTableId && (r.status === "Pending" || r.status === "Confirmed"),
          ) ?? null,
        ),
      )
      .catch(() => setTableReservation(null));
  }, [orderTableId, canViewReservations]);

  useEffect(() => {
    loadTableReservation();
  }, [loadTableReservation]);

  const canChangePrice = useHasPermission("Pos.ChangePrice");
  const canApplyDiscount = useHasPermission("Discount.Apply");
  const canPrint = useHasPermission("Printer.Print");
  const canPrintBill = useHasPermission("Pos.PrintReceipt");
  const canUnlockBill = useHasPermission("Pos.UnlockBill");

  const [printers, setPrinters] = useState<PrinterProfile[]>([]);
  const [printingId, setPrintingId] = useState<number | null>(null);
  const [pinPrinter, setPinPrinter] = useState<PrinterProfile | null>(null);
  const [pinInput, setPinInput] = useState("");
  // "OK" — confirm the order and send it to the kitchen.
  const [okPinOpen, setOkPinOpen] = useState(false);
  const [okBusy, setOkBusy] = useState(false);
  const [marsBusy, setMarsBusy] = useState(false);
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

  // "ZAL" in the order header: the hall (restaurant section) of this order's table.
  const orderRestaurantId = order?.restaurantId;
  useEffect(() => {
    if (!orderTableId || !orderRestaurantId) return;
    let cancelled = false;
    Promise.all([getRestaurantTableById(orderTableId), getRestaurantSections(orderRestaurantId)])
      .then(([table, sections]) => {
        if (cancelled) return;
        setHallName(sections.find((s) => s.id === table.sectionId)?.name ?? null);
      })
      .catch(() => !cancelled && setHallName(null));
    return () => {
      cancelled = true;
    };
  }, [orderTableId, orderRestaurantId]);

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

  // "OK": confirms the order (draft → open, so the kitchen display sees it) and sends every station its
  // new items. Back to the tables when everything went out; stays put if a printer failed.
  const handleOk = async (pin?: string) => {
    if (!order || okBusy) return;
    if (order.lines.length === 0) {
      toast.error("Sifariş boşdur");
      return;
    }
    const sendsToKitchen = canPrint && printerGroups.length > 0;
    if (sendsToKitchen && branding?.waiterConfirmWithPin === true && pin === undefined) {
      setPinInput("");
      setOkPinOpen(true);
      return;
    }
    setOkBusy(true);
    let failed = false;
    try {
      if (order.status === "draft") await submitOrder(order.id);
      let sent = 0;
      if (sendsToKitchen) {
        for (const group of printerGroups) {
          try {
            sent += await printKitchenTicket(order.id, group.printer.id, pin);
          } catch (err) {
            failed = true;
            toast.error(
              err instanceof Error ? `${group.printer.name}: ${err.message}` : `${group.printer.name}-ə göndərilmədi`,
            );
          }
        }
      }
      setOkPinOpen(false);
      if (failed) {
        await load();
        return;
      }
      toast.success(sent > 0 ? `Sifariş mətbəxə göndərildi (${sent} məhsul)` : "Sifariş təsdiqləndi");
      router.replace("/pos");
    } catch (err) {
      toast.error(err instanceof Error ? err.message : "Sifariş təsdiqlənmədi");
    } finally {
      setOkBusy(false);
    }
  };

  const handlePrintGroup = async (printer: PrinterProfile, pin?: string) => {
    if (!order) return;
    // "Ofisiant təsdiqlə" — ask the order's waiter for their code before the kitchen gets it.
    if (branding?.waiterConfirmWithPin === true && pin === undefined) {
      setPinInput("");
      setPinPrinter(printer);
      return;
    }
    setPrintingId(printer.id);
    try {
      const count = await printKitchenTicket(order.id, printer.id, pin);
      setPinPrinter(null);
      toast.success(count > 0 ? `${printer.name}-ə ${count} məhsul göndərildi` : "Yeni məhsul yoxdur");
      await load();
    } catch (err) {
      toast.error(err instanceof Error ? err.message : "Printerə qoşulmaq mümkün olmadı");
    } finally {
      setPrintingId(null);
    }
  };

  const receiptCategoryName = (id: number | null | undefined) =>
    id == null ? "" : categories.find((c) => c.id === id)?.name ?? "";


  const handleMars = async () => {
    if (!order) return;
    setMarsBusy(true);
    try {
      const count = await sendMars(order.id);
      toast.success(`Marş ${count} mətbəx printerinə göndərildi`);
      await load();
    } catch (err) {
      toast.error(err instanceof Error ? err.message : "Marş göndərilmədi");
    } finally {
      setMarsBusy(false);
    }
  };

  const handleUnlockBill = async () => {
    if (!order) return;
    try {
      await unlockBill(order.id);
      toast.success("Hesab kilidi açıldı");
      await load();
    } catch (err) {
      toast.error(err instanceof Error ? err.message : "Hesab kilidi açılmadı");
    }
  };

  // The customer receipt is drawn as an image (logo, every Azerbaijani letter, adjustable fonts)
  // — the dialog shows that same image, and network printers receive it as ESC/POS raster.
  const receiptDesign = useMemo(() => designFromBranding(branding), [branding]);

  const handlePrintReceiptToPrinter = async (printer: PrinterProfile, r?: OrderReceiptDto, opts?: { preCheck?: boolean }) => {
    const target = r ?? receipt;
    if (!target) return;
    setPrintingId(printer.id);
    try {
      const canvas = await renderReceipt(target, receiptDesign, receiptCategoryName, opts);
      const trailer = order ? `
${escPosBarcode(receiptBarcodeValue(order.id))}` : undefined;
      await printImageToPrinter(printer.id, canvasToRaster(canvas), trailer);
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

  // Part payments ("Hesab"): who already paid for what.
  const [payments, setPayments] = useState<OrderPaymentsDto | null>(null);
  const [splitOpen, setSplitOpen] = useState(false);
  const [receiptsOpen, setReceiptsOpen] = useState(false);
  const hasPartPayments = payments != null && payments.payments.length > 0;
  // What is still to pay (without service charge): the whole bill, or what is left after part payments.
  const dueBase = hasPartPayments
    ? payments!.remainingAmount
    : (order?.totalAmount ?? 0) + (order?.tableRentalAmount ?? 0);

  const [paymentsError, setPaymentsError] = useState<string | null>(null);

  const refreshPayments = useCallback(async (id: number) => {
    try {
      setPayments(await getOrderPayments(id));
      setPaymentsError(null);
    } catch (err) {
      setPayments(null);
      setPaymentsError(err instanceof Error ? err.message : "Ödənişlər yüklənmədi");
    }
  }, []);

  useEffect(() => {
    if (order?.id) void refreshPayments(order.id);
  }, [order?.id, order?.isPaid, order?.totalAmount, order?.lines.length, refreshPayments]);

  const printPaymentReceipt = async (paymentId: number) => {
    if (!order) return;
    const printer = sortedPrinters[0];
    if (!printer) {
      toast.error("Çek çıxarmaq üçün aktiv printer tapılmadı.");
      return;
    }
    try {
      const r = await getPaymentReceipt(order.id, paymentId);
      await handlePrintReceiptToPrinter(printer, r);
    } catch (err) {
      toast.error(err instanceof Error ? err.message : "Çek çıxarıla bilmədi");
    }
  };

  // One guest paid their items: the dialog closes, the rest of the order stays on the page (paid
  // items are tagged) and the guest's customer receipt comes out of the printer automatically.
  const handlePartPaid = async (result: { paymentId: number; amount: number; orderClosed: boolean }) => {
    if (!order) return;
    setSplitOpen(false);
    // A printer problem never undoes the payment — it is reported and "Müştəri qəbzi" can retry.
    await printPaymentReceipt(result.paymentId);
    if (result.orderClosed) {
      // The last guest: the table is closed; their receipt (earlier guests' items marked paid) stays on screen.
      setFiscalMode(false);
      try {
        setReceipt(await getPaymentReceipt(order.id, result.paymentId));
      } catch {
        router.replace("/pos");
      }
      return;
    }
    toast.success(`Hesab ödənildi: ${result.amount.toFixed(2)} ₼`);
    try {
      setOrder(await getOrderById(order.id));
    } catch {
      /* the list refreshes on the next load */
    }
    await refreshPayments(order.id);
  };

  // "Müştəri qəbzi": the pre-check for the guest. Nothing is recorded or locked — the order can still be changed.
  const printWholeReceipt = async () => {
    if (!order) return;
    const printer = sortedPrinters[0];
    if (!printer) {
      toast.error("Çek çıxarmaq üçün aktiv printer tapılmadı.");
      return;
    }
    try {
      await handlePrintReceiptToPrinter(printer, await getOrderReceipt(order.id, fiscalOn), { preCheck: !order.isPaid });
    } catch (err) {
      toast.error(err instanceof Error ? err.message : "Çek çıxarıla bilmədi");
    }
  };

  const handleCustomerReceipt = () => {
    // Nobody paid a share yet: print the pre-check straight away. Otherwise pick whose receipt.
    if (hasPartPayments) setReceiptsOpen(true);
    else void printWholeReceipt();
  };

  const topLevelCategories = useMemo(
    () => categories.filter((c) => c.parentCategoryId == null),
    [categories],
  );

  // A banquet order (opened in a banquet hall) sells only banquet-menu products, at the banquet price —
  // the server refuses anything else. `items` stays whole: set components still need every product.
  const isBanquet = order?.isBanquet === true;
  const sellableItems = useMemo(
    () => (isBanquet ? items.filter((i) => i.banquetPrice != null) : items),
    [items, isBanquet],
  );
  const displayPrice = (item: MenuItem) =>
    isBanquet && item.banquetPrice != null ? item.banquetPrice : (item.stationPrice ?? item.price);

  const subCategoriesWithItems = useMemo(() => {
    if (activeCategoryId == null) return [];
    return categories
      .filter((c) => c.parentCategoryId === activeCategoryId)
      .filter((c) => sellableItems.some((i) => i.menuCategoryId === c.id));
  }, [categories, sellableItems, activeCategoryId]);

  const ownItems = useMemo(
    () => (activeCategoryId == null ? [] : sellableItems.filter((i) => i.menuCategoryId === activeCategoryId)),
    [sellableItems, activeCategoryId],
  );

  const itemsInCategory = useMemo(() => {
    if (activeSubCategoryId != null) {
      return sellableItems.filter((i) => i.menuCategoryId === activeSubCategoryId);
    }
    return ownItems;
  }, [sellableItems, ownItems, activeSubCategoryId]);

  // Whatever the product pane shows changed: back to its first page, then re-check the ↑ / ↓ buttons.
  useEffect(() => {
    productScrollRef.current?.scrollTo({ top: 0 });
    syncProductScroll();
  }, [activeCategoryId, activeSubCategoryId, productSearch, items, syncProductScroll]);

  useEffect(() => {
    const el = productScrollRef.current;
    if (!el || typeof ResizeObserver === "undefined") return;
    const observer = new ResizeObserver(syncProductScroll);
    observer.observe(el);
    return () => observer.disconnect();
  }, [loading, syncProductScroll]);

  const productSearchTrimmed = productSearch.trim();
  const searchResults = useMemo(() => {
    if (!productSearchTrimmed) return [];
    const term = normalizeSearch(productSearchTrimmed);
    return sellableItems.filter((i) => {
      if (i.hideFromPosSearch) return false;
      const nameMatch = normalizeSearch(i.name).includes(term);
      const codeMatch =
        !i.hideBarcode &&
        ((i.barcode != null && i.barcode === productSearchTrimmed) ||
          (i.weightCode != null && i.weightCode === productSearchTrimmed));
      return nameMatch || codeMatch;
    });
  }, [sellableItems, productSearchTrimmed]);

  const handleBarcodeEnter = (e: React.KeyboardEvent<HTMLInputElement>) => {
    if (e.key !== "Enter") return;
    const exact = sellableItems.find(
      (i) => !i.hideBarcode && (i.barcode === productSearchTrimmed || i.weightCode === productSearchTrimmed),
    );
    if (exact) {
      void handleAddItem(exact.id);
      setProductSearch("");
    }
  };

  const handleAddItem = async (menuItemId: number, quantity = 1) => {
    if (!order || busy || outOfStockIds.has(menuItemId)) return;
    setBusy(true);
    try {
      const updated = await addOrderLine({ orderId: order.id, menuItemId, quantity });
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

  /** "Məhsul kodu zorunlu" — the barcode is the product code; weight-sold items may use their weight code. */
  const lacksProductCode = (item: MenuItem) =>
    branding?.requireProductCode === true &&
    !item.barcode?.trim() &&
    !(isWeightBasedItem(item) && item.weightCode?.trim());

  /** A SET product, or any product of a set category ("Set kateqoriyası"), is added through the set dialog. */
  const isSetProduct = (item: MenuItem) => {
    if (item.isSet) return true;
    const category = categories.find((c) => c.id === item.menuCategoryId);
    if (category?.isSetCategory) return true;
    const parent = category?.parentCategoryId != null ? categories.find((c) => c.id === category.parentCategoryId) : null;
    return parent?.isSetCategory === true;
  };

  /**
   * Adds the SET with the waiter's picks. The server adds the set with its full contents; the components picked
   * differently are then changed ("Pos.EditProductInSale") or taken off ("Pos.DeleteProductInSale"). If that
   * fails, the set is taken back off so the order never keeps a set the waiter did not pick.
   */
  const handleConfirmSet = async (picks: SetPick[]) => {
    if (!order || !setDialogItem || busy) return;
    if (!canAddProduct) {
      toast.error("Sifarişə məhsul əlavə etmək üçün icazəniz yoxdur.");
      return;
    }
    if (order.status === "paid" || order.isBillLocked) {
      toast.error("Hesab bağlıdır — məhsul əlavə etmək olmaz.");
      return;
    }
    if (picks.some((p) => !isSetPickAllowed(p, canEditProduct, canDeleteProduct))) {
      toast.error("Setin tərkibini dəyişmək üçün icazəniz yoxdur.");
      return;
    }
    const knownLineIds = new Set(order.lines.map((l) => l.id));
    setBusy(true);
    let current: OrderDto | null = null;
    let setLineId: number | null = null;
    try {
      current = await addOrderLine({ orderId: order.id, menuItemId: setDialogItem.id, quantity: 1 });
      const setLine = current.lines.find(
        (l) => l.menuItemId === setDialogItem.id && l.parentLineId == null && !knownLineIds.has(l.id),
      );
      setLineId = setLine?.id ?? null;
      const changed = picks.filter((p) => p.quantity !== p.defaultQuantity);
      if (changed.length > 0) {
        if (!setLine) throw new Error("Əlavə olunan set tapılmadı.");
        const children = current.lines.filter((l) => l.parentLineId === setLine.id);
        for (const pick of changed) {
          const child = children.find((l) => l.menuItemId === pick.componentMenuItemId);
          if (!child) continue;
          current =
            pick.quantity === 0
              ? await deleteOrderLine(child.id)
              : await updateOrderLine({ id: child.id, quantity: pick.quantity });
        }
      }
      setOrder(current);
      setSetDialogItem(null);
    } catch (err) {
      const message = err instanceof Error ? err.message : "Set əlavə olunmadı";
      if (current && setLineId != null) {
        try {
          current = await deleteOrderLine(setLineId);
          toast.error(`${message} — set sifarişə əlavə olunmadı.`);
        } catch {
          toast.error(`${message} — set tam tərkibdə əlavə olundu, sətirdən düzəldin.`);
          setSetDialogItem(null);
        }
        setOrder(current);
      } else {
        toast.error(message);
      }
    } finally {
      setBusy(false);
      void loadAvailability(order.restaurantId);
    }
  };

  const handleItemClick = (item: MenuItem) => {
    // A SET: the waiter picks its products first; nothing goes on the order until the dialog is confirmed.
    if (isSetProduct(item)) {
      if (outOfStockIds.has(item.id)) return;
      if (!canAddProduct) {
        toast.error("Sifarişə məhsul əlavə etmək üçün icazəniz yoxdur.");
        return;
      }
      setSetDialogItem(item);
      return;
    }
    // Weigh window switched off: add 1 kg (1000 g) straight away — it can be re-weighed on the line.
    if (isWeightBasedItem(item) && branding?.posShowWeighWindow === false) {
      void handleAddItem(item.id, 1000);
      return;
    }
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

  /**
   * Removing a product that was already sent to the kitchen needs a reason (ləğvlər hesabatı);
   * one still only on the screen is removed straight away. Returns false if the user backed out.
   */
  const removeLineWithReason = async (lineId: number): Promise<boolean> => {
    if (!order) return false;
    const line = order.lines.find((l) => l.id === lineId);
    let cancel;
    if (line?.kitchenPrintedAt) {
      cancel = await askCancelReason(`"${line.menuItemName}" ləğv et`);
      if (!cancel) return false;
    }
    const updated = await deleteOrderLine(lineId, cancel ?? undefined);
    setOrder(updated);
    return true;
  };

  const handleQuantityChange = async (lineId: number, currentQty: number, delta: number) => {
    if (!order || busy) return;
    const newQty = currentQty + delta;
    setBusy(true);
    try {
      if (newQty <= 0) {
        await removeLineWithReason(lineId);
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
      await removeLineWithReason(lineId);
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

  const handleLineDoubleClick = (e: React.MouseEvent, line: OrderLineDto) => {
    if (!order || busy || order.isPaid || order.isBillLocked) return;
    // Quick taps on +/- or other controls must not count as a double-tap on the line itself.
    if ((e.target as HTMLElement).closest("button, input, a")) return;
    // Minute-billed items (PlayStation...) take no note: a tap on them asks "Dayandır" / "Davam et".
    if (line.isTimeBased) return;
    if (!canEditProduct) return;
    setLineNoteLine(line);
    setLineNoteInput(line.note ?? "");
  };

  const priceOptionsFor = (line: OrderLineDto) => {
    const item = items.find((i) => i.id === line.menuItemId);
    if (!item) return [];
    const options: { type: number; label: string; value: number | null }[] = [
      { type: MenuItemPriceType.Price, label: "Satış qiyməti", value: item.price },
      { type: MenuItemPriceType.Station, label: "Dəzgah qiyməti", value: item.stationPrice },
      { type: MenuItemPriceType.Package, label: "Paket qiyməti", value: item.packagePrice },
      { type: MenuItemPriceType.Special1, label: "Xüsusi qiymət 1", value: item.specialPrice1 },
      { type: MenuItemPriceType.Special2, label: "Xüsusi qiymət 2", value: item.specialPrice2 },
      { type: MenuItemPriceType.Special3, label: "Xüsusi qiymət 3", value: item.specialPrice3 },
      { type: MenuItemPriceType.Special4, label: "Xüsusi qiymət 4", value: item.specialPrice4 },
      { type: MenuItemPriceType.Special5, label: "Xüsusi qiymət 5", value: item.specialPrice5 },
    ];
    return options.filter((o): o is { type: number; label: string; value: number } => o.value != null && o.value > 0);
  };

  const handleLineClick = (e: React.MouseEvent, line: OrderLineDto) => {
    if ((e.target as HTMLElement).closest("button, input, a")) return;
    if (!priceMode && !giftMode) {
      setSelectedLineId(line.id);
      const timerRunning = line.isTimeBased && line.timeBasedStartedAt != null && line.timeBasedStoppedAt == null;
      if (timerRunning && canEditProduct && order && !busy && !order.isPaid && !order.isBillLocked) setTimerLine(line);
      return;
    }
    if (!order || busy || order.isPaid || order.isBillLocked) return;
    if (giftMode) {
      if (line.isTimeBased) {
        toast.error("Dəqiqə ilə hesablanan məhsul hədiyyə edilə bilməz.");
        return;
      }
      void handleToggleGift(line.id, line.quantity, !line.isGift).then(() => setGiftMode(false));
      return;
    }
    if (line.isGift) {
      toast.error("Hədiyyə məhsulun qiymətini dəyişmək olmaz.");
      return;
    }
    setPriceLine(line);
  };

  const handlePickPrice = async (priceType: number) => {
    if (!priceLine || busy) return;
    setBusy(true);
    try {
      const updated = await updateOrderLine({ id: priceLine.id, quantity: priceLine.quantity, priceType });
      setOrder(updated);
      setPriceLine(null);
      setPriceMode(false);
    } catch (err) {
      toast.error(err instanceof Error ? err.message : "Qiymət dəyişdirilmədi");
    } finally {
      setBusy(false);
    }
  };

  const handleManualDiscount = async () => {
    if (!order || !manualDiscountKind || manualDiscountBusy) return;
    const value = Number(manualDiscountInput.replace(",", "."));
    if (!Number.isFinite(value) || value <= 0) {
      toast.error("Endirimi düzgün daxil edin");
      return;
    }
    if (manualDiscountKind === "Percent" && value > 100) {
      toast.error("Faiz 100-dən çox ola bilməz");
      return;
    }
    setManualDiscountBusy(true);
    try {
      const amount = await setManualDiscount(order.id, manualDiscountKind, value);
      setOrder(await getOrderById(order.id));
      setManualDiscountKind(null);
      toast.success(`Endirim tətbiq edildi: -${amount.toFixed(2)} ₼`);
    } catch (err) {
      toast.error(err instanceof Error ? err.message : "Endirim tətbiq edilmədi");
    } finally {
      setManualDiscountBusy(false);
    }
  };

  // The lamp: on = this sale goes through the fiscal (tax) register. It switches on only when the
  // branch has an active fiscal device registered (when that cannot be checked, it is allowed).
  const toggleFiscal = async () => {
    if (fiscalOn) {
      setFiscalMode(false);
      return;
    }
    if (order) {
      try {
        const devices = await getFiscalDevices(order.restaurantId);
        if (!devices.some((d) => d.isActive)) {
          toast.error("Bu filial üçün aktiv vergi kassası (fiskal cihaz) qeydiyyatdan keçməyib.");
          return;
        }
      } catch {
        /* no right to list devices — let the cashier switch it on */
      }
    }
    setFiscalMode(true);
  };

  const handleSaveLineNote = async () => {
    if (!lineNoteLine || busy) return;
    setBusy(true);
    try {
      const updated = await updateOrderLine({
        id: lineNoteLine.id,
        quantity: lineNoteLine.quantity,
        note: lineNoteInput.trim() || null,
      });
      setOrder(updated);
      setLineNoteLine(null);
    } catch (err) {
      toast.error(err instanceof Error ? err.message : "Qeyd saxlanmadı");
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

  const openPayDialog = (method: PayMethod | null = null) => {
    if (!order) return;
    const hasRental = order.tableHourlyRate != null;
    if (order.lines.length === 0 && !hasRental) return;
    if (hasRental && order.tableRentalStartedAt != null && order.tableRentalStoppedAt == null) {
      toast.error("Əvvəlcə icarə taymerini dayandırın");
      return;
    }
    setPayMethod(method);
    setPayOpen(true);
  };

  // "Miqdar": type the new count on the number pad; 0 removes the line (with a reason if it reached the kitchen).
  const handleSaveQuantity = async () => {
    if (!qtyLine) return;
    const quantity = Math.floor(Number(qtyInput));
    if (!Number.isFinite(quantity) || quantity < 0) {
      toast.error("Miqdar düzgün deyil");
      return;
    }
    const line = qtyLine;
    setQtyLine(null);
    if (quantity !== line.quantity) await handleQuantityChange(line.id, line.quantity, quantity - line.quantity);
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

  // "Qəbz çap et" is the final print: afterwards the order cannot be edited (only unlocked by a manager).
  const handlePrintBill = async () => {
    if (!order || order.lines.length === 0) return;
    if (!window.confirm("Qəbz çap edildikdən sonra sifarişə düzəliş etmək olmaz. Davam edək?")) return;
    setBusy(true);
    try {
      const locked = await markBillPrinted(order.id, true);
      const r = await getOrderReceipt(order.id, fiscalOn);
      setReceipt(r);
      maybeAutoPrint(false);
      if (locked) await load();
    } catch (err) {
      toast.error(err instanceof Error ? err.message : "Qəbz alına bilmədi");
    } finally {
      setBusy(false);
    }
  };

  // The last payment closed the order: show its receipt and run the "on payment" prints.
  const finishAfterClose = async (paymentId?: number) => {
    if (!order) return;
    try {
      const r = paymentId ? await getPaymentReceipt(order.id, paymentId) : await getOrderReceipt(order.id);
      setFiscalMode(false);
      setReceipt(r);
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
      toast.error(err instanceof Error ? err.message : "Qəbz alına bilmədi");
    }
  };

  // After a cash/card payment in the "Hesab" dialog: either the bill is settled, or part of it
  // was deducted and the rest is still to pay.
  const handleTenderPaid = async (result: { amount: number; orderClosed: boolean; remainingAmount: number }) => {
    if (!order) return;
    if (result.orderClosed) {
      setPayOpen(false);
      await finishAfterClose();
      return;
    }
    // Part of the bill is paid: the dialog closes and the rest stays on the page ("Ödənilib" / "Qalıq"
    // in the totals) until the guest pays it with the "Hesab" button again.
    setPayOpen(false);
    toast.success(`${result.amount.toFixed(2)} ₼ ödənildi. Qalıq: ${result.remainingAmount.toFixed(2)} ₼`);
    try {
      setOrder(await getOrderById(order.id));
    } catch {
      /* refreshed on the next load */
    }
    await refreshPayments(order.id);
  };

  const handleDeleteOrder = async () => {
    if (!order || busy) return;
    const cancel = await askCancelReason("Sifarişi ləğv et");
    if (!cancel) return;
    setBusy(true);
    try {
      await cancelOrder(order.id, cancel);
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
        tables.filter((t) => t.restaurantId === order.restaurantId &&
          t.isActive &&
          !t.isOccupied &&
          t.type !== RestaurantTableType.Delivery &&
          t.type !== RestaurantTableType.TakeAway),
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

  // "Ofisiantı dəyiş" is shown and works by the Pos.ChangeWaiter permission alone — no code.
  const startReassign = () => {
    void openReassignDialog();
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
  const isRentalRunning =
    order.tableHourlyRate != null && order.tableRentalStartedAt != null && order.tableRentalStoppedAt == null;
  const rentalElapsedMs = isRentalRunning ? now - new Date(order.tableRentalStartedAt!).getTime() : 0;
  const rentalLiveTotal = isRentalRunning
    ? (order.tableHourlyRate ?? 0) * (rentalElapsedMs / 3_600_000)
    : order.tableRentalAmount ?? 0;

  const selectedLine = selectedLineId != null ? order.lines.find((l) => l.id === selectedLineId) ?? null : null;
  const linesLocked = isPaid || order.isBillLocked;
  const inKitchenStage = (l: OrderLineDto) => l.status === "InPreparation" || l.status === "Ready" || l.status === "Served";
  const formatLineTime = (iso: string | null) =>
    iso
      ? new Date(iso.endsWith("Z") ? iso : `${iso}Z`).toLocaleTimeString("az-AZ", { hour: "2-digit", minute: "2-digit" })
      : "";

  // After payment totalAmount already includes the service charge; before it, rental is added on top.
  const service = order.serviceChargeAmount ?? 0;
  const grandTotal = isPaid
    ? order.totalAmount
    : order.totalAmount + (isRentalRunning ? rentalLiveTotal : order.tableRentalAmount ?? 0) + service;
  const subtotal = grandTotal + order.discountAmount - service;
  const receivedAmount = isPaid ? order.paidAmount : hasPartPayments ? payments!.paidAmount : null;

  const showProductSearch = searchOpen || productSearchTrimmed !== "" || isStoreMode;
  const toggleProductSearch = () => {
    if (searchOpen && !isStoreMode) {
      setSearchOpen(false);
      setSearchKeyboardOpen(false);
      setProductSearch("");
      return;
    }
    setSearchOpen(true);
    setSearchKeyboardOpen(true);
    window.setTimeout(() => searchInputRef.current?.focus(), 0);
  };

  const pendingKitchenLines = printerGroups.reduce((sum, g) => sum + g.lines.length, 0);
  const lineColumns = "grid grid-cols-[3.25rem_minmax(0,1fr)_4rem_4.5rem_3rem] items-center gap-x-2";

  const renderProduct = (item: MenuItem) => {
    const isOutOfStock = outOfStockIds.has(item.id);
    const isMissingCode = lacksProductCode(item);
    return (
      <button
        key={item.id}
        type="button"
        disabled={busy || isOutOfStock || isMissingCode}
        onClick={() => handleItemClick(item)}
        className={cn(
          "relative flex h-28 select-none flex-col overflow-hidden rounded-xl border-2 border-border bg-card text-left shadow-sm transition-all",
          "hover:border-primary/50 active:scale-95 disabled:opacity-50",
          (isOutOfStock || isMissingCode) && "bg-muted grayscale",
        )}
      >
        {item.imageUrl && (
          // eslint-disable-next-line @next/next/no-img-element
          <img src={item.imageUrl} alt="" className="h-12 w-full shrink-0 object-cover" loading="lazy" />
        )}
        <div className="flex min-h-0 flex-1 flex-col justify-between gap-1 p-2.5">
          <span className={cn("font-semibold leading-tight", item.imageUrl ? "line-clamp-1 text-sm" : "line-clamp-2 text-base")}>
            {item.name}
          </span>
          <span className="text-sm font-bold tabular-nums text-primary">{displayPrice(item).toFixed(2)} ₼</span>
        </div>
        {isWeightBasedItem(item) && (
          <Scale className="absolute right-2 bottom-2.5 h-4 w-4 text-violet-700" aria-label="Çəki ilə satılır" />
        )}
        {isOutOfStock && (
          <span className="absolute right-1 top-1 rounded-full bg-destructive px-1.5 py-0.5 text-[10px] font-semibold text-destructive-foreground">
            Bitib
          </span>
        )}
        {isMissingCode && !isOutOfStock && (
          <span className="absolute right-1 top-1 rounded-full bg-muted-foreground px-1.5 py-0.5 text-[10px] font-semibold text-background">
            Kodsuz
          </span>
        )}
      </button>
    );
  };

  // The selected line's own controls (quantity, timer, weight, discount, hold, price lists, delete).
  const renderLineControls = (line: OrderLineDto, isHeld: boolean, isTimerRunning: boolean) => (
    <div className="mt-2 flex flex-wrap items-center justify-between gap-2 rounded-lg border bg-background p-2 shadow-sm">
      <div className="flex items-center gap-2">
        {line.isTimeBased ? (
          isTimerRunning ? (
            <Button
              size="sm"
              variant="outline"
              className="h-8 gap-1 px-2 text-emerald-700"
              disabled={busy || !canEditProduct}
              onClick={() => setTimerLine(line)}
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
              className="h-8 gap-1 px-2"
              disabled={busy || !canEditProduct}
              onClick={() => void handleStartTimer(line.id)}
            >
              <Play className="h-3 w-3" />
              Başlat
            </Button>
          )
        ) : line.isWeightBased ? (
          canEditProduct && (
            <Button size="sm" variant="outline" className="h-8 gap-1 px-2" disabled={busy} onClick={() => openReweighDialog(line)}>
              <Scale className="h-3.5 w-3.5" />
              Çəkini dəyiş
            </Button>
          )
        ) : (
          <>
            <button
              type="button"
              disabled={busy || !canEditProduct}
              onClick={() => void handleQuantityChange(line.id, line.quantity, -1)}
              className="flex h-8 w-8 items-center justify-center rounded-md border disabled:opacity-50"
              aria-label="Azalt"
            >
              <Minus className="h-3.5 w-3.5" />
            </button>
            <span className="w-7 text-center text-sm font-semibold">{line.quantity}</span>
            <button
              type="button"
              disabled={busy || !canEditProduct}
              onClick={() => void handleQuantityChange(line.id, line.quantity, 1)}
              className="flex h-8 w-8 items-center justify-center rounded-md border disabled:opacity-50"
              aria-label="Artır"
            >
              <Plus className="h-3.5 w-3.5" />
            </button>
          </>
        )}
      </div>

      {priceEditingLineId === line.id ? (
        <div className="flex items-center gap-1">
          <Input
            type="number"
            step="0.01"
            autoFocus
            value={priceInput}
            onChange={(e) => setPriceInput(e.target.value)}
            className="h-8 w-20 text-right text-sm"
          />
          <Button size="sm" className="h-8 px-2" disabled={busy} onClick={() => void handleSavePrice(line.id, line.quantity)}>
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
            className="h-8 w-24 text-right text-sm"
          />
          <Button size="sm" className="h-8 px-2" disabled={busy} onClick={() => void handleSaveDiscount(line.id, line.quantity)}>
            OK
          </Button>
        </div>
      ) : (
        <div className="flex items-center gap-2.5">
          {canEditProduct && !line.isTimeBased && (
            <button
              type="button"
              onClick={() => {
                setLineNoteLine(line);
                setLineNoteInput(line.note ?? "");
              }}
              className={cn("text-muted-foreground hover:text-amber-700", line.note && "text-amber-600")}
              title="Qeyd"
            >
              <StickyNote className="h-4 w-4" />
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
          {canEditProduct && (branding?.posShowHoldButton !== false || isHeld) && !inKitchenStage(line) && (
            <button
              type="button"
              onClick={() => openHoldDialog(line)}
              className={cn("text-muted-foreground hover:text-foreground", isHeld && "text-amber-600 hover:text-amber-700")}
              title="Gözlət"
            >
              <Clock className="h-4 w-4" />
            </button>
          )}
          {canChangePrice && !line.isTimeBased && (
            <button
              type="button"
              onClick={() => startPriceEdit(line.id, line.unitPrice)}
              className="text-muted-foreground hover:text-primary"
              title="Qiyməti dəyiş"
            >
              <Pencil className="h-4 w-4" />
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
                  <Package className="h-4 w-4" />
                </button>
              );
            })()}
          {canChangePrice &&
            !line.isTimeBased &&
            !busy &&
            ([1, 2, 3] as const).map((n) => {
              const menuItem = items.find((i) => i.id === line.menuItemId);
              const price = n === 1 ? menuItem?.specialPrice1 : n === 2 ? menuItem?.specialPrice2 : menuItem?.specialPrice3;
              if (price == null || price === line.unitPrice) return null;
              return (
                <button
                  key={n}
                  type="button"
                  onClick={() => void applyQuickPrice(line.id, line.quantity, price)}
                  className="text-xs font-bold text-muted-foreground hover:text-primary"
                  title={`Qiymət ${n} tətbiq et (${price.toFixed(2)} ₼)`}
                >
                  Q{n}
                </button>
              );
            })}
          {canDeleteProduct && !inKitchenStage(line) && (
            <button
              type="button"
              onClick={() => void handleRemoveLine(line.id)}
              className="text-muted-foreground hover:text-destructive"
              title="Sil"
            >
              <Trash2 className="h-4 w-4" />
            </button>
          )}
        </div>
      )}
    </div>
  );

  return (
    <div className="flex h-full min-h-0 flex-col">
      {reasonDialog}
      <div className="flex min-h-0 flex-1 flex-col md:flex-row">
        {/* Order */}
        <section className="flex min-h-0 w-full shrink-0 flex-col border-b bg-background md:w-[360px] md:border-b-0 md:border-r lg:w-[420px]">
          <div className="flex items-stretch border-b">
            {!isStoreMode && (
              <>
                <InfoCell
                  label={order.isBanquet ? "Banket zalı" : "Zal"}
                  value={hallName ?? (order.isBanquet ? "Banket" : order.isDelivery ? "Çatdırılma" : "—")}
                />
                <InfoCell label="Masa" value={order.tableName ?? "—"} strong />
                {!order.isDelivery && (
                  <InfoCell
                    label="Adam"
                    value={order.guestCount != null ? String(order.guestCount) : "—"}
                    strong
                  />
                )}
              </>
            )}
            <InfoCell label="Çek №" value={order.orderNumber} />
            <InfoCell label="İstifadəçi" value={order.waiterName ?? "—"} grow />
            <button
              type="button"
              onClick={() => setMoreOpen(true)}
              className="relative flex w-12 shrink-0 items-center justify-center text-muted-foreground hover:bg-muted hover:text-foreground"
              title="Əlavə əməliyyatlar"
              aria-label="Əlavə əməliyyatlar"
            >
              <MoreHorizontal className="h-5 w-5" />
              {pendingKitchenLines > 0 && canPrint && !isPaid && (
                <span className="absolute right-1.5 top-2 min-w-4 rounded-full bg-orange-500 px-1 text-[10px] font-bold text-white">
                  {pendingKitchenLines}
                </span>
              )}
            </button>
          </div>

          {/* The customer (and their debt) shows under "Müştəri adı" in the totals. */}
          {(order.discountCode || order.note) && (
            <div className="flex flex-wrap items-center gap-1.5 border-b px-3 py-2">
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
                  className="flex max-w-[200px] items-center gap-1 truncate rounded-full bg-amber-100 px-2 py-0.5 text-xs text-amber-800"
                  title={order.note}
                >
                  <StickyNote className="h-3 w-3 shrink-0" />
                  {order.note}
                </span>
              )}
            </div>
          )}

          {order.isBillLocked && (
            <div className="flex flex-wrap items-center justify-between gap-2 border-b bg-red-50 px-3 py-2 text-sm font-medium text-red-800">
              <span>Hesab verilib — sifarişə dəyişiklik etmək olmaz.</span>
              {canUnlockBill && (
                <Button size="sm" variant="outline" onClick={() => void handleUnlockBill()}>
                  Kilidi aç
                </Button>
              )}
            </div>
          )}
          {order.holdUntilUtc && (
            <div className="flex items-center gap-2 border-b bg-amber-50 px-3 py-2 text-sm font-medium text-amber-800">
              <Clock className="h-4 w-4 shrink-0" />
              Sifariş gözlədədir — mətbəxə göndərilmir.
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
                  <Button size="sm" variant="outline" className="ml-auto gap-1" disabled={busy} onClick={() => void handleStartRental()}>
                    <Play className="h-3.5 w-3.5" />
                    Başlat
                  </Button>
                )
              )}
            </div>
          )}

          {/* Toolbar for the selected line */}
          {!isPaid && (
            <div className="space-y-1.5 border-b px-3 py-2">
              <div className="grid grid-cols-4 gap-1.5">
                <ToolButton
                  icon={Tag}
                  label="Qiymət"
                  active={priceMode}
                  // Banquet prices are fixed: the server refuses a price change on a banquet order.
                  disabled={busy || order.isBillLocked || order.isBanquet || order.lines.length === 0}
                  onClick={() => {
                    setGiftMode(false);
                    if (selectedLine && !priceMode && !selectedLine.isGift && !selectedLine.isTimeBased) {
                      setPriceLine(selectedLine);
                      return;
                    }
                    setPriceMode((v) => !v);
                  }}
                />
                <ToolButton
                  icon={Hash}
                  label="Miqdar"
                  disabled={busy || !selectedLine || linesLocked || !canEditProduct || selectedLine.isTimeBased}
                  onClick={() => {
                    if (!selectedLine) return;
                    if (selectedLine.isWeightBased) {
                      openReweighDialog(selectedLine);
                      return;
                    }
                    setQtyInput(String(selectedLine.quantity));
                    setQtyLine(selectedLine);
                  }}
                />
                <ToolButton
                  icon={Plus}
                  label="+1"
                  disabled={
                    busy || !selectedLine || linesLocked || !canEditProduct || selectedLine.isTimeBased || selectedLine.isWeightBased
                  }
                  onClick={() => selectedLine && void handleQuantityChange(selectedLine.id, selectedLine.quantity, 1)}
                />
                <ToolButton
                  icon={Trash2}
                  label="Silmə"
                  tone="danger"
                  disabled={busy || !selectedLine || linesLocked || !canDeleteProduct || inKitchenStage(selectedLine)}
                  onClick={() => selectedLine && void handleRemoveLine(selectedLine.id)}
                />
              </div>
              {(priceMode || giftMode) && (
                <p className="rounded-md bg-primary/10 px-2 py-1 text-xs font-medium text-primary">
                  {priceMode
                    ? "Qiymətini dəyişəcəyiniz məhsula toxunun"
                    : "Hədiyyə edəcəyiniz (və ya hədiyyədən çıxaracağınız) məhsula toxunun"}
                </p>
              )}
            </div>
          )}

          {/* Lines */}
          <div className={cn(lineColumns, "border-b bg-muted/40 px-3 py-1.5 text-[11px] font-semibold uppercase tracking-wide text-muted-foreground")}>
            <span className="text-center">Miqdar</span>
            <span>Məhsul</span>
            <span className="text-right">Qiymət</span>
            <span className="text-right">Cəmi</span>
            <span className="text-right">Saat</span>
          </div>
          <div className="min-h-32 flex-1 overflow-y-auto">
            {order.lines.length === 0 && (
              <div className="flex flex-col items-center gap-2 py-10 text-center text-muted-foreground">
                <UtensilsCrossed className="h-8 w-8" />
                <p className="text-sm">Sifariş boşdur — sağdan məhsul seçin</p>
              </div>
            )}
            {orderedLines.map((line) => {
              if (line.parentLineId != null) {
                return (
                  <div key={line.id} className={cn(lineColumns, "border-b px-3 py-1 text-xs text-muted-foreground")}>
                    <span className="text-center">x{line.quantity}</span>
                    <span className="truncate pl-2">↳ {line.menuItemName}</span>
                  </div>
                );
              }
              const isSelected = line.id === selectedLineId;
              const isHeld = line.holdUntilUtc != null && new Date(line.holdUntilUtc).getTime() > now;
              const holdRemainingMinutes = isHeld ? Math.ceil((new Date(line.holdUntilUtc!).getTime() - now) / 60000) : 0;
              const isTimerRunning = line.isTimeBased && line.timeBasedStartedAt != null && line.timeBasedStoppedAt == null;
              const timerElapsedMs = isTimerRunning ? now - new Date(line.timeBasedStartedAt!).getTime() : 0;
              const timerLiveTotal = isTimerRunning ? line.unitPrice * (timerElapsedMs / 3_600_000) : line.lineTotal;
              const paidQty = payments?.paidLines.find((p) => p.orderLineId === line.id)?.paidQuantity ?? 0;
              return (
                <div
                  key={line.id}
                  className={cn(
                    "cursor-pointer select-none border-b px-3 py-2 transition-colors",
                    isSelected ? "bg-primary/10 shadow-[inset_3px_0_0] shadow-primary" : "hover:bg-muted/50",
                    (priceMode || giftMode) && "ring-1 ring-inset ring-primary/30",
                  )}
                  onClick={(e) => handleLineClick(e, line)}
                  onDoubleClick={(e) => handleLineDoubleClick(e, line)}
                >
                  <div className={lineColumns}>
                    <span className="text-center text-base font-bold tabular-nums">
                      {line.isTimeBased ? (
                        <Clock className={cn("mx-auto h-4 w-4", isTimerRunning ? "text-emerald-600" : "text-muted-foreground")} />
                      ) : line.isWeightBased ? (
                        <span className="text-sm">
                          {(line.quantity / 1000).toFixed(3)}
                          <span className="block text-[10px] font-medium text-muted-foreground">kq</span>
                        </span>
                      ) : (
                        line.quantity
                      )}
                    </span>
                    <div className="min-w-0">
                      <p className="truncate text-sm font-medium leading-tight" title={line.menuItemName}>
                        {line.menuItemName}
                      </p>
                      <div className="flex flex-wrap items-center gap-1">
                        {line.isGift && (
                          <Badge className="h-4 bg-pink-100 px-1.5 text-[10px] text-pink-800 hover:bg-pink-100">
                            <Gift className="mr-0.5 h-2.5 w-2.5" />
                            Hədiyyə
                          </Badge>
                        )}
                        {!line.isGift && line.discountAmount > 0 && (
                          <Badge className="h-4 bg-blue-100 px-1.5 text-[10px] text-blue-800 hover:bg-blue-100">
                            -{line.discountAmount.toFixed(2)} ₼
                          </Badge>
                        )}
                        {paidQty > 0 && (
                          <span className="text-[11px] font-semibold text-emerald-700">
                            {paidQty >= line.quantity || line.isWeightBased || line.isTimeBased
                              ? "Ödənilib"
                              : `Ödənilib: ${paidQty} / ${line.quantity}`}
                          </span>
                        )}
                      </div>
                    </div>
                    <span className="text-right text-sm tabular-nums text-muted-foreground">
                      {line.unitPrice.toFixed(2)}
                      {line.isTimeBased && <span className="block text-[10px]">/ saat</span>}
                    </span>
                    <span className="text-right text-sm font-semibold tabular-nums">{timerLiveTotal.toFixed(2)}</span>
                    <span className="text-right text-xs tabular-nums text-muted-foreground">{formatLineTime(line.createdAtUtc)}</span>
                  </div>
                  {line.note && (
                    <p className="mt-0.5 flex items-center gap-1 pl-[3.75rem] text-[11px] italic text-amber-700">
                      <StickyNote className="h-3 w-3 shrink-0" />
                      {line.note}
                    </p>
                  )}
                  {isHeld && (
                    <p className="mt-0.5 flex items-center gap-1 pl-[3.75rem] text-[11px] font-medium text-amber-600">
                      <Clock className="h-3 w-3" />
                      Gözlədə — {holdRemainingMinutes} dəq sonra
                    </p>
                  )}
                  {isTimerRunning && (
                    <p className="mt-0.5 flex items-center gap-1 pl-[3.75rem] text-[11px] font-medium text-emerald-600">
                      <Clock className="h-3 w-3" />
                      İşləyir — {formatDuration(timerElapsedMs)}
                    </p>
                  )}
                  {isSelected && !priceMode && !giftMode && !linesLocked && renderLineControls(line, isHeld, isTimerRunning)}
                </div>
              );
            })}
          </div>

          {/* Totals */}
          <div className="shrink-0 space-y-2 border-t bg-muted/20 p-3">
            <dl className="grid grid-cols-2 gap-x-4 gap-y-0.5 text-sm">
              <TotalRow label="Cəm" value={`${subtotal.toFixed(2)} ₼`} />
              <TotalRow label="Endirim" value={order.discountAmount > 0 ? `-${order.discountAmount.toFixed(2)} ₼` : "0.00 ₼"} />
              <TotalRow label="Alınan pul" value={receivedAmount != null ? `${receivedAmount.toFixed(2)} ₼` : "—"} />
              <TotalRow label="Qaytarılan məbləğ" value={isPaid ? `${order.changeAmount.toFixed(2)} ₼` : "—"} />
              {service > 0 && <TotalRow label="Servis haqqı" value={`${service.toFixed(2)} ₼`} />}
              {!isPaid && hasPartPayments && (
                <TotalRow label="Qalıq" value={`${payments!.remainingAmount.toFixed(2)} ₼`} strong />
              )}
              <div className="col-span-2 flex items-center justify-between gap-2 border-t pt-1">
                <dt className="text-muted-foreground">Müştəri adı</dt>
                <dd className="min-w-0 truncate text-right font-semibold" title={order.counterpartyName ?? undefined}>
                  {order.counterpartyName ?? "—"}
                  {order.counterpartyDebtAmount != null && order.counterpartyDebtAmount > 0 && (
                    <span className="ml-1 text-xs font-medium text-destructive">
                      (borc: {order.counterpartyDebtAmount.toFixed(2)} ₼)
                    </span>
                  )}
                </dd>
              </div>
            </dl>
            {!isPaid && (
              <div className="flex gap-1.5">
                {canApplyDiscount && (
                  <>
                    <ToolButton
                      label="₼"
                      className="w-11 flex-none"
                      title="Endirim — manatla"
                      disabled={busy || order.isBillLocked}
                      onClick={() => {
                        setManualDiscountInput("");
                        setManualDiscountKind("Amount");
                      }}
                    />
                    <ToolButton
                      icon={Percent}
                      className="w-11 flex-none"
                      title="Endirim — faizlə"
                      disabled={busy || order.isBillLocked}
                      onClick={() => {
                        setManualDiscountInput("");
                        setManualDiscountKind("Percent");
                      }}
                    />
                    <ToolButton
                      icon={Gift}
                      label="Hədiyyə"
                      active={giftMode}
                      disabled={busy || order.isBillLocked || order.lines.length === 0}
                      onClick={() => {
                        setPriceMode(false);
                        setGiftMode((v) => !v);
                      }}
                    />
                  </>
                )}
                <ToolButton
                  icon={Lightbulb}
                  className="w-11 flex-none"
                  ariaLabel={fiscalOn ? "Vergi kassası işləyir — söndürmək üçün basın" : "Vergi kassasını yandır"}
                  active={fiscalOn}
                  activeClassName="border-amber-500 bg-amber-500 text-white"
                  onClick={() => void toggleFiscal()}
                />
                {branding?.posShowCustomerSelect !== false && (
                  <ToolButton
                    icon={User}
                    label="Müştəri"
                    disabled={busy}
                    onClick={() => void openCounterpartyDialog()}
                  />
                )}
              </div>
            )}
            <div className="flex items-baseline justify-between rounded-xl bg-primary px-4 py-2 text-primary-foreground">
              <span className="text-sm font-semibold uppercase tracking-wide" lang="az">
                Ümumi məbləğ
              </span>
              <span className="text-2xl font-bold tabular-nums">{grandTotal.toFixed(2)} ₼</span>
            </div>
            {isPaid && (
              <p className="text-center text-xs text-muted-foreground">Sifariş ödənilib, düzəlişlər avtomatik saxlanılır.</p>
            )}
          </div>
        </section>

        {/* Products */}
        <section className="flex min-h-0 min-w-0 flex-1 flex-col">
          {showProductSearch && (
            <div className="flex items-center gap-2 border-b bg-background px-3 py-2">
              <Search className="h-4 w-4 shrink-0 text-muted-foreground" />
              <Input
                ref={searchInputRef}
                value={productSearch}
                onChange={(e) => setProductSearch(e.target.value)}
                onKeyDown={(e) => {
                  handleBarcodeEnter(e);
                  if (e.key === "Escape") setSearchKeyboardOpen(false);
                }}
                inputMode={searchKeyboardOpen ? "none" : undefined}
                placeholder="Məhsul axtar və ya barkod skan et..."
                className="h-10 text-base"
              />
              <Button
                type="button"
                variant={searchKeyboardOpen ? "default" : "outline"}
                size="icon"
                onMouseDown={(e) => e.preventDefault()}
                onClick={() => setSearchKeyboardOpen((v) => !v)}
                title="Ekran klaviaturası"
                aria-label="Ekran klaviaturası"
              >
                <Keyboard className="h-4 w-4" />
              </Button>
              {(productSearch || (searchOpen && !isStoreMode)) && (
                <Button
                  type="button"
                  variant="ghost"
                  size="icon"
                  onClick={() => (productSearch ? setProductSearch("") : toggleProductSearch())}
                  title={productSearch ? "Təmizlə" : "Bağla"}
                  aria-label={productSearch ? "Təmizlə" : "Bağla"}
                >
                  <X className="h-4 w-4" />
                </Button>
              )}
            </div>
          )}

          <div ref={productScrollRef} onScroll={syncProductScroll} className="min-h-0 flex-1 overflow-y-auto p-3">
            {productSearchTrimmed ? (
              <>
                <p className="mb-2 text-sm font-semibold text-muted-foreground">
                  “{productSearchTrimmed}” üzrə {searchResults.length} məhsul
                </p>
                <div className="grid grid-cols-2 gap-2.5 sm:grid-cols-3 xl:grid-cols-4 2xl:grid-cols-5">
                  {searchResults.map(renderProduct)}
                  {searchResults.length === 0 && (
                    <p className="col-span-full py-8 text-center text-sm text-muted-foreground">Nəticə tapılmadı</p>
                  )}
                </div>
              </>
            ) : (
              <>
                <div className="mb-2 flex items-center gap-2">
                  {activeSubCategoryId != null ? (
                    <button
                      type="button"
                      onClick={() => setActiveSubCategoryId(null)}
                      className="flex items-center gap-1 rounded-lg px-2 py-1 text-sm font-semibold text-muted-foreground hover:bg-muted hover:text-foreground"
                    >
                      <ArrowLeft className="h-4 w-4" />
                      {categories.find((c) => c.id === activeCategoryId)?.name}
                      <span className="text-foreground">/ {categories.find((c) => c.id === activeSubCategoryId)?.name}</span>
                    </button>
                  ) : (
                    <p className="flex items-center gap-1.5 px-1 text-sm font-semibold text-muted-foreground">
                      <LayoutGrid className="h-4 w-4" />
                      {categories.find((c) => c.id === activeCategoryId)?.name ?? "Məhsullar"}
                    </p>
                  )}
                </div>

                {activeSubCategoryId == null && subCategoriesWithItems.length > 0 && (
                  <div className="mb-2.5 grid grid-cols-2 gap-2.5 sm:grid-cols-3 xl:grid-cols-4 2xl:grid-cols-5">
                    {subCategoriesWithItems.map((sub) => (
                      <button
                        key={sub.id}
                        type="button"
                        onClick={() => setActiveSubCategoryId(sub.id)}
                        className="flex h-20 flex-col items-center justify-center gap-1 rounded-xl border-2 border-primary/30 bg-primary/5 p-2 text-center shadow-sm transition-transform hover:bg-primary/10 active:scale-95"
                      >
                        <span className="text-sm font-semibold leading-tight text-foreground">{sub.name}</span>
                        <span className="text-xs text-muted-foreground">Bölmə</span>
                      </button>
                    ))}
                  </div>
                )}

                <div className="grid grid-cols-2 gap-2.5 sm:grid-cols-3 xl:grid-cols-4 2xl:grid-cols-5">
                  {itemsInCategory.map(renderProduct)}
                  {itemsInCategory.length === 0 && subCategoriesWithItems.length === 0 && (
                    <p className="col-span-full py-8 text-center text-sm text-muted-foreground">Bu kateqoriyada məhsul yoxdur</p>
                  )}
                </div>
              </>
            )}
          </div>

          {showProductSearch && searchKeyboardOpen && (
            <VirtualKeyboard
              value={productSearch}
              onChange={setProductSearch}
              onClose={() => setSearchKeyboardOpen(false)}
              className="m-2 mt-0 shrink-0"
            />
          )}
        </section>

        {/* Categories */}
        <aside className="flex min-h-0 w-full shrink-0 flex-col border-t bg-background md:w-28 md:border-l md:border-t-0 lg:w-40">
          <button
            type="button"
            onClick={() => pageProducts(-1)}
            disabled={!productScroll.up}
            className="hidden h-14 shrink-0 flex-col items-center justify-center border-b text-muted-foreground transition-colors hover:bg-muted hover:text-foreground disabled:opacity-30 md:flex"
            title="Əvvəlki səhifəyə keç"
          >
            <ChevronUp className="h-6 w-6" />
            <span className="text-[10px] font-medium">Əvvəlki səhifə</span>
          </button>
          <div className="flex min-h-0 flex-1 gap-2 overflow-auto p-2 md:flex-col">
            {topLevelCategories.map((cat) => {
              const active = !productSearchTrimmed && cat.id === activeCategoryId;
              return (
                <button
                  key={cat.id}
                  type="button"
                  onClick={() => {
                    setActiveCategoryId(cat.id);
                    setActiveSubCategoryId(null);
                    setProductSearch("");
                  }}
                  style={branding?.categoryFontSize ? { fontSize: `${branding.categoryFontSize}px` } : undefined}
                  className={cn(
                    "flex min-h-14 shrink-0 items-center justify-center rounded-xl border-2 px-2 py-2 text-center text-sm font-semibold leading-tight shadow-sm transition-all active:scale-95",
                    active
                      ? "border-primary bg-primary text-primary-foreground"
                      : "border-border bg-card text-card-foreground hover:border-primary/40",
                  )}
                >
                  {cat.name}
                </button>
              );
            })}
          </div>
          <button
            type="button"
            onClick={() => pageProducts(1)}
            disabled={!productScroll.down}
            className="hidden h-14 shrink-0 flex-col items-center justify-center border-t text-muted-foreground transition-colors hover:bg-muted hover:text-foreground disabled:opacity-30 md:flex"
            title="Növbəti səhifəyə keç"
          >
            <span className="text-[10px] font-medium">Növbəti səhifə</span>
            <ChevronDown className="h-6 w-6" />
          </button>
        </aside>
      </div>

      {/* Action bar */}
      <footer className="flex shrink-0 flex-wrap gap-1.5 border-t bg-background p-2">
        {!isPaid && (
          <BarButton
            label={okBusy ? "Göndərilir…" : "OK"}
            tone="primary"
            className="min-w-[6.5rem] flex-[1.4] text-xl"
            disabled={order.lines.length === 0 || busy || okBusy}
            onClick={() => void handleOk()}
          />
        )}
        {!isPaid && canPrintBill && (
          <BarButton icon={Receipt} label="Müştəri qəbzi" disabled={order.lines.length === 0} onClick={handleCustomerReceipt} />
        )}
        {!isPaid && canPay && branding?.paymentCardEnabled !== false && (
          <BarButton
            icon={CreditCard}
            label="Kart"
            tone="card"
            disabled={order.lines.length === 0 || busy}
            onClick={() => openPayDialog("Card")}
          />
        )}
        {!isPaid && canPay && branding?.paymentCashEnabled !== false && (
          <BarButton
            icon={Banknote}
            label="Nəğd"
            tone="cash"
            disabled={order.lines.length === 0 || busy}
            onClick={() => openPayDialog("Cash")}
          />
        )}
        {!isPaid && canPay && (
          <BarButton icon={Receipt} label="Hesab" disabled={order.lines.length === 0 || busy} onClick={() => openPayDialog()} />
        )}
        <BarButton icon={Search} label="Məhsul axtarışı" active={searchOpen && !isStoreMode} onClick={toggleProductSearch} />
        {!isPaid && !isStoreMode && canChangeWaiter && (
          <BarButton icon={UserCog} label="Ofisiant dəyiş" disabled={busy} onClick={startReassign} />
        )}
        {!isPaid && !isStoreMode && canMoveTable && (
          <BarButton icon={ArrowLeftRight} label="Masa dəyiş" disabled={busy} onClick={() => void openMoveTableDialog()} />
        )}
        {!isPaid && canPrintBill && (
          <BarButton
            icon={Printer}
            title="Qəbz çap et"
            className="max-w-20 flex-[0.7]"
            disabled={order.lines.length === 0 || busy}
            onClick={() => void handlePrintBill()}
          />
        )}
        <BarButton icon={LogOut} label="Çıxış" tone="danger" onClick={() => void handleBack()} />
      </footer>

      {/* "Əlavə" — the order's other actions */}
      <Dialog open={moreOpen} onOpenChange={setMoreOpen}>
        <DialogContent className="sm:max-w-sm">
          <DialogHeader>
            <DialogTitle>Əlavə əməliyyatlar</DialogTitle>
            <DialogDescription>
              {order.tableName} · Çek № {order.orderNumber}
            </DialogDescription>
          </DialogHeader>
          <div className="grid gap-2">
            <Button
              variant="outline"
              className="h-11 justify-start gap-2"
              disabled={busy}
              onClick={() => {
                setMoreOpen(false);
                openNoteDialog();
              }}
            >
              <StickyNote className="h-4 w-4" />
              Sifarişə qeyd
            </Button>
            {!isPaid && canEditProduct && (branding?.posShowHoldButton !== false || order.holdUntilUtc) && (
              <Button
                variant="outline"
                className={cn("h-11 justify-start gap-2", order.holdUntilUtc && "border-amber-300 bg-amber-50 text-amber-700 hover:bg-amber-100")}
                disabled={orderHoldBusy}
                onClick={() => {
                  setMoreOpen(false);
                  void handleToggleOrderHold();
                }}
              >
                <Clock className="h-4 w-4" />
                {order.holdUntilUtc ? "Gözləməni ləğv et" : "Bütöv sifarişi gözlət"}
              </Button>
            )}
            {!isPaid && order.isDelivery && canRedirectUser && (
              <Button
                variant="outline"
                className="h-11 justify-start gap-2"
                disabled={busy}
                onClick={() => {
                  setMoreOpen(false);
                  void openDriverDialog();
                }}
              >
                <UserCog className="h-4 w-4" />
                {order.deliveryDriverName ? `Kuryer: ${order.deliveryDriverName}` : "Kuryer təyin et"}
              </Button>
            )}
            {canPrint && !isPaid && branding?.posMarsEnabled === true && order.lines.some((l) => l.kitchenPrintedAt != null) && (
              <Button
                variant="outline"
                className="h-11 justify-start gap-2 border-orange-300 text-orange-700 hover:bg-orange-50"
                disabled={marsBusy}
                onClick={() => {
                  setMoreOpen(false);
                  void handleMars();
                }}
              >
                <Play className="h-4 w-4" />
                Marş — mətbəx indi hazırlasın
              </Button>
            )}
            {canPrint &&
              !isPaid &&
              printerGroups.map(({ printer, lines }) => (
                <Button
                  key={printer.id}
                  variant="outline"
                  className="h-11 justify-start gap-2"
                  disabled={printingId === printer.id}
                  onClick={() => {
                    setMoreOpen(false);
                    void handlePrintGroup(printer);
                  }}
                >
                  <Printer className="h-4 w-4" />
                  {printer.name}-ə göndər ({lines.length})
                </Button>
              ))}
            {canCancelOrderStatus && canDeleteOrder && (
              <Button
                variant="outline"
                className="h-11 justify-start gap-2 text-destructive"
                disabled={busy}
                onClick={() => {
                  setMoreOpen(false);
                  void handleDeleteOrder();
                }}
              >
                <X className="h-4 w-4" />
                Sifarişi ləğv et
              </Button>
            )}
          </div>
        </DialogContent>
      </Dialog>

      {/* "Miqdar" */}
      <Dialog open={qtyLine !== null} onOpenChange={(o) => !o && setQtyLine(null)}>
        <DialogContent className="sm:max-w-xs">
          <DialogHeader>
            <DialogTitle>Miqdar</DialogTitle>
            <DialogDescription>{qtyLine?.menuItemName}</DialogDescription>
          </DialogHeader>
          <form
            className="space-y-3"
            onSubmit={(e) => {
              e.preventDefault();
              void handleSaveQuantity();
            }}
          >
            <Input
              type="number"
              inputMode="numeric"
              min={0}
              autoFocus
              value={qtyInput}
              onChange={(e) => setQtyInput(e.target.value.replace(/\D/g, ""))}
              className="h-12 text-center text-2xl font-bold"
            />
            <TouchNumpad value={qtyInput} onChange={setQtyInput} />
            <DialogFooter>
              <Button type="button" variant="outline" onClick={() => setQtyLine(null)}>
                İmtina et
              </Button>
              <Button type="submit" disabled={busy || qtyInput === ""}>
                Təsdiqlə
              </Button>
            </DialogFooter>
          </form>
        </DialogContent>
      </Dialog>

      {/* Minute-billed line: stop or keep going */}
      <Dialog open={timerLine !== null} onOpenChange={(o) => !o && setTimerLine(null)}>
        <DialogContent className="sm:max-w-xs">
          <DialogHeader>
            <DialogTitle>{timerLine?.menuItemName}</DialogTitle>
            <DialogDescription>
              {timerLine?.timeBasedStartedAt
                ? `İşləyir — ${formatDuration(now - new Date(timerLine.timeBasedStartedAt).getTime())}`
                : "Vaxt hesablanır"}
            </DialogDescription>
          </DialogHeader>
          <div className="grid grid-cols-2 gap-2">
            <Button
              variant="outline"
              className="h-14 gap-2 text-base text-emerald-700"
              disabled={busy}
              onClick={() => {
                const line = timerLine;
                setTimerLine(null);
                if (line) void handleStopTimer(line.id);
              }}
            >
              <Square className="h-4 w-4" />
              Dayandır
            </Button>
            <Button className="h-14 gap-2 text-base" onClick={() => setTimerLine(null)}>
              <Play className="h-4 w-4" />
              Davam et
            </Button>
          </div>
        </DialogContent>
      </Dialog>

      <NewReservationDialog
        open={reservationOpen}
        onOpenChange={setReservationOpen}
        restaurantId={order.restaurantId}
        onCreated={() => {
          setReservationOpen(false);
          loadTableReservation();
        }}
      />

      {/* "OK" — the order's waiter confirms with their code before the kitchen gets the order */}
      <Dialog open={okPinOpen} onOpenChange={(o) => !o && setOkPinOpen(false)}>
        <DialogContent className="sm:max-w-xs">
          <DialogHeader>
            <DialogTitle>Ofisiant təsdiqi</DialogTitle>
            <DialogDescription>Sifarişi mətbəxə göndərmək üçün ofisiant öz kodunu daxil etsin.</DialogDescription>
          </DialogHeader>
          <form
            className="space-y-3"
            onSubmit={(e) => {
              e.preventDefault();
              if (pinInput.trim()) void handleOk(pinInput.trim());
            }}
          >
            <Input
              type="password"
              inputMode="numeric"
              autoFocus
              value={pinInput}
              onChange={(e) => setPinInput(e.target.value)}
              placeholder="Ofisiant kodu"
            />
            {branding?.touchScreenMode === true && <TouchNumpad value={pinInput} onChange={setPinInput} />}
            <DialogFooter>
              <Button type="button" variant="outline" onClick={() => setOkPinOpen(false)}>
                İmtina et
              </Button>
              <Button type="submit" disabled={!pinInput.trim() || okBusy}>
                Təsdiqlə
              </Button>
            </DialogFooter>
          </form>
        </DialogContent>
      </Dialog>

      {/* Waiter PIN — "Ofisiant təsdiqlə" */}
      <Dialog open={pinPrinter !== null} onOpenChange={(o) => !o && setPinPrinter(null)}>
        <DialogContent className="sm:max-w-xs">
          <DialogHeader>
            <DialogTitle>Ofisiant təsdiqi</DialogTitle>
            <DialogDescription>
              {pinPrinter?.name}-ə göndərmək üçün sifarişin ofisiantı öz kodunu daxil etsin.
            </DialogDescription>
          </DialogHeader>
          <form
            className="space-y-3"
            onSubmit={(e) => {
              e.preventDefault();
              if (pinPrinter && pinInput.trim()) void handlePrintGroup(pinPrinter, pinInput.trim());
            }}
          >
            <Input
              type="password"
              inputMode="numeric"
              autoFocus
              value={pinInput}
              onChange={(e) => setPinInput(e.target.value)}
              placeholder="Ofisiant kodu"
            />
            {branding?.touchScreenMode === true && <TouchNumpad value={pinInput} onChange={setPinInput} />}
            <DialogFooter>
              <Button type="button" variant="outline" onClick={() => setPinPrinter(null)}>
                İmtina et
              </Button>
              <Button type="submit" disabled={!pinInput.trim() || printingId !== null}>
                Təsdiqlə
              </Button>
            </DialogFooter>
          </form>
        </DialogContent>
      </Dialog>

      <SplitBillDialog
        open={splitOpen}
        onOpenChange={setSplitOpen}
        order={order}
        payments={payments}
        loadError={paymentsError}
        onRetry={() => order && void refreshPayments(order.id)}
        cashEnabled={branding?.paymentCashEnabled !== false}
        cardEnabled={branding?.paymentCardEnabled !== false}
        creditEnabled={branding?.paymentCreditEnabled !== false}
        touchScreen={branding?.touchScreenMode === true}
        onPaid={handlePartPaid}
        onPrintPayment={(id) => void printPaymentReceipt(id)}
      />

      <CustomerReceiptsDialog
        open={receiptsOpen}
        onOpenChange={setReceiptsOpen}
        tableName={order.tableName}
        payments={payments}
        onPrintPayment={(id) => void printPaymentReceipt(id)}
        onPrintWhole={() => void printWholeReceipt()}
      />

      <PayDialog
        open={payOpen}
        onOpenChange={setPayOpen}
        order={order}
        payments={payments}
        loadError={paymentsError}
        onRetry={() => order && void refreshPayments(order.id)}
        cashEnabled={branding?.paymentCashEnabled !== false}
        cardEnabled={branding?.paymentCardEnabled !== false}
        creditEnabled={branding?.paymentCreditEnabled !== false}
        touchScreen={branding?.touchScreenMode === true}
        onSplit={() => {
          setPayOpen(false);
          setSplitOpen(true);
        }}
        onPaid={handleTenderPaid}
        initialMethod={payMethod}
      />

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
            <DialogHeader className="sr-only">
              <DialogTitle>Qəbz</DialogTitle>
              <DialogDescription>{receipt?.tableName}</DialogDescription>
            </DialogHeader>
            <div className="space-y-1 text-sm">
              {receipt && (
                <ReceiptPreview receipt={receipt} design={receiptDesign} categoryName={receiptCategoryName} />
              )}
              {order && receipt && (
                <div className="mt-3 flex justify-center">
                  <BarcodeSvg value={receiptBarcodeValue(order.id)} height={40} />
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

      {/* "₼" / "%" — hand-typed order discount */}
      <Dialog open={manualDiscountKind !== null} onOpenChange={(o) => !o && setManualDiscountKind(null)}>
        <DialogContent className="sm:max-w-xs">
          <DialogHeader>
            <DialogTitle>{manualDiscountKind === "Percent" ? "Endirim — faiz" : "Endirim — manat"}</DialogTitle>
            <DialogDescription>
              {manualDiscountKind === "Percent"
                ? "Sifarişin cəmindən neçə faiz endirim olunsun?"
                : "Sifarişin cəmindən neçə manat endirim olunsun?"}
            </DialogDescription>
          </DialogHeader>
          <form
            className="space-y-3"
            onSubmit={(e) => {
              e.preventDefault();
              void handleManualDiscount();
            }}
          >
            <div className="flex items-center gap-2">
              <Input
                type="number"
                step={manualDiscountKind === "Percent" ? "1" : "0.01"}
                min={0}
                autoFocus
                value={manualDiscountInput}
                onChange={(e) => setManualDiscountInput(e.target.value)}
                placeholder={manualDiscountKind === "Percent" ? "10" : "0.00"}
              />
              <span className="text-lg font-semibold">{manualDiscountKind === "Percent" ? "%" : "₼"}</span>
            </div>
            {branding?.touchScreenMode === true && (
              <TouchNumpad value={manualDiscountInput} onChange={setManualDiscountInput} allowDecimal />
            )}
            <DialogFooter>
              <Button type="button" variant="outline" onClick={() => setManualDiscountKind(null)} disabled={manualDiscountBusy}>
                Ləğv et
              </Button>
              <Button type="submit" disabled={manualDiscountBusy || !manualDiscountInput.trim()}>
                Tətbiq et
              </Button>
            </DialogFooter>
          </form>
        </DialogContent>
      </Dialog>

      {/* "Qiymət" — pick one of the product's price lists */}
      <Dialog open={priceLine !== null} onOpenChange={(o) => !o && setPriceLine(null)}>
        <DialogContent className="sm:max-w-sm">
          <DialogHeader>
            <DialogTitle>{priceLine?.menuItemName}</DialogTitle>
            <DialogDescription>Hansı qiymətlə satılsın?</DialogDescription>
          </DialogHeader>
          <div className="space-y-1.5">
            {priceLine &&
              priceOptionsFor(priceLine).map((o) => {
                const current = Math.abs(o.value - priceLine.unitPrice) < 0.005;
                return (
                  <button
                    key={o.type}
                    type="button"
                    disabled={busy}
                    onClick={() => void handlePickPrice(o.type)}
                    className={cn(
                      "flex w-full items-center justify-between rounded-lg border px-3 py-3 text-left text-sm hover:bg-muted disabled:opacity-50",
                      current && "border-primary bg-primary/5",
                    )}
                  >
                    <span>
                      {o.label}
                      {current && <span className="ml-2 text-xs text-primary">(indiki)</span>}
                    </span>
                    <span className="font-semibold">{o.value.toFixed(2)} ₼</span>
                  </button>
                );
              })}
            {priceLine && priceOptionsFor(priceLine).length === 0 && (
              <p className="py-4 text-center text-sm text-muted-foreground">Bu məhsul üçün qiymət siyahısı yoxdur.</p>
            )}
          </div>
        </DialogContent>
      </Dialog>

      {/* Line note (double-tap on a line) */}
      <Dialog open={lineNoteLine !== null} onOpenChange={(o) => !o && setLineNoteLine(null)}>
        <DialogContent className="sm:max-w-sm">
          <DialogHeader>
            <DialogTitle>{lineNoteLine?.menuItemName}</DialogTitle>
            <DialogDescription>Məhsul üçün qeyd (məs: şəkərsiz, acısız).</DialogDescription>
          </DialogHeader>
          <Input
            autoFocus
            value={lineNoteInput}
            onChange={(e) => setLineNoteInput(e.target.value)}
            onKeyDown={(e) => {
              if (e.key === "Enter") void handleSaveLineNote();
            }}
            placeholder="Qeyd"
          />
          <DialogFooter>
            <Button variant="outline" onClick={() => setLineNoteLine(null)} disabled={busy}>
              Ləğv et
            </Button>
            <Button onClick={() => void handleSaveLineNote()} disabled={busy}>
              Saxla
            </Button>
          </DialogFooter>
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

      <SetSelectionDialog
        item={setDialogItem}
        price={setDialogItem ? displayPrice(setDialogItem) : undefined}
        items={items}
        categories={categories}
        busy={busy}
        canChangeCount={canEditProduct}
        canLeaveOut={canDeleteProduct}
        onClose={() => setSetDialogItem(null)}
        onConfirm={(picks) => void handleConfirmSet(picks)}
      />

      <Dialog open={weightDialogItem != null} onOpenChange={(o) => !o && setWeightDialogItem(null)}>
        <DialogContent className="sm:max-w-sm">
          <DialogHeader>
            <DialogTitle>{weightDialogItem?.name}</DialogTitle>
            <DialogDescription>
              Tərəzidə göstərilən çəkini kq olaraq daxil edin
              {weightDialogItem && (
                <> — {displayPrice(weightDialogItem).toFixed(2)} ₼/kq</>
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
          {branding?.touchScreenMode === true && (
            <TouchNumpad value={weightKgInput} onChange={setWeightKgInput} allowDecimal />
          )}
          {weightDialogItem && Number(weightKgInput) > 0 && (
            <p className="text-sm text-muted-foreground">
              Cəm: {(displayPrice(weightDialogItem) * Number(weightKgInput)).toFixed(2)} ₼
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

type IconType = React.ComponentType<{ className?: string }>;

/** One cell of the order header: ZAL · MASA · ÇEK № · İSTİFADƏÇİ. */
function InfoCell({ label, value, strong, grow }: { label: string; value: string; strong?: boolean; grow?: boolean }) {
  return (
    <div className={cn("min-w-0 border-r px-3 py-2 leading-tight", grow ? "flex-1" : "shrink-0")}>
      <p className="text-[10px] font-semibold uppercase tracking-wide text-muted-foreground" lang="az">
        {label}
      </p>
      <p className={cn("truncate", strong ? "text-base font-bold" : "text-sm font-semibold")} title={value}>
        {value}
      </p>
    </div>
  );
}

function TotalRow({ label, value, strong }: { label: string; value: string; strong?: boolean }) {
  return (
    <div className="flex items-center justify-between gap-2">
      <dt className="truncate text-muted-foreground">{label}</dt>
      <dd className={cn("shrink-0 tabular-nums", strong ? "font-bold" : "font-semibold")}>{value}</dd>
    </div>
  );
}

/** Compact button of the order panel (line toolbar, discount row). */
function ToolButton({
  icon: Icon,
  label,
  title,
  ariaLabel,
  active = false,
  activeClassName = "border-primary bg-primary text-primary-foreground",
  tone = "default",
  className,
  disabled,
  onClick,
}: {
  icon?: IconType;
  label?: string;
  title?: string;
  /** Accessible name without a hover tooltip. */
  ariaLabel?: string;
  active?: boolean;
  activeClassName?: string;
  tone?: "default" | "danger";
  className?: string;
  disabled?: boolean;
  onClick: () => void;
}) {
  return (
    <button
      type="button"
      title={title ?? label}
      aria-label={ariaLabel ?? title ?? label}
      aria-pressed={active}
      disabled={disabled}
      onClick={onClick}
      className={cn(
        "flex h-10 min-w-10 flex-1 select-none items-center justify-center gap-1.5 rounded-lg border-2 px-2 text-sm font-semibold transition-all active:scale-95",
        "disabled:pointer-events-none disabled:opacity-40",
        active
          ? activeClassName
          : tone === "danger"
            ? "border-destructive/30 bg-destructive/5 text-destructive hover:bg-destructive/10"
            : "border-border bg-card hover:border-primary/40 hover:bg-muted/50",
        className,
      )}
    >
      {Icon && <Icon className="h-4 w-4 shrink-0" />}
      {label && <span className="truncate">{label}</span>}
    </button>
  );
}

const BAR_TONES = {
  default: "border-border bg-card text-card-foreground hover:border-primary/40 hover:bg-muted/50",
  primary: "border-primary bg-primary text-primary-foreground hover:bg-primary/90",
  card: "border-sky-300 bg-sky-50 text-sky-800 hover:bg-sky-100 dark:border-sky-500/40 dark:bg-sky-500/10 dark:text-sky-300",
  cash: "border-emerald-300 bg-emerald-50 text-emerald-800 hover:bg-emerald-100 dark:border-emerald-500/40 dark:bg-emerald-500/10 dark:text-emerald-300",
  reserve: "border-teal-300 bg-teal-50 text-teal-800 hover:bg-teal-100 dark:border-teal-500/40 dark:bg-teal-500/10 dark:text-teal-300",
  danger: "border-destructive/30 bg-destructive/5 text-destructive hover:bg-destructive/10",
} as const;

/** Button of the bottom action bar. */
function BarButton({
  icon: Icon,
  label,
  title,
  tone = "default",
  active = false,
  className,
  disabled,
  onClick,
}: {
  icon?: IconType;
  label?: string;
  title?: string;
  tone?: keyof typeof BAR_TONES;
  active?: boolean;
  className?: string;
  disabled?: boolean;
  onClick: () => void;
}) {
  return (
    <button
      type="button"
      title={title ?? label}
      aria-label={title ?? label}
      aria-pressed={active}
      disabled={disabled}
      onClick={onClick}
      className={cn(
        "flex h-16 min-w-[4.75rem] flex-1 select-none flex-col items-center justify-center gap-1 rounded-xl border-2 px-1.5 text-center text-xs font-semibold leading-tight shadow-sm transition-all active:scale-95 lg:text-sm",
        "focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring disabled:pointer-events-none disabled:opacity-40",
        active ? BAR_TONES.primary : BAR_TONES[tone],
        className,
      )}
    >
      {Icon && <Icon className="h-5 w-5 shrink-0" />}
      {label && <span lang="az">{label}</span>}
    </button>
  );
}
