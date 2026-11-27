"use client";

import { useState } from "react";
import { addDays, format, parseISO } from "date-fns";
import { az } from "date-fns/locale";
import { CalendarDays, ChevronDown, RotateCcw, X } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Calendar } from "@/components/ui/calendar";
import { Popover, PopoverContent, PopoverTrigger } from "@/components/ui/popover";
import { DateInput } from "@/components/ui/date-input";
import { TimePicker } from "@/components/ui/time-picker";

/**
 * Inclusive range of `yyyy-MM-dd` days on the terminal's own calendar, optionally narrowed by `HH:mm` times:
 * from `fromTime` on the first day to the end of the `toTime` minute on the last day ("" or missing = no time).
 */
export type PosDateRange = { from: string; to: string; fromTime?: string; toTime?: string };

/** Whether the range is narrowed by a time of day. */
export function hasRangeTime(range: PosDateRange): boolean {
  return !!(range.fromTime || range.toTime);
}

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

export function formatRange(range: PosDateRange): string {
  const { from, to, fromTime, toTime } = range;
  const fmt = (iso: string) => format(parseISO(iso), "dd.MM.yyyy");
  if (!hasRangeTime(range)) return from === to ? fmt(from) : `${fmt(from)} – ${fmt(to)}`;
  // The times belong to the first and the last day: "04.11.2026 14:00 – 18:30" or "04.11.2026 14:00 – 06.11.2026 18:30".
  const start = fromTime ? `${fmt(from)} ${fromTime}` : fmt(from);
  const end = toTime ? (from === to ? toTime : `${fmt(to)} ${toTime}`) : from === to ? "" : fmt(to);
  return end ? `${start} – ${end}` : `${start}-dən`;
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

/** One side of the range: its own calendar, the day typed by hand and, with `withTime`, the time. */
function RangeSide({
  label,
  day,
  onDay,
  month,
  onMonth,
  time,
  onTime,
  timePlaceholder,
  withTime,
}: {
  label: string;
  day: string;
  onDay: (iso: string) => void;
  month: Date;
  onMonth: (month: Date) => void;
  time: string;
  onTime: (time: string) => void;
  timePlaceholder: string;
  withTime: boolean;
}) {
  return (
    <div className="flex flex-col">
      <span className="px-3 pt-3 text-xs font-medium text-muted-foreground">{label}</span>
      {/* Each calendar holds one day: the left one the first day, the right one the last day. */}
      <Calendar
        mode="single"
        required
        locale={az}
        weekStartsOn={1}
        month={month}
        onMonthChange={onMonth}
        selected={parseISO(day)}
        onSelect={(d) => onDay(format(d, "yyyy-MM-dd"))}
        className="p-3 pt-2"
      />
      <div className="flex flex-col gap-1.5 px-3 pb-3">
        <DateInput value={day} onChange={onDay} aria-label={`${label} tarix`} />
        {withTime && <TimePicker value={time} onChange={onTime} placeholder={timePlaceholder} aria-label={`${label} saat`} />}
      </div>
    </div>
  );
}

/**
 * Shared POS date filter: quick presets plus one button that opens two calendars, the first day on the left and the
 * last day on the right. Days (and with `withTime` the start and end times) can also be typed by hand, to the minute;
 * the start time applies to the first day and the end time to the last day.
 * Changes are a draft until "Tətbiq et", so the list is not reloaded on every click.
 */
export function PosDateRangeFilter({
  value,
  onChange,
  withTime = false,
}: {
  value: PosDateRange;
  onChange: (range: PosDateRange) => void;
  withTime?: boolean;
}) {
  const [open, setOpen] = useState(false);
  const [draft, setDraft] = useState<PosDateRange>(value);
  // The months the two calendars show; a picked or typed day brings its calendar there.
  const [fromMonth, setFromMonth] = useState(() => parseISO(value.from));
  const [toMonth, setToMonth] = useState(() => parseISO(value.to));

  const reset = (range: PosDateRange) => {
    setDraft(range);
    setFromMonth(parseISO(range.from));
    setToMonth(parseISO(range.to));
  };

  const openChange = (next: boolean) => {
    if (next) reset(value);
    setOpen(next);
  };

  // The range stays in order: a start after the end pulls the end with it, and an end before the start the start.
  const setFrom = (iso: string) => {
    setDraft((d) => ({ ...d, from: iso, to: iso > d.to ? iso : d.to }));
    setFromMonth(parseISO(iso));
    if (iso > draft.to) setToMonth(parseISO(iso));
  };
  const setTo = (iso: string) => {
    setDraft((d) => ({ ...d, to: iso, from: iso < d.from ? iso : d.from }));
    setToMonth(parseISO(iso));
    if (iso < draft.from) setFromMonth(parseISO(iso));
  };

  const apply = () => {
    onChange(draft);
    setOpen(false);
  };

  const presetActive = PRESETS.some((p) => {
    const r = p.range();
    return r.from === value.from && r.to === value.to;
  });

  return (
    <div className="flex flex-wrap items-center gap-2">
      {PRESETS.map((p) => {
        const r = p.range();
        const active = r.from === value.from && r.to === value.to;
        return (
          <Button
            key={p.label}
            variant={active ? "default" : "outline"}
            size="sm"
            onClick={() => onChange({ ...r, fromTime: value.fromTime, toTime: value.toTime })}
          >
            {p.label}
          </Button>
        );
      })}

      <Popover open={open} onOpenChange={openChange}>
        <PopoverTrigger asChild>
          <Button
            variant={presetActive && !hasRangeTime(value) ? "outline" : "secondary"}
            size="sm"
            className="gap-2 font-normal tabular-nums"
            aria-label="Tarix və saat seç"
          >
            <CalendarDays className="h-4 w-4 opacity-70" />
            {formatRange(value)}
            <ChevronDown className="h-3.5 w-3.5 opacity-50" />
          </Button>
        </PopoverTrigger>
        <PopoverContent className="max-h-[var(--radix-popover-content-available-height)] w-auto overflow-y-auto p-0" align="start">
          <div className="flex flex-col divide-y sm:flex-row sm:divide-x sm:divide-y-0">
            <RangeSide
              label="Başlanğıc"
              day={draft.from}
              onDay={setFrom}
              month={fromMonth}
              onMonth={setFromMonth}
              time={draft.fromTime ?? ""}
              onTime={(t) => setDraft((d) => ({ ...d, fromTime: t }))}
              timePlaceholder="Gün başı"
              withTime={withTime}
            />
            <RangeSide
              label="Son"
              day={draft.to}
              onDay={setTo}
              month={toMonth}
              onMonth={setToMonth}
              time={draft.toTime ?? ""}
              onTime={(t) => setDraft((d) => ({ ...d, toTime: t }))}
              timePlaceholder="Gün sonu"
              withTime={withTime}
            />
          </div>

          <div className="flex items-center justify-between gap-2 border-t p-3">
            <Button variant="ghost" size="sm" className="text-muted-foreground" onClick={() => reset(todayRange())}>
              <RotateCcw className="mr-1.5 h-3.5 w-3.5" />
              Sıfırla
            </Button>
            <Button size="sm" onClick={apply}>
              Tətbiq et
            </Button>
          </div>
        </PopoverContent>
      </Popover>

      {withTime && hasRangeTime(value) && (
        <Button
          variant="ghost"
          size="sm"
          className="h-8 gap-1 px-2 text-muted-foreground"
          onClick={() => onChange({ ...value, fromTime: "", toTime: "" })}
          title="Saat filtrini götür"
        >
          <X className="h-3.5 w-3.5" />
          Saatı götür
        </Button>
      )}
    </div>
  );
}
