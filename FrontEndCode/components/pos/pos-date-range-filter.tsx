"use client";

import { addDays, format, parseISO } from "date-fns";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";

/** Inclusive range of `yyyy-MM-dd` days on the terminal's own calendar. */
export type PosDateRange = { from: string; to: string };

/** The terminal's own calendar day (toISOString would give yesterday's date after midnight in UTC+4). */
export function todayIso(): string {
  return format(new Date(), "yyyy-MM-dd");
}

function shiftDay(iso: string, days: number): string {
  return format(addDays(parseISO(iso), days), "yyyy-MM-dd");
}

export function todayRange(): PosDateRange {
  const today = todayIso();
  return { from: today, to: today };
}

export function formatRange({ from, to }: PosDateRange): string {
  const fmt = (iso: string) => format(parseISO(iso), "dd.MM.yyyy");
  return from === to ? fmt(from) : `${fmt(from)} – ${fmt(to)}`;
}

const PRESETS: { label: string; range: () => PosDateRange }[] = [
  { label: "Bu gün", range: todayRange },
  {
    label: "Dünən",
    range: () => {
      const d = shiftDay(todayIso(), -1);
      return { from: d, to: d };
    },
  },
  { label: "7 gün", range: () => ({ from: shiftDay(todayIso(), -6), to: todayIso() }) },
  { label: "30 gün", range: () => ({ from: shiftDay(todayIso(), -29), to: todayIso() }) },
];

/** Shared POS date filter: quick presets plus a start–end pair of date inputs. */
export function PosDateRangeFilter({
  value,
  onChange,
}: {
  value: PosDateRange;
  onChange: (range: PosDateRange) => void;
}) {
  // A cleared input keeps the old day; picking a start after the end (or the reverse) moves the other end along.
  const setFrom = (from: string) => from && onChange({ from, to: from > value.to ? from : value.to });
  const setTo = (to: string) => to && onChange({ from: to < value.from ? to : value.from, to });

  return (
    <div className="flex flex-wrap items-center gap-2">
      {PRESETS.map((p) => {
        const r = p.range();
        const active = r.from === value.from && r.to === value.to;
        return (
          <Button key={p.label} variant={active ? "default" : "outline"} size="sm" onClick={() => onChange(r)}>
            {p.label}
          </Button>
        );
      })}
      <Input
        type="date"
        className="w-40"
        value={value.from}
        onChange={(e) => setFrom(e.target.value)}
        aria-label="Başlanğıc tarix"
      />
      <span className="text-sm text-muted-foreground">—</span>
      <Input
        type="date"
        className="w-40"
        value={value.to}
        onChange={(e) => setTo(e.target.value)}
        aria-label="Son tarix"
      />
    </div>
  );
}
