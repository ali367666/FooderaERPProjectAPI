"use client";

import { useEffect } from "react";
import { useRouter } from "next/navigation";
import { useAuth } from "@/context/auth-context";

export default function HomePage() {
  const router = useRouter();
  const { token, loading } = useAuth();

  useEffect(() => {
    if (loading) return;
    router.replace(token ? "/dashboard" : "/login");
  }, [token, loading, router]);

  return null;
}
