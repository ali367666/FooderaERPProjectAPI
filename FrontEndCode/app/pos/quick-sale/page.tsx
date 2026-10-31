"use client";

import { useCallback, useEffect, useMemo, useState } from "react";
import { useRouter } from "next/navigation";
import { toast } from "sonner";
import { ArrowLeft, Banknote, CreditCard, Minus, Plus, Printer, Scale, Search, Trash2, X } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { cn } from "@/lib/utils";
import { BankTiles } from "@/components/pos/bank-picker";
import {
  createQuickSale,
  getPaymentBanks,
  printKitchenTicket,
  type OrderReceiptDto,
  type PaymentBank,
} from "@/lib/services/order-service";
import { getMenuCategories, type MenuCategory } from "@/lib/services/menu-category-service";
import {
  getMenuItems,
  getMenuItemAvailability,
  UnitOfMeasure,
  type MenuItem,
} from "@/lib/services/menu-item-service";
import { getPrinters, printImageToPrinter, type Printer as PrinterProfile } from "@/lib/services/printer-service";
import { canvasToRaster, designFromBranding, renderReceipt } from "@/lib/receipt-render";
import { ReceiptPreview } from "@/components/receipt-preview";
import { getPosTerminalContext } from "@/lib/pos-terminal-client";
import { escPosBarcode, receiptBarcodeValue } from "@/lib/receipt-barcode";
import { BarcodeSvg } from "@/components/barcode-svg";
import { useFiscalMode } from "@/lib/pos-fiscal-mode";
import { TouchNumpad } from "@/components/pos/touch-numpad";
import { getCurrentEmployeeId } from "@/lib/pos-session";
import { useHasPermission } from "@/hooks/use-auth-permissions";
import {
  getCompanySettingsBranding,
  type CompanySettingsBranding,
} from "@/lib/services/company-settings-service";

const r2 = (n: number) => Math.round((n + Number.EPSILON) * 100) / 100;

/** Lower-cases and folds Azerbaijani letters so typing "aci" finds "Acı", "qehve" finds "Qəhvə". */
function normalizeSearch(s: string): string {
  let lower: string;
  try {
    lower = s.toLocaleLowerCase("az");
  } catch {
    lower = s.toLowerCase();
  }
  return lower
    .replace(/ı/g, "i")
    .replace(/ə/g, "e")
    .replace(/ö/g, "o")
    .replace(/ü/g, "u")
    .replace(/ş/g, "s")
    .replace(/ç/g, "c")
    .replace(/ğ/g, "g");
}

const isWeightBased = (item: MenuItem) =>
  item.unitId === UnitOfMeasure.Kg || item.unitId === UnitOfMeasure.Gram;

/** Price shown on the screen — the server prices the sale itself, this only mirrors the product tile. */
const unitPriceOf = (item: MenuItem) => item.stationPrice ?? item.price;

/** What one cart line costs; weight-sold Kg products carry grams. */
const lineTotalOf = (item: MenuItem, quantity: number) =>
  r2(item.unitId === UnitOfMeasure.Kg ? (unitPriceOf(item) * quantity) / 1000 : unitPriceOf(item) * quantity);

/**
 * "Tez satış" — a take-away sale on its own light screen. Nothing is saved while the cart is built; the
 * sale, its stock and its payment go to the server in one request, so there is no order to open, edit
 * line by line, or clean up, and no table data to load.
 */
export default function QuickSalePage() {
  const router = useRouter();
  const fiscal = useFiscalMode();

  const canCreate = useHasPermission("Orders.Create");
  const canPay = useHasPermission("Orders.Pay");
  const canSell = canCreate && canPay;
  const canPrintKitchen = useHasPermission("Printer.Print");

  const [loading, setLoading] = useState(true);
  const [categories, setCategories] = useState<MenuCategory[]>([]);
  const [items, setItems] = useState<MenuItem[]>([]);
  const [outOfStockIds, setOutOfStockIds] = useState<Set<number>>(new Set());
  const [printers, setPrinters] = useState<PrinterProfile[]>([]);
  const [branding, setBranding] = useState<CompanySettingsBranding | null>(null);

  const [activeCategoryId, setActiveCategoryId] = useState<number | null>(null);
  const [activeSubCategoryId, setActiveSubCategoryId] = useState<number | null>(null);
  const [search, setSearch] = useState("");

  // menuItemId → quantity (pieces, or grams for weight-sold products), in the order they were added.
  const [cart, setCart] = useState<[number, number][]>([]);

  const [weightItem, setWeightItem] = useState<MenuItem | null>(null);
  const [weightKgInput, setWeightKgInput] = useState("");

  // A card payment is recorded against a bank (the server requires it).
  const [banks, setBanks] = useState<PaymentBank[]>([]);
  const [bankPickOpen, setBankPickOpen] = useState(false);

  const [cashOpen, setCashOpen] = useState(false);
  const [cashInput, setCashInput] = useState("");
  const [paying, setPaying] = useState(false);

  const [receipt, setReceipt] = useState<OrderReceiptDto | null>(null);
  const [soldOrderId, setSoldOrderId] = useState<number | null>(null);
  const [printingId, setPrintingId] = useState<number | null>(null);

  const touchScreen = branding?.touchScreenMode === true;
  const cashEnabled = branding?.paymentCashEnabled !== false;
  const cardEnabled = branding?.paymentCardEnabled !== false;
  const receiptDesign = useMemo(() => designFromBranding(branding), [branding]);

  // The menu, the stock flags, the printers and the branding come together; the tables, reservations,
  // halls and employees the table order screen needs are never touched.
  const load = useCallback(async () => {
    setLoading(true);
    const terminal = getPosTerminalContext();
    try {
      const [cats, mi, brand, avail, printerList, bankList] = await Promise.all([
        getMenuCategories(),
        getMenuItems(),
        terminal ? getCompanySettingsBranding(terminal.companyId).catch(() => null) : Promise.resolve(null),
        terminal?.restaurantId ? getMenuItemAvailability(terminal.restaurantId).catch(() => new Set<number>()) : Promise.resolve(new Set<number>()),
        terminal?.restaurantId ? getPrinters(terminal.restaurantId).catch(() => [] as PrinterProfile[]) : Promise.resolve([] as PrinterProfile[]),
        getPaymentBanks().catch(() => [] as PaymentBank[]),
      ]);
      const activeCats = cats.filter((c) => c.isActive);
      setCategories(activeCats);
      setItems(mi.filter((i) => i.isActive && !i.isTimeBased));
      setBranding(brand);
      setOutOfStockIds(avail);
      setPrinters(printerList.filter((p) => p.isActive));
      setBanks(bankList);
      setActiveCategoryId((prev) => prev ?? activeCats.find((c) => c.parentCategoryId == null)?.id ?? null);
    } catch (err) {
      toast.error(err instanceof Error ? err.message : "Menyu yüklənmədi");
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    void load();
  }, [load]);

  const itemById = useMemo(() => new Map(items.map((i) => [i.id, i])), [items]);

  const topLevelCategories = useMemo(() => categories.filter((c) => c.parentCategoryId == null), [categories]);
  const subCategories = useMemo(
    () =>
      activeCategoryId == null
        ? []
        : categories
            .filter((c) => c.parentCategoryId === activeCategoryId)
            .filter((c) => items.some((i) => i.menuCategoryId === c.id)),
    [categories, items, activeCategoryId],
  );

  const searchTerm = search.trim();
  const visibleItems = useMemo(() => {
    if (searchTerm) {
      const term = normalizeSearch(searchTerm);
      return items.filter((i) => {
        if (i.hideFromPosSearch) return false;
        const codeMatch =
          !i.hideBarcode && (i.barcode === searchTerm || (i.weightCode != null && i.weightCode === searchTerm));
        return codeMatch || normalizeSearch(i.name).includes(term);
      });
    }
    const categoryId = activeSubCategoryId ?? activeCategoryId;
    return categoryId == null ? [] : items.filter((i) => i.menuCategoryId === categoryId);
  }, [items, searchTerm, activeCategoryId, activeSubCategoryId]);

  const addToCart = (item: MenuItem, quantity: number) => {
    setCart((prev) => {
      const existing = prev.find(([id]) => id === item.id);
      if (existing) return prev.map(([id, q]) => (id === item.id ? [id, q + quantity] : [id, q]));
      return [...prev, [item.id, quantity]];
    });
  };

  const setQuantity = (menuItemId: number, quantity: number) => {
    setCart((prev) =>
      quantity <= 0
        ? prev.filter(([id]) => id !== menuItemId)
        : prev.map(([id, q]) => (id === menuItemId ? [id, quantity] : [id, q])),
    );
  };

  const handleItemClick = (item: MenuItem) => {
    if (outOfStockIds.has(item.id)) return;
    // A SET with choice groups is filled on the table order screen.
    if (item.isSet && item.setGroups.length > 0) {
      toast.error("Seçimli set tez satışda satılmır — masa sifarişindən istifadə edin.");
      return;
    }
    if (isWeightBased(item)) {
      if (branding?.posShowWeighWindow === false) {
        addToCart(item, 1000);
        return;
      }
      setWeightItem(item);
      setWeightKgInput("");
      return;
    }
    addToCart(item, 1);
  };

  const handleSearchEnter = (e: React.KeyboardEvent<HTMLInputElement>) => {
    if (e.key !== "Enter" || !searchTerm) return;
    const exact = items.find((i) => !i.hideBarcode && (i.barcode === searchTerm || i.weightCode === searchTerm));
    if (exact) {
      handleItemClick(exact);
      setSearch("");
    }
  };

  const handleConfirmWeight = () => {
    if (!weightItem) return;
    const kg = Number(weightKgInput.replace(",", "."));
    if (!Number.isFinite(kg) || kg <= 0) {
      toast.error("Çəki düzgün deyil");
      return;
    }
    addToCart(weightItem, Math.round(kg * 1000));
    setWeightItem(null);
    setWeightKgInput("");
  };

  const cartLines = useMemo(
    () =>
      cart
        .map(([id, quantity]) => ({ item: itemById.get(id), quantity }))
        .filter((l): l is { item: MenuItem; quantity: number } => l.item !== undefined),
    [cart, itemById],
  );
  const total = r2(cartLines.reduce((sum, l) => sum + lineTotalOf(l.item, l.quantity), 0));

  const sortedPrinters = useMemo(
    () => [...printers].sort((a, b) => Number(b.isPrimary) - Number(a.isPrimary)),
    [printers],
  );

  const receiptCategoryName = (id: number | null | undefined) =>
    id == null ? "" : categories.find((c) => c.id === id)?.name ?? "";

  const printReceiptTo = async (printer: PrinterProfile, target: OrderReceiptDto, orderId: number) => {
    setPrintingId(printer.id);
    try {
      const canvas = await renderReceipt(target, receiptDesign, receiptCategoryName);
      await printImageToPrinter(printer.id, canvasToRaster(canvas), `\n${escPosBarcode(receiptBarcodeValue(orderId))}`);
      toast.success(`${printer.name}-ə göndərildi`);
    } catch (err) {
      toast.error(err instanceof Error ? err.message : "Printerə qoşulmaq mümkün olmadı");
    } finally {
      setPrintingId(null);
    }
  };

  const sell = async (method: "Cash" | "Card", paidAmount: number, bankId: number | null = null) => {
    if (paying || cartLines.length === 0) return;
    const terminal = getPosTerminalContext();
    if (!terminal?.restaurantId) {
      toast.error("Terminal filiala bağlı deyil.");
      return;
    }
    setPaying(true);
    try {
      const waiterId = await getCurrentEmployeeId();
      if (!waiterId) {
        toast.error("Bu istifadəçi heç bir işçiyə bağlı deyil. Users səhifəsindən bağlayın.");
        return;
      }
      const sold = cartLines;
      const result = await createQuickSale({
        restaurantId: terminal.restaurantId,
        waiterId,
        items: sold.map((l) => ({ menuItemId: l.item.id, quantity: l.quantity })),
        paymentMethod: method,
        paidAmount,
        isFiscal: fiscal,
        bankId,
      });
      setCashOpen(false);
      setBankPickOpen(false);
      setCart([]);
      setReceipt(result.receipt);
      setSoldOrderId(result.orderId);
      void getMenuItemAvailability(terminal.restaurantId).then(setOutOfStockIds).catch(() => undefined);

      // A printer problem never undoes the sale — it is reported and the receipt window can retry.
      const primary = printers.find((p) => p.isPrimary);
      if (branding?.printAutoOnPayment === true && primary) {
        void printReceiptTo(primary, result.receipt, result.orderId);
      }
      if (branding?.printKitchenOnPayment === true && canPrintKitchen) {
        const printerIds = new Set<number>();
        for (const l of sold) {
          const printerId = l.item.isSet ? l.item.setPrinterId ?? l.item.printerId : l.item.printerId;
          if (printerId && printers.some((p) => p.id === printerId)) printerIds.add(printerId);
        }
        for (const printerId of printerIds) {
          try {
            await printKitchenTicket(result.orderId, printerId);
          } catch (err) {
            toast.error(err instanceof Error ? err.message : "Mətbəx çapı göndərilmədi");
          }
        }
      }
    } catch (err) {
      toast.error(err instanceof Error ? err.message : "Satış tamamlanmadı");
    } finally {
      setPaying(false);
    }
  };

  const openCash = () => {
    if (cartLines.length === 0) return;
    setCashInput(total.toFixed(2));
    setCashOpen(true);
  };

  // One bank needs no question; several are picked from a list.
  const payByCard = () => {
    if (cartLines.length === 0) return;
    if (banks.length === 0) {
      toast.error("Kart ödənişi üçün bank tapılmadı.");
      return;
    }
    if (banks.length === 1) {
      void sell("Card", total, banks[0].id);
      return;
    }
    setBankPickOpen(true);
  };

  const cashAmount = r2(Number(cashInput.replace(",", ".")) || 0);
  const change = Math.max(0, r2(cashAmount - total));

  const closeReceipt = () => {
    setReceipt(null);
    setSoldOrderId(null);
  };

  if (!canSell) {
    return (
      <div className="flex h-full flex-col items-center justify-center gap-3 p-6 text-center">
        <p className="text-muted-foreground">Tez satış üçün icazəniz yoxdur.</p>
        <Button variant="outline" onClick={() => router.push("/pos")}>
          Geri
        </Button>
      </div>
    );
  }

  return (
    <div className="flex h-full min-h-0 flex-col gap-3 p-3 lg:flex-row">
      {/* Products */}
      <div className="flex min-h-0 min-w-0 flex-1 flex-col gap-3">
        <div className="flex items-center gap-2">
          <Button variant="outline" size="icon" onClick={() => router.push("/pos")} aria-label="Geri">
            <ArrowLeft className="h-4 w-4" />
          </Button>
          <h1 className="text-lg font-semibold">Tez satış</h1>
          <div className="relative ml-auto w-full max-w-xs">
            <Search className="pointer-events-none absolute left-2.5 top-2.5 h-4 w-4 text-muted-foreground" />
            <Input
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              onKeyDown={handleSearchEnter}
              placeholder="Məhsul və ya barkod"
              className="pl-8 pr-8"
            />
            {search && (
              <button
                type="button"
                onClick={() => setSearch("")}
                className="absolute right-2 top-2.5 text-muted-foreground hover:text-foreground"
                aria-label="Təmizlə"
              >
                <X className="h-4 w-4" />
              </button>
            )}
          </div>
        </div>

        {!searchTerm && (
          <div className="flex flex-wrap gap-2">
            {topLevelCategories.map((c) => (
              <Button
                key={c.id}
                variant={activeCategoryId === c.id ? "default" : "outline"}
                onClick={() => {
                  setActiveCategoryId(c.id);
                  setActiveSubCategoryId(null);
                }}
              >
                {c.name}
              </Button>
            ))}
          </div>
        )}
        {!searchTerm && subCategories.length > 0 && (
          <div className="flex flex-wrap gap-2">
            {subCategories.map((c) => (
              <Button
                key={c.id}
                size="sm"
                variant={activeSubCategoryId === c.id ? "default" : "secondary"}
                onClick={() => setActiveSubCategoryId(activeSubCategoryId === c.id ? null : c.id)}
              >
                {c.name}
              </Button>
            ))}
          </div>
        )}

        <div className="min-h-0 flex-1 overflow-y-auto">
          {loading ? (
            <p className="py-10 text-center text-sm text-muted-foreground">Yüklənir…</p>
          ) : visibleItems.length === 0 ? (
            <p className="py-10 text-center text-sm text-muted-foreground">Məhsul tapılmadı</p>
          ) : (
            <div className="grid grid-cols-2 gap-2 sm:grid-cols-3 xl:grid-cols-4">
              {visibleItems.map((item) => {
                const outOfStock = outOfStockIds.has(item.id);
                return (
                  <button
                    key={item.id}
                    type="button"
                    disabled={outOfStock}
                    onClick={() => handleItemClick(item)}
                    className={cn(
                      "relative flex h-28 select-none flex-col overflow-hidden rounded-xl border-2 border-border bg-card text-left shadow-sm transition-all",
                      "hover:border-primary/50 active:scale-95 disabled:opacity-50",
                      outOfStock && "bg-muted grayscale",
                    )}
                  >
                    {item.imageUrl && (
                      // eslint-disable-next-line @next/next/no-img-element
                      <img src={item.imageUrl} alt="" className="h-12 w-full shrink-0 object-cover" loading="lazy" />
                    )}
                    <div className="flex min-h-0 flex-1 flex-col justify-between gap-1 p-2.5">
                      <span
                        className={cn(
                          "font-semibold leading-tight",
                          item.imageUrl ? "line-clamp-1 text-sm" : "line-clamp-2 text-base",
                        )}
                      >
                        {item.name}
                      </span>
                      <span className="text-sm font-bold tabular-nums text-primary">
                        {unitPriceOf(item).toFixed(2)} ₼
                      </span>
                    </div>
                    {isWeightBased(item) && (
                      <Scale className="absolute bottom-2.5 right-2 h-4 w-4 text-violet-700" aria-label="Çəki ilə satılır" />
                    )}
                    {outOfStock && (
                      <span className="absolute right-1 top-1 rounded-full bg-destructive px-1.5 py-0.5 text-[10px] font-semibold text-destructive-foreground">
                        Bitib
                      </span>
                    )}
                  </button>
                );
              })}
            </div>
          )}
        </div>
      </div>

      {/* Cart */}
      <div className="flex min-h-0 w-full flex-col rounded-xl border bg-card lg:w-96">
        <div className="flex items-center justify-between border-b px-4 py-3">
          <span className="font-semibold">Səbət</span>
          {cartLines.length > 0 && (
            <Button variant="ghost" size="sm" onClick={() => setCart([])}>
              <Trash2 className="mr-1 h-4 w-4" />
              Təmizlə
            </Button>
          )}
        </div>

        <div className="min-h-32 flex-1 space-y-2 overflow-y-auto p-3">
          {cartLines.length === 0 ? (
            <p className="py-8 text-center text-sm text-muted-foreground">Məhsul seçin</p>
          ) : (
            cartLines.map(({ item, quantity }) => {
              const weight = isWeightBased(item);
              return (
                <div key={item.id} className="flex items-center gap-2 rounded-lg border bg-background p-2">
                  <div className="min-w-0 flex-1">
                    <p className="truncate text-sm font-medium">{item.name}</p>
                    <p className="text-xs text-muted-foreground tabular-nums">
                      {weight ? `${(quantity / 1000).toFixed(3)} kq` : `${quantity} × ${unitPriceOf(item).toFixed(2)} ₼`}
                    </p>
                  </div>
                  {!weight && (
                    <div className="flex items-center gap-1">
                      <Button
                        variant="outline"
                        size="icon"
                        className="h-8 w-8"
                        onClick={() => setQuantity(item.id, quantity - 1)}
                        aria-label="Azalt"
                      >
                        <Minus className="h-4 w-4" />
                      </Button>
                      <Button
                        variant="outline"
                        size="icon"
                        className="h-8 w-8"
                        onClick={() => setQuantity(item.id, quantity + 1)}
                        aria-label="Artır"
                      >
                        <Plus className="h-4 w-4" />
                      </Button>
                    </div>
                  )}
                  <span className="w-16 text-right text-sm font-semibold tabular-nums">
                    {lineTotalOf(item, quantity).toFixed(2)}
                  </span>
                  <button
                    type="button"
                    onClick={() => setQuantity(item.id, 0)}
                    className="text-muted-foreground hover:text-destructive"
                    aria-label="Sil"
                  >
                    <X className="h-4 w-4" />
                  </button>
                </div>
              );
            })
          )}
        </div>

        <div className="space-y-3 border-t p-4">
          <div className="flex items-baseline justify-between">
            <span className="text-muted-foreground">Cəmi</span>
            <span className="text-2xl font-bold tabular-nums">{total.toFixed(2)} ₼</span>
          </div>
          <div className="grid grid-cols-2 gap-2">
            {cashEnabled && (
              <Button className="h-14 text-base" disabled={cartLines.length === 0 || paying} onClick={openCash}>
                <Banknote className="mr-2 h-5 w-5" />
                Nağd
              </Button>
            )}
            {cardEnabled && (
              <Button
                className="h-14 text-base"
                variant="outline"
                disabled={cartLines.length === 0 || paying}
                onClick={payByCard}
              >
                <CreditCard className="mr-2 h-5 w-5" />
                {paying ? "Gözləyin…" : "Kart"}
              </Button>
            )}
          </div>
        </div>
      </div>

      {/* Weight */}
      <Dialog open={weightItem !== null} onOpenChange={(o) => !o && setWeightItem(null)}>
        <DialogContent className="sm:max-w-sm">
          <DialogHeader>
            <DialogTitle>{weightItem?.name}</DialogTitle>
            <DialogDescription>
              Çəkini kiloqramla daxil edin
              {weightItem && <> — {unitPriceOf(weightItem).toFixed(2)} ₼/kq</>}
            </DialogDescription>
          </DialogHeader>
          <div className="space-y-3">
            <Input
              autoFocus
              inputMode="decimal"
              value={weightKgInput}
              onChange={(e) => setWeightKgInput(e.target.value)}
              onKeyDown={(e) => e.key === "Enter" && handleConfirmWeight()}
              placeholder="0.000"
            />
            {touchScreen && <TouchNumpad value={weightKgInput} onChange={setWeightKgInput} allowDecimal />}
            {weightItem && (
              <p className="text-sm text-muted-foreground">
                Cəm: {r2(unitPriceOf(weightItem) * (Number(weightKgInput.replace(",", ".")) || 0)).toFixed(2)} ₼
              </p>
            )}
          </div>
          <DialogFooter>
            <Button variant="outline" onClick={() => setWeightItem(null)}>
              Ləğv et
            </Button>
            <Button onClick={handleConfirmWeight}>Əlavə et</Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* Cash */}
      <Dialog open={cashOpen} onOpenChange={(o) => !paying && setCashOpen(o)}>
        <DialogContent className="max-h-[90vh] overflow-y-auto sm:max-w-sm">
          <DialogHeader>
            <DialogTitle>Nağd ödəniş</DialogTitle>
            <DialogDescription>Ödəniləcək: {total.toFixed(2)} ₼</DialogDescription>
          </DialogHeader>
          <div className="space-y-3">
            <div className="space-y-1">
              <Label htmlFor="quick-cash">Alınan məbləğ</Label>
              <Input
                id="quick-cash"
                type="number"
                step="0.01"
                autoFocus
                value={cashInput}
                onChange={(e) => setCashInput(e.target.value)}
                onKeyDown={(e) => e.key === "Enter" && cashAmount >= total && void sell("Cash", cashAmount)}
              />
              {touchScreen && <TouchNumpad value={cashInput} onChange={setCashInput} allowDecimal />}
            </div>
            <div className="rounded-lg bg-muted/50 p-3 text-sm">
              <p className="font-medium">Qaytarılan: {change.toFixed(2)} ₼</p>
            </div>
          </div>
          <DialogFooter>
            <Button variant="outline" disabled={paying} onClick={() => setCashOpen(false)}>
              Geri
            </Button>
            <Button disabled={paying || cashAmount < total} onClick={() => void sell("Cash", cashAmount)}>
              {paying ? "Gözləyin…" : "Ödə"}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* Card: bank */}
      <Dialog open={bankPickOpen} onOpenChange={(o) => !paying && setBankPickOpen(o)}>
        <DialogContent className="sm:max-w-sm">
          <DialogHeader>
            <DialogTitle>Kartla ödəniş</DialogTitle>
            <DialogDescription>Ödəniləcək: {total.toFixed(2)} ₼ — bankı seçin</DialogDescription>
          </DialogHeader>
          <BankTiles banks={banks} onPick={(bankId) => void sell("Card", total, bankId)} disabled={paying} />
          <DialogFooter>
            <Button variant="outline" disabled={paying} onClick={() => setBankPickOpen(false)}>
              Geri
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* Receipt */}
      <Dialog open={receipt !== null} onOpenChange={(o) => !o && closeReceipt()}>
        <DialogContent className="sm:max-w-sm">
          <div
            id="receipt-print-area"
            style={branding?.receiptFontSize ? { fontSize: `${branding.receiptFontSize}px` } : undefined}
          >
            <DialogHeader className="sr-only">
              <DialogTitle>Qəbz</DialogTitle>
              <DialogDescription>Tez satış</DialogDescription>
            </DialogHeader>
            <div className="space-y-1 text-sm">
              {receipt && (
                <ReceiptPreview receipt={receipt} design={receiptDesign} categoryName={receiptCategoryName} />
              )}
              {soldOrderId != null && receipt && (
                <div className="mt-3 flex justify-center">
                  <BarcodeSvg value={receiptBarcodeValue(soldOrderId)} height={40} />
                </div>
              )}
            </div>
          </div>
          <DialogFooter className="flex-col gap-2 sm:flex-col">
            <Button variant="outline" className="w-full" onClick={() => window.print()}>
              <Printer className="mr-2 h-4 w-4" />
              Qəbzi çap et
            </Button>
            {receipt &&
              soldOrderId != null &&
              sortedPrinters.map((printer) => (
                <Button
                  key={printer.id}
                  variant={printer.isPrimary ? "default" : "outline"}
                  className="w-full"
                  disabled={printingId === printer.id}
                  onClick={() => void printReceiptTo(printer, receipt, soldOrderId)}
                >
                  <Printer className="mr-2 h-4 w-4" />
                  {printer.name}-ə göndər
                  {printer.isPrimary && " (əsas)"}
                </Button>
              ))}
            <Button className="w-full" onClick={closeReceipt}>
              Yeni satış
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}
