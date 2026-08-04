"use client";

import { useCallback, useEffect, useState } from "react";
import { toast } from "sonner";
import {
  getReservations, createReservation, updateReservation,
  changeReservationStatus, deleteReservation,
  type ReservationDto, type CreateReservationInput,
} from "@/lib/services/reservation-service";
import { getRestaurants } from "@/lib/services/restaurant-service";
import { getRestaurantTables } from "@/lib/services/restaurant-table-service";
import { toApiFormError } from "@/lib/api-error";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { cn } from "@/lib/utils";
import {
  Plus, RefreshCw, ChevronLeft, ChevronRight,
  Users, Clock, Phone, Mail, StickyNote, X, Check,
  Trash2, Edit2, Calendar,
} from "lucide-react";

// ── Status helpers ───────────────────────────────────────────────────────────

const STATUS_LABEL: Record<string, string> = {
  Pending: "Gözləyir", Confirmed: "Təsdiqləndi",
  Seated: "Oturdu", Completed: "Tamamlandı",
  Cancelled: "Ləğv edildi", NoShow: "Gəlmədi",
};
const STATUS_COLOR: Record<string, string> = {
  Pending: "bg-amber-100 text-amber-800 dark:bg-amber-950 dark:text-amber-300",
  Confirmed: "bg-blue-100 text-blue-800 dark:bg-blue-950 dark:text-blue-300",
  Seated: "bg-indigo-100 text-indigo-800 dark:bg-indigo-950 dark:text-indigo-300",
  Completed: "bg-emerald-100 text-emerald-800 dark:bg-emerald-950 dark:text-emerald-300",
  Cancelled: "bg-slate-100 text-slate-600 dark:bg-slate-800 dark:text-slate-400",
  NoShow: "bg-red-100 text-red-800 dark:bg-red-950 dark:text-red-300",
};

function StatusBadge({ status }: { status: string }) {
  return (
    <span className={cn("inline-flex items-center px-2 py-0.5 rounded text-xs font-medium", STATUS_COLOR[status] ?? "bg-muted text-muted-foreground")}>
      {STATUS_LABEL[status] ?? status}
    </span>
  );
}

// ── Date navigation ──────────────────────────────────────────────────────────

function isoDate(d: Date) {
  return d.toISOString().split("T")[0];
}
function addDays(d: Date, n: number) {
  const r = new Date(d); r.setDate(r.getDate() + n); return r;
}
function displayDate(d: Date) {
  return d.toLocaleDateString("az-AZ", { weekday: "long", day: "numeric", month: "long" });
}

// ── Empty form ───────────────────────────────────────────────────────────────

const EMPTY_FORM: CreateReservationInput = {
  restaurantId: 0, tableId: null,
  guestName: "", guestPhone: "", guestEmail: "",
  guestCount: 2, reservationDate: isoDate(new Date()),
  reservationTime: "19:00", durationMinutes: 90, note: "",
};

// ── Main ─────────────────────────────────────────────────────────────────────

export function ReservationsPage() {
  const [selectedDate, setSelectedDate] = useState(new Date());
  const [items, setItems] = useState<ReservationDto[]>([]);
  const [restaurants, setRestaurants] = useState<{ id: number; name: string }[]>([]);
  const [tables, setTables] = useState<{ id: number; restaurantId: number; name: string; capacity: number }[]>([]);
  const [loading, setLoading] = useState(true);
  const [showForm, setShowForm] = useState(false);
  const [editItem, setEditItem] = useState<ReservationDto | null>(null);
  const [form, setForm] = useState<CreateReservationInput>(EMPTY_FORM);
  const [saving, setSaving] = useState(false);

  const load = useCallback(async () => {
    try {
      setLoading(true);
      const [r, t, res] = await Promise.all([
        getRestaurants(),
        getRestaurantTables(),
        getReservations(isoDate(selectedDate)),
      ]);
      setRestaurants(r.map(x => ({ id: x.id, name: x.name })));
      setTables(t.map(x => ({ id: x.id, restaurantId: x.restaurantId, name: x.name, capacity: x.capacity })));
      setItems(res);
    } catch (e) {
      toast.error(toApiFormError(e, "Yüklənmə xətası").message);
    } finally {
      setLoading(false);
    }
  }, [selectedDate]);

  useEffect(() => { void load(); }, [load]);

  function openCreate() {
    setEditItem(null);
    setForm({ ...EMPTY_FORM, reservationDate: isoDate(selectedDate), restaurantId: restaurants[0]?.id ?? 0 });
    setShowForm(true);
  }

  function openEdit(item: ReservationDto) {
    setEditItem(item);
    setForm({
      restaurantId: item.restaurantId,
      tableId: item.tableId,
      guestName: item.guestName,
      guestPhone: item.guestPhone,
      guestEmail: item.guestEmail ?? "",
      guestCount: item.guestCount,
      reservationDate: item.reservationDate.split("T")[0],
      reservationTime: item.reservationTime,
      durationMinutes: item.durationMinutes,
      note: item.note ?? "",
    });
    setShowForm(true);
  }

  async function handleSave() {
    if (!form.guestName.trim() || !form.guestPhone.trim()) {
      toast.error("Ad və telefon mütləqdir."); return;
    }
    if (!form.restaurantId) { toast.error("Restoran seçin."); return; }
    setSaving(true);
    try {
      if (editItem) {
        await updateReservation(editItem.id, form);
        toast.success("Rezervasiya yeniləndi.");
      } else {
        await createReservation(form);
        toast.success("Rezervasiya yaradıldı.");
      }
      setShowForm(false);
      void load();
    } catch (e) {
      toast.error(toApiFormError(e, "Xəta").message);
    } finally {
      setSaving(false);
    }
  }

  async function handleAction(id: number, action: "confirm" | "seat" | "complete" | "cancel" | "noshow") {
    try {
      await changeReservationStatus(id, action);
      void load();
    } catch (e) {
      toast.error(toApiFormError(e, "Status dəyişdirilmədi").message);
    }
  }

  async function handleDelete(id: number) {
    if (!confirm("Rezervasiyanı silmək istəyirsiniz?")) return;
    try {
      await deleteReservation(id);
      toast.success("Silindi.");
      void load();
    } catch (e) {
      toast.error(toApiFormError(e, "Silinmədi").message);
    }
  }

  const visibleTables = tables.filter(t => t.restaurantId === form.restaurantId);

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex flex-col gap-3 sm:flex-row sm:items-end sm:justify-between">
        <div>
          <h1 className="text-2xl font-semibold tracking-tight">Rezervasiyalar</h1>
          <p className="text-muted-foreground text-sm mt-1">Stol bron sistemi</p>
        </div>
        <div className="flex gap-2">
          <Button variant="outline" size="sm" onClick={() => void load()} disabled={loading}>
            <RefreshCw className={cn("h-4 w-4", loading && "animate-spin")} />
          </Button>
          <Button size="sm" onClick={openCreate} className="gap-1.5">
            <Plus className="h-4 w-4" />
            Yeni rezervasiya
          </Button>
        </div>
      </div>

      {/* Date navigation */}
      <div className="flex items-center gap-3">
        <Button variant="outline" size="icon" onClick={() => setSelectedDate(d => addDays(d, -1))}>
          <ChevronLeft className="h-4 w-4" />
        </Button>
        <div className="flex items-center gap-2 flex-1 justify-center">
          <Calendar className="h-4 w-4 text-muted-foreground" />
          <span className="font-medium">{displayDate(selectedDate)}</span>
          {isoDate(selectedDate) === isoDate(new Date()) && (
            <span className="text-xs bg-indigo-100 text-indigo-700 px-2 py-0.5 rounded-full dark:bg-indigo-950 dark:text-indigo-300">Bu gün</span>
          )}
        </div>
        <Button variant="outline" size="icon" onClick={() => setSelectedDate(d => addDays(d, 1))}>
          <ChevronRight className="h-4 w-4" />
        </Button>
        <Button variant="ghost" size="sm" onClick={() => setSelectedDate(new Date())} className="text-xs">
          Bu gün
        </Button>
        <input
          type="date"
          value={isoDate(selectedDate)}
          onChange={e => setSelectedDate(new Date(e.target.value))}
          className="rounded-lg border border-border bg-background px-3 py-1.5 text-sm"
        />
      </div>

      {/* Summary strip */}
      <div className="flex gap-3 flex-wrap text-sm">
        {Object.entries(STATUS_LABEL).map(([key, label]) => {
          const count = items.filter(i => i.status === key).length;
          if (!count) return null;
          return (
            <span key={key} className={cn("px-3 py-1 rounded-full text-xs font-medium", STATUS_COLOR[key])}>
              {label}: {count}
            </span>
          );
        })}
        {items.length === 0 && !loading && (
          <span className="text-muted-foreground text-sm">Bu gün üçün rezervasiya yoxdur.</span>
        )}
      </div>

      {/* Reservations list */}
      {loading ? (
        <div className="space-y-3">
          {Array.from({ length: 4 }).map((_, i) => (
            <div key={i} className="h-24 rounded-xl bg-muted animate-pulse" />
          ))}
        </div>
      ) : (
        <div className="space-y-3">
          {items.map(item => (
            <div key={item.id} className="rounded-xl border border-border bg-card p-4">
              <div className="flex items-start gap-4">
                {/* Time block */}
                <div className="shrink-0 text-center w-16">
                  <div className="text-xl font-bold text-indigo-600 dark:text-indigo-400">{item.reservationTime}</div>
                  <div className="text-[10px] text-muted-foreground">{item.durationMinutes} dəq</div>
                </div>

                {/* Info */}
                <div className="flex-1 min-w-0">
                  <div className="flex items-center gap-2 flex-wrap">
                    <span className="font-semibold">{item.guestName}</span>
                    <StatusBadge status={item.status} />
                    {item.tableName && (
                      <span className="text-xs bg-muted px-2 py-0.5 rounded">
                        {item.tableName}
                      </span>
                    )}
                  </div>
                  <div className="flex gap-4 mt-1.5 flex-wrap text-xs text-muted-foreground">
                    <span className="flex items-center gap-1"><Users className="h-3 w-3" />{item.guestCount} nəfər</span>
                    <span className="flex items-center gap-1"><Phone className="h-3 w-3" />{item.guestPhone}</span>
                    {item.guestEmail && <span className="flex items-center gap-1"><Mail className="h-3 w-3" />{item.guestEmail}</span>}
                    {item.note && <span className="flex items-center gap-1"><StickyNote className="h-3 w-3" />{item.note}</span>}
                  </div>
                  <div className="text-[10px] text-muted-foreground mt-1">{item.restaurantName}</div>
                </div>

                {/* Actions */}
                <div className="flex gap-1.5 flex-wrap shrink-0">
                  {item.status === "Pending" && (
                    <Button size="sm" variant="outline" className="h-7 text-xs gap-1 text-blue-600 border-blue-200"
                      onClick={() => handleAction(item.id, "confirm")}>
                      <Check className="h-3 w-3" />Təsdiqlə
                    </Button>
                  )}
                  {item.status === "Confirmed" && (
                    <Button size="sm" variant="outline" className="h-7 text-xs gap-1 text-indigo-600 border-indigo-200"
                      onClick={() => handleAction(item.id, "seat")}>
                      <Users className="h-3 w-3" />Oturt
                    </Button>
                  )}
                  {item.status === "Seated" && (
                    <Button size="sm" variant="outline" className="h-7 text-xs gap-1 text-emerald-600 border-emerald-200"
                      onClick={() => handleAction(item.id, "complete")}>
                      <Check className="h-3 w-3" />Tamamla
                    </Button>
                  )}
                  {(item.status === "Pending" || item.status === "Confirmed") && (
                    <>
                      <Button size="sm" variant="outline" className="h-7 text-xs gap-1 text-red-600 border-red-200"
                        onClick={() => handleAction(item.id, "cancel")}>
                        <X className="h-3 w-3" />Ləğv et
                      </Button>
                      <Button size="sm" variant="ghost" className="h-7 text-xs"
                        onClick={() => openEdit(item)}>
                        <Edit2 className="h-3 w-3" />
                      </Button>
                    </>
                  )}
                  {item.status === "Pending" && (
                    <Button size="sm" variant="outline" className="h-7 text-xs gap-1 text-amber-600 border-amber-200"
                      onClick={() => handleAction(item.id, "noshow")}>
                      <Clock className="h-3 w-3" />Gəlmədi
                    </Button>
                  )}
                  <Button size="sm" variant="ghost" className="h-7 text-xs text-destructive hover:text-destructive"
                    onClick={() => handleDelete(item.id)}>
                    <Trash2 className="h-3 w-3" />
                  </Button>
                </div>
              </div>
            </div>
          ))}
        </div>
      )}

      {/* Create/Edit form modal */}
      {showForm && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50 p-4">
          <div className="w-full max-w-lg rounded-2xl border border-border bg-background shadow-2xl overflow-y-auto max-h-[90vh]">
            <div className="flex items-center justify-between p-5 border-b border-border">
              <h2 className="font-semibold text-lg">{editItem ? "Rezervasiyanı düzəlt" : "Yeni rezervasiya"}</h2>
              <button onClick={() => setShowForm(false)} className="text-muted-foreground hover:text-foreground">
                <X className="h-5 w-5" />
              </button>
            </div>

            <div className="p-5 space-y-4">
              {/* Restaurant */}
              <div>
                <label className="text-xs text-muted-foreground mb-1 block">Restoran *</label>
                <select
                  value={form.restaurantId}
                  onChange={e => setForm(f => ({ ...f, restaurantId: Number(e.target.value), tableId: null }))}
                  className="w-full rounded-lg border border-border bg-background px-3 py-2 text-sm"
                >
                  <option value={0}>Seçin...</option>
                  {restaurants.map(r => <option key={r.id} value={r.id}>{r.name}</option>)}
                </select>
              </div>

              {/* Guest name + phone */}
              <div className="grid grid-cols-2 gap-3">
                <div>
                  <label className="text-xs text-muted-foreground mb-1 block">Qonağın adı *</label>
                  <Input value={form.guestName} onChange={e => setForm(f => ({ ...f, guestName: e.target.value }))} placeholder="Ad Soyad" />
                </div>
                <div>
                  <label className="text-xs text-muted-foreground mb-1 block">Telefon *</label>
                  <Input value={form.guestPhone} onChange={e => setForm(f => ({ ...f, guestPhone: e.target.value }))} placeholder="+994 50 000 00 00" />
                </div>
              </div>

              {/* Email + guest count */}
              <div className="grid grid-cols-2 gap-3">
                <div>
                  <label className="text-xs text-muted-foreground mb-1 block">E-mail</label>
                  <Input value={form.guestEmail ?? ""} onChange={e => setForm(f => ({ ...f, guestEmail: e.target.value }))} placeholder="email@mail.com" type="email" />
                </div>
                <div>
                  <label className="text-xs text-muted-foreground mb-1 block">Nəfər sayı *</label>
                  <Input type="number" min={1} max={50} value={form.guestCount}
                    onChange={e => setForm(f => ({ ...f, guestCount: Number(e.target.value) }))} />
                </div>
              </div>

              {/* Date + time + duration */}
              <div className="grid grid-cols-3 gap-3">
                <div>
                  <label className="text-xs text-muted-foreground mb-1 block">Tarix *</label>
                  <Input type="date" value={form.reservationDate}
                    onChange={e => setForm(f => ({ ...f, reservationDate: e.target.value }))} />
                </div>
                <div>
                  <label className="text-xs text-muted-foreground mb-1 block">Vaxt *</label>
                  <Input type="time" value={form.reservationTime}
                    onChange={e => setForm(f => ({ ...f, reservationTime: e.target.value }))} />
                </div>
                <div>
                  <label className="text-xs text-muted-foreground mb-1 block">Müddət (dəq)</label>
                  <Input type="number" min={30} max={480} step={15} value={form.durationMinutes}
                    onChange={e => setForm(f => ({ ...f, durationMinutes: Number(e.target.value) }))} />
                </div>
              </div>

              {/* Table */}
              <div>
                <label className="text-xs text-muted-foreground mb-1 block">Masa (ixtiyari)</label>
                <select
                  value={form.tableId ?? ""}
                  onChange={e => setForm(f => ({ ...f, tableId: e.target.value ? Number(e.target.value) : null }))}
                  className="w-full rounded-lg border border-border bg-background px-3 py-2 text-sm"
                >
                  <option value="">Seçilməyib</option>
                  {visibleTables.map(t => (
                    <option key={t.id} value={t.id}>{t.name} ({t.capacity} nəfər)</option>
                  ))}
                </select>
              </div>

              {/* Note */}
              <div>
                <label className="text-xs text-muted-foreground mb-1 block">Qeyd</label>
                <textarea
                  value={form.note ?? ""}
                  onChange={e => setForm(f => ({ ...f, note: e.target.value }))}
                  placeholder="Xüsusi istəklər, allergiyalar..."
                  rows={2}
                  className="w-full rounded-lg border border-border bg-background px-3 py-2 text-sm resize-none"
                />
              </div>
            </div>

            <div className="flex justify-end gap-2 p-5 border-t border-border">
              <Button variant="outline" onClick={() => setShowForm(false)}>Ləğv et</Button>
              <Button onClick={handleSave} disabled={saving}>
                {saving ? "Saxlanır..." : editItem ? "Yadda saxla" : "Yarat"}
              </Button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
