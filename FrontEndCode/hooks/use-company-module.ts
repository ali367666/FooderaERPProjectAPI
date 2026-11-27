"use client";

import { useEffect, useState } from "react";
import { useSelectedCompany } from "@/contexts/selected-company-context";
import { useSelectedRestaurant } from "@/contexts/selected-restaurant-context";
import { getStoredToken } from "@/lib/auth-client";
import { getCompanyIdFromToken } from "@/lib/jwt-permissions";
import { getCompanySettingsBranding, type CompanySettingsBranding } from "@/lib/services/company-settings-service";

type ModuleKey = "moduleDepartment";

/**
 * Whether a module the SuperAdmin can switch is on for the company being looked at (the one picked in the dashboard filter,
 * else the user's own). null while it is not known yet; pages treat "not known" like "on" so nothing flickers away.
 * Uses the same answer (and cache) as the sidebar.
 */
export function useCompanyModule(key: ModuleKey): boolean | null {
  const { selectedCompanyId } = useSelectedCompany();
  const { selectedRestaurantId } = useSelectedRestaurant();
  const companyId = selectedCompanyId ?? getCompanyIdFromToken(getStoredToken());
  const cacheKey = `branding:${companyId}:${selectedRestaurantId ?? 0}`;
  const [value, setValue] = useState<boolean | null>(null);

  useEffect(() => {
    if (!companyId) {
      setValue(true);
      return;
    }
    let cancelled = false;
    try {
      const cached = localStorage.getItem(cacheKey);
      setValue(cached ? (JSON.parse(cached) as Partial<CompanySettingsBranding>)[key] !== false : null);
    } catch {
      setValue(null);
    }
    getCompanySettingsBranding(companyId, selectedRestaurantId ?? undefined)
      .then((b) => !cancelled && setValue(b[key] !== false))
      .catch(() => !cancelled && setValue(true));
    return () => {
      cancelled = true;
    };
  }, [companyId, selectedRestaurantId, cacheKey, key]);

  return value;
}
