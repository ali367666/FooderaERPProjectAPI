"use client";

import Link from "next/link";
import { ArrowLeft } from "lucide-react";
import { SelectedCompanyProvider } from "@/contexts/selected-company-context";
import { SelectedRestaurantProvider } from "@/contexts/selected-restaurant-context";

/**
 * Hosts a report page inside the POS: staff who sign in at the till cannot open the admin panel, so
 * the same report pages are served under /pos/reports. They expect the company / branch pickers
 * the dashboard provides, so those providers wrap them here.
 */
export function PosReportShell({ children }: { children: React.ReactNode }) {
  return (
    <SelectedCompanyProvider>
      <SelectedRestaurantProvider>
        <div className="p-4 sm:p-6">
          <Link
            href="/pos/reports"
            className="mb-4 inline-flex items-center gap-1 text-sm text-muted-foreground hover:text-foreground"
          >
            <ArrowLeft className="h-4 w-4" />
            Hesabat
          </Link>
          {children}
        </div>
      </SelectedRestaurantProvider>
    </SelectedCompanyProvider>
  );
}
