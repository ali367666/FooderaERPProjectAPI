"use client";

import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useState,
  type ReactNode,
} from "react";
import { getRestaurants, type Restaurant } from "@/lib/services/restaurant-service";
import { useSelectedCompany } from "@/contexts/selected-company-context";
import { resolveCompanyId } from "@/lib/resolve-company-id";

const STORAGE_KEY = "dashboardSelectedRestaurantId";

type SelectedRestaurantContextValue = {
  restaurants: Restaurant[];
  restaurantsLoading: boolean;
  selectedRestaurantId: number | null;
  setSelectedRestaurantId: (id: number | null) => void;
  reloadRestaurants: () => Promise<void>;
};

const SelectedRestaurantContext = createContext<SelectedRestaurantContextValue | null>(null);

/**
 * "Data Seçimi" — a business owner with several branches (e.g. an İstirahət Kompleksi with a
 * restaurant, a store and a fitness branch under one Company) picks ONE branch to view/manage its
 * own data and, when the Company is in Kompleks mode, its own module set. Nested inside
 * SelectedCompanyProvider: the branch list always belongs to whichever company is currently in
 * scope (SuperAdmin's picked company, or the tenant's own).
 */
export function SelectedRestaurantProvider({ children }: { children: ReactNode }) {
  const { selectedCompanyId } = useSelectedCompany();
  const [restaurants, setRestaurants] = useState<Restaurant[]>([]);
  const [restaurantsLoading, setRestaurantsLoading] = useState(true);
  const [selectedRestaurantId, setSelectedRestaurantIdState] = useState<number | null>(null);
  const [storageHydrated, setStorageHydrated] = useState(false);

  const effectiveCompanyId = useMemo(() => {
    if (selectedCompanyId != null) return selectedCompanyId;
    try {
      return resolveCompanyId();
    } catch {
      return null;
    }
  }, [selectedCompanyId]);

  const reloadRestaurants = useCallback(async () => {
    try {
      setRestaurantsLoading(true);
      const data = await getRestaurants(effectiveCompanyId ?? undefined);
      setRestaurants(effectiveCompanyId != null ? data.filter((r) => r.companyId === effectiveCompanyId) : data);
    } catch {
      setRestaurants([]);
    } finally {
      setRestaurantsLoading(false);
    }
  }, [effectiveCompanyId]);

  useEffect(() => {
    void reloadRestaurants();
  }, [reloadRestaurants]);

  useEffect(() => {
    if (typeof window === "undefined") return;
    const raw = localStorage.getItem(STORAGE_KEY);
    if (raw === null || raw === "") {
      setStorageHydrated(true);
      return;
    }
    const n = Number(raw);
    if (Number.isFinite(n) && n > 0) setSelectedRestaurantIdState(n);
    setStorageHydrated(true);
  }, []);

  // Clear a stale/foreign selection once the branch list for the current company scope is known.
  useEffect(() => {
    if (!storageHydrated || restaurantsLoading || selectedRestaurantId == null) return;
    if (!restaurants.some((r) => r.id === selectedRestaurantId)) {
      setSelectedRestaurantIdState(null);
      if (typeof window !== "undefined") localStorage.removeItem(STORAGE_KEY);
    }
  }, [restaurants, restaurantsLoading, selectedRestaurantId, storageHydrated]);

  const setSelectedRestaurantId = useCallback((id: number | null) => {
    setSelectedRestaurantIdState(id);
    if (typeof window === "undefined") return;
    if (id == null) localStorage.removeItem(STORAGE_KEY);
    else localStorage.setItem(STORAGE_KEY, String(id));
  }, []);

  const value = useMemo<SelectedRestaurantContextValue>(
    () => ({
      restaurants,
      restaurantsLoading,
      selectedRestaurantId,
      setSelectedRestaurantId,
      reloadRestaurants,
    }),
    [restaurants, restaurantsLoading, selectedRestaurantId, setSelectedRestaurantId, reloadRestaurants],
  );

  return (
    <SelectedRestaurantContext.Provider value={value}>{children}</SelectedRestaurantContext.Provider>
  );
}

export function useSelectedRestaurant(): SelectedRestaurantContextValue {
  const ctx = useContext(SelectedRestaurantContext);
  if (!ctx) {
    throw new Error("useSelectedRestaurant must be used within SelectedRestaurantProvider");
  }
  return ctx;
}
