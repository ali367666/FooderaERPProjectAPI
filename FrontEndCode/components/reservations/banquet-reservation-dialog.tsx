"use client";

import { useEffect, useMemo, useState } from "react";
import { toast } from "sonner";
import { X } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { cn } from "@/lib/utils";
import { toApiFormError } from "@/lib/api-error";
import {
  createBanquetReservation,
  getBanquetTables,
  type BanquetTableOption,
} from "@/lib/services/reservation-service";
import { getRestaurantSections, type RestaurantSection } from "@/lib/services/restaurant-section-service";

type Props = {
  open: boolean;
  onClose: () => void;
  onCreated: () => void;
  restaurants: { id: number; name: string }[];
  defaultDate: string;
};

const todayIso = () => new Date().toISOString().split("T")[0];

/**
 * Books a banquet: pick the branch and the hall, the date and time, then tick the hall's tables that
 * stay free for that time. Busy tables are disabled and say why.
 */
export function BanquetReservationDialog({ open, onClose, onCreated, restaurants, defaultDate }: Props) {
  const [restaurantId, setRestaurantId] = useState(0);
  const [sections, setSections] = useState<RestaurantSection[]>([]);
  const [sectionId, setSectionId] = useState(0);
  const [date, setDate] = useState(defaultDate);
  const [time, setTime] = useState("19:00");
  const [duration, setDuration] = useState(240);
  const [tables, setTables] = useState<BanquetTableOption[]>([]);
  const [tablesLoading, setTablesLoading] = useState(false);
  const [picked, setPicked] = useState<Set<number>>(new Set());
  const [guestName, setGuestName] = useState("");
  const [guestPhone, setGuestPhone] = useState("");
  const [guestCount, setGuestCount] = useState(20);
  const [note, setNote] = useState("");
  const [saving, setSaving] = useState(false);

  useEffect(() => {
    if (!open) return;
    setRestaurantId(restaurants[0]?.id ?? 0);
    setSectionId(0);
    setDate(defaultDate || todayIso());
    setTime("19:00");
    setDuration(240);
    setPicked(new Set());
    setGuestName("");
    setGuestPhone("");
    setGuestCount(20);
    setNote("");
  }, [open, restaurants, defaultDate]);

  // The halls of the chosen branch.
  useEffect(() => {
    if (!open || !restaurantId) {
      setSections([]);
      return;
    }
    let cancelled = false;
    getRestaurantSections(restaurantId)
      .then((rows) => {
        if (cancelled) return;
        const active = rows.filter((s) => s.isActive);
        setSections(active);
        setSectionId(active[0]?.id ?? 0);
      })
      .catch(() => !cancelled && setSections([]));
    return () => {
      cancelled = true;
    };
  }, [open, restaurantId]);

  // The hall's tables, marked busy for that date and time; changing any of them clears the picks.
  useEffect(() => {
    setPicked(new Set());
    if (!open || !restaurantId || !sectionId || !date || !time) {
      setTables([]);
      return;
    }
    let cancelled = false;
    setTablesLoading(true);
    getBanquetTables({ restaurantId, sectionId, date, time, durationMinutes: duration })
      .then((rows) => !cancelled && setTables(rows))
      .catch((e) => {
        if (cancelled) return;
        setTables([]);
        toast.error(toApiFormError(e, "Masalar yüklənmədi").message);
      })
      .finally(() => !cancelled && setTablesLoading(false));
    return () => {
      cancelled = true;
    };
  }, [open, restaurantId, sectionId, date, time, duration]);

  const capacity = useMemo(
    () => tables.filter((t) => picked.has(t.id)).reduce((sum, t) => sum + t.capacity, 0),
    [tables, picked],
  );

  const toggle = (id: number) =>
    setPicked((prev) => {
      const next = new Set(prev);
      if (next.has(id)) next.delete(id);
      else next.add(id);
      return next;
    });

  const handleSave = async () => {
    if (!restaurantId) return toast.error("Filial seçin.");
    if (!sectionId) return toast.error("Zalı seçin.");
    if (picked.size === 0) return toast.error("Ən azı bir masa seçin.");
    if (!guestName.trim()) return toast.error("Müştərinin adı mütləqdir.");
    if (!guestPhone.trim()) return toast.error("Telefon nömrəsi mütləqdir.");
    if (!Number.isFinite(guestCount) || guestCount < 1) return toast.error("Nəfər sayı mütləqdir.");
    if (new Date(`${date}T${time}`).getTime() < Date.now()) return toast.error("Keçmiş vaxta banket yazmaq olmaz.");

    setSaving(true);
    try {
      await createBanquetReservation({
        restaurantId,
        tableIds: [...picked],
        guestName: guestName.trim(),
        guestPhone: guestPhone.trim(),
        guestCount,
        reservationDate: date,
        reservationTime: time,
        durationMinutes: duration,
        note: note.trim() || null,
      });
      toast.success("Banket rezervi yaradıldı.");
      onCreated();
      onClose();
    } catch (e) {
      toast.error(toApiFormError(e, "Banket rezervi yaradılmadı").message);
    } finally {
      setSaving(false);
    }
  };

  if (!open) return null;

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50 p-4">
      <div className="max-h-[90vh] w-full max-w-2xl overflow-y-auto rounded-2xl border border-border bg-background shadow-2xl">
        <div className="flex items-center justify-between border-b border-border p-5">
          <h2 className="text-lg font-semibold">Yeni banket rezervi</h2>
          <button onClick={onClose} className="text-muted-foreground hover:text-foreground">
            <X className="h-5 w-5" />
          </button>
        </div>

        <div className="space-y-4 p-5">
          <div className="grid grid-cols-2 gap-3">
            <div>
              <label className="mb-1 block text-xs text-muted-foreground">Filial *</label>
              <select
                value={restaurantId}
                onChange={(e) => setRestaurantId(Number(e.target.value))}
                className="w-full rounded-lg border border-border bg-background px-3 py-2 text-sm"
              >
                <option value={0}>Seçin...</option>
                {restaurants.map((r) => (
                  <option key={r.id} value={r.id}>
                    {r.name}
                  </option>
                ))}
              </select>
            </div>
            <div>
              <label className="mb-1 block text-xs text-muted-foreground">Zal *</label>
              <select
                value={sectionId}
                onChange={(e) => setSectionId(Number(e.target.value))}
                className="w-full rounded-lg border border-border bg-background px-3 py-2 text-sm"
              >
                <option value={0}>{sections.length === 0 ? "Zal tapılmadı" : "Seçin..."}</option>
                {sections.map((s) => (
                  <option key={s.id} value={s.id}>
                    {s.name}
                  </option>
                ))}
              </select>
            </div>
          </div>

          <div className="grid grid-cols-3 gap-3">
            <div>
              <label className="mb-1 block text-xs text-muted-foreground">Tarix *</label>
              <Input type="date" value={date} onChange={(e) => setDate(e.target.value)} />
            </div>
            <div>
              <label className="mb-1 block text-xs text-muted-foreground">Vaxt *</label>
              <Input type="time" value={time} onChange={(e) => setTime(e.target.value)} />
            </div>
            <div>
              <label className="mb-1 block text-xs text-muted-foreground">Müddət (dəq)</label>
              <Input
                type="number"
                min={60}
                max={720}
                step={30}
                value={duration}
                onChange={(e) => setDuration(Number(e.target.value))}
              />
            </div>
          </div>

          <div>
            <div className="mb-1 flex items-center justify-between">
              <label className="text-xs text-muted-foreground">Masalar *</label>
              <span className="text-xs text-muted-foreground">
                Seçilib: {picked.size} masa · {capacity} yer
              </span>
            </div>
            {tablesLoading ? (
              <p className="py-4 text-center text-sm text-muted-foreground">Yüklənir…</p>
            ) : tables.length === 0 ? (
              <p className="py-4 text-center text-sm text-muted-foreground">
                {sectionId ? "Bu zalda masa yoxdur." : "Əvvəlcə zalı seçin."}
              </p>
            ) : (
              <div className="grid grid-cols-3 gap-2 sm:grid-cols-4">
                {tables.map((t) => {
                  const on = picked.has(t.id);
                  return (
                    <button
                      key={t.id}
                      type="button"
                      disabled={!t.isAvailable}
                      onClick={() => toggle(t.id)}
                      title={t.busyReason ?? undefined}
                      className={cn(
                        "rounded-lg border p-2 text-left text-sm transition-colors",
                        on
                          ? "border-primary bg-primary/10 font-semibold"
                          : "border-border hover:border-primary/40",
                        !t.isAvailable && "cursor-not-allowed opacity-40 hover:border-border",
                      )}
                    >
                      <div>{t.name}</div>
                      <div className="text-[11px] text-muted-foreground">{t.capacity} nəfər</div>
                      {!t.isAvailable && <div className="mt-0.5 text-[10px] text-destructive">Məşğul</div>}
                    </button>
                  );
                })}
              </div>
            )}
          </div>

          <div className="grid grid-cols-3 gap-3">
            <div className="col-span-1">
              <label className="mb-1 block text-xs text-muted-foreground">Müştəri adı *</label>
              <Input value={guestName} onChange={(e) => setGuestName(e.target.value)} placeholder="Ad Soyad" />
            </div>
            <div className="col-span-1">
              <label className="mb-1 block text-xs text-muted-foreground">Telefon *</label>
              <Input value={guestPhone} onChange={(e) => setGuestPhone(e.target.value)} placeholder="+994 50 000 00 00" />
            </div>
            <div className="col-span-1">
              <label className="mb-1 block text-xs text-muted-foreground">Nəfər sayı *</label>
              <Input
                type="number"
                min={1}
                value={guestCount}
                onChange={(e) => setGuestCount(Number(e.target.value))}
              />
            </div>
          </div>

          <div>
            <label className="mb-1 block text-xs text-muted-foreground">Qeyd</label>
            <textarea
              value={note}
              onChange={(e) => setNote(e.target.value)}
              rows={2}
              placeholder="Tədbirin növü, xüsusi istəklər..."
              className="w-full resize-none rounded-lg border border-border bg-background px-3 py-2 text-sm"
            />
          </div>
        </div>

        <div className="flex justify-end gap-2 border-t border-border p-5">
          <Button variant="outline" onClick={onClose}>
            Ləğv et
          </Button>
          <Button onClick={() => void handleSave()} disabled={saving}>
            {saving ? "Saxlanır..." : "Yarat"}
          </Button>
        </div>
      </div>
    </div>
  );
}
