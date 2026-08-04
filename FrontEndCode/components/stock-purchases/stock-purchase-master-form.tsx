"use client";

import { useCallback, useEffect, useState } from "react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Checkbox } from "@/components/ui/checkbox";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { Textarea } from "@/components/ui/textarea";
import { Loader2, RefreshCw, Trash2, Plus } from "lucide-react";
import { toast } from "sonner";
import {
  PurchaseCurrency,
  CURRENCY_LABELS,
  getCbarExchangeRate,
  type CreateStockPurchasePayload,
  type UpdateStockPurchasePayload,
  type StockPurchaseDto,
  type PurchaseCurrencyValue,
} from "@/lib/services/stock-purchase-service";
import { getWarehouses } from "@/lib/services/warehouse-service";
import { getStockItemsForAllCompanies } from "@/lib/services/stock-item-service";
import { useSelectedCompany } from "@/contexts/selected-company-context";
import { toApiFormError } from "@/lib/api-error";

type LineState = {
  key: string;
  stockItemId: number | "";
  quantity: string;
  unitPriceForeign: string;
  unitPriceAzn: number;
};

type Props = {
  companyId: number;
  initial?: StockPurchaseDto;
  readOnly?: boolean;
  onSave: (payload: CreateStockPurchasePayload | UpdateStockPurchasePayload) => Promise<void>;
  onCancel: () => void;
};

export function StockPurchaseMasterForm({ companyId, initial, readOnly, onSave, onCancel }: Props) {
  const { companies } = useSelectedCompany();
  const [supplierName, setSupplierName] = useState(initial?.supplierName ?? "");
  const [isImport, setIsImport] = useState(initial?.isImport ?? false);
  const [currency, setCurrency] = useState<PurchaseCurrencyValue>(initial?.currency ?? PurchaseCurrency.AZN);
  const [exchangeRate, setExchangeRate] = useState(String(initial?.exchangeRate ?? 1));
  const [purchaseDate, setPurchaseDate] = useState(
    initial?.purchaseDate ? initial.purchaseDate.slice(0, 10) : new Date().toISOString().slice(0, 10)
  );
  const [warehouseId, setWarehouseId] = useState<number | "">(initial?.warehouseId ?? "");
  const [note, setNote] = useState(initial?.note ?? "");
  const [lines, setLines] = useState<LineState[]>(
    initial?.lines.map((l) => ({
      key: String(l.id),
      stockItemId: l.stockItemId,
      quantity: String(l.quantity),
      unitPriceForeign: String(l.unitPriceForeign),
      unitPriceAzn: l.unitPriceAzn,
    })) ?? []
  );

  const [warehouses, setWarehouses] = useState<{ id: number; name: string }[]>([]);
  const [stockItems, setStockItems] = useState<{ id: number; name: string }[]>([]);
  const [fetchingRate, setFetchingRate] = useState(false);
  const [saving, setSaving] = useState(false);
  const [errors, setErrors] = useState<string[]>([]);

  useEffect(() => {
    if (companies.length === 0 && !companyId) return;
    void (async () => {
      try {
        const allCompanyIds = companies.length > 0
          ? companies.map((c) => c.id)
          : [companyId];
        const [wh, si] = await Promise.all([
          getWarehouses(),
          getStockItemsForAllCompanies(allCompanyIds),
        ]);
        setWarehouses(wh);
        setStockItems(si);
      } catch {
        toast.error("Anbar / məhsul siyahısı yüklənmədi.");
      }
    })();
  }, [companies, companyId]);

  // Valyuta dəyişdikdə AZN üçün rate=1 et
  useEffect(() => {
    if (currency === PurchaseCurrency.AZN) setExchangeRate("1");
  }, [currency]);

  // Məzənnə dəyişdikdə bütün sətirləri yenilə
  const rate = parseFloat(exchangeRate) || 1;
  useEffect(() => {
    setLines((prev) =>
      prev.map((l) => ({
        ...l,
        unitPriceAzn: (parseFloat(l.unitPriceForeign) || 0) * rate,
      }))
    );
  }, [rate]);

  const fetchRate = useCallback(async () => {
    if (currency === PurchaseCurrency.AZN) return;
    const code = CURRENCY_LABELS[currency];
    setFetchingRate(true);
    try {
      const r = await getCbarExchangeRate(code, purchaseDate);
      setExchangeRate(String(r));
      toast.success(`CBAR məzənnəsi alındı: 1 ${code} = ${r} AZN`);
    } catch (e) {
      toast.error(toApiFormError(e, "CBAR məzənnəsi alınmadı").message);
    } finally {
      setFetchingRate(false);
    }
  }, [currency, purchaseDate]);

  function addLine() {
    setLines((prev) => [...prev, {
      key: `new-${Date.now()}`,
      stockItemId: "",
      quantity: "",
      unitPriceForeign: "",
      unitPriceAzn: 0,
    }]);
  }

  function removeLine(key: string) {
    setLines((prev) => prev.filter((l) => l.key !== key));
  }

  function updateLine(key: string, field: keyof LineState, value: string | number) {
    setLines((prev) =>
      prev.map((l) => {
        if (l.key !== key) return l;
        const updated = { ...l, [field]: value };
        if (field === "unitPriceForeign") {
          updated.unitPriceAzn = (parseFloat(String(value)) || 0) * rate;
        }
        return updated;
      })
    );
  }

  const totalForeign = lines.reduce((s, l) => s + (parseFloat(l.quantity) || 0) * (parseFloat(l.unitPriceForeign) || 0), 0);
  const totalAzn = lines.reduce((s, l) => s + (parseFloat(l.quantity) || 0) * l.unitPriceAzn, 0);

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    const errs: string[] = [];
    if (!supplierName.trim()) errs.push("Supplier adı tələb olunur.");
    if (!warehouseId) errs.push("Anbar seçilməlidir.");
    if (lines.length === 0) errs.push("Ən azı bir sətir əlavə edin.");
    for (const l of lines) {
      if (!l.stockItemId) errs.push("Bütün sətirlərdə məhsul seçilməlidir.");
      if (!(parseFloat(l.quantity) > 0)) errs.push("Miqdar 0-dan böyük olmalıdır.");
      if (parseFloat(l.unitPriceForeign) < 0) errs.push("Qiymət mənfi ola bilməz.");
    }
    if (errs.length) { setErrors(errs); return; }
    setErrors([]);
    setSaving(true);
    try {
      const payload = {
        companyId,
        supplierName: supplierName.trim(),
        isImport,
        currency,
        exchangeRate: parseFloat(exchangeRate),
        purchaseDate: new Date(purchaseDate).toISOString(),
        warehouseId: Number(warehouseId),
        note: note.trim() || null,
        lines: lines.map((l) => ({
          id: l.key.startsWith("new-") ? undefined : Number(l.key),
          stockItemId: Number(l.stockItemId),
          quantity: parseFloat(l.quantity),
          unitPriceForeign: parseFloat(l.unitPriceForeign),
        })),
      };
      await onSave(payload);
    } catch (e) {
      toast.error(toApiFormError(e, "Save failed").message);
    } finally {
      setSaving(false);
    }
  }

  const currencyCode = CURRENCY_LABELS[currency];

  return (
    <form onSubmit={handleSubmit} className="space-y-6">
      {errors.length > 0 && (
        <div className="rounded-md border border-destructive/40 bg-destructive/10 px-4 py-3 text-sm text-destructive space-y-1">
          {errors.map((e) => <div key={e}>{e}</div>)}
        </div>
      )}

      {/* Header fields */}
      <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3">
        <div className="space-y-1.5">
          <Label>Supplier Name *</Label>
          <Input value={supplierName} onChange={(e) => setSupplierName(e.target.value)} disabled={readOnly} placeholder="Supplier adı" />
        </div>

        <div className="space-y-1.5">
          <Label>Purchase Date *</Label>
          <Input type="date" value={purchaseDate} onChange={(e) => setPurchaseDate(e.target.value)} disabled={readOnly} />
        </div>

        <div className="space-y-1.5">
          <Label>Warehouse *</Label>
          <Select value={String(warehouseId)} onValueChange={(v) => setWarehouseId(Number(v))} disabled={readOnly}>
            <SelectTrigger><SelectValue placeholder="Anbar seçin" /></SelectTrigger>
            <SelectContent>
              {warehouses.map((w) => (
                <SelectItem key={w.id} value={String(w.id)}>{w.name}</SelectItem>
              ))}
            </SelectContent>
          </Select>
        </div>

        <div className="space-y-1.5">
          <Label>Currency</Label>
          <Select value={String(currency)} onValueChange={(v) => setCurrency(Number(v) as PurchaseCurrencyValue)} disabled={readOnly}>
            <SelectTrigger><SelectValue /></SelectTrigger>
            <SelectContent>
              {Object.entries(CURRENCY_LABELS).map(([val, label]) => (
                <SelectItem key={val} value={val}>{label}</SelectItem>
              ))}
            </SelectContent>
          </Select>
        </div>

        <div className="space-y-1.5">
          <Label>Exchange Rate (1 {currencyCode} = ? AZN)</Label>
          <div className="flex gap-2">
            <Input
              type="number"
              step="0.000001"
              min="0"
              value={exchangeRate}
              onChange={(e) => setExchangeRate(e.target.value)}
              disabled={readOnly || currency === PurchaseCurrency.AZN}
              className="flex-1"
            />
            {!readOnly && currency !== PurchaseCurrency.AZN && (
              <Button type="button" variant="outline" size="sm" onClick={fetchRate} disabled={fetchingRate}>
                {fetchingRate ? <Loader2 className="h-4 w-4 animate-spin" /> : <RefreshCw className="h-4 w-4" />}
                CBAR
              </Button>
            )}
          </div>
        </div>

        <div className="flex items-center gap-2 pt-6">
          <Checkbox
            id="isImport"
            checked={isImport}
            onCheckedChange={(v) => setIsImport(v === true)}
            disabled={readOnly}
          />
          <Label htmlFor="isImport">Import (xarici alış)</Label>
        </div>
      </div>

      <div className="space-y-1.5">
        <Label>Note</Label>
        <Textarea value={note} onChange={(e) => setNote(e.target.value)} disabled={readOnly} rows={2} placeholder="Qeyd..." />
      </div>

      {/* Lines */}
      <div className="space-y-3">
        <div className="flex items-center justify-between">
          <h3 className="font-medium">Purchase Lines</h3>
          {!readOnly && (
            <Button type="button" variant="outline" size="sm" onClick={addLine}>
              <Plus className="h-4 w-4 mr-1" /> Add line
            </Button>
          )}
        </div>

        <div className="rounded-lg border border-border overflow-x-auto">
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b border-border bg-muted/40">
                <th className="text-left py-2 px-3 font-medium">Stock Item</th>
                <th className="text-right py-2 px-3 font-medium">Qty</th>
                <th className="text-right py-2 px-3 font-medium">Unit Price ({currencyCode})</th>
                <th className="text-right py-2 px-3 font-medium">Exchange Rate</th>
                <th className="text-right py-2 px-3 font-medium">Unit Price (AZN)</th>
                <th className="text-right py-2 px-3 font-medium">Total ({currencyCode})</th>
                <th className="text-right py-2 px-3 font-medium">Total (AZN)</th>
                {!readOnly && <th className="w-10" />}
              </tr>
            </thead>
            <tbody>
              {lines.length === 0 ? (
                <tr>
                  <td colSpan={8} className="py-6 text-center text-muted-foreground text-sm">
                    Sətir yoxdur. &quot;Add line&quot; ilə əlavə edin.
                  </td>
                </tr>
              ) : (
                lines.map((l) => {
                  const qty = parseFloat(l.quantity) || 0;
                  const priceForeign = parseFloat(l.unitPriceForeign) || 0;
                  const priceAzn = l.unitPriceAzn;
                  return (
                    <tr key={l.key} className="border-b border-border">
                      <td className="py-2 px-3">
                        {readOnly ? (
                          <span>{stockItems.find((s) => s.id === l.stockItemId)?.name ?? l.stockItemId}</span>
                        ) : (
                          <Select
                            value={String(l.stockItemId)}
                            onValueChange={(v) => updateLine(l.key, "stockItemId", Number(v))}
                          >
                            <SelectTrigger className="h-8 min-w-[160px]"><SelectValue placeholder="Seçin" /></SelectTrigger>
                            <SelectContent>
                              {stockItems.map((s) => (
                                <SelectItem key={s.id} value={String(s.id)}>{s.name}</SelectItem>
                              ))}
                            </SelectContent>
                          </Select>
                        )}
                      </td>
                      <td className="py-2 px-3">
                        {readOnly ? (
                          <span className="block text-right">{qty}</span>
                        ) : (
                          <Input type="number" min="0" step="0.001" className="h-8 w-24 text-right" value={l.quantity}
                            onChange={(e) => updateLine(l.key, "quantity", e.target.value)} />
                        )}
                      </td>
                      <td className="py-2 px-3">
                        {readOnly ? (
                          <span className="block text-right">{priceForeign.toFixed(4)}</span>
                        ) : (
                          <Input type="number" min="0" step="0.0001" className="h-8 w-28 text-right" value={l.unitPriceForeign}
                            onChange={(e) => updateLine(l.key, "unitPriceForeign", e.target.value)} />
                        )}
                      </td>
                      <td className="py-2 px-3 text-right tabular-nums text-muted-foreground">{rate.toFixed(4)}</td>
                      <td className="py-2 px-3 text-right tabular-nums">{priceAzn.toFixed(4)}</td>
                      <td className="py-2 px-3 text-right tabular-nums">{(qty * priceForeign).toFixed(2)}</td>
                      <td className="py-2 px-3 text-right tabular-nums font-medium">{(qty * priceAzn).toFixed(2)} ₼</td>
                      {!readOnly && (
                        <td className="py-2 px-3 text-right">
                          <Button type="button" variant="ghost" size="icon" className="h-7 w-7"
                            onClick={() => removeLine(l.key)}>
                            <Trash2 className="h-4 w-4 text-destructive" />
                          </Button>
                        </td>
                      )}
                    </tr>
                  );
                })
              )}
            </tbody>
            {lines.length > 0 && (
              <tfoot>
                <tr className="border-t border-border bg-muted/20 font-medium">
                  <td colSpan={5} className="py-2 px-3 text-right">Total:</td>
                  <td className="py-2 px-3 text-right tabular-nums">{totalForeign.toFixed(2)} {currencyCode}</td>
                  <td className="py-2 px-3 text-right tabular-nums">{totalAzn.toFixed(2)} ₼</td>
                  {!readOnly && <td />}
                </tr>
              </tfoot>
            )}
          </table>
        </div>
      </div>

      {/* Actions */}
      <div className="flex justify-end gap-3">
        <Button type="button" variant="outline" onClick={onCancel}>Cancel</Button>
        {!readOnly && (
          <Button type="submit" disabled={saving}>
            {saving && <Loader2 className="h-4 w-4 mr-2 animate-spin" />}
            Save
          </Button>
        )}
      </div>
    </form>
  );
}
