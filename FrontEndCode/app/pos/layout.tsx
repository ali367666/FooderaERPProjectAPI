"use client";

import { useEffect, useState } from "react";
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
} from "lucide-react";
import PosAuthGuard from "@/components/pos/pos-auth-guard";
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
  { href: "/pos", label: "Masalar", icon: LayoutGrid, permission: null as string | null, module: null as keyof CompanySettingsBranding | null },
  { href: "/pos/orders", label: "Sifarişlər", icon: ClipboardList, permission: "Orders.View", module: null as keyof CompanySettingsBranding | null },
  { href: "/pos/reservations", label: "Rezervasiyalar", icon: CalendarCheck, permission: "Reservation.View", module: "moduleRezervasyon" as keyof CompanySettingsBranding | null },
  { href: "/pos/kitchen", label: "Mətbəx", icon: KitchenIcon, permission: "Kitchen.View", module: null as keyof CompanySettingsBranding | null },
  { href: "/pos/orders/history", label: "Köhnə qəbzlər", icon: History, permission: "Pos.PrintOldReceipt", module: null as keyof CompanySettingsBranding | null },
];

export default function PosLayout({ children }: { children: React.ReactNode }) {
  const router = useRouter();
  const pathname = usePathname();
  const [terminal, setTerminal] = useState<PosTerminalContext | null>(null);
  const [branding, setBranding] = useState<CompanySettingsBranding | null>(null);

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

  const showTabs = !pathname.startsWith("/pos/order");
  const hasOrdersView = useHasPermission("Orders.View");
  const hasReservationView = useHasPermission("Reservation.View");
  const hasKitchenView = useHasPermission("Kitchen.View");
  const hasPrintOldReceipt = useHasPermission("Pos.PrintOldReceipt");
  const canAccessSettings = useHasPermission("Pos.AccessSettings");
  const canZReport = useHasPermission("Pos.ZReport");
  const permissionMap: Record<string, boolean> = {
    "Orders.View": hasOrdersView,
    "Reservation.View": hasReservationView,
    "Kitchen.View": hasKitchenView,
    "Pos.PrintOldReceipt": hasPrintOldReceipt,
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

  return (
    <PosAuthGuard>
      <div className="flex min-h-screen flex-col bg-muted/30">
        <header className="flex h-14 shrink-0 items-center justify-between border-b bg-background px-4">
          <div className="flex items-center gap-2 font-semibold">
            <ChefHat className="h-5 w-5 text-primary" />
            <span>{terminal?.restaurantName ?? terminal?.companyName ?? "POS"}</span>
          </div>
          <div className="flex items-center gap-3">
            <span className="tabular-nums text-sm font-semibold text-muted-foreground">
              {clockNow.toLocaleTimeString("az-AZ", { hour: "2-digit", minute: "2-digit", second: "2-digit" })}
            </span>
            <button
              type="button"
              onClick={() => setShiftDialogOpen(true)}
              className={cn(
                "flex items-center gap-1.5 rounded-md px-2.5 py-1.5 text-xs font-medium",
                shift ? "bg-emerald-100 text-emerald-800" : "bg-slate-100 text-slate-600",
              )}
            >
              <Clock className="h-3.5 w-3.5" />
              {shift ? "Növbə açıqdır" : "Növbə bağlıdır"}
            </button>
            {canAccessSettings && (
              <button
                type="button"
                onClick={() => setSettingsOpen(true)}
                className="flex items-center gap-2 rounded-md p-1.5 text-muted-foreground hover:bg-muted"
              >
                <Settings className="h-4 w-4" />
              </button>
            )}
            <button
              type="button"
              onClick={handleLogout}
              title="Hesabı kilidlə"
              className="flex items-center justify-center rounded-md p-2 text-foreground hover:bg-muted"
            >
              <Lock className="h-6 w-6" />
            </button>
          </div>
        </header>
        {showTabs && (
          <nav className="flex shrink-0 gap-1 border-b bg-background px-3 py-2">
            {TABS.filter((tab) => {
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
                  {tab.label}
                </Link>
              );
            })}
          </nav>
        )}
        <main className="flex-1 overflow-auto">{children}</main>
      </div>

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
