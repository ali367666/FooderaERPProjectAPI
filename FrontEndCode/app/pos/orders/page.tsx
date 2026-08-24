"use client";

import { useCallback, useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { Clock, RefreshCw } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { cn } from "@/lib/utils";
import { getPosTerminalContext, type PosTerminalContext } from "@/lib/pos-terminal-client";
import { getOrders, type OrderDto, type OrderWorkflowStatus } from "@/lib/services/order-service";

function isActiveStatus(status: OrderWorkflowStatus): boolean {
  return status !== "paid" && status !== "cancelled";
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

function statusBadgeClass(status: OrderWorkflowStatus): string {
  switch (status) {
    case "in_preparation":
      return "bg-orange-100 text-orange-800";
    case "ready":
      return "bg-emerald-100 text-emerald-800";
    case "served":
      return "bg-pink-100 text-pink-700";
    default:
      return "bg-indigo-100 text-indigo-800";
  }
}

export default function PosOrdersPage() {
  const router = useRouter();
  const [terminal, setTerminal] = useState<PosTerminalContext | null | undefined>(undefined);
  const [orders, setOrders] = useState<OrderDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    setTerminal(getPosTerminalContext());
  }, []);

  const load = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const all = await getOrders();
      const active = all
        .filter((o) => isActiveStatus(o.status))
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

  if (loading || terminal === undefined) {
    return <div className="flex h-full items-center justify-center text-muted-foreground">Yüklənir...</div>;
  }

  return (
    <div className="p-4 sm:p-6">
      <div className="mb-4 flex items-center justify-between">
        <h1 className="text-xl font-bold">Aktiv sifarişlər</h1>
        <Button variant="outline" size="sm" onClick={() => void load()}>
          <RefreshCw className="h-4 w-4" />
        </Button>
      </div>

      {error && (
        <div className="mb-4 rounded-md border border-destructive/30 bg-destructive/10 px-4 py-3 text-sm text-destructive">
          {error}
        </div>
      )}

      {orders.length === 0 && !error && (
        <p className="text-sm text-muted-foreground">Aktiv sifariş yoxdur.</p>
      )}

      <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-3">
        {orders.map((order) => (
          <button
            key={order.id}
            type="button"
            onClick={() => router.push(`/pos/order/${order.id}`)}
            className="rounded-xl border bg-card p-4 text-left shadow-sm transition-transform active:scale-95"
          >
            <div className="mb-2 flex items-center justify-between">
              <span className="font-bold">{order.tableName ?? order.orderNumber}</span>
              <Badge className={cn("shrink-0", statusBadgeClass(order.status))}>
                {statusLabel(order.status)}
              </Badge>
            </div>
            <div className="flex items-center justify-between text-sm text-muted-foreground">
              <span className="flex items-center gap-1.5">
                <Clock className="h-3.5 w-3.5" />
                {new Date(order.openedAt).toLocaleTimeString("az-AZ", { hour: "2-digit", minute: "2-digit" })}
              </span>
              <span className="font-semibold text-foreground">{order.totalAmount.toFixed(2)} ₼</span>
            </div>
          </button>
        ))}
      </div>
    </div>
  );
}
