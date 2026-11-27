"use client";

import { useCallback, useEffect, useState } from "react";
import { FileText, Pencil, Plus, Search, Wallet } from "lucide-react";
import { toast } from "sonner";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from "@/components/ui/dialog";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import ReversePaymentDialog from "@/components/store/reverse-payment-dialog";
import { useStorePermissions as usePermissionSet } from "@/lib/store/use-store-permissions";
import { formatCurrency } from "@/lib/format-currency";
import { cn } from "@/lib/utils";
import {
  BONUS_TYPE_LABELS,
  LEDGER_TYPE_LABELS,
  PAYMENT_LABELS,
  StorePaymentMethod,
  reverseStoreCustomerPayment,
  adjustStoreBonus,
  getStoreCustomerStatement,
  payStoreCustomerDebt,
  saveStoreCustomer,
  searchStoreCustomers,
  type StoreCustomer,
  type StoreCustomerStatement,
} from "@/lib/store/store-service";

const num = (s: string) => Number(s.replace(",", ".")) || 0;
const fmt = (v: string) => new Date(v).toLocaleString("az-AZ");

type Form = { name: string; phone: string; email: string; note: string; creditLimit: string; code: string; isActive: boolean; bonusPercent: string };
const EMPTY: Form = { name: "", phone: "", email: "", note: "", creditLimit: "0", code: "", isActive: true, bonusPercent: "" };

export default function StoreCustomersPage() {
  const permissions = usePermissionSet();
  const canManage = permissions.has("StoreCustomer.Manage");
  const canPay = permissions.has("StoreCustomer.Pay");
  const canAdjustBonus = permissions.has("StoreBonus.Adjust");
  const canAdjust = permissions.has("StoreBonus.Adjust");

  const [q, setQ] = useState("");
  const [customers, setCustomers] = useState<StoreCustomer[]>([]);
  const [loading, setLoading] = useState(true);

  const [open, setOpen] = useState(false);
  const [saving, setSaving] = useState(false);
  const [editingId, setEditingId] = useState<number | null>(null);
  const [form, setForm] = useState<Form>(EMPTY);

  const [statement, setStatement] = useState<StoreCustomerStatement | null>(null);
  const [reversePayId, setReversePayId] = useState<number | null>(null);
  const [tab, setTab] = useState<"debt" | "bonus" | "payments">("debt");

  const [payOpen, setPayOpen] = useState(false);
  const [payAmount, setPayAmount] = useState("");
  const [payMethod, setPayMethod] = useState<number>(StorePaymentMethod.Cash);
  const [payRef, setPayRef] = useState("");
  const [paying, setPaying] = useState(false);

  const [bonusOpen, setBonusOpen] = useState(false);
  const [bonusAmount, setBonusAmount] = useState("");
  const [bonusReason, setBonusReason] = useState("");

  const load = useCallback(async () => {
    setLoading(true);
    try {
      setCustomers(await searchStoreCustomers(q.trim(), true));
    } catch (e) {
      toast.error(e instanceof Error ? e.message : "Müştərilər yüklənmədi.");
    } finally {
      setLoading(false);
    }
  }, [q]);

  useEffect(() => {
    const t = setTimeout(() => void load(), 250);
    return () => clearTimeout(t);
  }, [load]);

  const set = <K extends keyof Form>(key: K, value: Form[K]) => setForm((f) => ({ ...f, [key]: value }));

  const openDialog = (c: StoreCustomer | null) => {
    setEditingId(c?.id ?? null);
    setForm(
      c
        ? { name: c.name, phone: c.phone ?? "", email: c.email ?? "", note: c.note ?? "", creditLimit: String(c.creditLimit), code: c.code, isActive: c.isActive, bonusPercent: c.bonusPercentOverride == null ? "" : String(c.bonusPercentOverride) }
        : EMPTY,
    );
    setOpen(true);
  };

  const submit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!form.name.trim()) {
      toast.error("Müştərinin adını yazın.");
      return;
    }
    setSaving(true);
    try {
      await saveStoreCustomer(editingId, {
        name: form.name.trim(),
        phone: form.phone.trim() || null,
        email: form.email.trim() || null,
        note: form.note.trim() || null,
        creditLimit: Math.max(0, num(form.creditLimit)),
        isActive: form.isActive,
        code: form.code.trim() || null,
        bonusPercentOverride: form.bonusPercent.trim() === "" ? null : Math.min(100, Math.max(0, num(form.bonusPercent))),
      });
      toast.success("Müştəri yazıldı");
      setOpen(false);
      void load();
    } catch (err) {
      toast.error(err instanceof Error ? err.message : "Yazılmadı.");
    } finally {
      setSaving(false);
    }
  };

  const openStatement = async (c: StoreCustomer) => {
    try {
      setStatement(await getStoreCustomerStatement(c.id));
      setTab("debt");
    } catch (e) {
      toast.error(e instanceof Error ? e.message : "Hesab yüklənmədi.");
    }
  };

  const refresh = async (id: number) => {
    setStatement(await getStoreCustomerStatement(id));
    void load();
  };

  const submitPay = async () => {
    if (!statement) return;
    const amount = num(payAmount);
    if (amount <= 0) {
      toast.error("Məbləği yazın.");
      return;
    }
    setPaying(true);
    try {
      setStatement(await payStoreCustomerDebt({ customerId: statement.customer.id, amount, method: payMethod, reference: payRef.trim() || null }));
      toast.success("Ödəniş qəbul edildi");
      setPayOpen(false);
      void load();
    } catch (e) {
      toast.error(e instanceof Error ? e.message : "Ödəniş yazılmadı.");
    } finally {
      setPaying(false);
    }
  };

  const submitBonus = async () => {
    if (!statement) return;
    const amount = num(bonusAmount);
    if (amount === 0 || !bonusReason.trim()) {
      toast.error("Məbləği və səbəbi yazın.");
      return;
    }
    try {
      await adjustStoreBonus({ customerId: statement.customer.id, amount, reason: bonusReason.trim() });
      toast.success("Bonus düzəlişi yazıldı");
      setBonusOpen(false);
      setBonusAmount("");
      setBonusReason("");
      await refresh(statement.customer.id);
    } catch (e) {
      toast.error(e instanceof Error ? e.message : "Yazılmadı.");
    }
  };

  return (
    <div className="mx-auto max-w-6xl space-y-4 p-4">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div>
          <h1 className="text-2xl font-bold">Müştərilər</h1>
          <p className="text-sm text-muted-foreground">Borc və bonus balansı ledger-dən hesablanır; kassada kod və ya telefonla tapılır.</p>
        </div>
        {canManage && (
          <Button onClick={() => openDialog(null)}>
            <Plus className="mr-1 h-4 w-4" />
            Yeni müştəri
          </Button>
        )}
      </div>

      <div className="relative max-w-sm">
        <Search className="absolute left-2.5 top-2.5 h-4 w-4 text-muted-foreground" />
        <Input className="pl-8" placeholder="Ad, kod və ya telefon…" value={q} onChange={(e) => setQ(e.target.value)} />
      </div>

      <div className="overflow-x-auto rounded-lg border bg-card">
        <table className="w-full text-sm">
          <thead>
            <tr className="bg-muted/40 text-left text-xs text-muted-foreground">
              <th className="px-3 py-2 font-medium">Müştəri</th>
              <th className="px-3 py-2 font-medium">Kod / telefon</th>
              <th className="px-3 py-2 text-right font-medium">Kredit limiti</th>
              <th className="px-3 py-2 text-right font-medium">Borc</th>
              <th className="px-3 py-2 text-right font-medium">Bonus</th>
              <th className="px-3 py-2 text-right font-medium">Bonus %</th>
              <th className="px-3 py-2" />
            </tr>
          </thead>
          <tbody>
            {loading ? (
              <tr>
                <td colSpan={7} className="px-3 py-8 text-center text-muted-foreground">
                  Yüklənir…
                </td>
              </tr>
            ) : customers.length === 0 ? (
              <tr>
                <td colSpan={7} className="px-3 py-8 text-center text-muted-foreground">
                  Müştəri tapılmadı.
                </td>
              </tr>
            ) : (
              customers.map((c) => (
                <tr key={c.id} className="border-t">
                  <td className="px-3 py-2 font-medium">
                    {c.name} {!c.isActive && <Badge variant="outline">Passiv</Badge>}
                  </td>
                  <td className="px-3 py-2 text-muted-foreground">
                    {c.code}
                    {c.phone && <span className="block text-xs">{c.phone}</span>}
                  </td>
                  <td className="px-3 py-2 text-right">{c.creditLimit > 0 ? formatCurrency(c.creditLimit) : "—"}</td>
                  <td className={cn("px-3 py-2 text-right font-medium", c.debt > 0 && "text-red-600")}>{formatCurrency(Math.max(0, c.debt))}</td>
                  <td className="px-3 py-2 text-right">{formatCurrency(c.bonusBalance)}</td>
                  <td className="px-3 py-2 text-right text-muted-foreground">{c.bonusPercentOverride == null ? "ümumi" : `${c.bonusPercentOverride}%`}</td>
                  <td className="px-3 py-2 text-right">
                    <Button size="sm" variant="outline" onClick={() => void openStatement(c)}>
                      <FileText className="mr-1 h-4 w-4" />
                      Hesab
                    </Button>
                    {canManage && (
                      <Button size="sm" variant="ghost" onClick={() => openDialog(c)}>
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

      {/* Card */}
      <Dialog open={open} onOpenChange={(o) => !o && !saving && setOpen(false)}>
        <DialogContent className="sm:max-w-md">
          <DialogHeader>
            <DialogTitle>{editingId ? "Müştərini dəyiş" : "Yeni müştəri"}</DialogTitle>
            <DialogDescription>Kredit limiti 0 olanda müştəriyə borca satmaq olmur.</DialogDescription>
          </DialogHeader>
          <form className="space-y-3" onSubmit={submit}>
            <div className="space-y-1">
              <Label htmlFor="cu-name">Ad</Label>
              <Input id="cu-name" autoFocus value={form.name} onChange={(e) => set("name", e.target.value)} />
            </div>
            <div className="grid grid-cols-2 gap-3">
              <div className="space-y-1">
                <Label htmlFor="cu-phone">Telefon</Label>
                <Input id="cu-phone" value={form.phone} onChange={(e) => set("phone", e.target.value)} />
              </div>
              <div className="space-y-1">
                <Label htmlFor="cu-email">E-mail</Label>
                <Input id="cu-email" value={form.email} onChange={(e) => set("email", e.target.value)} />
              </div>
              <div className="space-y-1">
                <Label htmlFor="cu-code">Kart kodu (boşdursa avtomatik)</Label>
                <Input id="cu-code" value={form.code} onChange={(e) => set("code", e.target.value)} />
              </div>
              <div className="space-y-1">
                <Label htmlFor="cu-limit">Kredit limiti (AZN)</Label>
                <Input id="cu-limit" inputMode="decimal" value={form.creditLimit} onChange={(e) => set("creditLimit", e.target.value)} />
              </div>
            </div>
            {canAdjustBonus && (
              <div className="space-y-1">
                <Label htmlFor="cu-bonus">Bu müştəriyə bonus faizi (%) — boşdursa ümumi faiz</Label>
                <Input id="cu-bonus" inputMode="decimal" placeholder="məs. 7" value={form.bonusPercent} onChange={(e) => set("bonusPercent", e.target.value)} />
              </div>
            )}
            <div className="space-y-1">
              <Label htmlFor="cu-note">Qeyd</Label>
              <Input id="cu-note" value={form.note} onChange={(e) => set("note", e.target.value)} />
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
      <Dialog open={statement != null} onOpenChange={(o) => !o && setStatement(null)}>
        <DialogContent className="max-h-[92vh] overflow-y-auto sm:max-w-3xl">
          <DialogHeader>
            <DialogTitle>
              {statement?.customer.name} — {statement?.customer.code}
            </DialogTitle>
            <DialogDescription>
              Borc: {statement ? formatCurrency(Math.max(0, statement.customer.debt)) : ""} · Bonus: {statement ? formatCurrency(statement.customer.bonusBalance) : ""} ·
              Kredit limiti: {statement ? formatCurrency(statement.customer.creditLimit) : ""}
            </DialogDescription>
          </DialogHeader>
          {statement && (
            <div className="space-y-3">
              <div className="flex flex-wrap gap-2">
                {canPay && statement.customer.debt > 0 && (
                  <Button
                    size="sm"
                    onClick={() => {
                      setPayAmount(statement.customer.debt.toFixed(2));
                      setPayMethod(StorePaymentMethod.Cash);
                      setPayRef("");
                      setPayOpen(true);
                    }}
                  >
                    <Wallet className="mr-1 h-4 w-4" />
                    Borc ödənişi qəbul et
                  </Button>
                )}
                {canAdjust && (
                  <Button size="sm" variant="outline" onClick={() => setBonusOpen(true)}>
                    Bonus düzəlişi
                  </Button>
                )}
              </div>

              <div className="flex rounded-md border">
                {(
                  [
                    ["debt", "Borc hərəkətləri"],
                    ["bonus", "Bonus hərəkətləri"],
                    ["payments", "Ödənişlər"],
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

              {tab === "debt" && (
                <table className="w-full text-sm">
                  <thead>
                    <tr className="text-left text-xs text-muted-foreground">
                      <th className="py-1 font-medium">Tarix</th>
                      <th className="py-1 font-medium">Növ</th>
                      <th className="py-1 font-medium">Sənəd</th>
                      <th className="py-1 text-right font-medium">Məbləğ</th>
                      <th className="py-1 text-right font-medium">Borc</th>
                    </tr>
                  </thead>
                  <tbody>
                    {statement.debtEntries.length === 0 ? (
                      <tr>
                        <td colSpan={5} className="py-6 text-center text-muted-foreground">
                          Hərəkət yoxdur.
                        </td>
                      </tr>
                    ) : (
                      statement.debtEntries.map((e) => (
                        <tr key={e.id} className="border-t">
                          <td className="py-1.5">{fmt(e.entryDate)}</td>
                          <td className="py-1.5">{LEDGER_TYPE_LABELS[e.type] ?? e.typeName}</td>
                          <td className="py-1.5">{e.documentNo ?? "—"}</td>
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
              )}

              {tab === "bonus" && (
                <table className="w-full text-sm">
                  <thead>
                    <tr className="text-left text-xs text-muted-foreground">
                      <th className="py-1 font-medium">Tarix</th>
                      <th className="py-1 font-medium">Növ</th>
                      <th className="py-1 font-medium">Sənəd / qeyd</th>
                      <th className="py-1 text-right font-medium">Bonus</th>
                      <th className="py-1 text-right font-medium">Balans</th>
                    </tr>
                  </thead>
                  <tbody>
                    {statement.bonusEntries.length === 0 ? (
                      <tr>
                        <td colSpan={5} className="py-6 text-center text-muted-foreground">
                          Bonus hərəkəti yoxdur.
                        </td>
                      </tr>
                    ) : (
                      statement.bonusEntries.map((e) => (
                        <tr key={e.id} className="border-t">
                          <td className="py-1.5">{fmt(e.createdAtUtc)}</td>
                          <td className="py-1.5">{BONUS_TYPE_LABELS[e.type] ?? e.typeName}</td>
                          <td className="py-1.5">
                            {e.documentNo ?? ""}
                            {e.note && <span className="block text-xs text-muted-foreground">{e.note}</span>}
                          </td>
                          <td className={cn("py-1.5 text-right", e.amount > 0 ? "text-emerald-600" : "text-red-600")}>
                            {e.amount > 0 ? "+" : ""}
                            {e.amount.toFixed(2)}
                          </td>
                          <td className="py-1.5 text-right">{e.runningBalance.toFixed(2)}</td>
                        </tr>
                      ))
                    )}
                  </tbody>
                </table>
              )}

              {tab === "payments" && (
                <div className="space-y-1 text-sm">
                  {statement.payments.length === 0 ? (
                    <p className="py-6 text-center text-muted-foreground">Ödəniş yoxdur.</p>
                  ) : (
                    statement.payments.map((p) => (
                      <div key={p.id} className="flex justify-between border-t py-1.5">
                        <span>
                          {fmt(p.createdAtUtc)} — {p.documentNo} — {PAYMENT_LABELS[p.method] ?? p.method}
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
                    ))
                  )}
                </div>
              )}
            </div>
          )}
        </DialogContent>
      </Dialog>

      <ReversePaymentDialog
        open={reversePayId != null}
        title="Müştəri ödənişini ləğv et"
        onClose={() => setReversePayId(null)}
        onConfirm={async (reason) => {
          if (reversePayId == null) return;
          setStatement(await reverseStoreCustomerPayment(reversePayId, reason));
          toast.success("Ödəniş ləğv edildi.");
        }}
      />

      {/* Debt payment */}
      <Dialog open={payOpen} onOpenChange={(o) => !o && !paying && setPayOpen(false)}>
        <DialogContent className="sm:max-w-sm">
          <DialogHeader>
            <DialogTitle>Borc ödənişi</DialogTitle>
            <DialogDescription>Növbə açıq olmalıdır; nağd məbləğ kassanın nağdına düşür.</DialogDescription>
          </DialogHeader>
          <div className="space-y-3">
            <div className="space-y-1">
              <Label htmlFor="cp-amount">Məbləğ (AZN)</Label>
              <Input id="cp-amount" autoFocus inputMode="decimal" value={payAmount} onChange={(e) => setPayAmount(e.target.value)} />
            </div>
            <div className="space-y-1">
              <Label htmlFor="cp-method">Üsul</Label>
              <select
                id="cp-method"
                className="h-9 w-full rounded-md border border-input bg-background px-3 text-sm"
                value={payMethod}
                onChange={(e) => setPayMethod(Number(e.target.value))}
              >
                <option value={StorePaymentMethod.Cash}>Nağd</option>
                <option value={StorePaymentMethod.Card}>Kart</option>
              </select>
            </div>
            <div className="space-y-1">
              <Label htmlFor="cp-ref">İstinad (istəyə bağlı)</Label>
              <Input id="cp-ref" value={payRef} onChange={(e) => setPayRef(e.target.value)} />
            </div>
          </div>
          <DialogFooter>
            <Button variant="outline" disabled={paying} onClick={() => setPayOpen(false)}>
              Ləğv et
            </Button>
            <Button disabled={paying} onClick={() => void submitPay()}>
              {paying ? "Yazılır…" : "Qəbul et"}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* Bonus adjustment */}
      <Dialog open={bonusOpen} onOpenChange={setBonusOpen}>
        <DialogContent className="sm:max-w-sm">
          <DialogHeader>
            <DialogTitle>Bonus düzəlişi</DialogTitle>
            <DialogDescription>Plus bonus əlavə edir, minus azaldır. Səbəb məcburidir və bonus ledger-inə yazılır.</DialogDescription>
          </DialogHeader>
          <div className="space-y-3">
            <div className="space-y-1">
              <Label htmlFor="bn-amount">Məbləğ (məs. 5 və ya -5)</Label>
              <Input id="bn-amount" autoFocus inputMode="decimal" value={bonusAmount} onChange={(e) => setBonusAmount(e.target.value)} />
            </div>
            <div className="space-y-1">
              <Label htmlFor="bn-reason">Səbəb</Label>
              <Input id="bn-reason" value={bonusReason} onChange={(e) => setBonusReason(e.target.value)} />
            </div>
          </div>
          <DialogFooter>
            <Button variant="outline" onClick={() => setBonusOpen(false)}>
              Ləğv et
            </Button>
            <Button onClick={() => void submitBonus()}>Yaz</Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}
