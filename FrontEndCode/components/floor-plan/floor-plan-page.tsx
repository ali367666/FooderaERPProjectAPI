"use client";

import { useCallback, useEffect, useState } from "react";
import { getRestaurants, type Restaurant } from "@/lib/services/restaurant-service";
import {
  getRestaurantTables,
  type RestaurantTable,
} from "@/lib/services/restaurant-table-service";
import { FloorPlanDesigner } from "./floor-plan-designer";
import { cn } from "@/lib/utils";
import { toApiFormError } from "@/lib/api-error";
import { Button } from "@/components/ui/button";
import { RefreshCw } from "lucide-react";

export function FloorPlanPage() {
  const [restaurants, setRestaurants] = useState<Restaurant[]>([]);
  const [tables, setTables] = useState<RestaurantTable[]>([]);
  const [selectedRestaurantId, setSelectedRestaurantId] = useState<number | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const load = useCallback(async () => {
    try {
      setLoading(true);
      setError(null);
      const [r, t] = await Promise.all([getRestaurants(), getRestaurantTables()]);
      setRestaurants(r);
      setTables(t);
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
  }, [load]);

  return (
    <div className="flex flex-col h-[calc(100vh-8rem)] gap-4">
      {/* Header */}
      <div className="flex items-end justify-between gap-3">
        <div>
          <h1 className="text-2xl font-semibold tracking-tight">Zal Dizaynı</h1>
          <p className="text-muted-foreground text-sm mt-1">
            Masaları sürüşdürərək restoranın planını qurun, sonra &quot;Saxla&quot; düyməsinə basın.
          </p>
        </div>
        <Button variant="outline" size="sm" onClick={() => void load()} className="gap-1.5">
          <RefreshCw className="h-4 w-4" />
          Yenilə
        </Button>
      </div>

      {error && (
        <div className="rounded-md border border-destructive/40 bg-destructive/10 px-4 py-3 text-sm text-destructive">
          {error}
        </div>
      )}

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

      {loading ? (
        <div className="flex-1 rounded-xl bg-muted animate-pulse" />
      ) : (
        <div className="flex-1 min-h-0">
          <FloorPlanDesigner
            tables={tables}
            restaurantId={selectedRestaurantId}
            onSaved={() => void load()}
          />
        </div>
      )}
    </div>
  );
}
