const SELECTED_COMPANY_KEY = "dashboardSelectedCompanyId";
const TOKEN_KEY = "token";

function readCompanyIdFromToken(): number | null {
  if (typeof window === "undefined") return null;
  const token = localStorage.getItem(TOKEN_KEY);
  if (!token) return null;
  const parts = token.split(".");
  if (parts.length < 2) return null;
  try {
    const base64 = parts[1].replace(/-/g, "+").replace(/_/g, "/");
    const padded = base64 + "=".repeat((4 - (base64.length % 4)) % 4);
    const payload = JSON.parse(atob(padded)) as Record<string, unknown>;
    const raw = payload.companyId ?? payload.CompanyId ?? payload.company_id ?? payload.Company_id;
    const companyId = Number(raw);
    return Number.isFinite(companyId) && companyId > 0 ? companyId : null;
  } catch {
    return null;
  }
}

/** Resolves the current company: selected-company override, then the logged-in user's own JWT claim. */
export function resolveCompanyId(): number {
  if (typeof window !== "undefined") {
    const selected = Number(localStorage.getItem(SELECTED_COMPANY_KEY) || "");
    if (Number.isFinite(selected) && selected > 0) return selected;
  }

  const fromToken = readCompanyIdFromToken();
  if (fromToken && fromToken > 0) return fromToken;

  throw new Error("Unable to resolve companyId from selected company or token.");
}
