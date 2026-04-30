"use client";

import { useCallback, useEffect, useMemo, useRef, useState, type ReactNode } from "react";
import { ChevronDown } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { cn } from "@/lib/utils";

/** Shared compact select styling (h-8). Forms that need taller fields: `cn(FILTER_SELECT_CLASS, "h-10")`. */
export const FILTER_SELECT_CLASS =
  "flex h-8 w-full rounded-md border border-input bg-background px-2 py-1 text-xs ring-offset-background";

export type TableFilterUiKind =
  | "text"
  | "number"
  | "numberRange"
  | "select"
  | "status"
  | "dateRange"
  | "occupancy";

export type TableFilterDef<T> = {
  id: string;
  label: string;
  ui: TableFilterUiKind;
  options?: { value: string; label: string }[];
  /** Optional flex column sizing (e.g. wider date range): `min-w-*`, `flex-*`, `max-w-*`. */
  gridClassName?: string;
  match: (row: T, get: (key: string) => string) => boolean;
};

export function createInitialFilterState(defs: TableFilterDef<unknown>[]): Record<string, string> {
  const s: Record<string, string> = {};
  for (const d of defs) {
    if (d.ui === "numberRange") {
      s[`${d.id}:min`] = "";
      s[`${d.id}:max`] = "";
    } else if (d.ui === "dateRange") {
      s[`${d.id}:from`] = "";
      s[`${d.id}:to`] = "";
    } else if (d.ui === "status" || d.ui === "occupancy") {
      s[d.id] = "all";
    } else {
      s[d.id] = "";
    }
  }
  return s;
}

const inputCompactClass = "h-8 px-2 py-1 text-xs placeholder:text-xs";

type FilterControlProps<T> = {
  def: TableFilterDef<T>;
  state: Record<string, string>;
  setVal: (key: string, value: string) => void;
};

function FilterControl<T>({ def, state, setVal }: FilterControlProps<T>) {
  const wrap = (inner: ReactNode) => <div className="space-y-1">{inner}</div>;

  const labelCls = "text-xs font-medium text-muted-foreground leading-tight";

  switch (def.ui) {
    case "text":
    case "number":
      return wrap(
        <>
          <Label htmlFor={`filter-${def.id}`} className={labelCls}>
            {def.label}
          </Label>
          <Input
            id={`filter-${def.id}`}
            type="text"
            inputMode={def.ui === "number" ? "numeric" : undefined}
            className={inputCompactClass}
            placeholder={def.ui === "number" ? "e.g. 12" : "Contains…"}
            value={state[def.id] ?? ""}
            onChange={(e) => setVal(def.id, e.target.value)}
          />
        </>,
      );
    case "numberRange":
      return wrap(
        <>
          <Label className={labelCls}>{def.label} (min / max)</Label>
          <div className="flex gap-1.5">
            <Input
              type="number"
              className={inputCompactClass}
              placeholder="Min"
              value={state[`${def.id}:min`] ?? ""}
              onChange={(e) => setVal(`${def.id}:min`, e.target.value)}
            />
            <Input
              type="number"
              className={inputCompactClass}
              placeholder="Max"
              value={state[`${def.id}:max`] ?? ""}
              onChange={(e) => setVal(`${def.id}:max`, e.target.value)}
            />
          </div>
        </>,
      );
    case "select":
      return wrap(
        <>
          <Label htmlFor={`filter-${def.id}`} className={labelCls}>
            {def.label}
          </Label>
          <select
            id={`filter-${def.id}`}
            value={state[def.id] ?? ""}
            onChange={(e) => setVal(def.id, e.target.value)}
            className={FILTER_SELECT_CLASS}
          >
            <option value="">All</option>
            {(def.options ?? []).map((opt) => (
              <option key={opt.value} value={opt.value}>
                {opt.label}
              </option>
            ))}
          </select>
        </>,
      );
    case "status":
      return wrap(
        <>
          <Label htmlFor={`filter-${def.id}`} className={labelCls}>
            {def.label}
          </Label>
          <select
            id={`filter-${def.id}`}
            value={state[def.id] ?? "all"}
            onChange={(e) => setVal(def.id, e.target.value)}
            className={FILTER_SELECT_CLASS}
          >
            <option value="all">All</option>
            <option value="active">Active</option>
            <option value="inactive">Inactive</option>
          </select>
        </>,
      );
    case "occupancy":
      return wrap(
        <>
          <Label htmlFor={`filter-${def.id}`} className={labelCls}>
            {def.label}
          </Label>
          <select
            id={`filter-${def.id}`}
            value={state[def.id] ?? "all"}
            onChange={(e) => setVal(def.id, e.target.value)}
            className={FILTER_SELECT_CLASS}
          >
            <option value="all">All</option>
            <option value="available">Available</option>
            <option value="occupied">Occupied</option>
          </select>
        </>,
      );
    case "dateRange":
      return wrap(
        <>
          <Label className={labelCls}>{def.label} (from / to)</Label>
          <div className="flex gap-1.5">
            <Input
              type="date"
              className={inputCompactClass}
              value={state[`${def.id}:from`] ?? ""}
              onChange={(e) => setVal(`${def.id}:from`, e.target.value)}
            />
            <Input
              type="date"
              className={inputCompactClass}
              value={state[`${def.id}:to`] ?? ""}
              onChange={(e) => setVal(`${def.id}:to`, e.target.value)}
            />
          </div>
        </>,
      );
    default:
      return null;
  }
}

export type AdvancedTableFiltersProps<T> = {
  defs: TableFilterDef<T>[];
  data: T[];
  children: (filteredData: T[]) => ReactNode;
  title?: string;
  className?: string;
  /** Show a chevron to collapse filter fields (title row stays visible). */
  collapsible?: boolean;
  /** Initial collapsed state when `collapsible` is true. */
  defaultCollapsed?: boolean;
};

export function AdvancedTableFilters<T>({
  defs,
  data,
  children,
  title = "Column filters",
  className,
  collapsible = false,
  defaultCollapsed = false,
}: AdvancedTableFiltersProps<T>) {
  const defSignature = useMemo(() => defs.map((d) => `${d.id}:${d.ui}`).join("|"), [defs]);
  const defsRef = useRef(defs);
  defsRef.current = defs;
  const prevDefSignatureRef = useRef<string | null>(null);

  const [state, setState] = useState<Record<string, string>>(() =>
    createInitialFilterState(defs),
  );
  const [collapsed, setCollapsed] = useState(defaultCollapsed);

  useEffect(() => {
    if (prevDefSignatureRef.current === defSignature) return;
    prevDefSignatureRef.current = defSignature;
    setState(createInitialFilterState(defsRef.current));
  }, [defSignature]);

  const setVal = useCallback((key: string, value: string) => {
    setState((prev) => ({ ...prev, [key]: value }));
  }, []);

  const reset = useCallback(() => {
    setState(createInitialFilterState(defs));
  }, [defs]);

  const filteredData = useMemo(() => {
    const get = (key: string) => state[key] ?? "";
    return data.filter((row) => defs.every((def) => def.match(row, get)));
  }, [data, defs, state]);

  const showFields = !collapsible || !collapsed;

  return (
    <>
      <div
        className={cn(
          "mb-3 rounded-md border border-border/80 bg-muted/20 p-3 shadow-sm",
          className,
        )}
      >
        <div className="flex flex-wrap items-end gap-x-3 gap-y-2">
          {collapsible ? (
            <button
              type="button"
              className="flex h-8 shrink-0 items-center gap-1 rounded-md px-1 text-xs font-medium text-foreground hover:bg-muted/60"
              onClick={() => setCollapsed((c) => !c)}
              aria-expanded={showFields}
            >
              <ChevronDown
                className={cn("size-3.5 shrink-0 text-muted-foreground transition-transform", collapsed && "-rotate-90")}
                aria-hidden
              />
              {title}
            </button>
          ) : (
            <span className="flex h-8 shrink-0 items-center text-xs font-medium text-foreground">
              {title}
            </span>
          )}

          {showFields &&
            defs.map((def) => (
              <div
                key={def.id}
                className={cn(
                  "min-w-[180px] flex-1 basis-[180px]",
                  def.gridClassName ?? "max-w-[min(100%,320px)]",
                )}
              >
                <FilterControl def={def} state={state} setVal={setVal} />
              </div>
            ))}

          <Button
            type="button"
            variant="outline"
            className="h-8 shrink-0 px-2.5 text-xs font-medium ms-auto"
            onClick={reset}
          >
            Reset filters
          </Button>
        </div>
      </div>
      {children(filteredData)}
    </>
  );
}
