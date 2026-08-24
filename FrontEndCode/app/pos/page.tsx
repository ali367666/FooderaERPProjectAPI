"use client";

import { useCallback, useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { toast } from "sonner";
import { RefreshCw, Users } from "lucide-react";
import { Button } from "@/components/ui/button";
import { cn } from "@/lib/utils";
import { getPosTerminalContext, type PosTerminalContext } from "@/lib/pos-terminal-client";
import { getCurrentEmployeeId } from "@/lib/pos-session";
import { getRestaurantTables, type RestaurantTable } from "@/lib/services/restaurant-table-service";
import { getOrders, createOrder, type OrderDto, type OrderWorkflowStatus } from "@/lib/services/order-service";

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

export default function PosTablesPage() {
  const router = useRouter();
  const [tables, setTables] = useState<TableWithOrder[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [creatingTableId, setCreatingTableId] = useState<number | null>(null);
  const [terminal, setTerminal] = useState<PosTerminalContext | null | undefined>(undefined);

  useEffect(() => {
    setTerminal(getPosTerminalContext());
  }, []);

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

  const openTable = async (table: TableWithOrder) => {
    if (table.activeOrder) {
      router.push(`/pos/order/${table.activeOrder.id}`);
      return;
    }
    if (!terminal) return;

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
      });
      router.push(`/pos/order/${order.id}`);
    } catch (err) {
      toast.error(err instanceof Error ? err.message : "Sifariş yaradıla bilmədi");
    } finally {
      setCreatingTableId(null);
    }
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

      {tables.length === 0 && !error && (
        <p className="text-sm text-muted-foreground">Bu filial üçün masa tapılmadı.</p>
      )}

      <div className="grid grid-cols-3 gap-3 sm:grid-cols-4 md:grid-cols-6 lg:grid-cols-8">
        {tables.map((table) => {
          const occupied = table.activeOrder !== null;
          const status = table.activeOrder?.status ?? null;
          const isCreating = creatingTableId === table.id;

          return (
            <button
              key={table.id}
              type="button"
              onClick={() => void openTable(table)}
              disabled={!table.isActive || isCreating}
              className={cn(
                "relative flex h-28 flex-col items-center justify-center gap-1 rounded-xl border-2 transition-all",
                "select-none active:scale-95",
                occupied
                  ? "border-transparent text-white shadow-md " + statusBg(status)
                  : "border-border bg-card text-card-foreground hover:border-primary/40",
                !table.isActive && "cursor-not-allowed opacity-40",
              )}
            >
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
                <span className="mt-1 text-xs font-medium text-white/90">{statusLabel(status!)}</span>
              ) : (
                <span className="mt-0.5 text-xs text-muted-foreground">
                  {isCreating ? "..." : "Boş"}
                </span>
              )}
            </button>
          );
        })}
      </div>
    </div>
  );
}
