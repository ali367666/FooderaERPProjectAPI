/** Browser-only helpers for the POS terminal's remembered company/restaurant context. */

const TERMINAL_KEY = "posTerminalContext";

export type PosTerminalContext = {
  companyId: number;
  companyCode: string;
  companyName: string;
  restaurantId: number | null;
  restaurantName: string | null;
};

export function getPosTerminalContext(): PosTerminalContext | null {
  if (typeof window === "undefined") return null;
  const raw = localStorage.getItem(TERMINAL_KEY);
  if (!raw) return null;
  try {
    const parsed = JSON.parse(raw) as Partial<PosTerminalContext>;
    if (typeof parsed.companyId !== "number" || !parsed.companyCode || !parsed.companyName) {
      return null;
    }
    return {
      companyId: parsed.companyId,
      companyCode: parsed.companyCode,
      companyName: parsed.companyName,
      restaurantId: typeof parsed.restaurantId === "number" ? parsed.restaurantId : null,
      restaurantName: typeof parsed.restaurantName === "string" ? parsed.restaurantName : null,
    };
  } catch {
    return null;
  }
}

export function savePosTerminalContext(context: PosTerminalContext): void {
  localStorage.setItem(TERMINAL_KEY, JSON.stringify(context));
}

export function clearPosTerminalContext(): void {
  localStorage.removeItem(TERMINAL_KEY);
}
