"use client";

import { useMemo, useSyncExternalStore } from "react";
import { getStoredAuthUser, getStoredToken, subscribeToToken } from "@/lib/auth-client";
import { getPermissionClaimsFromToken, getRoleClaimsFromToken } from "@/lib/jwt-permissions";

function subscribe(onStoreChange: () => void): () => void {
  return subscribeToToken(() => onStoreChange());
}

function getTokenSnapshot(): string | null {
  return getStoredToken();
}

function getServerSnapshot(): string | null {
  return null;
}

/** Current user's Permission claims from the stored JWT (updates when token changes). */
export function usePermissionSet(): ReadonlySet<string> {
  const token = useSyncExternalStore(subscribe, getTokenSnapshot, getServerSnapshot);
  return useMemo(() => {
    const authUser = getStoredAuthUser();
    const permissions = authUser?.permissions ?? getPermissionClaimsFromToken(token);
    const roles = authUser?.roles ?? getRoleClaimsFromToken(token);
    // Temporary diagnostics requested by user.
    console.log("Logged user roles:", roles);
    console.log("Logged user permissions:", permissions);
    return new Set(permissions);
  }, [token]);
}

export function useHasPermission(permission: string): boolean {
  return hasPermission(permission, usePermissionSet());
}

/**
 * True only for the platform-level SuperAdmin (the reseller managing every tenant) — never for a
 * tenant's own "Admin" role, which is scoped to their own company.
 */
export function useIsSuperAdmin(): boolean {
  const token = useSyncExternalStore(subscribe, getTokenSnapshot, getServerSnapshot);
  return useMemo(() => {
    const authUser = getStoredAuthUser();
    const roles = authUser?.roles ?? getRoleClaimsFromToken(token);
    return roles.some((r) => r.trim().toLowerCase() === "superadmin");
  }, [token]);
}

export function hasPermission(permission: string, permissionSet: ReadonlySet<string>): boolean {
  const raw = permission.trim();
  if (!raw) return false;
  const permissions = Array.from(permissionSet);
  console.log("Checking permission:", raw);
  console.log("Available permissions:", permissions);
  return permissionSet.has(raw);
}
