"use client";

import { PosReportShell } from "@/components/pos/pos-report-shell";
import ZReportPage from "@/app/dashboard/z-report/page";

export default function PosZReportPage() {
  return (
    <PosReportShell>
      <ZReportPage />
    </PosReportShell>
  );
}
