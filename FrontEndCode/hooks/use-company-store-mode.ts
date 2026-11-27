"use client";

import { useEffect, useState } from "react";
import { useSelectedCompany } from "@/contexts/selected-company-context";
import { useSelectedRestaurant } from "@/contexts/selected-restaurant-context";
import { getStoredToken } from "@/lib/auth-client";
import { getCompanyIdFromToken } from "@/lib/jwt-permissions";
import { getCompanySettingsBranding } from "@/lib/services/company-settings-service";

/**
 * True when the company being looked at (the one picked in the dashboard filter, else the user's own) works in Mağaza rejimi.
 * null while it is not known yet. Uses the same answer (and cache) as the sidebar, so a page and its menu always agree.
 */
export function useCompanyStoreMode(): boolean | null {
  const { selectedCompanyId } = useSelectedCompany();
  const { selectedRestaurantId } = useSelectedRestaurant();
  const companyId = selectedCompanyId ?? getCompanyIdFromToken(getStoredToken());
  const cacheKey = `branding:${companyId}:${selectedRestaurantId ?? 0}`;
  const [value, setValue] = useState<boolean | null>(null);

  useEffect(() => {
    if (!companyId) {
      setValue(false);
      return;
    }
    let cancelled = false;
    try {
      const cached = sessionStorage.getItem(cacheKey);
      if (cached) setValue((JSON.parse(cached) as { moduleDataSecimi?: boolean }).moduleDataSecimi === true);
      else setValue(null);
    } catch {
      setValue(null);
    }
    getCompanySettingsBranding(companyId, selectedRestaurantId ?? undefined)
      .then((b) => !cancelled && setValue(b.moduleDataSecimi === true))
      .catch(() => !cancelled && setValue(false));
    return () => {
      cancelled = true;
    };
  }, [companyId, selectedRestaurantId, cacheKey]);

  return value;
}
