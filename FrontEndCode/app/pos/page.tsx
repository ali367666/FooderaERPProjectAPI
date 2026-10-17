"use client";

import { TouchNumpad } from "@/components/pos/touch-numpad";
import { useCallback, useEffect, useRef, useState } from "react";
import { useRouter } from "next/navigation";
import { toast } from "sonner";
import {
  ArrowLeftRight,
  Banknote,
  BarChart3,
  CalendarPlus,
  Lock,
  LogOut,
  Receipt,
  RefreshCw,
  SearchX,
  ShoppingBag,
  StickyNote,
  Truck,
  UserCog,
  Users,
  X,
} from "lucide-react";
import { clearStoredAuth } from "@/lib/auth-client";
import { usePosSearch } from "@/components/pos/pos-search-context";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import {
  Dialog,
  DialogContent,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { cn } from "@/lib/utils";
import { getPosTerminalContext, type PosTerminalContext } from "@/lib/pos-terminal-client";
import { getCurrentEmployeeId } from "@/lib/pos-session";
import {
  getRestaurantTables,
  ensureStoreSaleTable,
  RestaurantTableType,
  type RestaurantTable,
} from "@/lib/services/restaurant-table-service";
import {
  getOrders,
  createOrder,
  getOrderPayments,
  moveOrderTable,
  payOrder,
  reassignOrderWaiter,
  verifyRedirectCode,
  type OrderDto,
  type OrderWorkflowStatus,
} from "@/lib/services/order-service";
import { getEmployees, type Employee } from "@/lib/services/employee-service";
import { getRestaurantSections, type RestaurantSection } from "@/lib/services/restaurant-section-service";
import { getReservations, type ReservationDto } from "@/lib/services/reservation-service";
import { NewReservationDialog } from "@/components/pos/new-reservation-dialog";
import { ReservationActionsDialog } from "@/components/pos/reservation-actions-dialog";
import { isReservationExpired, localIsoDate, reservationEndMs, reservationStartMs } from "@/lib/reservation-rules";
import { useHasPermission, usePermissionSet } from "@/hooks/use-auth-permissions";
import { ANY_POS_REPORT_PERMISSION } from "@/lib/pos-reports";
import {
  getCompanySettingsBranding,
  type CompanySettingsBranding,
} from "@/lib/services/company-settings-service";
import { playPosAlert } from "@/lib/pos-sound-alert";
import { printBillForOrder, printReceiptForOrder } from "@/lib/pos-bill-print";
import { getFiscalMode, setFiscalMode } from "@/lib/pos-fiscal-mode";

type TableWithOrder = RestaurantTable & { activeOrder: OrderDto | null };

// The chosen hall survives opening an order and coming back, for this browser tab.
const HALL_STORAGE_KEY = "posSelectedHall";

function isActiveStatus(status: OrderWorkflowStatus): boolean {
  return status !== "paid" && status !== "cancelled";
}

function statusBg(status: OrderWorkflowStatus | null): string {
  switch (status) {
    case "in_preparation":
      return "bg-[#fb923c]";
    case "ready":
      return "bg-[#34d399]";
    case "served":
      return "bg-[#f472b6]";
    case null:
      return "";
    default:
      return "bg-[#6366f1]";
  }
}

function statusLabel(status: OrderWorkflowStatus): string {
  switch (status) {
    case "open":
      return "Açıq";
    case "in_preparation":
      return "Hazırlanır";
    case "ready":
      return "Hazır";
    case "served":
      return "Verildi";
    default:
      return status;
  }
}

function formatElapsed(openedAt: string, now: number): string {
  const opened = new Date(openedAt).getTime();
  if (!Number.isFinite(opened)) return "";
  const minutes = Math.max(0, Math.floor((now - opened) / 60000));
  if (minutes < 60) return `${minutes} dəq`;
  const hours = Math.floor(minutes / 60);
  const remMinutes = minutes % 60;
  return `${hours} saat ${remMinutes} dəq`;
}

export default function PosTablesPage() {
  const router = useRouter();
  const [tables, setTables] = useState<TableWithOrder[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [creatingTableId, setCreatingTableId] = useState<number | null>(null);
  const [terminal, setTerminal] = useState<PosTerminalContext | null | undefined>(undefined);
  const [sections, setSections] = useState<RestaurantSection[]>([]);
  const canChangeSection = useHasPermission("Pos.ChangeDepartment");
  const canViewAllTables = useHasPermission("Pos.RedirectUser");
  const canPrintBill = useHasPermission("Pos.PrintReceipt");
  // Every action button below shows only for someone who may use it.
  const canQuickSale = useHasPermission("Orders.Create");
  const canMoveTable = useHasPermission("Pos.MoveTable");
  const canChangeWaiter = useHasPermission("Pos.ChangeWaiter");
  const canPay = useHasPermission("Orders.Pay");
  const canReserve = useHasPermission("Reservation.Create");
  const canViewReservations = useHasPermission("Reservation.View");
  const permissionSet = usePermissionSet();
  const canSeeReports = ANY_POS_REPORT_PERMISSION.some((p) => permissionSet.has(p));
  // One-shot modes armed by the header buttons: the next table(s) tapped act instead of opening.
  //   pay         "Ödəniş"        — close that table: paid in cash in full, receipt printed
  //   bill        "Hesab"         — print the customer's pre-check; the table stays open (printing
  //                                  records, and may lock, the bill)
  //   move-pick   "Masa dəyiş"    — step 1: pick the table to move
  //   move-target                 — step 2: pick the empty table it goes to
  //   waiter-pick "Ofisiant dəyiş" — pick the table whose waiter changes (approval: see below)
  //   reserve     "Rezerv"        — pick the table to book; the reservation form opens for it
  const canRedirectUser = useHasPermission("Pos.RedirectUser");
  const [mode, setMode] = useState<"pay" | "bill" | "move-pick" | "move-target" | "waiter-pick" | "reserve" | null>(null);
  const [modeBusy, setModeBusy] = useState(false);
  const [moveSource, setMoveSource] = useState<TableWithOrder | null>(null);
  // "Ofisiant dəyiş" needs Pos.ChangeWaiter to show; whoever lacks Pos.RedirectUser must get a supervisor's
  // code approved first. The verified code rides along with the final request.
  const [codeDialogOpen, setCodeDialogOpen] = useState(false);
  const [codeInput, setCodeInput] = useState("");
  const [codeError, setCodeError] = useState<string | null>(null);
  const [supervisorCode, setSupervisorCode] = useState<string | null>(null);
  const [waiterTable, setWaiterTable] = useState<TableWithOrder | null>(null);
  const [employees, setEmployees] = useState<Employee[]>([]);

  const resetMode = () => {
    setMode(null);
    setMoveSource(null);
    setSupervisorCode(null);
    setWaiterTable(null);
  };
  const [now, setNow] = useState(() => new Date());
  const [branding, setBranding] = useState<CompanySettingsBranding | null>(null);
  const alertedTableIds = useRef<Set<number>>(new Set());
  const [guestCountTable, setGuestCountTable] = useState<TableWithOrder | null>(null);
  const [guestCountInput, setGuestCountInput] = useState("");
  const [currentEmployeeId, setCurrentEmployeeId] = useState<number | null>(null);
  const [todayReservations, setTodayReservations] = useState<ReservationDto[]>([]);
  const [deliveryDialogOpen, setDeliveryDialogOpen] = useState(false);
  const [deliveryAddressInput, setDeliveryAddressInput] = useState("");
  const [deliveryPhoneInput, setDeliveryPhoneInput] = useState("");
  const [deliveryCreating, setDeliveryCreating] = useState(false);
  const [reserveTable, setReserveTable] = useState<TableWithOrder | null>(null);
  const [actionReservation, setActionReservation] = useState<ReservationDto | null>(null);
  const alertedReservationIds = useRef<Set<number>>(new Set());
  const { query: searchQuery, setQuery: setSearchQuery } = usePosSearch();
  const [hallKey, setHallKey] = useState<string>(() => {
    try {
      return sessionStorage.getItem(HALL_STORAGE_KEY) ?? "all";
    } catch {
      return "all";
    }
  });

  const selectHall = (key: string) => {
    setHallKey(key);
    setSearchQuery("");
    try {
      sessionStorage.setItem(HALL_STORAGE_KEY, key);
    } catch {
      // storage unavailable — the choice just isn't remembered
    }
  };

  const handleLogout = () => {
    clearStoredAuth();
    router.replace("/pos-login");
  };

  useEffect(() => {
    void getCurrentEmployeeId().then(setCurrentEmployeeId);
  }, []);

  useEffect(() => {
    const id = window.setInterval(() => setNow(new Date()), 1000);
    return () => window.clearInterval(id);
  }, []);

  useEffect(() => {
    setTerminal(getPosTerminalContext());
  }, []);

  const [brandingLoaded, setBrandingLoaded] = useState(false);
  const [storeModeBusy, setStoreModeBusy] = useState(false);

  useEffect(() => {
    if (!terminal?.companyId) return;
    getCompanySettingsBranding(terminal.companyId)
      .then(setBranding)
      .catch(() => setBranding(null))
      .finally(() => setBrandingLoaded(true));
  }, [terminal]);

  const isStoreMode = branding?.moduleDataSecimi === true;

  const warningMinutes = branding?.tableTimeWarningMinutes ?? 45;

  useEffect(() => {
    const restaurantId = terminal?.restaurantId;
    if (!restaurantId || !isStoreMode) return;
    let cancelled = false;

    const ensureStoreSale = async () => {
      setStoreModeBusy(true);
      try {
        const waiterId = await getCurrentEmployeeId();
        if (!waiterId) {
          toast.error("Bu istifadəçi heç bir işçiyə bağlı deyil. Users səhifəsindən bağlayın.");
          return;
        }

        const [storeTableId, allOrders] = await Promise.all([
          ensureStoreSaleTable(restaurantId),
          getOrders(),
        ]);

        if (cancelled) return;

        const activeOrder = allOrders.find(
          (o) => o.tableId === storeTableId && isActiveStatus(o.status),
        );
        if (activeOrder) {
          router.replace(`/pos/order/${activeOrder.id}`);
          return;
        }

        const order = await createOrder({
          restaurantId,
          tableId: storeTableId,
          waiterId,
          guestCount: null,
        });
        if (!cancelled) router.replace(`/pos/order/${order.id}`);
      } catch (err) {
        if (!cancelled) toast.error(err instanceof Error ? err.message : "Satış ekranı açıla bilmədi");
      } finally {
        if (!cancelled) setStoreModeBusy(false);
      }
    };

    void ensureStoreSale();
    return () => {
      cancelled = true;
    };
  }, [terminal, isStoreMode, router]);

  useEffect(() => {
    const stillOverdueIds = new Set<number>();
    for (const table of tables) {
      const order = table.activeOrder;
      if (!order) continue;
      const elapsedMinutes = Math.floor((now.getTime() - new Date(order.openedAt).getTime()) / 60000);
      if (elapsedMinutes >= warningMinutes) stillOverdueIds.add(table.id);
    }
    const newlyOverdue = [...stillOverdueIds].some((id) => !alertedTableIds.current.has(id));
    if (newlyOverdue && branding?.tableBusyWarning !== false) playPosAlert(branding);
    alertedTableIds.current = stillOverdueIds;
  }, [tables, now, warningMinutes, branding]);

  const load = useCallback(async () => {
    if (terminal === undefined) return;
    if (!terminal) {
      router.replace("/pos-login");
      return;
    }
    setLoading(true);
    setError(null);
    try {
      const [allTables, allOrders] = await Promise.all([getRestaurantTables(), getOrders()]);
      const restaurantTables = allTables.filter(
        (t) => !terminal.restaurantId || t.restaurantId === terminal.restaurantId,
      );
      const merged: TableWithOrder[] = restaurantTables.map((t) => {
        const activeOrder =
          allOrders.find((o) => o.tableId === t.id && isActiveStatus(o.status)) ?? null;
        return { ...t, activeOrder };
      });
      setTables(merged);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Masalar yüklənə bilmədi");
    } finally {
      setLoading(false);
    }
  }, [terminal, router]);

  useEffect(() => {
    void load();
  }, [load]);

  useEffect(() => {
    if (!terminal?.restaurantId) return;
    getRestaurantSections(terminal.restaurantId)
      .then((s) => setSections(s.filter((x) => x.isActive)))
      .catch(() => setSections([]));
  }, [terminal]);

  // Reloaded every minute so a reservation the server cancelled automatically frees its table here too.
  const restaurantIdForReservations = terminal?.restaurantId;
  const loadReservations = useCallback(() => {
    if (!restaurantIdForReservations || !canViewReservations) return;
    getReservations(localIsoDate())
      .then((rows) =>
        setTodayReservations(
          rows.filter(
            (r) =>
              r.restaurantId === restaurantIdForReservations &&
              r.tableId != null &&
              (r.status === "Pending" || r.status === "Confirmed"),
          ),
        ),
      )
      .catch(() => setTodayReservations([]));
  }, [restaurantIdForReservations, canViewReservations]);

  useEffect(() => {
    loadReservations();
    const timer = window.setInterval(loadReservations, 60_000);
    return () => window.clearInterval(timer);
  }, [loadReservations]);

  // The held time of a reservation ran out and the guest never came: sound and a message, once per reservation;
  // its table keeps blinking until someone completes, extends, moves or cancels it.
  useEffect(() => {
    const nowMs = now.getTime();
    for (const r of todayReservations) {
      if (!isReservationExpired(r, nowMs) || alertedReservationIds.current.has(r.id)) continue;
      alertedReservationIds.current.add(r.id);
      toast.warning(`${r.tableName ?? "Masa"}: ${r.guestName} adına rezervasiyanın vaxtı bitdi.`);
      playPosAlert(branding);
    }
  }, [todayReservations, now, branding]);

  /** The reservation that stands for the table on the tables screen: an expired one first, else the soonest. */
  const findTableReservation = (tableId: number): ReservationDto | null => {
    const nowMs = Date.now();
    const mine = todayReservations.filter((r) => r.tableId === tableId);
    return (
      mine.find((r) => isReservationExpired(r, nowMs)) ??
      [...mine].sort((x, y) => reservationStartMs(x) - reservationStartMs(y))[0] ??
      null
    );
  };

  // Admin setting "Rezerv masaya öncədən sifariş açılmasın" (0 = off: only the old warning, 30 min).
  const reservationBlockMinutes = branding?.reservationBlockMinutes ?? 0;

  /**
   * A pending/confirmed reservation counts as an active/imminent conflict from the block window before
   * its start (the admin's choice, 30 min by default) until it ends. Once the party is seated the
   * table is theirs to order on.
   */
  const findConflictingReservation = (tableId: number): ReservationDto | null => {
    const nowMs = Date.now();
    const before = reservationBlockMinutes > 0 ? reservationBlockMinutes : 30;
    for (const r of todayReservations) {
      if (r.tableId !== tableId) continue;
      if (r.status !== "Pending" && r.status !== "Confirmed") continue;
      const start = reservationStartMs(r);
      if (!Number.isFinite(start)) continue;
      const windowStart = start - before * 60_000;
      const windowEnd = reservationEndMs(r);
      if (nowMs >= windowStart && nowMs <= windowEnd) return r;
    }
    return null;
  };

  const createOrderForTable = async (table: TableWithOrder, guestCount?: number) => {
    setCreatingTableId(table.id);
    try {
      const waiterId = await getCurrentEmployeeId();
      if (!waiterId) {
        toast.error("Bu istifadəçi heç bir işçiyə bağlı deyil. Users səhifəsindən bağlayın.");
        return;
      }
      const order = await createOrder({
        restaurantId: table.restaurantId,
        tableId: table.id,
        waiterId,
        guestCount: guestCount ?? null,
      });
      router.push(`/pos/order/${order.id}`);
    } catch (err) {
      toast.error(err instanceof Error ? err.message : "Sifariş yaradıla bilmədi");
    } finally {
      setCreatingTableId(null);
    }
  };

  // "Tez satış" — a take-away sale: a new order on its own virtual table, straight to the
  // order screen. Several can be open at once; they are listed above the tables.
  const [takeAwayCreating, setTakeAwayCreating] = useState(false);

  const handleCreateTakeAwayOrder = async () => {
    if (!terminal?.restaurantId) {
      toast.error("Terminal filiala bağlı deyil.");
      return;
    }
    setTakeAwayCreating(true);
    try {
      const waiterId = await getCurrentEmployeeId();
      if (!waiterId) {
        toast.error("Bu istifadəçi heç bir işçiyə bağlı deyil. Users səhifəsindən bağlayın.");
        return;
      }
      const order = await createOrder({
        restaurantId: terminal.restaurantId,
        waiterId,
        isTakeAway: true,
      });
      router.push(`/pos/order/${order.id}`);
    } catch (err) {
      toast.error(err instanceof Error ? err.message : "Tez satış yaradıla bilmədi");
    } finally {
      setTakeAwayCreating(false);
    }
  };

  const handleCreateDeliveryOrder = async () => {
    if (!terminal?.restaurantId) return;
    setDeliveryCreating(true);
    try {
      const waiterId = await getCurrentEmployeeId();
      if (!waiterId) {
        toast.error("Bu istifadəçi heç bir işçiyə bağlı deyil. Users səhifəsindən bağlayın.");
        return;
      }
      const order = await createOrder({
        restaurantId: terminal.restaurantId,
        waiterId,
        isDelivery: true,
        deliveryAddress: deliveryAddressInput.trim() || null,
        deliveryPhone: deliveryPhoneInput.trim() || null,
      });
      setDeliveryDialogOpen(false);
      setDeliveryAddressInput("");
      setDeliveryPhoneInput("");
      router.push(`/pos/order/${order.id}`);
    } catch (err) {
      toast.error(err instanceof Error ? err.message : "Çatdırılma sifarişi yaradıla bilmədi");
    } finally {
      setDeliveryCreating(false);
    }
  };

  const proceedToOpenTable = (table: TableWithOrder) => {
    if (branding?.askGuestCountOnOpen) {
      setGuestCountInput("");
      setGuestCountTable(table);
      return;
    }
    void createOrderForTable(table);
  };

  const printBillForTable = async (table: TableWithOrder) => {
    const order = table.activeOrder;
    if (!order) {
      toast.error("Bu masada sifariş yoxdur.");
      return;
    }
    const canOverrideOwnership = branding?.singleWaiterMode === true ? false : canViewAllTables;
    if (!canOverrideOwnership && currentEmployeeId != null && order.waiterId !== currentEmployeeId) {
      toast.error("Bu masa başqa ofisiantə aiddir.");
      return;
    }
    if (order.lines.length === 0) {
      toast.error("Bu masanın sifarişi boşdur.");
      return;
    }
    setModeBusy(true);
    try {
      const printerName = await printBillForOrder(order.id, order.restaurantId, branding);
      toast.success(`${table.name}: çek ${printerName}-ə göndərildi`);
      resetMode();
      await load();
    } catch (err) {
      toast.error(err instanceof Error ? err.message : "Çek çıxarıla bilmədi");
    } finally {
      setModeBusy(false);
    }
  };

  // "Ödəniş": one tap closes the table — paid in full, in cash — and prints the final receipt.
  const payTable = async (table: TableWithOrder) => {
    const order = table.activeOrder;
    if (!order) {
      toast.error("Bu masada sifariş yoxdur.");
      return;
    }
    const canOverrideOwnership = branding?.singleWaiterMode === true ? false : canViewAllTables;
    if (!canOverrideOwnership && currentEmployeeId != null && order.waiterId !== currentEmployeeId) {
      toast.error("Bu masa başqa ofisiantə aiddir.");
      return;
    }
    if (order.lines.length === 0) {
      toast.error("Bu masanın sifarişi boşdur.");
      return;
    }
    if (branding?.paymentCashEnabled === false) {
      toast.error("Nağd ödəniş söndürülüb. Ödənişi sifariş ekranından edin.");
      return;
    }
    setModeBusy(true);
    try {
      // Guests may already have paid their own shares ("Hesab") — this settles only what is left.
      const parts = await getOrderPayments(order.id);
      const total =
        (parts.payments.length > 0 ? parts.remainingAmount : order.totalAmount + (order.tableRentalAmount ?? 0)) +
        (order.serviceChargeAmount ?? 0);
      await payOrder(order.id, {
        paymentMethod: "Cash",
        paidAmount: total,
        isFiscal: getFiscalMode(),
      });
      setFiscalMode(false);
    } catch (err) {
      toast.error(err instanceof Error ? err.message : "Ödəniş uğursuz oldu");
      setModeBusy(false);
      return;
    }
    // The payment is done — a printer problem must not look like a failed payment.
    try {
      const printerName = await printReceiptForOrder(order.id, order.restaurantId, branding);
      toast.success(`${table.name}: nağd ödənildi, çek ${printerName}-ə göndərildi`);
    } catch (err) {
      toast.warning(
        `${table.name}: nağd ödənildi, amma çek çıxmadı${err instanceof Error ? ` (${err.message})` : ""}`,
      );
    }
    resetMode();
    await load();
    setModeBusy(false);
  };

  const isOthersOrder = (table: TableWithOrder) => {
    const canOverrideOwnership = branding?.singleWaiterMode === true ? false : canViewAllTables;
    return (
      !canOverrideOwnership &&
      currentEmployeeId != null &&
      table.activeOrder != null &&
      table.activeOrder.waiterId !== currentEmployeeId
    );
  };

  const handleMoveTap = async (table: TableWithOrder) => {
    if (mode === "move-pick") {
      if (!table.activeOrder) {
        toast.error("Bu masada sifariş yoxdur.");
        return;
      }
      if (isOthersOrder(table)) {
        toast.error("Bu masa başqa ofisiantə aiddir.");
        return;
      }
      setMoveSource(table);
      setMode("move-target");
      return;
    }
    // move-target
    if (!moveSource?.activeOrder) return;
    if (table.activeOrder || !table.isActive) {
      toast.error("Boş masa seçin.");
      return;
    }
    setModeBusy(true);
    try {
      await moveOrderTable(moveSource.activeOrder.id, table.id);
      toast.success(`${moveSource.name} → ${table.name}`);
      resetMode();
      await load();
    } catch (err) {
      toast.error(err instanceof Error ? err.message : "Masa dəyişdirilmədi");
    } finally {
      setModeBusy(false);
    }
  };

  const startWaiterChange = () => {
    if (mode === "waiter-pick") {
      resetMode();
      return;
    }
    resetMode();
    if (canRedirectUser) {
      setMode("waiter-pick");
      return;
    }
    setCodeInput("");
    setCodeError(null);
    setCodeDialogOpen(true);
  };

  const submitSupervisorCode = async () => {
    const code = codeInput.trim();
    if (!code) return;
    setModeBusy(true);
    setCodeError(null);
    try {
      await verifyRedirectCode(code);
      setSupervisorCode(code);
      setCodeDialogOpen(false);
      setCodeInput("");
      setMode("waiter-pick");
    } catch (err) {
      setCodeError(err instanceof Error ? err.message : "Kod yanlışdır");
    } finally {
      setModeBusy(false);
    }
  };

  const handleWaiterTap = async (table: TableWithOrder) => {
    if (!table.activeOrder) {
      toast.error("Bu masada sifariş yoxdur.");
      return;
    }
    setWaiterTable(table);
    try {
      setEmployees(await getEmployees());
    } catch {
      setEmployees([]);
    }
  };

  const handlePickWaiter = async (employee: Employee) => {
    const order = waiterTable?.activeOrder;
    if (!order || modeBusy) return;
    setModeBusy(true);
    try {
      await reassignOrderWaiter(order.id, employee.id, supervisorCode);
      toast.success("Ofisiant dəyişdirildi");
      resetMode();
      await load();
    } catch (err) {
      toast.error(err instanceof Error ? err.message : "Ofisiant dəyişdirilmədi");
    } finally {
      setModeBusy(false);
    }
  };

  const openTable = async (table: TableWithOrder) => {
    if (mode) {
      if (modeBusy || waiterTable) return;
      if (mode === "pay") await payTable(table);
      else if (mode === "bill") await printBillForTable(table);
      else if (mode === "waiter-pick") await handleWaiterTap(table);
      else if (mode === "reserve") handleReserveTap(table);
      else await handleMoveTap(table);
      return;
    }
    if (table.activeOrder) {
      const canOverrideOwnership = branding?.singleWaiterMode === true ? false : canViewAllTables;
      if (!canOverrideOwnership && currentEmployeeId != null && table.activeOrder.waiterId !== currentEmployeeId) {
        toast.error("Bu masa başqa ofisiantə aiddir, baxa bilməzsiniz.");
        return;
      }
      router.push(`/pos/order/${table.activeOrder.id}`);
      return;
    }
    if (!terminal) return;

    // A reserved table opens its reservation (complete / extend / move / cancel) instead of a new order.
    // With the "soft warning" setting off and no block window running, it opens an order as before.
    const reservation = findTableReservation(table.id);
    if (reservation) {
      const blocking = reservationBlockMinutes > 0 && findConflictingReservation(table.id) !== null;
      if (blocking || isReservationExpired(reservation, Date.now()) || branding?.tableReservationWarning !== false) {
        setActionReservation(reservation);
        return;
      }
    }

    proceedToOpenTable(table);
  };

  const handleReserveTap = (table: TableWithOrder) => {
    if (!table.isActive) return;
    setReserveTable(table);
  };

  const handleConfirmGuestCount = () => {
    if (!guestCountTable) return;
    const parsed = Number(guestCountInput);
    const guestCount = Number.isFinite(parsed) && parsed > 0 ? Math.floor(parsed) : undefined;
    const table = guestCountTable;
    setGuestCountTable(null);
    void createOrderForTable(table, guestCount);
  };

  if (loading || !brandingLoaded) {
    return <div className="flex h-full items-center justify-center text-muted-foreground">Yüklənir...</div>;
  }

  if (isStoreMode) {
    return (
      <div className="flex h-full items-center justify-center text-muted-foreground">
        {storeModeBusy ? "Satış ekranı açılır..." : "Satış ekranına yönləndirilir..."}
      </div>
    );
  }

  // Tables the signed-in user may see: delivery / take-away virtual tables never show, and without
  // Pos.ChangeDepartment only tables outside every zone are visible.
  const visibleTables = tables.filter(
    (t) =>
      t.isActive &&
      t.type !== RestaurantTableType.Delivery &&
      t.type !== RestaurantTableType.TakeAway &&
      (canChangeSection || t.sectionId == null),
  );
  const isCabinetTable = (t: TableWithOrder) =>
    t.type === RestaurantTableType.Kabinet ||
    sections.find((sec) => sec.id === t.sectionId)?.type === RestaurantTableType.Kabinet;
  const cabinetTables = visibleTables.filter(isCabinetTable);
  const regularTables = visibleTables.filter((t) => !isCabinetTable(t));
  const regularGroups: { key: string; title: string | null; tables: TableWithOrder[] }[] = [];
  const ungrouped = regularTables.filter((t) => t.sectionId == null);
  if (ungrouped.length > 0) regularGroups.push({ key: "none", title: null, tables: ungrouped });
  for (const sec of sections) {
    const inSection = regularTables.filter((t) => t.sectionId === sec.id);
    if (inSection.length > 0) regularGroups.push({ key: `s${sec.id}`, title: sec.name, tables: inSection });
  }
  const cabinetGroup = cabinetTables.length > 0 ? [{ key: "kabinet", title: "Kabinet", tables: cabinetTables }] : [];

  // Halls on the right edge: every zone, the cabinets together, and "Hamısı" (the old all-in-one view).
  const halls = [...regularGroups, ...cabinetGroup].map((g) => ({
    key: g.key,
    name: g.title ?? "Ümumi zal",
    tables: g.tables,
  }));
  if (halls.length > 1) halls.unshift({ key: "all", name: "Hamısı", tables: visibleTables });
  const activeHall = halls.find((h) => h.key === hallKey) ?? halls[0] ?? null;

  // A search looks through every hall the user may see; otherwise the chosen hall is shown.
  const normalizedQuery = searchQuery.trim().toLocaleLowerCase("az");
  const matchesSearch = (t: TableWithOrder) =>
    [t.name, t.activeOrder?.orderNumber, t.activeOrder?.waiterName, t.activeOrder?.note]
      .filter(Boolean)
      .join(" ")
      .toLocaleLowerCase("az")
      .includes(normalizedQuery);
  const shownGroups: { key: string; title: string | null; tables: TableWithOrder[] }[] = normalizedQuery
    ? [{ key: "search", title: null, tables: visibleTables.filter(matchesSearch) }]
    : !activeHall
      ? []
      : activeHall.key === "all"
        ? [...regularGroups, ...cabinetGroup]
        : [{ key: activeHall.key, title: null, tables: activeHall.tables }];

  const toggleMode = (next: "pay" | "bill" | "move-pick" | "reserve") => {
    const active = next === "move-pick" ? mode === "move-pick" || mode === "move-target" : mode === next;
    resetMode();
    if (!active) setMode(next);
  };

  const renderTableTile = (table: TableWithOrder) => {
    const occupied = table.activeOrder !== null;
    const status = table.activeOrder?.status ?? null;
    const isCreating = creatingTableId === table.id;

    const order = table.activeOrder;
    const elapsedMinutes = order ? Math.floor((now.getTime() - new Date(order.openedAt).getTime()) / 60000) : 0;
    const isOverdue = branding?.tableBusyWarning !== false && occupied && elapsedMinutes >= warningMinutes;
    const canOverrideOwnership = branding?.singleWaiterMode === true ? false : canViewAllTables;
    const isOtherWaiterTable =
      occupied && !canOverrideOwnership && currentEmployeeId != null && order!.waiterId !== currentEmployeeId;
    const reserved = !occupied ? findTableReservation(table.id) : null;
    const reservationExpired = reserved !== null && isReservationExpired(reserved, now.getTime());

    return (
      <button
        key={table.id}
        type="button"
        onClick={() => void openTable(table)}
        disabled={!table.isActive || isCreating}
        className={cn(
          "relative flex h-32 flex-col items-center justify-center gap-0.5 rounded-xl border-2 p-1.5 transition-all",
          "select-none active:scale-95",
          occupied
            ? "border-transparent text-white shadow-md " + statusBg(status)
            : "border-border bg-card text-card-foreground hover:border-primary/40",
          !table.isActive && "cursor-not-allowed opacity-40",
          isOverdue && "ring-2 ring-red-500 ring-offset-1",
          reserved && !reservationExpired && "border-amber-400 bg-amber-50 dark:bg-amber-950/30",
          reservationExpired && "animate-pulse border-red-500 bg-red-100 ring-2 ring-red-500 dark:bg-red-950/40",
          isOtherWaiterTable && "opacity-60",
        )}
      >
        {isOtherWaiterTable && <Lock className="absolute right-1.5 top-1.5 h-3.5 w-3.5 text-white/90" />}
        {table.note && branding?.tableShowNote !== false && (
          <span className="absolute left-1.5 top-1.5" title={table.note}>
            <StickyNote className={cn("h-3.5 w-3.5", occupied ? "text-white/90" : "text-amber-600")} />
          </span>
        )}
        <span className="text-lg font-bold leading-none">{table.name}</span>
        <span className={cn("flex items-center gap-1 text-xs", occupied ? "text-white/80" : "text-muted-foreground")}>
          <Users className="h-3 w-3" />
          {table.capacity}
        </span>
        {occupied ? (
          <>
            <span className="text-xs font-medium text-white/90">{statusLabel(status!)}</span>
            {order?.waiterName && branding?.tableShowWaiter !== false && (
              <span className="max-w-full truncate text-[11px] text-white/80">{order.waiterName}</span>
            )}
            {order?.guestCount != null && <span className="text-[11px] text-white/80">{order.guestCount} nəfər</span>}
            {branding?.tableShowTime !== false && (
              <span className="text-[11px] text-white/80">{formatElapsed(order!.openedAt, now.getTime())}</span>
            )}
            {branding?.tableShowAmount !== false && (
              <span className="text-xs font-semibold text-white">{order!.totalAmount.toFixed(2)} ₼</span>
            )}
            {order?.note && branding?.tableShowNote !== false && (
              <span className="max-w-full truncate text-[10px] italic text-white/70">{order.note}</span>
            )}
          </>
        ) : (
          <>
            <span
              className={cn(
                "mt-0.5 text-xs",
                reservationExpired ? "font-bold text-red-600" : reserved ? "font-semibold text-amber-600" : "text-muted-foreground",
              )}
            >
              {isCreating
                ? "..."
                : reservationExpired
                  ? "Rezerv vaxtı bitdi"
                  : reserved
                    ? `Rezerv · ${reserved.reservationTime}`
                    : "Boş"}
            </span>
            {reserved && (
              <span className="max-w-full truncate text-[11px] text-muted-foreground">
                {reserved.guestName} · {reserved.guestCount} nəfər
              </span>
            )}
          </>
        )}
      </button>
    );
  };

  return (
    <div className="flex h-full min-h-0 flex-col">
      <div className="flex min-h-0 flex-1 gap-3 p-3 sm:gap-4 sm:p-4">
        {/* Padding keeps the overdue ring of edge tiles from being clipped by the scroll box. */}
        <div className="-m-1 min-w-0 flex-1 space-y-4 overflow-y-auto p-1">
          {mode && (
            <div className="sticky top-0 z-10 flex items-center justify-between gap-3 rounded-xl border-2 border-primary/40 bg-primary/10 px-4 py-2.5 text-sm font-medium backdrop-blur">
              <span>
                {modeBusy
                  ? "Gözləyin..."
                  : mode === "pay"
                    ? "Nağd ödəniləcək masaya toxunun — masa bağlanacaq və çek çıxacaq"
                    : mode === "bill"
                    ? "Hesabını (ön çek) çıxarmaq istədiyiniz masaya toxunun — masa bağlanmır"
                    : mode === "reserve"
                      ? "Rezerv etmək istədiyiniz masaya toxunun"
                      : mode === "move-pick"
                      ? "Köçürmək istədiyiniz masaya toxunun"
                      : mode === "move-target"
                        ? `${moveSource?.name ?? "Masa"} hansı boş masaya köçürülsün?`
                        : "Ofisiantı dəyişəcəyiniz masaya toxunun"}
              </span>
              <Button size="sm" variant="outline" disabled={modeBusy} onClick={resetMode}>
                <X className="h-4 w-4" />
                Ləğv et
              </Button>
            </div>
          )}

          {(() => {
            const takeAwayOrders = tables.filter(
              (t) => t.type === RestaurantTableType.TakeAway && t.activeOrder && isActiveStatus(t.activeOrder.status),
            );
            if (takeAwayOrders.length === 0) return null;
            return (
              <div className="space-y-2">
                <p className="text-sm font-semibold text-muted-foreground">Aktiv tez satışlar</p>
                <div className="grid grid-cols-2 gap-2 sm:grid-cols-3 md:grid-cols-4 xl:grid-cols-6">
                  {takeAwayOrders.map((t) => (
                    <button
                      key={t.id}
                      type="button"
                      disabled={mode !== null}
                      onClick={() => router.push(`/pos/order/${t.activeOrder!.id}`)}
                      className="rounded-xl border bg-card p-3 text-left text-sm shadow-sm hover:bg-muted/50 disabled:opacity-50"
                    >
                      <p className="font-medium">{t.activeOrder!.orderNumber}</p>
                      <p className="text-xs text-muted-foreground">
                        {t.activeOrder!.lines.length} məhsul · {t.activeOrder!.totalAmount.toFixed(2)} ₼
                      </p>
                    </button>
                  ))}
                </div>
              </div>
            );
          })()}

          {branding?.modulePaket === true &&
            (() => {
              const deliveryOrders = tables.filter((t) => t.activeOrder?.isDelivery);
              if (deliveryOrders.length === 0) return null;
              return (
                <div className="space-y-2">
                  <p className="text-sm font-semibold text-muted-foreground">Aktiv çatdırılmalar</p>
                  <div className="grid grid-cols-1 gap-2 sm:grid-cols-2 md:grid-cols-3">
                    {deliveryOrders.map((t) => (
                      <button
                        key={t.id}
                        type="button"
                        onClick={() => router.push(`/pos/order/${t.activeOrder!.id}`)}
                        className="rounded-xl border bg-card p-3 text-left text-sm shadow-sm hover:bg-muted/50"
                      >
                        <p className="font-medium">{t.activeOrder!.orderNumber}</p>
                        {t.activeOrder!.deliveryPhone && (
                          <p className="text-xs text-muted-foreground">{t.activeOrder!.deliveryPhone}</p>
                        )}
                        {t.activeOrder!.deliveryAddress && (
                          <p className="truncate text-xs text-muted-foreground">{t.activeOrder!.deliveryAddress}</p>
                        )}
                        <p className="text-xs text-muted-foreground">
                          {t.activeOrder!.deliveryDriverName
                            ? `Kuryer: ${t.activeOrder!.deliveryDriverName}`
                            : "Kuryer təyin edilməyib"}
                        </p>
                      </button>
                    ))}
                  </div>
                </div>
              );
            })()}

          {error && (
            <div className="rounded-md border border-destructive/30 bg-destructive/10 px-4 py-3 text-sm text-destructive">
              {error}
            </div>
          )}

          {tables.length === 0 && !error && (
            <p className="text-sm text-muted-foreground">Bu filial üçün masa tapılmadı.</p>
          )}

          {normalizedQuery && (
            <div className="flex items-center justify-between gap-3">
              <p className="text-sm font-semibold text-muted-foreground">
                “{searchQuery.trim()}” üzrə {shownGroups[0]?.tables.length ?? 0} masa
              </p>
              <Button size="sm" variant="ghost" onClick={() => setSearchQuery("")}>
                <X className="h-4 w-4" />
                Axtarışı təmizlə
              </Button>
            </div>
          )}
          {normalizedQuery && shownGroups[0]?.tables.length === 0 && (
            <div className="flex flex-col items-center gap-2 py-12 text-center text-muted-foreground">
              <SearchX className="h-10 w-10" />
              <p className="text-sm">Axtarışa uyğun masa tapılmadı.</p>
            </div>
          )}

          {shownGroups
            .filter((group) => group.tables.length > 0)
            .map((group) => (
              <div key={group.key}>
                {group.title && (
                  <p lang="az" className="mb-2 text-sm font-semibold uppercase tracking-wide text-muted-foreground">
                    {group.title}
                  </p>
                )}
                <div className="grid grid-cols-3 gap-3 sm:grid-cols-4 md:grid-cols-5 lg:grid-cols-6 xl:grid-cols-7 2xl:grid-cols-8">
                  {group.tables.map((table) => renderTableTile(table))}
                </div>
              </div>
            ))}
        </div>

        {/* Halls on the right edge */}
        <aside className="flex w-28 shrink-0 flex-col gap-2 overflow-y-auto sm:w-40 lg:w-44">
          {halls.map((hall) => {
            const selected = !normalizedQuery && hall.key === activeHall?.key;
            const busy = hall.tables.filter((t) => t.activeOrder !== null).length;
            return (
              <button
                key={hall.key}
                type="button"
                onClick={() => selectHall(hall.key)}
                className={cn(
                  "flex min-h-16 flex-col items-center justify-center gap-0.5 rounded-xl border-2 px-2 py-2 text-center shadow-sm transition-all active:scale-95",
                  selected
                    ? "border-primary bg-primary text-primary-foreground"
                    : "border-border bg-card text-card-foreground hover:border-primary/40",
                )}
              >
                {/* lang="az" so upper case keeps the dotted İ (KABİNET). */}
                <span lang="az" className="line-clamp-2 text-sm font-bold uppercase leading-tight tracking-wide sm:text-base">
                  {hall.name}
                </span>
                <span className={cn("text-xs", selected ? "text-primary-foreground/80" : "text-muted-foreground")}>
                  {busy} / {hall.tables.length} dolu
                </span>
              </button>
            );
          })}
          <Button variant="outline" className="mt-auto h-11 shrink-0" onClick={() => void load()}>
            <RefreshCw className="h-4 w-4" />
            <span className="hidden sm:inline">Yenilə</span>
          </Button>
        </aside>
      </div>

      {/* Action bar */}
      <footer className="flex shrink-0 flex-wrap gap-2 border-t bg-background/95 p-2 backdrop-blur sm:p-3">
        {canSeeReports && (
          <ActionButton icon={BarChart3} label="Hesabat" disabled={modeBusy} onClick={() => router.push("/pos/reports")} />
        )}
        {canPay && (
          <ActionButton icon={Banknote} label="Ödəniş" active={mode === "pay"} disabled={modeBusy} onClick={() => toggleMode("pay")} />
        )}
        {canPrintBill && (
          <ActionButton icon={Receipt} label="Hesab" active={mode === "bill"} disabled={modeBusy} onClick={() => toggleMode("bill")} />
        )}
        {canReserve && (
          <ActionButton
            icon={CalendarPlus}
            label="Rezerv"
            active={mode === "reserve"}
            disabled={modeBusy}
            onClick={() => toggleMode("reserve")}
          />
        )}
        {canMoveTable && (
          <ActionButton
            icon={ArrowLeftRight}
            label="Masa dəyiş"
            active={mode === "move-pick" || mode === "move-target"}
            disabled={modeBusy}
            onClick={() => toggleMode("move-pick")}
          />
        )}
        {canChangeWaiter && (
          <ActionButton
            icon={UserCog}
            label="Ofisiant dəyiş"
            active={mode === "waiter-pick"}
            disabled={modeBusy}
            onClick={startWaiterChange}
          />
        )}
        {canQuickSale && (
          <ActionButton
            icon={ShoppingBag}
            label={takeAwayCreating ? "Açılır..." : "Tez satış"}
            disabled={takeAwayCreating || modeBusy}
            onClick={() => void handleCreateTakeAwayOrder()}
          />
        )}
        {branding?.modulePaket === true && (
          <ActionButton icon={Truck} label="Çatdırılma" onClick={() => setDeliveryDialogOpen(true)} />
        )}
        <ActionButton icon={LogOut} label="Çıxış" tone="danger" onClick={handleLogout} />
      </footer>

      {/* "Ofisiant dəyiş" — supervisor code */}
      <Dialog
        open={codeDialogOpen}
        onOpenChange={(o) => {
          if (!o) {
            setCodeDialogOpen(false);
            setCodeInput("");
            setCodeError(null);
          }
        }}
      >
        <DialogContent className="sm:max-w-sm">
          <DialogHeader>
            <DialogTitle>Ofisiant dəyiş</DialogTitle>
          </DialogHeader>
          <form
            className="space-y-3"
            onSubmit={(e) => {
              e.preventDefault();
              void submitSupervisorCode();
            }}
          >
            <p className="text-sm text-muted-foreground">
              Bu əməliyyat üçün icazəsi olan şəxsin kodunu daxil edin.
            </p>
            {codeError && <p className="text-sm text-destructive">{codeError}</p>}
            <Input
              type="password"
              inputMode="numeric"
              autoComplete="off"
              autoFocus
              value={codeInput}
              onChange={(e) => setCodeInput(e.target.value.replace(/\D/g, ""))}
              placeholder="Kod"
            />
            <DialogFooter>
              <Button type="button" variant="outline" onClick={() => setCodeDialogOpen(false)} disabled={modeBusy}>
                Ləğv et
              </Button>
              <Button type="submit" disabled={modeBusy || !codeInput.trim()}>
                {modeBusy ? "Yoxlanılır…" : "Təsdiqlə"}
              </Button>
            </DialogFooter>
          </form>
        </DialogContent>
      </Dialog>

      {/* "Ofisiant dəyiş" — pick the new waiter */}
      <Dialog open={waiterTable !== null} onOpenChange={(o) => !o && setWaiterTable(null)}>
        <DialogContent className="sm:max-w-sm">
          <DialogHeader>
            <DialogTitle>{waiterTable?.name}: yeni ofisiant</DialogTitle>
          </DialogHeader>
          <div className="max-h-64 space-y-1 overflow-y-auto">
            {employees.map((e) => (
              <button
                key={e.id}
                type="button"
                disabled={modeBusy}
                onClick={() => void handlePickWaiter(e)}
                className="flex w-full items-center rounded-md border px-3 py-2 text-left text-sm hover:bg-muted disabled:opacity-50"
              >
                {e.fullName?.trim() || `${e.firstName} ${e.lastName}`.trim() || `Employee #${e.id}`}
              </button>
            ))}
            {employees.length === 0 && (
              <p className="py-4 text-center text-sm text-muted-foreground">İşçi tapılmadı</p>
            )}
          </div>
        </DialogContent>
      </Dialog>

      <Dialog open={guestCountTable !== null} onOpenChange={(open) => !open && setGuestCountTable(null)}>
        <DialogContent className="sm:max-w-xs">
          <DialogHeader>
            <DialogTitle>Neçə nəfərsiniz?</DialogTitle>
          </DialogHeader>
          <div className="space-y-2">
            <Label htmlFor="guest-count-input">Qonaq sayı</Label>
            <Input
              id="guest-count-input"
              type="number"
              min={1}
              autoFocus
              value={guestCountInput}
              onChange={(e) => setGuestCountInput(e.target.value)}
              onKeyDown={(e) => {
                if (e.key === "Enter") handleConfirmGuestCount();
              }}
            />
            {branding?.touchScreenMode === true && (
              <TouchNumpad value={guestCountInput} onChange={setGuestCountInput} />
            )}
          </div>
          <DialogFooter>
            <Button variant="outline" onClick={() => setGuestCountTable(null)}>
              Ləğv et
            </Button>
            <Button onClick={handleConfirmGuestCount}>Təsdiqlə</Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      <NewReservationDialog
        open={reserveTable !== null}
        onOpenChange={(o) => {
          if (!o) {
            setReserveTable(null);
            resetMode();
          }
        }}
        restaurantId={terminal?.restaurantId ?? 0}
        initialTableId={reserveTable?.id ?? null}
        onCreated={loadReservations}
      />

      <ReservationActionsDialog
        reservation={actionReservation}
        tables={visibleTables.filter((t) => t.restaurantId === actionReservation?.restaurantId)}
        onClose={() => setActionReservation(null)}
        onChanged={loadReservations}
        onCompleted={(done) => {
          const table = tables.find((t) => t.id === done.tableId);
          if (table && !table.activeOrder) proceedToOpenTable(table);
        }}
      />

      <Dialog open={deliveryDialogOpen} onOpenChange={(o) => !o && setDeliveryDialogOpen(false)}>
        <DialogContent className="sm:max-w-sm">
          <DialogHeader>
            <DialogTitle>Çatdırılma sifarişi</DialogTitle>
          </DialogHeader>
          <div className="space-y-3">
            <div className="space-y-1.5">
              <Label htmlFor="delivery-phone">Telefon</Label>
              <Input
                id="delivery-phone"
                value={deliveryPhoneInput}
                onChange={(e) => setDeliveryPhoneInput(e.target.value)}
                placeholder="+994 XX XXX XX XX"
              />
            </div>
            <div className="space-y-1.5">
              <Label htmlFor="delivery-address">Ünvan</Label>
              <Input
                id="delivery-address"
                value={deliveryAddressInput}
                onChange={(e) => setDeliveryAddressInput(e.target.value)}
                placeholder="Ünvan"
              />
            </div>
          </div>
          <DialogFooter>
            <Button variant="outline" onClick={() => setDeliveryDialogOpen(false)} disabled={deliveryCreating}>
              Ləğv et
            </Button>
            <Button onClick={() => void handleCreateDeliveryOrder()} disabled={deliveryCreating}>
              {deliveryCreating ? "Yaradılır..." : "Sifarişi başlat"}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}

function ActionButton({
  icon: Icon,
  label,
  active = false,
  tone = "default",
  disabled,
  onClick,
}: {
  icon: React.ComponentType<{ className?: string }>;
  label: string;
  active?: boolean;
  tone?: "default" | "danger";
  disabled?: boolean;
  onClick: () => void;
}) {
  return (
    <button
      type="button"
      disabled={disabled}
      onClick={onClick}
      aria-pressed={active}
      className={cn(
        "flex h-16 min-w-[7.5rem] flex-1 select-none flex-col items-center justify-center gap-1 rounded-xl border-2 px-3 text-sm font-semibold shadow-sm transition-all active:scale-95",
        "focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring disabled:pointer-events-none disabled:opacity-50",
        active
          ? "border-primary bg-primary text-primary-foreground shadow-md"
          : tone === "danger"
            ? "border-destructive/30 bg-destructive/5 text-destructive hover:bg-destructive/10"
            : "border-border bg-card text-card-foreground hover:border-primary/40 hover:bg-muted/50",
      )}
    >
      <Icon className="h-5 w-5" />
      <span className="leading-none">{label}</span>
    </button>
  );
}
