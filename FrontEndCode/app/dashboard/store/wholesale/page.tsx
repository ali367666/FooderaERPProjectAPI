"use client";

import { useEffect, useMemo, useRef, useState } from "react";
import Link from "next/link";
import { Check, Plus, Printer, Search, Trash2, UserRound, X } from "lucide-react";
import { toast } from "sonner";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { formatCurrency } from "@/lib/format-currency";
import { printWholesaleInvoice } from "@/lib/store/wholesale-invoice-print";
import { useStorePermissions } from "@/lib/store/use-store-permissions";
import { cn } from "@/lib/utils";
import { getWarehouses, type Warehouse } from "@/lib/services/warehouse-service";
import {
  StorePaymentMethod,
  UNIT_LABELS,
  createStoreSale,
  getStoreRegisters,
  searchStoreCustomers,
  searchStoreProducts,
  type StoreCashRegister,
  type StoreCustomer,
  type StoreProduct,
  type StoreSale,
} from "@/lib/store/store-service";

const SELECT_CLASS = "h-9 w-full rounded-md border border-input bg-background px-2 text-sm";
const r2 = (n: number) => Math.round(n * 100) / 100;

type Line = { product: StoreProduct; quantity: string; discount: string };
type PayMode = "cash" | "split" | "debt";

/** The price the server will use: the wholesale price when the product has one, else the list price. */
const priceOf = (p: StoreProduct, retail: boolean) => (!retail && p.wholesalePrice != null && p.wholesalePrice > 0 ? p.wholesalePrice : p.effectivePrice);

/**
 * Wholesale sale (topdan satış) of the admin panel: pick the warehouse and the customer, add goods at the wholesale price, and settle it in cash,
 * on credit or part and part. The sale goes through the same rules as a sale at the till; the cash lands on the chosen till's open shift.
 */
export default function StoreWholesalePage() {
  const perms = useStorePermissions();
  const allowed = perms.has("StoreSale.Approve");

  const [warehouses, setWarehouses] = useState<Warehouse[]>([]);
  const [registers, setRegisters] = useState<StoreCashRegister[]>([]);
  const [warehouseId, setWarehouseId] = useState<number | "">("");
  const [registerId, setRegisterId] = useState<number | "">("");
  const [retail, setRetail] = useState(false);

  // customer
  const [customer, setCustomer] = useState<StoreCustomer | null>(null);
  const [customerTerm, setCustomerTerm] = useState("");
  const [customerResults, setCustomerResults] = useState<StoreCustomer[]>([]);
  const [customerOpen, setCustomerOpen] = useState(false);

  // goods
  const [term, setTerm] = useState("");
  const [results, setResults] = useState<StoreProduct[]>([]);
  const [lines, setLines] = useState<Line[]>([]);
  const searchRef = useRef<HTMLInputElement>(null);

  // payment
  const [mode, setMode] = useState<PayMode>("cash");
  const [cash, setCash] = useState("");
  const [note, setNote] = useState("");
  const [saving, setSaving] = useState(false);
  const [operationId, setOperationId] = useState(() => crypto.randomUUID());
  const [done, setDone] = useState<StoreSale | null>(null);

  useEffect(() => {
    getWarehouses().then((w) => {
      setWarehouses(w);
      setWarehouseId((cur) => (cur === "" ? (w[0]?.id ?? "") : cur));
    }).catch(() => setWarehouses([]));
    getStoreRegisters().then((r) => {
      setRegisters(r);
      setRegisterId((cur) => (cur === "" ? (r.find((x) => x.isActive && x.openShiftId != null)?.id ?? "") : cur));
    }).catch(() => setRegisters([]));
  }, []);

  // goods search (with the stock of the chosen warehouse)
  useEffect(() => {
    const q = term.trim();
    if (q.length < 1 || warehouseId === "") {
      setResults([]);
      return;
    }
    const t = setTimeout(() => {
      searchStoreProducts({ q, isActive: true, pageSize: 8, warehouseId }).then((r) => setResults(r.items)).catch(() => setResults([]));
    }, 200);
    return () => clearTimeout(t);
  }, [term, warehouseId]);

  useEffect(() => {
    if (!customerOpen) return;
    const t = setTimeout(() => {
      searchStoreCustomers(customerTerm.trim()).then(setCustomerResults).catch(() => setCustomerResults([]));
    }, 250);
    return () => clearTimeout(t);
  }, [customerOpen, customerTerm]);

  const addProduct = (p: StoreProduct) => {
    if (p.tracksSerial) {
      toast.error(`“${p.name}” IMEI/serial ilə izlənir; bu məhsulu kassadan satın.`);
      return;
    }
    setLines((prev) => {
      const i = prev.findIndex((l) => l.product.id === p.id);
      if (i >= 0) return prev.map((l, idx) => (idx === i ? { ...l, quantity: String((Number(l.quantity) || 0) + 1) } : l));
      return [...prev, { product: p, quantity: "1", discount: "" }];
    });
    setTerm("");
    setResults([]);
    searchRef.current?.focus();
  };

  const setLine = (i: number, patch: Partial<Line>) => setLines((prev) => prev.map((l, idx) => (idx === i ? { ...l, ...patch } : l)));

  const calc = useMemo(() => {
    const rows = lines.map((l) => {
      const qty = Number(l.quantity.replace(",", ".")) || 0;
      const price = priceOf(l.product, retail);
      const gross = r2(qty * price);
      const discount = Math.min(gross, Math.max(0, Number(l.discount.replace(",", ".")) || 0));
      return { qty, price, gross, discount, total: r2(gross - discount) };
    });
    return { rows, subTotal: r2(rows.reduce((s, r) => s + r.gross, 0)), discount: r2(rows.reduce((s, r) => s + r.discount, 0)), total: r2(rows.reduce((s, r) => s + r.total, 0)) };
  }, [lines, retail]);

  // The payment: all cash, part cash with the rest on credit, or all on credit.
  const cashPaid = mode === "cash" ? calc.total : mode === "debt" ? 0 : Math.min(calc.total, Math.max(0, Number(cash.replace(",", ".")) || 0));
  const debtPaid = r2(calc.total - cashPaid);
  const debtNow = customer?.debt ?? 0;
  const creditLeft = customer ? customer.creditLimit - Math.max(0, debtNow) : 0;
  const overLimit = debtPaid > 0 && customer != null && debtPaid > creditLeft;

  const register = registers.find((r) => r.id === registerId);
  const problems: string[] = [];
  if (warehouseId === "") problems.push("Anbarı seçin.");
  if (registerId === "") problems.push("Kassanı seçin.");
  else if (register && register.openShiftId == null) problems.push("Seçilən kassada açıq növbə yoxdur.");
  if (lines.length === 0) problems.push("Məhsul əlavə edin.");
  if (lines.some((l, i) => calc.rows[i].qty <= 0)) problems.push("Miqdarlar 0-dan böyük olmalıdır.");
  if (debtPaid > 0 && !customer) problems.push("Borca satış üçün alıcını seçin.");
  if (overLimit) problems.push(`Kredit limiti aşılır (qalan limit: ${formatCurrency(Math.max(0, creditLeft))}).`);
  if (calc.total <= 0 && lines.length > 0) problems.push("Satışın məbləği 0-dan böyük olmalıdır.");

  const submit = async () => {
    if (problems.length > 0) return void toast.error(problems[0]);
    setSaving(true);
    try {
      const sale = await createStoreSale({
        operationId,
        storeCashRegisterId: registerId as number,
        warehouseId: warehouseId as number,
        customerId: customer?.id ?? null,
        wholesale: true,
        useRetailPrice: retail,
        note: note.trim() || null,
        lines: lines.map((l, i) => ({ productId: l.product.id, quantity: calc.rows[i].qty, discountAmount: calc.rows[i].discount })),
        payments: [
          ...(cashPaid > 0 ? [{ method: StorePaymentMethod.Cash, amount: cashPaid }] : []),
          ...(debtPaid > 0 ? [{ method: StorePaymentMethod.Debt, amount: debtPaid }] : []),
        ],
      });
      setDone(sale);
      toast.success(`Satış rəsmiləşdirildi: ${sale.documentNo}`);
    } catch (e) {
      toast.error(e instanceof Error ? e.message : "Satış tamamlanmadı.");
    } finally {
      setSaving(false);
    }
  };

  const reset = () => {
    setLines([]);
    setCustomer(null);
    setCash("");
    setNote("");
    setMode("cash");
    setDone(null);
    setOperationId(crypto.randomUUID());
    searchRef.current?.focus();
  };

  const units = useMemo(() => Object.fromEntries(lines.map((l) => [l.product.id, l.product.unit])), [lines]);

  if (!allowed) {
    return (
      <div className="p-6">
        <h1 className="text-2xl font-bold">Topdan satış</h1>
        <p className="mt-2 text-sm text-muted-foreground">Bu bölmə menecer icazəsi (StoreSale.Approve) olan istifadəçilər üçündür.</p>
      </div>
    );
  }

  // the finished sale
  if (done) {
    return (
      <div className="mx-auto max-w-xl space-y-4 p-6">
        <div className="rounded-2xl border border-emerald-500/40 bg-emerald-500/5 p-6 text-center">
          <div className="mx-auto mb-3 flex h-12 w-12 items-center justify-center rounded-full bg-emerald-500 text-white">
            <Check className="h-6 w-6" />
          </div>
          <h1 className="text-xl font-bold">Satış rəsmiləşdirildi</h1>
          <p className="mt-1 font-mono text-muted-foreground">{done.documentNo}</p>
          <p className="mt-3 text-3xl font-bold">{formatCurrency(done.total)}</p>
          {done.debtAmount > 0 && <p className="mt-1 text-sm text-amber-600">Borca: {formatCurrency(done.debtAmount)}</p>}
          <p className="mt-1 text-sm text-muted-foreground">{done.customerName ?? "Nağd alıcı"}</p>
        </div>
        <div className="flex flex-wrap justify-center gap-2">
          <Button onClick={() => !printWholesaleInvoice(done, units) && toast.error("Çap pəncərəsi açılmadı — popup bloklamasını söndürün.")}>
            <Printer className="mr-1 h-4 w-4" />
            Qaimə çap et
          </Button>
          <Button variant="outline" onClick={reset}>
            <Plus className="mr-1 h-4 w-4" />
            Yeni satış
          </Button>
          <Button variant="ghost" asChild>
            <Link href="/dashboard/store/sales">Satışlara bax</Link>
          </Button>
        </div>
      </div>
    );
  }

  return (
    <div className="space-y-4 p-4">
      <div>
        <h1 className="text-2xl font-bold">Topdan satış</h1>
        <p className="text-sm text-muted-foreground">
          Anbardan alıcıya topdan qiymətlə satış: nağd, borca və ya hissə-hissə. Nağd hissə seçilən kassanın açıq növbəsinə düşür, borc alıcının hesabına yazılır.
        </p>
      </div>

      <div className="grid gap-4 lg:grid-cols-[1fr_22rem]">
        <div className="space-y-4">
          {/* the document's header */}
          <div className="grid gap-3 rounded-lg border bg-card p-3 sm:grid-cols-2 lg:grid-cols-4">
            <div className="space-y-1">
              <Label htmlFor="ws-wh">Anbar</Label>
              <select id="ws-wh" className={SELECT_CLASS} value={warehouseId} onChange={(e) => { setWarehouseId(e.target.value ? Number(e.target.value) : ""); setLines([]); }}>
                {warehouses.map((w) => (
                  <option key={w.id} value={w.id}>
                    {w.name}
                  </option>
                ))}
              </select>
            </div>
            <div className="space-y-1">
              <Label htmlFor="ws-reg">Kassa (nağd üçün)</Label>
              <select id="ws-reg" className={SELECT_CLASS} value={registerId} onChange={(e) => setRegisterId(e.target.value ? Number(e.target.value) : "")}>
                <option value="">Seçin</option>
                {registers.filter((r) => r.isActive).map((r) => (
                  <option key={r.id} value={r.id} disabled={r.openShiftId == null}>
                    {r.name}
                    {r.openShiftId == null ? " (növbə bağlıdır)" : ""}
                  </option>
                ))}
              </select>
            </div>
            <div className="space-y-1">
              <Label>Qiymət siyahısı</Label>
              <div className="flex rounded-md border">
                {(
                  [
                    [false, "Topdan"],
                    [true, "Pərakəndə"],
                  ] as const
                ).map(([v, label]) => (
                  <button key={label} type="button" onClick={() => setRetail(v)} className={cn("h-9 flex-1 px-3 text-sm", retail === v ? "bg-primary text-primary-foreground" : "hover:bg-muted")}>
                    {label}
                  </button>
                ))}
              </div>
            </div>
            <div className="space-y-1">
              <Label>Alıcı</Label>
              <div className="relative">
                {customer ? (
                  <div className="flex h-9 items-center justify-between rounded-md border bg-muted/40 px-2 text-sm">
                    <span className="flex items-center gap-1.5 truncate">
                      <UserRound className="h-4 w-4 shrink-0" />
                      {customer.name}
                    </span>
                    <button type="button" aria-label="Alıcını sil" onClick={() => setCustomer(null)}>
                      <X className="h-4 w-4" />
                    </button>
                  </div>
                ) : (
                  <Input
                    placeholder="Alıcı: ad, kod, telefon… (boş = nağd alıcı)"
                    value={customerTerm}
                    onFocus={() => setCustomerOpen(true)}
                    onChange={(e) => {
                      setCustomerTerm(e.target.value);
                      setCustomerOpen(true);
                    }}
                  />
                )}
                {customerOpen && !customer && (
                  <div className="absolute z-20 mt-1 max-h-56 w-full overflow-y-auto rounded-md border bg-popover shadow-md">
                    {customerResults.length === 0 ? (
                      <p className="p-3 text-center text-xs text-muted-foreground">Alıcı tapılmadı.</p>
                    ) : (
                      customerResults.map((c) => (
                        <button
                          key={c.id}
                          type="button"
                          className="flex w-full items-center justify-between px-3 py-2 text-left text-sm hover:bg-muted"
                          onClick={() => {
                            setCustomer(c);
                            setCustomerOpen(false);
                            setCustomerTerm("");
                          }}
                        >
                          <span>
                            {c.name}
                            <span className="block text-xs text-muted-foreground">{c.code}</span>
                          </span>
                          {c.debt > 0 && <span className="text-xs text-red-600">borc {formatCurrency(c.debt)}</span>}
                        </button>
                      ))
                    )}
                  </div>
                )}
              </div>
            </div>
          </div>

          {/* goods */}
          <div className="space-y-2 rounded-lg border bg-card p-3">
            <div className="relative">
              <Search className="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
              <Input
                ref={searchRef}
                className="pl-9"
                placeholder="Məhsul axtar: ad, SKU və ya barkod (Enter — ilk nəticəni əlavə edir)"
                value={term}
                onChange={(e) => setTerm(e.target.value)}
                onKeyDown={(e) => {
                  if (e.key === "Enter" && results[0]) {
                    e.preventDefault();
                    addProduct(results[0]);
                  }
                }}
              />
              {results.length > 0 && (
                <div className="absolute z-20 mt-1 w-full overflow-hidden rounded-md border bg-popover shadow-md">
                  {results.map((p) => (
                    <button key={p.id} type="button" onClick={() => addProduct(p)} className="flex w-full items-center justify-between gap-3 px-3 py-2 text-left text-sm hover:bg-muted">
                      <span className="min-w-0 flex-1 truncate font-medium">{p.name}</span>
                      <span className={cn("shrink-0 text-xs", (p.stock ?? 0) <= 0 ? "text-red-600" : "text-muted-foreground")}>
                        stok: {p.stock ?? 0} {UNIT_LABELS[p.unit]}
                      </span>
                      <span className="w-24 shrink-0 text-right font-semibold">{formatCurrency(priceOf(p, retail))}</span>
                    </button>
                  ))}
                </div>
              )}
            </div>

            <div className="overflow-x-auto rounded-md border">
              <table className="w-full text-sm">
                <thead>
                  <tr className="bg-muted/40 text-left text-xs text-muted-foreground">
                    <th className="px-3 py-2 font-medium">Məhsul</th>
                    <th className="px-3 py-2 text-right font-medium">Stok</th>
                    <th className="px-3 py-2 font-medium">Miqdar</th>
                    <th className="px-3 py-2 text-right font-medium">Qiymət</th>
                    <th className="px-3 py-2 font-medium">Endirim (AZN)</th>
                    <th className="px-3 py-2 text-right font-medium">Məbləğ</th>
                    <th />
                  </tr>
                </thead>
                <tbody>
                  {lines.length === 0 ? (
                    <tr>
                      <td colSpan={7} className="px-3 py-10 text-center text-muted-foreground">
                        Məhsul axtarıb əlavə edin.
                      </td>
                    </tr>
                  ) : (
                    lines.map((l, i) => {
                      const r = calc.rows[i];
                      const short = l.product.stock != null && r.qty > l.product.stock;
                      return (
                        <tr key={l.product.id} className="border-t">
                          <td className="px-3 py-2 font-medium">
                            {l.product.name}
                            {l.product.wholesalePrice != null && l.product.wholesalePrice > 0 && !retail && <Badge variant="outline" className="ml-2">topdan</Badge>}
                          </td>
                          <td className={cn("px-3 py-2 text-right text-xs", short ? "font-semibold text-red-600" : "text-muted-foreground")}>
                            {l.product.stock ?? "—"} {UNIT_LABELS[l.product.unit]}
                          </td>
                          <td className="px-3 py-2">
                            <div className="flex items-center gap-1">
                              <Input className="w-24" inputMode="decimal" value={l.quantity} onChange={(e) => setLine(i, { quantity: e.target.value })} />
                              <span className="text-xs text-muted-foreground">{UNIT_LABELS[l.product.unit]}</span>
                            </div>
                          </td>
                          <td className="px-3 py-2 text-right tabular-nums">{formatCurrency(r.price)}</td>
                          <td className="px-3 py-2">
                            <Input className="w-24" inputMode="decimal" placeholder="0" value={l.discount} onChange={(e) => setLine(i, { discount: e.target.value })} />
                          </td>
                          <td className="px-3 py-2 text-right font-medium tabular-nums">{formatCurrency(r.total)}</td>
                          <td className="px-2 py-2">
                            <Button variant="ghost" size="icon" aria-label="Sil" onClick={() => setLines((prev) => prev.filter((_, idx) => idx !== i))}>
                              <Trash2 className="h-4 w-4" />
                            </Button>
                          </td>
                        </tr>
                      );
                    })
                  )}
                </tbody>
              </table>
            </div>
          </div>
        </div>

        {/* the settlement */}
        <div className="h-fit space-y-3 rounded-lg border bg-card p-4 lg:sticky lg:top-4">
          <div className="space-y-1 text-sm">
            <div className="flex justify-between">
              <span className="text-muted-foreground">Məhsul cəmi</span>
              <span className="tabular-nums">{formatCurrency(calc.subTotal)}</span>
            </div>
            {calc.discount > 0 && (
              <div className="flex justify-between">
                <span className="text-muted-foreground">Endirim</span>
                <span className="tabular-nums text-amber-600">−{formatCurrency(calc.discount)}</span>
              </div>
            )}
            <div className="flex justify-between border-t pt-2 text-lg font-bold">
              <span>Cəmi</span>
              <span className="tabular-nums">{formatCurrency(calc.total)}</span>
            </div>
          </div>

          <div className="space-y-2">
            <Label>Ödəniş</Label>
            <div className="grid grid-cols-3 gap-1">
              {(
                [
                  ["cash", "Nağd"],
                  ["split", "Nağd + borc"],
                  ["debt", "Borca"],
                ] as const
              ).map(([k, label]) => (
                <button key={k} type="button" onClick={() => setMode(k)} className={cn("rounded-md border px-2 py-1.5 text-xs", mode === k ? "border-primary bg-primary text-primary-foreground" : "hover:bg-muted")}>
                  {label}
                </button>
              ))}
            </div>
            {mode === "split" && (
              <div className="space-y-1">
                <Label htmlFor="ws-cash" className="text-xs">
                  Nağd alınan məbləğ
                </Label>
                <Input id="ws-cash" inputMode="decimal" value={cash} onChange={(e) => setCash(e.target.value)} placeholder="0.00" />
              </div>
            )}
            <div className="rounded-md bg-muted/40 p-2 text-sm">
              <div className="flex justify-between">
                <span>Nağd</span>
                <b className="tabular-nums">{formatCurrency(cashPaid)}</b>
              </div>
              <div className={cn("flex justify-between", debtPaid > 0 && "text-amber-600")}>
                <span>Borca</span>
                <b className="tabular-nums">{formatCurrency(debtPaid)}</b>
              </div>
            </div>
            {customer && (
              <p className="text-xs text-muted-foreground">
                {customer.name}: indiki borc <b>{formatCurrency(Math.max(0, debtNow))}</b>
                {customer.creditLimit > 0 ? <> · limit <b>{formatCurrency(customer.creditLimit)}</b> · qalan <b className={cn(overLimit && "text-red-600")}>{formatCurrency(Math.max(0, creditLeft))}</b></> : " · kredit limiti verilməyib"}
              </p>
            )}
          </div>

          <div className="space-y-1">
            <Label htmlFor="ws-note" className="text-xs">
              Qeyd
            </Label>
            <Input id="ws-note" value={note} onChange={(e) => setNote(e.target.value)} />
          </div>

          {problems.length > 0 && lines.length > 0 && <p className="text-xs text-red-600">{problems[0]}</p>}

          <Button className="w-full" disabled={saving || problems.length > 0} onClick={() => void submit()}>
            Satışı rəsmiləşdir
          </Button>
          <Button className="w-full" variant="ghost" disabled={saving || (lines.length === 0 && !customer)} onClick={reset}>
            Təmizlə
          </Button>
        </div>
      </div>
    </div>
  );
}
