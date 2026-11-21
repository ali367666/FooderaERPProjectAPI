"use client";

import { useEffect, useState } from "react";
import { format, isValid, parse, parseISO } from "date-fns";
import { cn } from "@/lib/utils";

/** Shared look of the typed date and time fields. */
export const typedFieldClass =
  "h-9 rounded-md border border-input bg-transparent px-3 text-sm tabular-nums shadow-xs outline-none transition-[color,box-shadow] placeholder:text-muted-foreground dark:bg-input/30 focus-visible:border-ring focus-visible:ring-[3px] focus-visible:ring-ring/50";

/** Keeps only digits and puts the dots in as the user types: "04112026" → "04.11.2026". */
function maskDate(raw: string): string {
  const d = raw.replace(/\D/g, "").slice(0, 8);
  if (d.length <= 2) return d;
  if (d.length <= 4) return `${d.slice(0, 2)}.${d.slice(2)}`;
  return `${d.slice(0, 2)}.${d.slice(2, 4)}.${d.slice(4)}`;
}

/** "dd.MM.yyyy" → "yyyy-MM-dd", or null when it is not a real day (31.02.2026, half typed, …). */
function parseDate(text: string): string | null {
  if (!/^\d{2}\.\d{2}\.\d{4}$/.test(text)) return null;
  const date = parse(text, "dd.MM.yyyy", new Date());
  return isValid(date) && format(date, "dd.MM.yyyy") === text ? format(date, "yyyy-MM-dd") : null;
}

const display = (iso: string) => (iso ? format(parseISO(iso), "dd.MM.yyyy") : "");

/**
 * A date typed by hand as `dd.MM.yyyy` (the dots come by themselves). `value` is `yyyy-MM-dd`. The day is taken on
 * blur or Enter; a day that does not exist puts the last good one back.
 */
export function DateInput({
  value,
  onChange,
  className,
  "aria-label": ariaLabel,
}: {
  value: string;
  onChange: (value: string) => void;
  className?: string;
  "aria-label"?: string;
}) {
  const [text, setText] = useState(display(value));
  useEffect(() => setText(display(value)), [value]);

  const commit = () => {
    const iso = parseDate(text);
    if (iso === null) {
      setText(display(value));
      return;
    }
    if (iso !== value) onChange(iso);
  };

  return (
    <input
      type="text"
      inputMode="numeric"
      autoComplete="off"
      placeholder="gg.aa.iiii"
      aria-label={ariaLabel}
      value={text}
      onChange={(e) => setText(maskDate(e.target.value))}
      onBlur={commit}
      onKeyDown={(e) => {
        if (e.key === "Enter") {
          e.preventDefault();
          commit();
        }
      }}
      onFocus={(e) => e.target.select()}
      className={cn(typedFieldClass, "w-full min-w-0", className)}
    />
  );
}
