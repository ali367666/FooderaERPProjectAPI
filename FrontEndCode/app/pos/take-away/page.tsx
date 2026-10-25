"use client";

import { useCallback, useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { toast } from "sonner";
import { Plus, RefreshCw } from "lucide-react";
import { Button } from "@/components/ui/button";
import { PosEmpty, PosPage, PosPageError, PosPageLoading, PosTileGrid } from "@/components/pos/pos-page";
import { OrderTile, useMinuteClock } from "@/components/pos/order-tile";
import { useHasPermission } from "@/hooks/use-auth-permissions";
import { getCurrentEmployeeId } from "@/lib/pos-session";
import { getPosTerminalContext, type PosTerminalContext } from "@/lib/pos-terminal-client";
import { createOrder, getOrders, type OrderDto } from "@/lib/services/order-service";
import { getRestaurantTables, RestaurantTableType } from "@/lib/services/restaurant-table-service";
import { isActiveOrderStatus } from "@/lib/pos-order-status";

/** Active "Tez satış" (take-away) orders: each sits on its own virtual TakeAway table. */
export default function PosTakeAwayPage() {
  const router = useRouter();
  const [terminal, setTerminal] = useState<PosTerminalContext | null | undefined>(undefined);
  const [orders, setOrders] = useState<OrderDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [creating, setCreating] = useState(false);
  const now = useMinuteClock();
  const canCreate = useHasPermission("Orders.Create");

  useEffect(() => {
    setTerminal(getPosTerminalContext());
  }, []);

  const load = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const [tables, all] = await Promise.all([getRestaurantTables(), getOrders()]);
      const takeAwayTableIds = new Set(tables.filter((t) => t.type === RestaurantTableType.TakeAway).map((t) => t.id));
      setOrders(
        all
          .filter((o) => takeAwayTableIds.has(o.tableId) && isActiveOrderStatus(o.status))
          .filter((o) => !terminal?.restaurantId || o.restaurantId === terminal.restaurantId)
          .sort((a, b) => a.openedAt.localeCompare(b.openedAt)),
      );
    } catch (err) {
      setError(err instanceof Error ? err.message : "Tez satışlar yüklənə bilmədi");
    } finally {
      setLoading(false);
    }
  }, [terminal]);

  useEffect(() => {
    if (terminal === undefined) return;
    void load();
  }, [terminal, load]);

  const handleCreate = async () => {
    if (!terminal?.restaurantId) {
      toast.error("Terminal filiala bağlı deyil.");
      return;
    }
    setCreating(true);
    try {
      const waiterId = await getCurrentEmployeeId();
      if (!waiterId) {
        toast.error("Bu istifadəçi heç bir işçiyə bağlı deyil. Users səhifəsindən bağlayın.");
        return;
      }
      const order = await createOrder({ restaurantId: terminal.restaurantId, waiterId, isTakeAway: true });
      router.push(`/pos/order/${order.id}`);
    } catch (err) {
      toast.error(err instanceof Error ? err.message : "Tez satış yaradıla bilmədi");
    } finally {
      setCreating(false);
    }
  };

  if (loading || terminal === undefined) return <PosPageLoading />;

  return (
    <PosPage
      title="Tez satışlar"
      description="Açıq qalan tez satış sifarişləri"
      actions={
        <>
          {canCreate && (
            <Button size="sm" disabled={creating} onClick={() => void handleCreate()}>
              <Plus className="mr-1 h-4 w-4" />
              {creating ? "Açılır..." : "Yeni tez satış"}
            </Button>
          )}
          <Button variant="outline" size="sm" onClick={() => void load()}>
            <RefreshCw className="h-4 w-4" />
          </Button>
        </>
      }
    >
      {error && <PosPageError message={error} />}

      {orders.length === 0 && !error && <PosEmpty>Aktiv tez satış yoxdur.</PosEmpty>}

      <PosTileGrid>
        {orders.map((order) => (
          <OrderTile
            key={order.id}
            order={order}
            title={order.orderNumber}
            now={now}
            onClick={() => router.push(`/pos/order/${order.id}`)}
          />
        ))}
      </PosTileGrid>
    </PosPage>
  );
}
