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
