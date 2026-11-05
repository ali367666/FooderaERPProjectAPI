/**
 * Fired on `window` when a screen other than the POS layout closes the shift (e.g. "Hesabatı bağla" in the
 * report modal). `detail` is the Z report; the layout drops its shift and shows the Z report.
 */
export const POS_SHIFT_CLOSED_EVENT = "pos:shift-closed";
