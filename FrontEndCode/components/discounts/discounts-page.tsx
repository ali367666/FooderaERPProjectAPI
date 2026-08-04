"use client";

import { useCallback, useEffect, useState } from "react";
import { toast } from "sonner";
import {
  getDiscounts, createDiscount, updateDiscount, deleteDiscount,
  type DiscountDto, type DiscountInput, type DiscountType,
} from "@/lib/services/discount-service";
import { toApiFormError } from "@/lib/api-error";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { cn } from "@/lib/utils";
import {
  Plus, RefreshCw, X, Trash2, Edit2, Tag, Percent, Wallet,
  Clock, Calendar, CheckCircle2, XCircle,
} from "lucide-react";

function fmtAzn(n: number) {
  return new Intl.NumberFormat("az-AZ", { minimumFractionDigits: 2, maximumFractionDigits: 2 }).format(n) + " ₼";
}

function fmtDate(s: string) {
  return new Date(s).toLocaleDateString("az-AZ");
}

const EMPTY_FORM: DiscountInput = {
  code: "", name: "", type: "Percentage", value: 10,
  minOrderAmount: null, maxDiscountAmount: null,
  startDate: new Date().toISOString().split("T")[0],
  endDate: new Date(Date.now() + 30 * 86400000).toISOString().split("T")[0],
  startTime: null, endTime: null, maxUsageCount: null, isActive: true,
};

export function DiscountsPage() {
  const [items, setItems] = useState<DiscountDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [showForm, setShowForm] = useState(false);
  const [editItem, setEditItem] = useState<DiscountDto | null>(null);
  const [form, setForm] = useState<DiscountInput>(EMPTY_FORM);
  const [saving, setSaving] = useState(false);
  const [useTimeWindow, setUseTimeWindow] = useState(false);

  const load = useCallback(async () => {
    try {
      setLoading(true);
      setItems(await getDiscounts());
    } catch (e) {
      toast.error(toApiFormError(e, "Yüklənmə xətası").message);
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => { void load(); }, [load]);

  function openCreate() {
    setEditItem(null);
    setForm(EMPTY_FORM);
    setUseTimeWindow(false);
    setShowForm(true);
  }

  function openEdit(item: DiscountDto) {
    setEditItem(item);
    setForm({
      code: item.code, name: item.name, type: item.type, value: item.value,
      minOrderAmount: item.minOrderAmount, maxDiscountAmount: item.maxDiscountAmount,
      startDate: item.startDate.split("T")[0], endDate: item.endDate.split("T")[0],
      startTime: item.startTime, endTime: item.endTime,
      maxUsageCount: item.maxUsageCount, isActive: item.isActive,
    });
    setUseTimeWindow(!!item.startTime);
    setShowForm(true);
  }

  async function handleSave() {
    if (!form.code.trim() || !form.name.trim()) {
      toast.error("Kod və ad mütləqdir."); return;
    }
    if (form.value <= 0) { toast.error("Dəyər sıfırdan böyük olmalıdır."); return; }
    setSaving(true);
    try {
      const payload: DiscountInput = {
        ...form,
        startTime: useTimeWindow ? form.startTime : null,
        endTime: useTimeWindow ? form.endTime : null,
      };
      if (editItem) {
        await updateDiscount(editItem.id, payload);
        toast.success("Endirim yeniləndi.");
      } else {
        await createDiscount(payload);
        toast.success("Endirim yaradıldı.");
      }
      setShowForm(false);
      void load();
    } catch (e) {
      toast.error(toApiFormError(e, "Xəta").message);
    } finally {
      setSaving(false);
    }
  }

  async function handleToggleActive(item: DiscountDto) {
    try {
      await updateDiscount(item.id, {
        code: item.code, name: item.name, type: item.type, value: item.value,
        minOrderAmount: item.minOrderAmount, maxDiscountAmount: item.maxDiscountAmount,
        startDate: item.startDate.split("T")[0], endDate: item.endDate.split("T")[0],
        startTime: item.startTime, endTime: item.endTime,
        maxUsageCount: item.maxUsageCount, isActive: !item.isActive,
      });
      void load();
    } catch (e) {
      toast.error(toApiFormError(e, "Xəta").message);
    }
  }

  async function handleDelete(id: number) {
    if (!confirm("Endirimi silmək istəyirsiniz?")) return;
    try {
      await deleteDiscount(id);
      toast.success("Silindi.");
      void load();
    } catch (e) {
      toast.error(toApiFormError(e, "Silinmədi").message);
    }
  }

  return (
    <div className="space-y-6">
      <div className="flex flex-col gap-3 sm:flex-row sm:items-end sm:justify-between">
        <div>
          <h1 className="text-2xl font-semibold tracking-tight">Endirimlər</h1>
          <p className="text-muted-foreground text-sm mt-1">Kupon kodları, faiz/məbləğ endirimlər, happy hour</p>
        </div>
        <div className="flex gap-2">
          <Button variant="outline" size="sm" onClick={() => void load()} disabled={loading}>
            <RefreshCw className={cn("h-4 w-4", loading && "animate-spin")} />
          </Button>
          <Button size="sm" onClick={openCreate} className="gap-1.5">
            <Plus className="h-4 w-4" />
            Yeni endirim
          </Button>
        </div>
      </div>

      {loading ? (
        <div className="space-y-3">
          {Array.from({ length: 3 }).map((_, i) => <div key={i} className="h-24 rounded-xl bg-muted animate-pulse" />)}
        </div>
      ) : items.length === 0 ? (
        <div className="py-16 text-center text-muted-foreground text-sm">Heç bir endirim yaradılmayıb.</div>
      ) : (
        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          {items.map(item => (
            <div key={item.id} className="rounded-xl border border-border bg-card p-4">
              <div className="flex items-start justify-between gap-2">
                <div className="flex items-center gap-2">
                  <div className={cn(
                    "rounded-lg p-2",
                    item.type === "Percentage" ? "bg-indigo-100 text-indigo-600 dark:bg-indigo-950 dark:text-indigo-400"
                      : "bg-emerald-100 text-emerald-600 dark:bg-emerald-950 dark:text-emerald-400",
                  )}>
                    {item.type === "Percentage" ? <Percent className="h-4 w-4" /> : <Wallet className="h-4 w-4" />}
                  </div>
                  <div>
                    <div className="flex items-center gap-2">
                      <span className="font-mono font-bold text-sm bg-muted px-2 py-0.5 rounded">{item.code}</span>
                      {item.isCurrentlyValid ? (
                        <span className="text-[10px] flex items-center gap-0.5 text-emerald-600"><CheckCircle2 className="h-3 w-3" />Aktiv</span>
                      ) : (
                        <span className="text-[10px] flex items-center gap-0.5 text-muted-foreground"><XCircle className="h-3 w-3" />Qeyri-aktiv</span>
                      )}
                    </div>
                    <p className="text-sm font-medium mt-0.5">{item.name}</p>
                  </div>
                </div>
                <span className="text-lg font-bold text-primary">
                  {item.type === "Percentage" ? `${item.value}%` : fmtAzn(item.value)}
                </span>
              </div>

              <div className="flex flex-wrap gap-3 mt-3 text-xs text-muted-foreground">
                <span className="flex items-center gap-1"><Calendar className="h-3 w-3" />{fmtDate(item.startDate)} – {fmtDate(item.endDate)}</span>
                {item.startTime && (
                  <span className="flex items-center gap-1"><Clock className="h-3 w-3" />{item.startTime}–{item.endTime}</span>
                )}
                {item.minOrderAmount && (
                  <span className="flex items-center gap-1"><Tag className="h-3 w-3" />min {fmtAzn(item.minOrderAmount)}</span>
                )}
                {item.maxUsageCount && (
                  <span>{item.usedCount}/{item.maxUsageCount} istifadə</span>
                )}
                {!item.maxUsageCount && item.usedCount > 0 && (
                  <span>{item.usedCount} dəfə istifadə olunub</span>
                )}
              </div>

              <div className="flex gap-2 mt-3">
                <Button size="sm" variant="outline" className="h-7 text-xs" onClick={() => openEdit(item)}>
                  <Edit2 className="h-3 w-3 mr-1" />Düzəlt
                </Button>
                <Button
                  size="sm" variant="outline" className="h-7 text-xs"
                  onClick={() => handleToggleActive(item)}
                >
                  {item.isActive ? "Deaktiv et" : "Aktiv et"}
                </Button>
                <Button size="sm" variant="ghost" className="h-7 text-xs text-destructive hover:text-destructive ml-auto"
                  onClick={() => handleDelete(item.id)}>
                  <Trash2 className="h-3 w-3" />
                </Button>
              </div>
            </div>
          ))}
        </div>
      )}

      {showForm && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50 p-4">
          <div className="w-full max-w-lg rounded-2xl border border-border bg-background shadow-2xl overflow-y-auto max-h-[90vh]">
            <div className="flex items-center justify-between p-5 border-b border-border">
              <h2 className="font-semibold text-lg">{editItem ? "Endirimi düzəlt" : "Yeni endirim"}</h2>
              <button onClick={() => setShowForm(false)} className="text-muted-foreground hover:text-foreground">
                <X className="h-5 w-5" />
              </button>
            </div>

            <div className="p-5 space-y-4">
              <div className="grid grid-cols-2 gap-3">
                <div>
                  <label className="text-xs text-muted-foreground mb-1 block">Kupon kodu *</label>
                  <Input
                    value={form.code}
                    onChange={e => setForm(f => ({ ...f, code: e.target.value.toUpperCase() }))}
                    placeholder="SUMMER10"
                    className="font-mono"
                  />
                </div>
                <div>
                  <label className="text-xs text-muted-foreground mb-1 block">Ad *</label>
                  <Input value={form.name} onChange={e => setForm(f => ({ ...f, name: e.target.value }))} placeholder="Yay endirimi" />
                </div>
              </div>

              <div>
                <label className="text-xs text-muted-foreground mb-1.5 block">Endirim növü</label>
                <div className="flex gap-2">
                  {(["Percentage", "FixedAmount"] as DiscountType[]).map(t => (
                    <button
                      key={t}
                      onClick={() => setForm(f => ({ ...f, type: t }))}
                      className={cn(
                        "flex-1 px-3 py-2 rounded-lg text-sm border transition-colors",
                        form.type === t ? "bg-primary text-primary-foreground border-primary" : "border-border hover:border-primary/40",
                      )}
                    >
                      {t === "Percentage" ? "Faiz (%)" : "Sabit məbləğ (₼)"}
                    </button>
                  ))}
                </div>
              </div>

              <div className="grid grid-cols-2 gap-3">
                <div>
                  <label className="text-xs text-muted-foreground mb-1 block">
                    Dəyər * {form.type === "Percentage" ? "(%)" : "(₼)"}
                  </label>
                  <Input type="number" min={0} max={form.type === "Percentage" ? 100 : undefined} step="0.01"
                    value={form.value} onChange={e => setForm(f => ({ ...f, value: Number(e.target.value) }))} />
                </div>
                {form.type === "Percentage" && (
                  <div>
                    <label className="text-xs text-muted-foreground mb-1 block">Maks. endirim (₼)</label>
                    <Input type="number" min={0} step="0.01"
                      value={form.maxDiscountAmount ?? ""}
                      onChange={e => setForm(f => ({ ...f, maxDiscountAmount: e.target.value ? Number(e.target.value) : null }))}
                      placeholder="Limitsiz" />
                  </div>
                )}
              </div>

              <div>
                <label className="text-xs text-muted-foreground mb-1 block">Minimum sifariş məbləği (₼)</label>
                <Input type="number" min={0} step="0.01"
                  value={form.minOrderAmount ?? ""}
                  onChange={e => setForm(f => ({ ...f, minOrderAmount: e.target.value ? Number(e.target.value) : null }))}
                  placeholder="Məhdudiyyət yoxdur" />
              </div>

              <div className="grid grid-cols-2 gap-3">
                <div>
                  <label className="text-xs text-muted-foreground mb-1 block">Başlama tarixi *</label>
                  <Input type="date" value={form.startDate} onChange={e => setForm(f => ({ ...f, startDate: e.target.value }))} />
                </div>
                <div>
                  <label className="text-xs text-muted-foreground mb-1 block">Bitmə tarixi *</label>
                  <Input type="date" value={form.endDate} onChange={e => setForm(f => ({ ...f, endDate: e.target.value }))} />
                </div>
              </div>

              <div>
                <label className="flex items-center gap-2 text-xs text-muted-foreground mb-2">
                  <input type="checkbox" checked={useTimeWindow} onChange={e => setUseTimeWindow(e.target.checked)} />
                  Gündəlik saat aralığı (happy hour)
                </label>
                {useTimeWindow && (
                  <div className="grid grid-cols-2 gap-3">
                    <Input type="time" value={form.startTime ?? "17:00"}
                      onChange={e => setForm(f => ({ ...f, startTime: e.target.value }))} />
                    <Input type="time" value={form.endTime ?? "19:00"}
                      onChange={e => setForm(f => ({ ...f, endTime: e.target.value }))} />
                  </div>
                )}
              </div>

              <div>
                <label className="text-xs text-muted-foreground mb-1 block">Maksimum istifadə sayı</label>
                <Input type="number" min={1}
                  value={form.maxUsageCount ?? ""}
                  onChange={e => setForm(f => ({ ...f, maxUsageCount: e.target.value ? Number(e.target.value) : null }))}
                  placeholder="Limitsiz" />
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
