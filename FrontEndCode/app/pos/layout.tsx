"use client";

import { LicenseStatusBadge } from "@/components/license-status-badge";
import { FiscalLamp } from "@/components/pos/fiscal-lamp";
import { applyTheme } from "@/lib/theme";
import { useEffect, useMemo, useRef, useState } from "react";
import Link from "next/link";
import { usePathname, useRouter } from "next/navigation";
import {
  ChefHat,
  Lock,
  CalendarCheck,
  ChefHat as KitchenIcon,
  LayoutGrid,
  ClipboardList,
  Settings,
  Clock,
  History,
  Boxes,
  Undo2,
  Search,
  UserRound,
  X,
} from "lucide-react";
import { format } from "date-fns";
import PosAuthGuard from "@/components/pos/pos-auth-guard";
import { PosSearchContext } from "@/components/pos/pos-search-context";
import { VirtualKeyboard } from "@/components/pos/virtual-keyboard";
import { getCurrentEmployee, getCurrentUserNameFromToken } from "@/lib/pos-session";
import { Toaster } from "@/components/ui/sonner";
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
import { toast } from "sonner";
import { clearStoredAuth } from "@/lib/auth-client";
import { getPosTerminalContext, clearPosTerminalContext, type PosTerminalContext } from "@/lib/pos-terminal-client";
import { useHasPermission } from "@/hooks/use-auth-permissions";
import { cn } from "@/lib/utils";
import {
  getCompanySettingsBranding,
  type CompanySettingsBranding,
} from "@/lib/services/company-settings-service";
import { getCurrentShift, openShift, closeShift, type Shift, type ZReport } from "@/lib/services/shift-service";

const TABS = [
  { href: "/pos", label: "Masalar", storeLabel: "Satış", icon: LayoutGrid, permission: null as string | null, module: null as keyof CompanySettingsBranding | null, hideInStoreMode: false },
  { href: "/pos/orders", label: "Sifarişlər", storeLabel: "Satışlar", icon: ClipboardList, permission: "Orders.View", module: null as keyof CompanySettingsBranding | null, hideInStoreMode: false },
  { href: "/pos/reservations", label: "Rezervasiyalar", storeLabel: "Rezervasiyalar", icon: CalendarCheck, permission: "Reservation.View", module: "moduleRezervasyon" as keyof CompanySettingsBranding | null, hideInStoreMode: true },
  { href: "/pos/kitchen", label: "Mətbəx", storeLabel: "Mətbəx", icon: KitchenIcon, permission: "Kitchen.View", module: null as keyof CompanySettingsBranding | null, hideInStoreMode: true },
  { href: "/pos/returns", label: "Geri qaytarma", storeLabel: "Geri qaytarma", icon: Undo2, permission: "Pos.ReturnSale", module: null as keyof CompanySettingsBranding | null, hideInStoreMode: false },
  { href: "/pos/orders/history", label: "Köhnə qəbzlər", storeLabel: "Köhnə çeklər", icon: History, permission: "Pos.PrintOldReceipt", module: null as keyof CompanySettingsBranding | null, hideInStoreMode: false },
  { href: "/pos/warehouse-adjust", label: "Anbar düzəlişi", storeLabel: "Anbar düzəlişi", icon: Boxes, permission: "Pos.WarehouseAmountChange", module: null as keyof CompanySettingsBranding | null, hideInStoreMode: false },
];

export default function PosLayout({ children }: { children: React.ReactNode }) {
  const router = useRouter();
  const pathname = usePathname();
  const [terminal, setTerminal] = useState<PosTerminalContext | null>(null);
  const [branding, setBranding] = useState<CompanySettingsBranding | null>(null);

  useEffect(() => {
    applyTheme(branding);
  }, [branding]);

  useEffect(() => {
    setTerminal(getPosTerminalContext());
  }, []);

  useEffect(() => {
    if (!terminal) return;
    getCompanySettingsBranding(terminal.companyId)
      .then(setBranding)
      .catch(() => setBranding(null));
  }, [terminal]);

  const handleLogout = () => {
    clearStoredAuth();
    router.replace("/pos-login");
  };

  const isStoreMode = branding?.moduleDataSecimi === true;
  const showTabs = !pathname.startsWith("/pos/order");
  // The tables screen fills the viewport (its action bar sits at the bottom) and owns the search box.
  const isTablesPage = pathname === "/pos";
  // The order screen is laid out the same way: fixed to the viewport, with its own scrolling panes.
  const isOrderPage = pathname.startsWith("/pos/order/");

  const [searchQuery, setSearchQuery] = useState("");
  useEffect(() => {
    if (!isTablesPage) setSearchQuery("");
  }, [isTablesPage]);
  const searchContext = useMemo(() => ({ query: searchQuery, setQuery: setSearchQuery }), [searchQuery]);

  const [userName, setUserName] = useState<string | null>(null);
  useEffect(() => {
    setUserName(getCurrentUserNameFromToken());
    getCurrentEmployee()
      .then((e) => {
        const name = e && (e.fullName?.trim() || `${e.firstName} ${e.lastName}`.trim());
        if (name) setUserName(name);
      })
      .catch(() => undefined);
  }, []);
  const hasOrdersView = useHasPermission("Orders.View");
  const hasReservationView = useHasPermission("Reservation.View");
  const hasKitchenView = useHasPermission("Kitchen.View");
  const hasPrintOldReceipt = useHasPermission("Pos.PrintOldReceipt");
  const hasWarehouseAmountChange = useHasPermission("Pos.WarehouseAmountChange");
  const canAccessSettings = useHasPermission("Pos.AccessSettings");
  const canZReport = useHasPermission("Pos.ZReport");
  const permissionMap: Record<string, boolean> = {
    "Orders.View": hasOrdersView,
    "Reservation.View": hasReservationView,
    "Kitchen.View": hasKitchenView,
    "Pos.PrintOldReceipt": hasPrintOldReceipt,
    "Pos.WarehouseAmountChange": hasWarehouseAmountChange,
  };

  const [shift, setShift] = useState<Shift | null>(null);
  const [shiftDialogOpen, setShiftDialogOpen] = useState(false);
  const [openingCashInput, setOpeningCashInput] = useState("0");
  const [closingCashInput, setClosingCashInput] = useState("0");
  const [zReportResult, setZReportResult] = useState<ZReport | null>(null);
  const [settingsOpen, setSettingsOpen] = useState(false);
  const [shiftBusy, setShiftBusy] = useState(false);
  const [clockNow, setClockNow] = useState(() => new Date());

  useEffect(() => {
    const id = window.setInterval(() => setClockNow(new Date()), 1000);
    return () => window.clearInterval(id);
  }, []);

  const isBeforeOpening = useMemo(() => {
    if (!branding?.openingTime) return false;
    const parts = branding.openingTime.split(":").map(Number);
    if (parts.length < 2 || parts.some((n) => Number.isNaN(n))) return false;
    const openingMinutes = parts[0] * 60 + parts[1];
    const nowMinutes = clockNow.getHours() * 60 + clockNow.getMinutes();
    return nowMinutes < openingMinutes;
  }, [branding?.openingTime, clockNow]);

  useEffect(() => {
    if (!terminal?.restaurantId) return;
    getCurrentShift(terminal.restaurantId)
      .then(setShift)
      .catch(() => setShift(null));
  }, [terminal]);

  const handleOpenShift = async () => {
    if (!terminal?.restaurantId) return;
    setShiftBusy(true);
    try {
      const amount = Number(openingCashInput) || 0;
      const s = await openShift(terminal.restaurantId, amount);
      setShift(s);
      setShiftDialogOpen(false);
      toast.success("Növbə açıldı");
    } catch (err) {
      toast.error(err instanceof Error ? err.message : "Növbə açılmadı");
    } finally {
      setShiftBusy(false);
    }
  };

  const handleCloseShift = async () => {
    if (!shift) return;
    setShiftBusy(true);
    try {
      const amount = Number(closingCashInput) || 0;
      const report = await closeShift(shift.id, amount);
      setZReportResult(report);
      setShift(null);
      setShiftDialogOpen(false);
      toast.success("Növbə bağlandı");
    } catch (err) {
      toast.error(err instanceof Error ? err.message : "Növbə bağlanmadı");
    } finally {
      setShiftBusy(false);
    }
  };

  const handleResetTerminal = () => {
    clearPosTerminalContext();
    clearStoredAuth();
    router.replace("/pos-login");
  };

  const wallpaperOpacity =
    branding?.transparencyLevel != null ? Math.min(Math.max(branding.transparencyLevel, 0), 100) / 100 : 1;

  return (
    <PosAuthGuard>
      {/* The dashboard mounts its own Toaster; without this one every POS message (errors included) was invisible. */}
      <Toaster richColors closeButton position="top-center" />
      <PosSearchContext.Provider value={searchContext}>
      <div
        className={cn(
          "flex flex-col",
          isTablesPage || isOrderPage ? "h-dvh" : "min-h-screen",
          !branding?.wallpaperUrl && "bg-muted/30",
          branding?.touchScreenMode === true && "touch-mode",
        )}
      >
        {branding?.wallpaperUrl && (
          <div
            className="pointer-events-none fixed inset-0 -z-10 bg-cover bg-center"
            style={{ backgroundImage: `url(${branding.wallpaperUrl})`, opacity: wallpaperOpacity }}
            aria-hidden
          />
        )}
        <header className="relative z-30 flex h-16 shrink-0 items-stretch border-b bg-background">
          {/* Search on the tables screen; elsewhere the branch name holds the slot. */}
          <div className="flex min-w-0 flex-1 items-center px-3">
            {isTablesPage && !isBeforeOpening ? (
              <HeaderSearch value={searchQuery} onChange={setSearchQuery} />
            ) : (
              <div className="flex min-w-0 items-center gap-2 font-semibold">
                <ChefHat className="h-5 w-5 shrink-0 text-primary" />
                <span className="truncate">{terminal?.restaurantName ?? terminal?.companyName ?? "POS"}</span>
              </div>
            )}
          </div>

          <div className="hidden items-center border-l px-3 sm:flex">
            <LicenseStatusBadge className="py-1.5 text-sm" />
          </div>

          <div className="hidden min-w-0 items-center gap-2.5 border-l px-4 md:flex">
            <span className="flex h-9 w-9 shrink-0 items-center justify-center rounded-full bg-primary/10 text-primary">
              <UserRound className="h-5 w-5" />
            </span>
            <div className="min-w-0 leading-tight">
              <p className="max-w-48 truncate text-sm font-semibold">{userName ?? "—"}</p>
              <p className="max-w-48 truncate text-xs text-muted-foreground">
                {terminal?.restaurantName ?? terminal?.companyName}
                {terminal?.workstationName ? ` · ${terminal.workstationName}` : ""}
              </p>
            </div>
          </div>

          {branding?.posShowClock !== false && (
            <div className="flex flex-col items-end justify-center border-l px-4 leading-tight">
              <span className="text-lg font-bold tabular-nums">{format(clockNow, "HH:mm:ss")}</span>
              <span className="text-xs tabular-nums text-muted-foreground">{format(clockNow, "dd.MM.yyyy")}</span>
            </div>
          )}

          <div className="flex items-center gap-1.5 border-l px-3">
            <FiscalLamp />
            <button
              type="button"
              onClick={() => setShiftDialogOpen(true)}
              className={cn(
                "flex items-center gap-1.5 rounded-md px-2.5 py-1.5 text-xs font-medium",
                shift ? "bg-emerald-100 text-emerald-800" : "bg-slate-100 text-slate-600",
              )}
            >
              <Clock className="h-3.5 w-3.5" />
              <span className="hidden lg:inline">{shift ? "Növbə açıqdır" : "Növbə bağlıdır"}</span>
            </button>
            {canAccessSettings && (
              <button
                type="button"
                onClick={() => setSettingsOpen(true)}
                title="Ayarlar"
                className="flex items-center gap-2 rounded-md p-2 text-muted-foreground hover:bg-muted"
              >
                <Settings className="h-5 w-5" />
              </button>
            )}
            {/* The tables screen has its own "Çıxış" in the action bar. */}
            {!isTablesPage && (
              <button
                type="button"
                onClick={handleLogout}
                title="Hesabı kilidlə"
                className="flex items-center justify-center rounded-md p-2 text-foreground hover:bg-muted"
              >
                <Lock className="h-6 w-6" />
              </button>
            )}
          </div>
        </header>
        {isBeforeOpening ? (
          <main className="flex flex-1 flex-col items-center justify-center gap-2 p-6 text-center">
            <Lock className="h-10 w-10 text-muted-foreground" />
            <h2 className="text-xl font-semibold">Filial hələ bağlıdır</h2>
            <p className="text-muted-foreground">
              Açılış saatı: {branding?.openingTime?.slice(0, 5)}. Sistem həmin saatdan aktiv olacaq.
            </p>
          </main>
        ) : (
          <>
            {showTabs && (
              <nav className="flex shrink-0 gap-1 border-b bg-background px-3 py-2">
                {TABS.filter((tab) => {
                  if (isStoreMode && tab.hideInStoreMode) return false;
                  if (tab.module && branding && branding[tab.module] === false) return false;
                  return !tab.permission || permissionMap[tab.permission];
                }).map((tab) => {
                  const active = pathname === tab.href;
                  const Icon = tab.icon;
                  return (
                    <Link
                      key={tab.href}
                      href={tab.href}
                      className={cn(
                        "flex items-center gap-2 rounded-md px-3 py-2 text-sm font-medium transition-colors",
                        active ? "bg-primary text-primary-foreground" : "text-muted-foreground hover:bg-muted",
                      )}
                    >
                      <Icon className="h-4 w-4" />
                      {isStoreMode ? tab.storeLabel : tab.label}
                    </Link>
                  );
                })}
              </nav>
            )}
            <main className="min-h-0 flex-1 overflow-auto">{children}</main>
          </>
        )}
      </div>
      </PosSearchContext.Provider>

      {/* Shift open/close dialog */}
      <Dialog open={shiftDialogOpen} onOpenChange={setShiftDialogOpen}>
        <DialogContent className="sm:max-w-sm">
          <DialogHeader>
            <DialogTitle>{shift ? "Növbəni bağla" : "Növbə aç"}</DialogTitle>
            <DialogDescription>
              {shift
                ? `Açılıb: ${new Date(shift.openedAt).toLocaleString("az-AZ")}`
                : "Kassadakı başlanğıc nağd məbləği daxil edin."}
            </DialogDescription>
          </DialogHeader>
          {!shift && (
            <div className="space-y-2">
              <Label htmlFor="opening-cash">Başlanğıc nağd</Label>
              <Input id="opening-cash" type="number" step="0.01" value={openingCashInput} onChange={(e) => setOpeningCashInput(e.target.value)} />
            </div>
          )}
          {shift && canZReport && (
            <div className="space-y-2">
              <Label htmlFor="closing-cash">Bağlanış nağd</Label>
              <Input id="closing-cash" type="number" step="0.01" value={closingCashInput} onChange={(e) => setClosingCashInput(e.target.value)} />
            </div>
          )}
          {shift && !canZReport && (
            <p className="text-sm text-muted-foreground">Növbəni bağlamaq üçün icazəniz yoxdur.</p>
          )}
          <DialogFooter>
            <Button variant="outline" onClick={() => setShiftDialogOpen(false)}>
              Bağla
            </Button>
            {!shift && (
              <Button disabled={shiftBusy} onClick={() => void handleOpenShift()}>
                {shiftBusy ? "Açılır…" : "Növbəni aç"}
              </Button>
            )}
            {shift && canZReport && (
              <Button disabled={shiftBusy} onClick={() => void handleCloseShift()}>
                {shiftBusy ? "Bağlanır…" : "Növbəni bağla"}
              </Button>
            )}
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* Z-report result dialog */}
      <Dialog open={zReportResult !== null} onOpenChange={(o) => !o && setZReportResult(null)}>
        <DialogContent id="zreport-print-area" className="sm:max-w-sm">
          <DialogHeader>
            <DialogTitle>Z-Hesabatı</DialogTitle>
            <DialogDescription>
              {zReportResult && new Date(zReportResult.openedAt).toLocaleString("az-AZ")} —{" "}
              {zReportResult?.closedAt && new Date(zReportResult.closedAt).toLocaleString("az-AZ")}
            </DialogDescription>
          </DialogHeader>
          {zReportResult && (
            <div className="space-y-2 text-sm">
              <div className="flex justify-between"><span>Sifariş sayı</span><span>{zReportResult.orderCount}</span></div>
              <div className="flex justify-between"><span>Ümumi satış</span><span>{zReportResult.grossTotal.toFixed(2)} ₼</span></div>
              <div className="flex justify-between"><span>Endirim</span><span>{zReportResult.totalDiscount.toFixed(2)} ₼</span></div>
              <div className="flex justify-between"><span>Servis haqqı</span><span>{zReportResult.totalServiceCharge.toFixed(2)} ₼</span></div>
              <div className="flex justify-between"><span>Açılış nağd</span><span>{zReportResult.openingCashAmount.toFixed(2)} ₼</span></div>
              <div className="flex justify-between"><span>Bağlanış nağd</span><span>{(zReportResult.closingCashAmount ?? 0).toFixed(2)} ₼</span></div>
              <div className="mt-2 border-t pt-2 font-semibold">Ödəniş növünə görə</div>
              {zReportResult.paymentBreakdown.map((b) => (
                <div key={b.paymentMethod} className="flex justify-between text-muted-foreground">
                  <span>{b.paymentMethod} ({b.orderCount})</span>
                  <span>{b.totalAmount.toFixed(2)} ₼</span>
                </div>
              ))}
              {zReportResult.returnCount > 0 && (
                <>
                  <div className="mt-2 border-t pt-2 font-semibold">Geri qaytarmalar ({zReportResult.returnCount})</div>
                  {zReportResult.returnBreakdown.map((b) => (
                    <div key={b.paymentMethod} className="flex justify-between text-muted-foreground">
                      <span>{b.paymentMethod} ({b.orderCount})</span>
                      <span>−{b.totalAmount.toFixed(2)} ₼</span>
                    </div>
                  ))}
                </>
              )}
              <div className="mt-2 flex justify-between border-t pt-2 font-semibold">
                <span>Xalis satış</span>
                <span>{zReportResult.netTotal.toFixed(2)} ₼</span>
              </div>
            </div>
          )}
          <DialogFooter>
            <Button variant="outline" onClick={() => window.print()}>Çap et</Button>
            <Button onClick={() => setZReportResult(null)}>Bağla</Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* POS settings dialog */}
      <Dialog open={settingsOpen} onOpenChange={setSettingsOpen}>
        <DialogContent className="sm:max-w-sm">
          <DialogHeader>
            <DialogTitle>Ayarlar</DialogTitle>
            <DialogDescription>
              {terminal?.companyName} — {terminal?.restaurantName}
            </DialogDescription>
          </DialogHeader>
          <Button variant="destructive" onClick={handleResetTerminal}>
            Terminalı sıfırla
          </Button>
        </DialogContent>
      </Dialog>
    </PosAuthGuard>
  );
}

/** Search box of the tables screen; focusing it opens the on-screen keyboard below. */
function HeaderSearch({ value, onChange }: { value: string; onChange: (value: string) => void }) {
  const [keyboardOpen, setKeyboardOpen] = useState(false);
  const containerRef = useRef<HTMLDivElement | null>(null);
  const inputRef = useRef<HTMLInputElement | null>(null);

  useEffect(() => {
    if (!keyboardOpen) return;
    const onPointerDown = (e: PointerEvent) => {
      if (!containerRef.current?.contains(e.target as Node)) setKeyboardOpen(false);
    };
    document.addEventListener("pointerdown", onPointerDown);
    return () => document.removeEventListener("pointerdown", onPointerDown);
  }, [keyboardOpen]);

  const close = () => {
    setKeyboardOpen(false);
    inputRef.current?.blur();
  };

  return (
    <div ref={containerRef} className="relative w-full max-w-xl">
      <Search className="pointer-events-none absolute left-3.5 top-1/2 h-5 w-5 -translate-y-1/2 text-muted-foreground" />
      <input
        ref={inputRef}
        type="search"
        // The on-screen keyboard replaces the OS one; a physical keyboard still types normally.
        inputMode="none"
        autoComplete="off"
        value={value}
        onChange={(e) => onChange(e.target.value)}
        onFocus={() => setKeyboardOpen(true)}
        onKeyDown={(e) => {
          if (e.key === "Escape" || e.key === "Enter") close();
        }}
        placeholder="Masa, ofisiant və ya sifariş nömrəsi axtar…"
        className={cn(
          "h-11 w-full rounded-xl border-2 bg-muted/40 pl-11 pr-11 text-base outline-none transition-colors",
          "placeholder:text-muted-foreground focus:border-primary focus:bg-background",
          "[&::-webkit-search-cancel-button]:hidden",
        )}
      />
      {value && (
        <button
          type="button"
          aria-label="Axtarışı təmizlə"
          onMouseDown={(e) => e.preventDefault()}
          onClick={() => onChange("")}
          className="absolute right-2 top-1/2 flex h-8 w-8 -translate-y-1/2 items-center justify-center rounded-lg text-muted-foreground hover:bg-muted hover:text-foreground"
        >
          <X className="h-4 w-4" />
        </button>
      )}
      {keyboardOpen && (
        <VirtualKeyboard
          value={value}
          onChange={onChange}
          onClose={close}
          className="absolute left-0 top-full z-50 mt-2 w-[min(46rem,calc(100vw-1.5rem))]"
        />
      )}
    </div>
  );
}
