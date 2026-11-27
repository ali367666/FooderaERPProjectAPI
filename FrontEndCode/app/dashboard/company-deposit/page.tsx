"use client";

import { useCallback, useEffect, useState } from "react";
import { Plus } from "lucide-react";
import { toast } from "sonner";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from "@/components/ui/dialog";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { toApiFormError } from "@/lib/api-error";
import { formatCurrency } from "@/lib/format-currency";
import {
  addCompanyDeposit,
  getCompanyDeposits,
  type CompanyDepositSummary,
} from "@/lib/services/company-deposit-service";

function formatDateTime(value: string): string {
  const d = new Date(value);
  return Number.isNaN(d.getTime()) ? value : d.toLocaleString("az-AZ");
}

export default function CompanyDepositPage() {
  const [summary, setSummary] = useState<CompanyDepositSummary | null>(null);
  const [loading, setLoading] = useState(true);
  const [open, setOpen] = useState(false);
  const [saving, setSaving] = useState(false);
  const [amount, setAmount] = useState("");
  const [note, setNote] = useState("");

  const load = useCallback(async () => {
    setLoading(true);
    try {
      setSummary(await getCompanyDeposits());
    } catch (e) {
      toast.error(e instanceof Error ? e.message : "Depozit məlumatı yüklənmədi.");
      setSummary(null);
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    void load();
  }, [load]);

  const hasOpening = summary?.openingAmount != null;

  const submit = async (e: React.FormEvent) => {
    e.preventDefault();
    const value = Number(amount.replace(",", "."));
    if (!Number.isFinite(value) || value <= 0) {
      toast.error("Məbləğ düzgün deyil.");
      return;
    }
    setSaving(true);
    try {
      const message = await addCompanyDeposit(value, note.trim() || null);
      toast.success(message ?? "Depozit qeyd olundu");
      setOpen(false);
      setAmount("");
      setNote("");
      void load();
    } catch (err) {
      toast.error(toApiFormError(err, "Depozit yazılmadı").message);
    } finally {
      setSaving(false);
    }
  };

  return (
    <div className="space-y-6">
      <div className="flex flex-wrap items-start justify-between gap-3">
        <div>
          <h1 className="text-3xl font-bold text-foreground">Şirkət depoziti</h1>
          <p className="mt-1 text-muted-foreground">
            Şirkətin işə başladığı məbləğ və sonradan əlavə olunan depozitlər.
          </p>
        </div>
        <Button
          onClick={() => {
            setAmount("");
            setNote("");
            setOpen(true);
          }}
        >
          <Plus className="mr-1 h-4 w-4" />
          {hasOpening ? "Depozit əlavə et" : "Başlanğıc depozit yaz"}
        </Button>
      </div>

      {loading ? (
        <p className="text-sm text-muted-foreground">Yüklənir…</p>
      ) : !summary ? (
        <p className="text-sm text-muted-foreground">Məlumat yüklənmədi.</p>
      ) : (
        <>
          <div className="grid gap-3 sm:grid-cols-2">
            <div className="rounded-lg border bg-card p-4">
              <p className="text-xs text-muted-foreground">Başlanğıc depozit</p>
              <p className="mt-1 text-xl font-semibold">
                {summary.openingAmount != null ? formatCurrency(summary.openingAmount) : "Yazılmayıb"}
              </p>
              {summary.openingAtUtc && (
                <p className="text-xs text-muted-foreground">{formatDateTime(summary.openingAtUtc)}</p>
              )}
            </div>
            <div className="rounded-lg border bg-card p-4">
              <p className="text-xs text-muted-foreground">Ümumi depozit</p>
              <p className="mt-1 text-xl font-semibold">{formatCurrency(summary.totalAmount)}</p>
            </div>
          </div>

          <div className="rounded-lg border bg-card p-4">
            <h2 className="mb-3 font-semibold">Tarixçə</h2>
            {summary.entries.length === 0 ? (
              <p className="text-sm text-muted-foreground">Hələ depozit yazılmayıb.</p>
            ) : (
              <table className="w-full text-sm">
                <thead>
                  <tr className="text-left text-xs text-muted-foreground">
                    <th className="py-1 font-medium">Tarix</th>
                    <th className="py-1 font-medium">Kim yazıb</th>
                    <th className="py-1 font-medium">Növ</th>
                    <th className="py-1 font-medium">Qeyd</th>
                    <th className="py-1 text-right font-medium">Məbləğ</th>
                  </tr>
                </thead>
                <tbody>
                  {summary.entries.map((d) => (
                    <tr key={d.id} className="border-t">
                      <td className="py-1.5">{formatDateTime(d.createdAtUtc)}</td>
                      <td className="py-1.5">{d.createdByUserName ?? "—"}</td>
                      <td className="py-1.5">
                        <Badge variant={d.isOpening ? "default" : "secondary"}>{d.isOpening ? "Başlanğıc" : "Əlavə"}</Badge>
                      </td>
                      <td className="py-1.5 text-muted-foreground">{d.note ?? "—"}</td>
                      <td className="py-1.5 text-right font-medium">{formatCurrency(d.amount)}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            )}
          </div>
        </>
      )}

      <Dialog open={open} onOpenChange={(o) => !o && !saving && setOpen(false)}>
        <DialogContent className="sm:max-w-sm">
          <DialogHeader>
            <DialogTitle>{hasOpening ? "Depozit əlavə et" : "Başlanğıc depozit"}</DialogTitle>
            <DialogDescription>
              {hasOpening
                ? "Əlavə depozit tarixçəyə yazılır."
                : "Şirkətin işə başladığı məbləğ. Bir dəfə yazılır."}
            </DialogDescription>
          </DialogHeader>
          <form className="space-y-4" onSubmit={submit}>
            <div className="space-y-2">
              <Label htmlFor="dep-amount">Məbləğ (AZN)</Label>
              <Input
                id="dep-amount"
                inputMode="decimal"
                autoFocus
                value={amount}
                onChange={(e) => setAmount(e.target.value)}
              />
            </div>
            <div className="space-y-2">
              <Label htmlFor="dep-note">Qeyd (istəyə bağlı)</Label>
              <Input id="dep-note" value={note} onChange={(e) => setNote(e.target.value)} />
            </div>
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
    </div>
  );
}
