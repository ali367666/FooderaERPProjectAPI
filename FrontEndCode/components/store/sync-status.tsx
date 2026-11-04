"use client";

import { useCallback, useEffect, useState } from "react";
import { CloudOff, RefreshCw, Wifi } from "lucide-react";
import { toast } from "sonner";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Dialog, DialogContent, DialogDescription, DialogHeader, DialogTitle } from "@/components/ui/dialog";
import { formatCurrency } from "@/lib/format-currency";
import { getOfflineCatalogTime } from "@/lib/store/offline-catalog";
import { listQueuedSales, removeQueuedSale, retryQueuedSale, subscribeQueue, syncQueuedSales, type QueuedSale } from "@/lib/store/offline-queue";

const STATUS_LABEL: Record<QueuedSale["status"], string> = {
  Pending: "Gözləyir",
  Syncing: "Göndərilir",
  Failed: "Xəta",
  Conflict: "Konflikt",
};

/** Connection state and the sales waiting to reach the server; sends them when the connection returns. */
export default function SyncStatus() {
  const [online, setOnline] = useState(true);
  const [items, setItems] = useState<QueuedSale[]>([]);
  const [open, setOpen] = useState(false);
  const [busy, setBusy] = useState(false);
  const [catalogTime, setCatalogTime] = useState<string | null>(null);

  const sync = useCallback(async (announce: boolean) => {
    setBusy(true);
    try {
      const { synced, failed } = await syncQueuedSales();
      if (synced > 0) toast.success(`${synced} satış serverə göndərildi.`);
      if (failed > 0) toast.error(`${failed} satış qəbul edilmədi — “Sinxronizasiya” pəncərəsində baxın.`);
      if (announce && synced === 0 && failed === 0) toast.info("Göndəriləcək satış yoxdur və ya bağlantı yoxdur.");
    } finally {
      setBusy(false);
    }
  }, []);

  useEffect(() => {
    setOnline(navigator.onLine);
    setItems(listQueuedSales());
    const unsubscribe = subscribeQueue(() => setItems(listQueuedSales()));
    const goOnline = () => {
      setOnline(true);
      void sync(false);
    };
    const goOffline = () => setOnline(false);
    window.addEventListener("online", goOnline);
    window.addEventListener("offline", goOffline);
    // A server that was down still answers again at some point, even if the browser never said "online".
    const timer = setInterval(() => {
      if (listQueuedSales().some((x) => x.status === "Pending")) void sync(false);
    }, 30_000);
    void sync(false);
    return () => {
      unsubscribe();
      window.removeEventListener("online", goOnline);
      window.removeEventListener("offline", goOffline);
      clearInterval(timer);
    };
  }, [sync]);

  useEffect(() => {
    if (open) void getOfflineCatalogTime().then(setCatalogTime);
  }, [open]);

  const waiting = items.length;

  return (
    <>
      <Button variant="ghost" size="sm" className="gap-1" onClick={() => setOpen(true)} aria-label="Sinxronizasiya">
        {online ? <Wifi className="h-4 w-4 text-emerald-600" /> : <CloudOff className="h-4 w-4 text-red-600" />}
        {waiting > 0 && <Badge variant="destructive">{waiting}</Badge>}
      </Button>

      <Dialog open={open} onOpenChange={setOpen}>
        <DialogContent className="max-w-lg">
          <DialogHeader>
            <DialogTitle>Sinxronizasiya</DialogTitle>
            <DialogDescription>
              {online ? "Bağlantı var." : "Bağlantı yoxdur — satışlar bu cihazda saxlanılır."} Gözləyən satış: {waiting}.
            </DialogDescription>
          </DialogHeader>

          <p className="rounded-md bg-muted p-2 text-xs text-muted-foreground">
            Oflayn rejimdə məhsul və qiymətlər bu cihazdakı surətdən oxunur (son yenilənmə:{" "}
            {catalogTime ? new Date(catalogTime).toLocaleString("az-AZ") : "hələ yoxdur"}). Anbar qalığı yalnız həmin ana qədər dəqiqdir; satış serverə çatanda qalıq yenidən yoxlanılır və çatışmazlıq varsa satış “Konflikt” kimi qalır.
          </p>

          <div className="max-h-80 space-y-2 overflow-auto">
            {items.length === 0 ? (
              <p className="py-6 text-center text-sm text-muted-foreground">Hər şey göndərilib.</p>
            ) : (
              items.map((x) => (
                <div key={x.operationId} className="rounded-md border p-2 text-sm">
                  <div className="flex items-center justify-between">
                    <span>
                      {new Date(x.createdAt).toLocaleString("az-AZ")} — {formatCurrency(x.total)}
                    </span>
                    <Badge variant={x.status === "Failed" || x.status === "Conflict" ? "destructive" : "secondary"}>{STATUS_LABEL[x.status]}</Badge>
                  </div>
                  {x.error && <p className="mt-1 text-xs text-red-600">{x.error}</p>}
                  {(x.status === "Failed" || x.status === "Conflict") && (
                    <div className="mt-2 flex gap-2">
                      <Button size="sm" variant="outline" onClick={() => retryQueuedSale(x.operationId)}>
                        Yenidən cəhd
                      </Button>
                      <Button size="sm" variant="ghost" className="text-red-600" onClick={() => removeQueuedSale(x.operationId)}>
                        Sil
                      </Button>
                    </div>
                  )}
                </div>
              ))
            )}
          </div>

          <Button disabled={busy || waiting === 0} onClick={() => void sync(true)}>
            <RefreshCw className={busy ? "mr-2 h-4 w-4 animate-spin" : "mr-2 h-4 w-4"} />
            İndi göndər
          </Button>
        </DialogContent>
      </Dialog>
    </>
  );
}
