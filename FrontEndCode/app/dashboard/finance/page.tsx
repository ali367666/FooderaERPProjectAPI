"use client";

import { useCallback, useEffect, useState } from "react";
import { ChevronLeft, ChevronRight, Pencil, Plus, Trash2 } from "lucide-react";
import { toast } from "sonner";
import { Button } from "@/components/ui/button";
import { Dialog, DialogContent, DialogHeader, DialogTitle } from "@/components/ui/dialog";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { useSelectedRestaurant } from "@/contexts/selected-restaurant-context";
import { formatCurrency } from "@/lib/format-currency";
import { toDateInputValue } from "@/lib/report-period";
import { cn } from "@/lib/utils";
import {
  FinanceEntryType,
  FinanceGranularity,
  createFinanceEntry,
  deleteFinanceEntry,
  getFinanceEntry,
  getFinanceReport,
  updateFinanceEntry,
  type FinanceEntryTypeValue,
  type FinanceGranularityValue,
  type FinanceLine,
  type FinanceNameTotal,
  type FinanceReport,
} from "@/lib/services/finance-service";

const GRANULARITIES: { key: FinanceGranularityValue; label: string }[] = [
  { key: FinanceGranularity.Week, label: "Həftəlik" },
  { key: FinanceGranularity.Month, label: "Aylıq" },
  { key: FinanceGranularity.Year, label: "İllik" },
];

const SOURCE_LABELS: Record<string, string> = {
  Manual: "Əl ilə",
  Sales: "Satış",
  StockPurchase: "Alış",
  CashRegister: "Kassa",
  SaleReturn: "Qaytarma",
};

function formatDate(value: string): string {
  const [y, m, d] = value.slice(0, 10).split("-");
  return `${d}.${m}.${y}`;
}

/** Moves the anchor day one week / month / year back or forward. */
function shiftDate(date: string, granularity: FinanceGranularityValue, direction: 1 | -1): string {
  const d = new Date(`${date}T12:00:00`);
  if (granularity === FinanceGranularity.Week) d.setDate(d.getDate() + 7 * direction);
  else if (granularity === FinanceGranularity.Month) d.setMonth(d.getMonth() + direction, 1);
  else d.setFullYear(d.getFullYear() + direction, 0, 1);
  return toDateInputValue(d);
}

function profitClass(value: number): string {
  return value < 0 ? "text-red-600" : "text-emerald-600";
}

function NameTotals({ title, rows }: { title: string; rows: FinanceNameTotal[] }) {
  return (
    <div className="rounded-lg border bg-card p-4">
      <h2 className="mb-3 font-semibold">{title}</h2>
      {rows.length === 0 ? (
        <p className="text-sm text-muted-foreground">Qeyd yoxdur.</p>
      ) : (
        <table className="w-full text-sm">
          <tbody>
            {rows.map((r) => (
              <tr key={r.name} className="border-t first:border-t-0">
                <td className="py-1.5">{r.name}</td>
                <td className="py-1.5 text-right text-muted-foreground">{r.count} dəfə</td>
                <td className="py-1.5 text-right font-medium">{formatCurrency(r.total)}</td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </div>
  );
}

export default function FinancePage() {
  const { selectedRestaurantId } = useSelectedRestaurant();
  const [granularity, setGranularity] = useState<FinanceGranularityValue>(FinanceGranularity.Month);
  const [date, setDate] = useState(toDateInputValue(new Date()));
  const [report, setReport] = useState<FinanceReport | null>(null);
  const [loading, setLoading] = useState(true);

  const [dialogOpen, setDialogOpen] = useState(false);
  const [saving, setSaving] = useState(false);
  const [editingId, setEditingId] = useState<number | null>(null);
  const [type, setType] = useState<FinanceEntryTypeValue>(FinanceEntryType.Expense);
  const [name, setName] = useState("");
  const [amount, setAmount] = useState("");
  const [entryDate, setEntryDate] = useState(toDateInputValue(new Date()));
  const [note, setNote] = useState("");

  const load = useCallback(async () => {
    setLoading(true);
    try {
      setReport(await getFinanceReport({ granularity, date, restaurantId: selectedRestaurantId }));
    } catch (e) {
      toast.error(e instanceof Error ? e.message : "Maliyyə hesabatı yüklənmədi.");
      setReport(null);
    } finally {
      setLoading(false);
    }
  }, [granularity, date, selectedRestaurantId]);

  useEffect(() => {
    void load();
  }, [load]);

  const openAdd = (entryType: FinanceEntryTypeValue) => {
    setEditingId(null);
    setType(entryType);
    setName("");
    setAmount("");
    setEntryDate(toDateInputValue(new Date()));
    setNote("");
    setDialogOpen(true);
  };

  const openEdit = async (line: FinanceLine, dayDate: string) => {
    if (line.entryId == null) return;
    try {
      const entry = await getFinanceEntry(line.entryId, dayDate.slice(0, 10));
      if (!entry) {
        toast.error("Qeyd tapılmadı.");
        return;
      }
      setEditingId(entry.id);
      setType(entry.type);
      setName(entry.name);
      setAmount(String(entry.amount));
      setEntryDate(entry.entryDate.slice(0, 10));
      setNote(entry.note ?? "");
      setDialogOpen(true);
    } catch (e) {
      toast.error(e instanceof Error ? e.message : "Qeyd yüklənmədi.");
    }
  };

  const handleDelete = async (line: FinanceLine) => {
    if (line.entryId == null) return;
    if (!window.confirm(`"${line.name}" qeydini silmək istəyirsiniz?`)) return;
    try {
      await deleteFinanceEntry(line.entryId);
      toast.success("Qeyd silindi.");
      await load();
    } catch (e) {
      toast.error(e instanceof Error ? e.message : "Qeyd silinmədi.");
    }
  };

  const handleSave = async () => {
    const value = Number(amount.replace(",", "."));
    if (!name.trim()) return toast.error("Ad vacibdir.");
    if (!Number.isFinite(value) || value <= 0) return toast.error("Məbləğ 0-dan böyük olmalıdır.");
    const payload = {
      restaurantId: selectedRestaurantId ?? null,
      type,
      name: name.trim(),
      amount: value,
      entryDate,
      note: note.trim() || null,
    };
    setSaving(true);
    try {
      if (editingId == null) await createFinanceEntry(payload);
      else await updateFinanceEntry(editingId, payload);
      toast.success(editingId == null ? "Qeyd əlavə edildi." : "Qeyd yeniləndi.");
      setDialogOpen(false);
      await load();
    } catch (e) {
      toast.error(e instanceof Error ? e.message : "Yadda saxlanılmadı.");
    } finally {
      setSaving(false);
    }
  };

  const renderLines = (lines: FinanceLine[], dayDate: string, color: string) =>
    lines.map((line, i) => (
      <div key={`${line.name}-${line.entryId ?? i}`} className="flex items-center justify-between gap-2 py-0.5 text-sm">
        <span className="min-w-0 truncate">
          {line.name}
          <span className="ml-2 text-xs text-muted-foreground">{SOURCE_LABELS[line.source] ?? line.source}</span>
          {line.note && <span className="ml-2 text-xs text-muted-foreground">· {line.note}</span>}
        </span>
        <span className="flex shrink-0 items-center gap-1">
          <span className={cn("font-medium", color)}>{formatCurrency(line.amount)}</span>
          {line.entryId != null && (
            <>
              <Button variant="ghost" size="icon" className="h-6 w-6" onClick={() => void openEdit(line, dayDate)}>
                <Pencil className="h-3.5 w-3.5" />
              </Button>
              <Button variant="ghost" size="icon" className="h-6 w-6" onClick={() => void handleDelete(line)}>
                <Trash2 className="h-3.5 w-3.5" />
              </Button>
            </>
          )}
        </span>
      </div>
    ));

  return (
    <div className="space-y-6">
      <div className="flex flex-wrap items-start justify-between gap-3">
        <div>
          <h1 className="text-3xl font-bold text-foreground">Maliyyə</h1>
          <p className="mt-1 text-muted-foreground">
            Şirkətin gəlir və xərcləri, ay sonu qalan mənfəət. Satış, mal alışı və kassa xərcləri avtomatik gəlir.
          </p>
        </div>
        <div className="flex gap-2">
          <Button variant="outline" onClick={() => openAdd(FinanceEntryType.Income)}>
            <Plus className="mr-1 h-4 w-4" /> Gəlir
          </Button>
          <Button onClick={() => openAdd(FinanceEntryType.Expense)}>
            <Plus className="mr-1 h-4 w-4" /> Xərc
          </Button>
        </div>
      </div>

      <div className="flex flex-wrap items-center gap-2">
        {GRANULARITIES.map((g) => (
          <Button
            key={g.key}
            size="sm"
            variant={granularity === g.key ? "default" : "outline"}
            onClick={() => setGranularity(g.key)}
          >
            {g.label}
          </Button>
        ))}
        <Button size="icon" variant="outline" className="h-8 w-8" onClick={() => setDate(shiftDate(date, granularity, -1))}>
          <ChevronLeft className="h-4 w-4" />
        </Button>
        <Input type="date" className="h-8 w-40" value={date} onChange={(e) => e.target.value && setDate(e.target.value)} />
        <Button size="icon" variant="outline" className="h-8 w-8" onClick={() => setDate(shiftDate(date, granularity, 1))}>
          <ChevronRight className="h-4 w-4" />
        </Button>
        {report && (
          <span className="text-sm text-muted-foreground">
            {formatDate(report.fromDate)} – {formatDate(report.toDate)}
          </span>
        )}
      </div>

      {loading ? (
        <div className="text-sm text-muted-foreground">Yüklənir…</div>
      ) : !report ? (
        <div className="text-sm text-muted-foreground">Məlumat yoxdur.</div>
      ) : (
        <>
          <div className="grid gap-3 sm:grid-cols-3">
            <div className="rounded-lg border bg-card p-4">
              <div className="text-sm text-muted-foreground">Ümumi gəlir</div>
              <div className="mt-1 text-2xl font-bold text-emerald-600">{formatCurrency(report.totalIncome)}</div>
            </div>
            <div className="rounded-lg border bg-card p-4">
              <div className="text-sm text-muted-foreground">Ümumi xərc</div>
              <div className="mt-1 text-2xl font-bold text-red-600">{formatCurrency(report.totalExpense)}</div>
            </div>
            <div className="rounded-lg border bg-card p-4">
              <div className="text-sm text-muted-foreground">Qalan mənfəət</div>
              <div className={cn("mt-1 text-2xl font-bold", profitClass(report.profit))}>
                {formatCurrency(report.profit)}
              </div>
            </div>
          </div>

          <div className="overflow-x-auto rounded-lg border bg-card">
            <table className="w-full text-sm">
              <thead className="bg-muted/50 text-left">
                <tr>
                  <th className="px-3 py-2">{granularity === FinanceGranularity.Year ? "Ay" : "Gün"}</th>
                  <th className="px-3 py-2 text-right">Gəlir</th>
                  <th className="px-3 py-2 text-right">Xərc</th>
                  <th className="px-3 py-2 text-right">Mənfəət</th>
                </tr>
              </thead>
              <tbody>
                {report.buckets.map((b) => (
                  <tr key={b.fromDate} className="border-t">
                    <td className="px-3 py-1.5">{b.label}</td>
                    <td className="px-3 py-1.5 text-right">{formatCurrency(b.income)}</td>
                    <td className="px-3 py-1.5 text-right">{formatCurrency(b.expense)}</td>
                    <td className={cn("px-3 py-1.5 text-right font-medium", profitClass(b.profit))}>
                      {formatCurrency(b.profit)}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>

          <div className="grid gap-4 lg:grid-cols-2">
            <NameTotals title="Gəlirlər (ad üzrə cəm)" rows={report.incomeByName} />
            <NameTotals title="Xərclər (ad üzrə cəm)" rows={report.expenseByName} />
          </div>

          <div className="space-y-3">
            <h2 className="text-xl font-semibold">Günlük təfərrüat</h2>
            {report.days.length === 0 && <p className="text-sm text-muted-foreground">Bu dövrdə qeyd yoxdur.</p>}
            {report.days.map((day) => (
              <div key={day.date} className="rounded-lg border bg-card p-4">
                <div className="mb-2 flex flex-wrap items-center justify-between gap-2">
                  <span className="font-semibold">{formatDate(day.date)}</span>
                  <span className="text-sm">
                    <span className="text-emerald-600">{formatCurrency(day.income)}</span>
                    {" − "}
                    <span className="text-red-600">{formatCurrency(day.expense)}</span>
                    {" = "}
                    <span className={cn("font-semibold", profitClass(day.profit))}>{formatCurrency(day.profit)}</span>
                  </span>
                </div>
                <div className="grid gap-x-8 gap-y-2 md:grid-cols-2">
                  <div>
                    <div className="mb-1 text-xs font-medium uppercase text-muted-foreground">Gəlir</div>
                    {renderLines(day.incomeLines, day.date, "text-emerald-600")}
                  </div>
                  <div>
                    <div className="mb-1 text-xs font-medium uppercase text-muted-foreground">Xərc</div>
                    {renderLines(day.expenseLines, day.date, "text-red-600")}
                  </div>
                </div>
              </div>
            ))}
          </div>
        </>
      )}

      <Dialog open={dialogOpen} onOpenChange={setDialogOpen}>
        <DialogContent className="sm:max-w-md">
          <DialogHeader>
            <DialogTitle>
              {editingId == null ? "Yeni qeyd" : "Qeydi redaktə et"} —{" "}
              {type === FinanceEntryType.Income ? "Gəlir" : "Xərc"}
            </DialogTitle>
          </DialogHeader>
          <div className="space-y-3">
            <div>
              <Label htmlFor="fin-name">Ad</Label>
              <Input
                id="fin-name"
                className="mt-1"
                placeholder={type === FinanceEntryType.Income ? "məs. Digər gəlir" : "məs. İcarə, Elektrik, Maaş"}
                value={name}
                onChange={(e) => setName(e.target.value)}
              />
            </div>
            <div className="grid grid-cols-2 gap-3">
              <div>
                <Label htmlFor="fin-amount">Məbləğ (AZN)</Label>
                <Input
                  id="fin-amount"
                  className="mt-1"
                  inputMode="decimal"
                  value={amount}
                  onChange={(e) => setAmount(e.target.value)}
                />
              </div>
              <div>
                <Label htmlFor="fin-date">Tarix</Label>
                <Input
                  id="fin-date"
                  type="date"
                  className="mt-1"
                  value={entryDate}
                  onChange={(e) => setEntryDate(e.target.value)}
                />
              </div>
            </div>
            <div>
              <Label htmlFor="fin-note">Qeyd</Label>
              <Input id="fin-note" className="mt-1" value={note} onChange={(e) => setNote(e.target.value)} />
            </div>
          </div>
          <div className="mt-4 flex justify-end gap-2">
            <Button variant="outline" onClick={() => setDialogOpen(false)} disabled={saving}>
              Ləğv et
            </Button>
            <Button onClick={() => void handleSave()} disabled={saving}>
              {saving ? "Saxlanılır…" : "Saxla"}
            </Button>
          </div>
        </DialogContent>
      </Dialog>
    </div>
  );
}
