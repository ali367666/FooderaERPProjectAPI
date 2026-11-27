"use client";

import { useCallback, useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { Plus, RefreshCw } from "lucide-react";
import { Button } from "@/components/ui/button";
import { PosEmpty, PosPage, PosPageError, PosPageLoading, PosTileGrid } from "@/components/pos/pos-page";
import { OrderTile, useMinuteClock } from "@/components/pos/order-tile";
import { useHasPermission } from "@/hooks/use-auth-permissions";
import { getPosTerminalContext, type PosTerminalContext } from "@/lib/pos-terminal-client";
import { addDays, startOfDay } from "date-fns";
import { getActiveTakeAwayOrders, getOrders, type OrderDto } from "@/lib/services/order-service";
import { getRestaurantTables, RestaurantTableType } from "@/lib/services/restaurant-table-service";
import { isActiveOrderStatus } from "@/lib/pos-order-status";

/** Active "Tez satış" (take-away) orders: each sits on its own virtual TakeAway table. */
export default function PosTakeAwayPage() {
  const router = useRouter();
  const [terminal, setTerminal] = useState<PosTerminalContext | null | undefined>(undefined);
  const [orders, setOrders] = useState<OrderDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const now = useMinuteClock();
  const canCreate = useHasPermission("Orders.Create");

  useEffect(() => {
    setTerminal(getPosTerminalContext());
  }, []);

  const load = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      // A quick sale is paid the moment it is made, so today's paid ones are listed too (after the open ones).
      // "Today" is the terminal's local day; paid orders are narrowed to it here, on the client.
      const restaurantId = terminal?.restaurantId ?? undefined;
      const todayStartMs = startOfDay(new Date()).getTime();
      const tomorrowStartMs = addDays(todayStartMs, 1).getTime();
      const [open, paid, tables] = await Promise.all([
        getActiveTakeAwayOrders(restaurantId),
        getOrders({ status: "Paid" }),
        getRestaurantTables(),
      ]);
      const takeAwayTableIds = new Set(tables.filter((t) => t.type === RestaurantTableType.TakeAway).map((t) => t.id));
      const active = open
        .filter((o) => isActiveOrderStatus(o.status))
        .sort((a, b) => a.openedAt.localeCompare(b.openedAt));
      const paidToday = paid
        .filter((o) => takeAwayTableIds.has(o.tableId))
        .filter((o) => !restaurantId || o.restaurantId === restaurantId)
        .filter((o) => {
          if (!o.paidAt) return false;
          const paidMs = new Date(o.paidAt).getTime();
          return paidMs >= todayStartMs && paidMs < tomorrowStartMs;
        })
        .sort((a, b) => (b.paidAt ?? b.openedAt).localeCompare(a.paidAt ?? a.openedAt));
      setOrders([...active, ...paidToday]);
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

  // A new quick sale goes through its own one-request screen, not an open take-away order.
  const handleCreate = () => router.push("/pos/quick-sale");

  if (loading || terminal === undefined) return <PosPageLoading />;

  return (
    <PosPage
      title="Tez satışlar"
      description="Açıq qalan və bu gün ödənilmiş tez satışlar"
      actions={
        <>
          {canCreate && (
            <Button size="sm" onClick={handleCreate}>
              <Plus className="mr-1 h-4 w-4" />
              Yeni tez satış
            </Button>
          )}
          <Button variant="outline" size="sm" onClick={() => void load()}>
            <RefreshCw className="h-4 w-4" />
          </Button>
        </>
      }
    >
      {error && <PosPageError message={error} />}

      {orders.length === 0 && !error && <PosEmpty>Bu gün tez satış yoxdur.</PosEmpty>}

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
