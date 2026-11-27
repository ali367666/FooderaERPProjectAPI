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
import { getCompanies, getCompanyById, type Company } from "@/lib/services/company-service";
import type { SelectedCompanyScope } from "@/lib/company-scope-utils";
import { useHasPermission, useIsSuperAdmin } from "@/hooks/use-auth-permissions";
import { getStoredToken } from "@/lib/auth-client";
import { getCompanyIdFromToken } from "@/lib/jwt-permissions";
import { uniqueById } from "@/lib/utils";

const STORAGE_KEY = "dashboardSelectedCompanyId";

type SelectedCompanyContextValue = {
  companies: Company[];
  companiesLoading: boolean;
  companiesError: string | null;
  selectedCompanyId: SelectedCompanyScope;
  setSelectedCompanyId: (id: SelectedCompanyScope) => void;
  reloadCompanies: () => Promise<void>;
};

const SelectedCompanyContext = createContext<SelectedCompanyContextValue | null>(null);

export function SelectedCompanyProvider({ children }: { children: ReactNode }) {
  // Only the platform SuperAdmin looks across companies. Everyone else works in the company of their own account (the server does the
  // same), so for them the company is fixed here: every page loads and saves that company's data without asking.
  const isSuperAdmin = useIsSuperAdmin();
  const ownCompanyId = isSuperAdmin ? null : getCompanyIdFromToken(getStoredToken());
  const canViewOwnCompany = useHasPermission("Company.View");
  const canViewCompanies = isSuperAdmin;
  const [companies, setCompanies] = useState<Company[]>([]);
  const [companiesLoading, setCompaniesLoading] = useState(true);
  const [companiesError, setCompaniesError] = useState<string | null>(null);
  const [selectedCompanyId, setSelectedCompanyIdState] = useState<SelectedCompanyScope>(null);
  const [storageHydrated, setStorageHydrated] = useState(false);

  // Cross-company filtering (GET /companies) needs Company.View — skip the call entirely for
  // roles that don't hold it, rather than surface a 403 everywhere.
  const reloadCompanies = useCallback(async () => {
    if (!canViewCompanies) return;
    try {
      setCompaniesError(null);
      const data = await getCompanies();
      setCompanies(uniqueById(data));
    } catch (e) {
      setCompaniesError(e instanceof Error ? e.message : "Failed to load companies.");
    }
  }, [canViewCompanies]);

  useEffect(() => {
    if (!canViewCompanies) {
      const own: Company | null = ownCompanyId ? ({ id: ownCompanyId, name: "Şirkətim", companyCode: "" } as Company) : null;
      setCompanies(own ? [own] : []);
      setCompaniesLoading(false);
      if (!ownCompanyId || !canViewOwnCompany) return;
      let gone = false;
      void getCompanyById(ownCompanyId)
        .then((c) => !gone && setCompanies([c]))
        .catch(() => undefined);
      return () => {
        gone = true;
      };
    }
    let cancelled = false;
    (async () => {
      setCompaniesLoading(true);
      try {
        setCompaniesError(null);
        const data = await getCompanies();
        if (!cancelled) setCompanies(uniqueById(data));
      } catch (e) {
        if (!cancelled) {
          setCompaniesError(e instanceof Error ? e.message : "Failed to load companies.");
        }
      } finally {
        if (!cancelled) setCompaniesLoading(false);
      }
    })();
    return () => {
      cancelled = true;
    };
  }, [canViewCompanies, ownCompanyId, canViewOwnCompany]);

  useEffect(() => {
    if (typeof window === "undefined") return;
    const raw = localStorage.getItem(STORAGE_KEY);
    if (raw === null || raw === "") {
      setStorageHydrated(true);
      return;
    }
    const n = Number(raw);
    if (Number.isFinite(n) && n > 0) setSelectedCompanyIdState(n);
    setStorageHydrated(true);
  }, []);

  useEffect(() => {
    if (!storageHydrated || selectedCompanyId == null || companies.length === 0) return;
    if (!companies.some((c) => c.id === selectedCompanyId)) {
      setSelectedCompanyIdState(null);
      if (typeof window !== "undefined") localStorage.removeItem(STORAGE_KEY);
    }
  }, [companies, selectedCompanyId, storageHydrated]);

  const setSelectedCompanyId = useCallback((id: SelectedCompanyScope) => {
    setSelectedCompanyIdState(id);
    if (typeof window === "undefined") return;
    if (id == null) localStorage.removeItem(STORAGE_KEY);
    else localStorage.setItem(STORAGE_KEY, String(id));
  }, []);

  // A company left over in this browser (for example from a SuperAdmin session) must not leak into a tenant's pages: the stored choice,
  // which some requests read directly, is their own company.
  if (typeof window !== "undefined" && storageHydrated && ownCompanyId && localStorage.getItem(STORAGE_KEY) !== String(ownCompanyId)) {
    localStorage.setItem(STORAGE_KEY, String(ownCompanyId));
  }
  const effectiveCompanyId = ownCompanyId ?? selectedCompanyId;

  const value = useMemo<SelectedCompanyContextValue>(
    () => ({
      companies,
      companiesLoading,
      companiesError,
      selectedCompanyId: effectiveCompanyId,
      setSelectedCompanyId,
      reloadCompanies,
    }),
    [companies, companiesLoading, companiesError, effectiveCompanyId, setSelectedCompanyId, reloadCompanies],
  );

  // The saved company is read in the first effect; until then the pages below would load the signed-in user's own company
  // and switch a moment later (wrong data flashing, a second request). They are mounted only once the choice is known.
  if (!storageHydrated) return null;

  return <SelectedCompanyContext.Provider value={value}>{children}</SelectedCompanyContext.Provider>;
}

export function useSelectedCompany(): SelectedCompanyContextValue {
  const ctx = useContext(SelectedCompanyContext);
  if (!ctx) {
    throw new Error("useSelectedCompany must be used within SelectedCompanyProvider");
  }
  return ctx;
}
