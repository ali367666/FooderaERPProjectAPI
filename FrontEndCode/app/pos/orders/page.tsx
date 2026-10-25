"use client";

import { useCallback, useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { RefreshCw } from "lucide-react";
import { Button } from "@/components/ui/button";
import { PosEmpty, PosPage, PosPageError, PosPageLoading, PosTileGrid } from "@/components/pos/pos-page";
import { OrderTile, useMinuteClock } from "@/components/pos/order-tile";
import { getPosTerminalContext, type PosTerminalContext } from "@/lib/pos-terminal-client";
import { getOrders, type OrderDto } from "@/lib/services/order-service";
import { isActiveOrderStatus } from "@/lib/pos-order-status";

export default function PosOrdersPage() {
  const router = useRouter();
  const [terminal, setTerminal] = useState<PosTerminalContext | null | undefined>(undefined);
  const [orders, setOrders] = useState<OrderDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const now = useMinuteClock();

  useEffect(() => {
    setTerminal(getPosTerminalContext());
  }, []);

  const load = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const all = await getOrders();
      const active = all
        .filter((o) => isActiveOrderStatus(o.status))
        .filter((o) => !terminal?.restaurantId || o.restaurantId === terminal.restaurantId)
        .sort((a, b) => a.openedAt.localeCompare(b.openedAt));
      setOrders(active);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Sifarişlər yüklənə bilmədi");
    } finally {
      setLoading(false);
    }
  }, [terminal]);

  useEffect(() => {
    if (terminal === undefined) return;
    void load();
  }, [terminal, load]);

  if (loading || terminal === undefined) return <PosPageLoading />;

  return (
    <PosPage
      title="Aktiv sifarişlər"
      actions={
        <Button variant="outline" size="sm" onClick={() => void load()}>
          <RefreshCw className="h-4 w-4" />
        </Button>
      }
    >
      {error && <PosPageError message={error} />}
      {orders.length === 0 && !error && <PosEmpty>Aktiv sifariş yoxdur.</PosEmpty>}

      <PosTileGrid>
        {orders.map((order) => (
          <OrderTile
            key={order.id}
            order={order}
            title={order.tableName ?? order.orderNumber}
            now={now}
            onClick={() => router.push(`/pos/order/${order.id}`)}
          />
        ))}
      </PosTileGrid>
    </PosPage>
  );
}
