"use client";

import { Badge } from "@/components/ui/badge";
import { cn } from "@/lib/utils";
import {
  StockRequestStatus,
  stockRequestStatusLabel,
  type StockRequestStatusValue,
} from "@/lib/services/stock-request-service";

function stockRequestVariant(
  s: StockRequestStatusValue,
): "default" | "secondary" | "destructive" | "outline" {
  switch (s) {
    case StockRequestStatus.Draft:
      return "secondary";
    case StockRequestStatus.Submitted:
      return "outline";
    case StockRequestStatus.Approved:
    case StockRequestStatus.Fulfilled:
      return "default";
    case StockRequestStatus.Rejected:
    case StockRequestStatus.Cancelled:
      return "destructive";
    default:
      return "secondary";
  }
}

export function StockRequestStatusBadge({
  status,
  className,
}: {
  status: StockRequestStatusValue;
  className?: string;
}) {
  return (
    <Badge variant={stockRequestVariant(status)} className={cn("font-medium", className)}>
      {stockRequestStatusLabel(status)}
    </Badge>
  );
}
