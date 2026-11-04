"use client";

import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import { Banknote, ChevronDown, ChevronLeft, ChevronRight, Search, ChevronUp, CreditCard, Lock, Minus, Pause, Play, Plus, Printer, ScanBarcode, Tag, Trash2, Undo2, UserRound, Wallet, X } from "lucide-react";
import { toast } from "sonner";
import { useStoreApproval } from "@/components/store/approval";
import { ShiftReportView } from "@/components/store/shift-report-view";
import { Button } from "@/components/ui/button";
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from "@/components/ui/dialog";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { useStorePermissions as usePermissionSet } from "@/lib/store/use-store-permissions";
import { formatCurrency } from "@/lib/format-currency";
import { cn } from "@/lib/utils";
import { useRouter } from "next/navigation";
import { getPosTerminalContext } from "@/lib/pos-terminal-client";
import { printStoreSale } from "@/lib/store/store-receipt";
import { enqueueSale, syncQueuedSales } from "@/lib/store/offline-queue";
import { refreshOfflineCatalog } from "@/lib/store/offline-catalog";
import {
  StoreCashMovementType,
  StorePaymentMethod,
  UNIT_LABELS,
  UnitOfMeasure,
  addStoreCashMovement,
  closeStoreShift,
  createStoreSale,
  StoreOfflineError,
  discardStoreHeldSale,
  findStoreCustomer,
  checkStoreApproval,
  findStoreProductByCode,
  getStoreSettings,
  getMyStoreShift,
  getStoreCategories,
  getStoreHeldSales,
  getStoreProduct,
  getStoreQuickSlots,
  getStoreRegisters,
  getStoreShiftReport,
  holdStoreSale,
  openStoreShift,
  posSearchProducts,
  resumeStoreHeldSale,
  saveStoreCustomer,
  searchStoreCustomers,
  searchStoreSerials,
  type StoreCashRegister,
  type StoreCategory,
  type StoreCustomer,
  type StoreHeldSale,
  type StoreProduct,
  type StoreQuickSlot,
  type StoreSale,
  type StoreSerialUnit,
  type StoreShift,
  type StoreShiftReport,
} from "@/lib/store/store-service";

const r2 = (n: number) => Math.round((n + Number.EPSILON) * 100) / 100;
const num = (s: string) => Number(s.replace(",", ".")) || 0;

type CartLine = {
  product: StoreProduct;
  quantity: number;
  discount: number;
  /** The price the customer pays per unit; differs from the list price after a price change. */
  price: number;
  priceReason: string;
  serials: string[];
};

const lineGross = (l: CartLine) => r2(l.quantity * l.price);
const lineTotal = (l: CartLine) => r2(lineGross(l) - l.discount);

export default function StorePosPage() {
  const permissions = usePermissionSet();
  const canClose = permissions.has("StoreCash.Close");
  const canReceiptless = permissions.has("StoreSale.NoReceipt");
  const canOpen = permissions.has("StoreCash.Open");
  const approval = useStoreApproval();

  const [shift, setShift] = useState<StoreShift | null>(null);
  const [register, setRegister] = useState<StoreCashRegister | null>(null);
  const [registers, setRegisters] = useState<StoreCashRegister[]>([]);
  const router = useRouter();
  const [booting, setBooting] = useState(true);
  // The sale screen belongs to a set-up terminal (POS login), not to the admin dashboard.
  const [terminalState, setTerminalState] = useState<"checking" | "ready" | "missing">("checking");
  useEffect(() => {
    setTerminalState(getPosTerminalContext() ? "ready" : "missing");
  }, []);

  // Opening a shift
  const [pickRegisterId, setPickRegisterId] = useState<number | "">("");
  const [openingCash, setOpeningCash] = useState("0");
  const [opening, setOpening] = useState(false);

  // Catalog
  const [categories, setCategories] = useState<StoreCategory[]>([]);
  const [categoryId, setCategoryId] = useState<number | null>(null);
  const [term, setTerm] = useState("");
  const [products, setProducts] = useState<StoreProduct[]>([]);
  const [view, setView] = useState<"products" | "quick">("products");
  const [quickPage, setQuickPage] = useState(0);
  // The product list scrolls with the arrow buttons (large, for touch screens) as well as with the wheel.
  const listRef = useRef<HTMLDivElement>(null);
  // Search box right above the product list (the scan field at the top stays for barcodes).
  const [listFilter, setListFilter] = useState("");
  const scrollList = (direction: 1 | -1) =>
    listRef.current?.scrollBy({ top: direction * listRef.current.clientHeight * 0.85, behavior: "smooth" });
  const [slots, setSlots] = useState<StoreQuickSlot[]>([]);
  const scanRef = useRef<HTMLInputElement>(null);

  // Cart and payment
  const [cart, setCart] = useState<CartLine[]>([]);
  const [qtyFor, setQtyFor] = useState<StoreProduct | null>(null);
  const [qtyInput, setQtyInput] = useState("");
  const [serialFor, setSerialFor] = useState<StoreProduct | null>(null);
  const [serialOptions, setSerialOptions] = useState<StoreSerialUnit[]>([]);
  const [serialPicked, setSerialPicked] = useState<string[]>([]);
  const [serialTerm, setSerialTerm] = useState("");
  const [payOpen, setPayOpen] = useState(false);
  const [cashInput, setCashInput] = useState("");
  const [cardInput, setCardInput] = useState("");
  const [bonusInput, setBonusInput] = useState("");
  const [debtInput, setDebtInput] = useState("");
  const [paying, setPaying] = useState(false);
  const [receiptless, setReceiptless] = useState(false);
  const [allowReceiptless, setAllowReceiptless] = useState(false);
  const [bonusMaxPercent, setBonusMaxPercent] = useState(100);

  // Customer card
  const [customer, setCustomer] = useState<StoreCustomer | null>(null);
  const [customerOpen, setCustomerOpen] = useState(false);
  const [customerTerm, setCustomerTerm] = useState("");
  const [customerResults, setCustomerResults] = useState<StoreCustomer[]>([]);
  const [newCustomerName, setNewCustomerName] = useState("");
  const [newCustomerPhone, setNewCustomerPhone] = useState("");
  const [doneSale, setDoneSale] = useState<StoreSale | null>(null);
  const operation = useRef<{ id: string; signature: string } | null>(null);

  // Held sales
  const [heldOpen, setHeldOpen] = useState(false);
  const [held, setHeld] = useState<StoreHeldSale[]>([]);

  // Shift tools
  const [report, setReport] = useState<StoreShiftReport | null>(null);
  const [reportOpen, setReportOpen] = useState(false);
  const [closeOpen, setCloseOpen] = useState(false);
  const [actualCash, setActualCash] = useState("");
  const [closeNote, setCloseNote] = useState("");
  const [closing, setClosing] = useState(false);
  const [moveOpen, setMoveOpen] = useState(false);
  const [moveType, setMoveType] = useState<number>(StoreCashMovementType.In);
  const [moveAmount, setMoveAmount] = useState("");
  const [moveReason, setMoveReason] = useState("");

  const warehouseId = register?.warehouseId ?? null;

  useEffect(() => {
    getStoreSettings()
      .then((st) => {
        setAllowReceiptless(st.allowReceiptlessSale);
        setBonusMaxPercent(st.bonusMaxPaymentPercent);
      })
      .catch(() => undefined);
  }, []);

  // The till keeps its own copy of the catalog so it can sell when the connection drops.
  useEffect(() => {
    if (warehouseId == null) return;
    const refresh = () => {
      if (navigator.onLine) void refreshOfflineCatalog(warehouseId);
    };
    refresh();
    const timer = setInterval(refresh, 10 * 60_000);
    return () => clearInterval(timer);
  }, [warehouseId]);

  const boot = useCallback(async () => {
    setBooting(true);
    try {
      const [mine, regs] = await Promise.all([getMyStoreShift(), getStoreRegisters()]);
      setRegisters(regs);
      setShift(mine);
      setRegister(mine ? (regs.find((r) => r.id === mine.storeCashRegisterId) ?? null) : null);
    } catch (e) {
      toast.error(e instanceof Error ? e.message : "Yüklənmədi.");
    } finally {
      setBooting(false);
    }
  }, []);

  useEffect(() => {
    void boot();
    getStoreCategories().then((c) => setCategories(c.filter((x) => x.isActive))).catch(() => setCategories([]));
  }, [boot]);

  // Product list for the grid; typing a name filters it live.
  useEffect(() => {
    if (!shift) return;
    const t = setTimeout(() => {
      posSearchProducts({ q: listFilter.trim() || term.trim() || undefined, warehouseId, limit: 2000 })
        .then((r) => setProducts(r.items))
        .catch(() => setProducts([]));
    }, 200);
    return () => clearTimeout(t);
  }, [shift, term, listFilter, warehouseId]);

  useEffect(() => {
    if (!register || !shift) return;
    getStoreQuickSlots(register.id).then(setSlots).catch(() => setSlots([]));
  }, [register, shift]);

  const dialogOpen = customerOpen || payOpen || !!doneSale || !!qtyFor || !!serialFor || closeOpen || moveOpen || reportOpen || heldOpen;
  useEffect(() => {
    if (shift && !dialogOpen) scanRef.current?.focus();
  }, [shift, dialogOpen]);

  useEffect(() => {
    if (!customerOpen) return;
    const t = setTimeout(() => {
      searchStoreCustomers(customerTerm.trim())
        .then(setCustomerResults)
        .catch(() => setCustomerResults([]));
    }, 200);
    return () => clearTimeout(t);
  }, [customerOpen, customerTerm]);

  const total = useMemo(() => r2(cart.reduce((s, l) => s + lineTotal(l), 0)), [cart]);
  const discountTotal = useMemo(() => r2(cart.reduce((s, l) => s + l.discount, 0)), [cart]);

  const addToCart = useCallback((product: StoreProduct, quantity: number, serials: string[] = []) => {
    setCart((prev) => {
      const i = prev.findIndex((l) => l.product.id === product.id);
      if (i >= 0) {
        const next = [...prev];
        const merged = product.tracksSerial ? Array.from(new Set([...next[i].serials, ...serials])) : next[i].serials;
        next[i] = {
          ...next[i],
          serials: merged,
          quantity: product.tracksSerial ? merged.length : r2(next[i].quantity + quantity),
        };
        return next;
      }
      return [
        ...prev,
        {
          product,
          quantity: product.tracksSerial ? serials.length : quantity,
          discount: 0,
          price: product.effectivePrice,
          priceReason: "",
          serials,
        },
      ];
    });
  }, []);

  // Piece products go in at once; weighed ones ask for the amount; serial / IMEI ones ask which units.
  const pick = useCallback(
    async (product: StoreProduct) => {
      if (product.tracksSerial) {
        setSerialFor(product);
        setSerialPicked([]);
        setSerialTerm("");
        try {
          setSerialOptions(await searchStoreSerials({ productId: product.id, warehouseId, status: 1 }));
        } catch {
          setSerialOptions([]);
        }
        return;
      }
      if (product.unit === UnitOfMeasure.Piece) addToCart(product, 1);
      else {
        setQtyFor(product);
        setQtyInput("");
      }
    },
    [addToCart, warehouseId],
  );

  const onScan = async () => {
    const code = term.trim();
    if (!code) return;
    try {
      const product = await findStoreProductByCode(code, warehouseId);
      if (product.scannedQuantity && product.scannedQuantity > 0 && !product.tracksSerial) addToCart(product, product.scannedQuantity);
      else await pick(product);
      setTerm("");
    } catch {
      // Not a product barcode: it may be an IMEI/serial number typed or scanned directly.
      try {
        const units = await searchStoreSerials({ q: code, warehouseId, status: 1 });
        const unit = units.find((u) => u.serialNumber === code);
        if (unit) {
          const product = await getStoreProduct(unit.productId, warehouseId);
          addToCart(product, 1, [unit.serialNumber]);
          setTerm("");
          return;
        }
      } catch {
        // fall through
      }
      if (products.length === 1) {
        await pick(products[0]);
        setTerm("");
        return;
      }
      // Not a product either: a bonus / customer card scanned at the till selects its customer.
      try {
        const card = await findStoreCustomer(code);
        setCustomer(card);
        setTerm("");
        toast.success(`Müştəri: ${card.name}`);
      } catch {
        toast.error("Bu barkodla məhsul və ya müştəri kartı tapılmadı.");
      }
    }
  };

  // Taking something out of the cart needs the permission, or a manager's approval (name and password) on the spot.
  const canRemoveLine = permissions.has("StoreSale.RemoveLine");
  const requestRemoval = async (action: "RemoveLine" | "ClearCart", detail: string | null, doIt: () => void) => {
    if (canRemoveLine) {
      doIt();
      return;
    }
    try {
      await approval.run((appr) => checkStoreApproval(action, detail, appr ?? null));
      doIt();
    } catch (e) {
      if (e instanceof Error && e.message) toast.error(e.message);
    }
  };

  const setQuantity = (productId: number, quantity: number) =>
    setCart((prev) =>
      quantity <= 0
        ? prev.filter((l) => l.product.id !== productId)
        : prev.map((l) => (l.product.id === productId && !l.product.tracksSerial ? { ...l, quantity } : l)),
    );

  const updateLine = (productId: number, patch: Partial<CartLine>) =>
    setCart((prev) => prev.map((l) => (l.product.id === productId ? { ...l, ...patch } : l)));

  const removeSerial = (productId: number, serial: string) =>
    setCart((prev) =>
      prev
        .map((l) => {
          if (l.product.id !== productId) return l;
          const serials = l.serials.filter((s) => s !== serial);
          return { ...l, serials, quantity: serials.length };
        })
        .filter((l) => l.quantity > 0),
    );

  const clearCart = () => {
    setCart([]);
    operation.current = null;
  };

  const pickSlot = async (slot: StoreQuickSlot) => {
    if (!slot.productId) return;
    try {
      await pick(await getStoreProduct(slot.productId, warehouseId));
    } catch (e) {
      toast.error(e instanceof Error ? e.message : "Məhsul açılmadı.");
    }
  };

  // ---- Hold -------------------------------------------------------------------------------

  const holdCart = async () => {
    if (!register || cart.length === 0) return;
    try {
      await holdStoreSale({
        storeCashRegisterId: register.id,
        lines: cart.map((l) => ({ productId: l.product.id, quantity: l.quantity, discountAmount: l.discount, serialNumbers: l.serials })),
      });
      clearCart();
      toast.success("Satış saxlanıldı");
    } catch (e) {
      toast.error(e instanceof Error ? e.message : "Satış saxlanmadı.");
    }
  };

  const openHeld = async () => {
    if (!register) return;
    try {
      setHeld(await getStoreHeldSales(register.id));
      setHeldOpen(true);
    } catch (e) {
      toast.error(e instanceof Error ? e.message : "Saxlanılan satışlar yüklənmədi.");
    }
  };

  const resumeHeld = async (h: StoreHeldSale) => {
    if (cart.length > 0) {
      toast.error("Əvvəlcə cari səbəti tamamlayın, saxlayın və ya təmizləyin.");
      return;
    }
    try {
      const taken = await resumeStoreHeldSale(h.id);
      const loaded: CartLine[] = [];
      for (const line of taken.lines) {
        const product = await getStoreProduct(line.productId, warehouseId);
        const serials = line.serialNumbers ?? [];
        loaded.push({
          product,
          quantity: product.tracksSerial ? serials.length : line.quantity,
          discount: line.discountAmount,
          price: product.effectivePrice,
          priceReason: "",
          serials,
        });
      }
      setCart(loaded);
      setHeldOpen(false);
    } catch (e) {
      toast.error(e instanceof Error ? e.message : "Saxlanılan satış açılmadı.");
    }
  };

  // ---- Shift ------------------------------------------------------------------------------

  const doOpenShift = async () => {
    if (pickRegisterId === "") {
      toast.error("Kassanı seçin.");
      return;
    }
    setOpening(true);
    try {
      await openStoreShift(pickRegisterId, num(openingCash));
      toast.success("Növbə açıldı");
      await boot();
    } catch (e) {
      toast.error(e instanceof Error ? e.message : "Növbə açılmadı.");
    } finally {
      setOpening(false);
    }
  };

  const showReport = async () => {
    if (!shift) return;
    try {
      setReport(await getStoreShiftReport(shift.id));
      setReportOpen(true);
    } catch (e) {
      toast.error(e instanceof Error ? e.message : "Hesabat yüklənmədi.");
    }
  };

  const startClose = async () => {
    if (cart.length > 0) {
      toast.error("Əvvəlcə açıq səbəti tamamlayın və ya təmizləyin.");
      return;
    }
    await showReport();
    setReportOpen(false);
    setActualCash("");
    setCloseNote("");
    setCloseOpen(true);
  };

  const doClose = async () => {
    if (!shift) return;
    if (actualCash.trim() === "") {
      toast.error("Kassadakı faktiki nağd məbləği yazın.");
      return;
    }
    setClosing(true);
    try {
      const final = await closeStoreShift(shift.id, num(actualCash), closeNote.trim() || null);
      setCloseOpen(false);
      setReport(final);
      setReportOpen(true);
      setShift(null);
      setRegister(null);
      toast.success("Növbə bağlandı");
      void boot();
    } catch (e) {
      toast.error(e instanceof Error ? e.message : "Növbə bağlanmadı.");
    } finally {
      setClosing(false);
    }
  };

  const doMovement = async () => {
    if (!shift) return;
    if (num(moveAmount) <= 0 || !moveReason.trim()) {
      toast.error("Məbləğ və səbəbi yazın.");
      return;
    }
    try {
      // The cashier without the permission for this kind of movement is asked for a manager's name and password.
      await approval.run((appr) => addStoreCashMovement(shift.id, { type: moveType, amount: num(moveAmount), reason: moveReason.trim(), approval: appr ?? null }));
      toast.success(moveType === StoreCashMovementType.In ? "Mədaxil yazıldı" : "Məxaric yazıldı");
      setMoveOpen(false);
      setMoveAmount("");
      setMoveReason("");
    } catch (e) {
      toast.error(e instanceof Error ? e.message : "Yazılmadı.");
    }
  };

  // ---- Payment ----------------------------------------------------------------------------

  const openPay = useCallback(() => {
    if (cart.length === 0) {
      toast.error("Səbət boşdur.");
      return;
    }
    const missingReason = cart.find((l) => l.price !== l.product.effectivePrice && !l.priceReason.trim());
    if (missingReason) {
      toast.error(`"${missingReason.product.name}" üçün qiymət dəyişmə səbəbini yazın.`);
      return;
    }
    setCashInput(total.toFixed(2));
    setCardInput("");
    setBonusInput("");
    setDebtInput("");
    setPayOpen(true);
  }, [cart, total]);

  useEffect(() => {
    const onKey = (e: KeyboardEvent) => {
      if (e.key === "F9" && shift && !payOpen) {
        e.preventDefault();
        openPay();
      }
      if (e.key === "F2") {
        e.preventDefault();
        scanRef.current?.focus();
      }
    };
    window.addEventListener("keydown", onKey);
    return () => window.removeEventListener("keydown", onKey);
  }, [shift, payOpen, openPay]);

  const cash = num(cashInput);
  const card = num(cardInput);
  const bonusPay = num(bonusInput);
  const debtPay = num(debtInput);
  const nonCash = r2(card + bonusPay + debtPay);
  const cashDue = r2(total - nonCash);
  const change = r2(cash - cashDue);
  const payValid =
    card >= 0 && bonusPay >= 0 && debtPay >= 0 && nonCash <= total && cash >= 0 && cash >= cashDue && ((bonusPay === 0 && debtPay === 0) || customer != null);

  // Bonus can pay at most the card's balance and the company's share of the sale.
  const maxBonus = customer ? Math.max(0, Math.min(customer.bonusBalance, r2((total * bonusMaxPercent) / 100), total)) : 0;
  const fmt = (n: number) => Math.max(0, r2(n)).toFixed(2);

  // What is left after bonus and credit has to be paid by cash and card together: typing one of them fills the other.
  const afterBonusDebt = (b: string, d: string) => Math.max(0, r2(total - num(b) - num(d)));

  const onCardChange = (value: string, b = bonusInput, d = debtInput) => {
    const left = afterBonusDebt(b, d);
    const c = Math.min(Math.max(0, num(value)), left);
    setCardInput(value === "" ? "" : value);
    setCashInput(fmt(left - c));
  };

  const onCashChange = (value: string, b = bonusInput, d = debtInput) => {
    setCashInput(value);
    const left = afterBonusDebt(b, d);
    const c = num(value);
    // Less cash than what is left: the rest goes to the card. More cash: it covers everything and change is given back.
    setCardInput(c < left ? fmt(left - c) : "");
  };

  const onBonusChange = (value: string) => {
    const b = Math.min(Math.max(0, num(value)), maxBonus);
    const text = value === "" ? "" : b === num(value) ? value : fmt(b);
    setBonusInput(text);
    // Card keeps what it had as far as it still fits; cash takes the rest.
    const left = afterBonusDebt(text, debtInput);
    const c = Math.min(num(cardInput), left);
    setCardInput(c > 0 ? fmt(c) : "");
    setCashInput(fmt(left - c));
  };

  const confirmPay = async () => {
    if (!register || !payValid) return;
    const lines = cart.map((l) => ({
      productId: l.product.id,
      quantity: l.quantity,
      discountAmount: l.discount,
      overridePrice: l.price !== l.product.effectivePrice ? l.price : null,
      priceChangeReason: l.price !== l.product.effectivePrice ? l.priceReason.trim() : null,
      serialNumbers: l.product.tracksSerial ? l.serials : null,
    }));
    const payments = [
      ...(cash > 0 ? [{ method: StorePaymentMethod.Cash, amount: cash }] : []),
      ...(card > 0 ? [{ method: StorePaymentMethod.Card, amount: card }] : []),
      ...(bonusPay > 0 ? [{ method: StorePaymentMethod.Bonus, amount: bonusPay }] : []),
      ...(debtPay > 0 ? [{ method: StorePaymentMethod.Debt, amount: debtPay }] : []),
    ];

    // The same id is resent only for the same sale, so a retry after a lost answer cannot sell twice.
    const signature = JSON.stringify({ lines, payments, customerId: customer?.id ?? null });
    if (!operation.current || operation.current.signature !== signature) {
      operation.current = { id: crypto.randomUUID(), signature };
    }
    const operationId = operation.current.id;

    setPaying(true);
    try {
      // A manager's approval is asked for only when the server says the step needs one.
      const sale = await approval.run((appr) =>
        createStoreSale({ operationId, receiptless, storeCashRegisterId: register.id, customerId: customer?.id ?? null, lines, payments, approval: appr ?? null }),
      );
      operation.current = null;
      setPayOpen(false);
      setCart([]);
      setCustomer(null);
      setDoneSale(sale);
      setReceiptless(false);
    } catch (e) {
      if (e instanceof StoreOfflineError) {
        // No connection: the sale waits on this device and goes to the server as soon as it is back.
        enqueueSale({ operationId, receiptless, storeCashRegisterId: register.id, customerId: customer?.id ?? null, lines, payments, approval: null }, total);
        operation.current = null;
        setPayOpen(false);
        setCart([]);
        setCustomer(null);
        toast.warning("Bağlantı yoxdur — satış bu cihazda saxlanıldı, bağlantı gələndə avtomatik göndəriləcək.");
        void syncQueuedSales();
      } else {
        toast.error(e instanceof Error ? e.message : "Satış tamamlanmadı.");
      }
    } finally {
      setPaying(false);
    }
  };

  // ---- Screens ----------------------------------------------------------------------------

  if (terminalState === "missing")
    return (
      <div className="mx-auto mt-16 max-w-md space-y-3 rounded-xl border bg-card p-6 text-center">
        <h1 className="text-lg font-semibold">Satış ekranı terminaldan açılır</h1>
        <p className="text-sm text-muted-foreground">
          Mağaza POS-u quraşdırılmış terminalda (biznes kodu, rejim və filial seçilib) kassirin giriş kodu ilə açılır. Admin paneldən satış edilmir; satışlara, qaytarmalara və hesabatlara yuxarıdakı bölmələrdən baxa bilərsiniz.
        </p>
        <Button onClick={() => window.open("/pos-login", "_blank")}>Terminal giriş səhifəsi</Button>
      </div>
    );

  if (booting || terminalState === "checking") return <p className="p-6 text-sm text-muted-foreground">Yüklənir…</p>;

  if (!shift) {
    const free = registers.filter((r) => r.isActive && !r.openShiftId);
    return (
      <div className="mx-auto mt-10 max-w-sm space-y-4 rounded-xl border bg-card p-6">
        <div className="flex items-center gap-2">
          <Lock className="h-5 w-5 text-muted-foreground" />
          <h1 className="text-lg font-semibold">Növbə bağlıdır</h1>
        </div>
        {registers.length === 0 ? (
          <p className="text-sm text-muted-foreground">
            Hələ kassa yoxdur. “Kassalar” bölməsindən kassa yaradın (filial və anbarı seçilir).
          </p>
        ) : !canOpen ? (
          <p className="text-sm text-muted-foreground">Növbə açmaq üçün icazəniz yoxdur.</p>
        ) : (
          <>
            <p className="text-sm text-muted-foreground">Satışa başlamaq üçün kassanı seçib növbəni açın.</p>
            <div className="space-y-2">
              <Label htmlFor="open-reg">Kassa</Label>
              <select
                id="open-reg"
                className="h-10 w-full rounded-md border border-input bg-background px-3 text-sm"
                value={pickRegisterId}
                onChange={(e) => setPickRegisterId(e.target.value ? Number(e.target.value) : "")}
              >
                <option value="">Seçin…</option>
                {free.map((r) => (
                  <option key={r.id} value={r.id}>
                    {r.name} — {r.restaurantName}
                  </option>
                ))}
              </select>
              {free.length === 0 && <p className="text-xs text-amber-600">Bütün kassalarda növbə açıqdır.</p>}
            </div>
            <div className="space-y-2">
              <Label htmlFor="open-cash">İlkin nağd (AZN)</Label>
              <Input id="open-cash" inputMode="decimal" value={openingCash} onChange={(e) => setOpeningCash(e.target.value)} />
            </div>
            <Button className="h-11 w-full" disabled={opening} onClick={() => void doOpenShift()}>
              {opening ? "Açılır…" : "Növbəni aç"}
            </Button>
          </>
        )}
      </div>
    );
  }

  // 100 shared buttons in 2 pages of 50; the right / left arrow turns the page.
  const allFilledSlots = slots.filter((s) => s.productId);
  const filledSlots = allFilledSlots.filter((s) => s.slotNumber > quickPage * 50 && s.slotNumber <= quickPage * 50 + 50);

  return (
    <div className="flex h-full min-h-0 flex-col md:flex-row">
      {approval.dialog}

      {/* Left: scan, search, products */}
      <section className="flex min-h-0 flex-1 flex-col gap-2 p-3 md:order-2">
        <div className="flex flex-wrap items-center gap-2">
          <div className="relative min-w-56 flex-1">
            <ScanBarcode className="absolute left-3 top-3 h-5 w-5 text-muted-foreground" />
            <Input
              ref={scanRef}
              className="h-11 pl-10 text-base"
              placeholder="Barkod / IMEI skan edin və ya ad yazın… (F2)"
              value={term}
              onChange={(e) => setTerm(e.target.value)}
              onKeyDown={(e) => {
                if (e.key === "Enter") {
                  e.preventDefault();
                  void onScan();
                }
              }}
            />
          </div>
          <Button variant={customer ? "default" : "outline"} className="h-11" onClick={() => setCustomerOpen(true)}>
            <UserRound className="mr-1 h-4 w-4" />
            {customer ? customer.name : "Müştəri"}
          </Button>
          <Button variant="outline" className="h-11" onClick={() => void openHeld()}>
            <Play className="mr-1 h-4 w-4" />
            Saxlanılanlar
          </Button>
          <Button variant="outline" className="h-11" onClick={() => void showReport()}>
            X-Hesabat
          </Button>
          <Button variant="outline" className="h-11" onClick={() => router.push("/store/returns")}>
            <Undo2 className="mr-1 h-4 w-4" />
            Qaytarma
          </Button>
          <Button variant="outline" className="h-11" onClick={() => setMoveOpen(true)}>
            <Wallet className="mr-1 h-4 w-4" />
            Mədaxil/Məxaric
          </Button>
          {canClose && (
            <Button variant="outline" className="h-11" onClick={() => void startClose()}>
              <Lock className="mr-1 h-4 w-4" />
              Növbəni bağla
            </Button>
          )}
        </div>

        {customer && (
          <div className="flex flex-wrap items-center gap-x-4 gap-y-1 rounded-lg border bg-card px-3 py-2 text-sm">
            <span className="font-semibold">{customer.name}</span>
            <span className="text-muted-foreground">Kart: {customer.code}</span>
            <span>
              Bonus: <b>{customer.bonusBalance.toFixed(2)}</b> <span className="text-muted-foreground">(= {formatCurrency(customer.bonusBalance)})</span>
            </span>
            <span>
              Bonus faizi: <b>{customer.bonusPercentShown}%</b>
            </span>
            {customer.debt > 0 && <span className="text-red-600">Borc: {formatCurrency(customer.debt)}</span>}
          </div>
        )}

        <p className="text-xs text-muted-foreground">
          {register?.name} — {register?.warehouseName} — kassir: {shift.cashierName ?? "—"}
        </p>

        <div className="flex items-center gap-2">
          <div className="flex rounded-md border">
            {(
              [
                ["products", "Məhsullar"],
                ["quick", `Sürətli düymələr (${allFilledSlots.length})`],
              ] as const
            ).map(([k, label]) => (
              <button
                key={k}
                onClick={() => setView(k)}
                className={cn("px-3 py-1.5 text-sm", view === k ? "bg-primary text-primary-foreground" : "hover:bg-muted")}
              >
                {label}
              </button>
            ))}
          </div>
        </div>

        {view === "products" ? (
          <div className="flex min-h-0 flex-1 flex-col gap-2">
            <div className="relative">
              <Search className="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
              <Input
                className="h-11 pl-9 pr-10"
                placeholder="Məhsul axtar (ad, SKU, barkod)…"
                value={listFilter}
                onChange={(e) => setListFilter(e.target.value)}
              />
              {listFilter && (
                <button type="button" aria-label="Təmizlə" className="absolute right-2 top-1/2 -translate-y-1/2 rounded p-1 text-muted-foreground hover:bg-muted" onClick={() => setListFilter("")}>
                  <X className="h-4 w-4" />
                </button>
              )}
            </div>
            <div className="flex min-h-0 flex-1 gap-2">
            <div ref={listRef} className="min-h-0 flex-1 divide-y overflow-y-auto rounded-lg border bg-card">
              {products.length === 0 ? (
                <p className="py-10 text-center text-sm text-muted-foreground">Məhsul tapılmadı.</p>
              ) : (
                products.map((p) => {
                  const out = p.stock != null && p.stock <= 0;
                  return (
                    <button
                      key={p.id}
                      onClick={() => void pick(p)}
                      className={cn(
                        "flex w-full items-center justify-between gap-3 px-3 py-2.5 text-left transition-colors hover:bg-muted active:bg-muted",
                        out && "opacity-60",
                      )}
                    >
                      <span className="min-w-0 flex-1 truncate text-sm font-medium">{p.name}</span>
                      <span className={cn("shrink-0 text-xs", out ? "text-red-600" : "text-muted-foreground")}>
                        {p.stock == null ? "" : `${p.stock} ${UNIT_LABELS[p.unit]}`}
                      </span>
                      <span className="w-20 shrink-0 text-right font-semibold">{formatCurrency(p.effectivePrice)}</span>
                    </button>
                  );
                })
              )}
            </div>
            <div className="flex shrink-0 flex-col justify-center gap-3">
              <Button type="button" variant="outline" className="h-20 w-14" aria-label="Yuxarı" onClick={() => scrollList(-1)}>
                <ChevronUp className="h-8 w-8" />
              </Button>
              <Button type="button" variant="outline" className="h-20 w-14" aria-label="Aşağı" onClick={() => scrollList(1)}>
                <ChevronDown className="h-8 w-8" />
              </Button>
            </div>
            </div>
          </div>
        ) : (
          <div className="flex min-h-0 flex-1 gap-2">
          <div className="grid min-h-0 flex-1 auto-rows-min grid-cols-3 gap-2 overflow-y-auto sm:grid-cols-5 lg:grid-cols-6 xl:grid-cols-8">
            {filledSlots.length === 0 ? (
              <p className="col-span-full py-10 text-center text-sm text-muted-foreground">
                Bu kassa üçün sürətli düymə təyin edilməyib. “Kassalar” bölməsindən düymələri təyin edin.
              </p>
            ) : (
              filledSlots.map((s) => (
                <button
                  key={s.slotNumber}
                  onClick={() => void pickSlot(s)}
                  style={s.color ? { backgroundColor: s.color, color: "#fff" } : undefined}
                  className="flex min-h-20 flex-col items-center justify-center rounded-lg border bg-card p-2 text-center transition-opacity hover:opacity-90 active:scale-[0.98]"
                >
                  <span className="line-clamp-2 text-sm font-semibold">{s.label || s.productName}</span>
                  <span className="mt-1 text-xs opacity-80">{s.salePrice != null ? formatCurrency(s.salePrice) : ""}</span>
                </button>
              ))
            )}
          </div>
          <div className="flex shrink-0 flex-col justify-center gap-3">
            <Button type="button" variant="outline" className="h-20 w-14" aria-label="Əvvəlki 50" disabled={quickPage === 0} onClick={() => setQuickPage((p) => Math.max(0, p - 1))}>
              <ChevronLeft className="h-8 w-8" />
            </Button>
            <Button type="button" variant="outline" className="h-20 w-14" aria-label="Növbəti 50" disabled={quickPage >= Math.ceil(slots.length / 50) - 1} onClick={() => setQuickPage((p) => p + 1)}>
              <ChevronRight className="h-8 w-8" />
            </Button>
          </div>
          </div>
        )}
      </section>

      {/* Right: cart */}
      <aside className="flex min-h-0 w-full shrink-0 flex-col border-t bg-card md:order-1 md:w-96 md:border-r md:border-t-0">
        <div className="flex items-center justify-between border-b px-3 py-2">
          <h2 className="font-semibold">Səbət</h2>
          {cart.length > 0 && (
            <div className="flex gap-1">
              <Button size="sm" variant="ghost" onClick={() => void holdCart()}>
                <Pause className="mr-1 h-4 w-4" />
                Saxla
              </Button>
              <Button size="sm" variant="ghost" onClick={() => void requestRemoval("ClearCart", null, clearCart)}>
                <Trash2 className="mr-1 h-4 w-4" />
                Təmizlə
              </Button>
            </div>
          )}
        </div>

        <div className="min-h-0 flex-1 overflow-y-auto">
          {cart.length === 0 ? (
            <p className="p-6 text-center text-sm text-muted-foreground">Məhsulu skan edin və ya seçin.</p>
          ) : (
            cart.map((l) => {
              const changed = l.price !== l.product.effectivePrice;
              return (
                <div key={l.product.id} className="border-b px-3 py-2">
                  <div className="flex items-start justify-between gap-2">
                    <span className="text-sm font-medium">{l.product.name}</span>
                    <span className="text-sm font-semibold">{formatCurrency(lineTotal(l))}</span>
                  </div>
                  {l.product.tracksSerial ? (
                    <div className="mt-1 flex flex-wrap gap-1">
                      {l.serials.map((s) => (
                        <span key={s} className="inline-flex items-center gap-1 rounded bg-muted px-2 py-0.5 text-xs">
                          {s}
                          <button onClick={() => void requestRemoval("RemoveLine", `${l.product.name} ${s}`, () => removeSerial(l.product.id, s))} aria-label="Sil">
                            <X className="h-3 w-3" />
                          </button>
                        </span>
                      ))}
                      <Button size="sm" variant="outline" className="h-6 px-2 text-xs" onClick={() => void pick(l.product)}>
                        <Plus className="mr-1 h-3 w-3" />
                        IMEI
                      </Button>
                    </div>
                  ) : (
                    <div className="mt-1 flex items-center gap-2">
                      <Button size="icon" variant="outline" className="h-8 w-8" onClick={() =>
                          l.quantity - 1 <= 0
                            ? void requestRemoval("RemoveLine", l.product.name, () => setQuantity(l.product.id, 0))
                            : setQuantity(l.product.id, r2(l.quantity - 1))
                        }>
                        <Minus className="h-4 w-4" />
                      </Button>
                      <Input
                        className="h-8 w-20 text-center"
                        inputMode="decimal"
                        value={String(l.quantity)}
                        onChange={(e) => {
                          // typing a quantity never removes the line; removal goes through the minus / bin buttons
                          const q = num(e.target.value);
                          if (q > 0) setQuantity(l.product.id, q);
                        }}
                      />
                      <Button size="icon" variant="outline" className="h-8 w-8" onClick={() => setQuantity(l.product.id, r2(l.quantity + 1))}>
                        <Plus className="h-4 w-4" />
                      </Button>
                      <span className="text-xs text-muted-foreground">{UNIT_LABELS[l.product.unit]}</span>
                      <Button size="icon" variant="ghost" className="ml-auto h-8 w-8" onClick={() => void requestRemoval("RemoveLine", l.product.name, () => setQuantity(l.product.id, 0))}>
                        <Trash2 className="h-4 w-4" />
                      </Button>
                    </div>
                  )}
                  <div className="mt-1 flex flex-wrap items-center gap-2 text-xs text-muted-foreground">
                    <Tag className="h-3 w-3" />
                    <Input
                      className="h-7 w-20"
                      inputMode="decimal"
                      value={String(l.price)}
                      onChange={(e) => updateLine(l.product.id, { price: Math.max(num(e.target.value), 0) })}
                      title="Vahid qiyməti"
                    />
                    {changed && (
                      <Input
                        className="h-7 min-w-24 flex-1"
                        placeholder="Qiymət dəyişmə səbəbi"
                        value={l.priceReason}
                        onChange={(e) => updateLine(l.product.id, { priceReason: e.target.value })}
                      />
                    )}
                    <span>Endirim:</span>
                    <Input
                      className="h-7 w-20"
                      inputMode="decimal"
                      value={l.discount ? String(l.discount) : ""}
                      placeholder="0"
                      onChange={(e) => updateLine(l.product.id, { discount: Math.min(Math.max(num(e.target.value), 0), lineGross(l)) })}
                    />
                  </div>
                </div>
              );
            })
          )}
        </div>

        <div className="space-y-2 border-t p-3">
          {discountTotal > 0 && (
            <div className="flex justify-between text-sm text-muted-foreground">
              <span>Endirim</span>
              <span>−{formatCurrency(discountTotal)}</span>
            </div>
          )}
          <div className="flex justify-between text-xl font-bold">
            <span>Yekun</span>
            <span>{formatCurrency(total)}</span>
          </div>
          <Button className="h-12 w-full text-base" disabled={cart.length === 0} onClick={openPay}>
            <Banknote className="mr-2 h-5 w-5" />
            Ödəniş (F9)
          </Button>
        </div>
      </aside>

      {/* Quantity for weighed / measured products */}
      <Dialog open={qtyFor != null} onOpenChange={(o) => !o && setQtyFor(null)}>
        <DialogContent className="sm:max-w-xs">
          <DialogHeader>
            <DialogTitle>{qtyFor?.name}</DialogTitle>
            <DialogDescription>
              Miqdar ({qtyFor ? UNIT_LABELS[qtyFor.unit] : ""}) — qiymət {qtyFor ? formatCurrency(qtyFor.effectivePrice) : ""}
            </DialogDescription>
          </DialogHeader>
          <Input
            autoFocus
            inputMode="decimal"
            value={qtyInput}
            onChange={(e) => setQtyInput(e.target.value)}
            onKeyDown={(e) => {
              if (e.key === "Enter" && qtyFor && num(qtyInput) > 0) {
                addToCart(qtyFor, num(qtyInput));
                setQtyFor(null);
              }
            }}
          />
          <DialogFooter>
            <Button variant="outline" onClick={() => setQtyFor(null)}>
              Ləğv et
            </Button>
            <Button
              disabled={num(qtyInput) <= 0}
              onClick={() => {
                if (qtyFor) addToCart(qtyFor, num(qtyInput));
                setQtyFor(null);
              }}
            >
              Əlavə et
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* IMEI / serial picker */}
      <Dialog open={serialFor != null} onOpenChange={(o) => !o && setSerialFor(null)}>
        <DialogContent className="max-h-[90vh] overflow-y-auto sm:max-w-sm">
          <DialogHeader>
            <DialogTitle>{serialFor?.name} — IMEI / serial</DialogTitle>
            <DialogDescription>Satılacaq vahidləri seçin və ya nömrəni skan edin.</DialogDescription>
          </DialogHeader>
          <Input
            autoFocus
            placeholder="IMEI / serial skan edin…"
            value={serialTerm}
            onChange={(e) => setSerialTerm(e.target.value)}
            onKeyDown={(e) => {
              if (e.key !== "Enter") return;
              e.preventDefault();
              const hit = serialOptions.find((u) => u.serialNumber === serialTerm.trim());
              if (hit) {
                setSerialPicked((p) => (p.includes(hit.serialNumber) ? p : [...p, hit.serialNumber]));
                setSerialTerm("");
              } else {
                toast.error("Bu nömrə bu anbarda stokda yoxdur.");
              }
            }}
          />
          <div className="max-h-60 overflow-y-auto rounded-md border">
            {serialOptions.length === 0 ? (
              <p className="p-4 text-center text-sm text-muted-foreground">Bu anbarda stokda vahid yoxdur.</p>
            ) : (
              serialOptions
                .filter((u) => u.serialNumber.includes(serialTerm.trim()))
                .map((u) => (
                  <label key={u.id} className="flex cursor-pointer items-center gap-2 border-b px-3 py-2 text-sm last:border-b-0 hover:bg-muted">
                    <input
                      type="checkbox"
                      checked={serialPicked.includes(u.serialNumber)}
                      onChange={(e) =>
                        setSerialPicked((p) => (e.target.checked ? [...p, u.serialNumber] : p.filter((x) => x !== u.serialNumber)))
                      }
                    />
                    {u.serialNumber}
                  </label>
                ))
            )}
          </div>
          <DialogFooter>
            <Button variant="outline" onClick={() => setSerialFor(null)}>
              Ləğv et
            </Button>
            <Button
              disabled={serialPicked.length === 0}
              onClick={() => {
                if (serialFor) addToCart(serialFor, serialPicked.length, serialPicked);
                setSerialFor(null);
              }}
            >
              Əlavə et ({serialPicked.length})
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* Held sales */}
      <Dialog open={heldOpen} onOpenChange={setHeldOpen}>
        <DialogContent className="max-h-[90vh] overflow-y-auto sm:max-w-md">
          <DialogHeader>
            <DialogTitle>Saxlanılan satışlar</DialogTitle>
            <DialogDescription>Saxladığınız səbətləri yenidən açın.</DialogDescription>
          </DialogHeader>
          {held.length === 0 ? (
            <p className="py-6 text-center text-sm text-muted-foreground">Saxlanılan satış yoxdur.</p>
          ) : (
            <div className="space-y-2">
              {held.map((h) => (
                <div key={h.id} className="flex items-center justify-between rounded-md border p-3 text-sm">
                  <div>
                    <p className="font-medium">
                      {h.title || `${h.itemCount} məhsul`} — {formatCurrency(h.total)}
                    </p>
                    <p className="text-xs text-muted-foreground">
                      {new Date(h.createdAtUtc).toLocaleTimeString("az-AZ")} — {h.cashierName ?? "kassir"}
                    </p>
                  </div>
                  <div className="flex gap-1">
                    <Button size="sm" onClick={() => void resumeHeld(h)}>
                      Aç
                    </Button>
                    <Button
                      size="sm"
                      variant="ghost"
                      onClick={async () => {
                        try {
                          await discardStoreHeldSale(h.id);
                          setHeld((prev) => prev.filter((x) => x.id !== h.id));
                        } catch (e) {
                          toast.error(e instanceof Error ? e.message : "Silinmədi.");
                        }
                      }}
                    >
                      <Trash2 className="h-4 w-4" />
                    </Button>
                  </div>
                </div>
              ))}
            </div>
          )}
        </DialogContent>
      </Dialog>

      {/* Customer */}
      <Dialog open={customerOpen} onOpenChange={setCustomerOpen}>
        <DialogContent className="max-h-[90vh] overflow-y-auto sm:max-w-md">
          <DialogHeader>
            <DialogTitle>Müştəri</DialogTitle>
            <DialogDescription>Kod və ya telefonla tapın; bonus və borc üçün lazımdır.</DialogDescription>
          </DialogHeader>
          <Input
            autoFocus
            placeholder="Kod, telefon və ya ad…"
            value={customerTerm}
            onChange={(e) => setCustomerTerm(e.target.value)}
            onKeyDown={async (e) => {
              if (e.key !== "Enter" || !customerTerm.trim()) return;
              e.preventDefault();
              try {
                setCustomer(await findStoreCustomer(customerTerm.trim()));
                setCustomerOpen(false);
              } catch {
                toast.error("Bu kod və ya telefonla müştəri tapılmadı.");
              }
            }}
          />
          <div className="max-h-52 overflow-y-auto rounded-md border">
            {customerResults.length === 0 ? (
              <p className="p-4 text-center text-sm text-muted-foreground">Müştəri tapılmadı.</p>
            ) : (
              customerResults.map((c) => (
                <button
                  type="button"
                  key={c.id}
                  className="flex w-full items-center justify-between border-b px-3 py-2 text-left text-sm last:border-b-0 hover:bg-muted"
                  onClick={() => {
                    setCustomer(c);
                    setCustomerOpen(false);
                  }}
                >
                  <span>
                    {c.name}
                    <span className="block text-xs text-muted-foreground">
                      {c.code} {c.phone && `· ${c.phone}`}
                    </span>
                  </span>
                  <span className="text-right text-xs">
                    <span className="block">Bonus: {formatCurrency(c.bonusBalance)}</span>
                    {c.debt > 0 && <span className="block text-red-600">Borc: {formatCurrency(c.debt)}</span>}
                  </span>
                </button>
              ))
            )}
          </div>
          <div className="space-y-2 rounded-md border p-3">
            <p className="text-sm font-medium">Yeni müştəri kartı</p>
            <div className="grid grid-cols-2 gap-2">
              <Input placeholder="Ad" value={newCustomerName} onChange={(e) => setNewCustomerName(e.target.value)} />
              <Input placeholder="Telefon" value={newCustomerPhone} onChange={(e) => setNewCustomerPhone(e.target.value)} />
            </div>
            <Button
              size="sm"
              variant="outline"
              disabled={!newCustomerName.trim()}
              onClick={async () => {
                try {
                  const created = await saveStoreCustomer(null, {
                    name: newCustomerName.trim(),
                    phone: newCustomerPhone.trim() || null,
                    email: null,
                    note: null,
                    creditLimit: 0,
                    isActive: true,
                  });
                  setCustomer(created);
                  setNewCustomerName("");
                  setNewCustomerPhone("");
                  setCustomerOpen(false);
                  toast.success(`Kart yaradıldı: ${created.code}`);
                } catch (err) {
                  toast.error(err instanceof Error ? err.message : "Kart yaradılmadı.");
                }
              }}
            >
              Kart yarat və seç
            </Button>
          </div>
          <DialogFooter>
            {customer && (
              <Button variant="outline" onClick={() => { setCustomer(null); setCustomerOpen(false); }}>
                Müştərini götür
              </Button>
            )}
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* Payment */}
      <Dialog open={payOpen} onOpenChange={(o) => !o && !paying && setPayOpen(false)}>
        <DialogContent className="sm:max-w-sm">
          <DialogHeader>
            <DialogTitle>Ödəniş</DialogTitle>
            <DialogDescription>Yekun: {formatCurrency(total)}</DialogDescription>
          </DialogHeader>
          <div className="space-y-3">
            <div className="space-y-1">
              <Label htmlFor="pay-cash">
                <Banknote className="mr-1 inline h-4 w-4" />
                Nağd (verilən məbləğ)
              </Label>
              <Input
                id="pay-cash"
                autoFocus
                inputMode="decimal"
                className="h-11 text-lg"
                value={cashInput}
                onChange={(e) => onCashChange(e.target.value)}
                onFocus={(e) => e.currentTarget.select()}
              />
              <div className="flex gap-1">
                {[5, 10, 20, 50, 100].map((v) => (
                  <Button key={v} type="button" size="sm" variant="outline" onClick={() => onCashChange(String(v))}>
                    {v}
                  </Button>
                ))}
              </div>
            </div>
            <div className="space-y-1">
              <Label htmlFor="pay-card">
                <CreditCard className="mr-1 inline h-4 w-4" />
                Kart
              </Label>
              <Input
                id="pay-card"
                inputMode="decimal"
                className="h-11 text-lg"
                value={cardInput}
                onChange={(e) => onCardChange(e.target.value)}
                onFocus={(e) => e.currentTarget.select()}
              />
              <Button type="button" size="sm" variant="outline" onClick={() => { setBonusInput(""); setDebtInput(""); setCardInput(total.toFixed(2)); setCashInput("0.00"); }}>
                Tam kartla
              </Button>
            </div>
            {customer && (
              <div className="grid grid-cols-2 gap-2 rounded-md border p-2">
                <div className="space-y-1">
                  <Label htmlFor="pay-bonus">Bonusla ödəmək (kartda: {formatCurrency(customer.bonusBalance)})</Label>
                  <div className="flex gap-1">
                    <Input id="pay-bonus" inputMode="decimal" value={bonusInput} onChange={(e) => onBonusChange(e.target.value)} onFocus={(e) => e.currentTarget.select()} />
                    <Button type="button" size="sm" variant="outline" disabled={maxBonus <= 0} onClick={() => onBonusChange(fmt(maxBonus))}>
                      Max
                    </Button>
                  </div>
                  <p className="text-[11px] text-muted-foreground">Ən çox {formatCurrency(maxBonus)} ödənilə bilər.</p>
                </div>
                <div className="space-y-1">
                  <Label htmlFor="pay-debt">Borca (limit: {formatCurrency(customer.creditAvailable)})</Label>
                  <Input
                    id="pay-debt"
                    inputMode="decimal"
                    value={debtInput}
                    onChange={(e) => {
                      setDebtInput(e.target.value);
                      const left = afterBonusDebt(bonusInput, e.target.value);
                      const c = Math.min(num(cardInput), left);
                      setCardInput(c > 0 ? fmt(c) : "");
                      setCashInput(fmt(left - c));
                    }}
                  />
                </div>
              </div>
            )}
            <div className="rounded-md bg-muted p-3 text-sm">
              {nonCash > total ? (
                <p className="text-red-600">Kart, bonus və borc cəmi yekundan çox ola bilməz.</p>
              ) : cash < cashDue ? (
                <p className="text-red-600">Çatışmır: {formatCurrency(r2(cashDue - cash))}</p>
              ) : (
                <p className="text-lg font-semibold text-emerald-700">Qaytarılacaq pul: {formatCurrency(Math.max(0, change))}</p>
              )}
            </div>
          </div>
          {allowReceiptless && canReceiptless && (
            <label className="flex items-center gap-2 text-sm">
              <input type="checkbox" checked={receiptless} onChange={(e) => setReceiptless(e.target.checked)} />
              Çeksiz satış (çek çap olunmur; satış yenə də qeyd edilir)
            </label>
          )}
          <DialogFooter>
            <Button variant="outline" disabled={paying} onClick={() => setPayOpen(false)}>
              Geri
            </Button>
            <Button disabled={!payValid || paying} onClick={() => void confirmPay()}>
              {paying ? "Gedir…" : "Satışı tamamla"}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* Receipt */}
      <Dialog open={doneSale != null} onOpenChange={(o) => !o && setDoneSale(null)}>
        <DialogContent className="sm:max-w-sm">
          <DialogHeader>
            <DialogTitle>Satış tamamlandı — {doneSale?.documentNo}</DialogTitle>
            <DialogDescription>Yekun {doneSale ? formatCurrency(doneSale.total) : ""}</DialogDescription>
          </DialogHeader>
          {doneSale && doneSale.fiscalStatus === 3 && (
            <p className="rounded-md bg-red-50 p-3 text-sm text-red-700">
              Fiskal təsdiq alınmadı: {doneSale.fiscalError ?? "naməlum xəta"}. Satış qüvvədədir; “Satışlar” bölməsindən yenidən göndərə bilərsiniz.
            </p>
          )}
          {doneSale && doneSale.changeAmount > 0 && (
            <p className="rounded-md bg-emerald-50 p-3 text-center text-lg font-semibold text-emerald-700">
              Qaytarılacaq pul: {formatCurrency(doneSale.changeAmount)}
            </p>
          )}
          <DialogFooter>
            {doneSale?.isReceiptless ? (
              <span className="mr-auto text-sm text-muted-foreground">Çeksiz satış</span>
            ) : (
              <Button
                variant="outline"
                onClick={() => {
                  if (doneSale && !printStoreSale(doneSale)) toast.error("Çap pəncərəsi açılmadı — popup bloklamasını söndürün.");
                }}
              >
                <Printer className="mr-1 h-4 w-4" />
                Çek çap et
              </Button>
            )}
            <Button onClick={() => setDoneSale(null)}>Yeni satış</Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* X / Z report */}
      <Dialog open={reportOpen} onOpenChange={setReportOpen}>
        <DialogContent className="max-h-[90vh] overflow-y-auto sm:max-w-md">
          <DialogHeader>
            <DialogTitle>Növbə hesabatı</DialogTitle>
          </DialogHeader>
          {report && <ShiftReportView report={report} />}
        </DialogContent>
      </Dialog>

      {/* Close shift (Z) */}
      <Dialog open={closeOpen} onOpenChange={(o) => !o && !closing && setCloseOpen(false)}>
        <DialogContent className="sm:max-w-sm">
          <DialogHeader>
            <DialogTitle>Növbəni bağla (Z-Hesabat)</DialogTitle>
            <DialogDescription>Kassadakı nağd pulu sayın və faktiki məbləği yazın. Bağlanandan sonra növbəyə satış yazmaq olmaz.</DialogDescription>
          </DialogHeader>
          <div className="space-y-3">
            <div className="space-y-1">
              <Label htmlFor="close-cash">Faktiki nağd (AZN)</Label>
              <Input id="close-cash" autoFocus inputMode="decimal" className="h-11 text-lg" value={actualCash} onChange={(e) => setActualCash(e.target.value)} />
            </div>
            <div className="space-y-1">
              <Label htmlFor="close-note">Qeyd (istəyə bağlı)</Label>
              <Input id="close-note" value={closeNote} onChange={(e) => setCloseNote(e.target.value)} />
            </div>
          </div>
          <DialogFooter>
            <Button variant="outline" disabled={closing} onClick={() => setCloseOpen(false)}>
              Ləğv et
            </Button>
            <Button disabled={closing} onClick={() => void doClose()}>
              {closing ? "Bağlanır…" : "Növbəni bağla"}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* Cash in / out */}
      <Dialog open={moveOpen} onOpenChange={setMoveOpen}>
        <DialogContent className="sm:max-w-sm">
          <DialogHeader>
            <DialogTitle>Kassa mədaxil / məxaric</DialogTitle>
            <DialogDescription>Satışdan kənar nağd pul daxil etmə və çıxarma. Növbə hesabatına düşür.</DialogDescription>
          </DialogHeader>
          <div className="space-y-3">
            <div className="flex rounded-md border">
              {[
                { v: StoreCashMovementType.In, label: "Mədaxil" },
                { v: StoreCashMovementType.Out, label: "Məxaric" },
              ].map((o) => (
                <button
                  key={o.v}
                  type="button"
                  onClick={() => setMoveType(o.v)}
                  className={cn("flex-1 py-2 text-sm", moveType === o.v ? "bg-primary text-primary-foreground" : "hover:bg-muted")}
                >
                  {o.label}
                </button>
              ))}
            </div>
            <div className="space-y-1">
              <Label htmlFor="mv-amount">Məbləğ (AZN)</Label>
              <Input id="mv-amount" inputMode="decimal" value={moveAmount} onChange={(e) => setMoveAmount(e.target.value)} />
            </div>
            <div className="space-y-1">
              <Label htmlFor="mv-reason">Səbəb</Label>
              <Input id="mv-reason" value={moveReason} onChange={(e) => setMoveReason(e.target.value)} />
            </div>
          </div>
          <DialogFooter>
            <Button variant="outline" onClick={() => setMoveOpen(false)}>
              Ləğv et
            </Button>
            <Button onClick={() => void doMovement()}>Yaz</Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}
