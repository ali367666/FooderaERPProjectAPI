"use client";

import { useCallback, useEffect, useState } from "react";
import { toast } from "sonner";
import { ShiftReportView } from "@/components/store/shift-report-view";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from "@/components/ui/dialog";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { useStorePermissions as usePermissionSet } from "@/lib/store/use-store-permissions";
import { formatCurrency } from "@/lib/format-currency";
import { toDateInputValue } from "@/lib/report-period";
import { cn } from "@/lib/utils";
import {
  StoreShiftStatus,
  getStoreRegisters,
  getStoreShiftReport,
  getStoreShifts,
  reopenStoreShift,
  type StoreCashRegister,
  type StoreShift,
  type StoreShiftReport,
} from "@/lib/store/store-service";

export default function StoreShiftsPage() {
  const today = toDateInputValue(new Date());
  const weekAgo = toDateInputValue(new Date(Date.now() - 6 * 86_400_000));

  const [from, setFrom] = useState(weekAgo);
  const [to, setTo] = useState(today);
  const [registerId, setRegisterId] = useState<number | "">("");
  const [registers, setRegisters] = useState<StoreCashRegister[]>([]);
  const [shifts, setShifts] = useState<StoreShift[]>([]);
  const [loading, setLoading] = useState(true);
  const [report, setReport] = useState<StoreShiftReport | null>(null);
  const canReopen = usePermissionSet().has("StoreCash.Reopen");
  const [reopenFor, setReopenFor] = useState<StoreShift | null>(null);
  const [reopenReason, setReopenReason] = useState("");
  const [reopening, setReopening] = useState(false);

  const doReopen = async () => {
    if (!reopenFor) return;
    setReopening(true);
    try {
      await reopenStoreShift(reopenFor.id, reopenReason.trim());
      toast.success("Növbə yenidən açıldı.");
      setReopenFor(null);
      setReopenReason("");
      void load();
    } catch (e) {
      toast.error(e instanceof Error ? e.message : "Növbə yenidən açılmadı.");
    } finally {
      setReopening(false);
    }
  };

  useEffect(() => {
    getStoreRegisters().then(setRegisters).catch(() => setRegisters([]));
  }, []);

  const load = useCallback(async () => {
    setLoading(true);
    try {
      setShifts(await getStoreShifts({ registerId: registerId === "" ? null : registerId, from, to }));
    } catch (e) {
      toast.error(e instanceof Error ? e.message : "Növbələr yüklənmədi.");
    } finally {
      setLoading(false);
    }
  }, [registerId, from, to]);

  useEffect(() => {
    void load();
  }, [load]);

  const openReport = async (id: number) => {
    try {
      setReport(await getStoreShiftReport(id));
    } catch (e) {
      toast.error(e instanceof Error ? e.message : "Hesabat yüklənmədi.");
    }
  };

  return (
    <div className="mx-auto max-w-5xl space-y-4 p-4">
      <h1 className="text-2xl font-bold">Növbələr və X/Z hesabatlar</h1>

      <div className="flex flex-wrap items-end gap-3 rounded-lg border bg-card p-3">
        <div className="space-y-1">
          <Label htmlFor="sh-from">Başlanğıc</Label>
          <Input id="sh-from" type="date" value={from} max={to} onChange={(e) => setFrom(e.target.value)} />
        </div>
        <div className="space-y-1">
          <Label htmlFor="sh-to">Son</Label>
          <Input id="sh-to" type="date" value={to} min={from} onChange={(e) => setTo(e.target.value)} />
        </div>
        <div className="space-y-1">
          <Label htmlFor="sh-reg">Kassa</Label>
          <select
            id="sh-reg"
            className="h-9 rounded-md border border-input bg-background px-3 text-sm"
            value={registerId}
            onChange={(e) => setRegisterId(e.target.value ? Number(e.target.value) : "")}
          >
            <option value="">Hamısı</option>
            {registers.map((r) => (
              <option key={r.id} value={r.id}>
                {r.name}
              </option>
            ))}
          </select>
        </div>
      </div>

      <div className="overflow-x-auto rounded-lg border bg-card">
        <table className="w-full text-sm">
          <thead>
            <tr className="bg-muted/40 text-left text-xs text-muted-foreground">
              <th className="px-3 py-2 font-medium">Açılış</th>
              <th className="px-3 py-2 font-medium">Kassa</th>
              <th className="px-3 py-2 font-medium">Kassir</th>
              <th className="px-3 py-2 font-medium">Status</th>
              <th className="px-3 py-2 text-right font-medium">Gözlənilən</th>
              <th className="px-3 py-2 text-right font-medium">Faktiki</th>
              <th className="px-3 py-2 text-right font-medium">Fərq</th>
              <th className="px-3 py-2" />
            </tr>
          </thead>
          <tbody>
            {loading ? (
              <tr>
                <td colSpan={8} className="px-3 py-8 text-center text-muted-foreground">
                  Yüklənir…
                </td>
              </tr>
            ) : shifts.length === 0 ? (
              <tr>
                <td colSpan={8} className="px-3 py-8 text-center text-muted-foreground">
                  Bu dövrdə növbə yoxdur.
                </td>
              </tr>
            ) : (
              shifts.map((s) => (
                <tr key={s.id} className="border-t">
                  <td className="px-3 py-2">{new Date(s.openedAtUtc).toLocaleString("az-AZ")}</td>
                  <td className="px-3 py-2">{s.registerName}</td>
                  <td className="px-3 py-2">{s.cashierName ?? "—"}</td>
                  <td className="px-3 py-2">
                    {s.status === StoreShiftStatus.Open ? <Badge>Açıq</Badge> : <Badge variant="secondary">Bağlı</Badge>}
                  </td>
                  <td className="px-3 py-2 text-right">{s.expectedCash == null ? "—" : formatCurrency(s.expectedCash)}</td>
                  <td className="px-3 py-2 text-right">{s.actualCash == null ? "—" : formatCurrency(s.actualCash)}</td>
                  <td
                    className={cn(
                      "px-3 py-2 text-right font-medium",
                      (s.cashDifference ?? 0) < 0 && "text-red-600",
                      (s.cashDifference ?? 0) > 0 && "text-amber-600",
                    )}
                  >
                    {s.cashDifference == null ? "—" : formatCurrency(s.cashDifference)}
                  </td>
                  <td className="px-3 py-2 text-right">
                    <Button size="sm" variant="outline" onClick={() => void openReport(s.id)}>
                      {s.status === StoreShiftStatus.Open ? "X-Hesabat" : "Z-Hesabat"}
                    </Button>
                    {canReopen && s.status !== StoreShiftStatus.Open && (
                      <Button size="sm" variant="ghost" className="ml-1" onClick={() => setReopenFor(s)}>
                        Yenidən aç
                      </Button>
                    )}
                  </td>
                </tr>
              ))
            )}
          </tbody>
        </table>
      </div>

      <Dialog open={reopenFor != null} onOpenChange={(o) => !o && setReopenFor(null)}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Növbəni yenidən aç</DialogTitle>
            <DialogDescription>Z-hesabatın rəqəmləri silinir və növbə bağlananda yenidən hesablanır. Səbəb audit jurnalına yazılır.</DialogDescription>
          </DialogHeader>
          <div className="space-y-2">
            <Label htmlFor="reopen-reason">Səbəb</Label>
            <Input id="reopen-reason" value={reopenReason} onChange={(e) => setReopenReason(e.target.value)} />
          </div>
          <DialogFooter>
            <Button variant="outline" onClick={() => setReopenFor(null)}>
              Ləğv et
            </Button>
            <Button disabled={reopening || !reopenReason.trim()} onClick={() => void doReopen()}>
              Yenidən aç
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      <Dialog open={report != null} onOpenChange={(o) => !o && setReport(null)}>
        <DialogContent className="max-h-[90vh] overflow-y-auto sm:max-w-md">
          <DialogHeader>
            <DialogTitle>Növbə hesabatı</DialogTitle>
          </DialogHeader>
          {report && <ShiftReportView report={report} />}
        </DialogContent>
      </Dialog>
    </div>
  );
}
