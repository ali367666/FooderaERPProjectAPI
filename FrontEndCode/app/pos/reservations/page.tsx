"use client";

import { useCallback, useEffect, useRef, useState } from "react";
import { toast } from "sonner";
import { format, parseISO } from "date-fns";
import { Plus, RefreshCw, Users } from "lucide-react";
import { Button } from "@/components/ui/button";
import { cn } from "@/lib/utils";
import { PosEmpty, PosPage, PosPageError, PosPageLoading, PosTileGrid, posTileClass } from "@/components/pos/pos-page";
import {
  PosDateRangeFilter,
  formatRange,
  todayIso,
  todayRange,
  type PosDateRange,
} from "@/components/pos/pos-date-range-filter";
import { getPosTerminalContext, type PosTerminalContext } from "@/lib/pos-terminal-client";
import { useHasPermission } from "@/hooks/use-auth-permissions";
import { NewReservationDialog } from "@/components/pos/new-reservation-dialog";
import {
  getReservations,
  changeReservationStatus,
  type ReservationDto,
  type ReservationStatus,
} from "@/lib/services/reservation-service";

function statusLabel(status: ReservationStatus): string {
  switch (status) {
    case "Pending":
      return "Gözləyir";
    case "Confirmed":
      return "Təsdiqləndi";
    case "Seated":
      return "Oturdu";
    case "Completed":
      return "Tamamlandı";
    case "Cancelled":
      return "Ləğv edildi";
    case "NoShow":
      return "Gəlmədi";
    default:
      return status;
  }
}

/** Waiting reservations look like a reserved table (amber); a no-show/cancel is red. */
function tileTone(status: ReservationStatus): string {
  switch (status) {
    case "Pending":
    case "Confirmed":
      return "border-amber-400 bg-amber-50 dark:bg-amber-950/30";
    case "Cancelled":
    case "NoShow":
      return "border-red-300 bg-red-50 dark:bg-red-950/30";
    case "Completed":
      return "opacity-60";
    default:
      return "";
  }
}

function statusTextClass(status: ReservationStatus): string {
  switch (status) {
    case "Pending":
    case "Confirmed":
      return "text-amber-600";
    case "Cancelled":
    case "NoShow":
      return "text-red-600";
    default:
      return "text-muted-foreground";
  }
}

export default function PosReservationsPage() {
  const [terminal, setTerminal] = useState<PosTerminalContext | null | undefined>(undefined);
  const [reservations, setReservations] = useState<ReservationDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [busyId, setBusyId] = useState<number | null>(null);
  const canCancel = useHasPermission("Reservation.Cancel");
  const canCreate = useHasPermission("Reservation.Create");
  const [newOpen, setNewOpen] = useState(false);
  const [range, setRange] = useState<PosDateRange>(todayRange);
  const loadSeq = useRef(0);

  useEffect(() => {
    setTerminal(getPosTerminalContext());
  }, []);

  const load = useCallback(async () => {
    const requestId = ++loadSeq.current;
    setLoading(true);
    setError(null);
    try {
      const all = await getReservations({ dateFrom: range.from, dateTo: range.to });
      // Days paged through quickly: an older answer must not replace the newer day's list.
      if (requestId !== loadSeq.current) return;
      const filtered = all.filter((r) => !terminal?.restaurantId || r.restaurantId === terminal.restaurantId);
      filtered.sort(
        (a, b) =>
          a.reservationDate.slice(0, 10).localeCompare(b.reservationDate.slice(0, 10)) ||
          a.reservationTime.localeCompare(b.reservationTime),
      );
      setReservations(filtered);
    } catch (err) {
      if (requestId === loadSeq.current) setError(err instanceof Error ? err.message : "Rezervasiyalar yüklənə bilmədi");
    } finally {
      if (requestId === loadSeq.current) setLoading(false);
    }
  }, [terminal, range]);

  useEffect(() => {
    if (terminal === undefined) return;
    void load();
  }, [terminal, load]);

  const handleAction = async (id: number, action: "confirm" | "seat" | "complete" | "cancel" | "noshow") => {
    setBusyId(id);
    try {
      const updated = await changeReservationStatus(id, action);
      setReservations((prev) => prev.map((r) => (r.id === id ? updated : r)));
    } catch (err) {
      toast.error(err instanceof Error ? err.message : "Status dəyişdirilmədi");
    } finally {
      setBusyId(null);
    }
  };

  if (terminal === undefined) return <PosPageLoading />;

  const isToday = range.from === todayIso() && range.to === todayIso();
  /** Tiles show their date only when the list spans more than one day. */
  const singleDay = range.from === range.to;

  return (
    <PosPage
      title={isToday ? "Bugünkü rezervasiyalar" : `Rezervasiyalar: ${formatRange(range)}`}
      actions={
        <>
          {canCreate && terminal?.restaurantId && (
            <Button size="sm" onClick={() => setNewOpen(true)}>
              <Plus className="mr-1 h-4 w-4" />
              Yeni rezervasiya
            </Button>
          )}
          <Button variant="outline" size="sm" onClick={() => void load()}>
            <RefreshCw className="h-4 w-4" />
          </Button>
        </>
      }
    >
      {terminal?.restaurantId && (
        <NewReservationDialog
          open={newOpen}
          onOpenChange={setNewOpen}
          restaurantId={terminal.restaurantId}
          onCreated={() => void load()}
        />
      )}

      <PosDateRangeFilter value={range} onChange={setRange} />

      {error && <PosPageError message={error} />}

      {loading && <PosPageLoading />}

      {!loading && reservations.length === 0 && !error && (
        <PosEmpty>
          {isToday ? "Bu gün üçün rezervasiya yoxdur." : `${formatRange(range)} üçün rezervasiya yoxdur.`}
        </PosEmpty>
      )}

      {!loading && <PosTileGrid wide>
        {reservations.map((r) => {
          const busy = busyId === r.id;
          const seated = r.status === "Seated";
          return (
            <div key={r.id} className={cn(posTileClass(seated ? "bg-[#34d399]" : null), tileTone(r.status))}>
              {!singleDay && (
                <span className={cn("text-xs font-medium tabular-nums", seated ? "text-white/80" : "text-muted-foreground")}>
                  {format(parseISO(r.reservationDate), "dd.MM.yyyy")}
                </span>
              )}
              <span className="text-lg font-bold leading-none tabular-nums">{r.reservationTime}</span>
              <span className="max-w-full truncate font-semibold">{r.guestName}</span>
              <span className={cn("flex items-center gap-1 text-xs", seated ? "text-white/80" : "text-muted-foreground")}>
                <Users className="h-3 w-3" />
                {r.guestCount} nəfər{r.tableName ? ` · ${r.tableName}` : ""}
              </span>
              <span className={cn("text-xs tabular-nums", seated ? "text-white/80" : "text-muted-foreground")}>{r.guestPhone}</span>
              <span className={cn("text-xs font-semibold", seated ? "text-white" : statusTextClass(r.status))}>
                {statusLabel(r.status)}
              </span>
              {r.note && (
                <span className={cn("max-w-full truncate text-[11px] italic", seated ? "text-white/70" : "text-muted-foreground")}>
                  {r.note}
                </span>
              )}

              <div className="mt-2 flex flex-wrap justify-center gap-2">
                {r.status === "Pending" && (
                  <>
                    <Button size="sm" disabled={busy} onClick={() => void handleAction(r.id, "confirm")}>
                      Təsdiqlə
                    </Button>
                    {canCancel && (
                      <Button size="sm" variant="outline" disabled={busy} onClick={() => void handleAction(r.id, "cancel")}>
                        Ləğv et
                      </Button>
                    )}
                  </>
                )}
                {r.status === "Confirmed" && (
                  <>
                    <Button size="sm" disabled={busy} onClick={() => void handleAction(r.id, "seat")}>
                      Oturt
                    </Button>
                    <Button size="sm" variant="outline" disabled={busy} onClick={() => void handleAction(r.id, "noshow")}>
                      Gəlmədi
                    </Button>
                  </>
                )}
                {seated && (
                  <Button size="sm" variant="secondary" disabled={busy} onClick={() => void handleAction(r.id, "complete")}>
                    Tamamla
                  </Button>
                )}
              </div>
            </div>
          );
        })}
      </PosTileGrid>}
    </PosPage>
  );
}
