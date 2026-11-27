"use client";

import type { ReactNode } from "react";
import { useIsSuperAdmin } from "@/hooks/use-auth-permissions";

/**
 * Shown to the platform SuperAdmin only. Used for the company field of forms: everyone else works in their own company, which the form
 * already holds, so there is nothing to choose.
 */
export function SuperAdminOnly({ children }: { children: ReactNode }) {
  return useIsSuperAdmin() ? <>{children}</> : null;
}
