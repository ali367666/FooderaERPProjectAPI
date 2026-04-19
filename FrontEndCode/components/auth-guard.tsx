"use client";

import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";

const TOKEN_KEY = "token";

export default function AuthGuard({
  children,
}: {
  children: React.ReactNode;
}) {
  const router = useRouter();
  const [token, setToken] = useState<string | null | undefined>(undefined);

  useEffect(() => {
    const sync = () => setToken(localStorage.getItem(TOKEN_KEY));

    sync();
    const intervalId = window.setInterval(sync, 1000);
    window.addEventListener("storage", sync);

    return () => {
      window.clearInterval(intervalId);
      window.removeEventListener("storage", sync);
    };
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
