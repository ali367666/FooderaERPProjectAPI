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
import {
  createBanquetReservation,
  createReservation,
  getBanquetHalls,
  type BanquetHallOption,
} from "@/lib/services/reservation-service";
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

/** A banquet holds the whole hall, usually for an evening. */
const BANQUET_DEFAULT_DURATION = "240";

/**
 * New reservation from the POS: either a table (optional) or a whole banquet hall.
 * Guest name, phone, number of guests, date and time are required.
 */
export function NewReservationDialog({ open, onOpenChange, restaurantId, onCreated, initialTableId }: Props) {
  const [kind, setKind] = useState<"table" | "banquet">("table");
  const [tables, setTables] = useState<RestaurantTable[]>([]);
  const [tableId, setTableId] = useState("");
  const [halls, setHalls] = useState<BanquetHallOption[]>([]);
  const [hallsLoading, setHallsLoading] = useState(false);
  const [hallId, setHallId] = useState("");
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
    setKind("table");
    setTableId(initialTableId ? String(initialTableId) : "");
    setHallId("");
    setHalls([]);
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

  // Banquet halls, marked free/busy for the chosen date, time and duration (a hall is booked as a whole).
  useEffect(() => {
    if (!open || kind !== "banquet") return;
    const minutes = Number(duration);
    const validSpan = date && time && Number.isInteger(minutes) && minutes >= MIN_DURATION && minutes <= MAX_DURATION;
    let cancelled = false;
    setHallsLoading(true);
    const timer = setTimeout(() => {
      getBanquetHalls({
        restaurantId,
        ...(validSpan ? { date, time, durationMinutes: minutes } : {}),
      })
        .then((list) => {
          if (cancelled) return;
          setHalls(list);
          // A hall that became busy for the new time can't stay picked.
          setHallId((prev) => (prev && list.some((h) => String(h.id) === prev && h.isAvailable) ? prev : ""));
        })
        .catch((err) => {
          if (cancelled) return;
          setHalls([]);
          toast.error(err instanceof Error ? err.message : "Banket zalları yüklənmədi.");
        })
        .finally(() => !cancelled && setHallsLoading(false));
    }, 250);
    return () => {
      cancelled = true;
      clearTimeout(timer);
    };
  }, [open, kind, restaurantId, date, time, duration]);

  const switchKind = (next: "table" | "banquet") => {
    if (next === kind) return;
    setKind(next);
    // Banquets are booked for longer; switch the default holding time along unless it was typed by hand.
    if (next === "banquet" && duration === "90") setDuration(BANQUET_DEFAULT_DURATION);
    if (next === "table" && duration === BANQUET_DEFAULT_DURATION) setDuration("90");
  };

  const selectedHall = halls.find((h) => String(h.id) === hallId) ?? null;

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
    if (kind === "banquet") {
      if (!selectedHall) return toast.error("Banket zalını seçin.");
      if (!selectedHall.isAvailable)
        return toast.error(selectedHall.busyReason ?? "Bu zal seçilən vaxtda məşğuldur.");
    }

    setBusy(true);
    try {
      const common = {
        restaurantId,
        guestName: guestName.trim(),
        guestPhone: guestPhone.trim(),
        guestCount: Math.floor(count),
        reservationDate: date,
        reservationTime: time,
        durationMinutes: minutes,
        note: note.trim() || null,
      };
      if (kind === "banquet") {
        await createBanquetReservation({ ...common, sectionId: selectedHall!.id });
      } else {
        await createReservation({ ...common, tableId: tableId ? Number(tableId) : null });
      }
      toast.success(kind === "banquet" ? "Banket zalı rezerv edildi." : "Rezervasiya yaradıldı.");
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
          <div className="grid grid-cols-2 gap-2">
            <Button type="button" variant={kind === "table" ? "default" : "outline"} onClick={() => switchKind("table")}>
              Masa
            </Button>
            <Button type="button" variant={kind === "banquet" ? "default" : "outline"} onClick={() => switchKind("banquet")}>
              Banket zalı
            </Button>
          </div>
          {kind === "table" ? (
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
          ) : (
            <div>
              <Label htmlFor="rv-hall">Banket zalı *</Label>
              <select
                id="rv-hall"
                className={selectClass + " mt-1"}
                value={hallId}
                onChange={(e) => setHallId(e.target.value)}
                disabled={hallsLoading && halls.length === 0}
              >
                <option value="">
                  {hallsLoading && halls.length === 0
                    ? "Yüklənir…"
                    : halls.length === 0
                      ? "Banket zalı yoxdur"
                      : "Zal seçin"}
                </option>
                {halls.map((h) => (
                  <option key={h.id} value={String(h.id)} disabled={!h.isAvailable}>
                    {h.name} ({h.tableCount} masa, {h.capacity} yer){h.isAvailable ? "" : ` — ${h.busyReason ?? "məşğuldur"}`}
                  </option>
                ))}
              </select>
              <p className="mt-1 text-xs text-muted-foreground">
                Zal bütövlükdə rezerv olunur — bütün masaları seçilən vaxt üçün saxlanılır.
              </p>
              {selectedHall && Number(guestCount) > selectedHall.capacity && (
                <p className="mt-1 text-xs text-amber-600">
                  Adam sayı zalın tutumundan ({selectedHall.capacity} yer) çoxdur.
                </p>
              )}
            </div>
          )}
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
            <Label htmlFor="rv-duration">{kind === "banquet" ? "Zal" : "Masa"} nə qədər saxlanılsın (dəqiqə)</Label>
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
