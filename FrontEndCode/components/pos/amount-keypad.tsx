"use client";

import { useEffect, useRef } from "react";
import { Delete } from "lucide-react";
import { cn } from "@/lib/utils";

/** Up to 7 manat digits and 2 qəpik digits. */
const MAX_INT_DIGITS = 7;
const MAX_DEC_DIGITS = 2;

/**
 * Applies one calculator key to a money string ("12.5"). `fresh` means the value was filled in for the
 * cashier (the bill total, a quick note): the first digit typed replaces it instead of appending.
 */
export function applyAmountKey(value: string, key: string, fresh: boolean): string {
  if (key === "C") return "";
  if (key === "⌫") return fresh ? "" : value.slice(0, -1);

  const base = fresh ? "" : value;
  if (key === ".") {
    if (base.includes(".")) return base;
    return (base === "" ? "0" : base) + ".";
  }

  let next = base;
  for (const digit of key) {
    if (!/\d/.test(digit)) continue;
    const [intPart, decPart] = next.split(".");
    if (decPart !== undefined) {
      if (decPart.length >= MAX_DEC_DIGITS) break;
    } else if (intPart === "0") {
      next = "";
    } else if (intPart.length >= MAX_INT_DIGITS) {
      break;
    }
    next += digit;
  }
  return next;
}

type AmountKeypadProps = {
  value: string;
  fresh: boolean;
  onChange: (value: string) => void;
  disabled?: boolean;
};

const KEYS: { key: string; label?: string; tone?: "action" }[] = [
  { key: "7" }, { key: "8" }, { key: "9" }, { key: "⌫", tone: "action" },
  { key: "4" }, { key: "5" }, { key: "6" }, { key: "C", tone: "action" },
  { key: "1" }, { key: "2" }, { key: "3" }, { key: "00" },
];

/** Calculator-style keypad for money (manat + qəpik) — POS screens have no keyboard. */
export function AmountKeypad({ value, fresh, onChange, disabled }: AmountKeypadProps) {
  const press = (key: string) => onChange(applyAmountKey(value, key, fresh));

  const keyClass = (tone?: "action") =>
    cn(
      "flex h-14 select-none items-center justify-center rounded-xl border text-2xl font-semibold shadow-sm transition-all active:scale-95 disabled:pointer-events-none disabled:opacity-50",
      tone === "action"
        ? "border-border bg-muted text-foreground hover:bg-muted/70"
        : "border-border bg-card text-card-foreground hover:bg-accent",
    );

  return (
    <div className="grid grid-cols-4 gap-2" data-keypad>
      {KEYS.map(({ key, tone }) => (
        <button
          key={key}
          type="button"
          tabIndex={-1}
          disabled={disabled}
          className={keyClass(tone)}
          aria-label={key === "⌫" ? "Sil" : key === "C" ? "Təmizlə" : key}
          onClick={() => press(key)}
        >
          {key === "⌫" ? <Delete className="h-6 w-6" /> : key}
        </button>
      ))}
      <button type="button" tabIndex={-1} disabled={disabled} className={cn(keyClass(), "col-span-2")} onClick={() => press("0")}>
        0
      </button>
      <button
        type="button"
        tabIndex={-1}
        disabled={disabled}
        className={cn(keyClass(), "col-span-2")}
        aria-label="Qəpik (onluq nöqtə)"
        onClick={() => press(".")}
      >
        .
      </button>
    </div>
  );
}

export type AmountStatus = {
  /** ok = green (change), short = red (not enough), partial = amber (part of the bill). */
  tone: "ok" | "short" | "partial";
  label: string;
  amount: number;
};

/** Cash status for a payment that must cover `due`: the change, or how much is missing. */
export function cashStatus(received: number, due: number): AmountStatus {
  const r = (n: number) => Math.round((n + Number.EPSILON) * 100) / 100;
  return received >= due
    ? { tone: "ok", label: "Qaytarılan", amount: r(received - due) }
    : { tone: "short", label: "Çatmır", amount: r(due - received) };
}

type AmountCalculatorProps = {
  /** What is to be paid — shown on top and used by "Tam". */
  due: number;
  value: string;
  /** The value was filled in (bill total / quick note): the first key typed replaces it. */
  fresh: boolean;
  onChange: (value: string, fresh: boolean) => void;
  label?: string;
  /** Cash: the round notes above the bill as one-tap buttons. */
  quickNotes?: boolean;
  /** Coloured strip under the display (the change, the missing part...). */
  status?: AmountStatus | null;
  /** Enter on a physical keyboard. */
  onEnter?: () => void;
  /** Take the keyboard focus when shown (no text input, so no caret / on-screen keyboard). */
  autoFocus?: boolean;
  disabled?: boolean;
};

const STATUS_CLASS: Record<AmountStatus["tone"], string> = {
  ok: "bg-emerald-600",
  short: "bg-destructive",
  partial: "bg-amber-500",
};

/**
 * Calculator for the money handed over: a display (due / received / change), one-tap amounts and a keypad
 * with qəpik. POS screens have no keyboard — a physical one still works (digits, "." / ",", Backspace,
 * Delete, Enter).
 */
export function AmountCalculator({
  due,
  value,
  fresh,
  onChange,
  label = "Alınan məbləğ",
  quickNotes = false,
  status,
  onEnter,
  autoFocus = true,
  disabled,
}: AmountCalculatorProps) {
  const panelRef = useRef<HTMLDivElement | null>(null);
  const quick = quickNotes ? quickCashAmounts(due) : [];

  useEffect(() => {
    if (autoFocus) panelRef.current?.focus();
  }, [autoFocus]);

  const handleKey = (e: React.KeyboardEvent<HTMLDivElement>) => {
    const target = e.target as HTMLElement;
    if (target !== e.currentTarget && !target.closest("[data-keypad]")) return;
    let key: string | null = null;
    if (/^\d$/.test(e.key)) key = e.key;
    else if (e.key === "." || e.key === ",") key = ".";
    else if (e.key === "Backspace") key = "⌫";
    else if (e.key === "Delete") key = "C";
    else if (e.key === "Enter") {
      e.preventDefault();
      if (!disabled) onEnter?.();
      return;
    }
    if (key == null || disabled) return;
    e.preventDefault();
    onChange(applyAmountKey(value, key, fresh), false);
  };

  return (
    <div ref={panelRef} tabIndex={-1} onKeyDown={handleKey} className="space-y-3 outline-none">
      <div className="overflow-hidden rounded-xl border bg-muted/40">
        <div className="flex items-center justify-between px-4 pt-3 text-sm text-muted-foreground">
          <span>Ödəniləcək</span>
          <span className="font-semibold tabular-nums text-foreground">{due.toFixed(2)} ₼</span>
        </div>
        <div className="px-4 pb-3 pt-1">
          <div className="text-xs text-muted-foreground">{label}</div>
          <div
            aria-live="polite"
            className={cn("truncate text-right text-4xl font-bold tabular-nums", value === "" && "text-muted-foreground")}
          >
            {value === "" ? "0.00" : value} <span className="text-2xl">₼</span>
          </div>
        </div>
        {status && (
          <div className={cn("flex items-center justify-between gap-3 px-4 py-3 text-white", STATUS_CLASS[status.tone])}>
            <span className={cn("font-semibold", status.tone === "partial" ? "text-sm" : "text-base")}>{status.label}</span>
            <span className="text-3xl font-bold tabular-nums">{status.amount.toFixed(2)} ₼</span>
          </div>
        )}
      </div>

      <div className="grid grid-cols-5 gap-2">
        <button
          type="button"
          tabIndex={-1}
          disabled={disabled || due <= 0}
          onClick={() => onChange(due.toFixed(2), true)}
          className="flex h-11 items-center justify-center rounded-lg border-2 border-primary/50 bg-primary/10 text-sm font-semibold text-primary transition-all active:scale-95 disabled:opacity-50"
        >
          Tam
        </button>
        {quick.map((v) => (
          <button
            key={v}
            type="button"
            tabIndex={-1}
            disabled={disabled}
            onClick={() => onChange(v.toFixed(2), true)}
            className="flex h-11 items-center justify-center rounded-lg border bg-card text-sm font-semibold tabular-nums shadow-sm transition-all hover:bg-accent active:scale-95 disabled:opacity-50"
          >
            {v} ₼
          </button>
        ))}
      </div>

      <AmountKeypad value={value} fresh={fresh} onChange={(v) => onChange(v, false)} disabled={disabled} />
    </div>
  );
}

/** The bill and the next round notes above it — one tap for the money the guest hands over. */
export function quickCashAmounts(due: number, count = 4): number[] {
  if (due <= 0) return [];
  const result = new Set<number>();
  for (const step of [1, 5, 10, 20, 50, 100, 200]) {
    const v = Math.ceil(due / step) * step;
    if (v > due + 0.001) result.add(v);
  }
  return [...result].sort((a, b) => a - b).slice(0, count);
}
