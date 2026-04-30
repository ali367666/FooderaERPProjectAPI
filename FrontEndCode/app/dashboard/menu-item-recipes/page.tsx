"use client";

import { useEffect, useMemo, useState } from "react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import {
  createRecipe,
  getAllMenuItemRecipes,
  getRecipeByMenuItemId,
  type MenuItemRecipe,
  updateRecipe,
} from "@/lib/services/menu-item-recipe-service";
import { getMenuItems, type MenuItem } from "@/lib/services/menu-item-service";
import { getCompanies } from "@/lib/services/company-service";
import { getStockItemsForAllCompanies, unitLabel, type StockItem } from "@/lib/services/stock-item-service";
import { toast } from "sonner";

type EditableRecipeLine = {
  stockItemId: string;
  quantity: string;
};

const selectClass =
  "flex h-10 w-full rounded-md border border-input bg-background px-3 py-2 text-sm ring-offset-background";

export default function MenuItemRecipesPage() {
  const [menuItems, setMenuItems] = useState<MenuItem[]>([]);
  const [stockItems, setStockItems] = useState<StockItem[]>([]);
  const [menuItemId, setMenuItemId] = useState("");
  const [lines, setLines] = useState<EditableRecipeLine[]>([]);
  const [recipes, setRecipes] = useState<MenuItemRecipe[]>([]);
  const [loading, setLoading] = useState(true);
  const [recipeLoading, setRecipeLoading] = useState(false);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const loadPageData = async () => {
    try {
      setLoading(true);
      setError(null);
      const [items, companies] = await Promise.all([getMenuItems(), getCompanies()]);
      const companyIds = companies.map((x) => x.id);
      const [stocks, recipeList] = await Promise.all([
        getStockItemsForAllCompanies(companyIds),
        getAllMenuItemRecipes(),
      ]);
      setMenuItems(items);
      setStockItems(stocks);
      setRecipes(recipeList);
      if (!menuItemId && items.length > 0) {
        setMenuItemId(String(items[0].id));
      }
    } catch (e) {
      setError(e instanceof Error ? e.message : "Failed to load menu item recipes.");
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    void loadPageData();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  useEffect(() => {
    const id = Number(menuItemId);
    if (!Number.isFinite(id) || id <= 0) {
      setLines([]);
      return;
    }
    (async () => {
      try {
        setRecipeLoading(true);
        setError(null);
        const recipe = await getRecipeByMenuItemId(id);
        setLines(
          (recipe?.lines ?? []).map((x) => ({
            stockItemId: String(x.stockItemId),
            quantity: String(x.quantity),
          })),
        );
      } catch (e) {
        setLines([]);
        // "Recipe not found" should be treated as empty state, not an error.
        const message = e instanceof Error ? e.message : "";
        if (!/not found/i.test(message)) {
          setError(message || "Failed to load recipe lines.");
        }
      } finally {
        setRecipeLoading(false);
      }
    })();
  }, [menuItemId]);

  const groupedSummary = useMemo(() => recipes, [recipes]);

  const addLine = () => {
    setLines((prev) => [...prev, { stockItemId: "", quantity: "" }]);
  };

  const updateLine = (idx: number, patch: Partial<EditableRecipeLine>) => {
    setLines((prev) => prev.map((row, i) => (i === idx ? { ...row, ...patch } : row)));
  };

  const removeLine = (idx: number) => {
    setLines((prev) => prev.filter((_, i) => i !== idx));
  };

  const saveRecipe = async () => {
    const selectedMenuItemId = Number(menuItemId);
    if (!Number.isFinite(selectedMenuItemId) || selectedMenuItemId <= 0) {
      setError("Please select a menu item.");
      return;
    }

    const normalizedLines = lines.map((x) => ({
      stockItemId: Number(x.stockItemId),
      quantity: Number(x.quantity),
    }));

    if (normalizedLines.some((x) => !Number.isFinite(x.stockItemId) || x.stockItemId <= 0)) {
      setError("Each recipe line must have a stock item.");
      return;
    }
    if (normalizedLines.some((x) => !Number.isFinite(x.quantity) || x.quantity <= 0)) {
      setError("Each recipe line must have quantity greater than zero.");
      return;
    }

    try {
      setSaving(true);
      setError(null);
      const payload = {
        menuItemId: selectedMenuItemId,
        lines: normalizedLines,
      };
      console.log("save payload", payload);
      const existingRecipe = recipes.find((x) => x.menuItemId === selectedMenuItemId);
      const url = existingRecipe
        ? `/api/MenuItemRecipes/${selectedMenuItemId}`
        : "/api/MenuItemRecipes";
      console.log("Saving recipe to:", url);
      const saved = existingRecipe ? await updateRecipe(payload) : await createRecipe(payload);
      setLines(
        (saved?.lines ?? []).map((x) => ({
          stockItemId: String(x.stockItemId),
          quantity: String(x.quantity),
        })),
      );
      const [recipesResponse, selectedResponse] = await Promise.all([
        getAllMenuItemRecipes(),
        getRecipeByMenuItemId(selectedMenuItemId),
      ]);
      setRecipes(recipesResponse);
      setLines(
        (selectedResponse?.lines ?? []).map((x) => ({
          stockItemId: String(x.stockItemId),
          quantity: String(x.quantity),
        })),
      );
      toast.success("Recipe saved successfully.");
    } catch (e) {
      setError(e instanceof Error ? e.message : "Failed to save recipe.");
    } finally {
      setSaving(false);
    }
  };

  if (loading) {
    return <div className="p-6 text-sm text-muted-foreground">Loading menu item recipes...</div>;
  }

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-3xl font-bold text-foreground">Menu Item Recipes</h1>
        <p className="text-muted-foreground mt-1">Configure stock consumption rules for menu items.</p>
      </div>

      {error ? (
        <div className="rounded-md border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-600">{error}</div>
      ) : null}

      <div className="rounded-lg border p-4 space-y-4">
        <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
          <div>
            <label className="mb-2 block text-sm font-medium text-foreground">Menu Item</label>
            <select className={selectClass} value={menuItemId} onChange={(e) => setMenuItemId(e.target.value)}>
              <option value="">Select menu item</option>
              {menuItems.map((x) => (
                <option key={x.id} value={x.id}>
                  {x.name}
                </option>
              ))}
            </select>
          </div>
          <div className="flex items-end justify-start sm:justify-end">
            <Button type="button" variant="outline" onClick={addLine}>
              Add Recipe Line
            </Button>
          </div>
        </div>

        {recipeLoading ? (
          <p className="text-sm text-muted-foreground">Loading selected menu item recipe...</p>
        ) : (
          <div className="space-y-2">
            {lines.length === 0 ? (
              <p className="text-sm text-muted-foreground">No recipe lines found for selected menu item.</p>
            ) : (
              lines.map((line, index) => (
                <div key={`${index}-${line.stockItemId}`} className="grid grid-cols-1 gap-2 rounded-md border p-3 sm:grid-cols-[1fr_220px_auto]">
                  <select
                    className={selectClass}
                    value={line.stockItemId}
                    onChange={(e) => updateLine(index, { stockItemId: e.target.value })}
                  >
                    <option value="">Select stock item</option>
                    {stockItems.map((x) => (
                      <option key={x.id} value={x.id}>
                        {x.name} ({unitLabel(x.unit)})
                      </option>
                    ))}
                  </select>
                  <Input
                    type="number"
                    min={0}
                    step="0.0001"
                    value={line.quantity}
                    onChange={(e) => updateLine(index, { quantity: e.target.value })}
                    placeholder="Quantity"
                  />
                  <Button type="button" variant="destructive" onClick={() => removeLine(index)}>
                    Remove
                  </Button>
                </div>
              ))
            )}
          </div>
        )}

        <div className="flex justify-end">
          <Button type="button" onClick={saveRecipe} disabled={saving}>
            {saving ? "Saving..." : "Save Recipe"}
          </Button>
        </div>
      </div>

      <div className="rounded-lg border p-4">
        <h2 className="text-lg font-semibold mb-3">Recipe List</h2>
        {groupedSummary.length === 0 ? (
          <p className="text-sm text-muted-foreground">No recipes available.</p>
        ) : (
          <div className="space-y-2">
            {groupedSummary.map((row) => (
              <div key={row.menuItemId} className="rounded-md border px-3 py-2">
                <h4 className="text-sm font-medium text-foreground">{row.menuItemName}</h4>
                {row.lines.length === 0 ? (
                  <p className="mt-1 ml-3 text-xs text-muted-foreground">No ingredients defined</p>
                ) : (
                  <ul className="mt-1 ml-3 space-y-1 text-xs text-muted-foreground list-disc">
                    {row.lines.map((line) => (
                      <li key={`${row.menuItemId}-${line.stockItemId}`}>
                        {line.stockItemName}: {line.quantity} {line.unit}
                      </li>
                    ))}
                  </ul>
                )}
              </div>
            ))}
          </div>
        )}
      </div>
    </div>
  );
}
