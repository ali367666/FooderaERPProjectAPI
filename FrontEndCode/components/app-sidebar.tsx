"use client";

import { applyTheme } from "@/lib/theme";
import Link from "next/link";
import { usePathname } from "next/navigation";
import { navGroups } from "@/lib/constants";
import { cn } from "@/lib/utils";
import { LogoutButton } from "@/components/logout-button";
import { ChefHat, Menu, Search, X } from "lucide-react";
import { PAGE_SEARCH_INDEX } from "@/lib/search-index.generated";
import { matchesAllWords, searchWords } from "@/lib/search-text";
import { searchData, type GlobalSearchResult } from "@/lib/services/global-search-service";
import { useEffect, useMemo, useState } from "react";
import { usePermissionSet } from "@/hooks/use-auth-permissions";
import { getStoredAuthUser, getStoredToken } from "@/lib/auth-client";
import { getCompanyIdFromToken } from "@/lib/jwt-permissions";
import {
  getCompanySettingsBranding,
  type CompanySettingsBranding,
} from "@/lib/services/company-settings-service";
import { useSelectedCompany } from "@/contexts/selected-company-context";
import { useSelectedRestaurant } from "@/contexts/selected-restaurant-context";

export function AppSidebar() {
  const pathname = usePathname();
  const [isOpen, setIsOpen] = useState(true);
  const permissionSet = usePermissionSet();
  const [branding, setBranding] = useState<CompanySettingsBranding | null>(null);

  useEffect(() => {
    applyTheme(branding);
  }, [branding]);
  const { selectedCompanyId } = useSelectedCompany();
  const { selectedRestaurantId } = useSelectedRestaurant();

  // Only the platform SuperAdmin bypasses permission checks in the nav — a tenant's own "Admin"
  // role is scoped to their company and must go through the normal permissionSet check, so
  // platform-only links (e.g. Companies) correctly stay hidden from them.
  const isSuperAdmin = useMemo(() => {
    const authUser = getStoredAuthUser();
    const roles = authUser?.roles ?? [];
    return roles.some((r) => r.trim().toLowerCase() === "superadmin");
  }, [permissionSet]);

  useEffect(() => {
    // A SuperAdmin browsing another company (Company filter) — and, within it, one specific branch
    // (Filial filter / "Data Seçimi") — must see THAT company/branch's own module-driven nav, not
    // their own. A tenant Admin only ever has their own company anyway.
    const ownCompanyId = getCompanyIdFromToken(getStoredToken());
    const effectiveCompanyId = selectedCompanyId ?? ownCompanyId;
    if (!effectiveCompanyId) return;
    getCompanySettingsBranding(effectiveCompanyId, selectedRestaurantId ?? undefined)
      .then(setBranding)
      .catch(() => setBranding(null));
  }, [selectedCompanyId, selectedRestaurantId]);

  const visibleNavGroups = useMemo(
    () =>
      navGroups
        .map((group) => ({
          ...group,
          items: group.items.filter((item) => {
            if (item.module && branding && branding[item.module] === false) return false;
            if (item.hiddenForStoreMode && branding?.moduleDataSecimi) return false;
            if (!item.permission) return true;
            if (isSuperAdmin) return true;
            return permissionSet.has(item.permission);
          }),
        }))
        .filter((group) => group.items.length > 0),
    [isSuperAdmin, permissionSet, branding],
  );

  const isStoreMode = Boolean(branding?.moduleDataSecimi);

  // The search looks at the page names AND at the words written inside every page (labels, headings,
  // placeholders...), so "slogan" finds Tənzimləmələr. Only pages the user may open are searched.
  const [query, setQuery] = useState("");
  const words = useMemo(() => searchWords(query), [query]);
  // The records themselves (products, employees, counterparties...), asked from the server while typing.
  const [dataResults, setDataResults] = useState<GlobalSearchResult[]>([]);
  useEffect(() => {
    const term = query.trim();
    if (words.length === 0 || term.length < 2) {
      setDataResults([]);
      return;
    }
    let cancelled = false;
    const timer = window.setTimeout(() => {
      searchData(term, selectedCompanyId)
        .then((rows) => !cancelled && setDataResults(rows))
        .catch(() => !cancelled && setDataResults([]));
    }, 300);
    return () => {
      cancelled = true;
      window.clearTimeout(timer);
    };
  }, [query, words, selectedCompanyId]);

  const searchResults = useMemo(() => {
    if (words.length === 0) return [];
    const results: { href: string; title: string; group: string; titleMatch: boolean; phrases: string[] }[] = [];
    for (const group of visibleNavGroups) {
      const groupTitle = isStoreMode && group.storeModeTitle ? group.storeModeTitle : group.title;
      for (const item of group.items) {
        const title = isStoreMode && item.storeModeTitle ? item.storeModeTitle : item.title;
        const titleMatch = matchesAllWords(`${title} ${groupTitle}`, words);
        const phrases = (PAGE_SEARCH_INDEX[item.href] ?? []).filter((p) => matchesAllWords(p, words));
        if (titleMatch || phrases.length > 0) {
          results.push({ href: item.href, title, group: groupTitle, titleMatch, phrases: phrases.slice(0, 3) });
        }
      }
    }
    // Pages whose name matches come first.
    return results.sort((a, b) => Number(b.titleMatch) - Number(a.titleMatch));
  }, [words, visibleNavGroups, isStoreMode]);

  return (
    <>
      {/* Mobile Toggle */}
      <button
        onClick={() => setIsOpen(!isOpen)}
        className="fixed top-4 left-4 z-50 p-2 rounded-md bg-primary text-primary-foreground lg:hidden"
      >
        {isOpen ? <X size={20} /> : <Menu size={20} />}
      </button>

      {/* Sidebar */}
      <aside
        className={cn(
          "fixed left-0 top-0 h-screen w-64 bg-sidebar border-r border-sidebar-border transition-all duration-300 z-40 flex flex-col",
          !isOpen && "-translate-x-full lg:translate-x-0"
        )}
      >
        {/* Logo Section */}
        <div className="flex-shrink-0 p-6 border-b border-sidebar-border">
          <Link href="/dashboard" className="flex items-center gap-3">
            <div className="w-10 h-10 rounded-lg bg-sidebar-primary flex items-center justify-center">
              <ChefHat size={24} className="text-sidebar-primary-foreground" />
            </div>
            <div className="flex flex-col">
              <span className="font-bold text-lg text-sidebar-foreground">
                Foodera
              </span>
              <span className="text-xs text-sidebar-foreground/60">ERP</span>
            </div>
          </Link>
        </div>

        {/* Search */}
        <div className="flex-shrink-0 px-4 pt-4">
          <div className="relative">
            <Search size={16} className="pointer-events-none absolute left-3 top-1/2 -translate-y-1/2 text-sidebar-foreground/60" />
            <input
              type="search"
              value={query}
              onChange={(e) => setQuery(e.target.value)}
              onKeyDown={(e) => {
                if (e.key === "Escape") setQuery("");
                if (e.key === "Enter" && searchResults[0]) {
                  window.location.assign(`${searchResults[0].href}?find=${encodeURIComponent(query.trim())}`);
                }
              }}
              placeholder="Menyuda və səhifələrdə axtar…"
              className="h-9 w-full rounded-md border border-sidebar-border bg-sidebar-accent/10 pl-9 pr-8 text-sm text-sidebar-foreground placeholder:text-sidebar-foreground/50 focus:outline-none focus:ring-1 focus:ring-sidebar-primary [&::-webkit-search-cancel-button]:hidden"
            />
            {query && (
              <button
                type="button"
                onClick={() => setQuery("")}
                className="absolute right-2 top-1/2 -translate-y-1/2 text-sidebar-foreground/60 hover:text-sidebar-foreground"
                aria-label="Axtarışı təmizlə"
              >
                <X size={14} />
              </button>
            )}
          </div>
        </div>

        {/* Navigation */}
        <nav className="flex-1 overflow-y-auto p-4">
          {words.length > 0 ? (
            <div className="space-y-1">
              {searchResults.length === 0 && dataResults.length === 0 && (
                <p className="px-4 py-3 text-sm text-sidebar-foreground/60">Heç nə tapılmadı.</p>
              )}
              {searchResults.map((r) => (
                <Link
                  key={r.href}
                  href={`${r.href}?find=${encodeURIComponent(query.trim())}`}
                  onClick={() => {
                    setIsOpen(false);
                    setQuery("");
                  }}
                  className="block rounded-md px-4 py-2.5 text-sm text-sidebar-foreground hover:bg-sidebar-accent/20"
                >
                  <span className="block font-medium">{r.title}</span>
                  <span className="block text-[11px] uppercase tracking-wide text-sidebar-foreground/50">{r.group}</span>
                  {r.phrases.map((p) => (
                    <span key={p} className="mt-0.5 block truncate text-xs text-sidebar-foreground/70" title={p}>
                      {p}
                    </span>
                  ))}
                </Link>
              ))}

              {dataResults.length > 0 && (
                <>
                  <h3 className="px-4 pb-1 pt-4 text-xs font-semibold uppercase tracking-wider text-sidebar-foreground/60">
                    Məlumatlar
                  </h3>
                  {dataResults.map((r) => (
                    <Link
                      key={`${r.kind}-${r.title}-${r.subtitle ?? ""}`}
                      href={`${r.href}?find=${encodeURIComponent(r.title)}`}
                      onClick={() => {
                        setIsOpen(false);
                        setQuery("");
                      }}
                      className="block rounded-md px-4 py-2.5 text-sm text-sidebar-foreground hover:bg-sidebar-accent/20"
                    >
                      <span className="block truncate font-medium" title={r.title}>{r.title}</span>
                      <span className="block truncate text-[11px] uppercase tracking-wide text-sidebar-foreground/50">
                        {r.kind}
                        {r.subtitle ? ` · ${r.subtitle}` : ""}
                      </span>
                    </Link>
                  ))}
                </>
              )}
            </div>
          ) : (
          visibleNavGroups.map((group) => (
            <div key={group.title} className="mb-8">
              <h3 className="px-4 py-2 text-xs font-semibold text-sidebar-foreground/60 uppercase tracking-wider">
                {isStoreMode && group.storeModeTitle ? group.storeModeTitle : group.title}
              </h3>
              <ul className="space-y-2">
                {group.items.map((item) => {
                  const isActive = pathname === item.href;
                  const Icon = item.icon;

                  return (
                    <li key={item.href}>
                      <Link
                        href={item.href}
                        onClick={() => setIsOpen(false)}
                        className={cn(
                          "flex items-center gap-3 px-4 py-3 rounded-md text-sm font-medium transition-colors",
                          isActive
                            ? "bg-sidebar-primary text-sidebar-primary-foreground"
                            : "text-sidebar-foreground hover:bg-sidebar-accent/20"
                        )}
                      >
                        <Icon size={20} />
                        <span>{isStoreMode && item.storeModeTitle ? item.storeModeTitle : item.title}</span>
                        {item.badge && (
                          <span className="ml-auto bg-sidebar-primary text-sidebar-primary-foreground text-xs rounded-full px-2 py-0.5">
                            {item.badge}
                          </span>
                        )}
                      </Link>
                    </li>
                  );
                })}
              </ul>
            </div>
          ))
          )}
        </nav>

        {/* Footer Section */}
        <div className="mt-auto p-4 border-t border-sidebar-border">
          <LogoutButton />
          <p className="mt-3 text-xs text-sidebar-foreground/60">© 2024 Foodera ERP</p>
        </div>
      </aside>

      {/* Mobile Overlay */}
      {isOpen && (
        <div
          className="fixed inset-0 bg-black/50 z-30 lg:hidden"
          onClick={() => setIsOpen(false)}
        />
      )}
    </>
  );
}
