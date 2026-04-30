"use client";

import { useCallback, useEffect, useState } from "react";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { toast } from "sonner";
import { Loader2 } from "lucide-react";
import { usePermissionSet } from "@/hooks/use-auth-permissions";
import { AppPermissions } from "@/lib/app-permissions";
import {
  getKitchenOrders,
  markKitchenLineReady,
  startPreparingKitchenLine,
  type KitchenLineStatus,
  type KitchenOrderGroupDto,
} from "@/lib/services/kitchen-service";
import { toApiFormError } from "@/lib/api-error";
import { useSelectedCompany } from "@/contexts/selected-company-context";
import { getRestaurants, type Restaurant } from "@/lib/services/restaurant-service";

type KitchenStatusFilter = "active" | "pending" | "inPreparation" | "ready" | "all";

const statusBadge: Record<KitchenLineStatus, string> = {
  Pending: "bg-amber-100 text-amber-800",
  InPreparation: "bg-blue-100 text-blue-800",
  Ready: "bg-emerald-100 text-emerald-800",
  Served: "bg-slate-100 text-slate-800",
};

export default function KitchenPage() {
  const permissions = usePermissionSet();
  const canView = permissions.has(AppPermissions.KitchenView);
  const canStart = permissions.has(AppPermissions.KitchenStartPreparing);
  const canReady = permissions.has(AppPermissions.KitchenMarkReady);
  const { selectedCompanyId } = useSelectedCompany();

  const [orders, setOrders] = useState<KitchenOrderGroupDto[]>([]);
  const [restaurants, setRestaurants] = useState<Restaurant[]>([]);
  const [restaurantId, setRestaurantId] = useState<string>("");
  const [restaurantsLoading, setRestaurantsLoading] = useState(false);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [pendingLineId, setPendingLineId] = useState<number | null>(null);
  const [statusFilter, setStatusFilter] = useState<KitchenStatusFilter>("active");

  const loadRestaurants = useCallback(async () => {
    try {
      setRestaurantsLoading(true);
      const list = await getRestaurants();
      setRestaurants(list);
    } catch (e) {
      setError(toApiFormError(e, "Failed to load restaurants").message);
    } finally {
      setRestaurantsLoading(false);
    }
  }, []);

  const load = useCallback(async () => {
    const selectedRestaurantId = Number(restaurantId);
    if (!Number.isFinite(selectedRestaurantId) || selectedRestaurantId <= 0) {
      setOrders([]);
      setLoading(false);
      setError("Please select restaurant");
      return;
    }

    try {
      setLoading(true);
      setError(null);
      const nextOrders = await getKitchenOrders(selectedRestaurantId);
      console.log("Kitchen lines after refresh", nextOrders.flatMap((x) => x.lines));
      setOrders(nextOrders);
    } catch (e) {
      setError(toApiFormError(e, "Failed to load kitchen dashboard").message);
    } finally {
      setLoading(false);
    }
  }, [restaurantId]);

  useEffect(() => {
    if (!canView) return;
    void loadRestaurants();
  }, [canView, loadRestaurants]);

  useEffect(() => {
    if (!canView) return;
    void load();
  }, [canView, load]);

  const runAction = useCallback(
    async (orderLineId: number, action: "start" | "ready") => {
      try {
        setPendingLineId(orderLineId);
        if (action === "start") await startPreparingKitchenLine(orderLineId);
        else await markKitchenLineReady(orderLineId);
        toast.success("Kitchen line status updated");
        window.dispatchEvent(new Event("orders:refresh"));
        await load();
      } catch (e) {
        toast.error(toApiFormError(e, "Failed to update kitchen line").message);
      } finally {
        setPendingLineId(null);
      }
    },
    [load],
  );

  if (!canView) {
    return <div className="text-sm text-muted-foreground">You do not have Kitchen view permission.</div>;
  }

  const restaurantOptions = selectedCompanyId
    ? restaurants.filter((r) => r.companyId === selectedCompanyId)
    : restaurants;

  const getLineStatusValue = (status: unknown): number => {
    if (typeof status === "number") return status;
    if (typeof status === "string") {
      if (status === "Pending") return 1;
      if (status === "InPreparation") return 2;
      if (status === "Ready") return 3;
      if (status === "Served") return 4;
      if (status === "Cancelled") return 5;
      const normalized = status.trim().toLowerCase();
      if (normalized === "pending") return 1;
      if (normalized === "inpreparation" || normalized === "in_preparation" || normalized === "inprogress") return 2;
      if (normalized === "ready") return 3;
      if (normalized === "served") return 4;
      if (normalized === "cancelled") return 5;
    }
    return 0;
  };

  const lineMatchesFilter = (status: unknown) => {
    const statusValue = getLineStatusValue(status);
    if (statusFilter === "all") return true;
    if (statusFilter === "active") return statusValue === 1 || statusValue === 2;
    if (statusFilter === "pending") return statusValue === 1;
    if (statusFilter === "inPreparation") return statusValue === 2;
    if (statusFilter === "ready") return statusValue === 3;
    return true;
  };

  const filteredOrders = orders
    .map((order) => ({
      ...order,
      lines: order.lines.filter((line) => lineMatchesFilter(line.kitchenStatus)),
    }))
    .filter((order) => order.lines.length > 0);

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold">Kitchen Orders</h1>
          <p className="text-muted-foreground mt-1">Kitchen-only order lines grouped by order and table</p>
        </div>
        <div className="flex items-center gap-2">
          <select
            className="h-10 rounded-md border border-input bg-background px-3 py-2 text-sm"
            value={restaurantId}
            onChange={(e) => setRestaurantId(e.target.value)}
            disabled={restaurantsLoading}
          >
            <option value="">Select restaurant</option>
            {restaurantOptions.map((restaurant) => (
              <option key={restaurant.id} value={restaurant.id}>
                {restaurant.name}
              </option>
            ))}
          </select>
          <select
            className="h-10 rounded-md border border-input bg-background px-3 py-2 text-sm"
            value={statusFilter}
            onChange={(e) => setStatusFilter(e.target.value as KitchenStatusFilter)}
          >
            <option value="active">Active / Not Ready</option>
            <option value="pending">Pending</option>
            <option value="inPreparation">In Preparation</option>
            <option value="ready">Ready</option>
            <option value="all">All</option>
          </select>
          <Button variant="outline" onClick={() => void load()} disabled={loading || !restaurantId}>
            Refresh
          </Button>
        </div>
      </div>

      {error ? (
        <div className="rounded-md border border-destructive/40 bg-destructive/10 px-4 py-3 text-sm text-destructive">
          {error}
        </div>
      ) : null}

      {!restaurantId ? (
        <div className="rounded-md border p-6 text-sm text-muted-foreground">Please select restaurant</div>
      ) : loading ? (
        <div className="text-sm text-muted-foreground">Loading kitchen orders...</div>
      ) : filteredOrders.length === 0 ? (
        <div className="rounded-md border p-6 text-sm text-muted-foreground">No kitchen lines for selected filter.</div>
      ) : (
        filteredOrders.map((order) => (
          <Card key={order.orderId}>
            <CardHeader>
              <CardTitle>{order.orderNumber}</CardTitle>
              <CardDescription>
                {order.restaurantName ? `${order.restaurantName} | ` : ""}
                Table: {order.tableName || `#${order.tableId}`} | Order status: {order.orderStatus}
              </CardDescription>
            </CardHeader>
            <CardContent className="space-y-3">
              {order.lines.map((line) => (
                <div
                  key={line.orderLineId}
                  className="flex flex-wrap items-center justify-between gap-3 rounded-md border p-3"
                >
                  <div className="space-y-1">
                    <div className="font-medium">{line.menuItemName}</div>
                    <div className="text-sm text-muted-foreground">Qty: {line.quantity}</div>
                    {line.note ? <div className="text-sm text-muted-foreground">Note: {line.note}</div> : null}
                  </div>
                  <div className="flex items-center gap-2">
                    <Badge className={statusBadge[line.kitchenStatus]}>{line.kitchenStatus}</Badge>
                    {line.kitchenStatus === "Pending" ? (
                      <Button
                        size="sm"
                        disabled={!canStart || pendingLineId === line.orderLineId}
                        onClick={() => void runAction(line.orderLineId, "start")}
                      >
                        {pendingLineId === line.orderLineId ? <Loader2 className="h-4 w-4 animate-spin" /> : null}
                        Start Preparation
                      </Button>
                    ) : null}
                    {line.kitchenStatus === "InPreparation" ? (
                      <Button
                        size="sm"
                        disabled={!canReady || pendingLineId === line.orderLineId}
                        onClick={() => void runAction(line.orderLineId, "ready")}
                      >
                        {pendingLineId === line.orderLineId ? <Loader2 className="h-4 w-4 animate-spin" /> : null}
                        Mark Ready
                      </Button>
                    ) : null}
                    {line.kitchenStatus === "Ready" ? (
                      <Button size="sm" variant="outline" disabled>
                        Ready
                      </Button>
                    ) : null}
                  </div>
                </div>
              ))}
            </CardContent>
          </Card>
        ))
      )}
    </div>
  );
}
