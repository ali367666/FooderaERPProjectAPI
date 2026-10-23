"use client";

import { useEffect, useMemo, useState } from "react";
import { Check, Layers, Lock, Minus, Plus } from "lucide-react";
import { Button } from "@/components/ui/button";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { cn } from "@/lib/utils";
import {
  getMenuItemSetComponents,
  PreparationType,
  type MenuItem,
  type SetComponent,
} from "@/lib/services/menu-item-service";
import type { MenuCategory } from "@/lib/services/menu-category-service";

/** One component of the SET and how many of it go on the order (0 = left out). */
export type SetPick = {
  componentMenuItemId: number;
  defaultQuantity: number;
  quantity: number;
  /** False for a product that needs no preparation: the server adds its line as Ready, and a Ready line can't be removed. */
  removable: boolean;
};

type Props = {
  /** The SET being added; null keeps the dialog closed. */
  item: MenuItem | null;
  /** Menu products and categories — used for each component's picture and its category heading. */
  items: MenuItem[];
  categories: MenuCategory[];
  busy: boolean;
  /** "Pos.EditProductInSale": the count of a component may differ from the set's own count. */
  canChangeCount: boolean;
  /** "Pos.DeleteProductInSale": a component may be left out of the set entirely. */
  canLeaveOut: boolean;
  onClose: () => void;
  onConfirm: (picks: SetPick[]) => void;
};

const maxOf = (c: SetComponent) => Math.max(c.limit ?? c.quantity, c.quantity);

/** Whether the user's permissions allow `quantity` of this component (the set's own count is always allowed). */
export function isSetPickAllowed(
  pick: Pick<SetPick, "defaultQuantity" | "quantity" | "removable">,
  canChangeCount: boolean,
  canLeaveOut: boolean,
): boolean {
  if (pick.quantity === pick.defaultQuantity) return true;
  if (pick.quantity === 0) return canLeaveOut && pick.removable;
  return canChangeCount;
}

/**
 * POS dialog for a SET: the products it holds, grouped by their menu category. The waiter picks which of them
 * (and how many, up to the set's maximum) go on the order; nothing is added until "Əlavə et".
 */
export function SetSelectionDialog({
  item,
  items,
  categories,
  busy,
  canChangeCount,
  canLeaveOut,
  onClose,
  onConfirm,
}: Props) {
  const [components, setComponents] = useState<SetComponent[] | null>(null);
  const [loadError, setLoadError] = useState<string | null>(null);
  const [counts, setCounts] = useState<Record<number, number>>({});
  const [error, setError] = useState<string | null>(null);

  const itemById = useMemo(() => new Map(items.map((i) => [i.id, i])), [items]);
  const removable = (c: SetComponent) => {
    const product = itemById.get(c.componentMenuItemId);
    return product != null && product.preparationType !== PreparationType.None;
  };

  const fullSet = (list: SetComponent[]) =>
    Object.fromEntries(list.map((c) => [c.componentMenuItemId, c.quantity])) as Record<number, number>;
  // What the waiter starts from: only the products that can't be left out.
  const startingSet = (list: SetComponent[]) =>
    Object.fromEntries(list.filter((c) => !removable(c)).map((c) => [c.componentMenuItemId, c.quantity])) as Record<
      number,
      number
    >;

  useEffect(() => {
    setComponents(null);
    setLoadError(null);
    setCounts({});
    setError(null);
    if (!item) return;
    let cancelled = false;
    getMenuItemSetComponents(item.id)
      .then((list) => {
        if (cancelled) return;
        setComponents(list);
        // Who may leave products out starts from an empty set and picks; everyone else starts from the full set.
        setCounts(canLeaveOut ? startingSet(list) : fullSet(list));
      })
      .catch((e) => {
        if (!cancelled) setLoadError(e instanceof Error ? e.message : "Setin tərkibi yüklənmədi.");
      });
    return () => {
      cancelled = true;
    };
    // Reload only when another set is opened; the menu list refreshing must not reset the waiter's picks.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [item, canLeaveOut]);

  const groups = useMemo(() => {
    const byCategory = new Map<number | null, SetComponent[]>();
    for (const c of components ?? []) {
      const categoryId = itemById.get(c.componentMenuItemId)?.menuCategoryId ?? null;
      byCategory.set(categoryId, [...(byCategory.get(categoryId) ?? []), c]);
    }
    const order = new Map(categories.map((c, i) => [c.id, i]));
    return [...byCategory.entries()]
      .map(([categoryId, list]) => ({
        key: categoryId ?? "other",
        name: categories.find((c) => c.id === categoryId)?.name ?? "Digər",
        rank: categoryId == null ? Number.MAX_SAFE_INTEGER : order.get(categoryId) ?? Number.MAX_SAFE_INTEGER - 1,
        list,
      }))
      .sort((a, b) => a.rank - b.rank);
  }, [components, itemById, categories]);

  const countOf = (c: SetComponent) => counts[c.componentMenuItemId] ?? 0;
  const pickedTotal = (components ?? []).reduce((s, c) => s + countOf(c), 0);
  const allowed = (c: SetComponent, quantity: number) =>
    isSetPickAllowed({ defaultQuantity: c.quantity, quantity, removable: removable(c) }, canChangeCount, canLeaveOut);

  const setCount = (c: SetComponent, quantity: number) => {
    if (busy || quantity < 0 || quantity > maxOf(c) || !allowed(c, quantity)) return;
    setError(null);
    setCounts((prev) => ({ ...prev, [c.componentMenuItemId]: quantity }));
  };

  // Tapping a card picks it (1, or the set's count when the count may not change) or leaves it out again.
  const toggle = (c: SetComponent) => {
    const current = countOf(c);
    if (current > 0) setCount(c, 0);
    else setCount(c, canChangeCount ? 1 : c.quantity);
  };

  const handleConfirm = () => {
    if (!components) return;
    if (components.length > 0 && pickedTotal === 0) {
      setError("Setə ən azı bir məhsul seçin.");
      return;
    }
    const picks = components.map((c) => ({
      componentMenuItemId: c.componentMenuItemId,
      defaultQuantity: c.quantity,
      quantity: countOf(c),
      removable: removable(c),
    }));
    if (picks.some((p, i) => p.quantity > maxOf(components[i]) || !allowed(components[i], p.quantity))) {
      setError("Bu seçim üçün icazəniz yoxdur.");
      return;
    }
    onConfirm(picks);
  };

  const locked = !canChangeCount && !canLeaveOut;
  const price = item ? item.stationPrice ?? item.price : 0;

  return (
    <Dialog open={item != null} onOpenChange={(o) => !o && !busy && onClose()}>
      <DialogContent className="flex max-h-[calc(100dvh-1rem)] w-full max-w-[calc(100%-1rem)] flex-col gap-0 overflow-hidden p-0 sm:max-h-[90dvh] sm:max-w-3xl">
        {/* pr-12 keeps the price clear of the dialog's close (X) button in the corner. */}
        <DialogHeader className="shrink-0 border-b py-4 pl-4 pr-12 text-left sm:pl-6 sm:pr-14">
          <DialogTitle className="flex items-center gap-2 text-xl">
            <Layers className="h-5 w-5 text-primary" />
            <span className="truncate">{item?.name}</span>
            <span className="ml-auto shrink-0 text-lg font-bold tabular-nums text-primary">{price.toFixed(2)} ₼</span>
          </DialogTitle>
          <DialogDescription>
            {locked
              ? "Set tam tərkibdə əlavə olunacaq — tərkibi dəyişmək üçün icazəniz yoxdur."
              : "Setə daxil olacaq məhsullara toxunun. Hər məhsulun yanında ən çox neçə ədəd ola biləcəyi yazılıb."}
          </DialogDescription>
        </DialogHeader>

        <div className="min-h-0 flex-1 overflow-y-auto bg-muted/30 px-4 py-4 sm:px-6">
          {loadError ? (
            <p className="py-10 text-center text-sm text-destructive">{loadError}</p>
          ) : components === null ? (
            <p className="py-10 text-center text-sm text-muted-foreground">Yüklənir…</p>
          ) : components.length === 0 ? (
            <p className="py-10 text-center text-sm text-muted-foreground">
              Bu setin tərkibi təyin edilməyib — set özü əlavə olunacaq.
            </p>
          ) : (
            <div className="space-y-5">
              {groups.map((group) => {
                const picked = group.list.filter((c) => countOf(c) > 0).length;
                return (
                  <section key={group.key} className="space-y-2">
                    <div className="flex items-baseline justify-between gap-2">
                      <h3 className="text-sm font-semibold uppercase tracking-wide text-muted-foreground">{group.name}</h3>
                      <span className={cn("text-xs tabular-nums", picked > 0 ? "font-semibold text-primary" : "text-muted-foreground")}>
                        {picked} / {group.list.length} seçilib
                      </span>
                    </div>
                    <div className="grid grid-cols-2 gap-2 sm:grid-cols-3">
                      {group.list.map((c) => {
                        const product = itemById.get(c.componentMenuItemId);
                        const count = countOf(c);
                        const max = maxOf(c);
                        const selected = count > 0;
                        const canToggle = selected ? allowed(c, 0) : allowed(c, canChangeCount ? 1 : c.quantity);
                        const showStepper = selected && canChangeCount && max > 1;
                        return (
                          <div
                            key={c.componentMenuItemId}
                            className={cn(
                              "relative flex flex-col overflow-hidden rounded-xl border-2 bg-card shadow-sm transition-all",
                              selected ? "border-primary ring-2 ring-primary/20" : "border-border",
                            )}
                          >
                            <button
                              type="button"
                              disabled={busy || !canToggle}
                              onClick={() => toggle(c)}
                              className="flex min-h-24 flex-1 flex-col text-left active:scale-[0.98] disabled:cursor-not-allowed"
                              aria-pressed={selected}
                            >
                              {product?.imageUrl && (
                                // eslint-disable-next-line @next/next/no-img-element
                                <img src={product.imageUrl} alt="" className="h-16 w-full shrink-0 object-cover" loading="lazy" />
                              )}
                              <div className="flex flex-1 flex-col justify-between gap-1 p-2.5 pr-9">
                                <span className="line-clamp-2 text-sm font-semibold leading-tight">{c.componentMenuItemName}</span>
                                <span className="text-xs text-muted-foreground">
                                  Maks. {max}
                                  {!removable(c) && " · məcburi"}
                                </span>
                              </div>
                            </button>
                            <span
                              className={cn(
                                "pointer-events-none absolute right-2 top-2 flex h-6 min-w-6 items-center justify-center rounded-full border-2 px-1 text-xs font-bold tabular-nums",
                                selected
                                  ? "border-primary bg-primary text-primary-foreground"
                                  : "border-muted-foreground/40 bg-background/90 text-transparent",
                              )}
                            >
                              {selected ? (max > 1 ? count : <Check className="h-3.5 w-3.5" />) : <Check className="h-3.5 w-3.5" />}
                            </span>
                            {!canToggle && selected && (
                              <Lock className="absolute bottom-2 right-2 h-3.5 w-3.5 text-muted-foreground" aria-label="İcazə yoxdur" />
                            )}
                            {showStepper && (
                              <div className="flex items-center justify-between border-t bg-primary/5 p-1.5">
                                <Button
                                  type="button"
                                  size="icon"
                                  variant="outline"
                                  className="h-9 w-9"
                                  disabled={busy || !allowed(c, count - 1)}
                                  onClick={() => setCount(c, count - 1)}
                                  aria-label="Azalt"
                                >
                                  <Minus className="h-4 w-4" />
                                </Button>
                                <span className="text-base font-bold tabular-nums">{count}</span>
                                <Button
                                  type="button"
                                  size="icon"
                                  variant="outline"
                                  className="h-9 w-9"
                                  disabled={busy || count >= max || !allowed(c, count + 1)}
                                  onClick={() => setCount(c, count + 1)}
                                  aria-label="Artır"
                                >
                                  <Plus className="h-4 w-4" />
                                </Button>
                              </div>
                            )}
                          </div>
                        );
                      })}
                    </div>
                  </section>
                );
              })}
            </div>
          )}
        </div>

        <DialogFooter className="shrink-0 flex-col gap-2 border-t px-4 py-3 sm:flex-row sm:items-center sm:justify-between sm:px-6">
          <div className="min-h-5 text-sm">
            {error ? (
              <span className="font-medium text-destructive">{error}</span>
            ) : components && components.length > 0 ? (
              <span className="text-muted-foreground">
                Seçilib: <span className="font-semibold text-foreground tabular-nums">{pickedTotal}</span> məhsul
              </span>
            ) : null}
          </div>
          <div className="flex flex-wrap justify-end gap-2">
            {components && components.length > 0 && !locked && (
              <Button type="button" variant="ghost" size="lg" disabled={busy} onClick={() => setCounts(fullSet(components))}>
                Tam set
              </Button>
            )}
            <Button type="button" variant="outline" size="lg" onClick={onClose} disabled={busy}>
              Ləğv et
            </Button>
            <Button type="button" size="lg" onClick={handleConfirm} disabled={busy || components === null || !!loadError}>
              {busy ? "Gözləyin…" : "Əlavə et"}
            </Button>
          </div>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
