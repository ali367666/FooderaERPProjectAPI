"use client";

import { PosReportShell } from "@/components/pos/pos-report-shell";
import KassaPage from "@/app/dashboard/kassa/page";

export default function PosKassaPage() {
  return (
    <PosReportShell>
      <KassaPage />
    </PosReportShell>
  );
}
