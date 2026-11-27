"use client";

import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import { QRCodeSVG } from "qrcode.react";
import { Copy, ExternalLink, Printer, RefreshCw } from "lucide-react";
import { toast } from "sonner";
import { Button } from "@/components/ui/button";
import { useSelectedCompany } from "@/contexts/selected-company-context";
import { filterBySelectedCompany } from "@/lib/company-scope-utils";
import { getQrMenuStatus, publishQrMenu, type QrMenuStatus, type QrMenuTable } from "@/lib/services/qr-menu-service";
import { getRestaurants, type Restaurant } from "@/lib/services/restaurant-service";

// Prints just one QR code (and its caption) on a page of its own.
function printQr(svg: string | undefined, title: string, caption: string, url: string) {
  if (!svg) return;
  const w = window.open("", "_blank", "width=480,height=640");
  if (!w) {
    toast.error("Çap pəncərəsi açılmadı — brauzerin popup bloklamasını söndürün.");
    return;
  }
  w.document.write(`<!doctype html><html><head><meta charset="utf-8" /><title>${title} — QR Menyu</title>
<style>
  body { margin: 0; padding: 32px; font-family: sans-serif; text-align: center; }
  svg { width: 320px; height: 320px; }
  h1 { font-size: 24px; margin: 24px 0 4px; }
  h2 { font-size: 20px; margin: 0 0 8px; color: #333; }
  p { margin: 0; color: #555; word-break: break-all; font-size: 13px; }
</style></head><body>
  ${svg}
  <h1>${title}</h1>
  ${caption ? `<h2>${caption}</h2>` : ""}
  <p>${url}</p>
</body></html>`);
  w.document.close();
  w.focus();
  w.print();
  w.close();
}

/** One table's own QR code: the link carries the table's token, so a scan tells which table it came from. */
function TableQr({ restaurant, table, origin }: { restaurant: Restaurant; table: QrMenuTable; origin: string }) {
  const qrRef = useRef<HTMLDivElement>(null);
  const url = `${origin}/menu/${restaurant.id}?t=${table.qrToken}`;

  return (
    <div className="flex flex-col items-center gap-2 rounded-lg border p-3">
      <p className="text-sm font-medium">{table.name}</p>
      {table.sectionName && <p className="text-xs text-muted-foreground">{table.sectionName}</p>}
      <div ref={qrRef} className="rounded bg-white p-2">
        <QRCodeSVG value={url} size={120} />
      </div>
      <Button
        size="sm"
        variant="outline"
        onClick={() => printQr(qrRef.current?.innerHTML, restaurant.name, table.name, url)}
      >
        <Printer className="mr-1 h-4 w-4" />
        Çap et
      </Button>
    </div>
  );
}

function QrCard({ restaurant, origin }: { restaurant: Restaurant; origin: string }) {
  const qrRef = useRef<HTMLDivElement>(null);
  const url = `${origin}/menu/${restaurant.id}`;
  const [status, setStatus] = useState<QrMenuStatus | null>(null);
  const [publishing, setPublishing] = useState(false);
  const [showTables, setShowTables] = useState(false);

  useEffect(() => {
    let cancelled = false;
    getQrMenuStatus(restaurant.id)
      .then((s) => {
        if (!cancelled) setStatus(s);
      })
      .catch((e) => toast.error(e instanceof Error ? e.message : "QR menyu məlumatı yüklənmədi."));
    return () => {
      cancelled = true;
    };
  }, [restaurant.id]);

  const copy = async () => {
    try {
      await navigator.clipboard.writeText(url);
      toast.success("Link kopyalandı.");
    } catch {
      toast.error("Link kopyalanmadı.");
    }
  };

  // "Update QR": the codes start showing the current products and prices.
  const publish = async () => {
    setPublishing(true);
    try {
      setStatus(await publishQrMenu(restaurant.id));
      toast.success("QR menyu yeniləndi.");
    } catch (e) {
      toast.error(e instanceof Error ? e.message : "QR menyu yenilənmədi.");
    } finally {
      setPublishing(false);
    }
  };

  const publishedLabel = status?.publishedAtUtc
    ? `Son yenilənmə: ${new Date(status.publishedAtUtc).toLocaleString("az-AZ")}`
    : "Hələ yenilənməyib — canlı menyu göstərilir";

  return (
    <div className="flex flex-col items-center gap-3 rounded-xl border bg-card p-5">
      <h2 className="text-base font-semibold">{restaurant.name}</h2>
      <div ref={qrRef} className="rounded-lg bg-white p-3">
        <QRCodeSVG value={url} size={200} />
      </div>
      <p className="max-w-full break-all text-center text-xs text-muted-foreground">{url}</p>

      <div className="flex flex-col items-center gap-1">
        <Button
          size="sm"
          variant={status?.hasPendingChanges ? "default" : "outline"}
          disabled={publishing}
          onClick={() => void publish()}
        >
          <RefreshCw className={`mr-1 h-4 w-4 ${publishing ? "animate-spin" : ""}`} />
          QR-u yenilə
        </Button>
        <p className="text-xs text-muted-foreground">{publishedLabel}</p>
        {status?.hasPendingChanges && (
          <p className="text-xs font-medium text-amber-600">Menyuda dəyişiklik var — yeniləyin.</p>
        )}
      </div>

      <div className="flex flex-wrap justify-center gap-2">
        <Button size="sm" onClick={() => printQr(qrRef.current?.innerHTML, restaurant.name, "", url)}>
          <Printer className="mr-1 h-4 w-4" />
          Çap et
        </Button>
        <Button size="sm" variant="outline" onClick={() => void copy()}>
          <Copy className="mr-1 h-4 w-4" />
          Linki kopyala
        </Button>
        <Button size="sm" variant="outline" asChild>
          <a href={url} target="_blank" rel="noreferrer">
            <ExternalLink className="mr-1 h-4 w-4" />
            Menyuya bax
          </a>
        </Button>
      </div>

      {status && status.tables.length > 0 && (
        <div className="w-full border-t pt-3">
          <Button size="sm" variant="ghost" className="w-full" onClick={() => setShowTables((v) => !v)}>
            {showTables ? "Masa QR kodlarını gizlət" : `Masa QR kodları (${status.tables.length})`}
          </Button>
          {showTables && (
            <div className="mt-3 grid grid-cols-2 gap-3">
              {status.tables.map((t) => (
                <TableQr key={t.id} restaurant={restaurant} table={t} origin={origin} />
              ))}
            </div>
          )}
        </div>
      )}
    </div>
  );
}

export default function QrMenuPage() {
  const { selectedCompanyId } = useSelectedCompany();
  const [restaurants, setRestaurants] = useState<Restaurant[]>([]);
  const [loading, setLoading] = useState(true);
  const [origin, setOrigin] = useState("");

  const load = useCallback(async () => {
    setLoading(true);
    try {
      setRestaurants(await getRestaurants());
    } catch (e) {
      toast.error(e instanceof Error ? e.message : "Filiallar yüklənmədi.");
      setRestaurants([]);
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    setOrigin(window.location.origin);
    void load();
  }, [load]);

  const visible = useMemo(
    () => filterBySelectedCompany(restaurants, selectedCompanyId, (r) => r.companyId),
    [restaurants, selectedCompanyId],
  );

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-3xl font-bold text-foreground">QR Menyu</h1>
        <p className="mt-1 text-muted-foreground">
          Hər filialın menyusunu müştərinin telefonunda açan QR kod; hər masanın da öz QR kodu var. Məhsul və ya qiymət dəyişəndə “QR-u yenilə” düyməsinə basın.
        </p>
      </div>

      {loading ? (
        <p className="text-sm text-muted-foreground">Yüklənir…</p>
      ) : visible.length === 0 ? (
        <p className="text-sm text-muted-foreground">Filial tapılmadı.</p>
      ) : (
        <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 xl:grid-cols-3">
          {visible.map((r) => (
            <QrCard key={r.id} restaurant={r} origin={origin} />
          ))}
        </div>
      )}
    </div>
  );
}
