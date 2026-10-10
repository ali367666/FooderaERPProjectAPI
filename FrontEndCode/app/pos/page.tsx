"use client";

import { TouchNumpad } from "@/components/pos/touch-numpad";
import { useCallback, useEffect, useRef, useState } from "react";
import { useRouter } from "next/navigation";
import { toast } from "sonner";
import { ArrowLeftRight, Banknote, BarChart3, Lock, Receipt, RefreshCw, ShoppingBag, StickyNote, UserCog, Users } from "lucide-react";
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
  const permissionSet = usePermissionSet();
  const canSeeReports = ANY_POS_REPORT_PERMISSION.some((p) => permissionSet.has(p));
  // One-shot modes armed by the header buttons: the next table(s) tapped act instead of opening.
  //   pay         "Ödəniş"        — close that table: paid in cash in full, receipt printed
  //   bill        "Hesab"         — print the customer's pre-check; the table stays open (printing
  //                                  records, and may lock, the bill)
  //   move-pick   "Masa dəyiş"    — step 1: pick the table to move
  //   move-target                 — step 2: pick the empty table it goes to
  //   waiter-pick "Ofisiant dəyiş" — pick the table whose waiter changes (approval: see below)
  const canRedirectUser = useHasPermission("Pos.RedirectUser");
  const [mode, setMode] = useState<"pay" | "bill" | "move-pick" | "move-target" | "waiter-pick" | null>(null);
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
  const [reservationWarning, setReservationWarning] = useState<{
    table: TableWithOrder;
    reservation: ReservationDto;
  } | null>(null);

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
  useEffect(() => {
    if (!terminal?.restaurantId) return;
    const loadReservations = () => {
      const todayIso = new Date().toISOString().slice(0, 10);
      getReservations(todayIso)
        .then((rows) =>
          setTodayReservations(
            rows.filter(
              (r) =>
                r.restaurantId === terminal.restaurantId &&
                r.tableId != null &&
                r.status !== "Cancelled" &&
                r.status !== "NoShow" &&
                r.status !== "Completed",
            ),
          ),
        )
        .catch(() => setTodayReservations([]));
    };
    loadReservations();
    const timer = window.setInterval(loadReservations, 60_000);
    return () => window.clearInterval(timer);
  }, [terminal]);

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
      const start = new Date(`${r.reservationDate.slice(0, 10)}T${r.reservationTime}`).getTime();
      if (!Number.isFinite(start)) continue;
      const windowStart = start - before * 60_000;
      const windowEnd = start + r.durationMinutes * 60_000;
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

    // With a block window the reserved table simply takes no order; without one, the old soft warning.
    if (reservationBlockMinutes > 0) {
      const blocking = findConflictingReservation(table.id);
      if (blocking) {
        toast.error(
          `Bu masa ${blocking.reservationTime} üçün ${blocking.guestName} adına rezerv olunub — sifariş açmaq olmaz.`,
        );
        return;
      }
    }

    const conflict = branding?.tableReservationWarning === false ? null : findConflictingReservation(table.id);
    if (conflict) {
      setReservationWarning({ table, reservation: conflict });
      return;
    }

    proceedToOpenTable(table);
  };

  const handleConfirmReservationWarning = () => {
    if (!reservationWarning) return;
    const { table } = reservationWarning;
    setReservationWarning(null);
    proceedToOpenTable(table);
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

  const renderTableTile = (table: TableWithOrder, wide = false) => {
    const occupied = table.activeOrder !== null;
    const status = table.activeOrder?.status ?? null;
    const isCreating = creatingTableId === table.id;

    const order = table.activeOrder;
    const elapsedMinutes = order ? Math.floor((now.getTime() - new Date(order.openedAt).getTime()) / 60000) : 0;
    const isOverdue = branding?.tableBusyWarning !== false && occupied && elapsedMinutes >= warningMinutes;
    const canOverrideOwnership = branding?.singleWaiterMode === true ? false : canViewAllTables;
    const isOtherWaiterTable =
      occupied && !canOverrideOwnership && currentEmployeeId != null && order!.waiterId !== currentEmployeeId;
    const reserved = !occupied && reservationBlockMinutes > 0 ? findConflictingReservation(table.id) : null;

    return (
      <button
        key={table.id}
        type="button"
        onClick={() => void openTable(table)}
        disabled={!table.isActive || isCreating}
        className={cn(
          "relative flex flex-col items-center justify-center gap-0.5 rounded-xl border-2 p-1.5 transition-all",
          wide ? "min-h-24 w-full" : "h-32",
          "select-none active:scale-95",
          occupied
            ? "border-transparent text-white shadow-md " + statusBg(status)
            : "border-border bg-card text-card-foreground hover:border-primary/40",
          !table.isActive && "cursor-not-allowed opacity-40",
          isOverdue && "ring-2 ring-red-500 ring-offset-1",
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
          <span className={cn("mt-0.5 text-xs", reserved ? "font-semibold text-amber-600" : "text-muted-foreground")}>
            {isCreating ? "..." : reserved ? `Rezerv · ${reserved.reservationTime}` : "Boş"}
          </span>
        )}
      </button>
    );
  };

  return (
    <div className="p-4 sm:p-6">
      <div className="mb-4 flex items-center justify-between">
        <h1 className="text-xl font-bold">Masalar</h1>
        <div className="flex items-center gap-2">
          {canQuickSale && (
            <Button size="sm" disabled={takeAwayCreating || modeBusy} onClick={() => void handleCreateTakeAwayOrder()}>
              <ShoppingBag className="mr-2 h-4 w-4" />
              {takeAwayCreating ? "Açılır..." : "Tez satış"}
            </Button>
          )}
          {branding?.modulePaket === true && (
            <Button size="sm" onClick={() => setDeliveryDialogOpen(true)}>
              Çatdırılma sifarişi
            </Button>
          )}
          {canMoveTable && (
            <Button
              size="sm"
              variant={mode === "move-pick" || mode === "move-target" ? "default" : "outline"}
              disabled={modeBusy}
              onClick={() => {
                const active = mode === "move-pick" || mode === "move-target";
                resetMode();
                if (!active) setMode("move-pick");
              }}
            >
              <ArrowLeftRight className="mr-2 h-4 w-4" />
              Masa dəyiş
            </Button>
          )}
          {canChangeWaiter && (
            <Button
              size="sm"
              variant={mode === "waiter-pick" ? "default" : "outline"}
              disabled={modeBusy}
              onClick={startWaiterChange}
            >
              <UserCog className="mr-2 h-4 w-4" />
              Ofisiant dəyiş
            </Button>
          )}
          {canPrintBill && (
            <Button
              size="sm"
              variant={mode === "bill" ? "default" : "outline"}
              disabled={modeBusy}
              onClick={() => {
                const active = mode === "bill";
                resetMode();
                if (!active) setMode("bill");
              }}
            >
              <Receipt className="mr-2 h-4 w-4" />
              Hesab
            </Button>
          )}
          {canPay && (
            <Button
              size="sm"
              variant={mode === "pay" ? "default" : "outline"}
              disabled={modeBusy}
              onClick={() => {
                const active = mode === "pay";
                resetMode();
                if (!active) setMode("pay");
              }}
            >
              <Banknote className="mr-2 h-4 w-4" />
              Ödəniş
            </Button>
          )}
          {canSeeReports && (
            <Button variant="outline" size="sm" disabled={modeBusy} onClick={() => router.push("/pos/reports")}>
              <BarChart3 className="mr-2 h-4 w-4" />
              Hesabat
            </Button>
          )}
          <Button variant="outline" size="sm" onClick={() => void load()}>
            <RefreshCw className="h-4 w-4" />
          </Button>
        </div>
      </div>

      {mode && (
        <div className="mb-4 flex items-center justify-between rounded-lg border border-primary/40 bg-primary/10 px-3 py-2 text-sm">
          <span>
            {modeBusy
              ? "Gözləyin..."
              : mode === "pay"
                ? "Nağd ödəniləcək masaya toxunun — masa bağlanacaq və çek çıxacaq"
                : mode === "bill"
                ? "Hesabını (ön çek) çıxarmaq istədiyiniz masaya toxunun — masa bağlanmır"
                : mode === "move-pick"
                  ? "Köçürmək istədiyiniz masaya toxunun"
                  : mode === "move-target"
                    ? `${moveSource?.name ?? "Masa"} hansı boş masaya köçürülsün?`
                    : "Ofisiantı dəyişəcəyiniz masaya toxunun"}
          </span>
          <Button size="sm" variant="ghost" disabled={modeBusy} onClick={resetMode}>
            Ləğv et
          </Button>
        </div>
      )}

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

      {(() => {
        const takeAwayOrders = tables.filter(
          (t) => t.type === RestaurantTableType.TakeAway && t.activeOrder && isActiveStatus(t.activeOrder.status),
        );
        if (takeAwayOrders.length === 0) return null;
        return (
          <div className="mb-4 space-y-2">
            <p className="text-sm font-semibold text-muted-foreground">Aktiv tez satışlar</p>
            <div className="grid grid-cols-2 gap-2 sm:grid-cols-3 md:grid-cols-4">
              {takeAwayOrders.map((t) => (
                <button
                  key={t.id}
                  type="button"
                  disabled={mode !== null}
                  onClick={() => router.push(`/pos/order/${t.activeOrder!.id}`)}
                  className="rounded-md border bg-card p-3 text-left text-sm hover:bg-muted/50 disabled:opacity-50"
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
            <div className="mb-4 space-y-2">
              <p className="text-sm font-semibold text-muted-foreground">Aktiv çatdırılmalar</p>
              <div className="grid grid-cols-1 gap-2 sm:grid-cols-2 md:grid-cols-3">
                {deliveryOrders.map((t) => (
                  <button
                    key={t.id}
                    type="button"
                    onClick={() => router.push(`/pos/order/${t.activeOrder!.id}`)}
                    className="rounded-md border bg-card p-3 text-left text-sm hover:bg-muted/50"
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
        <div className="mb-4 rounded-md border border-destructive/30 bg-destructive/10 px-4 py-3 text-sm text-destructive">
          {error}
        </div>
      )}

      {/* Regular tables on the left (grouped by zone); every cabinet / hall on the right edge, one under another. */}
      <div className="flex items-start gap-3">
        <div className="min-w-0 flex-1 space-y-4">
          {tables.length === 0 && !error && (
            <p className="text-sm text-muted-foreground">Bu filial üçün masa tapılmadı.</p>
          )}
          {regularGroups.map((group) => (
            <div key={group.key}>
              {group.title && (
                <p className="mb-2 text-sm font-semibold text-muted-foreground">{group.title}</p>
              )}
              <div className="grid grid-cols-3 gap-3 sm:grid-cols-4 md:grid-cols-5 lg:grid-cols-6 xl:grid-cols-7">
                {group.tables.map((table) => renderTableTile(table))}
              </div>
            </div>
          ))}
        </div>

        {cabinetTables.length > 0 && (
          <aside className="sticky top-4 flex w-32 shrink-0 flex-col gap-2 sm:w-44">
            <p className="text-sm font-semibold text-muted-foreground">Kabinet və zallar</p>
            {cabinetTables.map((table) => renderTableTile(table, true))}
          </aside>
        )}
      </div>

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

      <Dialog open={reservationWarning !== null} onOpenChange={(open) => !open && setReservationWarning(null)}>
        <DialogContent className="sm:max-w-sm">
          <DialogHeader>
            <DialogTitle>Masa rezerv edilib</DialogTitle>
          </DialogHeader>
          {reservationWarning && (
            <p className="text-sm text-muted-foreground">
              {reservationWarning.table.name} masası saat {reservationWarning.reservation.reservationTime} üçün{" "}
              <span className="font-medium text-foreground">{reservationWarning.reservation.guestName}</span> adına
              rezerv edilib ({reservationWarning.reservation.guestCount} nəfər). Yenə də açmaq istəyirsiniz?
            </p>
          )}
          <DialogFooter>
            <Button variant="outline" onClick={() => setReservationWarning(null)}>
              Ləğv et
            </Button>
            <Button onClick={handleConfirmReservationWarning}>Yenə də aç</Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

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
