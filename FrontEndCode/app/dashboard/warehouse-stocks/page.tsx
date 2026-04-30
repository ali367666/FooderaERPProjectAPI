"use client";

import { useEffect, useMemo, useState } from "react";
import { AdvancedTableFilters, type TableFilterDef } from "@/components/advanced-table-filters";
import { DataTable } from "@/components/data-table";
import {
  searchWarehouseStockBalancesForAllCompanies,
  type WarehouseStockBalanceRow,
} from "@/lib/services/warehouse-stock-balance-service";
import { getWarehouses } from "@/lib/services/warehouse-service";
import { getStockItemsForAllCompanies, type StockItem } from "@/lib/services/stock-item-service";
import { useSelectedCompany } from "@/contexts/selected-company-context";
import { filterBySelectedCompany } from "@/lib/company-scope-utils";

type Row = Omit<WarehouseStockBalanceRow, "id"> & {
  id: string;
  quantityDisplay: string;
};

export default function WarehouseStockBalancesPage() {
  const { companies, companiesLoading, selectedCompanyId: scopeCompanyId } = useSelectedCompany();
  const [balances, setBalances] = useState<WarehouseStockBalanceRow[]>([]);
  const [warehouses, setWarehouses] = useState<Awaited<ReturnType<typeof getWarehouses>>>([]);
  const [stockItems, setStockItems] = useState<StockItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const loadData = async (silent = false) => {
    if (companies.length === 0) {
      setBalances([]);
      setWarehouses([]);
      setStockItems([]);
      return;
    }
    try {
      if (!silent) setLoading(true);
      setError(null);
      const ids = companies.map((c) => c.id);
      const [bal, wh, items] = await Promise.all([
        searchWarehouseStockBalancesForAllCompanies(ids),
        getWarehouses(),
        getStockItemsForAllCompanies(ids),
      ]);
      setBalances(bal);
      setWarehouses(wh);
      setStockItems(items);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to load balances.");
    } finally {
      if (!silent) setLoading(false);
    }
  };

  useEffect(() => {
    if (companiesLoading) return;
    void loadData();
  }, [companiesLoading, companies]);

  const warehousesScoped = useMemo(() => {
    if (scopeCompanyId == null) return warehouses;
    return warehouses.filter((w) => w.companyId === scopeCompanyId);
  }, [warehouses, scopeCompanyId]);

  const stockItemsScoped = useMemo(() => {
    if (scopeCompanyId == null) return stockItems;
    return stockItems.filter((s) => s.companyId === scopeCompanyId);
  }, [stockItems, scopeCompanyId]);

  const warehouseOptions = useMemo(
    () =>
      [...warehousesScoped]
        .sort((a, b) => a.name.localeCompare(b.name))
        .map((w) => ({ value: String(w.id), label: w.name })),
    [warehousesScoped],
  );

  const stockItemOptions = useMemo(
    () =>
      [...stockItemsScoped]
        .sort((a, b) => a.name.localeCompare(b.name))
        .map((s) => ({ value: String(s.id), label: s.name })),
    [stockItemsScoped],
  );

  const rows: Row[] = useMemo(
    () =>
      balances.map((b) => ({
        ...b,
        id: String(b.id),
        quantityDisplay: `${b.quantity} ${b.unitLabel}`.trim(),
      })),
    [balances],
  );

  const scopedRows = useMemo(
    () => filterBySelectedCompany(rows, scopeCompanyId, (r) => r.companyId || null),
    [rows, scopeCompanyId],
  );

  const filterDefs = useMemo<TableFilterDef<Row>[]>(
    () => [
      {
        id: "warehouse",
        label: "Warehouse",
        ui: "select",
        options: warehouseOptions,
        match: (row, get) => {
          const v = get("warehouse");
          if (!v) return true;
          return row.warehouseId === Number(v);
        },
      },
      {
        id: "stockItem",
        label: "Stock item",
        ui: "select",
        options: stockItemOptions,
        match: (row, get) => {
          const v = get("stockItem");
          if (!v) return true;
          return row.stockItemId === Number(v);
        },
      },
    ],
    [warehouseOptions, stockItemOptions],
  );

  const columns = [
    { key: "warehouseName" as const, label: "Warehouse" },
    { key: "stockItemName" as const, label: "Stock item" },
    { key: "quantityDisplay" as const, label: "Quantity" },
  ];

  if (companiesLoading || loading) {
    return <div className="p-6 text-sm text-muted-foreground">Loading warehouse stock balances…</div>;
  }

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-3xl font-bold text-foreground">Warehouse stock balances</h1>
        <p className="text-muted-foreground mt-1">Current on-hand quantities per warehouse and item (updated when documents are approved or transfers are processed).</p>
      </div>

      {error && (
        <div className="rounded-md border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-600">{error}</div>
      )}

      <AdvancedTableFilters defs={filterDefs} data={scopedRows}>
        {(filtered) => (
          <DataTable
            title="Current stock"
            columns={columns}
            data={filtered}
            idSortKey="id"
            searchableFields={["warehouseName", "stockItemName", "quantityDisplay"]}
            searchPlaceholder="Search warehouse or item…"
          />
        )}
      </AdvancedTableFilters>
    </div>
  );
}
