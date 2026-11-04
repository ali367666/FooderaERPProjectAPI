"use client";

import { LicenseStatusBadge } from "@/components/license-status-badge";
import { useSelectedCompany } from "@/contexts/selected-company-context";

/** The dashboard's licence badge follows the company picked in the company filter, not the signed-in user's own company. */
export function DashboardLicenseBadge() {
  const { selectedCompanyId } = useSelectedCompany();
  return <LicenseStatusBadge companyId={selectedCompanyId} />;
}
