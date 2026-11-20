"use client";

import { useCallback, useEffect, useMemo, useState } from "react";
import { Plus, Printer, Settings2, Trash2 } from "lucide-react";
import { toast } from "sonner";
import ReversePaymentDialog from "@/components/store/reverse-payment-dialog";
import { TimeRangeInputs, useTimeRangeEnabled } from "@/components/store/time-range-inputs";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from "@/components/ui/dialog";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { formatCurrency } from "@/lib/format-currency";
import { toDateInputValue, withTime } from "@/lib/report-period";
import { printStoreExpense } from "@/lib/store/expense-print";
import { useStorePermissions } from "@/lib/store/use-store-permissions";
import { cn } from "@/lib/utils";
import {
  cancelStoreExpense,
  createStoreExpense,
  getStoreExpenseItems,
  getStoreExpenses,
  getStoreRegisters,
  saveStoreExpenseItem,
  type StoreCashRegister,
  type StoreExpense,
  type StoreExpenseItem,
  type StoreExpenseList,
} from "@/lib/store/store-service";

const SELECT_CLASS = "h-9 w-full rounded-md border border-input bg-background px-2 text-sm";
const dateTime = (iso: string) => {
  const d = new Date(iso);
  return `${d.toLocaleDateString("az-AZ")} ${d.toLocaleTimeString("az-AZ", { hour: "2-digit", minute: "2-digit" })}`;
};

type DraftLine = { registerId: number | ""; payee: string; itemId: number | ""; amount: string; note: string };
const emptyLine = (registerId: number | ""): DraftLine => ({ registerId, payee: "", itemId: "", amount: "", note: "" });

/** Admin: expense documents (digər xərclər) - money paid out of the tills for expense items such as salary, rent or utilities. */
export default function StoreExpensesPage() {
  const perms = useStorePermissions();
  const canCreate = perms.has("StoreCash.MovementOut");
  const canManageItems = perms.has("StoreRegister.Manage");
  const timesOn = useTimeRangeEnabled();

  const today = toDateInputValue(new Date());
  const monthStart = toDateInputValue(new Date(new Date().getFullYear(), new Date().getMonth(), 1));
  const [from, setFrom] = useState(monthStart);
  const [to, setTo] = useState(today);
  const [fromTime, setFromTime] = useState("");
  const [toTime, setToTime] = useState("");
  const [registerId, setRegisterId] = useState<number | "">("");
  const [itemId, setItemId] = useState<number | "">("");
  const [payee, setPayee] = useState("");
  const [codeInput, setCodeInput] = useState("");
  const [code, setCode] = useState("");
  const [showCancelled, setShowCancelled] = useState(true);

  const [registers, setRegisters] = useState<StoreCashRegister[]>([]);
  const [items, setItems] = useState<StoreExpenseItem[]>([]);
  const [list, setList] = useState<StoreExpenseList | null>(null);
  const [loading, setLoading] = useState(true);

  const [selected, setSelected] = useState<StoreExpense | null>(null);
  const [cancelFor, setCancelFor] = useState<StoreExpense | null>(null);

  const [creating, setCreating] = useState(false);
  const [lines, setLines] = useState<DraftLine[]>([]);
  const [docNote, setDocNote] = useState("");
  const [saving, setSaving] = useState(false);

  const [itemsOpen, setItemsOpen] = useState(false);
  const [newItem, setNewItem] = useState("");

  const loadRefs = useCallback(() => {
    getStoreRegisters().then(setRegisters).catch(() => setRegisters([]));
    getStoreExpenseItems(true).then(setItems).catch(() => setItems([]));
  }, []);

  useEffect(() => {
    loadRefs();
  }, [loadRefs]);

  useEffect(() => {
    const t = setTimeout(() => setCode(codeInput.trim()), 300);
    return () => clearTimeout(t);
  }, [codeInput]);

  const load = useCallback(async () => {
    setLoading(true);
    try {
      setList(
        await getStoreExpenses({
          from: timesOn ? withTime(from, fromTime) : from,
          to: timesOn ? withTime(to, toTime) : to,
          registerId: registerId === "" ? null : registerId,
          expenseItemId: itemId === "" ? null : itemId,
          payee,
          code: code || undefined,
          includeCancelled: showCancelled,
        }),
      );
    } catch (e) {
      toast.error(e instanceof Error ? e.message : "Xərclər yüklənmədi.");
    } finally {
      setLoading(false);
    }
  }, [from, to, fromTime, toTime, timesOn, registerId, itemId, payee, code, showCancelled]);

  useEffect(() => {
    void load();
  }, [load]);

  const activeItems = useMemo(() => items.filter((i) => i.isActive), [items]);
  const openRegisters = useMemo(() => registers.filter((r) => r.isActive), [registers]);

  // ---- create -----------------------------------------------------------------------------
  const startCreate = () => {
    const first = openRegisters.find((r) => r.openShiftId != null)?.id ?? "";
    setLines([emptyLine(first)]);
    setDocNote("");
    setCreating(true);
  };
  const setLine = (i: number, patch: Partial<DraftLine>) => setLines((prev) => prev.map((l, idx) => (idx === i ? { ...l, ...patch } : l)));
  const draftTotal = lines.reduce((s, l) => s + (Number(l.amount.replace(",", ".")) || 0), 0);

  const submit = async (andPrint: boolean) => {
    for (const [i, l] of lines.entries()) {
      if (l.registerId === "") return void toast.error(`${i + 1}-ci sətirdə kassanı seçin.`);
      if (l.itemId === "") return void toast.error(`${i + 1}-ci sətirdə xərc maddəsini seçin.`);
      const amount = Number(l.amount.replace(",", "."));
      if (!Number.isFinite(amount) || amount <= 0) return void toast.error(`${i + 1}-ci sətirdə məbləği yazın.`);
    }
    setSaving(true);
    try {
      const doc = await createStoreExpense({
        note: docNote.trim() || null,
        lines: lines.map((l) => ({
          registerId: l.registerId as number,
          expenseItemId: l.itemId as number,
          payee: l.payee.trim() || null,
          amount: Number(l.amount.replace(",", ".")),
          note: l.note.trim() || null,
        })),
      });
      toast.success(`Xərc sənədi yazıldı: ${doc.documentNo}`);
      setCreating(false);
      if (andPrint && !printStoreExpense(doc)) toast.error("Çap pəncərəsi açılmadı — popup bloklamasını söndürün.");
      void load();
      loadRefs();
    } catch (e) {
      toast.error(e instanceof Error ? e.message : "Xərc sənədi yazılmadı.");
    } finally {
      setSaving(false);
    }
  };

  const cancel = async (reason: string) => {
    if (!cancelFor) return;
    await cancelStoreExpense(cancelFor.id, reason);
    toast.success("Xərc sənədi ləğv edildi, pul kassaya qaytarıldı.");
    setSelected(null);
    void load();
  };

  const addItem = async () => {
    if (!newItem.trim()) return;
    try {
      await saveStoreExpenseItem(null, { name: newItem.trim(), isActive: true });
      setNewItem("");
      loadRefs();
    } catch (e) {
      toast.error(e instanceof Error ? e.message : "Yazılmadı.");
    }
  };

  const registersOf = (d: StoreExpense) => [...new Set(d.lines.map((l) => l.registerName))].join(", ");

  return (
    <div className="space-y-4 p-4">
      <div className="flex flex-wrap items-start justify-between gap-3">
        <div>
          <h1 className="text-2xl font-bold">Digər xərclər</h1>
          <p className="text-sm text-muted-foreground">
            Kassadan ödənilən xərclər: maaş, kirayə, kommunal və s. Hər sətir seçilən kassanın açıq növbəsindən çıxır və kassanın gözlənilən nağdını azaldır.
          </p>
        </div>
        <div className="flex gap-2">
          {canManageItems && (
            <Button variant="outline" onClick={() => setItemsOpen(true)}>
              <Settings2 className="mr-1 h-4 w-4" />
              Xərc maddələri
            </Button>
          )}
          {canCreate && (
            <Button onClick={startCreate}>
              <Plus className="mr-1 h-4 w-4" />
              Yeni xərc sənədi
            </Button>
          )}
        </div>
      </div>

      <div className="flex flex-wrap items-end gap-3 rounded-lg border bg-card p-3">
        <div className="space-y-1">
          <Label htmlFor="ex-from">Başlanğıc</Label>
          <Input id="ex-from" type="date" value={from} max={to} disabled={code !== ""} onChange={(e) => setFrom(e.target.value)} />
        </div>
        <div className="space-y-1">
          <Label htmlFor="ex-to">Son</Label>
          <Input id="ex-to" type="date" value={to} min={from} disabled={code !== ""} onChange={(e) => setTo(e.target.value)} />
        </div>
        <TimeRangeInputs idPrefix="ex" fromTime={fromTime} toTime={toTime} onFromTime={setFromTime} onToTime={setToTime} />
        <div className="space-y-1">
          <Label htmlFor="ex-reg">Kassa</Label>
          <select id="ex-reg" className={cn(SELECT_CLASS, "w-40")} value={registerId} onChange={(e) => setRegisterId(e.target.value ? Number(e.target.value) : "")}>
            <option value="">Hamısı</option>
            {registers.map((r) => (
              <option key={r.id} value={r.id}>
                {r.name}
              </option>
            ))}
          </select>
        </div>
        <div className="space-y-1">
          <Label htmlFor="ex-item">Xərc maddəsi</Label>
          <select id="ex-item" className={cn(SELECT_CLASS, "w-44")} value={itemId} onChange={(e) => setItemId(e.target.value ? Number(e.target.value) : "")}>
            <option value="">Hamısı</option>
            {items.map((i) => (
              <option key={i.id} value={i.id}>
                {i.name}
              </option>
            ))}
          </select>
        </div>
        <div className="space-y-1">
          <Label htmlFor="ex-payee">Kimə</Label>
          <Input id="ex-payee" className="w-44" placeholder="Ad…" value={payee} onChange={(e) => setPayee(e.target.value)} />
        </div>
        <div className="space-y-1">
          <Label htmlFor="ex-code">Sənəd kodu</Label>
          <Input id="ex-code" className="w-36" placeholder="XR-0000012" value={codeInput} onChange={(e) => setCodeInput(e.target.value)} />
        </div>
        <label className="flex h-9 items-center gap-2 text-sm">
          <input type="checkbox" checked={showCancelled} onChange={(e) => setShowCancelled(e.target.checked)} />
          Ləğv edilənlər
        </label>
      </div>

      {/* totals: overall and per item (a chip filters by that item) */}
      <div className="flex flex-wrap items-center gap-2">
        <div className="rounded-lg border px-4 py-2 text-sm">
          <span className="text-muted-foreground">Cəmi xərc: </span>
          <b className="text-red-600">{formatCurrency(list?.total ?? 0)}</b>
        </div>
        {(list?.byItem ?? []).map((b) => {
          const it = items.find((i) => i.name === b.name);
          return (
            <button
              key={b.name}
              type="button"
              onClick={() => setItemId(itemId === it?.id ? "" : (it?.id ?? ""))}
              className={cn("rounded-full border px-3 py-1 text-xs hover:bg-muted", it && itemId === it.id && "border-primary bg-primary/10")}
            >
              {b.name}: <b>{formatCurrency(b.amount)}</b>
            </button>
          );
        })}
      </div>

      <div className="space-y-2">
        {loading ? (
          <p className="py-8 text-center text-sm text-muted-foreground">Yüklənir…</p>
        ) : (list?.items ?? []).length === 0 ? (
          <p className="py-8 text-center text-sm text-muted-foreground">Bu şərtlərlə xərc sənədi tapılmadı.</p>
        ) : (
          list!.items.map((d) => (
            <button
              key={d.id}
              type="button"
              onClick={() => setSelected(d)}
              className={cn("block w-full rounded-lg border bg-card px-4 py-3 text-left transition-colors hover:bg-muted/50", d.isCancelled && "opacity-60")}
            >
              <div className="flex items-center justify-between gap-3">
                <span className="flex items-center gap-2 font-semibold">
                  Xərc sənədi
                  {d.isCancelled && <Badge variant="destructive">Ləğv edilib</Badge>}
                </span>
                <span className="font-mono text-sm text-muted-foreground">{d.documentNo}</span>
              </div>
              <div className="mt-1 flex flex-wrap items-center justify-between gap-x-6 gap-y-1 text-sm">
                <span className="flex flex-wrap gap-x-6 gap-y-1">
                  <span>
                    <span className="text-muted-foreground">Kassa: </span>
                    {registersOf(d)}
                  </span>
                  <span>
                    <span className="text-muted-foreground">Müəllif: </span>
                    {d.authorName ?? "—"}
                  </span>
                  <span>
                    <span className="text-muted-foreground">Tarix: </span>
                    {dateTime(d.documentDate)}
                  </span>
                  <span className="text-muted-foreground">
                    {d.lines.map((l) => l.expenseItemName).filter((v, i, a) => a.indexOf(v) === i).join(", ")}
                  </span>
                </span>
                <b className={cn("text-red-600", d.isCancelled && "line-through")}>−{formatCurrency(d.total)}</b>
              </div>
            </button>
          ))
        )}
      </div>

      {/* the document */}
      <Dialog open={selected != null} onOpenChange={(o) => !o && setSelected(null)}>
        <DialogContent className="sm:max-w-3xl">
          {selected && (
            <>
              <DialogHeader>
                <DialogTitle className="flex items-center justify-between gap-3 pr-6">
                  <span>Xərc sənədi</span>
                  <span className="font-mono text-base text-muted-foreground">{selected.documentNo}</span>
                </DialogTitle>
                <DialogDescription>
                  {dateTime(selected.documentDate)}
                  {selected.authorName ? ` · Müəllif: ${selected.authorName}` : ""}
                </DialogDescription>
              </DialogHeader>
              <div className="overflow-x-auto rounded-md border">
                <table className="w-full text-sm">
                  <thead>
                    <tr className="bg-muted/40 text-left text-xs text-muted-foreground">
                      <th className="px-3 py-2 font-medium">N</th>
                      <th className="px-3 py-2 font-medium">Kassa</th>
                      <th className="px-3 py-2 font-medium">Kimə</th>
                      <th className="px-3 py-2 font-medium">Xərc maddəsi</th>
                      <th className="px-3 py-2 text-right font-medium">Məbləğ</th>
                      <th className="px-3 py-2 font-medium">Qeyd</th>
                    </tr>
                  </thead>
                  <tbody>
                    {selected.lines.map((l, i) => (
                      <tr key={l.id} className="border-t">
                        <td className="px-3 py-2">{i + 1}</td>
                        <td className="px-3 py-2">{l.registerName}</td>
                        <td className="px-3 py-2">{l.payee ?? "—"}</td>
                        <td className="px-3 py-2">{l.expenseItemName}</td>
                        <td className="px-3 py-2 text-right">{formatCurrency(l.amount)}</td>
                        <td className="px-3 py-2 text-muted-foreground">{l.note ?? ""}</td>
                      </tr>
                    ))}
                  </tbody>
                  <tfoot>
                    <tr className="border-t font-semibold">
                      <td colSpan={4} className="px-3 py-2">
                        Cəmi
                      </td>
                      <td className="px-3 py-2 text-right">{formatCurrency(selected.total)}</td>
                      <td />
                    </tr>
                  </tfoot>
                </table>
              </div>
              {selected.note && <p className="text-sm text-muted-foreground">Qeyd: {selected.note}</p>}
              {selected.isCancelled && (
                <p className="rounded-md bg-red-50 p-2 text-sm text-red-700 dark:bg-red-950/30 dark:text-red-300">
                  Ləğv edilib{selected.cancelledAtUtc ? `: ${dateTime(selected.cancelledAtUtc)}` : ""}
                  {selected.cancelReason ? ` — ${selected.cancelReason}` : ""}
                </p>
              )}
              <DialogFooter>
                <Button
                  variant="outline"
                  onClick={() => {
                    if (!printStoreExpense(selected)) toast.error("Çap pəncərəsi açılmadı — popup bloklamasını söndürün.");
                  }}
                >
                  <Printer className="mr-1 h-4 w-4" />
                  Çap forması
                </Button>
                {!selected.isCancelled && canCreate && (
                  <Button variant="destructive" onClick={() => setCancelFor(selected)}>
                    Ləğv et
                  </Button>
                )}
              </DialogFooter>
            </>
          )}
        </DialogContent>
      </Dialog>

      <ReversePaymentDialog open={cancelFor != null} title={cancelFor ? `Xərc sənədi ${cancelFor.documentNo}` : ""} onClose={() => setCancelFor(null)} onConfirm={cancel} />

      {/* a new document: one line per till / payee / item */}
      <Dialog open={creating} onOpenChange={(o) => !o && !saving && setCreating(false)}>
        <DialogContent className="sm:max-w-4xl">
          <DialogHeader>
            <DialogTitle>Digər xərclər (yeni)</DialogTitle>
            <DialogDescription>Hər sətir üçün kassanı, kimə ödənildiyini, xərc maddəsini və məbləği yazın. Kassada açıq növbə və kifayət qədər nağd olmalıdır.</DialogDescription>
          </DialogHeader>
          <div className="space-y-2 overflow-x-auto">
            <table className="w-full text-sm">
              <thead>
                <tr className="text-left text-xs text-muted-foreground">
                  <th className="px-1 py-1 font-medium">Kassa</th>
                  <th className="px-1 py-1 font-medium">Kimə</th>
                  <th className="px-1 py-1 font-medium">Xərc maddəsi</th>
                  <th className="px-1 py-1 font-medium">Məbləğ</th>
                  <th className="px-1 py-1 font-medium">Qeyd</th>
                  <th />
                </tr>
              </thead>
              <tbody>
                {lines.map((l, i) => (
                  <tr key={i}>
                    <td className="px-1 py-1">
                      <select className={SELECT_CLASS} value={l.registerId} onChange={(e) => setLine(i, { registerId: e.target.value ? Number(e.target.value) : "" })}>
                        <option value="">Seçin</option>
                        {openRegisters.map((r) => (
                          <option key={r.id} value={r.id} disabled={r.openShiftId == null}>
                            {r.name}
                            {r.openShiftId == null ? " (növbə bağlıdır)" : ""}
                          </option>
                        ))}
                      </select>
                    </td>
                    <td className="px-1 py-1">
                      <Input value={l.payee} onChange={(e) => setLine(i, { payee: e.target.value })} placeholder="Ad Soyad" />
                    </td>
                    <td className="px-1 py-1">
                      <select className={SELECT_CLASS} value={l.itemId} onChange={(e) => setLine(i, { itemId: e.target.value ? Number(e.target.value) : "" })}>
                        <option value="">Seçin</option>
                        {activeItems.map((it) => (
                          <option key={it.id} value={it.id}>
                            {it.name}
                          </option>
                        ))}
                      </select>
                    </td>
                    <td className="px-1 py-1">
                      <Input className="w-28" inputMode="decimal" value={l.amount} onChange={(e) => setLine(i, { amount: e.target.value })} />
                    </td>
                    <td className="px-1 py-1">
                      <Input value={l.note} onChange={(e) => setLine(i, { note: e.target.value })} />
                    </td>
                    <td className="px-1 py-1">
                      <Button variant="ghost" size="icon" aria-label="Sətri sil" disabled={lines.length === 1} onClick={() => setLines((prev) => prev.filter((_, idx) => idx !== i))}>
                        <Trash2 className="h-4 w-4" />
                      </Button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
            <div className="flex items-center justify-between">
              <Button variant="outline" size="sm" onClick={() => setLines((prev) => [...prev, emptyLine(prev[prev.length - 1]?.registerId ?? "")])}>
                <Plus className="mr-1 h-4 w-4" />
                Sətir əlavə et
              </Button>
              <span className="text-sm">
                Cəmi: <b>{formatCurrency(draftTotal)}</b>
              </span>
            </div>
            <div className="space-y-1">
              <Label htmlFor="ex-note">Sənədin qeydi</Label>
              <Input id="ex-note" value={docNote} onChange={(e) => setDocNote(e.target.value)} />
            </div>
          </div>
          <DialogFooter>
            <Button variant="outline" disabled={saving} onClick={() => setCreating(false)}>
              Bağla
            </Button>
            <Button variant="outline" disabled={saving} onClick={() => void submit(true)}>
              <Printer className="mr-1 h-4 w-4" />
              Qeyd et və çap et
            </Button>
            <Button disabled={saving} onClick={() => void submit(false)}>
              Qeyd et
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* expense items */}
      <Dialog open={itemsOpen} onOpenChange={setItemsOpen}>
        <DialogContent className="sm:max-w-md">
          <DialogHeader>
            <DialogTitle>Xərc maddələri</DialogTitle>
            <DialogDescription>Xərc sənədində seçilən maddələr. Köhnə sənədlər itməsin deyə maddə silinmir, söndürülür.</DialogDescription>
          </DialogHeader>
          <div className="max-h-72 space-y-1 overflow-y-auto">
            {items.length === 0 && <p className="py-4 text-center text-sm text-muted-foreground">Hələ maddə yoxdur.</p>}
            {items.map((i) => (
              <div key={i.id} className="flex items-center justify-between rounded-md border px-3 py-1.5 text-sm">
                <span className={cn(!i.isActive && "text-muted-foreground line-through")}>{i.name}</span>
                <Button
                  size="sm"
                  variant="ghost"
                  onClick={async () => {
                    try {
                      await saveStoreExpenseItem(i.id, { name: i.name, isActive: !i.isActive });
                      loadRefs();
                    } catch (e) {
                      toast.error(e instanceof Error ? e.message : "Yazılmadı.");
                    }
                  }}
                >
                  {i.isActive ? "Söndür" : "Aktiv et"}
                </Button>
              </div>
            ))}
          </div>
          <div className="flex gap-2">
            <Input placeholder="Yeni maddə: Maaş, Kirayə, Kommunal…" value={newItem} onChange={(e) => setNewItem(e.target.value)} onKeyDown={(e) => e.key === "Enter" && void addItem()} />
            <Button onClick={() => void addItem()}>Əlavə et</Button>
          </div>
        </DialogContent>
      </Dialog>
    </div>
  );
}
