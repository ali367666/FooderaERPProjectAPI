"use client";

import { usePathname } from "next/navigation";
import { FILTER_SELECT_CLASS } from "@/components/advanced-table-filters";
import { useSelectedCompany } from "@/contexts/selected-company-context";
import { cn } from "@/lib/utils";

/** Global company scope control; hidden on pages where it does not apply. */
export function DashboardCompanyToolbar() {
  const pathname = usePathname() ?? "";
  const { companies, companiesLoading, companiesError, selectedCompanyId, setSelectedCompanyId } =
    useSelectedCompany();

  if (pathname === "/dashboard/companies" || pathname === "/dashboard/company") {
    return null;
  }

  return (
    <div className="mb-3 flex flex-wrap items-center gap-2 rounded-md border border-border/80 bg-muted/20 px-3 py-2 shadow-sm">
      <div className="flex flex-col gap-0.5 sm:flex-row sm:items-center sm:gap-2">
        <span className="text-xs font-medium text-foreground whitespace-nowrap">Company filter</span>
        <select
          className={cn(FILTER_SELECT_CLASS, "min-w-[200px]")}
          disabled={companiesLoading || companies.length === 0}
          value={selectedCompanyId ?? ""}
          onChange={(e) => {
            const v = e.target.value;
            setSelectedCompanyId(v === "" ? null : Number(v));
          }}
          aria-label="Filter dashboard data by company"
        >
          <option value="">All Companies</option>
          {companies.map((c) => (
            <option key={c.id} value={String(c.id)}>
              {c.name}
            </option>
          ))}
        </select>
      </div>
      {companiesError && <p className="text-xs text-destructive">{companiesError}</p>}
      {companiesLoading && <p className="text-xs text-muted-foreground">Loading companies…</p>}
    </div>
  );
}
