/** Client-side scope: `null` means all companies. */
export type SelectedCompanyScope = number | null;

export function filterBySelectedCompany<T>(
  items: T[],
  selectedCompanyId: SelectedCompanyScope,
  getCompanyId: (item: T) => number | null | undefined,
): T[] {
  if (selectedCompanyId == null) return items;
  return items.filter((item) => getCompanyId(item) === selectedCompanyId);
}

export async function fetchListsPerCompany<T>(
  companyIds: number[],
  fetcher: (companyId: number) => Promise<T[]>,
): Promise<T[]> {
  const unique = [...new Set(companyIds.filter((id) => Number.isFinite(id) && id > 0))];
  if (unique.length === 0) return [];
  const chunks = await Promise.all(unique.map((id) => fetcher(id)));
  return chunks.flat();
}

export function numericCompanyId(value: unknown): number | null {
  if (value === undefined || value === null) return null;
  const n = typeof value === "number" ? value : Number(String(value).trim());
  if (!Number.isFinite(n) || n <= 0) return null;
  return Math.trunc(n);
}
