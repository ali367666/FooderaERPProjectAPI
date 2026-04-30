"use client";

import type { ReactNode } from "react";

type DocumentHeaderProps = {
  title: string;
  documentNo: string;
  statusBadge: ReactNode;
  subtitle?: string;
  rightSlot?: ReactNode;
};

export function DocumentHeader({
  title,
  documentNo,
  statusBadge,
  subtitle,
  rightSlot,
}: DocumentHeaderProps) {
  return (
    <div className="flex flex-col gap-4 border-b border-border pb-4 sm:flex-row sm:items-start sm:justify-between">
      <div className="space-y-1 min-w-0">
        <div className="flex flex-wrap items-center gap-2">
          <h1 className="text-2xl font-semibold tracking-tight">{title}</h1>
          {statusBadge}
        </div>
        <p className="text-sm text-muted-foreground font-mono">{documentNo}</p>
        {subtitle ? <p className="text-xs text-muted-foreground">{subtitle}</p> : null}
      </div>
      {rightSlot ? <div className="flex flex-wrap gap-2 shrink-0">{rightSlot}</div> : null}
    </div>
  );
}
