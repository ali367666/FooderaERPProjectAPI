"use client";

import { useEffect, useMemo, useState } from "react";
import { useRouter } from "next/navigation";
import { AdvancedTableFilters, type TableFilterDef } from "@/components/advanced-table-filters";
import { DataTable } from "@/components/data-table";
import {
  searchWarehouseStockDocumentsForAllCompanies,
  warehouseStockDocumentStatusLabel,
  type WarehouseStockDocumentSummary,
} from "@/lib/services/warehouse-stock-service";
import { getWarehouses } from "@/lib/services/warehouse-service";
import { useSelectedCompany } from "@/contexts/selected-company-context";
import { filterBySelectedCompany } from "@/lib/company-scope-utils";

type Row = {
  id: string;
  documentId: number;
  documentNo: string;
  warehouseName: string;
  warehouseId: number;
  warehouseCompanyId: number;
  createdDisplay: string;
  statusLabel: string;
  lineCount: number;
};

export default function WarehouseStockDocumentsPage() {
  const router = useRouter();
  const { companies, companiesLoading, selectedCompanyId: scopeCompanyId } = useSelectedCompany();
  const [documents, setDocuments] = useState<WarehouseStockDocumentSummary[]>([]);
  const [warehouses, setWarehouses] = useState<Awaited<ReturnType<typeof getWarehouses>>>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const loadData = async (silent = false) => {
    if (companies.length === 0) {
      setDocuments([]);
      setWarehouses([]);
      return;
    }
    try {
      if (!silent) setLoading(true);
      setError(null);
      const ids = companies.map((c) => c.id);
      const [docs, wh] = await Promise.all([
        searchWarehouseStockDocumentsForAllCompanies(ids),
        getWarehouses(),
      ]);
      setDocuments(docs);
      setWarehouses(wh);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to load documents.");
    } finally {
      if (!silent) setLoading(false);
    }
  };

  useEffect(() => {
    if (companiesLoading) return;
    void loadData();
  }, [companiesLoading, companies]);

  const warehouseCompanyMap = useMemo(
    () => new Map(warehouses.map((w) => [w.id, w.companyId])),
    [warehouses],
  );

  const warehousesScoped = useMemo(() => {
    if (scopeCompanyId == null) return warehouses;
    return warehouses.filter((w) => w.companyId === scopeCompanyId);
  }, [warehouses, scopeCompanyId]);

  const sortedWarehouseOptions = useMemo(
    () =>
      [...warehousesScoped]
        .sort((a, b) => a.name.localeCompare(b.name))
        .map((w) => ({ value: String(w.id), label: w.name })),
    [warehousesScoped],
  );

  const rows: Row[] = useMemo(
    () =>
      documents.map((d) => ({
        id: String(d.id),
        documentId: d.id,
        documentNo: d.documentNo || `WSD #${d.id}`,
        warehouseName: d.warehouseName || `Warehouse #${d.warehouseId}`,
        warehouseId: d.warehouseId,
        warehouseCompanyId: warehouseCompanyMap.get(d.warehouseId) ?? d.companyId,
        createdDisplay: d.createdAtUtc ? new Date(d.createdAtUtc).toLocaleString() : "—",
        statusLabel: warehouseStockDocumentStatusLabel(d.status),
        lineCount: d.lineCount,
      })),
    [documents, warehouseCompanyMap],
  );

  const scopedRows = useMemo(
    () => filterBySelectedCompany(rows, scopeCompanyId, (r) => r.warehouseCompanyId || null),
    [rows, scopeCompanyId],
  );

  const filterDefs = useMemo<TableFilterDef<Row>[]>(
    () => [
      {
        id: "docId",
        label: "Document ID",
        ui: "number",
        match: (row, get) => {
          const q = get("docId").trim().toLowerCase();
          if (!q) return true;
          return String(row.documentId).includes(q);
        },
      },
      {
        id: "warehouse",
        label: "Warehouse",
        ui: "select",
        options: sortedWarehouseOptions,
        match: (row, get) => {
          const v = get("warehouse");
          if (!v) return true;
          return row.warehouseId === Number(v);
        },
      },
    ],
    [sortedWarehouseOptions],
  );

  const columns = [
    { key: "documentNo" as const, label: "Document no." },
    { key: "warehouseName" as const, label: "Warehouse" },
    { key: "createdDisplay" as const, label: "Created" },
    { key: "statusLabel" as const, label: "Status" },
    { key: "lineCount" as const, label: "Lines" },
  ];

  if (companiesLoading || loading) {
    return <div className="p-6 text-sm text-muted-foreground">Loading warehouse stock documents…</div>;
  }

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-3xl font-bold text-foreground">Stock entry documents</h1>
        <p className="text-muted-foreground mt-1">Draft documents do not change inventory until approved.</p>
      </div>

      {error && (
        <div className="rounded-md border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-600">{error}</div>
      )}

      <AdvancedTableFilters defs={filterDefs} data={scopedRows}>
        {(filtered) => (
          <DataTable
            title="Warehouse stock documents"
            columns={columns}
            data={filtered}
            idSortKey="documentId"
            searchableFields={["documentNo", "warehouseName", "createdDisplay", "statusLabel", "lineCount", "id"]}
            searchPlaceholder="Search documents…"
            onAdd={() => router.push("/dashboard/warehouse-stock-documents/new")}
            onEdit={(row) => router.push(`/dashboard/warehouse-stock-documents/${row.documentId}`)}
          />
        )}
      </AdvancedTableFilters>
    </div>
  );
}
