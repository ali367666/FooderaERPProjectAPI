"use client";

import Link from "next/link";
import { ArrowLeft, BarChart3, CalendarCheck, FileBarChart, FileText, Percent, Wallet, type LucideIcon } from "lucide-react";
import { usePermissionSet } from "@/hooks/use-auth-permissions";
import { POS_REPORT_PERMISSION } from "@/lib/pos-reports";

type Entry = { href: string; title: string; description: string; icon: LucideIcon; permission: string };

const ENTRIES: Entry[] = [
  {
    href: "/pos/reports/sales",
    title: "Hesabatlar",
    description: "Satış, məhsul, masa, ofisiant, kateqoriya, çeklər, ləğvlər, hədiyyələr — servis haqqı da daxil.",
    icon: BarChart3,
    permission: POS_REPORT_PERMISSION.reports,
  },
  {
    href: "/pos/reports/z-report",
    title: "Z-hesabatı",
    description: "Seçilmiş dövr üzrə gün yekunu.",
    icon: FileBarChart,
    permission: POS_REPORT_PERMISSION.zReport,
  },
  {
    href: "/pos/reports/service-charge",
    title: "Servis haqqı qeyd etmək",
    description: "Masanı seçin, servis haqqını faizlə və ya manatla yazın — masa bağlananda hesaba əlavə olunur.",
    icon: Percent,
    permission: POS_REPORT_PERMISSION.serviceCharge,
  },
  {
    href: "/pos/reports/documents",
    title: "Satış sənədləri (çeklər)",
    description: "Bağlanmış satışların çekləri — baxmaq və çap etmək.",
    icon: FileText,
    permission: POS_REPORT_PERMISSION.documents,
  },
  {
    href: "/pos/reservations",
    title: "Rezervasiya",
    description: "Masa rezervasiyaları.",
    icon: CalendarCheck,
    permission: POS_REPORT_PERMISSION.reservations,
  },
  {
    href: "/pos/reports/kassa",
    title: "Kassa",
    description: "Nağd daxilolma / çıxış və müştərinin borcunu ödəmək.",
    icon: Wallet,
    permission: POS_REPORT_PERMISSION.kassa,
  },
];

/** "Hesabat" — everything the till staff can look up, in one place (opened from the tables screen). */
export default function PosReportsPage() {
  const permissions = usePermissionSet();
  const entries = ENTRIES.filter((e) => permissions.has(e.permission));

  return (
    <div className="p-4 sm:p-6">
      <Link
        href="/pos"
        className="mb-4 inline-flex items-center gap-1 text-sm text-muted-foreground hover:text-foreground"
      >
        <ArrowLeft className="h-4 w-4" />
        Masalar
      </Link>
      <h1 className="mb-4 text-xl font-bold">Hesabat</h1>

      {entries.length === 0 ? (
        <p className="text-sm text-muted-foreground">Bu hesab üçün açıq hesabat yoxdur.</p>
      ) : (
        <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-3">
          {entries.map((e) => {
            const Icon = e.icon;
            return (
              <Link
                key={e.href}
                href={e.href}
                className="flex items-start gap-3 rounded-xl border bg-card p-4 transition-colors hover:border-primary/50 hover:bg-muted/40"
              >
                <Icon className="mt-0.5 h-6 w-6 shrink-0 text-primary" />
                <span>
                  <span className="block font-semibold">{e.title}</span>
                  <span className="mt-0.5 block text-sm text-muted-foreground">{e.description}</span>
                </span>
              </Link>
            );
          })}
        </div>
      )}
    </div>
  );
}
