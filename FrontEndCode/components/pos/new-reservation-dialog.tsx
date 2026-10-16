"use client";

import { useEffect, useState } from "react";
import { toast } from "sonner";
import { Button } from "@/components/ui/button";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { DURATION_SHORTCUTS, MAX_DURATION, MIN_DURATION, localIsoDate } from "@/lib/reservation-rules";
import { createReservation } from "@/lib/services/reservation-service";
import {
  getRestaurantTables,
  RestaurantTableType,
  type RestaurantTable,
} from "@/lib/services/restaurant-table-service";

const selectClass =
  "flex h-10 w-full rounded-md border border-input bg-background px-3 py-2 text-sm ring-offset-background";

/** The next quarter hour from now, as HH:mm. */
const nextQuarterHour = () => {
  const d = new Date(Date.now() + 15 * 60_000);
  d.setMinutes(Math.floor(d.getMinutes() / 15) * 15, 0, 0);
  const p = (n: number) => String(n).padStart(2, "0");
  return `${p(d.getHours())}:${p(d.getMinutes())}`;
};

type Props = {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  restaurantId: number;
  onCreated: () => void;
  /** Table preselected when the dialog opens (e.g. tapped on the tables screen). */
  initialTableId?: number | null;
};

/** New table reservation from the POS: guest name, phone, number of guests, date and time are required. */
export function NewReservationDialog({ open, onOpenChange, restaurantId, onCreated, initialTableId }: Props) {
  const [tables, setTables] = useState<RestaurantTable[]>([]);
  const [tableId, setTableId] = useState("");
  const [guestName, setGuestName] = useState("");
  const [guestPhone, setGuestPhone] = useState("");
  const [guestCount, setGuestCount] = useState("2");
  const [date, setDate] = useState(localIsoDate());
  const [time, setTime] = useState(nextQuarterHour());
  const [duration, setDuration] = useState("90");
  const [note, setNote] = useState("");
  const [busy, setBusy] = useState(false);

  useEffect(() => {
    if (!open) return;
    setTableId(initialTableId ? String(initialTableId) : "");
    setGuestName("");
    setGuestPhone("");
    setGuestCount("2");
    setDate(localIsoDate());
    setTime(nextQuarterHour());
    setDuration("90");
    setNote("");
    getRestaurantTables()
      .then((all) =>
        setTables(
          all.filter(
            (t) =>
              t.restaurantId === restaurantId &&
              t.isActive &&
              t.type !== RestaurantTableType.Delivery &&
              t.type !== RestaurantTableType.TakeAway,
          ),
        ),
      )
      .catch(() => setTables([]));
  }, [open, restaurantId, initialTableId]);

  const save = async () => {
    const count = Number(guestCount);
    if (!guestName.trim()) return toast.error("Müştərinin adı mütləqdir.");
    if (!guestPhone.trim()) return toast.error("Telefon nömrəsi mütləqdir.");
    if (!Number.isFinite(count) || count < 1) return toast.error("Adam sayı mütləqdir (ən azı 1).");
    if (!date) return toast.error("Tarix mütləqdir.");
    if (!time) return toast.error("Saat mütləqdir.");
    const minutes = Number(duration);
    if (!Number.isInteger(minutes) || minutes < MIN_DURATION || minutes > MAX_DURATION)
      return toast.error(`Saxlama müddəti ${MIN_DURATION}–${MAX_DURATION} dəqiqə arasında olmalıdır.`);
    if (new Date(`${date}T${time}`).getTime() < Date.now()) return toast.error("Keçmiş vaxta rezerv yazmaq olmaz.");

    setBusy(true);
    try {
      await createReservation({
        restaurantId,
        tableId: tableId ? Number(tableId) : null,
        guestName: guestName.trim(),
        guestPhone: guestPhone.trim(),
        guestCount: Math.floor(count),
        reservationDate: date,
        reservationTime: time,
        durationMinutes: minutes,
        note: note.trim() || null,
      });
      toast.success("Rezervasiya yaradıldı.");
      onOpenChange(false);
      onCreated();
    } catch (err) {
      toast.error(err instanceof Error ? err.message : "Rezervasiya yaradılmadı.");
    } finally {
      setBusy(false);
    }
  };

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-h-[90vh] overflow-y-auto sm:max-w-md">
        <DialogHeader>
          <DialogTitle>Yeni rezervasiya</DialogTitle>
          <DialogDescription>Ad, telefon, adam sayı, tarix və saat mütləqdir.</DialogDescription>
        </DialogHeader>

        <div className="space-y-3">
          <div>
            <Label htmlFor="rv-table">Masa</Label>
            <select id="rv-table" className={selectClass + " mt-1"} value={tableId} onChange={(e) => setTableId(e.target.value)}>
              <option value="">Masa seçilməyib</option>
              {tables.map((t) => (
                <option key={t.id} value={String(t.id)}>
                  {t.name} ({t.capacity} yer)
                </option>
              ))}
            </select>
          </div>
          <div className="grid grid-cols-2 gap-3">
            <div>
              <Label htmlFor="rv-name">Müştərinin adı *</Label>
              <Input id="rv-name" className="mt-1" value={guestName} onChange={(e) => setGuestName(e.target.value)} placeholder="Ad Soyad" />
            </div>
            <div>
              <Label htmlFor="rv-phone">Telefon *</Label>
              <Input id="rv-phone" className="mt-1" value={guestPhone} onChange={(e) => setGuestPhone(e.target.value)} placeholder="+994 50 000 00 00" />
            </div>
          </div>
          <div className="grid grid-cols-3 gap-3">
            <div>
              <Label htmlFor="rv-count">Adam sayı *</Label>
              <Input id="rv-count" className="mt-1" type="number" min={1} value={guestCount} onChange={(e) => setGuestCount(e.target.value)} />
            </div>
            <div>
              <Label htmlFor="rv-date">Tarix *</Label>
              <Input id="rv-date" className="mt-1" type="date" value={date} onChange={(e) => setDate(e.target.value)} />
            </div>
            <div>
              <Label htmlFor="rv-time">Saat *</Label>
              <Input id="rv-time" className="mt-1" type="time" value={time} onChange={(e) => setTime(e.target.value)} />
            </div>
          </div>
          <div>
            <Label htmlFor="rv-duration">Masa nə qədər saxlanılsın (dəqiqə)</Label>
            <Input
              id="rv-duration"
              className="mt-1"
              type="number"
              min={MIN_DURATION}
              max={MAX_DURATION}
              value={duration}
              onChange={(e) => setDuration(e.target.value)}
            />
            <div className="mt-2 flex flex-wrap gap-1.5">
              {DURATION_SHORTCUTS.map((m) => (
                <Button key={m} type="button" size="sm" variant={Number(duration) === m ? "default" : "outline"} onClick={() => setDuration(String(m))}>
                  {m % 60 === 0 ? `${m / 60} saat` : `${m} dəq`}
                </Button>
              ))}
            </div>
          </div>
          <div>
            <Label htmlFor="rv-note">Qeyd</Label>
            <Input id="rv-note" className="mt-1" value={note} onChange={(e) => setNote(e.target.value)} placeholder="İstəyə görə" />
          </div>
        </div>

        <DialogFooter>
          <Button variant="outline" onClick={() => onOpenChange(false)} disabled={busy}>
            Ləğv et
          </Button>
          <Button onClick={() => void save()} disabled={busy}>
            {busy ? "Saxlanılır…" : "Saxla"}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
