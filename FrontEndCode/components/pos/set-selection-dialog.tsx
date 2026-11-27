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
  /**
   * The component belongs to a choice group (its category has a pick cap in the set, e.g. "1 of the 3 desserts").
   * Leaving an option of a choice group out is the set's own rule, not a change of the set, so no permission is needed.
   */
  choice?: boolean;
};

type Props = {
  /** The SET being added; null keeps the dialog closed. */
  item: MenuItem | null;
  /** The set's price as the order sells it (e.g. the banquet price); defaults to the station/sale price. */
  price?: number;
  /** Menu products and categories — used for each component's picture and its category heading. */
  items: MenuItem[];
  categories: MenuCategory[];
  busy: boolean;
  /** "Pos.EditProductInSale": the count of a component may differ from the set's own count. */
  canChangeCount: boolean;
  /** "Pos.DeleteProductInSale": a fixed component may be left out of the set entirely. */
  canLeaveOut: boolean;
  onClose: () => void;
  onConfirm: (picks: SetPick[]) => void;
};

const maxOf = (c: SetComponent) => Math.max(c.limit ?? c.quantity, c.quantity);

/** Whether the user's permissions allow `quantity` of this component (the set's own count is always allowed). */
export function isSetPickAllowed(
  pick: Pick<SetPick, "defaultQuantity" | "quantity" | "removable" | "choice">,
  canChangeCount: boolean,
  canLeaveOut: boolean,
): boolean {
  if (pick.quantity === pick.defaultQuantity) return true;
  if (pick.quantity === 0) return pick.choice === true || (canLeaveOut && pick.removable);
  return canChangeCount;
}

type Group = {
  key: string;
  name: string;
  rank: number;
  list: SetComponent[];
  /** Most picks (sum of quantities) from this group in one set; null = a fixed part of the set. */
  cap: number | null;
};

/**
 * POS dialog for a SET. The set's contents come grouped by menu category in two kinds:
 * - fixed groups ("Setə daxildir") — always in the set; with permission a product may be left out or recounted;
 * - choice groups ("N seçin") — the category has a pick cap in the set (`categoryMaxPick`), the waiter must choose
 *   at least one and at most N of its products. The server checks the same cap when the order is sent (OK).
 * Nothing is added until "Əlavə et".
 */
export function SetSelectionDialog({
  item,
  price: priceOverride,
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

  const groups = useMemo<Group[]>(() => {
    const byCategory = new Map<number | null, SetComponent[]>();
    for (const c of components ?? []) {
      const categoryId = c.componentCategoryId ?? itemById.get(c.componentMenuItemId)?.menuCategoryId ?? null;
      byCategory.set(categoryId, [...(byCategory.get(categoryId) ?? []), c]);
    }
    const order = new Map(categories.map((c, i) => [c.id, i]));
    return [...byCategory.entries()]
      .map(([categoryId, list]) => {
        // Every component of a category carries the same cap; the smallest wins, as on the server.
        const caps = list.map((c) => c.categoryMaxPick).filter((v): v is number => v != null && v > 0);
        return {
          key: String(categoryId ?? "other"),
          name: categories.find((c) => c.id === categoryId)?.name ?? "Digər",
          rank: categoryId == null ? Number.MAX_SAFE_INTEGER : order.get(categoryId) ?? Number.MAX_SAFE_INTEGER - 1,
          list,
          cap: caps.length > 0 ? Math.min(...caps) : null,
        };
      })
      // Fixed contents first, then the choices, each in menu order.
      .sort((a, b) => Number(a.cap != null) - Number(b.cap != null) || a.rank - b.rank);
  }, [components, itemById, categories]);

  const groupOf = useMemo(() => {
    const map = new Map<number, Group>();
    for (const g of groups) for (const c of g.list) map.set(c.componentMenuItemId, g);
    return map;
  }, [groups]);

  /** Start: every fixed product at its set count; a choice group only pre-picked when it has no more options than its cap. */
  const initialCounts = (list: SetComponent[], gs: Group[]) => {
    const result: Record<number, number> = {};
    for (const g of gs) {
      if (g.cap == null) {
        for (const c of g.list) result[c.componentMenuItemId] = c.quantity;
        continue;
      }
      let left = g.cap;
      if (g.list.length <= g.cap) {
        for (const c of g.list) {
          const q = Math.min(c.quantity, left);
          if (q > 0) result[c.componentMenuItemId] = q;
          left -= q;
        }
      }
    }
    return list.length > 0 ? result : {};
  };

  useEffect(() => {
    setComponents(null);
    setLoadError(null);
    setCounts({});
    setError(null);
    if (!item) return;
    let cancelled = false;
    getMenuItemSetComponents(item.id)
      .then((list) => {
        if (!cancelled) setComponents(list);
      })
      .catch((e) => {
        if (!cancelled) setLoadError(e instanceof Error ? e.message : "Setin tərkibi yüklənmədi.");
      });
    return () => {
      cancelled = true;
    };
  }, [item]);

  // The starting picks need the groups (caps), so they are set once the components are in.
  useEffect(() => {
    if (components) setCounts(initialCounts(components, groups));
    // Only when another set's components arrive; the menu list refreshing must not reset the waiter's picks.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [components]);

  const countOf = (c: SetComponent) => counts[c.componentMenuItemId] ?? 0;
  const groupPicked = (g: Group) => g.list.reduce((s, c) => s + countOf(c), 0);
  const isChoice = (c: SetComponent) => groupOf.get(c.componentMenuItemId)?.cap != null;

  const allowed = (c: SetComponent, quantity: number) =>
    isSetPickAllowed(
      { defaultQuantity: c.quantity, quantity, removable: removable(c), choice: isChoice(c) },
      canChangeCount,
      canLeaveOut,
    );

  /** Most of `c` the waiter can have now: its own maximum, and in a choice group what the cap leaves. */
  const ceilingOf = (c: SetComponent) => {
    const g = groupOf.get(c.componentMenuItemId);
    if (!g || g.cap == null) return maxOf(c);
    return Math.min(maxOf(c), g.cap - groupPicked(g) + countOf(c));
  };

  const setCount = (c: SetComponent, quantity: number) => {
    if (busy || quantity < 0 || quantity > ceilingOf(c) || !allowed(c, quantity)) return;
    setError(null);
    setCounts((prev) => ({ ...prev, [c.componentMenuItemId]: quantity }));
  };

  /** Tapping a row: a fixed product goes out / back in; in a choice group it is picked, or (cap 1) replaces the pick. */
  const tap = (c: SetComponent) => {
    if (busy) return;
    const g = groupOf.get(c.componentMenuItemId);
    const current = countOf(c);
    if (current > 0) {
      setCount(c, 0);
      return;
    }
    if (g?.cap == null) {
      setCount(c, c.quantity);
      return;
    }
    const pickQty = canChangeCount ? 1 : c.quantity;
    if (g.cap === 1 || groupPicked(g) + pickQty > g.cap) {
      // Radio behaviour for "1 seçin"; for a larger cap that is full, the tap swaps out the other picks.
      if (pickQty > g.cap) return;
      setError(null);
      setCounts((prev) => {
        const next = { ...prev };
        for (const other of g.list) next[other.componentMenuItemId] = 0;
        next[c.componentMenuItemId] = pickQty;
        return next;
      });
      return;
    }
    setCount(c, pickQty);
  };

  const missingChoice = groups.find((g) => g.cap != null && groupPicked(g) === 0);
  const pickedTotal = (components ?? []).reduce((s, c) => s + countOf(c), 0);

  const handleConfirm = () => {
    if (!components) return;
    if (missingChoice) {
      setError(`“${missingChoice.name}” üçün seçim edin.`);
      return;
    }
    const over = groups.find((g) => g.cap != null && groupPicked(g) > g.cap);
    if (over) {
      setError(`“${over.name}” kateqoriyasından ən çox ${over.cap} seçim edilə bilər.`);
      return;
    }
    if (components.length > 0 && pickedTotal === 0) {
      setError("Setə ən azı bir məhsul seçin.");
      return;
    }
    const picks: SetPick[] = components.map((c) => ({
      componentMenuItemId: c.componentMenuItemId,
      defaultQuantity: c.quantity,
      quantity: countOf(c),
      removable: removable(c),
      choice: isChoice(c),
    }));
    if (picks.some((p, i) => p.quantity > maxOf(components[i]) || !allowed(components[i], p.quantity))) {
      setError("Bu seçim üçün icazəniz yoxdur.");
      return;
    }
    onConfirm(picks);
  };

  const hasChoices = groups.some((g) => g.cap != null);
  const fixedLocked = !canChangeCount && !canLeaveOut;
  const price = priceOverride ?? (item ? item.stationPrice ?? item.price : 0);

  return (
    <Dialog open={item != null} onOpenChange={(o) => !o && !busy && onClose()}>
      <DialogContent className="flex max-h-[calc(100dvh-1rem)] w-full max-w-[calc(100%-1rem)] flex-col gap-0 overflow-hidden p-0 sm:max-h-[85dvh] sm:max-w-2xl">
        {/* pr-12 keeps the price clear of the dialog's close (X) button in the corner. */}
        <DialogHeader className="shrink-0 border-b py-3 pl-4 pr-12 text-left sm:pl-5 sm:pr-14">
          <DialogTitle className="flex items-center gap-2 text-lg">
            <Layers className="h-5 w-5 shrink-0 text-primary" />
            <span className="truncate">{item?.name}</span>
            <span className="ml-auto shrink-0 font-bold tabular-nums text-primary">{price.toFixed(2)} ₼</span>
          </DialogTitle>
          <DialogDescription className="text-xs">
            {hasChoices
              ? "Seçim qruplarından məhsul seçin. Sabit tərkib setə avtomatik daxildir."
              : fixedLocked
                ? "Set tam tərkibdə əlavə olunacaq."
                : "Set tam tərkibdə əlavə olunur — lazım olsa məhsulu çıxarın və ya sayını dəyişin."}
          </DialogDescription>
        </DialogHeader>

        <div className="min-h-0 flex-1 overflow-y-auto px-4 py-3 sm:px-5">
          {loadError ? (
            <p className="py-10 text-center text-sm text-destructive">{loadError}</p>
          ) : components === null ? (
            <p className="py-10 text-center text-sm text-muted-foreground">Yüklənir…</p>
          ) : components.length === 0 ? (
            <p className="py-10 text-center text-sm text-muted-foreground">
              Bu setin tərkibi təyin edilməyib — set özü əlavə olunacaq.
            </p>
          ) : (
            <div className="space-y-4">
              {groups.map((group) => {
                const picked = groupPicked(group);
                const choice = group.cap != null;
                const full = choice && picked >= group.cap!;
                const missing = choice && picked === 0;
                return (
                  <section key={group.key}>
                    <div className="mb-1.5 flex items-center gap-2">
                      <h3 className="truncate text-sm font-semibold">{group.name}</h3>
                      {choice ? (
                        <span
                          className={cn(
                            "rounded-full px-2 py-0.5 text-[11px] font-semibold",
                            missing ? "bg-destructive/10 text-destructive" : "bg-primary/10 text-primary",
                          )}
                        >
                          {group.cap === 1 ? "1 seçin" : `${group.cap}-ə qədər seçin`} · məcburi
                        </span>
                      ) : (
                        <span className="rounded-full bg-muted px-2 py-0.5 text-[11px] font-medium text-muted-foreground">
                          Setə daxildir
                        </span>
                      )}
                      {choice && (
                        <span
                          className={cn(
                            "ml-auto text-xs font-semibold tabular-nums",
                            full ? "text-primary" : missing ? "text-destructive" : "text-muted-foreground",
                          )}
                        >
                          {picked} / {group.cap}
                        </span>
                      )}
                    </div>

                    <div className="grid gap-1.5 sm:grid-cols-2">
                      {group.list.map((c) => {
                        const product = itemById.get(c.componentMenuItemId);
                        const count = countOf(c);
                        const selected = count > 0;
                        const ceiling = ceilingOf(c);
                        const tapQty = choice ? (canChangeCount ? 1 : c.quantity) : c.quantity;
                        // A full multi-pick group still lets a tap swap the picks; only a pick bigger than the cap is impossible.
                        const canTap = selected
                          ? allowed(c, 0)
                          : allowed(c, tapQty) && (!choice || tapQty <= group.cap!);
                        const dimmed = choice && full && !selected;
                        const showStepper = selected && canChangeCount && maxOf(c) > 1;
                        return (
                          <div
                            key={c.componentMenuItemId}
                            className={cn(
                              "flex min-h-12 items-center gap-2 rounded-lg border px-2 transition-colors",
                              selected ? "border-primary bg-primary/5" : "border-border bg-card",
                              dimmed && "opacity-55",
                            )}
                          >
                            <button
                              type="button"
                              disabled={busy || !canTap}
                              onClick={() => tap(c)}
                              aria-pressed={selected}
                              className="flex min-w-0 flex-1 items-center gap-2.5 self-stretch py-1.5 text-left disabled:cursor-not-allowed"
                            >
                              <span
                                className={cn(
                                  "flex h-5 w-5 shrink-0 items-center justify-center border-2",
                                  choice && group.cap === 1 ? "rounded-full" : "rounded",
                                  selected ? "border-primary bg-primary text-primary-foreground" : "border-muted-foreground/40",
                                )}
                              >
                                {selected &&
                                  (choice && group.cap === 1 ? (
                                    <span className="h-2 w-2 rounded-full bg-primary-foreground" />
                                  ) : (
                                    <Check className="h-3.5 w-3.5" strokeWidth={3} />
                                  ))}
                              </span>
                              {product?.imageUrl && (
                                // eslint-disable-next-line @next/next/no-img-element
                                <img src={product.imageUrl} alt="" className="h-8 w-8 shrink-0 rounded object-cover" loading="lazy" />
                              )}
                              <span className="min-w-0 flex-1">
                                <span
                                  className={cn(
                                    "line-clamp-2 text-sm font-medium leading-tight",
                                    !choice && !selected && "text-muted-foreground line-through",
                                  )}
                                >
                                  {c.componentMenuItemName}
                                </span>
                                {!choice && !selected && <span className="text-[11px] text-muted-foreground">setdən çıxarılıb</span>}
                              </span>
                              {!canTap && !choice && <Lock className="h-3.5 w-3.5 shrink-0 text-muted-foreground" aria-label="İcazə yoxdur" />}
                            </button>

                            {showStepper ? (
                              <div className="flex shrink-0 items-center gap-1">
                                <Button
                                  type="button"
                                  size="icon"
                                  variant="outline"
                                  className="h-8 w-8"
                                  disabled={busy || count <= 1 || !allowed(c, count - 1)}
                                  onClick={() => setCount(c, count - 1)}
                                  aria-label="Azalt"
                                >
                                  <Minus className="h-3.5 w-3.5" />
                                </Button>
                                <span className="w-6 text-center text-sm font-bold tabular-nums">{count}</span>
                                <Button
                                  type="button"
                                  size="icon"
                                  variant="outline"
                                  className="h-8 w-8"
                                  disabled={busy || count >= ceiling || !allowed(c, count + 1)}
                                  onClick={() => setCount(c, count + 1)}
                                  aria-label="Artır"
                                >
                                  <Plus className="h-3.5 w-3.5" />
                                </Button>
                              </div>
                            ) : (
                              selected &&
                              count > 1 && <span className="shrink-0 text-sm font-semibold tabular-nums text-primary">× {count}</span>
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

        <DialogFooter className="shrink-0 flex-row items-center gap-2 border-t px-4 py-3 sm:justify-between sm:px-5">
          <div className="min-w-0 flex-1 text-sm">
            {error ? (
              <span className="font-medium text-destructive">{error}</span>
            ) : missingChoice ? (
              <span className="text-muted-foreground">
                “<span className="font-medium text-foreground">{missingChoice.name}</span>” üçün seçim edin
              </span>
            ) : components && components.length > 0 ? (
              <span className="text-muted-foreground">
                Setdə: <span className="font-semibold tabular-nums text-foreground">{pickedTotal}</span> məhsul
              </span>
            ) : null}
          </div>
          <Button type="button" variant="outline" size="lg" onClick={onClose} disabled={busy}>
            Ləğv et
          </Button>
          <Button
            type="button"
            size="lg"
            onClick={handleConfirm}
            disabled={busy || components === null || !!loadError || !!missingChoice}
          >
            {busy ? "Gözləyin…" : "Əlavə et"}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
