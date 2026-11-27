"use client";

import { useCallback, useEffect, useState } from "react";
import { FileText, Pencil, Plus, Wallet } from "lucide-react";
import { toast } from "sonner";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from "@/components/ui/dialog";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import ReversePaymentDialog from "@/components/store/reverse-payment-dialog";
import { useStorePermissions as usePermissionSet } from "@/lib/store/use-store-permissions";
import { formatCurrency } from "@/lib/format-currency";
import { toDateInputValue } from "@/lib/report-period";
import { cn } from "@/lib/utils";
import {
  LEDGER_TYPE_LABELS,
  PAYMENT_LABELS,
  StorePaymentMethod,
  reverseStoreSupplierPayment,
  getStoreSupplierDebts,
  getStoreSupplierStatement,
  getStoreSuppliers,
  payStoreSupplier,
  saveStoreSupplier,
  type StoreSupplier,
  type StoreSupplierDebt,
  type StoreSupplierDebtReport,
  type StoreSupplierStatement,
} from "@/lib/store/store-service";

const SELECT = "h-9 w-full rounded-md border border-input bg-background px-3 text-sm";
const num = (s: string) => Number(s.replace(",", ".")) || 0;
const fmtDate = (v: string) => new Date(v).toLocaleDateString("az-AZ");

type Form = {
  name: string;
  legalName: string;
  phone: string;
  email: string;
  address: string;
  taxNumber: string;
  note: string;
  paymentTermDays: string;
  openingBalance: string;
  isActive: boolean;
};

const EMPTY: Form = {
  name: "",
  legalName: "",
  phone: "",
  email: "",
  address: "",
  taxNumber: "",
  note: "",
  paymentTermDays: "0",
  openingBalance: "",
  isActive: true,
};

export default function StoreSuppliersPage() {
  const permissions = usePermissionSet();
  const canManage = permissions.has("StoreSupplier.Manage");
  const canPay = permissions.has("StoreSupplier.Pay");

  const [tab, setTab] = useState<"list" | "debts">("list");
  const [suppliers, setSuppliers] = useState<StoreSupplier[]>([]);
  const [debts, setDebts] = useState<StoreSupplierDebtReport | null>(null);
  const [loading, setLoading] = useState(true);

  const [open, setOpen] = useState(false);
  const [saving, setSaving] = useState(false);
  const [editingId, setEditingId] = useState<number | null>(null);
  const [form, setForm] = useState<Form>(EMPTY);

  const [statement, setStatement] = useState<StoreSupplierStatement | null>(null);
  const [reversePayId, setReversePayId] = useState<number | null>(null);
  const [from, setFrom] = useState(toDateInputValue(new Date(new Date().getFullYear(), new Date().getMonth(), 1)));
  const [to, setTo] = useState(toDateInputValue(new Date()));

  const [payFor, setPayFor] = useState<StoreSupplier | null>(null);
  const [payAmount, setPayAmount] = useState("");
  const [payMethod, setPayMethod] = useState<number>(StorePaymentMethod.Cash);
  const [payRef, setPayRef] = useState("");
  const [payNote, setPayNote] = useState("");
  const [payOpenPurchases, setPayOpenPurchases] = useState<StoreSupplierDebt["openPurchases"]>([]);
  const [manual, setManual] = useState(false);
  const [alloc, setAlloc] = useState<Record<number, string>>({});
  const [paying, setPaying] = useState(false);

  const load = useCallback(async () => {
    setLoading(true);
    try {
      const [list, report] = await Promise.all([getStoreSuppliers(true), getStoreSupplierDebts()]);
      setSuppliers(list);
      setDebts(report);
    } catch (e) {
      toast.error(e instanceof Error ? e.message : "Yüklənmədi.");
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    void load();
  }, [load]);

  const set = <K extends keyof Form>(key: K, value: Form[K]) => setForm((f) => ({ ...f, [key]: value }));

  const openDialog = (s: StoreSupplier | null) => {
    setEditingId(s?.id ?? null);
    setForm(
      s
        ? {
            name: s.name,
            legalName: s.legalName ?? "",
            phone: s.phone ?? "",
            email: s.email ?? "",
            address: s.address ?? "",
            taxNumber: s.taxNumber ?? "",
            note: s.note ?? "",
            paymentTermDays: String(s.paymentTermDays),
            openingBalance: "",
            isActive: s.isActive,
          }
        : EMPTY,
    );
    setOpen(true);
  };

  const submit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!form.name.trim()) {
      toast.error("Təchizatçının adını yazın.");
      return;
    }
    setSaving(true);
    try {
      await saveStoreSupplier(editingId, {
        name: form.name.trim(),
        legalName: form.legalName.trim() || null,
        phone: form.phone.trim() || null,
        email: form.email.trim() || null,
        address: form.address.trim() || null,
        taxNumber: form.taxNumber.trim() || null,
        note: form.note.trim() || null,
        paymentTermDays: Math.max(0, Math.floor(num(form.paymentTermDays))),
        isActive: form.isActive,
        openingBalance: editingId == null && form.openingBalance.trim() ? num(form.openingBalance) : null,
      });
      toast.success("Təchizatçı yazıldı");
      setOpen(false);
      void load();
    } catch (err) {
      toast.error(err instanceof Error ? err.message : "Yazılmadı.");
    } finally {
      setSaving(false);
    }
  };

  const openStatement = async (s: StoreSupplier, f = from, t = to) => {
    try {
      setStatement(await getStoreSupplierStatement(s.id, { from: f, to: t }));
    } catch (e) {
      toast.error(e instanceof Error ? e.message : "Hesab çıxarışı yüklənmədi.");
    }
  };

  const openPay = (s: StoreSupplier) => {
    const debt = debts?.suppliers.find((d) => d.supplierId === s.id);
    setPayFor(s);
    setPayAmount(s.balance > 0 ? s.balance.toFixed(2) : "");
    setPayMethod(StorePaymentMethod.Cash);
    setPayRef("");
    setPayNote("");
    setManual(false);
    setAlloc({});
    setPayOpenPurchases(debt?.openPurchases ?? []);
  };

  const submitPay = async () => {
    if (!payFor) return;
    const amount = num(payAmount);
    if (amount <= 0) {
      toast.error("Məbləği yazın.");
      return;
    }
    const allocations = manual
      ? Object.entries(alloc)
          .map(([id, v]) => ({ purchaseId: Number(id), amount: num(v) }))
          .filter((a) => a.amount > 0)
      : null;
    setPaying(true);
    try {
      const res = await payStoreSupplier({
        supplierId: payFor.id,
        amount,
        method: payMethod,
        reference: payRef.trim() || null,
        note: payNote.trim() || null,
        allocations,
      });
      toast.success(
        `Ödəniş yazıldı: ${res.documentNo}${res.advance > 0 ? ` — ${formatCurrency(res.advance)} avans kimi qalır` : ""}`,
      );
      setPayFor(null);
      void load();
    } catch (err) {
      toast.error(err instanceof Error ? err.message : "Ödəniş yazılmadı.");
    } finally {
      setPaying(false);
    }
  };

  return (
    <div className="mx-auto max-w-6xl space-y-4 p-4">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <h1 className="text-2xl font-bold">Təchizatçılar</h1>
        {canManage && (
          <Button onClick={() => openDialog(null)}>
            <Plus className="mr-1 h-4 w-4" />
            Yeni təchizatçı
          </Button>
        )}
      </div>

      {debts && (
        <div className="grid grid-cols-3 gap-3">
          <div className="rounded-lg border bg-card p-4">
            <p className="text-xs text-muted-foreground">Cəmi borcumuz</p>
            <p className="mt-1 text-xl font-semibold">{formatCurrency(debts.totalOwed)}</p>
          </div>
          <div className="rounded-lg border bg-card p-4">
            <p className="text-xs text-muted-foreground">Gecikmiş borc</p>
            <p className={cn("mt-1 text-xl font-semibold", debts.totalOverdue > 0 && "text-red-600")}>{formatCurrency(debts.totalOverdue)}</p>
          </div>
          <div className="rounded-lg border bg-card p-4">
            <p className="text-xs text-muted-foreground">Avans (təchizatçı bizə borcludur)</p>
            <p className="mt-1 text-xl font-semibold text-emerald-600">{formatCurrency(debts.totalAdvance)}</p>
          </div>
        </div>
      )}

      <div className="flex rounded-md border">
        {(
          [
            ["list", "Siyahı"],
            ["debts", "Borclar və yaşlandırma (aging)"],
          ] as const
        ).map(([k, label]) => (
          <button
            key={k}
            onClick={() => setTab(k)}
            className={cn("flex-1 px-3 py-1.5 text-sm", tab === k ? "bg-primary text-primary-foreground" : "hover:bg-muted")}
          >
            {label}
          </button>
        ))}
      </div>

      {tab === "list" ? (
        <div className="overflow-x-auto rounded-lg border bg-card">
          <table className="w-full text-sm">
            <thead>
              <tr className="bg-muted/40 text-left text-xs text-muted-foreground">
                <th className="px-3 py-2 font-medium">Təchizatçı</th>
                <th className="px-3 py-2 font-medium">Əlaqə</th>
                <th className="px-3 py-2 text-right font-medium">Ödəniş müddəti</th>
                <th className="px-3 py-2 text-right font-medium">Balans</th>
                <th className="px-3 py-2" />
              </tr>
            </thead>
            <tbody>
              {loading ? (
                <tr>
                  <td colSpan={5} className="px-3 py-8 text-center text-muted-foreground">
                    Yüklənir…
                  </td>
                </tr>
              ) : suppliers.length === 0 ? (
                <tr>
                  <td colSpan={5} className="px-3 py-8 text-center text-muted-foreground">
                    Təchizatçı yoxdur.
                  </td>
                </tr>
              ) : (
                suppliers.map((s) => (
                  <tr key={s.id} className="border-t">
                    <td className="px-3 py-2 font-medium">
                      {s.name} {!s.isActive && <Badge variant="outline">Passiv</Badge>}
                      {s.legalName && <span className="block text-xs text-muted-foreground">{s.legalName}</span>}
                    </td>
                    <td className="px-3 py-2 text-muted-foreground">
                      {s.phone ?? "—"}
                      {s.email && <span className="block text-xs">{s.email}</span>}
                    </td>
                    <td className="px-3 py-2 text-right">{s.paymentTermDays} gün</td>
                    <td className={cn("px-3 py-2 text-right font-semibold", s.balance > 0 && "text-red-600", s.balance < 0 && "text-emerald-600")}>
                      {formatCurrency(Math.abs(s.balance))} {s.balance < 0 && <span className="text-xs font-normal">(avans)</span>}
                    </td>
                    <td className="px-3 py-2 text-right">
                      <Button size="sm" variant="outline" onClick={() => void openStatement(s)}>
                        <FileText className="mr-1 h-4 w-4" />
                        Hesab
                      </Button>
                      {canPay && (
                        <Button size="sm" variant="outline" className="ml-1" onClick={() => openPay(s)}>
                          <Wallet className="mr-1 h-4 w-4" />
                          Ödə
                        </Button>
                      )}
                      {canManage && (
                        <Button size="sm" variant="ghost" onClick={() => openDialog(s)}>
                          <Pencil className="h-4 w-4" />
                        </Button>
                      )}
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>
      ) : (
        <div className="overflow-x-auto rounded-lg border bg-card">
          <table className="w-full text-sm">
            <thead>
              <tr className="bg-muted/40 text-left text-xs text-muted-foreground">
                <th className="px-3 py-2 font-medium">Təchizatçı</th>
                <th className="px-3 py-2 text-right font-medium">Balans</th>
                <th className="px-3 py-2 text-right font-medium">Vaxtı çatmayıb</th>
                <th className="px-3 py-2 text-right font-medium">1–30 gün</th>
                <th className="px-3 py-2 text-right font-medium">31–60</th>
                <th className="px-3 py-2 text-right font-medium">61–90</th>
                <th className="px-3 py-2 text-right font-medium">90+</th>
                <th className="px-3 py-2 font-medium">Son ödəniş</th>
              </tr>
            </thead>
            <tbody>
              {!debts || debts.suppliers.length === 0 ? (
                <tr>
                  <td colSpan={8} className="px-3 py-8 text-center text-muted-foreground">
                    Borc yoxdur.
                  </td>
                </tr>
              ) : (
                debts.suppliers.map((d) => (
                  <tr key={d.supplierId} className="border-t">
                    <td className="px-3 py-2 font-medium">{d.supplierName}</td>
                    <td className="px-3 py-2 text-right font-semibold">{formatCurrency(d.balance)}</td>
                    <td className="px-3 py-2 text-right">{formatCurrency(d.notDue)}</td>
                    <td className={cn("px-3 py-2 text-right", d.overdue1To30 > 0 && "text-amber-600")}>{formatCurrency(d.overdue1To30)}</td>
                    <td className={cn("px-3 py-2 text-right", d.overdue31To60 > 0 && "text-orange-600")}>{formatCurrency(d.overdue31To60)}</td>
                    <td className={cn("px-3 py-2 text-right", d.overdue61To90 > 0 && "text-red-600")}>{formatCurrency(d.overdue61To90)}</td>
                    <td className={cn("px-3 py-2 text-right font-medium", d.overdue90Plus > 0 && "text-red-700")}>{formatCurrency(d.overdue90Plus)}</td>
                    <td className="px-3 py-2">{d.lastPaymentDate ? fmtDate(d.lastPaymentDate) : "—"}</td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>
      )}

      {/* Supplier card */}
      <Dialog open={open} onOpenChange={(o) => !o && !saving && setOpen(false)}>
        <DialogContent className="max-h-[90vh] overflow-y-auto sm:max-w-lg">
          <DialogHeader>
            <DialogTitle>{editingId ? "Təchizatçını dəyiş" : "Yeni təchizatçı"}</DialogTitle>
            <DialogDescription>Borc balansı alış və ödənişlərdən (ledger) hesablanır, əl ilə yazılmır.</DialogDescription>
          </DialogHeader>
          <form className="space-y-3" onSubmit={submit}>
            <div className="space-y-1">
              <Label htmlFor="sup-name">Ad</Label>
              <Input id="sup-name" autoFocus value={form.name} onChange={(e) => set("name", e.target.value)} />
            </div>
            <div className="space-y-1">
              <Label htmlFor="sup-legal">Hüquqi ad</Label>
              <Input id="sup-legal" value={form.legalName} onChange={(e) => set("legalName", e.target.value)} />
            </div>
            <div className="grid grid-cols-2 gap-3">
              <div className="space-y-1">
                <Label htmlFor="sup-phone">Telefon</Label>
                <Input id="sup-phone" value={form.phone} onChange={(e) => set("phone", e.target.value)} />
              </div>
              <div className="space-y-1">
                <Label htmlFor="sup-email">E-mail</Label>
                <Input id="sup-email" value={form.email} onChange={(e) => set("email", e.target.value)} />
              </div>
            </div>
            <div className="space-y-1">
              <Label htmlFor="sup-address">Ünvan</Label>
              <Input id="sup-address" value={form.address} onChange={(e) => set("address", e.target.value)} />
            </div>
            <div className="grid grid-cols-2 gap-3">
              <div className="space-y-1">
                <Label htmlFor="sup-tax">VÖEN</Label>
                <Input id="sup-tax" value={form.taxNumber} onChange={(e) => set("taxNumber", e.target.value)} />
              </div>
              <div className="space-y-1">
                <Label htmlFor="sup-term">Ödəniş müddəti (gün)</Label>
                <Input id="sup-term" inputMode="numeric" value={form.paymentTermDays} onChange={(e) => set("paymentTermDays", e.target.value)} />
              </div>
            </div>
            {editingId == null && (
              <div className="space-y-1">
                <Label htmlFor="sup-open">Başlanğıc qalıq (sistemdən əvvəl borcumuz, AZN)</Label>
                <Input id="sup-open" inputMode="decimal" value={form.openingBalance} onChange={(e) => set("openingBalance", e.target.value)} />
              </div>
            )}
            <div className="space-y-1">
              <Label htmlFor="sup-note">Qeyd</Label>
              <Input id="sup-note" value={form.note} onChange={(e) => set("note", e.target.value)} />
            </div>
            <label className="flex items-center gap-2 text-sm">
              <input type="checkbox" checked={form.isActive} onChange={(e) => set("isActive", e.target.checked)} />
              Aktiv
            </label>
            <DialogFooter>
              <Button type="button" variant="outline" disabled={saving} onClick={() => setOpen(false)}>
                Ləğv et
              </Button>
              <Button type="submit" disabled={saving}>
                {saving ? "Yazılır…" : "Yaz"}
              </Button>
            </DialogFooter>
          </form>
        </DialogContent>
      </Dialog>

      {/* Statement */}
      <ReversePaymentDialog
        open={reversePayId != null}
        title="Təchizatçıya ödənişi ləğv et"
        onClose={() => setReversePayId(null)}
        onConfirm={async (reason) => {
          if (reversePayId == null || !statement) return;
          await reverseStoreSupplierPayment(reversePayId, reason);
          await openStatement(statement.supplier);
          toast.success("Ödəniş ləğv edildi.");
        }}
      />

      <Dialog open={statement != null} onOpenChange={(o) => !o && setStatement(null)}>
        <DialogContent className="max-h-[90vh] overflow-y-auto sm:max-w-3xl">
          <DialogHeader>
            <DialogTitle>{statement?.supplier.name} — hesab çıxarışı</DialogTitle>
            <DialogDescription>Başlanğıc qalıq, dövr üzrə artım və azalma, yekun qalıq.</DialogDescription>
          </DialogHeader>
          {statement && (
            <div className="space-y-3">
              <div className="flex flex-wrap items-end gap-2">
                <div className="space-y-1">
                  <Label htmlFor="st-from">Başlanğıc</Label>
                  <Input id="st-from" type="date" value={from} onChange={(e) => setFrom(e.target.value)} />
                </div>
                <div className="space-y-1">
                  <Label htmlFor="st-to">Son</Label>
                  <Input id="st-to" type="date" value={to} onChange={(e) => setTo(e.target.value)} />
                </div>
                <Button variant="outline" onClick={() => void openStatement(statement.supplier, from, to)}>
                  Göstər
                </Button>
              </div>
              <div className="grid grid-cols-4 gap-2 text-sm">
                <div className="rounded-md border p-2">
                  <p className="text-xs text-muted-foreground">Başlanğıc qalıq</p>
                  <p className="font-semibold">{formatCurrency(statement.openingBalance)}</p>
                </div>
                <div className="rounded-md border p-2">
                  <p className="text-xs text-muted-foreground">Artım (alış)</p>
                  <p className="font-semibold">{formatCurrency(statement.totalIncrease)}</p>
                </div>
                <div className="rounded-md border p-2">
                  <p className="text-xs text-muted-foreground">Azalma (ödəniş, qaytarma)</p>
                  <p className="font-semibold">{formatCurrency(statement.totalDecrease)}</p>
                </div>
                <div className="rounded-md border p-2">
                  <p className="text-xs text-muted-foreground">Yekun qalıq</p>
                  <p className="font-semibold">{formatCurrency(statement.closingBalance)}</p>
                </div>
              </div>
              <table className="w-full text-sm">
                <thead>
                  <tr className="text-left text-xs text-muted-foreground">
                    <th className="py-1 font-medium">Tarix</th>
                    <th className="py-1 font-medium">Növ</th>
                    <th className="py-1 font-medium">Sənəd</th>
                    <th className="py-1 text-right font-medium">Məbləğ</th>
                    <th className="py-1 text-right font-medium">Qalıq</th>
                  </tr>
                </thead>
                <tbody>
                  {statement.entries.length === 0 ? (
                    <tr>
                      <td colSpan={5} className="py-6 text-center text-muted-foreground">
                        Bu dövrdə hərəkət yoxdur.
                      </td>
                    </tr>
                  ) : (
                    statement.entries.map((e) => (
                      <tr key={e.id} className="border-t">
                        <td className="py-1.5">{fmtDate(e.entryDate)}</td>
                        <td className="py-1.5">{LEDGER_TYPE_LABELS[e.type] ?? e.typeName}</td>
                        <td className="py-1.5">
                          {e.documentNo ?? "—"}
                          {e.note && <span className="block text-xs text-muted-foreground">{e.note}</span>}
                        </td>
                        <td className={cn("py-1.5 text-right", e.amount > 0 ? "text-red-600" : "text-emerald-600")}>
                          {e.amount > 0 ? "+" : ""}
                          {formatCurrency(e.amount)}
                        </td>
                        <td className="py-1.5 text-right">{formatCurrency(e.runningBalance)}</td>
                      </tr>
                    ))
                  )}
                </tbody>
              </table>
              {statement.payments.length > 0 && (
                <div>
                  <h3 className="mb-1 text-sm font-semibold">Ödənişlər</h3>
                  {statement.payments.map((p) => (
                    <div key={p.id} className="border-t py-1.5 text-sm">
                      <div className="flex justify-between">
                        <span>
                          {fmtDate(p.paymentDate)} — {p.documentNo} — {PAYMENT_LABELS[p.method] ?? p.method}
                        </span>
                        <span className="font-medium">
                          {p.isReversed ? (
                            <span className="text-red-600" title={p.reverseReason ?? ""}>
                              <s>{formatCurrency(p.amount)}</s> ləğv
                            </span>
                          ) : (
                            <>
                              {formatCurrency(p.amount)}
                              {canPay && (
                                <Button size="sm" variant="ghost" className="ml-1 text-red-600" onClick={() => setReversePayId(p.id)}>
                                  Ləğv et
                                </Button>
                              )}
                            </>
                          )}
                        </span>
                      </div>
                      <p className="text-xs text-muted-foreground">
                        {p.allocations.map((a) => `${a.purchaseDocumentNo}: ${formatCurrency(a.amount)}`).join(", ")}
                        {p.advance > 0 && ` · avans ${formatCurrency(p.advance)}`}
                      </p>
                    </div>
                  ))}
                </div>
              )}
            </div>
          )}
        </DialogContent>
      </Dialog>

      {/* Pay */}
      <Dialog open={payFor != null} onOpenChange={(o) => !o && !paying && setPayFor(null)}>
        <DialogContent className="max-h-[90vh] overflow-y-auto sm:max-w-md">
          <DialogHeader>
            <DialogTitle>{payFor?.name} — ödəniş</DialogTitle>
            <DialogDescription>
              Hazırkı borc: {payFor ? formatCurrency(Math.max(0, payFor.balance)) : ""}. Artıq ödənən hissə avans kimi qalır.
            </DialogDescription>
          </DialogHeader>
          <div className="space-y-3">
            <div className="grid grid-cols-2 gap-3">
              <div className="space-y-1">
                <Label htmlFor="pay-amount">Məbləğ (AZN)</Label>
                <Input id="pay-amount" autoFocus inputMode="decimal" value={payAmount} onChange={(e) => setPayAmount(e.target.value)} />
              </div>
              <div className="space-y-1">
                <Label htmlFor="pay-method">Üsul</Label>
                <select id="pay-method" className={SELECT} value={payMethod} onChange={(e) => setPayMethod(Number(e.target.value))}>
                  <option value={StorePaymentMethod.Cash}>Nağd</option>
                  <option value={StorePaymentMethod.Card}>Kart</option>
                  <option value={StorePaymentMethod.BankTransfer}>Bank köçürməsi</option>
                </select>
              </div>
            </div>
            <div className="space-y-1">
              <Label htmlFor="pay-ref">İstinad / sənəd nömrəsi</Label>
              <Input id="pay-ref" value={payRef} onChange={(e) => setPayRef(e.target.value)} />
            </div>
            <div className="space-y-1">
              <Label htmlFor="pay-note">Qeyd</Label>
              <Input id="pay-note" value={payNote} onChange={(e) => setPayNote(e.target.value)} />
            </div>
            <label className="flex items-center gap-2 text-sm">
              <input type="checkbox" checked={manual} onChange={(e) => setManual(e.target.checked)} />
              Hansı alışlara düşdüyünü özüm seçirəm (yoxsa ən köhnələrdən başlayır)
            </label>
            {manual && (
              <div className="space-y-1 rounded-md border p-2">
                {payOpenPurchases.length === 0 ? (
                  <p className="text-sm text-muted-foreground">Açıq alış yoxdur.</p>
                ) : (
                  payOpenPurchases.map((p) => (
                    <div key={p.purchaseId} className="flex items-center justify-between gap-2 text-sm">
                      <span>
                        {p.documentNo} <span className="text-xs text-muted-foreground">(qalan {formatCurrency(p.outstanding)})</span>
                      </span>
                      <Input
                        className="h-8 w-28 text-right"
                        inputMode="decimal"
                        value={alloc[p.purchaseId] ?? ""}
                        onChange={(e) => setAlloc((prev) => ({ ...prev, [p.purchaseId]: e.target.value }))}
                      />
                    </div>
                  ))
                )}
              </div>
            )}
          </div>
          <DialogFooter>
            <Button variant="outline" disabled={paying} onClick={() => setPayFor(null)}>
              Ləğv et
            </Button>
            <Button disabled={paying} onClick={() => void submitPay()}>
              {paying ? "Yazılır…" : "Ödəniş yaz"}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}
