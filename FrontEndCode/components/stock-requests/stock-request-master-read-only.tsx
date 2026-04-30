"use client";

import type { ReactNode } from "react";
import { StockRequestStatusBadge } from "@/components/documents/document-status-badge";
import type { StockRequestStatusValue } from "@/lib/services/stock-request-service";
import type { StockRequestDetailLabels } from "@/lib/stock-request-detail-mapper";
import { cn } from "@/lib/utils";

type StockRequestMasterReadOnlyProps = {
  status: StockRequestStatusValue;
  labels: StockRequestDetailLabels;
  className?: string;
};

function Field({
  label,
  children,
}: {
  label: string;
  children: ReactNode;
}) {
  return (
    <div className="space-y-1 min-w-0">
      <div className="text-xs font-medium uppercase tracking-wide text-muted-foreground">{label}</div>
      <div className="text-sm text-foreground break-words">{children}</div>
    </div>
  );
}

export function StockRequestMasterReadOnly({ status, labels, className }: StockRequestMasterReadOnlyProps) {
  return (
    <div className={cn("grid gap-6 sm:grid-cols-2", className)}>
      <div className="sm:col-span-2 flex flex-wrap items-center gap-2 border-b border-border pb-4">
        <span className="text-xs font-medium uppercase tracking-wide text-muted-foreground">Status</span>
        <StockRequestStatusBadge status={status} />
      </div>

      <Field label="Company">{labels.companyName}</Field>
      <Field label="Request date">{labels.requestDateLabel}</Field>

      <Field label="Requesting warehouse (destination)">{labels.requestingWarehouseName}</Field>
      <Field label="Supplying warehouse (source)">{labels.supplyingWarehouseName}</Field>

      <div className="sm:col-span-2 space-y-1 min-w-0">
        <div className="text-xs font-medium uppercase tracking-wide text-muted-foreground">
          Description / note
        </div>
        <div className="text-sm text-foreground whitespace-pre-wrap break-words rounded-md border border-border bg-muted/30 px-3 py-2 min-h-[72px]">
          {labels.noteDisplay}
        </div>
      </div>
    </div>
  );
}
