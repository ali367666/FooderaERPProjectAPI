"use client";

import { useEffect, useState } from "react";
import { toast } from "sonner";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { useHasPermission } from "@/hooks/use-auth-permissions";
import { EXTEND_SHORTCUTS, MAX_DURATION, isReservationExpired } from "@/lib/reservation-rules";
import {
  changeReservationStatus,
  extendReservation,
  moveReservation,
  type ReservationDto,
} from "@/lib/services/reservation-service";
import type { RestaurantTable } from "@/lib/services/restaurant-table-service";

type Props = {
  reservation: ReservationDto | null;
  /** Tables of the same branch a reservation may be moved to (the current one is skipped). */
  tables: RestaurantTable[];
  onClose: () => void;
  /** Called after any change so the screen reloads its reservations. */
  onChanged: () => void;
  /** The guest has arrived and the reservation is completed — the table can take an order now. */
  onCompleted?: (reservation: ReservationDto) => void;
};

const selectClass =
  "flex h-10 w-full rounded-md border border-input bg-background px-3 py-2 text-sm ring-offset-background";

/**
 * What can be done with an active reservation: complete it (guest arrived), hold the table longer,
 * move it to another table, or cancel it. Every action shows only for someone holding its permission.
 */
export function ReservationActionsDialog({ reservation, tables, onClose, onChanged, onCompleted }: Props) {
  const canConfirm = useHasPermission("Reservation.Confirm");
  const canUpdate = useHasPermission("Reservation.Update");
  const canCancel = useHasPermission("Reservation.Cancel");
  const [busy, setBusy] = useState(false);
  const [moving, setMoving] = useState(false);
  const [targetTableId, setTargetTableId] = useState("");
  const [extendInput, setExtendInput] = useState("");

  useEffect(() => {
    setMoving(false);
    setTargetTableId("");
    setExtendInput("");
  }, [reservation?.id]);

  const run = async (task: () => Promise<unknown>, success: string, close = true) => {
    setBusy(true);
    try {
      await task();
      toast.success(success);
      onChanged();
      if (close) onClose();
    } catch (err) {
      toast.error(err instanceof Error ? err.message : "Əməliyyat alınmadı");
    } finally {
      setBusy(false);
    }
  };

  const r = reservation;
  const expired = r ? isReservationExpired(r, Date.now()) : false;
  const moveTargets = tables.filter((t) => t.id !== r?.tableId);

  return (
    <Dialog open={r !== null} onOpenChange={(open) => !open && onClose()}>
      <DialogContent className="sm:max-w-md">
        <DialogHeader>
          <DialogTitle>
            {r?.tableName ?? "Masa"} — rezervasiya{expired ? " (vaxtı bitib)" : ""}
          </DialogTitle>
          {r && (
            <DialogDescription>
              {r.guestName} · {r.guestPhone} · {r.guestCount} nəfər · saat {r.reservationTime} ({r.durationMinutes} dəq
              saxlanılır)
              {r.note ? ` · ${r.note}` : ""}
            </DialogDescription>
          )}
        </DialogHeader>

        {r && (
          <div className="space-y-4">
            {canConfirm && (
              <Button
                className="h-11 w-full"
                disabled={busy}
                onClick={() =>
                  void run(async () => {
                    const done = await changeReservationStatus(r.id, "complete");
                    onCompleted?.(done);
                  }, "Rezervasiya tamamlandı — müştəri masaya əyləşdi")
                }
              >
                Müştəri gəldi — rezervasiyanı tamamla
              </Button>
            )}

            {canUpdate && (
              <div className="space-y-2">
                <p className="text-sm font-medium">Müddəti artır</p>
                <div className="flex gap-2">
                  {EXTEND_SHORTCUTS.map((m) => (
                    <Button
                      key={m}
                      variant="outline"
                      className="flex-1"
                      disabled={busy}
                      onClick={() => void run(() => extendReservation(r.id, m), `Müddət ${m} dəq artırıldı`)}
                    >
                      +{m} dəq
                    </Button>
                  ))}
                </div>
                <div className="flex gap-2">
                  <Input
                    type="number"
                    min={1}
                    max={MAX_DURATION}
                    placeholder="Dəqiqə yazın"
                    value={extendInput}
                    onChange={(e) => setExtendInput(e.target.value)}
                  />
                  <Button
                    disabled={busy || !(Number(extendInput) > 0)}
                    onClick={() =>
                      void run(() => extendReservation(r.id, Math.floor(Number(extendInput))), `Müddət ${extendInput} dəq artırıldı`)
                    }
                  >
                    Artır
                  </Button>
                </div>
              </div>
            )}

            {canUpdate && (
              <div className="space-y-2">
                <p className="text-sm font-medium">Başqa masaya keçir</p>
                {moving ? (
                  <div className="flex gap-2">
                    <select
                      className={selectClass}
                      value={targetTableId}
                      onChange={(e) => setTargetTableId(e.target.value)}
                    >
                      <option value="">Masa seçin</option>
                      {moveTargets.map((t) => (
                        <option key={t.id} value={String(t.id)}>
                          {t.name} ({t.capacity} yer)
                        </option>
                      ))}
                    </select>
                    <Button
                      disabled={busy || !targetTableId}
                      onClick={() =>
                        void run(() => moveReservation(r.id, Number(targetTableId)), "Rezervasiya başqa masaya keçirildi")
                      }
                    >
                      Keçir
                    </Button>
                  </div>
                ) : (
                  <Button variant="outline" className="w-full" disabled={busy} onClick={() => setMoving(true)}>
                    Masa seç
                  </Button>
                )}
              </div>
            )}
          </div>
        )}

        <DialogFooter className="gap-2 sm:justify-between">
          {canCancel && r ? (
            <Button
              variant="outline"
              className="text-destructive"
              disabled={busy}
              onClick={() => void run(() => changeReservationStatus(r.id, "cancel"), "Rezervasiya ləğv edildi")}
            >
              Rezervasiyanı ləğv et
            </Button>
          ) : (
            <span />
          )}
          <Button variant="outline" onClick={onClose} disabled={busy}>
            Bağla
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
