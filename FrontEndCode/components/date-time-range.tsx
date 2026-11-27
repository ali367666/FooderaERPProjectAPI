"use client";

import { useCallback, useState } from "react";
import { matchesDateTimeRange } from "@/components/advanced-table-filters";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { cn } from "@/lib/utils";

const EMPTY = { from: "", fromTime: "", to: "", toTime: "" };

type Range = typeof EMPTY;

/**
 * Date and time range over a history that is already loaded: from `fromTime` on the first day to the end of the `toTime` minute on the last day.
 * With no day chosen the time of day applies to every day. Empty = everything.
 */
export function useDateTimeRange() {
  const [range, setRange] = useState<Range>(EMPTY);
  const active = Object.values(range).some(Boolean);

  const matches = useCallback(
    (iso: string | null | undefined) =>
      matchesDateTimeRange(iso ? Date.parse(iso) : 0, (key) => range[key.slice("range:".length) as keyof Range] ?? "", "range"),
    [range],
  );

  return { range, setRange, active, matches };
}

export function DateTimeRangeBar({
  value,
  timeOnly = false,
  className,
}: {
  value: ReturnType<typeof useDateTimeRange>;
  /** For a view that already shows one day: only the hours are chosen. */
  timeOnly?: boolean;
  className?: string;
}) {
  const { range, setRange, active } = value;

  const field = (key: keyof Range, label: string, type: "date" | "time") => (
    <div className="space-y-1">
      <Label className="text-xs text-muted-foreground">{label}</Label>
      <Input
        type={type}
        aria-label={label}
        className={cn("h-9", type === "date" ? "w-40" : "w-28")}
        value={range[key]}
        onChange={(e) => setRange((r) => ({ ...r, [key]: e.target.value }))}
      />
    </div>
  );

  return (
    <div className={cn("flex flex-wrap items-end gap-3", className)}>
      {!timeOnly && field("from", "Başlanğıc tarix", "date")}
      {field("fromTime", "Saat: başlanğıc", "time")}
      {!timeOnly && field("to", "Son tarix", "date")}
      {field("toTime", "Saat: son", "time")}
      {active && (
        <Button type="button" variant="ghost" size="sm" onClick={() => setRange(EMPTY)}>
          Təmizlə
        </Button>
      )}
    </div>
  );
}
