"use client";

import Link from "next/link";
import { useEffect, useMemo, useState } from "react";
import { AdvancedTableFilters, type TableFilterDef } from "@/components/advanced-table-filters";
import { DataTable } from "@/components/data-table";
import {
  searchStockMovementsForAllCompanies,
  type StockMovementRow,
} from "@/lib/services/stock-movement-service";
import { useSelectedCompany } from "@/contexts/selected-company-context";
import { filterBySelectedCompany } from "@/lib/company-scope-utils";
import { formatUnit } from "@/lib/format-unit";

type Row = Omit<StockMovementRow, "id"> & {
  id: string;
  dateDisplay: string;
  documentDisplay: string;
  fromDisplay: string;
  toDisplay: string;
  quantityDisplay: string;
};

function formatMovementDate(iso: string): string {
  if (!iso) return "—";
  const d = new Date(iso);
  if (Number.isNaN(d.getTime())) return iso;
  return d.toLocaleString(undefined, {
    dateStyle: "short",
    timeStyle: "short",
  });
}

export default function StockMovementsPage() {
  const { companies, companiesLoading, selectedCompanyId: scopeCompanyId } = useSelectedCompany();
  const [movements, setMovements] = useState<StockMovementRow[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const loadData = async (silent = false) => {
    if (companies.length === 0) {
      setMovements([]);
      return;
    }
    try {
      if (!silent) setLoading(true);
      setError(null);
      const ids = companies.map((c) => c.id);
      const rows = await searchStockMovementsForAllCompanies(ids);
      setMovements(rows);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to load stock movements.");
    } finally {
      if (!silent) setLoading(false);
    }
  };

  useEffect(() => {
    if (companiesLoading) return;
    void loadData();
  }, [companiesLoading, companies]);

  const rows: Row[] = useMemo(
    () =>
      movements.map((m) => ({
        ...m,
        id: String(m.id),
        dateDisplay: formatMovementDate(m.movementDate),
        documentDisplay: m.sourceDocumentNo?.trim() ? m.sourceDocumentNo : "—",
        fromDisplay: m.fromWarehouseName?.trim() ? m.fromWarehouseName : "—",
        toDisplay: m.toWarehouseName?.trim() ? m.toWarehouseName : "—",
        quantityDisplay: (() => {
          const u = formatUnit(m.stockItemUnit ?? undefined);
          return u ? `${m.quantity} ${u}` : String(m.quantity);
        })(),
      })),
    [movements],
  );

  const scopedRows = useMemo(
    () => filterBySelectedCompany(rows, scopeCompanyId, (r) => r.companyId || null),
    [rows, scopeCompanyId],
  );

  const filterDefs = useMemo<TableFilterDef<Row>[]>(() => [], []);

  const columns = [
    { key: "documentDisplay" as const, label: "Document no." },
    { key: "stockItemName" as const, label: "Stock item" },
    { key: "fromDisplay" as const, label: "From warehouse" },
    { key: "toDisplay" as const, label: "To warehouse" },
    { key: "quantityDisplay" as const, label: "Quantity" },
    { key: "movementType" as const, label: "Movement type" },
    { key: "dateDisplay" as const, label: "Date" },
  ];

  if (companiesLoading || loading) {
    return <div className="p-6 text-sm text-muted-foreground">Loading stock movements…</div>;
  }

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-3xl font-bold text-foreground">Stock movements</h1>
        <p className="text-muted-foreground mt-1">
          Transaction history (transfers, stock entries). Current balances are on{" "}
          <Link className="text-primary underline" href="/dashboard/warehouse-stocks">
            Warehouse stock balances
          </Link>
          .
        </p>
      </div>

      {error && (
        <div className="rounded-md border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-600">{error}</div>
      )}

      <AdvancedTableFilters defs={filterDefs} data={scopedRows}>
        {(filtered) => (
          <DataTable
            title="Movement history"
            columns={columns}
            data={filtered}
            idSortKey="id"
            searchableFields={[
              "documentDisplay",
              "stockItemName",
              "fromDisplay",
              "toDisplay",
              "movementType",
              "sourceType",
              "warehouseName",
            ]}
            searchPlaceholder="Search document, item, warehouse…"
          />
        )}
      </AdvancedTableFilters>
    </div>
  );
}
