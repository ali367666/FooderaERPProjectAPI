"use client";

import { useEffect, useState } from "react";
import { Users } from "lucide-react";
import { posTileButtonClass } from "@/components/pos/pos-page";
import type { OrderDto } from "@/lib/services/order-service";
import { formatElapsed, orderStatusBg, orderStatusLabel } from "@/lib/pos-order-status";

/** An open order drawn like a busy table on the tables screen. */
export function OrderTile({
  order,
  title,
  now,
  onClick,
}: {
  order: OrderDto;
  title: string;
  now: number;
  onClick: () => void;
}) {
  return (
    <button type="button" onClick={onClick} className={posTileButtonClass(orderStatusBg(order.status))}>
      <span className="max-w-full truncate text-lg font-bold leading-none">{title}</span>
      {order.guestCount != null && (
        <span className="flex items-center gap-1 text-xs text-white/80">
          <Users className="h-3 w-3" />
          {order.guestCount}
        </span>
      )}
      <span className="text-xs font-medium text-white/90">{orderStatusLabel(order.status)}</span>
      {order.waiterName && <span className="max-w-full truncate text-[11px] text-white/80">{order.waiterName}</span>}
      <span className="text-[11px] text-white/80">{formatElapsed(order.openedAt, now)}</span>
      <span className="text-xs font-semibold text-white">{order.totalAmount.toFixed(2)} ₼</span>
      {order.note && <span className="max-w-full truncate text-[10px] italic text-white/70">{order.note}</span>}
    </button>
  );
}

export function useMinuteClock() {
  // Re-render once a minute so the elapsed time on the tiles keeps moving.
  const [now, setNow] = useState(() => Date.now());
  useEffect(() => {
    const id = window.setInterval(() => setNow(Date.now()), 60_000);
    return () => window.clearInterval(id);
  }, []);
  return now;
}
