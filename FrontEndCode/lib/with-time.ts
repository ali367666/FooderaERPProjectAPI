/** "2026-11-27" + "14:30" gives "2026-11-27T14:30"; without a time the whole day is meant. */
export function withTime(day: string, time: string): string {
  return day && time ? `${day}T${time}` : day;
}
