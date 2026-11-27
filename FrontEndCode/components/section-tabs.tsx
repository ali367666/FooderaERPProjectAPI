"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import { useIsSuperAdmin, usePermissionSet } from "@/hooks/use-auth-permissions";
import { cn } from "@/lib/utils";

/** Pages that share one menu entry are switched with these tabs. */
const SECTIONS: { href: string; label: string; permission: string }[][] = [
  [
    { href: "/dashboard/stock-purchases", label: "Alışlar", permission: "StockPurchase.View" },
    { href: "/dashboard/stock-purchase-costs", label: "Alış xərcləri", permission: "StockPurchase.View" },
    { href: "/dashboard/counterparties", label: "Konturagentlər", permission: "Counterparty.View" },
  ],
];

const isOn = (pathname: string, href: string) => pathname === href || pathname.startsWith(href + "/");

export function SectionTabs() {
  const pathname = usePathname();
  const permissions = usePermissionSet();
  const isSuperAdmin = useIsSuperAdmin();

  const section = SECTIONS.find((tabs) => tabs.some((t) => isOn(pathname, t.href)));
  if (!section) return null;
  const tabs = section.filter((t) => isSuperAdmin || permissions.has(t.permission));
  if (tabs.length < 2) return null;

  return (
    <nav aria-label="Bölmə" className="mb-4 flex flex-wrap gap-1 border-b">
      {tabs.map((t) => (
        <Link
          key={t.href}
          href={t.href}
          aria-current={isOn(pathname, t.href) ? "page" : undefined}
          className={cn(
            "-mb-px border-b-2 px-3 py-2 text-sm",
            isOn(pathname, t.href) ? "border-primary font-semibold text-foreground" : "border-transparent text-muted-foreground hover:text-foreground",
          )}
        >
          {t.label}
        </Link>
      ))}
    </nav>
  );
}
