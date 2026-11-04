"use client";

import { useCallback, useEffect, useMemo, useState } from "react";
import { Search, Trash2 } from "lucide-react";
import { toast } from "sonner";
import { useStoreApproval } from "@/components/store/approval";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Textarea } from "@/components/ui/textarea";
import { formatCurrency } from "@/lib/format-currency";
import { toDateInputValue } from "@/lib/report-period";
import { cn } from "@/lib/utils";
import {
  RETURN_CONDITION_LABELS,
  StorePaymentMethod,
  StoreReturnCondition,
  UNIT_LABELS,
  createStoreExchange,
  createStoreReturn,
  findStoreSaleForReturn,
  getMyStoreShift,
  posSearchProducts,
  searchStoreReturns,
  type StoreProduct,
  type StoreReturn,
  type StoreReturnConditionValue,
  type StoreSaleForReturn,
  type StoreShift,
} from "@/lib/store/store-service";

const r2 = (n: number) => Math.round((n + Number.EPSILON) * 100) / 100;
const num = (s: string) => Number(s.replace(",", ".")) || 0;

type PickState = { quantity: string; condition: StoreReturnConditionValue; serials: string[] };
type NewLine = { product: StoreProduct; quantity: number; serialText: string };

export default function StoreReturnsPage() {
  const approval = useStoreApproval();
  const [shift, setShift] = useState<StoreShift | null>(null);
  const [documentNo, setDocumentNo] = useState("");
  const [found, setFound] = useState<StoreSaleForReturn | null>(null);
  const [picks, setPicks] = useState<Record<number, PickState>>({});
  const [reason, setReason] = useState("");
  const [refundMethod, setRefundMethod] = useState<number>(StorePaymentMethod.Cash);
  const [saving, setSaving] = useState(false);

  // Exchange
  const [exchange, setExchange] = useState(false);
  const [newLines, setNewLines] = useState<NewLine[]>([]);
  const [term, setTerm] = useState("");
  const [options, setOptions] = useState<StoreProduct[]>([]);
  const [cashInput, setCashInput] = useState("");
  const [cardInput, setCardInput] = useState("");

  const [recent, setRecent] = useState<StoreReturn[]>([]);

  const loadRecent = useCallback(() => {
    const today = toDateInputValue(new Date());
    searchStoreReturns({ from: today, to: today }).then(setRecent).catch(() => setRecent([]));
  }, []);

  useEffect(() => {
    getMyStoreShift().then(setShift).catch(() => setShift(null));
    loadRecent();
  }, [loadRecent]);

  useEffect(() => {
    if (!exchange || term.trim().length < 1) {
      setOptions([]);
      return;
    }
    const t = setTimeout(() => {
      posSearchProducts({ q: term.trim() })
        .then((r) => setOptions(r.items))
        .catch(() => setOptions([]));
    }, 200);
    return () => clearTimeout(t);
  }, [exchange, term]);

  const find = async () => {
    if (!documentNo.trim()) return;
    try {
      const res = await findStoreSaleForReturn(documentNo.trim());
      setFound(res);
      setPicks({});
      setReason("");
      setExchange(false);
      setNewLines([]);
    } catch (e) {
      setFound(null);
      toast.error(e instanceof Error ? e.message : "Çek tapılmadı.");
    }
  };

  // A scanner that does not press Enter: a full receipt number (S-0000123) is looked up as soon as it is complete.
  useEffect(() => {
    if (/^S-\d{7,}$/i.test(documentNo.trim())) void find();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [documentNo]);

  const setPick = (lineId: number, patch: Partial<PickState>) =>
    setPicks((prev) => {
      const base: PickState = prev[lineId] ?? { quantity: "", condition: StoreReturnCondition.Sellable, serials: [] };
      return { ...prev, [lineId]: { ...base, ...patch } };
    });

  const chosen = useMemo(() => {
    if (!found) return [];
    return found.lines
      .map((l) => {
        const p = picks[l.saleLineId];
        const qty = p ? (l.tracksSerial ? p.serials.length : num(p.quantity)) : 0;
        return { line: l, pick: p, qty };
      })
      .filter((x) => x.qty > 0);
  }, [found, picks]);

  const returnValue = r2(chosen.reduce((s, x) => s + x.qty * x.line.unitAmount, 0));
  const newTotal = r2(newLines.reduce((s, l) => s + l.quantity * l.product.salePrice, 0));
  const difference = r2(newTotal - returnValue); // positive: the customer pays this much
  const card = num(cardInput);
  const cash = num(cashInput);
  const payable = Math.max(0, difference);
  const payOk = !exchange || difference <= 0 || (card <= payable && cash + card >= payable);

  const buildReturn = (operationId: string) => ({
    operationId,
    storeSaleId: found!.sale.id,
    reason: reason.trim(),
    refundMethod: refundMethod as typeof StorePaymentMethod.Cash,
    lines: chosen.map((x) => ({
      saleLineId: x.line.saleLineId,
      quantity: x.qty,
      condition: x.pick?.condition ?? StoreReturnCondition.Sellable,
      serialNumbers: x.line.tracksSerial ? x.pick?.serials ?? [] : null,
    })),
  });

  const submit = async () => {
    if (!found || !shift) return;
    if (chosen.length === 0) {
      toast.error("Qaytarılacaq məhsulu və miqdarı yazın.");
      return;
    }
    if (!reason.trim()) {
      toast.error("Qaytarma səbəbini yazın.");
      return;
    }
    if (exchange && newLines.length === 0) {
      toast.error("Dəyişdirmə üçün yeni məhsul seçin.");
      return;
    }
    if (!payOk) {
      toast.error("Fərq düzgün ödənilməyib.");
      return;
    }

    setSaving(true);
    try {
      if (exchange) {
        const payments = [
          ...(cash > 0 ? [{ method: StorePaymentMethod.Cash, amount: cash }] : []),
          ...(card > 0 ? [{ method: StorePaymentMethod.Card, amount: card }] : []),
        ];
        const res = await approval.run((appr) =>
          createStoreExchange({
            return: { ...buildReturn(crypto.randomUUID()), approval: appr ?? null },
            newSale: {
              operationId: crypto.randomUUID(),
              storeCashRegisterId: shift.storeCashRegisterId,
              lines: newLines.map((l) => ({
                productId: l.product.id,
                quantity: l.product.tracksSerial ? l.serialText.split(/[\n,;]+/).map((x) => x.trim()).filter(Boolean).length : l.quantity,
                discountAmount: 0,
                serialNumbers: l.product.tracksSerial ? l.serialText.split(/[\n,;]+/).map((x) => x.trim()).filter(Boolean) : null,
              })),
              payments,
            },
          }),
        );
        toast.success(`Dəyişdirmə yazıldı: ${res.return.documentNo} → ${res.sale.documentNo}`);
      } else {
        const operationId = crypto.randomUUID();
        const res = await approval.run((appr) => createStoreReturn({ ...buildReturn(operationId), approval: appr ?? null }));
        toast.success(`Qaytarma yazıldı: ${res.documentNo} — ${formatCurrency(res.refundAmount)} geri verilir`);
      }
      setFound(null);
      setDocumentNo("");
      setPicks({});
      setNewLines([]);
      setCashInput("");
      setCardInput("");
      loadRecent();
    } catch (e) {
      toast.error(e instanceof Error ? e.message : "Yazılmadı.");
    } finally {
      setSaving(false);
    }
  };

  return (
    <div className="mx-auto max-w-5xl space-y-4 p-4">
      {approval.dialog}
      <h1 className="text-2xl font-bold">Geri qaytarma və dəyişdirmə</h1>
      {!shift && (
        <p className="rounded-md border border-amber-300 bg-amber-50 p-3 text-sm text-amber-800">
          Qaytarma və ya dəyişdirmə üçün əvvəlcə “Satış” bölməsində növbəni açın.
        </p>
      )}

      <div className="flex gap-2 rounded-lg border bg-card p-3">
        <Input
          placeholder="Çekin barkodunu oxudun və ya nömrəsini yazın (məs. S-0000123)"
          autoFocus
          value={documentNo}
          onChange={(e) => setDocumentNo(e.target.value)}
          onKeyDown={(e) => e.key === "Enter" && void find()}
        />
        <Button onClick={() => void find()}>
          <Search className="mr-1 h-4 w-4" />
          Tap
        </Button>
      </div>

      {found && (
        <div className="space-y-4 rounded-lg border bg-card p-4">
          <p className="text-sm">
            <span className="font-semibold">{found.sale.documentNo}</span> — {new Date(found.sale.createdAtUtc).toLocaleString("az-AZ")} —{" "}
            {formatCurrency(found.sale.total)}
          </p>

          <div className="overflow-x-auto rounded-md border">
            <table className="w-full text-sm">
              <thead>
                <tr className="bg-muted/40 text-left text-xs text-muted-foreground">
                  <th className="px-3 py-2 font-medium">Məhsul</th>
                  <th className="px-3 py-2 text-right font-medium">Satılıb</th>
                  <th className="px-3 py-2 text-right font-medium">Qaytarıla bilər</th>
                  <th className="px-3 py-2 text-right font-medium">Qaytarılır</th>
                  <th className="px-3 py-2 font-medium">Vəziyyət</th>
                </tr>
              </thead>
              <tbody>
                {found.lines.map((l) => {
                  const p = picks[l.saleLineId];
                  return (
                    <tr key={l.saleLineId} className="border-t align-top">
                      <td className="px-3 py-2">
                        {l.productName}
                        <span className="block text-xs text-muted-foreground">{formatCurrency(l.unitAmount)} / vahid</span>
                      </td>
                      <td className="px-3 py-2 text-right">{l.soldQuantity}</td>
                      <td className="px-3 py-2 text-right">{l.returnableQuantity}</td>
                      <td className="px-3 py-2 text-right">
                        {l.tracksSerial ? (
                          <div className="space-y-1 text-left">
                            {l.soldSerials.length === 0 ? (
                              <span className="text-xs text-muted-foreground">—</span>
                            ) : (
                              l.soldSerials.map((s) => (
                                <label key={s} className="flex items-center gap-1 text-xs">
                                  <input
                                    type="checkbox"
                                    checked={p?.serials.includes(s) ?? false}
                                    onChange={(e) =>
                                      setPick(l.saleLineId, {
                                        serials: e.target.checked ? [...(p?.serials ?? []), s] : (p?.serials ?? []).filter((x) => x !== s),
                                      })
                                    }
                                  />
                                  {s}
                                </label>
                              ))
                            )}
                          </div>
                        ) : (
                          <Input
                            className="ml-auto h-8 w-24 text-right"
                            inputMode="decimal"
                            disabled={l.returnableQuantity <= 0}
                            value={p?.quantity ?? ""}
                            onChange={(e) => setPick(l.saleLineId, { quantity: e.target.value })}
                          />
                        )}
                      </td>
                      <td className="px-3 py-2">
                        <select
                          className="h-8 rounded-md border border-input bg-background px-2 text-xs"
                          value={p?.condition ?? StoreReturnCondition.Sellable}
                          onChange={(e) => setPick(l.saleLineId, { condition: Number(e.target.value) as StoreReturnConditionValue })}
                        >
                          {Object.entries(RETURN_CONDITION_LABELS).map(([k, label]) => (
                            <option key={k} value={k}>
                              {label}
                            </option>
                          ))}
                        </select>
                      </td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          </div>

          <div className="grid gap-3 sm:grid-cols-2">
            <div className="space-y-1">
              <Label htmlFor="ret-reason">Səbəb</Label>
              <Input id="ret-reason" value={reason} onChange={(e) => setReason(e.target.value)} placeholder="Məs.: nöqsanlıdır, müştəri fikrini dəyişdi" />
            </div>
            <div className="space-y-1">
              <Label htmlFor="ret-method">Pul necə qaytarılır</Label>
              <select
                id="ret-method"
                className="h-9 w-full rounded-md border border-input bg-background px-3 text-sm"
                value={refundMethod}
                onChange={(e) => setRefundMethod(Number(e.target.value))}
              >
                <option value={StorePaymentMethod.Cash}>Nağd</option>
                <option value={StorePaymentMethod.Card}>Karta</option>
              </select>
            </div>
          </div>

          <label className="flex items-center gap-2 text-sm">
            <input type="checkbox" checked={exchange} onChange={(e) => setExchange(e.target.checked)} />
            Dəyişdirmə (qaytarılanın əvəzinə yeni məhsul verilir)
          </label>

          {exchange && (
            <div className="space-y-3 rounded-md border p-3">
              <Input placeholder="Yeni məhsul: ad, SKU və ya barkod…" value={term} onChange={(e) => setTerm(e.target.value)} />
              {options.length > 0 && (
                <div className="max-h-40 overflow-y-auto rounded-md border">
                  {options.map((p) => (
                    <button
                      type="button"
                      key={p.id}
                      className="flex w-full justify-between px-3 py-1.5 text-left text-sm hover:bg-muted"
                      onClick={() => {
                        setNewLines((prev) =>
                          prev.some((l) => l.product.id === p.id)
                            ? prev.map((l) => (l.product.id === p.id ? { ...l, quantity: l.quantity + 1 } : l))
                            : [...prev, { product: p, quantity: 1, serialText: "" }],
                        );
                        setTerm("");
                      }}
                    >
                      <span>{p.name}</span>
                      <span className="text-muted-foreground">{formatCurrency(p.salePrice)}</span>
                    </button>
                  ))}
                </div>
              )}
              {newLines.map((l) => (
                <div key={l.product.id} className="flex items-start gap-2 text-sm">
                  <span className="flex-1">
                    {l.product.name} <span className="text-xs text-muted-foreground">({formatCurrency(l.product.salePrice)})</span>
                    {l.product.tracksSerial && (
                      <Textarea
                        className="mt-1"
                        rows={2}
                        placeholder="IMEI / serial (hər sətirdə bir)"
                        value={l.serialText}
                        onChange={(e) =>
                          setNewLines((prev) => prev.map((x) => (x.product.id === l.product.id ? { ...x, serialText: e.target.value } : x)))
                        }
                      />
                    )}
                  </span>
                  {!l.product.tracksSerial && (
                    <Input
                      className="h-8 w-20 text-right"
                      inputMode="decimal"
                      value={String(l.quantity)}
                      onChange={(e) =>
                        setNewLines((prev) => prev.map((x) => (x.product.id === l.product.id ? { ...x, quantity: num(e.target.value) } : x)))
                      }
                    />
                  )}
                  <span className="text-xs text-muted-foreground">{UNIT_LABELS[l.product.unit]}</span>
                  <Button size="icon" variant="ghost" className="h-8 w-8" onClick={() => setNewLines((prev) => prev.filter((x) => x.product.id !== l.product.id))}>
                    <Trash2 className="h-4 w-4" />
                  </Button>
                </div>
              ))}
              {difference > 0 && (
                <div className="grid grid-cols-2 gap-2">
                  <div className="space-y-1">
                    <Label htmlFor="ex-cash">Nağd (müştəri verir)</Label>
                    <Input id="ex-cash" inputMode="decimal" value={cashInput} onChange={(e) => setCashInput(e.target.value)} />
                  </div>
                  <div className="space-y-1">
                    <Label htmlFor="ex-card">Kart</Label>
                    <Input id="ex-card" inputMode="decimal" value={cardInput} onChange={(e) => setCardInput(e.target.value)} />
                  </div>
                </div>
              )}
            </div>
          )}

          <div className="rounded-md bg-muted p-3 text-sm">
            <div className="flex justify-between">
              <span>Qaytarılan malın dəyəri</span>
              <span className="font-semibold">{formatCurrency(returnValue)}</span>
            </div>
            {exchange && (
              <>
                <div className="flex justify-between">
                  <span>Yeni məhsulların dəyəri</span>
                  <span className="font-semibold">{formatCurrency(newTotal)}</span>
                </div>
                <div className={cn("flex justify-between font-semibold", difference > 0 ? "text-red-600" : "text-emerald-700")}>
                  <span>{difference > 0 ? "Müştəri əlavə ödəyir" : "Müştərinin geri alacağı"}</span>
                  <span>{formatCurrency(Math.abs(difference))}</span>
                </div>
              </>
            )}
            {!exchange && (
              <div className="flex justify-between font-semibold text-emerald-700">
                <span>Müştəriyə qaytarılır</span>
                <span>{formatCurrency(returnValue)}</span>
              </div>
            )}
          </div>

          <div className="flex justify-end">
            <Button disabled={saving || !shift || chosen.length === 0} onClick={() => void submit()}>
              {saving ? "Yazılır…" : exchange ? "Dəyişdirməni tamamla" : "Qaytarmanı tamamla"}
            </Button>
          </div>
        </div>
      )}

      <div className="rounded-lg border bg-card p-4">
        <h2 className="mb-3 font-semibold">Bu günkü qaytarmalar</h2>
        {recent.length === 0 ? (
          <p className="text-sm text-muted-foreground">Qaytarma yoxdur.</p>
        ) : (
          <table className="w-full text-sm">
            <thead>
              <tr className="text-left text-xs text-muted-foreground">
                <th className="py-1 font-medium">№</th>
                <th className="py-1 font-medium">Çek</th>
                <th className="py-1 font-medium">Saat</th>
                <th className="py-1 font-medium">Kassir</th>
                <th className="py-1 font-medium">Səbəb</th>
                <th className="py-1 text-right font-medium">Məbləğ</th>
              </tr>
            </thead>
            <tbody>
              {recent.map((r) => (
                <tr key={r.id} className="border-t">
                  <td className="py-1.5 font-medium">
                    {r.documentNo} {r.exchangeSaleId && <Badge variant="secondary">Dəyişdirmə</Badge>}
                  </td>
                  <td className="py-1.5">{r.saleDocumentNo}</td>
                  <td className="py-1.5">{new Date(r.createdAtUtc).toLocaleTimeString("az-AZ")}</td>
                  <td className="py-1.5">{r.cashierName ?? "—"}</td>
                  <td className="py-1.5">{r.reason}</td>
                  <td className="py-1.5 text-right">{formatCurrency(r.totalAmount)}</td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </div>
    </div>
  );
}
