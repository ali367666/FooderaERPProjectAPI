/** Mirrors Domain.Constants.ReservationRules — the choices behind the two reservation settings (0 = off). */
export const RESERVATION_BLOCK_OPTIONS: { value: number; label: string }[] = [
  { value: 0, label: "Söndürülüb" },
  { value: 30, label: "Yarım saat (30 dəq)" },
  { value: 45, label: "45 dəqiqə" },
  { value: 60, label: "1 saat" },
  { value: 90, label: "1,5 saat" },
];

export const RESERVATION_AUTO_CANCEL_OPTIONS: { value: number; label: string }[] = [
  { value: 0, label: "Söndürülüb" },
  { value: 60, label: "1 saat" },
  { value: 90, label: "1,5 saat" },
  { value: 120, label: "2 saat" },
];

/** The person reserving types the holding time; these are only quick picks. Mirrors the server limits. */
export const DURATION_SHORTCUTS = [30, 60, 90, 120, 180];
export const MIN_DURATION = 15;
export const MAX_DURATION = 24 * 60;

/** Quick picks for extending a reservation; any number of minutes can be typed instead. */
export const EXTEND_SHORTCUTS = [15, 30, 60];

type ReservationSpan = { reservationDate: string; reservationTime: string; durationMinutes: number };

/** Local calendar date as yyyy-mm-dd (toISOString would shift it to UTC). */
export function localIsoDate(d: Date = new Date()): string {
  const p = (n: number) => String(n).padStart(2, "0");
  return `${d.getFullYear()}-${p(d.getMonth() + 1)}-${p(d.getDate())}`;
}

export function reservationStartMs(r: ReservationSpan): number {
  return new Date(`${r.reservationDate.slice(0, 10)}T${r.reservationTime}`).getTime();
}

export function reservationEndMs(r: ReservationSpan): number {
  return reservationStartMs(r) + r.durationMinutes * 60_000;
}

/** A pending/confirmed reservation whose held time has run out and whose guest was never seated. */
export function isReservationExpired(r: ReservationSpan & { status: string }, nowMs: number): boolean {
  return (r.status === "Pending" || r.status === "Confirmed") && nowMs > reservationEndMs(r);
}
