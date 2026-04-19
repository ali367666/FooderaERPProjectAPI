"use client";

import { useEffect } from "react";
import { useRouter } from "next/navigation";
import { subscribeToToken } from "@/lib/auth-client";

export default function HomePage() {
  const router = useRouter();

  useEffect(() => {
    return subscribeToToken((token) => {
      router.replace(token ? "/dashboard" : "/login");
    });
  }, [router]);

  return null;
}
