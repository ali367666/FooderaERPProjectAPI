"use client";

import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { subscribeToToken } from "@/lib/auth-client";

export default function AuthGuard({
  children,
}: {
  children: React.ReactNode;
}) {
  const router = useRouter();
  const [token, setToken] = useState<string | null | undefined>(undefined);

  useEffect(() => {
    return subscribeToToken(setToken);
  }, []);

  useEffect(() => {
    if (token === undefined) return;
    if (!token) {
      router.replace("/login");
    }
  }, [token, router]);

  if (token === undefined || !token) {
    return null;
  }

  return <>{children}</>;
}
