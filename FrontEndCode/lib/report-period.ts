/** Shared date-period picker logic for the report pages (Hesabatlar, Z Hesabatı). */

export type PeriodKey = "today" | "yesterday" | "week" | "month" | "custom";

export const PERIODS: { key: PeriodKey; label: string }[] = [
  { key: "today", label: "Bugün" },
  { key: "yesterday", label: "Dünən" },
  { key: "week", label: "Bu həftə" },
  { key: "month", label: "Bu ay" },
  { key: "custom", label: "Özəl tarix" },
];

function startOfDay(d: Date): Date {
  const x = new Date(d);
  x.setHours(0, 0, 0, 0);
  return x;
}

function endOfDay(d: Date): Date {
  const x = new Date(d);
  x.setHours(23, 59, 59, 999);
  return x;
}

export function toDateInputValue(d: Date): string {
  const x = new Date(d);
  x.setMinutes(x.getMinutes() - x.getTimezoneOffset());
  return x.toISOString().slice(0, 10);
}

/** "HH:mm" -> [hours, minutes]; null when empty or not a time. */
function parseTime(value: string | undefined): [number, number] | null {
  const m = /^(\d{1,2}):(\d{2})/.exec(value ?? "");
  return m ? [Number(m[1]), Number(m[2])] : null;
}

/**
 * The period's window, narrowed to a time of day: from `fromTime` on the first day to the end of the `toTime` minute on the last day
 * (e.g. today, 18:00 - 23:00). Empty times keep the whole days.
 */
export function computeRangeWithTimes(
  period: PeriodKey,
  customFrom: string,
  customTo: string,
  fromTime: string,
  toTime: string,
): { from: Date; to: Date } | null {
  const range = computeRange(period, customFrom, customTo);
  if (!range) return null;
  const f = parseTime(fromTime);
  const t = parseTime(toTime);
  const from = new Date(range.from);
  const to = new Date(range.to);
  if (f) from.setHours(f[0], f[1], 0, 0);
  if (t) to.setHours(t[0], t[1], 59, 999);
  return { from, to };
}

export function computeRange(period: PeriodKey, customFrom: string, customTo: string): { from: Date; to: Date } | null {
  const now = new Date();
  switch (period) {
    case "today":
      return { from: startOfDay(now), to: endOfDay(now) };
    case "yesterday": {
      const y = new Date(now);
      y.setDate(y.getDate() - 1);
      return { from: startOfDay(y), to: endOfDay(y) };
    }
    case "week": {
      const from = new Date(now);
      from.setDate(from.getDate() - 6);
      return { from: startOfDay(from), to: endOfDay(now) };
    }
    case "month": {
      const from = new Date(now.getFullYear(), now.getMonth(), 1);
      return { from: startOfDay(from), to: endOfDay(now) };
    }
    case "custom": {
      if (!customFrom || !customTo) return null;
      return { from: startOfDay(new Date(customFrom)), to: endOfDay(new Date(customTo)) };
    }
    default:
      return null;
  }
}

/** "2026-11-12" + "18:00" -> "2026-11-12T18:00"; without a time the plain date (whole days). */
export function withTime(date: string, time: string): string {
  return date && time ? `${date}T${time}` : date;
}
