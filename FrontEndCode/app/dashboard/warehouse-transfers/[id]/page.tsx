"use client";

import { Suspense } from "react";
import { WarehouseTransferFormPage } from "@/components/warehouse-transfers/warehouse-transfer-form-page";

export default function Page() {
  return (
    <Suspense fallback={<p className="text-sm text-muted-foreground">Loading…</p>}>
      <WarehouseTransferFormPage variant="edit" />
    </Suspense>
  );
}
