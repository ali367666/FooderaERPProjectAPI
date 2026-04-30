"use client";

import { useCallback } from "react";
import { useRouter } from "next/navigation";
import { clearStoredAuth } from "@/lib/auth-client";

export function useLogout() {
  const router = useRouter();

  return useCallback(() => {
    clearStoredAuth();
    router.replace("/login");
  }, [router]);
}
