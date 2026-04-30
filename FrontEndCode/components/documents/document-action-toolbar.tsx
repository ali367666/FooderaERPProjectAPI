"use client";

import type { LucideIcon } from "lucide-react";
import { Button } from "@/components/ui/button";
import { cn } from "@/lib/utils";

export type DocumentToolbarAction = {
  id: string;
  label: string;
  icon: LucideIcon;
  /** When false and a row is selected, the button stays disabled. */
  enabled: boolean;
  /** Explains why the action is disabled when a row is selected (ignored when !hasSelection). */
  disabledReason?: string;
  variant?: "default" | "destructive" | "secondary" | "outline" | "ghost" | "link";
  onClick: () => void;
};

type DocumentActionToolbarProps = {
  /** Whether a document row is selected in the list. */
  hasSelection: boolean;
  /** Optional subtitle when selected (e.g. document number). */
  selectedSummary?: string | null;
  actions: DocumentToolbarAction[];
  className?: string;
};

function actionTitle(
  hasSelection: boolean,
  action: DocumentToolbarAction,
): string | undefined {
  if (!hasSelection) return "Select a row to enable actions";
  if (action.enabled) return action.label;
  return action.disabledReason?.trim() || "This action is not available for the current status.";
}

export function DocumentActionToolbar({
  hasSelection,
  selectedSummary,
  actions,
  className,
}: DocumentActionToolbarProps) {
  return (
    <div
      className={cn(
        "rounded-lg border border-border bg-muted/25 px-3 py-2.5 shadow-sm",
        className,
      )}
    >
      <div className="flex flex-col gap-2 sm:flex-row sm:items-center sm:justify-between sm:gap-4">
        <div className="min-w-0 text-sm text-muted-foreground">
          {!hasSelection ? (
            <span>Select a row to enable actions</span>
          ) : selectedSummary ? (
            <span className="text-foreground">
              Selected: <span className="font-medium tabular-nums">{selectedSummary}</span>
            </span>
          ) : (
            <span className="text-foreground">1 row selected</span>
          )}
        </div>
        <div className="flex flex-wrap items-center gap-2">
          {actions.map((action) => {
            const Icon = action.icon;
            const disabled = !hasSelection || !action.enabled;
            return (
              <Button
                key={action.id}
                type="button"
                variant={action.variant ?? "outline"}
                size="sm"
                className="h-8 gap-1.5"
                disabled={disabled}
                title={actionTitle(hasSelection, action)}
                onClick={action.onClick}
              >
                <Icon className="size-4 shrink-0" aria-hidden />
                {action.label}
              </Button>
            );
          })}
        </div>
      </div>
    </div>
  );
}
