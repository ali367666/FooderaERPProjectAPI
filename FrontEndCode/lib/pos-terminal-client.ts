/** Browser-only helpers for the POS terminal's remembered company/restaurant context. */

const TERMINAL_KEY = "posTerminalContext";

/** Which POS this terminal opens: the restaurant POS (tables, orders) or the store POS (direct sales). */
export type PosTerminalMode = "restaurant" | "store";

export type PosTerminalContext = {
  /** Chosen when the terminal is set up; terminals set up before this existed follow the company's mode. */
  mode?: PosTerminalMode;
  companyId: number;
  companyCode: string;
  companyName: string;
  restaurantId: number | null;
  restaurantName: string | null;
  workstationId: number | null;
  workstationName: string | null;
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
      mode: parsed.mode === "store" || parsed.mode === "restaurant" ? parsed.mode : undefined,
      companyId: parsed.companyId,
      companyCode: parsed.companyCode,
      companyName: parsed.companyName,
      restaurantId: typeof parsed.restaurantId === "number" ? parsed.restaurantId : null,
      restaurantName: typeof parsed.restaurantName === "string" ? parsed.restaurantName : null,
      workstationId: typeof parsed.workstationId === "number" ? parsed.workstationId : null,
      workstationName: typeof parsed.workstationName === "string" ? parsed.workstationName : null,
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
