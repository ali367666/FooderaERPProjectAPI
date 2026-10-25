"use client";

import { useCallback, useEffect, useRef, useState } from "react";
import { toast } from "sonner";
import { Clock, RefreshCw } from "lucide-react";
import { Button } from "@/components/ui/button";
import { cn } from "@/lib/utils";
import { PosEmpty, PosPage, PosPageError, PosPageLoading, PosTileGrid, posTileClass } from "@/components/pos/pos-page";
import { formatElapsed, orderStatusBg } from "@/lib/pos-order-status";
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

/** Tile colour like a busy table: any line cooking → orange, all ready → green, else waiting. */
function groupBg(group: KitchenOrderGroupDto): string {
  const statuses = group.lines.map((l) => l.kitchenStatus);
  if (statuses.includes("InPreparation")) return orderStatusBg("in_preparation");
  if (statuses.length > 0 && statuses.every((s) => s === "Ready" || s === "Served")) return orderStatusBg("ready");
  return orderStatusBg("open");
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

  if (loading || terminal === undefined) return <PosPageLoading />;

  return (
    <PosPage
      title="Mətbəx"
      actions={
        <Button variant="outline" size="sm" onClick={() => void load()}>
          <RefreshCw className="h-4 w-4" />
        </Button>
      }
    >
      {error && <PosPageError message={error} />}

      {groups.length === 0 && !error && <PosEmpty>Hazırlanacaq sifariş yoxdur.</PosEmpty>}

      <PosTileGrid wide>
        {groups.map((group) => (
          <div key={group.orderId} className={cn(posTileClass(groupBg(group)), "justify-start")}>
            <span className="max-w-full truncate text-lg font-bold leading-none">{group.tableName ?? group.orderNumber}</span>
            <span className="flex items-center gap-1 text-[11px] text-white/80">
              <Clock className="h-3 w-3" />
              {formatElapsed(group.createdAt, now)}
            </span>
            <div className="mt-1.5 w-full space-y-1.5">
              {group.lines.map((line) => (
                <div key={line.orderLineId} className="rounded-lg bg-white/15 px-2 py-1.5 text-left">
                  <div className="flex items-center justify-between gap-2">
                    <span className="text-sm font-semibold">
                      {line.quantity} × {line.menuItemName}
                    </span>
                    <span className="shrink-0 text-[11px] font-medium text-white/90">{statusLabel(line.kitchenStatus)}</span>
                  </div>
                  {line.note && <p className="text-[11px] italic text-white/75">{line.note}</p>}
                  {line.holdUntilUtc && new Date(line.holdUntilUtc).getTime() > now && (
                    <p className="flex items-center gap-1 text-[11px] font-medium text-white">
                      <Clock className="h-3 w-3" />
                      Gözlədə — {Math.ceil((new Date(line.holdUntilUtc).getTime() - now) / 60000)} dəq sonra
                    </p>
                  )}
                  {canManage && line.kitchenStatus === "Pending" && canStart && (
                    <Button
                      size="sm"
                      variant="secondary"
                      className="mt-1.5 w-full"
                      disabled={busyLineId === line.orderLineId}
                      onClick={() => void handleStart(line.orderLineId)}
                    >
                      Başla
                    </Button>
                  )}
                  {canManage && line.kitchenStatus === "InPreparation" && canMarkReady && (
                    <Button
                      size="sm"
                      variant="secondary"
                      className="mt-1.5 w-full"
                      disabled={busyLineId === line.orderLineId}
                      onClick={() => void handleReady(line.orderLineId)}
                    >
                      Hazırdır
                    </Button>
                  )}
                </div>
              ))}
            </div>
          </div>
        ))}
      </PosTileGrid>
    </PosPage>
  );
}
