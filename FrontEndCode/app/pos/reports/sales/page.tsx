"use client";

import { PosReportShell } from "@/components/pos/pos-report-shell";
import SalesReportsPage from "@/app/dashboard/reports/page";

export default function PosSalesReportsPage() {
  return (
    <PosReportShell>
      <SalesReportsPage />
    </PosReportShell>
  );
}
