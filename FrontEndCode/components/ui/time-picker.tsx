"use client";

import { useEffect, useRef, useState } from "react";
import { Clock, X } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Popover, PopoverAnchor, PopoverContent, PopoverTrigger } from "@/components/ui/popover";
import { typedFieldClass } from "@/components/ui/date-input";
import { cn } from "@/lib/utils";

const HOURS = Array.from({ length: 24 }, (_, i) => String(i).padStart(2, "0"));

function minutesFor(step: number, current?: string): string[] {
  const list = Array.from({ length: Math.ceil(60 / step) }, (_, i) => String(i * step).padStart(2, "0"));
  // A typed value (e.g. 14:07) still shows as chosen.
  return current && !list.includes(current) ? [...list, current].sort() : list;
}

/** Keeps only digits and puts the colon in as the user types: "1407" → "14:07"; a first digit above 2 is an hour, "9" → "09". */
function maskTime(raw: string): string {
  let d = raw.replace(/\D/g, "");
  if (d.length > 0 && Number(d[0]) > 2) d = `0${d}`;
  d = d.slice(0, 4);
  return d.length <= 2 ? d : `${d.slice(0, 2)}:${d.slice(2)}`;
}

/** The typed text as `HH:mm`, "" when it is empty, or null when it is not a time ("25:00", "14:75"). */
function parseTime(text: string): string | null {
  if (!text) return "";
  const [h, m] = text.split(":");
  const hour = Number(h);
  const minute = Number(m || "0");
  if (!Number.isInteger(hour) || !Number.isInteger(minute) || hour > 23 || minute > 59) return null;
  return `${String(hour).padStart(2, "0")}:${String(minute).padStart(2, "0")}`;
}

/** One scrollable column of hours or minutes; the chosen cell scrolls into view when it opens. */
function TimeColumn({
  label,
  items,
  selected,
  onPick,
}: {
  label: string;
  items: string[];
  selected?: string;
  onPick: (v: string) => void;
}) {
  const listRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    const el = listRef.current?.querySelector<HTMLElement>("[data-selected=true]");
    el?.scrollIntoView({ block: "center" });
  }, []);

  return (
    <div className="flex flex-col">
      <span className="pb-1.5 text-center text-[11px] font-medium uppercase tracking-wide text-muted-foreground">{label}</span>
      <div ref={listRef} className="flex h-56 w-14 flex-col gap-0.5 overflow-y-auto overscroll-contain pr-0.5 [scrollbar-width:thin]">
        {items.map((v) => {
          const active = v === selected;
          return (
            <button
              key={v}
              type="button"
              data-selected={active}
              onClick={() => onPick(v)}
              className={cn(
                "h-8 shrink-0 rounded-md text-sm tabular-nums transition-colors",
                active ? "bg-primary font-semibold text-primary-foreground" : "hover:bg-accent hover:text-accent-foreground",
              )}
            >
              {v}
            </button>
          );
        })}
      </div>
    </div>
  );
}

/**
 * An `HH:mm` field: the time can be typed to the minute (the colon comes by itself), or picked from hour and minute
 * columns behind the clock button. `value` is "" when no time is chosen. A typed time is taken on blur or Enter;
 * one that is not a time puts the last good value back.
 */
export function TimePicker({
  value,
  onChange,
  placeholder = "--:--",
  minuteStep = 5,
  className,
  "aria-label": ariaLabel,
}: {
  value: string;
  onChange: (value: string) => void;
  placeholder?: string;
  minuteStep?: number;
  className?: string;
  "aria-label"?: string;
}) {
  const [open, setOpen] = useState(false);
  const [text, setText] = useState(value);
  useEffect(() => setText(value), [value]);
  const [hour, minute] = value ? value.split(":") : [undefined, undefined];

  const commit = () => {
    const time = parseTime(text);
    if (time === null) {
      setText(value);
      return;
    }
    setText(time);
    if (time !== value) onChange(time);
  };

  return (
    <Popover open={open} onOpenChange={setOpen}>
      <PopoverAnchor asChild>
        <div className={cn("relative flex items-center", className)}>
          <input
            type="text"
            inputMode="numeric"
            autoComplete="off"
            placeholder={placeholder}
            aria-label={ariaLabel}
            value={text}
            onChange={(e) => setText(maskTime(e.target.value))}
            onBlur={commit}
            onKeyDown={(e) => {
              if (e.key === "Enter") {
                e.preventDefault();
                commit();
              }
            }}
            onFocus={(e) => e.target.select()}
            className={cn(typedFieldClass, "w-full min-w-0 pr-9")}
          />
          <PopoverTrigger asChild>
            <button
              type="button"
              aria-label="Saatı siyahıdan seç"
              className="absolute right-1 flex h-7 w-7 items-center justify-center rounded-md text-muted-foreground transition-colors hover:bg-accent hover:text-accent-foreground"
            >
              <Clock className="h-4 w-4" />
            </button>
          </PopoverTrigger>
        </div>
      </PopoverAnchor>
      <PopoverContent className="w-auto p-3" align="start">
        <div className="flex gap-2">
          <TimeColumn label="Saat" items={HOURS} selected={hour} onPick={(h) => onChange(`${h}:${minute ?? "00"}`)} />
          <div className="w-px self-stretch bg-border" />
          <TimeColumn
            label="Dəq"
            items={minutesFor(minuteStep, minute)}
            selected={minute}
            onPick={(m) => onChange(`${hour ?? "00"}:${m}`)}
          />
        </div>
        <div className="mt-3 flex items-center justify-between gap-2 border-t pt-3">
          <Button
            type="button"
            variant="ghost"
            size="sm"
            className="h-8 px-2 text-muted-foreground"
            disabled={!value}
            onClick={() => onChange("")}
          >
            <X className="mr-1 h-3.5 w-3.5" />
            Təmizlə
          </Button>
          <Button type="button" size="sm" className="h-8" onClick={() => setOpen(false)}>
            Hazır
          </Button>
        </div>
      </PopoverContent>
    </Popover>
  );
}
