"use client";

import { useCallback, useEffect, useMemo, useState } from "react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Badge } from "@/components/ui/badge";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { toast } from "sonner";
import { getRestaurants, type Restaurant } from "@/lib/services/restaurant-service";
import { useSelectedRestaurant } from "@/contexts/selected-restaurant-context";
import { getZReport, type ZReport } from "@/lib/services/analytics-service";
import {
  adjustCounterpartyDebt,
  getCounterparties,
  type Counterparty,
} from "@/lib/services/counterparty-service";
import {
  CashMovementType,
  createCashMovement,
  getCashMovements,
  type CashMovement,
  type CashMovementTypeValue,
} from "@/lib/services/cash-movement-service";

const selectClass =
  "flex h-10 w-full rounded-md border border-input bg-background px-3 py-2 text-sm ring-offset-background";

function startOfDay(d: Date): Date {
  const x = new Date(d);
  x.setHours(0, 0, 0, 0);
  return x;
}

function endOfDay(d: Date): Date {
  const x = new Date(d);
  x.setHours(23, 59, 59, 999);
  return x;
}

export default function KassaPage() {
  const { selectedRestaurantId } = useSelectedRestaurant();
  const [restaurants, setRestaurants] = useState<Restaurant[]>([]);
  const [restaurantId, setRestaurantId] = useState<string>("");

  const [movements, setMovements] = useState<CashMovement[]>([]);
  const [report, setReport] = useState<ZReport | null>(null);
  const [debtors, setDebtors] = useState<Counterparty[]>([]);
  const [loading, setLoading] = useState(true);

  const [movementDialogOpen, setMovementDialogOpen] = useState(false);
  const [movementType, setMovementType] = useState<CashMovementTypeValue>(CashMovementType.Deposit);
  const [movementAmount, setMovementAmount] = useState("");
  const [movementReason, setMovementReason] = useState("");
  const [savingMovement, setSavingMovement] = useState(false);

  const [payDebtor, setPayDebtor] = useState<Counterparty | null>(null);
  const [payAmount, setPayAmount] = useState("");
  const [payingDebt, setPayingDebt] = useState(false);

  useEffect(() => {
    (async () => {
      try {
        const rs = await getRestaurants();
        setRestaurants(rs);
        // Follow the top "Filial filter" (Data Seçimi) when one is picked — otherwise default to
        // the first branch, same as before.
        if (selectedRestaurantId != null && rs.some((r) => r.id === selectedRestaurantId)) {
          setRestaurantId(String(selectedRestaurantId));
        } else if (rs.length > 0) {
          setRestaurantId(String(rs[0].id));
        }
      } catch {
        setRestaurants([]);
      }
    })();
  }, [selectedRestaurantId]);

  const loadAll = useCallback(async (rid: number) => {
    setLoading(true);
    try {
      const from = startOfDay(new Date());
      const to = endOfDay(new Date());
      const [movementsData, reportData, counterparties] = await Promise.all([
        getCashMovements(rid, from, to),
        getZReport(from, to),
        getCounterparties(),
      ]);
      setMovements(movementsData);
      setReport(reportData);
      setDebtors(counterparties.filter((c) => c.currentDebtAmount > 0).sort((a, b) => b.currentDebtAmount - a.currentDebtAmount));
    } catch (e) {
      toast.error(e instanceof Error ? e.message : "Kassa məlumatları yüklənmədi.");
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    const rid = Number(restaurantId);
    if (!Number.isFinite(rid) || rid <= 0) {
      setLoading(false);
      return;
    }
    void loadAll(rid);
  }, [restaurantId, loadAll]);

  const cashInTotal = useMemo(
    () => movements.filter((m) => m.type === CashMovementType.Deposit).reduce((s, m) => s + m.amount, 0),
    [movements],
  );
  const cashOutTotal = useMemo(
    () => movements.filter((m) => m.type === CashMovementType.Withdrawal).reduce((s, m) => s + m.amount, 0),
    [movements],
  );

  const openMovementDialog = (type: CashMovementTypeValue) => {
    setMovementType(type);
    setMovementAmount("");
    setMovementReason("");
    setMovementDialogOpen(true);
  };

  const handleSaveMovement = async () => {
    const rid = Number(restaurantId);
    const amount = Number(movementAmount);
    if (!Number.isFinite(amount) || amount <= 0) {
      toast.error("Məbləğ düzgün deyil.");
      return;
    }
    setSavingMovement(true);
    try {
      await createCashMovement({ restaurantId: rid, type: movementType, amount, reason: movementReason.trim() || null });
      toast.success(movementType === CashMovementType.Deposit ? "Mədaxil qeydə alındı." : "Məxaric qeydə alındı.");
      setMovementDialogOpen(false);
      await loadAll(rid);
    } catch (e) {
      toast.error(e instanceof Error ? e.message : "Yadda saxlanılmadı.");
    } finally {
      setSavingMovement(false);
    }
  };

  const handlePayDebt = async () => {
    if (!payDebtor) return;
    const rid = Number(restaurantId);
    const amount = Number(payAmount);
    if (!Number.isFinite(amount) || amount <= 0 || amount > payDebtor.currentDebtAmount) {
      toast.error("Məbləğ düzgün deyil.");
      return;
    }
    setPayingDebt(true);
    try {
      await adjustCounterpartyDebt(payDebtor.id, payDebtor.currentDebtAmount - amount);
      await createCashMovement({
        restaurantId: rid,
        type: CashMovementType.Deposit,
        amount,
        reason: `${payDebtor.name} borcunu ödədi`,
      });
      toast.success("Borc ödənişi qeydə alındı.");
      setPayDebtor(null);
      await loadAll(rid);
    } catch (e) {
      toast.error(e instanceof Error ? e.message : "Ödəniş qeydə alınmadı.");
    } finally {
      setPayingDebt(false);
    }
  };

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-3xl font-bold text-foreground">Kassa</h1>
        <p className="text-muted-foreground mt-1">Bugünkü kassa mədaxil/məxaric əməliyyatları və borclu müştərilər.</p>
      </div>

      <div className="max-w-xs">
        <Label htmlFor="kassa-restaurant">Filial</Label>
        <select
          id="kassa-restaurant"
          className={selectClass + " mt-1"}
          value={restaurantId}
          onChange={(e) => setRestaurantId(e.target.value)}
        >
          <option value="">Filial seçin</option>
          {restaurants.map((r) => (
            <option key={r.id} value={String(r.id)}>
              {r.name}
            </option>
          ))}
        </select>
      </div>

      {loading ? (
        <div className="text-sm text-muted-foreground">Yüklənir…</div>
      ) : (
        <>
          <div className="flex flex-wrap gap-2">
            <Button onClick={() => openMovementDialog(CashMovementType.Deposit)}>+ Mədaxil</Button>
            <Button variant="outline" onClick={() => openMovementDialog(CashMovementType.Withdrawal)}>
              − Məxaric
            </Button>
          </div>

          <div className="grid grid-cols-2 gap-4 lg:grid-cols-5">
            <SummaryCard label="Satışdan nağd" value={`${(report?.cashTotal ?? 0).toFixed(2)} ₼`} />
            <SummaryCard label="Satışdan pos" value={`${(report?.cardTotal ?? 0).toFixed(2)} ₼`} />
            <SummaryCard label="Kassaya mədaxil" value={`${cashInTotal.toFixed(2)} ₼`} />
            <SummaryCard label="Kassadan məxaric" value={`${cashOutTotal.toFixed(2)} ₼`} />
            <SummaryCard label="Bugünkü çek sayı" value={String(report?.orderCount ?? 0)} />
          </div>

          <div className="grid grid-cols-1 gap-4 lg:grid-cols-2">
            <div className="rounded-lg border bg-card">
              <div className="border-b px-4 py-3">
                <h2 className="text-sm font-semibold text-foreground">Bugünkü mədaxil/məxaric</h2>
              </div>
              <div className="max-h-80 overflow-y-auto">
                <table className="w-full text-sm">
                  <thead>
                    <tr className="border-b bg-muted/50">
                      <th className="px-3 py-2 text-left font-medium text-muted-foreground">Növ</th>
                      <th className="px-3 py-2 text-right font-medium text-muted-foreground">Məbləğ</th>
                      <th className="px-3 py-2 text-left font-medium text-muted-foreground">Səbəb</th>
                      <th className="px-3 py-2 text-left font-medium text-muted-foreground">Kim / Saat</th>
                    </tr>
                  </thead>
                  <tbody>
                    {movements.length === 0 ? (
                      <tr>
                        <td colSpan={4} className="px-3 py-6 text-center text-muted-foreground">
                          Bu gün mədaxil/məxaric yoxdur.
                        </td>
                      </tr>
                    ) : (
                      movements.map((m) => (
                        <tr key={m.id} className="border-b last:border-0">
                          <td className="px-3 py-2">
                            <Badge
                              className={
                                m.type === CashMovementType.Deposit
                                  ? "bg-emerald-100 text-emerald-800 hover:bg-emerald-100"
                                  : "bg-amber-100 text-amber-800 hover:bg-amber-100"
                              }
                            >
                              {m.type === CashMovementType.Deposit ? "Mədaxil" : "Məxaric"}
                            </Badge>
                          </td>
                          <td className="px-3 py-2 text-right">{m.amount.toFixed(2)} ₼</td>
                          <td className="px-3 py-2">{m.reason ?? "—"}</td>
                          <td className="px-3 py-2 text-xs text-muted-foreground">
                            {m.createdByUserName ?? "—"}, {new Date(m.createdAtUtc).toLocaleTimeString("az-AZ", { hour: "2-digit", minute: "2-digit" })}
                          </td>
                        </tr>
                      ))
                    )}
                  </tbody>
                </table>
              </div>
            </div>

            <div className="rounded-lg border bg-card">
              <div className="border-b px-4 py-3">
                <h2 className="text-sm font-semibold text-foreground">Borclu müştərilər</h2>
              </div>
              <div className="max-h-80 overflow-y-auto">
                <table className="w-full text-sm">
                  <thead>
                    <tr className="border-b bg-muted/50">
                      <th className="px-3 py-2 text-left font-medium text-muted-foreground">Müştəri</th>
                      <th className="px-3 py-2 text-right font-medium text-muted-foreground">Borc</th>
                      <th className="px-3 py-2 text-left font-medium text-muted-foreground"></th>
                    </tr>
                  </thead>
                  <tbody>
                    {debtors.length === 0 ? (
                      <tr>
                        <td colSpan={3} className="px-3 py-6 text-center text-muted-foreground">
                          Borclu müştəri yoxdur.
                        </td>
                      </tr>
                    ) : (
                      debtors.map((d) => (
                        <tr key={d.id} className="border-b last:border-0">
                          <td className="px-3 py-2">
                            {d.name}
                            {d.phoneNumber && <span className="block text-xs text-muted-foreground">{d.phoneNumber}</span>}
                          </td>
                          <td className="px-3 py-2 text-right text-destructive font-medium">
                            {d.currentDebtAmount.toFixed(2)} ₼
                          </td>
                          <td className="px-3 py-2 text-right">
                            <Button
                              size="sm"
                              variant="outline"
                              onClick={() => {
                                setPayDebtor(d);
                                setPayAmount(d.currentDebtAmount.toFixed(2));
                              }}
                            >
                              Ödə
                            </Button>
                          </td>
                        </tr>
                      ))
                    )}
                  </tbody>
                </table>
              </div>
            </div>
          </div>
        </>
      )}

      <Dialog open={movementDialogOpen} onOpenChange={setMovementDialogOpen}>
        <DialogContent className="sm:max-w-sm">
          <DialogHeader>
            <DialogTitle>{movementType === CashMovementType.Deposit ? "Kassaya mədaxil" : "Kassadan məxaric"}</DialogTitle>
            <DialogDescription>Məbləği və səbəbini qeyd edin.</DialogDescription>
          </DialogHeader>
          <div className="space-y-3">
            <div>
              <Label htmlFor="mv-amount">Məbləğ</Label>
              <Input
                id="mv-amount"
                className="mt-1"
                type="number"
                min={0}
                step="0.01"
                value={movementAmount}
                onChange={(e) => setMovementAmount(e.target.value)}
                placeholder="məs. 300.00"
              />
            </div>
            <div>
              <Label htmlFor="mv-reason">Səbəb</Label>
              <Input
                id="mv-reason"
                className="mt-1"
                value={movementReason}
                onChange={(e) => setMovementReason(e.target.value)}
                placeholder="məs. Depozit qoyuldu"
              />
            </div>
          </div>
          <div className="mt-4 flex justify-end gap-2">
            <Button variant="outline" onClick={() => setMovementDialogOpen(false)} disabled={savingMovement}>
              Ləğv et
            </Button>
            <Button onClick={() => void handleSaveMovement()} disabled={savingMovement}>
              {savingMovement ? "Saxlanılır…" : "Saxla"}
            </Button>
          </div>
        </DialogContent>
      </Dialog>

      <Dialog open={payDebtor != null} onOpenChange={(o) => !o && setPayDebtor(null)}>
        <DialogContent className="sm:max-w-sm">
          <DialogHeader>
            <DialogTitle>{payDebtor?.name} — borc ödənişi</DialogTitle>
            <DialogDescription>Cari borc: {payDebtor?.currentDebtAmount.toFixed(2)} ₼</DialogDescription>
          </DialogHeader>
          <div>
            <Label htmlFor="pay-amount">Ödənilən məbləğ</Label>
            <Input
              id="pay-amount"
              className="mt-1"
              type="number"
              min={0}
              step="0.01"
              value={payAmount}
              onChange={(e) => setPayAmount(e.target.value)}
            />
          </div>
          <div className="mt-4 flex justify-end gap-2">
            <Button variant="outline" onClick={() => setPayDebtor(null)} disabled={payingDebt}>
              Ləğv et
            </Button>
            <Button onClick={() => void handlePayDebt()} disabled={payingDebt}>
              {payingDebt ? "Saxlanılır…" : "Ödənişi qeydə al"}
            </Button>
          </div>
        </DialogContent>
      </Dialog>
    </div>
  );
}

function SummaryCard({ label, value }: { label: string; value: string }) {
  return (
    <div className="rounded-lg border bg-card p-4">
      <p className="text-xs text-muted-foreground">{label}</p>
      <p className="mt-1 text-xl font-semibold text-foreground">{value}</p>
    </div>
  );
}
