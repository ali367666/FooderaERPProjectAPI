"use client";

import { Fragment, useCallback, useEffect, useMemo, useState } from "react";
import { getFoodCost, type FoodCostItem } from "@/lib/services/analytics-service";
import { toApiFormError } from "@/lib/api-error";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { cn } from "@/lib/utils";
import {
  RefreshCw, ChevronDown, ChevronUp, AlertTriangle,
  TrendingDown, TrendingUp, Search,
} from "lucide-react";

function fmt(n: number) {
  return new Intl.NumberFormat("az-AZ", { minimumFractionDigits: 2, maximumFractionDigits: 2 }).format(n) + " ₼";
}

function pctColor(pct: number) {
  if (pct >= 40) return "text-red-600 dark:text-red-400";
  if (pct >= 28) return "text-amber-600 dark:text-amber-400";
  return "text-emerald-600 dark:text-emerald-400";
}

function pctBg(pct: number) {
  if (pct >= 40) return "bg-red-100 dark:bg-red-950";
  if (pct >= 28) return "bg-amber-100 dark:bg-amber-950";
  return "bg-emerald-100 dark:bg-emerald-950";
}

export function FoodCostPage() {
  const [items, setItems] = useState<FoodCostItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [expanded, setExpanded] = useState<number | null>(null);
  const [search, setSearch] = useState("");
  const [sortKey, setSortKey] = useState<"foodCostPercentage" | "grossProfitMargin" | "grossProfit" | "sellingPrice">("foodCostPercentage");
  const [sortDesc, setSortDesc] = useState(true);

  const load = useCallback(async () => {
    try {
      setLoading(true);
      setError(null);
      setItems(await getFoodCost());
    } catch (e) {
      setError(toApiFormError(e, "Yüklənmə xətası").message);
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => { void load(); }, [load]);

  const filtered = useMemo(() => {
    let list = items;
    if (search.trim()) {
      const q = search.toLowerCase();
      list = list.filter(i => i.menuItemName.toLowerCase().includes(q) || i.categoryName.toLowerCase().includes(q));
    }
    return [...list].sort((a, b) => {
      const diff = a[sortKey] - b[sortKey];
      return sortDesc ? -diff : diff;
    });
  }, [items, search, sortKey, sortDesc]);

  function toggleSort(key: typeof sortKey) {
    if (sortKey === key) setSortDesc(d => !d);
    else { setSortKey(key); setSortDesc(true); }
  }

  const summary = useMemo(() => {
    const withRecipe = items.filter(i => i.hasRecipe);
    const avgFoodCostPct = withRecipe.length
      ? withRecipe.reduce((s, i) => s + i.foodCostPercentage, 0) / withRecipe.length
      : 0;
    const totalMissing = items.filter(i => i.hasMissingCost).length;
    const noRecipe = items.filter(i => !i.hasRecipe).length;
    const highCost = items.filter(i => i.hasRecipe && i.foodCostPercentage >= 40).length;
    return { avgFoodCostPct, totalMissing, noRecipe, highCost, count: items.length };
  }, [items]);

  if (loading && items.length === 0) {
    return (
      <div className="space-y-4">
        <div className="grid grid-cols-4 gap-4">
          {Array.from({ length: 4 }).map((_, i) => <div key={i} className="h-24 rounded-xl bg-muted animate-pulse" />)}
        </div>
        <div className="h-96 rounded-xl bg-muted animate-pulse" />
      </div>
    );
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-semibold tracking-tight">Food Cost</h1>
          <p className="text-muted-foreground text-sm mt-1">
            Resept dəyəri vs satış qiyməti — mənfəət analizi
          </p>
        </div>
        <Button variant="outline" size="sm" onClick={() => void load()} disabled={loading} className="gap-1.5">
          <RefreshCw className={cn("h-4 w-4", loading && "animate-spin")} />
          Yenilə
        </Button>
      </div>

      {error && (
        <div className="rounded-md border border-destructive/40 bg-destructive/10 px-4 py-3 text-sm text-destructive">{error}</div>
      )}

      {/* Summary cards */}
      <div className="grid grid-cols-2 lg:grid-cols-4 gap-4">
        <div className="rounded-xl border border-border bg-card p-4">
          <p className="text-sm text-muted-foreground">Orta food cost</p>
          <p className={cn("text-2xl font-semibold mt-1", pctColor(summary.avgFoodCostPct))}>
            {summary.avgFoodCostPct.toFixed(1)}%
          </p>
        </div>
        <div className="rounded-xl border border-border bg-card p-4">
          <p className="text-sm text-muted-foreground">Yüksək dəyər (≥40%)</p>
          <p className="text-2xl font-semibold mt-1 text-red-600 dark:text-red-400">{summary.highCost}</p>
        </div>
        <div className="rounded-xl border border-border bg-card p-4">
          <p className="text-sm text-muted-foreground">Resepti yoxdur</p>
          <p className="text-2xl font-semibold mt-1 text-amber-600 dark:text-amber-400">{summary.noRecipe}</p>
        </div>
        <div className="rounded-xl border border-border bg-card p-4">
          <p className="text-sm text-muted-foreground">Alış tarixçəsi yoxdur</p>
          <p className="text-2xl font-semibold mt-1 text-muted-foreground">{summary.totalMissing}</p>
        </div>
      </div>

      {/* Search */}
      <div className="relative max-w-sm">
        <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground" />
        <Input
          value={search}
          onChange={e => setSearch(e.target.value)}
          placeholder="Məhsul və ya kateqoriya axtar..."
          className="pl-9"
        />
      </div>

      {/* Table */}
      <div className="rounded-xl border border-border bg-card overflow-hidden">
        <table className="w-full text-sm">
          <thead className="bg-muted/50 text-xs text-muted-foreground">
            <tr>
              <th className="text-left px-4 py-3 font-medium">Məhsul</th>
              <th className="text-left px-4 py-3 font-medium">Kateqoriya</th>
              <th className="text-right px-4 py-3 font-medium cursor-pointer" onClick={() => toggleSort("sellingPrice")}>
                <span className="inline-flex items-center gap-1">Satış qiyməti {sortKey === "sellingPrice" && (sortDesc ? <ChevronDown className="h-3 w-3" /> : <ChevronUp className="h-3 w-3" />)}</span>
              </th>
              <th className="text-right px-4 py-3 font-medium">Resept dəyəri</th>
              <th className="text-right px-4 py-3 font-medium cursor-pointer" onClick={() => toggleSort("foodCostPercentage")}>
                <span className="inline-flex items-center gap-1">Food cost % {sortKey === "foodCostPercentage" && (sortDesc ? <ChevronDown className="h-3 w-3" /> : <ChevronUp className="h-3 w-3" />)}</span>
              </th>
              <th className="text-right px-4 py-3 font-medium cursor-pointer" onClick={() => toggleSort("grossProfit")}>
                <span className="inline-flex items-center gap-1">Mənfəət {sortKey === "grossProfit" && (sortDesc ? <ChevronDown className="h-3 w-3" /> : <ChevronUp className="h-3 w-3" />)}</span>
              </th>
              <th className="text-right px-4 py-3 font-medium cursor-pointer" onClick={() => toggleSort("grossProfitMargin")}>
                <span className="inline-flex items-center gap-1">Margin % {sortKey === "grossProfitMargin" && (sortDesc ? <ChevronDown className="h-3 w-3" /> : <ChevronUp className="h-3 w-3" />)}</span>
              </th>
              <th className="w-8"></th>
            </tr>
          </thead>
          <tbody className="divide-y divide-border">
            {filtered.map(item => (
              <Fragment key={item.menuItemId}>
                <tr
                  className="hover:bg-muted/30 cursor-pointer transition-colors"
                  onClick={() => setExpanded(expanded === item.menuItemId ? null : item.menuItemId)}
                >
                  <td className="px-4 py-3 font-medium">
                    <div className="flex items-center gap-2">
                      {item.menuItemName}
                      {!item.hasRecipe && (
                        <span title="Resept təyin edilməyib">
                          <AlertTriangle className="h-3.5 w-3.5 text-amber-500" />
                        </span>
                      )}
                      {item.hasMissingCost && (
                        <span title="Bəzi inqredientlərin alış tarixçəsi yoxdur">
                          <AlertTriangle className="h-3.5 w-3.5 text-muted-foreground" />
                        </span>
                      )}
                    </div>
                  </td>
                  <td className="px-4 py-3 text-muted-foreground">{item.categoryName}</td>
                  <td className="px-4 py-3 text-right">{fmt(item.sellingPrice)}</td>
                  <td className="px-4 py-3 text-right">{item.hasRecipe ? fmt(item.foodCost) : "—"}</td>
                  <td className="px-4 py-3 text-right">
                    {item.hasRecipe ? (
                      <span className={cn("inline-flex items-center px-2 py-0.5 rounded text-xs font-semibold", pctBg(item.foodCostPercentage), pctColor(item.foodCostPercentage))}>
                        {item.foodCostPercentage.toFixed(1)}%
                      </span>
                    ) : "—"}
                  </td>
                  <td className="px-4 py-3 text-right">
                    {item.hasRecipe ? (
                      <span className={cn("inline-flex items-center gap-1", item.grossProfit >= 0 ? "text-emerald-600 dark:text-emerald-400" : "text-red-600 dark:text-red-400")}>
                        {item.grossProfit >= 0 ? <TrendingUp className="h-3 w-3" /> : <TrendingDown className="h-3 w-3" />}
                        {fmt(item.grossProfit)}
                      </span>
                    ) : "—"}
                  </td>
                  <td className="px-4 py-3 text-right font-medium">
                    {item.hasRecipe ? `${item.grossProfitMargin.toFixed(1)}%` : "—"}
                  </td>
                  <td className="px-2">
                    {item.hasRecipe && (
                      expanded === item.menuItemId ? <ChevronUp className="h-4 w-4 text-muted-foreground" /> : <ChevronDown className="h-4 w-4 text-muted-foreground" />
                    )}
                  </td>
                </tr>
                {expanded === item.menuItemId && item.hasRecipe && (
                  <tr>
                    <td colSpan={8} className="bg-muted/20 px-4 py-3">
                      <p className="text-xs font-semibold text-muted-foreground uppercase tracking-wider mb-2">İnqredientlər</p>
                      <table className="w-full text-xs">
                        <thead>
                          <tr className="text-muted-foreground">
                            <th className="text-left py-1.5 font-medium">İnqredient</th>
                            <th className="text-right py-1.5 font-medium">Miqdar</th>
                            <th className="text-right py-1.5 font-medium">Vahid dəyər</th>
                            <th className="text-right py-1.5 font-medium">Cəmi</th>
                          </tr>
                        </thead>
                        <tbody className="divide-y divide-border/50">
                          {item.lines.map(line => (
                            <tr key={line.stockItemId}>
                              <td className="py-1.5 flex items-center gap-1.5">
                                {line.stockItemName}
                                {line.missingCost && (
                                  <span title="Alış tarixçəsi yoxdur, dəyər 0 qəbul edildi">
                                    <AlertTriangle className="h-3 w-3 text-amber-500" />
                                  </span>
                                )}
                              </td>
                              <td className="text-right py-1.5">{line.quantityPerPortion} {line.unit}</td>
                              <td className="text-right py-1.5">{fmt(line.unitCost)}</td>
                              <td className="text-right py-1.5 font-medium">{fmt(line.lineCost)}</td>
                            </tr>
                          ))}
                        </tbody>
                      </table>
                    </td>
                  </tr>
                )}
              </Fragment>
            ))}
          </tbody>
        </table>

        {filtered.length === 0 && (
          <div className="py-12 text-center text-muted-foreground text-sm">Heç bir məhsul tapılmadı.</div>
        )}
      </div>

      <p className="text-xs text-muted-foreground">
        ⓘ Resept dəyəri inqredientlərin orta alış qiymətinə (StockPurchase tarixçəsi) əsasən hesablanır.
        Food cost % 28%-dən aşağı yaxşı, 40%-dən yuxarı isə qiymət siyasətini nəzərdən keçirməyi tövsiyə edir.
      </p>
    </div>
  );
}
