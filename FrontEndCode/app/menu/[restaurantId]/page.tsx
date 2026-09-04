"use client";

import { useEffect, useMemo, useRef, useState } from "react";
import { useParams } from "next/navigation";
import { Badge } from "@/components/ui/badge";
import { getPublicMenu, type PublicMenu } from "@/lib/services/public-menu-service";

export default function PublicMenuPage() {
  const params = useParams<{ restaurantId: string }>();
  const restaurantId = Number(params.restaurantId);

  const [menu, setMenu] = useState<PublicMenu | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [activeCategoryId, setActiveCategoryId] = useState<number | null>(null);
  const sectionRefs = useRef<Record<number, HTMLDivElement | null>>({});

  useEffect(() => {
    if (!Number.isFinite(restaurantId) || restaurantId <= 0) {
      setError("Yanlış menyu linki.");
      setLoading(false);
      return;
    }
    let cancelled = false;
    (async () => {
      try {
        const data = await getPublicMenu(restaurantId);
        if (cancelled) return;
        setMenu(data);
        setActiveCategoryId(data.categories[0]?.id ?? null);
      } catch {
        if (!cancelled) setError("Menyu tapılmadı və ya yüklənə bilmədi.");
      } finally {
        if (!cancelled) setLoading(false);
      }
    })();
    return () => {
      cancelled = true;
    };
  }, [restaurantId]);

  const accentColor = menu?.productColor || "#0f172a";

  const scrollToCategory = (id: number) => {
    setActiveCategoryId(id);
    sectionRefs.current[id]?.scrollIntoView({ behavior: "smooth", block: "start" });
  };

  const socialLinks = useMemo(() => {
    if (!menu?.socialLinks) return [];
    return menu.socialLinks
      .split(/[\n,]/)
      .map((s) => s.trim())
      .filter(Boolean);
  }, [menu?.socialLinks]);

  if (loading) {
    return (
      <div className="flex min-h-screen items-center justify-center bg-muted/30">
        <p className="text-sm text-muted-foreground">Yüklənir…</p>
      </div>
    );
  }

  if (error || !menu) {
    return (
      <div className="flex min-h-screen items-center justify-center bg-muted/30 p-6 text-center">
        <p className="text-sm text-muted-foreground">{error ?? "Menyu tapılmadı."}</p>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-muted/20 pb-16">
      <div className="sticky top-0 z-10 bg-background shadow-sm">
        <div className="mx-auto flex max-w-2xl flex-col items-center gap-2 px-4 py-5 text-center">
          {menu.logoUrl && (
            // eslint-disable-next-line @next/next/no-img-element
            <img src={menu.logoUrl} alt={menu.restaurantName} className="h-16 w-16 rounded-full object-cover" />
          )}
          <h1 className="text-xl font-bold text-foreground">{menu.restaurantName}</h1>
          {menu.slogan && <p className="text-sm text-muted-foreground">{menu.slogan}</p>}
        </div>

        {menu.categories.length > 0 && (
          <div className="scrollbar-none flex gap-2 overflow-x-auto border-t px-4 py-3">
            {menu.categories.map((c) => (
              <button
                key={c.id}
                onClick={() => scrollToCategory(c.id)}
                className="shrink-0 rounded-full border px-4 py-1.5 text-sm font-medium transition-colors"
                style={
                  activeCategoryId === c.id
                    ? { backgroundColor: accentColor, borderColor: accentColor, color: "#fff" }
                    : { borderColor: "var(--border)" }
                }
              >
                {c.name}
              </button>
            ))}
          </div>
        )}
      </div>

      <div className="mx-auto max-w-2xl px-4">
        {menu.categories.length === 0 && (
          <p className="py-16 text-center text-sm text-muted-foreground">Menyuda hələ məhsul yoxdur.</p>
        )}

        {menu.categories.map((category) => (
          <div
            key={category.id}
            ref={(el) => {
              sectionRefs.current[category.id] = el;
            }}
            className="scroll-mt-32 py-5"
          >
            <div className="mb-3 flex items-center gap-3">
              {category.imageUrl && (
                // eslint-disable-next-line @next/next/no-img-element
                <img
                  src={category.imageUrl}
                  alt=""
                  className="h-10 w-10 shrink-0 rounded-full border object-cover"
                />
              )}
              <h2 className="text-lg font-semibold text-foreground">{category.name}</h2>
            </div>
            <div className="space-y-3">
              {category.items.map((item) => (
                <div
                  key={item.id}
                  className="flex items-start justify-between gap-4 rounded-lg border bg-background p-4"
                >
                  {item.imageUrl && (
                    // eslint-disable-next-line @next/next/no-img-element
                    <img
                      src={item.imageUrl}
                      alt=""
                      className="h-16 w-16 shrink-0 rounded-md border object-cover"
                    />
                  )}
                  <div className="min-w-0 flex-1">
                    <div className="flex items-center gap-2">
                      <p className="font-medium text-foreground">{item.name}</p>
                      {!item.isAvailable && (
                        <Badge className="bg-slate-200 text-slate-700 hover:bg-slate-200">Bitib</Badge>
                      )}
                    </div>
                    {item.description && (
                      <p className="mt-1 text-sm text-muted-foreground">{item.description}</p>
                    )}
                    {item.portion && <p className="mt-1 text-xs text-muted-foreground">{item.portion}</p>}
                  </div>
                  <p className="shrink-0 whitespace-nowrap font-semibold" style={{ color: accentColor }}>
                    {item.price.toFixed(2)} ₼
                  </p>
                </div>
              ))}
            </div>
          </div>
        ))}

        {(menu.contactPhoneNumber || socialLinks.length > 0) && (
          <div className="mt-8 border-t pt-5 text-center text-sm text-muted-foreground">
            {menu.contactPhoneNumber && <p>{menu.contactPhoneNumber}</p>}
            {socialLinks.map((link) => (
              <p key={link}>{link}</p>
            ))}
          </div>
        )}
      </div>
    </div>
  );
}
