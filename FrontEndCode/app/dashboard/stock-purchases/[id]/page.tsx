"use client";

import { use } from "react";
import { useSearchParams } from "next/navigation";
import { StockPurchaseFormPage } from "@/components/stock-purchases/stock-purchase-form-page";

export default function Page({ params }: { params: Promise<{ id: string }> }) {
  const { id } = use(params);
  const searchParams = useSearchParams();
  const mode = (searchParams.get("mode") as "edit" | "view") ?? "view";
  return <StockPurchaseFormPage id={Number(id)} mode={mode} />;
}
