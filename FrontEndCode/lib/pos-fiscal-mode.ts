"use client";

import { useSyncExternalStore } from "react";

/**
 * The lamp: while it is ON, whatever is rung up goes out through the fiscal (tax) register — the
 * receipt carries its VAT. While it is OFF (the default, and again after every closed sale) the sale
 * goes through the ordinary register. Kept in memory on purpose: a reload always starts with the lamp off.
 */
let fiscalOn = false;
const listeners = new Set<() => void>();

export function getFiscalMode(): boolean {
  return fiscalOn;
}

export function setFiscalMode(on: boolean): void {
  if (fiscalOn === on) return;
  fiscalOn = on;
  listeners.forEach((l) => l());
}

function subscribe(listener: () => void) {
  listeners.add(listener);
  return () => {
    listeners.delete(listener);
  };
}

export function useFiscalMode(): boolean {
  return useSyncExternalStore(subscribe, () => fiscalOn, () => false);
}
