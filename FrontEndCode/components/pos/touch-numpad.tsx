"use client";

import { Delete } from "lucide-react";
import { Button } from "@/components/ui/button";

type TouchNumpadProps = {
  value: string;
  onChange: (value: string) => void;
  /** Allow a decimal point (amounts, weights); off for codes and counts. */
  allowDecimal?: boolean;
};

const KEYS = ["1", "2", "3", "4", "5", "6", "7", "8", "9"];

/** On-screen number pad for the touch screen mode ("Toxunuşlu ekran") — no physical keyboard needed. */
export function TouchNumpad({ value, onChange, allowDecimal = false }: TouchNumpadProps) {
  const press = (key: string) => {
    if (key === "." && (!allowDecimal || value.includes("."))) return;
    onChange(value === "0" && key !== "." ? key : value + key);
  };

  return (
    <div className="grid grid-cols-3 gap-2">
      {KEYS.map((k) => (
        <Button key={k} type="button" variant="outline" className="h-14 text-xl" onClick={() => press(k)}>
          {k}
        </Button>
      ))}
      <Button
        type="button"
        variant="outline"
        className="h-14 text-xl"
        disabled={!allowDecimal}
        onClick={() => press(".")}
      >
        {allowDecimal ? "." : ""}
      </Button>
      <Button type="button" variant="outline" className="h-14 text-xl" onClick={() => press("0")}>
        0
      </Button>
      <Button
        type="button"
        variant="outline"
        className="h-14"
        aria-label="Sil"
        onClick={() => onChange(value.slice(0, -1))}
        onDoubleClick={() => onChange("")}
      >
        <Delete className="h-5 w-5" />
      </Button>
    </div>
  );
}
