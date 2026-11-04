"use client";

import Link from "next/link";
import { useEffect, useState } from "react";
import { Store } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { useSelectedCompany } from "@/contexts/selected-company-context";
import { getStoredToken } from "@/lib/auth-client";
import { getCompanyIdFromToken } from "@/lib/jwt-permissions";
import { formatCurrency } from "@/lib/format-currency";
import { getCompanySettingsBranding } from "@/lib/services/company-settings-service";
import { toDateInputValue } from "@/lib/report-period";
import { getStoreAlerts, getStoreSalesReport, getStoreStockReport, type StoreAlert, type StoreSalesReport, type StoreStockReport } from "@/lib/store/store-service";

/** At the top of the dashboard when the selected company works in Mağaza rejimi: today's store numbers and what needs attention. */
export default function StoreDashboardOverview() {
  const { selectedCompanyId } = useSelectedCompany();
  const [isStore, setIsStore] = useState(false);
  const [sales, setSales] = useState<StoreSalesReport | null>(null);
  const [stock, setStock] = useState<StoreStockReport | null>(null);
  const [alerts, setAlerts] = useState<StoreAlert[]>([]);

  useEffect(() => {
    const companyId = selectedCompanyId ?? getCompanyIdFromToken(getStoredToken());
    if (!companyId) return;
    let cancelled = false;
    (async () => {
      try {
        const branding = await getCompanySettingsBranding(companyId);
        if (cancelled) return;
        setIsStore(branding.moduleDataSecimi === true);
        if (!branding.moduleDataSecimi) return;
        const today = toDateInputValue(new Date());
        const [s, st, a] = await Promise.all([
          getStoreSalesReport({ from: today, to: today }).catch(() => null),
          getStoreStockReport({ from: today, to: today }).catch(() => null),
          getStoreAlerts().catch(() => []),
        ]);
        if (cancelled) return;
        setSales(s);
        setStock(st);
        setAlerts(a);
      } catch {
        if (!cancelled) setIsStore(false);
      }
    })();
    return () => {
      cancelled = true;
    };
  }, [selectedCompanyId]);

  if (!isStore) return null;

  const stat = (label: string, value: string) => (
    <div className="rounded-lg border bg-card p-3">
      <p className="text-xs text-muted-foreground">{label}</p>
      <p className="mt-1 text-lg font-semibold">{value}</p>
    </div>
  );

  return (
    <Card>
      <CardHeader className="flex flex-row items-center justify-between space-y-0">
        <div>
          <CardTitle className="flex items-center gap-2">
            <Store className="h-5 w-5" /> Mağaza — bu gün
          </CardTitle>
          <CardDescription>Seçilmiş şirkət Mağaza rejimindədir.</CardDescription>
        </div>
        <Button asChild>
          <Link href="/dashboard/store/sales">Mağaza satışları</Link>
        </Button>
      </CardHeader>
      <CardContent className="space-y-3">
        <div className="grid grid-cols-2 gap-3 md:grid-cols-4">
          {stat("Satış sayı", String(sales?.salesCount ?? 0))}
          {stat("Xalis satış", formatCurrency(sales?.netSales ?? 0))}
          {stat("Mənfəət", formatCurrency(sales?.profit ?? 0))}
          {stat("Stokun dəyəri", formatCurrency(stock?.totalValue ?? 0))}
        </div>
        {alerts.length > 0 && (
          <ul className="space-y-1 text-sm">
            {alerts.map((a) => (
              <li key={a.kind}>
                <Link href={(a.link ?? "/store/sales").replace(/^\/store\//, "/dashboard/store/")} className="hover:underline">
                  <span className="font-medium">{a.title}:</span> <span className="text-muted-foreground">{a.message}</span>
                </Link>
              </li>
            ))}
          </ul>
        )}
      </CardContent>
    </Card>
  );
}
