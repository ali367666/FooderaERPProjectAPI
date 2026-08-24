"use client";

import { useEffect, useState } from "react";
import Link from "next/link";
import { usePathname, useRouter } from "next/navigation";
import { ChefHat, LogOut, CalendarCheck, ChefHat as KitchenIcon, LayoutGrid, ClipboardList } from "lucide-react";
import PosAuthGuard from "@/components/pos/pos-auth-guard";
import { clearStoredAuth } from "@/lib/auth-client";
import { getPosTerminalContext, type PosTerminalContext } from "@/lib/pos-terminal-client";
import { useHasPermission } from "@/hooks/use-auth-permissions";
import { cn } from "@/lib/utils";
import {
  getCompanySettingsBranding,
  type CompanySettingsBranding,
} from "@/lib/services/company-settings-service";

const TABS = [
  { href: "/pos", label: "Masalar", icon: LayoutGrid, permission: null as string | null, module: null as keyof CompanySettingsBranding | null },
  { href: "/pos/orders", label: "Sifarişlər", icon: ClipboardList, permission: "Orders.View", module: null as keyof CompanySettingsBranding | null },
  { href: "/pos/reservations", label: "Rezervasiyalar", icon: CalendarCheck, permission: "Reservation.View", module: "moduleRezervasyon" as keyof CompanySettingsBranding | null },
  { href: "/pos/kitchen", label: "Mətbəx", icon: KitchenIcon, permission: "Kitchen.View", module: null as keyof CompanySettingsBranding | null },
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
  const permissionMap: Record<string, boolean> = {
    "Orders.View": hasOrdersView,
    "Reservation.View": hasReservationView,
    "Kitchen.View": hasKitchenView,
  };

  return (
    <PosAuthGuard>
      <div className="flex min-h-screen flex-col bg-muted/30">
        <header className="flex h-14 shrink-0 items-center justify-between border-b bg-background px-4">
          <div className="flex items-center gap-2 font-semibold">
            <ChefHat className="h-5 w-5 text-primary" />
            <span>{terminal?.restaurantName ?? terminal?.companyName ?? "POS"}</span>
          </div>
          <button
            type="button"
            onClick={handleLogout}
            className="flex items-center gap-2 rounded-md px-3 py-1.5 text-sm font-medium text-muted-foreground hover:bg-muted"
          >
            <LogOut className="h-4 w-4" />
            Çıxış
          </button>
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
    </PosAuthGuard>
  );
}
