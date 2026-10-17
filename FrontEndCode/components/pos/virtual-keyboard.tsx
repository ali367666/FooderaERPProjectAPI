"use client";

import { Delete, Keyboard, X } from "lucide-react";
import { cn } from "@/lib/utils";

// Azerbaijani layout (lower case — keys print upper case), plus W for Latin names.
const ROWS = [
  ["1", "2", "3", "4", "5", "6", "7", "8", "9", "0"],
  ["q", "ü", "e", "r", "t", "y", "u", "i", "o", "p", "ö", "ğ"],
  ["a", "s", "d", "f", "g", "h", "j", "k", "l", "ı", "ə"],
  ["z", "x", "c", "v", "b", "n", "m", "ç", "ş", "w"],
];

/**
 * On-screen keyboard for touch terminals. Keys never take focus (mouse-down is cancelled), so the
 * input it types into stays focused and a physical keyboard keeps working alongside it.
 */
export function VirtualKeyboard({
  value,
  onChange,
  onClose,
  className,
}: {
  value: string;
  onChange: (value: string) => void;
  onClose: () => void;
  className?: string;
}) {
  const keyClass =
    "flex h-12 min-w-0 flex-1 select-none items-center justify-center rounded-lg border bg-background text-lg font-semibold text-foreground shadow-sm transition-all hover:bg-muted active:scale-95 active:bg-muted";

  return (
    <div
      className={cn("space-y-1.5 rounded-2xl border bg-card p-3 shadow-2xl", className)}
      onMouseDown={(e) => e.preventDefault()}
    >
      {ROWS.map((row, i) => (
        <div key={i} className="flex gap-1.5" style={{ paddingInline: `${i * 1.25}%` }}>
          {row.map((key) => (
            <button key={key} type="button" tabIndex={-1} className={keyClass} onClick={() => onChange(value + key)}>
              {key.toLocaleUpperCase("az")}
            </button>
          ))}
          {i === 0 && (
            <button
              type="button"
              tabIndex={-1}
              aria-label="Sil"
              className={cn(keyClass, "flex-[1.6] bg-muted/60")}
              onClick={() => onChange(value.slice(0, -1))}
            >
              <Delete className="h-5 w-5" />
            </button>
          )}
        </div>
      ))}
      <div className="flex gap-1.5">
        <button
          type="button"
          tabIndex={-1}
          className={cn(keyClass, "flex-[2] bg-muted/60 text-sm")}
          onClick={() => onChange("")}
        >
          Təmizlə
        </button>
        <button
          type="button"
          tabIndex={-1}
          aria-label="Boşluq"
          className={cn(keyClass, "flex-[6]")}
          onClick={() => onChange(value + " ")}
        >
          <Keyboard className="h-5 w-5 text-muted-foreground" />
        </button>
        <button
          type="button"
          tabIndex={-1}
          className={cn(keyClass, "flex-[2] border-primary bg-primary text-sm text-primary-foreground hover:bg-primary/90")}
          onClick={onClose}
        >
          <X className="mr-1 h-4 w-4" />
          Bağla
        </button>
      </div>
    </div>
  );
}
