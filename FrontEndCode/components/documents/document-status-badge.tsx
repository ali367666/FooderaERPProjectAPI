"use client";

import { Badge } from "@/components/ui/badge";
import { cn } from "@/lib/utils";
import {
  StockRequestStatus,
  stockRequestStatusLabel,
  type StockRequestStatusValue,
} from "@/lib/services/stock-request-service";
import {
  TransferStatus,
  transferStatusLabel,
  type TransferStatusValue,
} from "@/lib/services/warehouse-transfer-service";

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

function transferVariant(
  s: TransferStatusValue,
): "default" | "secondary" | "destructive" | "outline" {
  switch (s) {
    case TransferStatus.Draft:
      return "secondary";
    case TransferStatus.Pending:
      return "outline";
    case TransferStatus.Approved:
    case TransferStatus.Completed:
      return "default";
    case TransferStatus.InTransit:
      return "default";
    case TransferStatus.Rejected:
    case TransferStatus.Cancelled:
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

export function WarehouseTransferStatusBadge({
  status,
  className,
}: {
  status: TransferStatusValue;
  className?: string;
}) {
  return (
    <Badge variant={transferVariant(status)} className={cn("font-medium", className)}>
      {transferStatusLabel(status)}
    </Badge>
  );
}
