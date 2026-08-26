"use client";

import { useEffect, useMemo, useState } from "react";
import { usePathname } from "next/navigation";
import { ShieldAlert } from "lucide-react";
import { navGroups, type NavItem } from "@/lib/constants";
import { usePermissionSet } from "@/hooks/use-auth-permissions";
import { getStoredAuthUser, getStoredToken } from "@/lib/auth-client";
import { getCompanyIdFromToken } from "@/lib/jwt-permissions";
import {
  getCompanySettingsBranding,
  type CompanySettingsBranding,
} from "@/lib/services/company-settings-service";

const allNavItems: NavItem[] = navGroups
  .flatMap((group) => group.items)
  .sort((a, b) => b.href.length - a.href.length);

function findMatchingNavItem(pathname: string): NavItem | undefined {
  return allNavItems.find((item) => pathname === item.href || pathname.startsWith(item.href + "/"));
}

export function DashboardPermissionGuard({ children }: { children: React.ReactNode }) {
  const pathname = usePathname();
  const permissionSet = usePermissionSet();
  const [branding, setBranding] = useState<CompanySettingsBranding | null>(null);

  const isAdmin = useMemo(() => {
    const authUser = getStoredAuthUser();
    const roles = authUser?.roles ?? [];
    return roles.some((r) => r.trim().toLowerCase() === "admin");
  }, [permissionSet]);

  useEffect(() => {
    const companyId = getCompanyIdFromToken(getStoredToken());
    if (!companyId) return;
    getCompanySettingsBranding(companyId)
      .then(setBranding)
      .catch(() => setBranding(null));
  }, []);

  const navItem = useMemo(() => findMatchingNavItem(pathname), [pathname]);

  const allowed = useMemo(() => {
    if (!navItem) return true;
    if (navItem.module && branding && branding[navItem.module] === false) return false;
    if (!navItem.permission) return true;
    if (isAdmin) return true;
    return permissionSet.has(navItem.permission);
  }, [navItem, branding, isAdmin, permissionSet]);

  if (!allowed) {
    return (
      <div className="flex h-[60vh] flex-col items-center justify-center gap-3 text-center">
        <ShieldAlert className="h-12 w-12 text-muted-foreground" />
        <h2 className="text-lg font-semibold">Bu bölməyə icazəniz yoxdur</h2>
        <p className="max-w-sm text-sm text-muted-foreground">
          Bu səhifəyə giriş üçün lazımi icazə hesabınıza təyin edilməyib. Kömək üçün administratorunuzla əlaqə saxlayın.
        </p>
      </div>
    );
  }

  return <>{children}</>;
}
