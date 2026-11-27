"use client";

import { Fragment, useCallback, useEffect, useMemo, useState } from "react";
import { ChevronDown, ChevronRight, Minus, Pencil, Plus } from "lucide-react";
import { toast } from "sonner";
import { CategoryPanel, categoryWithChildren } from "@/components/stock/category-panel";
import { StockItemDialog, TYPE_LABELS, UNIT_LABELS } from "@/components/stock/item-dialog";
import { StockOperationDialog, type OperationKind, type OperationTarget } from "@/components/stock/stock-operation-dialog";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { useSelectedCompany } from "@/contexts/selected-company-context";
import { useIsSuperAdmin, usePermissionSet } from "@/hooks/use-auth-permissions";
import { getStoredToken } from "@/lib/auth-client";
import { formatCurrency } from "@/lib/format-currency";
import { getCompanyIdFromToken } from "@/lib/jwt-permissions";
import { getRestaurants, type Restaurant } from "@/lib/services/restaurant-service";
import { getStockAssetHistory, getStockAssets, type StockAsset, type StockAssetOperation } from "@/lib/services/stock-asset-service";
import { getAllStockCategories, type StockCategory } from "@/lib/services/stock-category-service";
import { getStockItemsForCompany, type StockItem } from "@/lib/services/stock-item-service";
import { getWarehouses, type Warehouse } from "@/lib/services/warehouse-service";
import { cn } from "@/lib/utils";
import { DateTimeRangeBar, useDateTimeRange } from "@/components/date-time-range";

const SELECT_CLASS = "h-9 rounded-md border border-input bg-background px-3 text-sm";
const th = "px-3 py-2 font-medium";
const qty = (n: number) => String(Math.round(n * 1000) / 1000);
const dateTime = (iso: string) => {
  const d = new Date(iso);
  return `${d.toLocaleDateString("az-AZ")} ${d.toLocaleTimeString("az-AZ", { hour: "2-digit", minute: "2-digit" })}`;
};
const HISTORY_STEP = 10;

/**
 * Stock in one place: the category tree on the left, the items with their stock on the right. An item opens into its stock per warehouse and its
 * history; adding, editing, increasing and decreasing all happen on this page.
 */
export default function StockPage() {
  const { selectedCompanyId, companiesLoading } = useSelectedCompany();
  const isSuperAdmin = useIsSuperAdmin();
  const permissions = usePermissionSet();
  const can = (p: string) => isSuperAdmin || permissions.has(p);
  const canSeeStock = can("WarehouseStock.View");
  const canChangeStock = can("StockWriteOff.Create");
  // The server acts on the company picked in the filter for the SuperAdmin only; everyone else works in their own company.
  const companyId = isSuperAdmin ? selectedCompanyId : getCompanyIdFromToken(getStoredToken());

  const [items, setItems] = useState<StockItem[]>([]);
  const [balances, setBalances] = useState<Map<number, StockAsset>>(new Map());
  const [categories, setCategories] = useState<StockCategory[]>([]);
  const [warehouses, setWarehouses] = useState<Warehouse[]>([]);
  const [restaurants, setRestaurants] = useState<Restaurant[]>([]);
  const [loading, setLoading] = useState(true);

  const [categoryId, setCategoryId] = useState<number | null>(null);
  const [warehouseId, setWarehouseId] = useState<number | null>(null);
  const [type, setType] = useState<number | null>(null);
  const [onlyEmpty, setOnlyEmpty] = useState(false);
  const [term, setTerm] = useState("");

  const [expanded, setExpanded] = useState<number | null>(null);
  const [history, setHistory] = useState<StockAssetOperation[] | null>(null);
  const [historyLimit, setHistoryLimit] = useState(HISTORY_STEP);
  const period = useDateTimeRange();
  const shownHistory = history?.filter((h) => period.matches(h.date)) ?? null;

  const [itemDialog, setItemDialog] = useState<{ itemId: number | null } | null>(null);
  const [operation, setOperation] = useState<OperationTarget | null>(null);

  const load = useCallback(
    async (silent = false) => {
      if (!companyId) {
        setLoading(false);
        return;
      }
      if (!silent) setLoading(true);
      const fail = <T,>(fallback: T, message: string) => (e: unknown): T => {
        toast.error(e instanceof Error ? e.message : message);
        return fallback;
      };
      const [itemList, categoryList, stock, warehouseList, restaurantList] = await Promise.all([
        getStockItemsForCompany(companyId).catch(fail<StockItem[]>([], "Məhsullar yüklənmədi.")),
        getAllStockCategories({ companyId }).catch(fail<StockCategory[]>([], "Kateqoriyalar yüklənmədi.")),
        canSeeStock ? getStockAssets(true).catch(fail(null, "Qalıqlar yüklənmədi.")) : Promise.resolve(null),
        getWarehouses().catch(() => [] as Warehouse[]),
        getRestaurants().catch(() => [] as Restaurant[]),
      ]);
      setItems(itemList);
      setCategories(categoryList);
      setBalances(new Map((stock?.items ?? []).map((i) => [i.stockItemId, i])));
      setWarehouses(warehouseList.filter((w) => w.companyId === companyId));
      setRestaurants(restaurantList.filter((r) => r.companyId === companyId));
      setLoading(false);
    },
    [companyId, canSeeStock],
  );

  useEffect(() => {
    // another company: nothing picked for the previous one stays
    setCategoryId(null);
    setWarehouseId(null);
    setExpanded(null);
    void load();
  }, [load]);

  const loadHistory = useCallback(async (itemId: number) => {
    setHistory(null);
    try {
      setHistory(await getStockAssetHistory({ stockItemId: itemId }));
    } catch (e) {
      toast.error(e instanceof Error ? e.message : "Tarixçə yüklənmədi.");
      setHistory([]);
    }
  }, []);

  const toggle = (itemId: number) => {
    if (expanded === itemId) {
      setExpanded(null);
      return;
    }
    setExpanded(itemId);
    setHistoryLimit(HISTORY_STEP);
    if (canSeeStock) void loadHistory(itemId);
  };

  /** The item's stock in the chosen warehouse, or in all of them. */
  const stockOf = useCallback(
    (itemId: number) => {
      const b = balances.get(itemId);
      if (!b) return 0;
      return warehouseId == null ? b.totalQuantity : (b.warehouses.find((w) => w.warehouseId === warehouseId)?.quantity ?? 0);
    },
    [balances, warehouseId],
  );

  const counts = useMemo(() => {
    const result: Record<number, number> = {};
    for (const i of items) result[i.categoryId] = (result[i.categoryId] ?? 0) + 1;
    return result;
  }, [items]);

  const shown = useMemo(() => {
    const q = term.trim().toLowerCase();
    const inCategory = categoryId == null ? null : categoryWithChildren(categories, categoryId);
    return items
      .filter((i) => !inCategory || inCategory.has(i.categoryId))
      .filter((i) => type == null || i.type === type)
      .filter((i) => !q || i.name.toLowerCase().includes(q) || (i.barcode ?? "").toLowerCase().includes(q))
      .filter((i) => !onlyEmpty || stockOf(i.id) <= 0)
      .sort((a, b) => a.name.localeCompare(b.name, "az"));
  }, [items, categories, categoryId, type, term, onlyEmpty, stockOf]);

  const valueOf = (itemId: number) => {
    const b = balances.get(itemId);
    return b?.averageCostAzn == null ? 0 : stockOf(itemId) * b.averageCostAzn;
  };
  const totalValue = shown.reduce((s, i) => s + valueOf(i.id), 0);

  const startOperation = (item: StockItem, start: OperationKind) =>
    setOperation({ stockItemId: item.id, name: item.name, unit: item.unit, warehouses: balances.get(item.id)?.warehouses ?? [], start });

  const afterOperation = () => {
    void load(true);
    if (expanded != null) void loadHistory(expanded);
  };

  if (isSuperAdmin && !companyId) {
    return (
      <div className="space-y-2 p-6">
        <h1 className="text-2xl font-bold">Stok</h1>
        <p className="text-sm text-muted-foreground">{companiesLoading ? "Yüklənir…" : "Yuxarıdakı şirkət süzgəcindən bir şirkət seçin."}</p>
      </div>
    );
  }

  const columns = 4 + (canSeeStock ? 3 : 0);

  return (
    <div className="space-y-4">
      <div className="flex flex-wrap items-start justify-between gap-3">
        <div>
          <h1 className="text-2xl font-bold">Stok</h1>
          <p className="text-sm text-muted-foreground">Kateqoriyalar, məhsullar, qalıqlar və bütün stok əməliyyatları bir yerdə.</p>
        </div>
        {can("StockItem.Create") && (
          <Button onClick={() => setItemDialog({ itemId: null })}>
            <Plus className="mr-1 h-4 w-4" />
            Yeni məhsul
          </Button>
        )}
      </div>

      <div className="grid items-start gap-4 lg:grid-cols-[16rem_1fr]">
        {companyId && (
          <CategoryPanel
            companyId={companyId}
            categories={categories}
            counts={counts}
            total={items.length}
            selected={categoryId}
            onSelect={setCategoryId}
            onChanged={() => void load(true)}
            canCreate={can("StockCategory.Create")}
            canUpdate={can("StockCategory.Update")}
            canDelete={can("StockCategory.Delete")}
          />
        )}

        <div className="min-w-0 space-y-3">
          <div className="flex flex-wrap items-center gap-2 rounded-lg border bg-card p-3">
            <Input className="w-56" aria-label="Məhsul axtar" placeholder="Ad və ya barkod…" value={term} onChange={(e) => setTerm(e.target.value)} />
            {canSeeStock && (
              <select aria-label="Anbar" className={SELECT_CLASS} value={warehouseId ?? ""} onChange={(e) => setWarehouseId(e.target.value ? Number(e.target.value) : null)}>
                <option value="">Bütün anbarlar</option>
                {warehouses.map((w) => (
                  <option key={w.id} value={w.id}>
                    {w.name}
                  </option>
                ))}
              </select>
            )}
            <div className="flex flex-wrap gap-1">
              {[null, ...Object.keys(TYPE_LABELS).map(Number)].map((t) => (
                <button
                  key={t ?? "all"}
                  type="button"
                  onClick={() => setType(t)}
                  className={cn("rounded-full border px-3 py-1 text-xs", type === t ? "border-primary bg-primary text-primary-foreground" : "hover:bg-muted")}
                >
                  {t == null ? "Bütün növlər" : TYPE_LABELS[t]}
                </button>
              ))}
            </div>
            {canSeeStock && (
              <label className="flex items-center gap-1.5 text-sm">
                <input type="checkbox" checked={onlyEmpty} onChange={(e) => setOnlyEmpty(e.target.checked)} />
                Yalnız bitənlər
              </label>
            )}
            <span className="ml-auto text-sm text-muted-foreground">
              {shown.length} məhsul
              {canSeeStock && (
                <>
                  {" · dəyəri "}
                  <b className="text-foreground">{formatCurrency(totalValue)}</b>
                </>
              )}
            </span>
          </div>

          <div className="overflow-x-auto rounded-lg border bg-card">
            <table className="w-full text-sm">
              <thead>
                <tr className="bg-muted/40 text-left text-xs text-muted-foreground">
                  <th className={th}>Məhsul</th>
                  <th className={th}>Kateqoriya</th>
                  <th className={th}>Növ</th>
                  {canSeeStock && (
                    <>
                      <th className={cn(th, "text-right")}>Qalıq</th>
                      <th className={cn(th, "text-right")}>Orta maya</th>
                      <th className={cn(th, "text-right")}>Dəyər</th>
                    </>
                  )}
                  <th className={th} />
                </tr>
              </thead>
              <tbody>
                {loading ? (
                  <tr>
                    <td colSpan={columns} className="px-3 py-10 text-center text-muted-foreground">
                      Yüklənir…
                    </td>
                  </tr>
                ) : shown.length === 0 ? (
                  <tr>
                    <td colSpan={columns} className="px-3 py-10 text-center text-muted-foreground">
                      {items.length === 0 ? "Hələ məhsul yoxdur. “Yeni məhsul” ilə əlavə edin." : "Bu şərtlərlə məhsul tapılmadı."}
                    </td>
                  </tr>
                ) : (
                  shown.map((i) => {
                    const open = expanded === i.id;
                    const balance = balances.get(i.id);
                    const quantity = stockOf(i.id);
                    return (
                      <Fragment key={i.id}>
                        <tr className={cn("border-t", open ? "bg-muted/30" : "hover:bg-muted/30")}>
                          <td className="px-3 py-2 font-medium">
                            <button type="button" className="flex items-center gap-1.5 text-left" aria-expanded={open} onClick={() => toggle(i.id)}>
                              {open ? <ChevronDown className="h-4 w-4 shrink-0" /> : <ChevronRight className="h-4 w-4 shrink-0" />}
                              <span>
                                {i.name}
                                {i.barcode && <span className="block text-xs font-normal text-muted-foreground">{i.barcode}</span>}
                              </span>
                            </button>
                          </td>
                          <td className="px-3 py-2 text-muted-foreground">{i.categoryName}</td>
                          <td className="px-3 py-2">
                            <Badge variant="secondary">{TYPE_LABELS[i.type] ?? i.type}</Badge>
                          </td>
                          {canSeeStock && (
                            <>
                              <td className={cn("px-3 py-2 text-right font-semibold whitespace-nowrap", quantity <= 0 && "text-red-600")}>
                                {qty(quantity)} {UNIT_LABELS[i.unit]}
                              </td>
                              <td className="px-3 py-2 text-right">{balance?.averageCostAzn == null ? "—" : formatCurrency(balance.averageCostAzn)}</td>
                              <td className="px-3 py-2 text-right">{formatCurrency(valueOf(i.id))}</td>
                            </>
                          )}
                          <td className="px-3 py-2 text-right whitespace-nowrap">
                            {canChangeStock && canSeeStock && (
                              <>
                                <Button size="icon" variant="outline" className="h-8 w-8" aria-label={`${i.name}: artım`} title="Artım" onClick={() => startOperation(i, "increase")}>
                                  <Plus className="h-4 w-4" />
                                </Button>
                                <Button
                                  size="icon"
                                  variant="outline"
                                  className="ml-1 h-8 w-8"
                                  aria-label={`${i.name}: azalma`}
                                  title="Azalma"
                                  disabled={(balance?.totalQuantity ?? 0) <= 0}
                                  onClick={() => startOperation(i, "broken")}
                                >
                                  <Minus className="h-4 w-4" />
                                </Button>
                              </>
                            )}
                            {can("StockItem.Update") && (
                              <Button size="icon" variant="ghost" className="ml-1 h-8 w-8" aria-label={`${i.name}: düzəlt`} title="Düzəlt" onClick={() => setItemDialog({ itemId: i.id })}>
                                <Pencil className="h-4 w-4" />
                              </Button>
                            )}
                          </td>
                        </tr>
                        {open && (
                          <tr className="bg-muted/15">
                            <td colSpan={columns} className="space-y-3 px-9 py-3">
                              {!canSeeStock ? (
                                <p className="text-xs text-muted-foreground">Qalıqları görmək üçün anbar qalığı icazəsi lazımdır.</p>
                              ) : (
                                <>
                                  <div className="flex flex-wrap gap-2">
                                    {(balance?.warehouses ?? []).length === 0 ? (
                                      <span className="text-xs text-muted-foreground">Heç bir anbarda stok yoxdur.</span>
                                    ) : (
                                      balance!.warehouses.map((w) => (
                                        <span key={w.warehouseId} className="rounded-md border bg-card px-3 py-1.5 text-xs">
                                          <span className="text-muted-foreground">{w.warehouseName}: </span>
                                          <b>
                                            {qty(w.quantity)} {UNIT_LABELS[i.unit]}
                                          </b>
                                        </span>
                                      ))
                                    )}
                                  </div>
                                  <div>
                                    <p className="mb-1 text-xs font-semibold">Tarixçə</p>
                                    <DateTimeRangeBar value={period} className="mb-2" />
                                    {shownHistory === null ? (
                                      <p className="text-xs text-muted-foreground">Yüklənir…</p>
                                    ) : shownHistory.length === 0 ? (
                                      <p className="text-xs text-muted-foreground">
                                        {period.active ? "Bu aralıqda hərəkət yoxdur." : "Hərəkət yoxdur."}
                                      </p>
                                    ) : (
                                      <table className="w-full text-xs">
                                        <tbody>
                                          {shownHistory.slice(0, historyLimit).map((h) => (
                                            <tr key={h.id} className="border-t">
                                              <td className="py-1 pr-3 whitespace-nowrap">{dateTime(h.date)}</td>
                                              <td className="py-1 pr-3">
                                                {h.operation}
                                                {h.documentNo && <span className="ml-1 font-mono text-muted-foreground">{h.documentNo}</span>}
                                              </td>
                                              <td className="py-1 pr-3 text-muted-foreground">{h.warehouseName}</td>
                                              <td className={cn("py-1 pr-3 text-right font-semibold", h.isIncrease ? "text-emerald-600" : "text-red-600")}>
                                                {h.isIncrease ? "+" : "−"}
                                                {qty(h.quantity)}
                                              </td>
                                              <td className="py-1 pr-3 text-muted-foreground">{h.userName ?? ""}</td>
                                              <td className="py-1 text-muted-foreground">{h.note ?? ""}</td>
                                            </tr>
                                          ))}
                                        </tbody>
                                      </table>
                                    )}
                                    {shownHistory && shownHistory.length > historyLimit && (
                                      <Button variant="link" size="sm" className="h-auto p-0 text-xs" onClick={() => setHistoryLimit((n) => n + 50)}>
                                        Daha çox göstər ({shownHistory.length - historyLimit})
                                      </Button>
                                    )}
                                  </div>
                                </>
                              )}
                            </td>
                          </tr>
                        )}
                      </Fragment>
                    );
                  })
                )}
              </tbody>
            </table>
          </div>
        </div>
      </div>

      {companyId && (
        <StockItemDialog
          open={itemDialog != null}
          itemId={itemDialog?.itemId ?? null}
          companyId={companyId}
          defaultCategoryId={categoryId}
          categories={categories}
          restaurants={restaurants}
          canDelete={can("StockItem.Delete")}
          onClose={() => setItemDialog(null)}
          onSaved={() => void load(true)}
        />
      )}
      <StockOperationDialog target={operation} warehouses={warehouses} preferredWarehouseId={warehouseId} onClose={() => setOperation(null)} onDone={afterOperation} />
    </div>
  );
}
