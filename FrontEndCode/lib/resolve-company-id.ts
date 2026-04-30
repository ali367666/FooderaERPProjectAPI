const COMPANY_FALLBACK = 5;

/** Matches other services: `localStorage.companyId` or fallback for dev. */
export function resolveCompanyId(): number {
  if (typeof window !== "undefined") {
    const stored = Number(localStorage.getItem("companyId") || "");
    if (Number.isFinite(stored) && stored > 0) return stored;
  }
  return COMPANY_FALLBACK;
}
