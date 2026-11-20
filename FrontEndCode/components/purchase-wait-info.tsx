import { cn } from "@/lib/utils";

/** "2 gün 4 saat", "3 saat 15 dəq", "12 dəq", "1 dəqiqədən az". */
export function formatWaitDuration(startIso: string, endIso?: string | null): string {
  const start = new Date(startIso).getTime();
  const end = endIso ? new Date(endIso).getTime() : Date.now();
  if (Number.isNaN(start) || Number.isNaN(end)) return "—";
  const minutes = Math.max(0, Math.floor((end - start) / 60_000));
  if (minutes < 1) return "1 dəqiqədən az";
  const days = Math.floor(minutes / 1440);
  const hours = Math.floor((minutes % 1440) / 60);
  const mins = minutes % 60;
  if (days > 0) return hours > 0 ? `${days} gün ${hours} saat` : `${days} gün`;
  if (hours > 0) return mins > 0 ? `${hours} saat ${mins} dəq` : `${hours} saat`;
  return `${mins} dəq`;
}

const dateTime = (iso: string) => {
  const d = new Date(iso);
  return Number.isNaN(d.getTime())
    ? iso
    : `${d.toLocaleDateString("az-AZ")} ${d.toLocaleTimeString("az-AZ", { hour: "2-digit", minute: "2-digit" })}`;
};

/**
 * The wait of a purchase: when it was put on hold, how long it waited and when it got its OK (or was rejected).
 * Nothing is drawn for a purchase that was never put on hold.
 */
export function PurchaseWaitInfo({
  waitingSince,
  decidedAt,
  decision = "ok",
  className,
}: {
  /** When the purchase started waiting; null when it never did. */
  waitingSince: string | null | undefined;
  /** When the wait ended; null/undefined while it still waits. */
  decidedAt?: string | null;
  decision?: "ok" | "rejected";
  className?: string;
}) {
  if (!waitingSince) return <span className="text-muted-foreground">—</span>;
  return (
    <div className={cn("space-y-0.5 text-xs leading-tight", className)}>
      <div>
        <span className="text-muted-foreground">Gözlədildi: </span>
        {dateTime(waitingSince)}
      </div>
      <div>
        <span className="text-muted-foreground">Müddət: </span>
        {formatWaitDuration(waitingSince, decidedAt)}
        {!decidedAt && <span className="text-amber-600"> (davam edir)</span>}
      </div>
      {decidedAt && (
        <div className={decision === "ok" ? "text-emerald-600" : "text-red-600"}>
          {decision === "ok" ? "OK verildi" : "Rədd edildi"}: {dateTime(decidedAt)}
        </div>
      )}
    </div>
  );
}
