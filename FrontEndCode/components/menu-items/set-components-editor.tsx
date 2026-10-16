"use client";

import { useState } from "react";
import { ChevronDown, ChevronRight } from "lucide-react";
import { Checkbox } from "@/components/ui/checkbox";
import { Input } from "@/components/ui/input";
import type { MenuItem, SetComponentInput } from "@/lib/services/menu-item-service";
import type { MenuCategory } from "@/lib/services/menu-category-service";

type Props = {
  components: SetComponentInput[];
  onChange: (components: SetComponentInput[]) => void;
  /** Every product a set may contain (sets themselves are excluded by the caller). */
  items: MenuItem[];
  categories: MenuCategory[];
};

/**
 * Admin editor for a SET's contents. Menu categories are listed; opening one shows its products. A ticked product
 * goes into the set with its own count — the most of it one set holds. The waiter can lower that count on the order.
 */
export function SetComponentsEditor({ components, onChange, items, categories }: Props) {
  const [expanded, setExpanded] = useState<number | null>(null);

  const rows = categories
    .map((category) => ({ category, products: items.filter((i) => i.menuCategoryId === category.id) }))
    .filter((r) => r.products.length > 0);

  const countOf = (id: number) => components.find((c) => c.componentMenuItemId === id)?.quantity;

  const toggle = (id: number, checked: boolean) =>
    onChange(
      checked
        ? [...components, { componentMenuItemId: id, quantity: 1, limit: 1 }]
        : components.filter((c) => c.componentMenuItemId !== id),
    );

  const setCount = (id: number, count: number) => {
    const value = Math.max(1, count || 1);
    onChange(components.map((c) => (c.componentMenuItemId === id ? { ...c, quantity: value, limit: value } : c)));
  };

  return (
    <div className="space-y-3">
      <div>
        <p className="text-sm font-medium text-foreground">Set tərkibi</p>
        <p className="text-xs text-muted-foreground">
          Kateqoriyaya basın, setə daxil olan məhsulları işarələyin və hər birinin sayını yazın. Bu say maksimumdur,
          ofisiant sifarişdə onu azalda bilər.
        </p>
      </div>

      {rows.length === 0 && <p className="text-sm text-muted-foreground">Məhsulu olan kateqoriya yoxdur.</p>}

      <div className="space-y-2">
        {rows.map(({ category, products }) => {
          const isOpen = expanded === category.id;
          const selected = products.filter((p) => countOf(p.id) != null).length;
          return (
            <div key={category.id} className={`rounded-md border ${selected > 0 ? "border-primary/60" : ""}`}>
              <button
                type="button"
                className="flex w-full items-center gap-2 px-3 py-2 text-left text-sm"
                onClick={() => setExpanded(isOpen ? null : category.id)}
              >
                {isOpen ? <ChevronDown className="h-4 w-4" /> : <ChevronRight className="h-4 w-4" />}
                <span className="font-medium">{category.name}</span>
                <span className="text-xs text-muted-foreground">
                  {selected > 0 ? `${selected} / ${products.length} seçilib` : `${products.length} məhsul`}
                </span>
              </button>

              {isOpen && (
                <div className="space-y-1 border-t p-2">
                  {products.map((p) => {
                    const count = countOf(p.id);
                    return (
                      <div key={p.id} className="flex items-center gap-2 rounded px-1 py-1 text-sm hover:bg-muted">
                        <label className="flex flex-1 cursor-pointer items-center gap-2">
                          <Checkbox checked={count != null} onCheckedChange={(v) => toggle(p.id, v === true)} />
                          {p.name}
                        </label>
                        {count != null && (
                          <div className="flex items-center gap-2">
                            <span className="text-xs text-muted-foreground">Say</span>
                            <Input
                              type="number"
                              min={1}
                              className="h-8 w-20"
                              value={count}
                              onChange={(e) => setCount(p.id, Number(e.target.value))}
                            />
                          </div>
                        )}
                      </div>
                    );
                  })}
                </div>
              )}
            </div>
          );
        })}
      </div>
    </div>
  );
}
