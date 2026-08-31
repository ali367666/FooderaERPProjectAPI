"use client";

import { useCallback, useEffect, useRef, useState } from "react";
import { toast } from "sonner";
import { Clock, RefreshCw } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { cn } from "@/lib/utils";
import { getPosTerminalContext, type PosTerminalContext } from "@/lib/pos-terminal-client";
import { useHasPermission } from "@/hooks/use-auth-permissions";
import {
  getCompanySettingsBranding,
  type CompanySettingsBranding,
} from "@/lib/services/company-settings-service";
import { playPosAlert } from "@/lib/pos-sound-alert";
import {
  getKitchenOrders,
  startPreparingKitchenLine,
  markKitchenLineReady,
  type KitchenOrderGroupDto,
  type KitchenLineStatus,
} from "@/lib/services/kitchen-service";

const POLL_INTERVAL_MS = 8000;

function statusLabel(status: KitchenLineStatus): string {
  switch (status) {
    case "Pending":
      return "Gözləyir";
    case "InPreparation":
      return "Hazırlanır";
    case "Ready":
      return "Hazır";
    case "Served":
      return "Verildi";
    default:
      return status;
  }
}

function formatElapsed(createdAt: string, now: number): string {
  const created = new Date(createdAt).getTime();
  if (!Number.isFinite(created)) return "";
  const minutes = Math.max(0, Math.floor((now - created) / 60000));
  if (minutes < 60) return `${minutes} dəq`;
  const hours = Math.floor(minutes / 60);
  const remMinutes = minutes % 60;
  return `${hours} saat ${remMinutes} dəq`;
}

function statusBadgeClass(status: KitchenLineStatus): string {
  switch (status) {
    case "Pending":
      return "bg-slate-100 text-slate-700";
    case "InPreparation":
      return "bg-orange-100 text-orange-800";
    case "Ready":
      return "bg-emerald-100 text-emerald-800";
    case "Served":
      return "bg-pink-100 text-pink-700";
    default:
      return "bg-slate-100 text-slate-700";
  }
}

export default function PosKitchenPage() {
  const [terminal, setTerminal] = useState<PosTerminalContext | null | undefined>(undefined);
  const [groups, setGroups] = useState<KitchenOrderGroupDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [busyLineId, setBusyLineId] = useState<number | null>(null);
  const [branding, setBranding] = useState<CompanySettingsBranding | null>(null);
  const [now, setNow] = useState(() => Date.now());

  const seenPendingIds = useRef<Set<number> | null>(null);

  useEffect(() => {
    const id = window.setInterval(() => setNow(Date.now()), 1000);
    return () => window.clearInterval(id);
  }, []);

  const canManage = useHasPermission("Kitchen.StartPreparing") || useHasPermission("Kitchen.MarkReady");
  const canStart = useHasPermission("Kitchen.StartPreparing");
  const canMarkReady = useHasPermission("Kitchen.MarkReady");

  useEffect(() => {
    setTerminal(getPosTerminalContext());
  }, []);

  useEffect(() => {
    if (!terminal?.companyId) return;
    getCompanySettingsBranding(terminal.companyId)
      .then(setBranding)
      .catch(() => setBranding(null));
  }, [terminal]);

  const load = useCallback(async () => {
    if (!terminal?.restaurantId) return;
    setLoading(true);
    setError(null);
    try {
      const data = await getKitchenOrders(terminal.restaurantId);
      setGroups(data);

      const pendingIds = new Set<number>();
      for (const group of data) {
        for (const line of group.lines) {
          if (line.kitchenStatus === "Pending") pendingIds.add(line.orderLineId);
        }
      }
      if (seenPendingIds.current !== null) {
        const hasNewPending = [...pendingIds].some((id) => !seenPendingIds.current!.has(id));
        if (hasNewPending) playPosAlert(branding);
      }
      seenPendingIds.current = pendingIds;
    } catch (err) {
      setError(err instanceof Error ? err.message : "Mətbəx sifarişləri yüklənə bilmədi");
    } finally {
      setLoading(false);
    }
  }, [terminal, branding]);

  useEffect(() => {
    if (terminal === undefined) return;
    void load();
  }, [terminal, load]);

  useEffect(() => {
    if (terminal === undefined || terminal === null) return;
    const interval = setInterval(() => void load(), POLL_INTERVAL_MS);
    return () => clearInterval(interval);
  }, [terminal, load]);

  const handleStart = async (lineId: number) => {
    setBusyLineId(lineId);
    try {
      await startPreparingKitchenLine(lineId);
      await load();
    } catch (err) {
      toast.error(err instanceof Error ? err.message : "Əməliyyat uğursuz oldu");
    } finally {
      setBusyLineId(null);
    }
  };

  const handleReady = async (lineId: number) => {
    setBusyLineId(lineId);
    try {
      await markKitchenLineReady(lineId);
      await load();
    } catch (err) {
      toast.error(err instanceof Error ? err.message : "Əməliyyat uğursuz oldu");
    } finally {
      setBusyLineId(null);
    }
  };

  if (loading || terminal === undefined) {
    return <div className="flex h-full items-center justify-center text-muted-foreground">Yüklənir...</div>;
  }

  return (
    <div className="p-4 sm:p-6">
      <div className="mb-4 flex items-center justify-between">
        <h1 className="text-xl font-bold">Mətbəx</h1>
        <Button variant="outline" size="sm" onClick={() => void load()}>
          <RefreshCw className="h-4 w-4" />
        </Button>
      </div>

      {error && (
        <div className="mb-4 rounded-md border border-destructive/30 bg-destructive/10 px-4 py-3 text-sm text-destructive">
          {error}
        </div>
      )}

      {groups.length === 0 && !error && (
        <p className="text-sm text-muted-foreground">Hazırlanacaq sifariş yoxdur.</p>
      )}

      <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-3">
        {groups.map((group) => (
          <div key={group.orderId} className="rounded-xl border bg-card p-4 shadow-sm">
            <div className="mb-2 flex items-center justify-between">
              <span className="font-bold">{group.tableName ?? group.orderNumber}</span>
              <span className="flex items-center gap-1 text-xs text-muted-foreground">
                <Clock className="h-3 w-3" />
                {formatElapsed(group.createdAt, now)}
              </span>
            </div>
            <div className="space-y-2">
              {group.lines.map((line) => (
                <div key={line.orderLineId} className="rounded-lg border p-2">
                  <div className="flex items-center justify-between gap-2">
                    <span className="text-sm font-medium">
                      {line.quantity} × {line.menuItemName}
                    </span>
                    <Badge className={cn("shrink-0", statusBadgeClass(line.kitchenStatus))}>
                      {statusLabel(line.kitchenStatus)}
                    </Badge>
                  </div>
                  {line.note && <p className="mt-1 text-xs italic text-muted-foreground">{line.note}</p>}
                  {canManage && (
                    <div className="mt-2 flex gap-2">
                      {line.kitchenStatus === "Pending" && canStart && (
                        <Button
                          size="sm"
                          disabled={busyLineId === line.orderLineId}
                          onClick={() => void handleStart(line.orderLineId)}
                        >
                          Başla
                        </Button>
                      )}
                      {line.kitchenStatus === "InPreparation" && canMarkReady && (
                        <Button
                          size="sm"
                          disabled={busyLineId === line.orderLineId}
                          onClick={() => void handleReady(line.orderLineId)}
                        >
                          Hazırdır
                        </Button>
                      )}
                    </div>
                  )}
                </div>
              ))}
            </div>
          </div>
        ))}
      </div>
    </div>
  );
}
