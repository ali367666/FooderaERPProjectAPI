"use client";

import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import { QRCodeSVG } from "qrcode.react";
import { Copy, ExternalLink, Printer } from "lucide-react";
import { toast } from "sonner";
import { Button } from "@/components/ui/button";
import { useSelectedCompany } from "@/contexts/selected-company-context";
import { filterBySelectedCompany } from "@/lib/company-scope-utils";
import { getRestaurants, type Restaurant } from "@/lib/services/restaurant-service";

function QrCard({ restaurant, origin }: { restaurant: Restaurant; origin: string }) {
  const qrRef = useRef<HTMLDivElement>(null);
  const url = `${origin}/menu/${restaurant.id}`;

  const copy = async () => {
    try {
      await navigator.clipboard.writeText(url);
      toast.success("Link kopyalandı.");
    } catch {
      toast.error("Link kopyalanmadı.");
    }
  };

  // Prints just this QR code (and the branch name) on a page of its own.
  const print = () => {
    const svg = qrRef.current?.innerHTML;
    if (!svg) return;
    const w = window.open("", "_blank", "width=480,height=640");
    if (!w) {
      toast.error("Çap pəncərəsi açılmadı — brauzerin popup bloklamasını söndürün.");
      return;
    }
    w.document.write(`<!doctype html><html><head><meta charset="utf-8" /><title>${restaurant.name} — QR Menyu</title>
<style>
  body { margin: 0; padding: 32px; font-family: sans-serif; text-align: center; }
  svg { width: 320px; height: 320px; }
  h1 { font-size: 24px; margin: 24px 0 4px; }
  p { margin: 0; color: #555; word-break: break-all; font-size: 13px; }
</style></head><body>
  ${svg}
  <h1>${restaurant.name}</h1>
  <p>${url}</p>
</body></html>`);
    w.document.close();
    w.focus();
    w.print();
    w.close();
  };

  return (
    <div className="flex flex-col items-center gap-3 rounded-xl border bg-card p-5">
      <h2 className="text-base font-semibold">{restaurant.name}</h2>
      <div ref={qrRef} className="rounded-lg bg-white p-3">
        <QRCodeSVG value={url} size={200} />
      </div>
      <p className="max-w-full break-all text-center text-xs text-muted-foreground">{url}</p>
      <div className="flex flex-wrap justify-center gap-2">
        <Button size="sm" onClick={print}>
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
          Hər filialın menyusunu müştərinin telefonunda açan QR kod. Çap edib masalara və ya qapıya qoyun.
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
