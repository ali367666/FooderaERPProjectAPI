"use client";

import Link from "next/link";
import { usePathname, useRouter } from "next/navigation";
import { useEffect, useState } from "react";
import { LayoutDashboard, AlertTriangle, ArrowLeftRight, BarChart3, Bell, Boxes, ClipboardCheck, ClipboardList, LogOut, Package, Settings, ShoppingCart, Timer, Truck, Building2, Undo2, Users } from "lucide-react";
import PosAuthGuard from "@/components/pos/pos-auth-guard";
import { Toaster } from "@/components/ui/sonner";
import { Button } from "@/components/ui/button";
import { clearStoredAuth, getStoredAuthUser, isPosSession } from "@/lib/auth-client";
import { getCurrentUserNameFromToken } from "@/lib/pos-session";
import { useStorePermissions as usePermissionSet } from "@/lib/store/use-store-permissions";
import { cn } from "@/lib/utils";
import { getStoreAlerts, getStoreSettings, type StoreAlert } from "@/lib/store/store-service";
import StoreTranslator from "@/components/store/store-translator";
import SyncStatus from "@/components/store/sync-status";
import { connectStoreRealtime } from "@/lib/store/realtime";
import { configureReceipt } from "@/lib/store/store-receipt";
import { getPosTerminalContext } from "@/lib/pos-terminal-client";
import { toast } from "sonner";

/**
 * Store (mağaza) mode. Its screens live under /store and share nothing with the restaurant POS except the
 * login and the permission system.
 */
const TABS = [
  { href: "/store", label: "Satış", icon: ShoppingCart, permission: "StoreSale.Create" },
  { href: "/store/sales", label: "Satışlar", icon: ClipboardList, permission: "StoreSale.View" },
  { href: "/store/returns", label: "Qaytarma", icon: Undo2, permission: "StoreSale.Create" },
  { href: "/store/purchases", label: "Alışlar", icon: Truck, permission: "StorePurchase.View" },
  { href: "/store/suppliers", label: "Təchizatçılar", icon: Building2, permission: "StoreSupplier.View" },
  { href: "/store/inventory", label: "İnventarizasiya", icon: ClipboardCheck, permission: "StoreInventory.View" },
  { href: "/store/transfers", label: "Transfer", icon: ArrowLeftRight, permission: "StoreTransfer.View" },
  { href: "/store/customers", label: "Müştərilər", icon: Users, permission: "StoreCustomer.View" },
  { href: "/store/reports", label: "Hesabatlar", icon: BarChart3, permission: "StoreReport.View" },
  { href: "/store/shifts", label: "Növbələr", icon: Timer, permission: "StoreReport.View" },
  { href: "/store/products", label: "Məhsullar", icon: Package, permission: "StoreProduct.View" },
  { href: "/store/stock", label: "Stok", icon: Boxes, permission: "StoreStock.View" },
  { href: "/store/registers", label: "Kassalar", icon: Settings, permission: "StoreRegister.Manage" },
];

export default function StoreLayout({ children }: { children: React.ReactNode }) {
  const router = useRouter();
  const pathname = usePathname();
  const permissions = usePermissionSet();
  const [userName, setUserName] = useState<string | null>(null);
  const [alerts, setAlerts] = useState<StoreAlert[]>([]);
  const [alertsOpen, setAlertsOpen] = useState(false);
  const [lang, setLang] = useState<"az" | "en">("az");
  // An admin / SuperAdmin who came from the dashboard goes back there and signs out to the admin login; store staff use the POS login.
  const [isAdmin, setIsAdmin] = useState(false);
  // Signed in at a POS terminal: the cashier gets the sale screen alone (returns, shifts and the rest are reached from it).
  const [cashierMode, setCashierMode] = useState(false);
  useEffect(() => {
    setCashierMode(isPosSession());
  }, []);
  useEffect(() => {
    const roles = (getStoredAuthUser()?.roles ?? []).map((r) => r.trim().toLowerCase());
    setIsAdmin(roles.some((r) => r === "superadmin" || r === "admin" || r === "companysuperadmin"));
  }, []);

  useEffect(() => {
    getStoreSettings().then(configureReceipt).catch(() => undefined);
  }, []);

  useEffect(() => {
    try {
      if (localStorage.getItem("store.lang") === "en") setLang("en");
    } catch {
      // storage blocked: stay on Azerbaijani
    }
  }, []);

  const switchLang = () => {
    const next = lang === "az" ? "en" : "az";
    setLang(next);
    try {
      localStorage.setItem("store.lang", next);
    } catch {
      // not remembered, still switched
    }
  };

  useEffect(() => {
    setUserName(getCurrentUserNameFromToken());
  }, []);

  // What needs attention is refreshed now and then; a failure just shows nothing.
  useEffect(() => {
    let cancelled = false;
    const load = () =>
      getStoreAlerts()
        .then((a) => {
          if (!cancelled) setAlerts(a);
        })
        .catch(() => undefined);
    void load();
    const timer = setInterval(load, 120_000);
    // The server also pushes events as they happen, so the bell updates at once.
    const disconnect = connectStoreRealtime((event) => {
      toast.info(event.title, { description: event.message });
      void load();
    });
    return () => {
      cancelled = true;
      clearInterval(timer);
      disconnect();
    };
  }, []);

  // An admin who came from the dashboard manages and watches; selling is done at the terminals.
  const visible = TABS.filter((t) => permissions.has(t.permission)).filter((t) => !(isAdmin && t.href === "/store" && !getPosTerminalContext()));

  return (
    <PosAuthGuard loginPath={isAdmin ? "/login" : "/pos-login"}>
      <div className="flex h-screen flex-col bg-background text-foreground">
        <header className="flex shrink-0 items-center gap-2 border-b bg-card px-3 py-2">
          <span className="mr-2 text-sm font-semibold">Mağaza</span>
          {cashierMode && pathname !== "/store" && (
            <Button variant="outline" size="sm" onClick={() => router.push("/store")}>
              <ShoppingCart className="mr-1 h-4 w-4" />
              Satış ekranı
            </Button>
          )}
          <nav className="flex flex-1 gap-1 overflow-x-auto">
            {(cashierMode ? [] : visible).map((t) => {
              const active = t.href === "/store" ? pathname === "/store" : pathname.startsWith(t.href);
              return (
                <Link
                  key={t.href}
                  href={t.href}
                  className={cn(
                    "flex shrink-0 items-center gap-1.5 rounded-md px-3 py-2 text-sm font-medium transition-colors",
                    active ? "bg-primary text-primary-foreground" : "text-muted-foreground hover:bg-muted",
                  )}
                >
                  <t.icon className="h-4 w-4" />
                  {t.label}
                </Link>
              );
            })}
          </nav>
          <StoreTranslator lang={lang} />
          <Button variant="ghost" size="sm" onClick={switchLang} aria-label="Language">
            {lang === "az" ? "EN" : "AZ"}
          </Button>
          <SyncStatus />
          <div className="relative">
            <Button variant="ghost" size="icon" onClick={() => setAlertsOpen((o) => !o)} aria-label="Bildirişlər">
              <Bell className="h-4 w-4" />
              {alerts.length > 0 && (
                <span className="absolute -right-0.5 -top-0.5 flex h-4 min-w-4 items-center justify-center rounded-full bg-red-600 px-1 text-[10px] text-white">
                  {alerts.length}
                </span>
              )}
            </Button>
            {alertsOpen && (
              <div className="absolute right-0 z-50 mt-1 w-80 rounded-md border bg-card p-2 shadow-lg">
                {alerts.length === 0 ? (
                  <p className="p-3 text-center text-sm text-muted-foreground">Diqqət tələb edən heç nə yoxdur.</p>
                ) : (
                  alerts.map((a) => (
                    <Link
                      key={a.kind}
                      href={a.link ?? "/store"}
                      onClick={() => setAlertsOpen(false)}
                      className="flex gap-2 rounded p-2 text-sm hover:bg-muted"
                    >
                      <AlertTriangle className={cn("mt-0.5 h-4 w-4 shrink-0", a.severity === "danger" ? "text-red-600" : "text-amber-500")} />
                      <span>
                        <span className="block font-medium">{a.title}</span>
                        <span className="block text-xs text-muted-foreground">{a.message}</span>
                      </span>
                    </Link>
                  ))
                )}
              </div>
            )}
          </div>
          {userName && <span className="hidden text-sm text-muted-foreground sm:inline">{userName}</span>}
          {isAdmin && (
            <Button variant="ghost" size="sm" onClick={() => router.push("/dashboard")}>
              <LayoutDashboard className="mr-1 h-4 w-4" />
              Dashboard
            </Button>
          )}
          <Button
            variant="ghost"
            size="sm"
            onClick={() => {
              router.replace(isAdmin ? "/login" : "/pos-login");
              clearStoredAuth();
            }}
          >
            <LogOut className="mr-1 h-4 w-4" />
            Çıxış
          </Button>
        </header>
        <main className="min-h-0 flex-1 overflow-auto">{children}</main>
        <Toaster />
      </div>
    </PosAuthGuard>
  );
}
