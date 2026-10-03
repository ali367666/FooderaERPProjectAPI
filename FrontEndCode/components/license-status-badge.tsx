"use client";

import { useEffect, useState } from "react";
import { Globe, MonitorSmartphone } from "lucide-react";
import { cn } from "@/lib/utils";
import { getMyLicenseStatus, type LicenseStatus } from "@/lib/services/license-service";
import { useIsSuperAdmin } from "@/hooks/use-auth-permissions";

/**
 * Online / Offline indicator. Online = the company's remote-access licence is active (shows days
 * left, amber in the last 3 days). Offline = only registered devices can use the system.
 */
export function LicenseStatusBadge({ className }: { className?: string }) {
  const [status, setStatus] = useState<LicenseStatus | null>(null);
  const isSuperAdmin = useIsSuperAdmin();

  useEffect(() => {
    // The platform admin isn't bound by any licence.
    if (isSuperAdmin) return;
    let cancelled = false;
    const load = () =>
      getMyLicenseStatus()
        .then((s) => !cancelled && setStatus(s))
        .catch(() => !cancelled && setStatus(null));
    void load();
    const id = window.setInterval(load, 10 * 60 * 1000);
    return () => {
      cancelled = true;
      window.clearInterval(id);
    };
  }, [isSuperAdmin]);

  if (isSuperAdmin || !status) return null;

  if (status.isLocalInstallation) {
    const soon = status.licenseKeyValid && status.daysLeft != null && status.daysLeft <= 3;
    return (
      <span
        title={
          status.licenseKeyValid
            ? `Yerli quraşdırma — lisenziya açarının bitməsinə ${status.daysLeft} gün qalıb.`
            : "Lisenziya açarının vaxtı bitib — yeni açar daxil edin."
        }
        className={cn(
          "inline-flex items-center gap-1.5 rounded-md px-2 py-1 text-xs font-medium",
          !status.licenseKeyValid ? "bg-red-100 text-red-800" : soon ? "bg-amber-100 text-amber-800" : "bg-slate-100 text-slate-700",
          className,
        )}
      >
        <MonitorSmartphone className="h-3.5 w-3.5" />
        {status.licenseKeyValid ? `Lisenziya · ${status.daysLeft} gün` : "Lisenziya bitib"}
      </span>
    );
  }

  const online = status.remoteAccessActive;
  const expiringSoon = online && status.daysLeft != null && status.daysLeft <= 3;

  return (
    <span
      title={
        online
          ? `Online — sistemə istənilən cihazdan daxil olmaq olar. Lisenziyanın bitməsinə ${status.daysLeft} gün qalıb.`
          : "Offline — sistemə yalnız restoranın qeydiyyatlı cihazlarından daxil olmaq olar."
      }
      className={cn(
        "inline-flex items-center gap-1.5 rounded-md px-2 py-1 text-xs font-medium",
        online
          ? expiringSoon
            ? "bg-amber-100 text-amber-800"
            : "bg-emerald-100 text-emerald-800"
          : "bg-slate-100 text-slate-700",
        className,
      )}
    >
      {online ? <Globe className="h-3.5 w-3.5" /> : <MonitorSmartphone className="h-3.5 w-3.5" />}
      {online ? `Online · ${status.daysLeft} gün` : "Offline"}
    </span>
  );
}
