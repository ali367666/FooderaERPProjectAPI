"use client";

import { useCallback, useEffect, useState } from "react";
import { toast } from "sonner";
import { Phone, RefreshCw, Users } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { cn } from "@/lib/utils";
import { getPosTerminalContext, type PosTerminalContext } from "@/lib/pos-terminal-client";
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

function statusBadgeClass(status: ReservationStatus): string {
  switch (status) {
    case "Pending":
      return "bg-amber-100 text-amber-800";
    case "Confirmed":
      return "bg-blue-100 text-blue-800";
    case "Seated":
      return "bg-emerald-100 text-emerald-800";
    case "Completed":
      return "bg-slate-100 text-slate-700";
    case "Cancelled":
    case "NoShow":
      return "bg-red-100 text-red-700";
    default:
      return "bg-slate-100 text-slate-700";
  }
}

function todayIso(): string {
  return new Date().toISOString().slice(0, 10);
}

export default function PosReservationsPage() {
  const [terminal, setTerminal] = useState<PosTerminalContext | null | undefined>(undefined);
  const [reservations, setReservations] = useState<ReservationDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [busyId, setBusyId] = useState<number | null>(null);

  useEffect(() => {
    setTerminal(getPosTerminalContext());
  }, []);

  const load = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const all = await getReservations(todayIso());
      const filtered = terminal?.restaurantId
        ? all.filter((r) => r.restaurantId === terminal.restaurantId)
        : all;
      filtered.sort((a, b) => a.reservationTime.localeCompare(b.reservationTime));
      setReservations(filtered);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Rezervasiyalar yüklənə bilmədi");
    } finally {
      setLoading(false);
    }
  }, [terminal]);

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

  if (loading || terminal === undefined) {
    return <div className="flex h-full items-center justify-center text-muted-foreground">Yüklənir...</div>;
  }

  return (
    <div className="p-4 sm:p-6">
      <div className="mb-4 flex items-center justify-between">
        <h1 className="text-xl font-bold">Bugünkü rezervasiyalar</h1>
        <Button variant="outline" size="sm" onClick={() => void load()}>
          <RefreshCw className="h-4 w-4" />
        </Button>
      </div>

      {error && (
        <div className="mb-4 rounded-md border border-destructive/30 bg-destructive/10 px-4 py-3 text-sm text-destructive">
          {error}
        </div>
      )}

      {reservations.length === 0 && !error && (
        <p className="text-sm text-muted-foreground">Bu gün üçün rezervasiya yoxdur.</p>
      )}

      <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-3">
        {reservations.map((r) => {
          const busy = busyId === r.id;
          return (
            <div key={r.id} className="rounded-xl border bg-card p-4 shadow-sm">
              <div className="flex items-start justify-between gap-2">
                <div>
                  <p className="text-lg font-bold">{r.reservationTime}</p>
                  <p className="font-medium">{r.guestName}</p>
                </div>
                <Badge className={cn("shrink-0", statusBadgeClass(r.status))}>
                  {statusLabel(r.status)}
                </Badge>
              </div>
              <div className="mt-2 space-y-1 text-sm text-muted-foreground">
                <div className="flex items-center gap-1.5">
                  <Users className="h-3.5 w-3.5" />
                  {r.guestCount} nəfər {r.tableName ? `— ${r.tableName}` : ""}
                </div>
                <div className="flex items-center gap-1.5">
                  <Phone className="h-3.5 w-3.5" />
                  {r.guestPhone}
                </div>
                {r.note && <p className="italic">{r.note}</p>}
              </div>

              <div className="mt-3 flex flex-wrap gap-2">
                {r.status === "Pending" && (
                  <>
                    <Button size="sm" disabled={busy} onClick={() => void handleAction(r.id, "confirm")}>
                      Təsdiqlə
                    </Button>
                    <Button
                      size="sm"
                      variant="outline"
                      disabled={busy}
                      onClick={() => void handleAction(r.id, "cancel")}
                    >
                      Ləğv et
                    </Button>
                  </>
                )}
                {r.status === "Confirmed" && (
                  <>
                    <Button size="sm" disabled={busy} onClick={() => void handleAction(r.id, "seat")}>
                      Oturt
                    </Button>
                    <Button
                      size="sm"
                      variant="outline"
                      disabled={busy}
                      onClick={() => void handleAction(r.id, "noshow")}
                    >
                      Gəlmədi
                    </Button>
                  </>
                )}
                {r.status === "Seated" && (
                  <Button size="sm" disabled={busy} onClick={() => void handleAction(r.id, "complete")}>
                    Tamamla
                  </Button>
                )}
              </div>
            </div>
          );
        })}
      </div>
    </div>
  );
}
