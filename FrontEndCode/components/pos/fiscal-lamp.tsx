"use client";

import { Lightbulb } from "lucide-react";
import { cn } from "@/lib/utils";
import { useFiscalMode } from "@/lib/pos-fiscal-mode";

/** Top-bar lamp: lit while the fiscal (tax) register is switched on. */
export function FiscalLamp({ className }: { className?: string }) {
  const on = useFiscalMode();
  return (
    <span
      className={cn(
        "flex h-8 w-8 items-center justify-center rounded-full transition-colors",
        on ? "bg-amber-100 text-amber-500 shadow-[0_0_12px_rgba(245,158,11,0.7)]" : "bg-muted text-muted-foreground/60",
        className,
      )}
      title={on ? "Vergi kassası işləyir — satış fiskal çekə düşür" : "Vergi kassası söndürülüb — adi kassa"}
      aria-label={on ? "Vergi kassası aktivdir" : "Vergi kassası söndürülüb"}
    >
      <Lightbulb className={cn("h-4 w-4", on && "fill-amber-400")} />
    </span>
  );
}
