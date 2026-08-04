"use client";

import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import { useRouter } from "next/navigation";
import { getRestaurants, type Restaurant } from "@/lib/services/restaurant-service";
import { getRestaurantTables, type RestaurantTable } from "@/lib/services/restaurant-table-service";
import { getOrders, type OrderDto } from "@/lib/services/order-service";
import { cn } from "@/lib/utils";
import {
  Users,
  Clock,
  ChefHat,
  CheckCircle,
  Plus,
  LayoutGrid,
  Map,
  ZoomIn,
  ZoomOut,
  RefreshCw,
} from "lucide-react";
// Users, Clock, ChefHat, CheckCircle used in grid view cards and status helpers
import { Button } from "@/components/ui/button";
import { toApiFormError } from "@/lib/api-error";
import { TableFurniture, type TableStatus } from "@/components/floor-plan/table-furniture";

type TableWithOrder = RestaurantTable & {
  activeOrder: OrderDto | null;
};

type ViewMode = "grid" | "plan";

// ── Status helpers ───────────────────────────────────────────────────────────

// Any order considered "active" (draft/open/etc.) but without a dedicated
// color gets mapped to "open" — an occupied table should never render as
// visually empty, regardless of its exact workflow status.
function toTableStatus(status: OrderDto["status"]): TableStatus {
  switch (status) {
    case "in_preparation":
    case "ready":
    case "served":
      return status;
    default:
      return "open";
  }
}

// Keep in sync with STATUS_COLORS in components/floor-plan/table-furniture.tsx
function statusBg(status: OrderDto["status"] | null): string {
  if (!status) return "";
  switch (status) {
    case "in_preparation":  return "bg-[#fb923c]";
    case "ready":           return "bg-[#34d399]";
    case "served":          return "bg-[#f472b6]";
    default:                return "bg-[#6366f1]"; // open / draft / anything active
  }
}

function statusLabel(status: OrderDto["status"]): string {
  switch (status) {
    case "open":            return "Açıq";
    case "in_preparation":  return "Hazırlanır";
    case "ready":           return "Hazır";
    case "served":          return "Verildi";
    case "paid":            return "Ödəndi";
    case "cancelled":       return "Ləğv";
    default:                return status;
  }
}

function StatusIcon({ status }: { status: OrderDto["status"] }) {
  switch (status) {
    case "open":            return <Clock className="h-3 w-3" />;
    case "in_preparation":  return <ChefHat className="h-3 w-3" />;
    case "ready":           return <CheckCircle className="h-3 w-3" />;
    default:                return null;
  }
}

// ── Grid view card ───────────────────────────────────────────────────────────

function TableCard({ table, onClick }: { table: TableWithOrder; onClick: () => void }) {
  const order = table.activeOrder;
  const occupied = order !== null;

  return (
    <button
      onClick={onClick}
      className={cn(
        "relative flex flex-col items-center justify-center rounded-xl border-2 transition-all duration-200",
        "w-28 h-28 gap-1 cursor-pointer select-none",
        "hover:scale-105 hover:shadow-lg active:scale-95",
        occupied
          ? "border-transparent shadow-md text-white " + statusBg(order!.status)
          : "border-border bg-card text-card-foreground hover:border-primary/40",
        !table.isActive && "opacity-40 cursor-not-allowed",
      )}
      disabled={!table.isActive}
      title={occupied ? `Sifariş #${order!.id} — ${statusLabel(order!.status)}` : "Boş masa"}
    >
      <span className={cn("text-lg font-bold leading-none", occupied ? "text-white" : "text-foreground")}>
        {table.name}
      </span>
      <span className={cn("flex items-center gap-1 text-xs", occupied ? "text-white/80" : "text-muted-foreground")}>
        <Users className="h-3 w-3" />
        {table.capacity}
      </span>
      {occupied && (
        <span className="flex items-center gap-1 text-xs font-medium text-white/90 mt-1">
          <StatusIcon status={order!.status} />
          {statusLabel(order!.status)}
        </span>
      )}
      {occupied && (
        <span className="absolute top-2 right-2 w-2.5 h-2.5 rounded-full bg-white/80 animate-pulse" />
      )}
      {!occupied && (
        <span className="text-xs text-muted-foreground mt-0.5">Boş</span>
      )}
    </button>
  );
}

// ── Plan view — floor plan canvas ────────────────────────────────────────────

const MIN_ZOOM = 0.3;
const MAX_ZOOM = 2;

function PlanView({
  tables,
  onTableClick,
}: {
  tables: TableWithOrder[];
  onTableClick: (table: TableWithOrder) => void;
}) {
  const [zoom, setZoom] = useState(1);
  const [pan, setPan] = useState({ x: 40, y: 40 });
  const panDrag = useRef<{
    active: boolean;
    startMouseX: number;
    startMouseY: number;
    startPanX: number;
    startPanY: number;
  } | null>(null);

  // Check if any tables have been positioned (non-zero)
  const hasPositions = tables.some((t) => t.posX !== 0 || t.posY !== 0);

  function handleCanvasMouseDown(e: React.MouseEvent) {
    if (e.button === 1 || e.button === 2) {
      e.preventDefault();
      panDrag.current = {
        active: true,
        startMouseX: e.clientX,
        startMouseY: e.clientY,
        startPanX: pan.x,
        startPanY: pan.y,
      };
    }
  }

  useEffect(() => {
    function onMove(e: MouseEvent) {
      if (!panDrag.current?.active) return;
      setPan({
        x: panDrag.current.startPanX + (e.clientX - panDrag.current.startMouseX),
        y: panDrag.current.startPanY + (e.clientY - panDrag.current.startMouseY),
      });
    }
    function onUp() { panDrag.current = null; }
    window.addEventListener("mousemove", onMove);
    window.addEventListener("mouseup", onUp);
    return () => { window.removeEventListener("mousemove", onMove); window.removeEventListener("mouseup", onUp); };
  }, []);

  function handleWheel(e: React.WheelEvent) {
    e.preventDefault();
    setZoom((z) => Math.min(MAX_ZOOM, Math.max(MIN_ZOOM, z - e.deltaY * 0.001)));
  }

  return (
    <div className="flex flex-col gap-2 flex-1 min-h-0">
      {/* Mini toolbar */}
      <div className="flex items-center gap-2">
        <Button size="sm" variant="outline" onClick={() => setZoom((z) => Math.min(MAX_ZOOM, z + 0.1))}>
          <ZoomIn className="h-4 w-4" />
        </Button>
        <span className="text-xs text-muted-foreground w-10 text-center">{Math.round(zoom * 100)}%</span>
        <Button size="sm" variant="outline" onClick={() => setZoom((z) => Math.max(MIN_ZOOM, z - 0.1))}>
          <ZoomOut className="h-4 w-4" />
        </Button>
        <Button size="sm" variant="outline" onClick={() => { setZoom(1); setPan({ x: 40, y: 40 }); }}>
          <RefreshCw className="h-4 w-4" />
        </Button>
        {!hasPositions && (
          <span className="text-xs text-amber-600 ml-2">
            ⚠ Masaların mövqeyi hələ təyin edilməyib — əvvəlcə &quot;Zal Dizaynı&quot;nda düzənləyin
          </span>
        )}
      </div>

      {/* Canvas */}
      <div
        className="flex-1 rounded-xl border border-border bg-muted/30 overflow-hidden relative"
        style={{ cursor: "default" }}
        onMouseDown={handleCanvasMouseDown}
        onWheel={handleWheel}
        onContextMenu={(e) => e.preventDefault()}
      >
        {/* Grid dots */}
        <svg className="absolute inset-0 w-full h-full pointer-events-none" xmlns="http://www.w3.org/2000/svg">
          <defs>
            <pattern id="dots" width={20 * zoom} height={20 * zoom} patternUnits="userSpaceOnUse"
              x={pan.x % (20 * zoom)} y={pan.y % (20 * zoom)}>
              <circle cx={zoom} cy={zoom} r="0.8" fill="currentColor" className="text-border" />
            </pattern>
          </defs>
          <rect width="100%" height="100%" fill="url(#dots)" />
        </svg>

        {/* Tables */}
        <div
          style={{
            position: "absolute",
            left: pan.x,
            top: pan.y,
            transformOrigin: "0 0",
            transform: `scale(${zoom})`,
          }}
        >
          {tables.map((table) => {
            const order = table.activeOrder;
            const tableStatus: TableStatus = order
              ? toTableStatus(order.status)
              : "empty";

            return (
              <div
                key={table.id}
                onClick={() => table.isActive && onTableClick(table)}
                style={{
                  position: "absolute",
                  left: table.posX,
                  top: table.posY,
                  width: table.width,
                  height: table.height,
                  transform: `rotate(${table.rotation}deg)`,
                  transformOrigin: "center center",
                  cursor: table.isActive ? "pointer" : "not-allowed",
                  transition: "transform 0.1s",
                }}
                className="hover:scale-105 active:scale-95 select-none"
                title={
                  order
                    ? `${table.name} — Sifariş #${order.id} (${statusLabel(order.status)})`
                    : `${table.name} — Boş`
                }
              >
                <TableFurniture
                  width={table.width}
                  height={table.height}
                  shape={table.shape}
                  capacity={table.capacity}
                  name={table.name}
                  status={tableStatus}
                  isActive={table.isActive}
                />
              </div>
            );
          })}
        </div>

        {tables.length === 0 && (
          <div className="absolute inset-0 flex items-center justify-center">
            <p className="text-muted-foreground text-sm">Bu restoran üçün masa tapılmadı.</p>
          </div>
        )}

        <div className="absolute bottom-2 right-3 text-[10px] text-muted-foreground/50 pointer-events-none">
          Scroll = zoom • Sağ klik+sürüşdür = pan
        </div>
      </div>
    </div>
  );
}

// ── Main page ────────────────────────────────────────────────────────────────

export function TableMapPage() {
  const router = useRouter();
  const [restaurants, setRestaurants] = useState<Restaurant[]>([]);
  const [tables, setTables] = useState<RestaurantTable[]>([]);
  const [orders, setOrders] = useState<OrderDto[]>([]);
  const [selectedRestaurantId, setSelectedRestaurantId] = useState<number | null>(null);
  const [viewMode, setViewMode] = useState<ViewMode>("plan");
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const load = useCallback(async () => {
    try {
      setLoading(true);
      setError(null);
      const [r, t, o] = await Promise.all([
        getRestaurants(),
        getRestaurantTables(),
        getOrders(),
      ]);
      setRestaurants(r);
      setTables(t);
      setOrders(o);
      if (r.length > 0 && selectedRestaurantId === null) {
        setSelectedRestaurantId(r[0].id);
      }
    } catch (e) {
      setError(toApiFormError(e, "Yüklənmə xətası").message);
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    void load();
    const interval = setInterval(() => void load(), 30_000);
    return () => clearInterval(interval);
  }, [load]);

  const activeOrders = useMemo(
    () => orders.filter((o) => o.status !== "paid" && o.status !== "cancelled"),
    [orders],
  );

  const filteredTables = useMemo(
    () =>
      selectedRestaurantId
        ? tables.filter((t) => t.restaurantId === selectedRestaurantId)
        : tables,
    [tables, selectedRestaurantId],
  );

  const tablesWithOrders: TableWithOrder[] = useMemo(
    () =>
      filteredTables.map((t) => ({
        ...t,
        activeOrder: activeOrders.find((o) => o.tableId === t.id) ?? null,
      })),
    [filteredTables, activeOrders],
  );

  const stats = useMemo(() => {
    const occupied = tablesWithOrders.filter((t) => t.activeOrder !== null).length;
    const total = tablesWithOrders.filter((t) => t.isActive).length;
    return { occupied, free: total - occupied, total };
  }, [tablesWithOrders]);

  function handleTableClick(table: TableWithOrder) {
    if (!table.isActive) return;
    if (table.activeOrder) {
      router.push(`/dashboard/orders/${table.activeOrder.id}`);
    } else {
      router.push(`/dashboard/orders/create?tableId=${table.id}&restaurantId=${table.restaurantId}`);
    }
  }

  return (
    <div className="flex flex-col gap-4" style={{ height: "calc(100vh - 8rem)" }}>
      {/* Header */}
      <div className="flex flex-col gap-3 sm:flex-row sm:items-end sm:justify-between shrink-0">
        <div>
          <h1 className="text-2xl font-semibold tracking-tight">Table Map</h1>
          <p className="text-muted-foreground text-sm mt-1">
            Masaya klikləyin — boşdursa yeni sifariş, doludursa mövcud sifarişi açır.
          </p>
        </div>
        <div className="flex gap-2 items-center">
          {/* View toggle */}
          <div className="flex rounded-lg border border-border overflow-hidden">
            <button
              onClick={() => setViewMode("plan")}
              className={cn(
                "flex items-center gap-1.5 px-3 py-1.5 text-sm transition-colors",
                viewMode === "plan"
                  ? "bg-primary text-primary-foreground"
                  : "bg-card hover:bg-muted text-foreground",
              )}
            >
              <Map className="h-4 w-4" />
              Plan
            </button>
            <button
              onClick={() => setViewMode("grid")}
              className={cn(
                "flex items-center gap-1.5 px-3 py-1.5 text-sm transition-colors border-l border-border",
                viewMode === "grid"
                  ? "bg-primary text-primary-foreground"
                  : "bg-card hover:bg-muted text-foreground",
              )}
            >
              <LayoutGrid className="h-4 w-4" />
              Grid
            </button>
          </div>
          <Button variant="outline" size="sm" onClick={() => void load()}>
            Yenilə
          </Button>
          <Button size="sm" onClick={() => router.push("/dashboard/orders/create")}>
            <Plus className="h-4 w-4 mr-1" />
            Yeni sifariş
          </Button>
        </div>
      </div>

      {error && (
        <div className="rounded-md border border-destructive/40 bg-destructive/10 px-4 py-3 text-sm text-destructive shrink-0">
          {error}
        </div>
      )}

      {/* Stats */}
      <div className="flex gap-4 flex-wrap shrink-0">
        <div className="rounded-lg border border-border bg-card px-4 py-3 flex items-center gap-3">
          <span className="w-3 h-3 rounded-full bg-muted-foreground/30" />
          <span className="text-sm">Boş: <strong>{stats.free}</strong></span>
        </div>
        <div className="rounded-lg border border-border bg-card px-4 py-3 flex items-center gap-3">
          <span className="w-3 h-3 rounded-full bg-[#6366f1]" />
          <span className="text-sm">Dolu: <strong>{stats.occupied}</strong></span>
        </div>
        <div className="rounded-lg border border-border bg-card px-4 py-3 flex items-center gap-3">
          <span className="w-3 h-3 rounded-full bg-border" />
          <span className="text-sm">Cəmi: <strong>{stats.total}</strong></span>
        </div>
      </div>

      {/* Restaurant tabs */}
      {restaurants.length > 1 && (
        <div className="flex gap-2 flex-wrap shrink-0">
          {restaurants.map((r) => (
            <button
              key={r.id}
              onClick={() => setSelectedRestaurantId(r.id)}
              className={cn(
                "px-4 py-2 rounded-lg text-sm font-medium border transition-colors",
                selectedRestaurantId === r.id
                  ? "bg-primary text-primary-foreground border-primary"
                  : "bg-card border-border hover:border-primary/40",
              )}
            >
              {r.name}
            </button>
          ))}
        </div>
      )}

      {/* Legend */}
      <div className="flex gap-4 flex-wrap text-xs text-muted-foreground shrink-0">
        <span className="flex items-center gap-1.5"><span className="w-3 h-3 rounded bg-[#f0fdfa] border border-[#5eead4]" /> Boş</span>
        <span className="flex items-center gap-1.5"><span className="w-3 h-3 rounded bg-[#6366f1]" /> Açıq sifariş</span>
        <span className="flex items-center gap-1.5"><span className="w-3 h-3 rounded bg-[#fb923c]" /> Hazırlanır</span>
        <span className="flex items-center gap-1.5"><span className="w-3 h-3 rounded bg-[#34d399]" /> Hazır</span>
        <span className="flex items-center gap-1.5"><span className="w-3 h-3 rounded bg-[#f472b6]" /> Verildi</span>
      </div>

      {/* Content */}
      {loading ? (
        <div className="flex-1 rounded-xl bg-muted animate-pulse" />
      ) : viewMode === "plan" ? (
        <PlanView tables={tablesWithOrders} onTableClick={handleTableClick} />
      ) : (
        /* Grid view */
        tablesWithOrders.length === 0 ? (
          <div className="py-16 text-center text-muted-foreground text-sm">
            Bu restoran üçün masa tapılmadı.
          </div>
        ) : (
          <div className="flex flex-wrap gap-4 pt-2 overflow-y-auto">
            {tablesWithOrders.map((table) => (
              <TableCard
                key={table.id}
                table={table}
                onClick={() => handleTableClick(table)}
              />
            ))}
          </div>
        )
      )}
    </div>
  );
}
