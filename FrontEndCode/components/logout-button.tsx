"use client";

import { LogOut } from "lucide-react";
import { useLogout } from "@/hooks/use-logout";
import { cn } from "@/lib/utils";

export function LogoutButton({ collapsed = false }: { collapsed?: boolean }) {
  const logout = useLogout();

  return (
    <button
      type="button"
      onClick={logout}
      title={collapsed ? "Logout" : undefined}
      className={cn(
        "flex w-full items-center gap-2 px-3 py-2 rounded-lg text-sm font-medium text-sidebar-foreground hover:bg-muted transition-colors",
        collapsed && "lg:justify-center lg:px-0",
      )}
    >
      <LogOut size={18} />
      <span className={cn(collapsed && "lg:sr-only")}>Logout</span>
    </button>
  );
}
