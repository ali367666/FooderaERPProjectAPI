import { api } from "@/lib/api";

export type GlobalSearchResult = {
  /** "Məhsul", "İşçi", "Konturagent"... */
  kind: string;
  title: string;
  subtitle: string | null;
  /** The dashboard page that lists this kind of record. */
  href: string;
};

/** Records (products, employees, counterparties, branches, tables...) whose name or code matches every word. */
export async function searchData(query: string, companyId?: number | null): Promise<GlobalSearchResult[]> {
  const response = await api.get<GlobalSearchResult[]>("/Search", {
    params: { q: query, companyId: companyId ?? undefined },
  });
  return Array.isArray(response.data) ? response.data : [];
}
