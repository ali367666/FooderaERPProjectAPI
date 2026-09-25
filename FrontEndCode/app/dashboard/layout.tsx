"use client";

import AuthGuard from "@/components/auth-guard";
import { AppSidebar } from "@/components/app-sidebar";
import { DashboardCompanyToolbar } from "@/components/dashboard-company-toolbar";
import { DashboardBranchToolbar } from "@/components/dashboard-branch-toolbar";
import { DashboardPermissionGuard } from "@/components/dashboard-permission-guard";
import { Toaster } from "@/components/ui/sonner";
import { SelectedCompanyProvider } from "@/contexts/selected-company-context";
import { SelectedRestaurantProvider } from "@/contexts/selected-restaurant-context";

export default function DashboardLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  return (
    <AuthGuard>
      <SelectedCompanyProvider>
        <SelectedRestaurantProvider>
          <div className="min-h-screen bg-background">
            <AppSidebar />
            <main className="lg:pl-64 min-h-screen p-6">
              <DashboardCompanyToolbar />
              <DashboardBranchToolbar />
              <Toaster richColors closeButton position="top-center" />
              <DashboardPermissionGuard>{children}</DashboardPermissionGuard>
            </main>
          </div>
        </SelectedRestaurantProvider>
      </SelectedCompanyProvider>
    </AuthGuard>
  );
}