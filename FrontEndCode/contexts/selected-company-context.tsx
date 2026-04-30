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
import { getCompanies, type Company } from "@/lib/services/company-service";
import type { SelectedCompanyScope } from "@/lib/company-scope-utils";

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
  const [companies, setCompanies] = useState<Company[]>([]);
  const [companiesLoading, setCompaniesLoading] = useState(true);
  const [companiesError, setCompaniesError] = useState<string | null>(null);
  const [selectedCompanyId, setSelectedCompanyIdState] = useState<SelectedCompanyScope>(null);
  const [storageHydrated, setStorageHydrated] = useState(false);

  const reloadCompanies = useCallback(async () => {
    try {
      setCompaniesError(null);
      const data = await getCompanies();
      setCompanies(data);
    } catch (e) {
      setCompaniesError(e instanceof Error ? e.message : "Failed to load companies.");
    }
  }, []);

  useEffect(() => {
    let cancelled = false;
    (async () => {
      setCompaniesLoading(true);
      try {
        setCompaniesError(null);
        const data = await getCompanies();
        if (!cancelled) setCompanies(data);
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
  }, []);

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

  const value = useMemo<SelectedCompanyContextValue>(
    () => ({
      companies,
      companiesLoading,
      companiesError,
      selectedCompanyId,
      setSelectedCompanyId,
      reloadCompanies,
    }),
    [companies, companiesLoading, companiesError, selectedCompanyId, setSelectedCompanyId, reloadCompanies],
  );

  return <SelectedCompanyContext.Provider value={value}>{children}</SelectedCompanyContext.Provider>;
}

export function useSelectedCompany(): SelectedCompanyContextValue {
  const ctx = useContext(SelectedCompanyContext);
  if (!ctx) {
    throw new Error("useSelectedCompany must be used within SelectedCompanyProvider");
  }
  return ctx;
}
