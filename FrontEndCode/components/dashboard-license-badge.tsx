"use client";

import { LicenseStatusBadge } from "@/components/license-status-badge";
import { useSelectedCompany } from "@/contexts/selected-company-context";
import { useIsSuperAdmin } from "@/hooks/use-auth-permissions";

/** The dashboard's licence badge follows the company picked in the company filter, not the signed-in user's own company. */
export function DashboardLicenseBadge() {
  const { selectedCompanyId } = useSelectedCompany();
  const isSuperAdmin = useIsSuperAdmin();
  // Reading a company by id is a platform-only endpoint; everyone else reads the licence of their own account.
  return <LicenseStatusBadge companyId={isSuperAdmin ? selectedCompanyId : null} />;
}
