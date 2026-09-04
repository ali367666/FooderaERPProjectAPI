"use client";

import { useCallback, useEffect, useRef, useState } from "react";
import { useRouter } from "next/navigation";
import { toast } from "sonner";
import { Lock, RefreshCw, Users } from "lucide-react";
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
import { getRestaurantTables, type RestaurantTable } from "@/lib/services/restaurant-table-service";
import { getOrders, createOrder, type OrderDto, type OrderWorkflowStatus } from "@/lib/services/order-service";
import { getRestaurantSections, type RestaurantSection } from "@/lib/services/restaurant-section-service";
import { useHasPermission } from "@/hooks/use-auth-permissions";
import {
  getCompanySettingsBranding,
  type CompanySettingsBranding,
} from "@/lib/services/company-settings-service";
import { playPosAlert } from "@/lib/pos-sound-alert";

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
  const [activeSectionId, setActiveSectionId] = useState<number | null>(null);
  const canChangeSection = useHasPermission("Pos.ChangeDepartment");
  const canViewAllTables = useHasPermission("Pos.RedirectUser");
  const [now, setNow] = useState(() => new Date());
  const [branding, setBranding] = useState<CompanySettingsBranding | null>(null);
  const alertedTableIds = useRef<Set<number>>(new Set());
  const [guestCountTable, setGuestCountTable] = useState<TableWithOrder | null>(null);
  const [guestCountInput, setGuestCountInput] = useState("");
  const [currentEmployeeId, setCurrentEmployeeId] = useState<number | null>(null);

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

  useEffect(() => {
    if (!terminal?.companyId) return;
    getCompanySettingsBranding(terminal.companyId)
      .then(setBranding)
      .catch(() => setBranding(null));
  }, [terminal]);

  const warningMinutes = branding?.tableTimeWarningMinutes ?? 45;

  useEffect(() => {
    const stillOverdueIds = new Set<number>();
    for (const table of tables) {
      const order = table.activeOrder;
      if (!order) continue;
      const elapsedMinutes = Math.floor((now.getTime() - new Date(order.openedAt).getTime()) / 60000);
      if (elapsedMinutes >= warningMinutes) stillOverdueIds.add(table.id);
    }
    const newlyOverdue = [...stillOverdueIds].some((id) => !alertedTableIds.current.has(id));
    if (newlyOverdue) playPosAlert(branding);
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

  const openTable = async (table: TableWithOrder) => {
    if (table.activeOrder) {
      if (!canViewAllTables && currentEmployeeId != null && table.activeOrder.waiterId !== currentEmployeeId) {
        toast.error("Bu masa başqa ofisiantə aiddir, baxa bilməzsiniz.");
        return;
      }
      router.push(`/pos/order/${table.activeOrder.id}`);
      return;
    }
    if (!terminal) return;

    if (branding?.askGuestCountOnOpen) {
      setGuestCountInput("");
      setGuestCountTable(table);
      return;
    }

    void createOrderForTable(table);
  };

  const handleConfirmGuestCount = () => {
    if (!guestCountTable) return;
    const parsed = Number(guestCountInput);
    const guestCount = Number.isFinite(parsed) && parsed > 0 ? Math.floor(parsed) : undefined;
    const table = guestCountTable;
    setGuestCountTable(null);
    void createOrderForTable(table, guestCount);
  };

  if (loading) {
    return <div className="flex h-full items-center justify-center text-muted-foreground">Yüklənir...</div>;
  }

  return (
    <div className="p-4 sm:p-6">
      <div className="mb-4 flex items-center justify-between">
        <h1 className="text-xl font-bold">Masalar</h1>
        <Button variant="outline" size="sm" onClick={() => void load()}>
          <RefreshCw className="h-4 w-4" />
        </Button>
      </div>

      {error && (
        <div className="mb-4 rounded-md border border-destructive/30 bg-destructive/10 px-4 py-3 text-sm text-destructive">
          {error}
        </div>
      )}

      {sections.length > 0 && (
        <div className="mb-4 flex flex-wrap gap-2">
          <button
            type="button"
            onClick={() => setActiveSectionId(null)}
            disabled={!canChangeSection && activeSectionId !== null}
            className={cn(
              "rounded-full px-3 py-1.5 text-sm font-medium",
              activeSectionId === null ? "bg-primary text-primary-foreground" : "bg-muted text-muted-foreground hover:bg-muted/70",
            )}
          >
            Hamısı
          </button>
          {sections.map((s) => (
            <button
              key={s.id}
              type="button"
              onClick={() => canChangeSection && setActiveSectionId(s.id)}
              disabled={!canChangeSection && activeSectionId !== s.id}
              className={cn(
                "rounded-full px-3 py-1.5 text-sm font-medium",
                activeSectionId === s.id ? "bg-primary text-primary-foreground" : "bg-muted text-muted-foreground hover:bg-muted/70",
                !canChangeSection && "cursor-not-allowed opacity-60",
              )}
            >
              {s.name}
            </button>
          ))}
        </div>
      )}

      {tables.length === 0 && !error && (
        <p className="text-sm text-muted-foreground">Bu filial üçün masa tapılmadı.</p>
      )}

      <div className="grid grid-cols-3 gap-3 sm:grid-cols-4 md:grid-cols-6 lg:grid-cols-8">
        {tables
          .filter((table) => (activeSectionId === null ? table.sectionId == null : table.sectionId === activeSectionId))
          .map((table) => {
          const occupied = table.activeOrder !== null;
          const status = table.activeOrder?.status ?? null;
          const isCreating = creatingTableId === table.id;

          const order = table.activeOrder;
          const elapsedMinutes = order ? Math.floor((now.getTime() - new Date(order.openedAt).getTime()) / 60000) : 0;
          const isOverdue = occupied && elapsedMinutes >= warningMinutes;
          const isOtherWaiterTable =
            occupied && !canViewAllTables && currentEmployeeId != null && order!.waiterId !== currentEmployeeId;

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
                isOtherWaiterTable && "opacity-60",
              )}
            >
              {isOtherWaiterTable && (
                <Lock className="absolute right-1.5 top-1.5 h-3.5 w-3.5 text-white/90" />
              )}
              <span className="text-lg font-bold leading-none">{table.name}</span>
              <span
                className={cn(
                  "flex items-center gap-1 text-xs",
                  occupied ? "text-white/80" : "text-muted-foreground",
                )}
              >
                <Users className="h-3 w-3" />
                {table.capacity}
              </span>
              {occupied ? (
                <>
                  <span className="text-xs font-medium text-white/90">{statusLabel(status!)}</span>
                  {order?.waiterName && (
                    <span className="max-w-full truncate text-[11px] text-white/80">{order.waiterName}</span>
                  )}
                  {order?.guestCount != null && (
                    <span className="text-[11px] text-white/80">{order.guestCount} nəfər</span>
                  )}
                  <span className="text-[11px] text-white/80">{formatElapsed(order!.openedAt, now.getTime())}</span>
                  <span className="text-xs font-semibold text-white">{order!.totalAmount.toFixed(2)} ₼</span>
                  {order?.note && (
                    <span className="max-w-full truncate text-[10px] italic text-white/70">{order.note}</span>
                  )}
                </>
              ) : (
                <span className="mt-0.5 text-xs text-muted-foreground">
                  {isCreating ? "..." : "Boş"}
                </span>
              )}
            </button>
          );
        })}
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
          </div>
          <DialogFooter>
            <Button variant="outline" onClick={() => setGuestCountTable(null)}>
              Ləğv et
            </Button>
            <Button onClick={handleConfirmGuestCount}>Təsdiqlə</Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}
