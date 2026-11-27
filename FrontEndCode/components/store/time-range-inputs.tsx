"use client";

import { usePathname } from "next/navigation";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";

/**
 * Time-of-day inputs next to a date range (from this time on the first day to the end of that minute on the last day). Shown in the admin panel
 * only - the store terminal itself (/store) keeps whole days.
 */
export function TimeRangeInputs({
  idPrefix,
  fromTime,
  toTime,
  onFromTime,
  onToTime,
}: {
  idPrefix: string;
  fromTime: string;
  toTime: string;
  onFromTime: (v: string) => void;
  onToTime: (v: string) => void;
}) {
  const inDashboard = usePathname().startsWith("/dashboard");
  if (!inDashboard) return null;
  return (
    <>
      <div className="space-y-1">
        <Label htmlFor={`${idPrefix}-from-time`}>Saat: başlanğıc</Label>
        <Input id={`${idPrefix}-from-time`} type="time" className="w-28" value={fromTime} onChange={(e) => onFromTime(e.target.value)} />
      </div>
      <div className="space-y-1">
        <Label htmlFor={`${idPrefix}-to-time`}>Saat: son</Label>
        <Input id={`${idPrefix}-to-time`} type="time" className="w-28" value={toTime} onChange={(e) => onToTime(e.target.value)} />
      </div>
    </>
  );
}

/** Whether the times are on offer (they are sent only from the admin panel; the store terminal keeps whole days). */
export function useTimeRangeEnabled(): boolean {
  return usePathname().startsWith("/dashboard");
}
