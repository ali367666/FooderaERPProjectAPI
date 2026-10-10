"use client";

import { PosReportShell } from "@/components/pos/pos-report-shell";
import SalesDocumentsPage from "@/app/dashboard/sales-documents/page";

export default function PosSalesDocumentsPage() {
  return (
    <PosReportShell>
      <SalesDocumentsPage />
    </PosReportShell>
  );
}
