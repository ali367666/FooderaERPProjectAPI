"use client";

import { usePathname } from "next/navigation";
import { FILTER_SELECT_CLASS } from "@/components/advanced-table-filters";
import { useSelectedRestaurant } from "@/contexts/selected-restaurant-context";
import { cn } from "@/lib/utils";

/** "Data Seçimi" — lets an owner with 2+ branches view/manage one branch's own data at a time. */
export function DashboardBranchToolbar() {
  const pathname = usePathname() ?? "";
  const { restaurants, restaurantsLoading, selectedRestaurantId, setSelectedRestaurantId } =
    useSelectedRestaurant();

  if (restaurantsLoading || restaurants.length < 2) {
    return null;
  }

  if (pathname === "/dashboard/restaurants") {
    return null;
  }

  return (
    <div className="mb-3 flex flex-wrap items-center gap-2 rounded-md border border-border/80 bg-muted/20 px-3 py-2 shadow-sm">
      <div className="flex flex-col gap-0.5 sm:flex-row sm:items-center sm:gap-2">
        <span className="text-xs font-medium text-foreground whitespace-nowrap">Filial filter</span>
        <select
          className={cn(FILTER_SELECT_CLASS, "min-w-[200px]")}
          value={selectedRestaurantId ?? ""}
          onChange={(e) => {
            const v = e.target.value;
            setSelectedRestaurantId(v === "" ? null : Number(v));
          }}
          aria-label="Filter dashboard data by branch"
        >
          <option value="">Bütün Filiallar</option>
          {restaurants.map((r) => (
            <option key={r.id} value={String(r.id)}>
              {r.name}
            </option>
          ))}
        </select>
      </div>
    </div>
  );
}
