"use client";

import { LogOut } from "lucide-react";
import { useLogout } from "@/hooks/use-logout";

export function LogoutButton() {
  const logout = useLogout();

  return (
    <button
      type="button"
      onClick={logout}
      className="flex w-full items-center gap-2 px-3 py-2 rounded-lg text-sm font-medium text-sidebar-foreground hover:bg-muted transition-colors"
    >
      <LogOut size={18} />
      <span>Logout</span>
    </button>
  );
}
