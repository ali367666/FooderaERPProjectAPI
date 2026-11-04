"use client";

import { useMemo } from "react";
import { useIsSuperAdmin, usePermissionSet } from "@/hooks/use-auth-permissions";

const ALL: ReadonlySet<string> = {
  has: () => true,
  size: Number.MAX_SAFE_INTEGER,
  forEach: () => undefined,
  entries: () => new Set<string>().entries() as unknown as SetIterator<[string, string]>,
  keys: () => new Set<string>().keys(),
  values: () => new Set<string>().values(),
  [Symbol.iterator]: () => new Set<string>()[Symbol.iterator](),
} as unknown as ReadonlySet<string>;

/** The store screens' permission set: the platform SuperAdmin may do everything there (the server still checks), everyone else uses their own. */
export function useStorePermissions(): ReadonlySet<string> {
  const own = usePermissionSet();
  const isSuperAdmin = useIsSuperAdmin();
  return useMemo(() => (isSuperAdmin ? ALL : own), [isSuperAdmin, own]);
}
