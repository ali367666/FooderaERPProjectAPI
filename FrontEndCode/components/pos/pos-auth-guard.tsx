"use client";

import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { subscribeToToken } from "@/lib/auth-client";

export default function PosAuthGuard({
  children,
  loginPath = "/pos-login",
}: {
  children: React.ReactNode;
  /** Where a signed-out visitor is sent (staff: the POS login, admins: the admin login). */
  loginPath?: string;
}) {
  const router = useRouter();
  const [token, setToken] = useState<string | null | undefined>(undefined);

  useEffect(() => {
    return subscribeToToken(setToken);
  }, []);

  useEffect(() => {
    if (token === undefined) return;
    if (!token) {
      router.replace(loginPath);
    }
  }, [token, router, loginPath]);

  if (token === undefined || !token) {
    return null;
  }

  return <>{children}</>;
}
