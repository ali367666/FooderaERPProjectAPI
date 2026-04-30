"use client";

import { Suspense } from "react";
import { StockRequestFormPage } from "@/components/stock-requests/stock-request-form-page";

export default function Page() {
  return (
    <Suspense fallback={<p className="text-sm text-muted-foreground">Loading…</p>}>
      <StockRequestFormPage variant="edit" />
    </Suspense>
  );
}
