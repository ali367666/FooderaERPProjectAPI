"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import { navGroups } from "@/lib/constants";
import { cn } from "@/lib/utils";
import { LogoutButton } from "@/components/logout-button";
import { ChefHat, Menu, X } from "lucide-react";
import { useMemo, useState } from "react";
import { usePermissionSet } from "@/hooks/use-auth-permissions";
import { getStoredAuthUser } from "@/lib/auth-client";

export function AppSidebar() {
  const pathname = usePathname();
  const [isOpen, setIsOpen] = useState(true);
  const permissionSet = usePermissionSet();

  const isAdmin = useMemo(() => {
    const authUser = getStoredAuthUser();
    const roles = authUser?.roles ?? [];
    return roles.some((r) => r.trim().toLowerCase() === "admin");
  }, [permissionSet]);

  const visibleNavGroups = useMemo(
    () =>
      navGroups
        .map((group) => ({
          ...group,
          items: group.items.filter((item) => {
            if (!item.permission) return true;
            if (isAdmin) return true;
            return permissionSet.has(item.permission);
          }),
        }))
        .filter((group) => group.items.length > 0),
    [isAdmin, permissionSet],
  );

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

        {/* Navigation */}
        <nav className="flex-1 overflow-y-auto p-4">
          {visibleNavGroups.map((group) => (
            <div key={group.title} className="mb-8">
              <h3 className="px-4 py-2 text-xs font-semibold text-sidebar-foreground/60 uppercase tracking-wider">
                {group.title}
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
                        <span>{item.title}</span>
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
          ))}
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
