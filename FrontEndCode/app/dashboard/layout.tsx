"use client";

import { DashboardLicenseBadge } from "@/components/dashboard-license-badge";
import AuthGuard from "@/components/auth-guard";
import { AppSidebar } from "@/components/app-sidebar";
import { DashboardCompanyToolbar } from "@/components/dashboard-company-toolbar";
import { DashboardBranchToolbar } from "@/components/dashboard-branch-toolbar";
import { DashboardPermissionGuard } from "@/components/dashboard-permission-guard";
import { Toaster } from "@/components/ui/sonner";
import { PageFindHighlighter } from "@/components/page-find-highlighter";
import { SectionTabs } from "@/components/section-tabs";
import { SelectedCompanyProvider } from "@/contexts/selected-company-context";
import { SelectedRestaurantProvider } from "@/contexts/selected-restaurant-context";
import { MyProfileProvider } from "@/contexts/my-profile-context";
import { ProfileCorner } from "@/components/profile/profile-corner";
import { useSidebarCollapsed } from "@/hooks/use-sidebar-collapsed";
import { cn } from "@/lib/utils";

export default function DashboardLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  const [sidebarCollapsed, setSidebarCollapsed] = useSidebarCollapsed();

  return (
    <AuthGuard>
      <SelectedCompanyProvider>
        <SelectedRestaurantProvider>
          <MyProfileProvider>
            <div className="min-h-screen bg-background">
              <AppSidebar collapsed={sidebarCollapsed} onCollapsedChange={setSidebarCollapsed} />
              <main
                className={cn(
                  "min-h-screen p-6 transition-all duration-300",
                  sidebarCollapsed ? "lg:pl-20" : "lg:pl-64",
                )}
              >
                <div className="mb-2 flex flex-col items-end gap-1.5">
                  <ProfileCorner />
                  <DashboardLicenseBadge />
                </div>
                <DashboardCompanyToolbar />
                <DashboardBranchToolbar />
                <Toaster richColors closeButton position="top-center" />
                <PageFindHighlighter />
                <SectionTabs />
                <DashboardPermissionGuard>{children}</DashboardPermissionGuard>
              </main>
            </div>
          </MyProfileProvider>
        </SelectedRestaurantProvider>
      </SelectedCompanyProvider>
    </AuthGuard>
  );
}