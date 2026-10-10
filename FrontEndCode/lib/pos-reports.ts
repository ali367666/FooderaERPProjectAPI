/**
 * Permissions behind the POS "Hesabat" menu. The tables-screen button shows when the user holds at
 * least one of them; the menu itself lists only the entries the user may open.
 */
export const POS_REPORT_PERMISSION = {
  reports: "Pos.ZReport",
  zReport: "Pos.ZReport",
  serviceCharge: "Pos.TableServiceCharge",
  documents: "Orders.View",
  reservations: "Reservation.View",
  kassa: "CashRegister.View",
} as const;

export const ANY_POS_REPORT_PERMISSION: string[] = Array.from(new Set(Object.values(POS_REPORT_PERMISSION)));
