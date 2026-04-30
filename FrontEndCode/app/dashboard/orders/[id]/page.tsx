"use client";

import { Suspense } from "react";
import { OrderFormPage } from "@/components/orders/order-form-page";

export default function Page() {
  return (
    <Suspense fallback={<p className="text-sm text-muted-foreground">Loading…</p>}>
      <OrderFormPage variant="edit" />
    </Suspense>
  );
}
