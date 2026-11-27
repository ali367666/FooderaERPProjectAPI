"use client";

import { useRouter } from "next/navigation";
import PosTablesPage from "@/app/pos/page";
import { PosDayReportModal } from "@/components/pos/pos-day-report-modal";

/**
 * /pos/reports opened directly (typed URL, refresh, bookmark): there is no page underneath to return
 * to, so the tables screen is drawn as the background and closing the modal lands on it. In-app
 * navigation is caught by app/pos/@modal/(.)reports instead.
 */
export default function PosReportsPage() {
  const router = useRouter();
  return (
    <>
      <PosTablesPage />
      <PosDayReportModal onClose={() => router.replace("/pos")} />
    </>
  );
}
