"use client";

import { useRouter } from "next/navigation";
import { PosDayReportModal } from "@/components/pos/pos-day-report-modal";

/**
 * /pos/reports opened from inside the POS (the "Hesabat" button): the report shows as a modal over
 * the page that was open, which stays mounted underneath. Closing goes back to it.
 */
export default function PosReportsModalRoute() {
  const router = useRouter();
  return <PosDayReportModal onClose={() => router.back()} />;
}
