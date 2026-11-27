import { clsx, type ClassValue } from 'clsx'
import { twMerge } from 'tailwind-merge'

export function cn(...inputs: ClassValue[]) {
  return twMerge(clsx(inputs))
}

/** Drops repeated rows with the same `id` (keeps the first), so list items get unique React keys. */
export function uniqueById<T extends { id: number | string }>(items: T[]): T[] {
  const seen = new Set<number | string>();
  return items.filter((item) => (seen.has(item.id) ? false : (seen.add(item.id), true)));
}
