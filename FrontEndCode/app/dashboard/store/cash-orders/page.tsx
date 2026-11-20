"use client";

import { useCallback, useEffect, useMemo, useState } from "react";
import { ArrowDownToLine, ArrowUpFromLine, Printer } from "lucide-react";
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
import { printCashOrder } from "@/lib/store/cash-order-print";
import { useStorePermissions } from "@/lib/store/use-store-permissions";
import { cn } from "@/lib/utils";
import {
  PAYMENT_LABELS,
  StorePaymentMethod,
  getStoreCashOrders,
  getStoreRegisters,
  getStoreSuppliers,
  payStoreCustomerDebt,
  payStoreSupplier,
  reverseStoreCustomerPayment,
  reverseStoreSupplierPayment,
  searchStoreCustomers,
  type StoreCashOrder,
  type StoreCashOrderList,
  type StoreCashRegister,
  type StoreCustomer,
  type StoreSupplier,
} from "@/lib/store/store-service";

const SELECT_CLASS = "h-9 w-full rounded-md border border-input bg-background px-3 text-sm";
const th = "px-3 py-2 font-medium";
const dateTime = (iso: string) => {
  const d = new Date(iso);
  return `${d.toLocaleDateString("az-AZ")} ${d.toLocaleTimeString("az-AZ", { hour: "2-digit", minute: "2-digit" })}`;
};
const kindLabel = (k: "in" | "out") => (k === "in" ? "Mədaxil orderi" : "Məxaric orderi");

/** Admin: cash orders - receipt orders (money taken from a customer) and payment orders (money paid to a supplier) as documents. */
export default function StoreCashOrdersPage() {
  const perms = useStorePermissions();
  const canIn = perms.has("StoreCustomer.Pay");
  const canOut = perms.has("StoreSupplier.Pay");
  const timesOn = useTimeRangeEnabled();

  const today = toDateInputValue(new Date());
  const monthStart = toDateInputValue(new Date(new Date().getFullYear(), new Date().getMonth(), 1));
  const [kind, setKind] = useState<"" | "in" | "out">("");
  const [from, setFrom] = useState(monthStart);
  const [to, setTo] = useState(today);
  const [fromTime, setFromTime] = useState("");
  const [toTime, setToTime] = useState("");
  const [registerId, setRegisterId] = useState<number | "">("");
  const [partyTerm, setPartyTerm] = useState("");
  const [codeInput, setCodeInput] = useState("");
  const [code, setCode] = useState("");
  const [registers, setRegisters] = useState<StoreCashRegister[]>([]);
  const [list, setList] = useState<StoreCashOrderList | null>(null);
  const [loading, setLoading] = useState(true);

  const [selected, setSelected] = useState<StoreCashOrder | null>(null);
  const [reverseFor, setReverseFor] = useState<StoreCashOrder | null>(null);

  // create form
  const [createKind, setCreateKind] = useState<"in" | "out" | null>(null);
  const [customers, setCustomers] = useState<StoreCustomer[]>([]);
  const [suppliers, setSuppliers] = useState<StoreSupplier[]>([]);
  const [fRegister, setFRegister] = useState<number | "">("");
  const [fParty, setFParty] = useState<number | "">("");
  const [fPartyTerm, setFPartyTerm] = useState("");
  const [fAmount, setFAmount] = useState("");
  const [fMethod, setFMethod] = useState<number>(StorePaymentMethod.Cash);
  const [fBasis, setFBasis] = useState("");
  const [fNote, setFNote] = useState("");
  const [saving, setSaving] = useState(false);

  useEffect(() => {
    getStoreRegisters().then(setRegisters).catch(() => setRegisters([]));
  }, []);

  useEffect(() => {
    const t = setTimeout(() => setCode(codeInput.trim()), 300);
    return () => clearTimeout(t);
  }, [codeInput]);

  const load = useCallback(async () => {
    setLoading(true);
    try {
      setList(
        await getStoreCashOrders({
          kind,
          from: timesOn ? withTime(from, fromTime) : from,
          to: timesOn ? withTime(to, toTime) : to,
          registerId: registerId === "" ? null : registerId,
          code: code || undefined,
        }),
      );
    } catch (e) {
      toast.error(e instanceof Error ? e.message : "Kassa orderləri yüklənmədi.");
    } finally {
      setLoading(false);
    }
  }, [kind, from, to, fromTime, toTime, timesOn, registerId, code]);

  useEffect(() => {
    void load();
  }, [load]);

  // The party filter is by name, applied to the loaded list.
  const items = useMemo(() => {
    const q = partyTerm.trim().toLowerCase();
    return (list?.items ?? []).filter((o) => !q || o.partyName.toLowerCase().includes(q) || (o.partyCode ?? "").toLowerCase().includes(q));
  }, [list, partyTerm]);
  const totalIn = items.filter((o) => o.kind === "in" && !o.isReversed).reduce((s, o) => s + o.amount, 0);
  const totalOut = items.filter((o) => o.kind === "out" && !o.isReversed).reduce((s, o) => s + o.amount, 0);

  // ---- create -----------------------------------------------------------------------------
  const startCreate = (k: "in" | "out") => {
    setCreateKind(k);
    setFRegister(registers.find((r) => r.isActive)?.id ?? "");
    setFParty("");
    setFPartyTerm("");
    setFAmount("");
    setFMethod(StorePaymentMethod.Cash);
    setFBasis("");
    setFNote("");
    if (k === "in") searchStoreCustomers("").then(setCustomers).catch(() => setCustomers([]));
    else getStoreSuppliers().then(setSuppliers).catch(() => setSuppliers([]));
  };

  // A customer search happens on the server (the list is long), the suppliers are filtered here.
  useEffect(() => {
    if (createKind !== "in") return;
    const t = setTimeout(() => {
      searchStoreCustomers(fPartyTerm.trim()).then(setCustomers).catch(() => setCustomers([]));
    }, 250);
    return () => clearTimeout(t);
  }, [createKind, fPartyTerm]);

  const partyOptions = useMemo(() => {
    if (createKind === "in") return customers.map((c) => ({ id: c.id, name: `${c.name} (${c.code})`, debt: c.debt }));
    const q = fPartyTerm.trim().toLowerCase();
    return suppliers.filter((s) => !q || s.name.toLowerCase().includes(q)).map((s) => ({ id: s.id, name: s.name, debt: undefined as number | undefined }));
  }, [createKind, customers, suppliers, fPartyTerm]);
  const chosen = partyOptions.find((p) => p.id === fParty);

  const submit = async (andPrint: boolean) => {
    const amount = Number(fAmount.replace(",", "."));
    if (fParty === "") return void toast.error(createKind === "in" ? "Alıcını seçin." : "Təchizatçını seçin.");
    if (!Number.isFinite(amount) || amount <= 0) return void toast.error("Məbləği yazın.");
    if (createKind === "in" && fRegister === "") return void toast.error("Kassanı seçin.");
    setSaving(true);
    try {
      if (createKind === "in") {
        await payStoreCustomerDebt({ customerId: fParty, amount, method: fMethod, reference: fBasis.trim() || null, note: fNote.trim() || null, registerId: fRegister === "" ? null : fRegister });
      } else {
        await payStoreSupplier({ supplierId: fParty, amount, method: fMethod, reference: fBasis.trim() || null, note: fNote.trim() || null });
      }
      toast.success(createKind === "in" ? "Mədaxil orderi yazıldı" : "Məxaric orderi yazıldı");
      const wasIn = createKind === "in";
      setCreateKind(null);
      const fresh = await getStoreCashOrders({ kind: "", from: monthStart, to: today });
      if (andPrint) {
        // the newest order of that kind is the one just written
        const created = fresh.items.find((o) => o.kind === (wasIn ? "in" : "out"));
        if (created && !printCashOrder(created)) toast.error("Çap pəncərəsi açılmadı — popup bloklamasını söndürün.");
      }
      void load();
    } catch (e) {
      toast.error(e instanceof Error ? e.message : "Order yazılmadı.");
    } finally {
      setSaving(false);
    }
  };

  const reverse = async (reason: string) => {
    if (!reverseFor) return;
    if (reverseFor.kind === "in") await reverseStoreCustomerPayment(reverseFor.id, reason);
    else await reverseStoreSupplierPayment(reverseFor.id, reason);
    toast.success("Order ləğv edildi.");
    setSelected(null);
    void load();
  };

  return (
    <div className="space-y-4 p-4">
      <div className="flex flex-wrap items-start justify-between gap-3">
        <div>
          <h1 className="text-2xl font-bold">Kassa orderləri</h1>
          <p className="text-sm text-muted-foreground">
            Mədaxil orderi — alıcıdan ödəniş, məxaric orderi — təchizatçıya ödəniş. Kassada qəbul edilən ödənişlər də burada sənəd kimi görünür.
          </p>
        </div>
        <div className="flex gap-2">
          {canIn && (
            <Button onClick={() => startCreate("in")}>
              <ArrowDownToLine className="mr-1 h-4 w-4" />
              Mədaxil orderi
            </Button>
          )}
          {canOut && (
            <Button variant="outline" onClick={() => startCreate("out")}>
              <ArrowUpFromLine className="mr-1 h-4 w-4" />
              Məxaric orderi
            </Button>
          )}
        </div>
      </div>

      <div className="flex flex-wrap items-end gap-3 rounded-lg border bg-card p-3">
        <div className="space-y-1">
          <Label htmlFor="co-kind">Növ</Label>
          <select id="co-kind" className={cn(SELECT_CLASS, "w-40")} value={kind} onChange={(e) => setKind(e.target.value as "" | "in" | "out")}>
            <option value="">Hamısı</option>
            <option value="in">Mədaxil</option>
            <option value="out">Məxaric</option>
          </select>
        </div>
        <div className="space-y-1">
          <Label htmlFor="co-from">Başlanğıc</Label>
          <Input id="co-from" type="date" value={from} max={to} disabled={code !== ""} onChange={(e) => setFrom(e.target.value)} />
        </div>
        <div className="space-y-1">
          <Label htmlFor="co-to">Son</Label>
          <Input id="co-to" type="date" value={to} min={from} disabled={code !== ""} onChange={(e) => setTo(e.target.value)} />
        </div>
        <TimeRangeInputs idPrefix="co" fromTime={fromTime} toTime={toTime} onFromTime={setFromTime} onToTime={setToTime} />
        <div className="space-y-1">
          <Label htmlFor="co-reg">Kassa</Label>
          <select id="co-reg" className={cn(SELECT_CLASS, "w-44")} value={registerId} onChange={(e) => setRegisterId(e.target.value ? Number(e.target.value) : "")}>
            <option value="">Hamısı</option>
            {registers.map((r) => (
              <option key={r.id} value={r.id}>
                {r.name}
              </option>
            ))}
          </select>
        </div>
        <div className="space-y-1">
          <Label htmlFor="co-party">Alıcı / təchizatçı</Label>
          <Input id="co-party" className="w-48" placeholder="Ad…" value={partyTerm} onChange={(e) => setPartyTerm(e.target.value)} />
        </div>
        <div className="space-y-1">
          <Label htmlFor="co-code">Sənəd kodu</Label>
          <Input id="co-code" className="w-40" placeholder="CP-0000012" value={codeInput} onChange={(e) => setCodeInput(e.target.value)} />
        </div>
      </div>

      <div className="grid grid-cols-2 gap-2 sm:max-w-md">
        <div className="rounded-lg border p-3">
          <p className="text-xs text-muted-foreground">Mədaxil (cəmi)</p>
          <p className="text-lg font-semibold text-emerald-600">{formatCurrency(totalIn)}</p>
        </div>
        <div className="rounded-lg border p-3">
          <p className="text-xs text-muted-foreground">Məxaric (cəmi)</p>
          <p className="text-lg font-semibold text-red-600">{formatCurrency(totalOut)}</p>
        </div>
      </div>

      <div className="space-y-2">
        {loading ? (
          <p className="py-8 text-center text-sm text-muted-foreground">Yüklənir…</p>
        ) : items.length === 0 ? (
          <p className="py-8 text-center text-sm text-muted-foreground">Bu şərtlərlə kassa orderi tapılmadı.</p>
        ) : (
          items.map((o) => (
            <button
              key={`${o.kind}-${o.id}`}
              type="button"
              onClick={() => setSelected(o)}
              className={cn("block w-full rounded-lg border bg-card px-4 py-3 text-left transition-colors hover:bg-muted/50", o.isReversed && "opacity-60")}
            >
              <div className="flex items-center justify-between gap-3">
                <span className="flex items-center gap-2 font-semibold">
                  {kindLabel(o.kind)}
                  {o.isReversed && <Badge variant="destructive">Ləğv edilib</Badge>}
                </span>
                <span className="font-mono text-sm text-muted-foreground">{o.documentNo}</span>
              </div>
              <div className="mt-1 flex flex-wrap items-center justify-between gap-x-6 gap-y-1 text-sm">
                <span className="flex flex-wrap gap-x-6 gap-y-1">
                  <span>
                    <span className="text-muted-foreground">{o.kind === "in" ? "Alıcı: " : "Təchizatçı: "}</span>
                    {o.partyName}
                  </span>
                  {o.registerName && (
                    <span>
                      <span className="text-muted-foreground">Kassa: </span>
                      {o.registerName}
                    </span>
                  )}
                  <span>
                    <span className="text-muted-foreground">Tarix: </span>
                    {dateTime(o.dateUtc)}
                  </span>
                  <span className="text-muted-foreground">{PAYMENT_LABELS[o.method] ?? o.method}</span>
                </span>
                <b className={cn(o.kind === "in" ? "text-emerald-600" : "text-red-600", o.isReversed && "line-through")}>
                  {o.kind === "in" ? "+" : "−"}
                  {formatCurrency(o.amount)}
                </b>
              </div>
            </button>
          ))
        )}
      </div>

      {/* the order as a document */}
      <Dialog open={selected != null} onOpenChange={(o) => !o && setSelected(null)}>
        <DialogContent className="sm:max-w-lg">
          {selected && (
            <>
              <DialogHeader>
                <DialogTitle className="flex items-center justify-between gap-3 pr-6">
                  <span>{kindLabel(selected.kind)}</span>
                  <span className="font-mono text-base text-muted-foreground">{selected.documentNo}</span>
                </DialogTitle>
                <DialogDescription>{selected.kind === "in" ? "Alıcıdan ödəniş" : "Təchizatçıya ödəniş"}</DialogDescription>
              </DialogHeader>
              <dl className="grid grid-cols-[8rem_1fr] gap-x-3 gap-y-2 text-sm">
                <dt className="text-muted-foreground">Tarix</dt>
                <dd>{dateTime(selected.dateUtc)}</dd>
                {selected.registerName && (
                  <>
                    <dt className="text-muted-foreground">Kassa</dt>
                    <dd>{selected.registerName}</dd>
                  </>
                )}
                <dt className="text-muted-foreground">{selected.kind === "in" ? "Alıcı" : "Təchizatçı"}</dt>
                <dd>
                  {selected.partyName}
                  {selected.partyCode && <span className="ml-1 text-muted-foreground">({selected.partyCode})</span>}
                </dd>
                <dt className="text-muted-foreground">Məbləğ</dt>
                <dd className="text-lg font-semibold">{formatCurrency(selected.amount)}</dd>
                <dt className="text-muted-foreground">Ödəniş növü</dt>
                <dd>{PAYMENT_LABELS[selected.method] ?? selected.method}</dd>
                <dt className="text-muted-foreground">Əsas</dt>
                <dd>{selected.basis ?? "—"}</dd>
                <dt className="text-muted-foreground">Qeyd</dt>
                <dd>{selected.note ?? "—"}</dd>
                <dt className="text-muted-foreground">Müəllif</dt>
                <dd>{selected.authorName ?? "—"}</dd>
              </dl>
              {selected.isReversed && (
                <p className="rounded-md bg-red-50 p-2 text-sm text-red-700 dark:bg-red-950/30 dark:text-red-300">
                  Ləğv edilib{selected.reversedAtUtc ? `: ${dateTime(selected.reversedAtUtc)}` : ""}
                  {selected.reverseReason ? ` — ${selected.reverseReason}` : ""}
                </p>
              )}
              <DialogFooter>
                <Button
                  variant="outline"
                  onClick={() => {
                    if (!printCashOrder(selected)) toast.error("Çap pəncərəsi açılmadı — popup bloklamasını söndürün.");
                  }}
                >
                  <Printer className="mr-1 h-4 w-4" />
                  Çap forması
                </Button>
                {!selected.isReversed && (selected.kind === "in" ? canIn : canOut) && (
                  <Button variant="destructive" onClick={() => setReverseFor(selected)}>
                    Ləğv et
                  </Button>
                )}
              </DialogFooter>
            </>
          )}
        </DialogContent>
      </Dialog>

      <ReversePaymentDialog
        open={reverseFor != null}
        title={reverseFor ? `${kindLabel(reverseFor.kind)} ${reverseFor.documentNo}` : ""}
        onClose={() => setReverseFor(null)}
        onConfirm={reverse}
      />

      {/* a new order */}
      <Dialog open={createKind != null} onOpenChange={(o) => !o && !saving && setCreateKind(null)}>
        <DialogContent className="sm:max-w-lg">
          <DialogHeader>
            <DialogTitle>{createKind === "in" ? "Kassa mədaxil orderi (yeni)" : "Kassa məxaric orderi (yeni)"}</DialogTitle>
            <DialogDescription>{createKind === "in" ? "Alıcıdan ödəniş" : "Təchizatçıya ödəniş"}</DialogDescription>
          </DialogHeader>
          <div className="space-y-3">
            {createKind === "in" && (
              <div className="space-y-1">
                <Label htmlFor="o-reg">Kassa</Label>
                <select id="o-reg" className={SELECT_CLASS} value={fRegister} onChange={(e) => setFRegister(e.target.value ? Number(e.target.value) : "")}>
                  <option value="">Seçin</option>
                  {registers
                    .filter((r) => r.isActive)
                    .map((r) => (
                      <option key={r.id} value={r.id}>
                        {r.name}
                      </option>
                    ))}
                </select>
                <p className="text-xs text-muted-foreground">Pul həmin kassanın açıq növbəsinə düşür; növbə açıq deyilsə order yazılmır.</p>
              </div>
            )}
            <div className="space-y-1">
              <Label htmlFor="o-party">{createKind === "in" ? "Alıcı" : "Təchizatçı"}</Label>
              <Input id="o-party" placeholder="Axtar: ad, kod, telefon…" value={fPartyTerm} onChange={(e) => setFPartyTerm(e.target.value)} />
              <select id="o-party-sel" size={5} className="w-full rounded-md border border-input bg-background px-2 py-1 text-sm" value={fParty} onChange={(e) => setFParty(e.target.value ? Number(e.target.value) : "")}>
                {partyOptions.map((p) => (
                  <option key={p.id} value={p.id}>
                    {p.name}
                  </option>
                ))}
              </select>
              {createKind === "in" && chosen && (
                <p className="text-sm">
                  Müştəri borcu: <b className={cn((chosen.debt ?? 0) > 0 && "text-red-600")}>{formatCurrency(chosen.debt ?? 0)}</b>
                </p>
              )}
            </div>
            <div className="grid grid-cols-2 gap-3">
              <div className="space-y-1">
                <Label htmlFor="o-amount">Məbləğ</Label>
                <Input id="o-amount" inputMode="decimal" value={fAmount} onChange={(e) => setFAmount(e.target.value)} />
              </div>
              <div className="space-y-1">
                <Label htmlFor="o-method">Ödəniş növü</Label>
                <select id="o-method" className={SELECT_CLASS} value={fMethod} onChange={(e) => setFMethod(Number(e.target.value))}>
                  <option value={StorePaymentMethod.Cash}>Nağd</option>
                  <option value={StorePaymentMethod.Card}>Kart</option>
                  {createKind === "out" && <option value={StorePaymentMethod.BankTransfer}>Bank köçürməsi</option>}
                </select>
              </div>
            </div>
            <div className="space-y-1">
              <Label htmlFor="o-basis">Əsas</Label>
              <Input id="o-basis" value={fBasis} onChange={(e) => setFBasis(e.target.value)} placeholder="Məsələn: qaimə №, razılaşma" />
            </div>
            <div className="space-y-1">
              <Label htmlFor="o-note">Qeyd</Label>
              <Input id="o-note" value={fNote} onChange={(e) => setFNote(e.target.value)} />
            </div>
          </div>
          <DialogFooter>
            <Button variant="outline" disabled={saving} onClick={() => setCreateKind(null)}>
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
    </div>
  );
}
