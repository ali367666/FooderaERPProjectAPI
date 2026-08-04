"use client";

import { useCallback, useEffect, useMemo, useState } from "react";
import { useRouter } from "next/navigation";
import { Eye, Pencil, Plus, Trash2, Send, Check, X } from "lucide-react";
import { AdvancedTableFilters, type TableFilterDef } from "@/components/advanced-table-filters";
import { DocumentActionToolbar } from "@/components/documents/document-action-toolbar";
import { ListSelectionSync } from "@/components/documents/list-selection-sync";
import { Button } from "@/components/ui/button";
import { Checkbox } from "@/components/ui/checkbox";
import { Input } from "@/components/ui/input";
import { Badge } from "@/components/ui/badge";
import { cn } from "@/lib/utils";
import { ConfirmDialog } from "@/components/documents/confirm-dialog";
import { useSelectedCompany } from "@/contexts/selected-company-context";
import { filterBySelectedCompany } from "@/lib/company-scope-utils";
import {
  StockPurchaseStatus,
  CURRENCY_LABELS,
  deleteStockPurchase,
  getStockPurchases,
  submitStockPurchase,
  approveStockPurchase,
  rejectStockPurchase,
  type StockPurchaseDto,
} from "@/lib/services/stock-purchase-service";
import { toApiFormError } from "@/lib/api-error";
import { toast } from "sonner";

const ITEMS_PER_PAGE = 10;

function formatDate(iso: string): string {
  if (!iso) return "—";
  const d = new Date(iso);
  return Number.isNaN(d.getTime()) ? iso : d.toLocaleDateString(undefined, { year: "numeric", month: "short", day: "numeric" });
}

function formatAzn(val: number): string {
  return val.toLocaleString("az-AZ", { minimumFractionDigits: 2, maximumFractionDigits: 2 }) + " ₼";
}

function StatusBadge({ status }: { status: number }) {
  const map: Record<number, { label: string; variant: "default" | "secondary" | "destructive" | "outline" }> = {
    [StockPurchaseStatus.Draft]:     { label: "Draft",     variant: "secondary" },
    [StockPurchaseStatus.Pending]:   { label: "Pending",   variant: "default" },
    [StockPurchaseStatus.Approved]:  { label: "Approved",  variant: "default" },
    [StockPurchaseStatus.Completed]: { label: "Completed", variant: "default" },
    [StockPurchaseStatus.Rejected]:  { label: "Rejected",  variant: "destructive" },
    [StockPurchaseStatus.Cancelled]: { label: "Cancelled", variant: "outline" },
  };
  const info = map[status] ?? { label: String(status), variant: "outline" };
  return <Badge variant={info.variant}>{info.label}</Badge>;
}

type ListRow = {
  id: string;
  numericId: number;
  documentNo: string;
  companyId: number;
  supplierName: string;
  isImport: boolean;
  currencyCode: string;
  exchangeRate: number;
  totalForeign: number;
  totalAzn: number;
  purchaseDateLabel: string;
  warehouseName: string;
  status: number;
  note: string;
  raw: StockPurchaseDto;
};

export function StockPurchaseListPage() {
  const router = useRouter();
  const { companies, selectedCompanyId: scopeCompanyId } = useSelectedCompany();
  const [items, setItems] = useState<StockPurchaseDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [search, setSearch] = useState("");
  const [page, setPage] = useState(1);
  const [sortDesc, setSortDesc] = useState(true);
  const [selectedRowId, setSelectedRowId] = useState<string | null>(null);
  const [deleteTarget, setDeleteTarget] = useState<ListRow | null>(null);
  const [workflowConfirm, setWorkflowConfirm] = useState<
    null | { kind: "approve" | "reject"; numericId: number }
  >(null);

  const load = useCallback(async () => {
    try {
      setLoading(true);
      setError(null);
      const data = await getStockPurchases();
      setItems(data);
    } catch (e) {
      setError(toApiFormError(e, "Failed to load stock purchases").message);
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => { void load(); }, [load]);
  useEffect(() => { setSelectedRowId(null); }, [page]);

  const baseRows: ListRow[] = useMemo(
    () =>
      items.map((r) => ({
        id: String(r.id),
        numericId: r.id,
        documentNo: r.documentNo,
        companyId: r.companyId,
        supplierName: r.supplierName,
        isImport: r.isImport,
        currencyCode: r.currencyCode,
        exchangeRate: r.exchangeRate,
        totalForeign: r.totalForeign,
        totalAzn: r.totalAzn,
        purchaseDateLabel: formatDate(r.purchaseDate),
        warehouseName: r.warehouseName,
        status: r.status,
        note: r.note?.trim() || "—",
        raw: r,
      })),
    [items],
  );

  const scopedRows = useMemo(
    () => filterBySelectedCompany(baseRows, scopeCompanyId, (r) => r.raw.companyId),
    [baseRows, scopeCompanyId],
  );

  const filterDefs = useMemo<TableFilterDef<ListRow>[]>(() => {
    const statusOpts = [
      { value: String(StockPurchaseStatus.Draft),     label: "Draft" },
      { value: String(StockPurchaseStatus.Pending),   label: "Pending" },
      { value: String(StockPurchaseStatus.Completed), label: "Completed" },
      { value: String(StockPurchaseStatus.Rejected),  label: "Rejected" },
      { value: String(StockPurchaseStatus.Cancelled), label: "Cancelled" },
    ];
    const companyOpts = [...companies]
      .sort((a, b) => a.name.localeCompare(b.name))
      .map((c) => ({ value: String(c.id), label: c.name }));
    return [
      {
        id: "status", label: "Status", ui: "select", options: statusOpts,
        match: (row, get) => { const v = get("status"); return !v || String(row.status) === v; },
      },
      {
        id: "company", label: "Company", ui: "select", options: companyOpts,
        match: (row, get) => { const v = get("company"); return !v || String(row.raw.companyId) === v; },
      },
      {
        id: "import", label: "Source", ui: "select",
        options: [{ value: "true", label: "Import (xarici)" }, { value: "false", label: "Local (daxili)" }],
        match: (row, get) => {
          const v = get("import");
          return !v || String(row.isImport) === v;
        },
      },
    ];
  }, [companies]);

  return (
    <div className="space-y-6">
      <div className="flex flex-col gap-3 sm:flex-row sm:items-end sm:justify-between">
        <div>
          <h1 className="text-2xl font-semibold tracking-tight">Stock Purchases</h1>
          <p className="text-muted-foreground text-sm mt-1">
            Baş ofis tərəfindən daxili və ya xarici mal alışları. Xarici alışlar CBAR məzənnəsi ilə AZN-ə çevrilir.
          </p>
        </div>
        <Button onClick={() => router.push("/dashboard/stock-purchases/new")}>
          <Plus className="h-4 w-4 mr-2" />
          New purchase
        </Button>
      </div>

      {error ? (
        <div className="rounded-md border border-destructive/40 bg-destructive/10 px-4 py-3 text-sm text-destructive">{error}</div>
      ) : null}

      {loading ? (
        <p className="text-sm text-muted-foreground">Loading…</p>
      ) : (
        <AdvancedTableFilters defs={filterDefs} data={scopedRows} title="Filters">
          {(filtered) => {
            const searched = filtered.filter((row) => {
              const q = search.trim().toLowerCase();
              if (!q) return true;
              return [row.documentNo, row.supplierName, row.warehouseName, row.note]
                .join(" ").toLowerCase().includes(q);
            });
            const sorted = [...searched].sort((a, b) =>
              sortDesc ? b.numericId - a.numericId : a.numericId - b.numericId,
            );
            const totalPages = Math.max(1, Math.ceil(sorted.length / ITEMS_PER_PAGE));
            const current = Math.min(page, totalPages);
            const slice = sorted.slice((current - 1) * ITEMS_PER_PAGE, current * ITEMS_PER_PAGE);
            const selectedRow = selectedRowId ? (sorted.find((r) => r.id === selectedRowId) ?? null) : null;
            const hasSelection = selectedRow != null;
            const st = selectedRow?.status;

            const canEdit   = hasSelection && st === StockPurchaseStatus.Draft;
            const canDelete = hasSelection && st === StockPurchaseStatus.Draft;
            const canSubmit = hasSelection && st === StockPurchaseStatus.Draft;
            const canApprove = hasSelection && st === StockPurchaseStatus.Pending;
            const canReject  = hasSelection && st === StockPurchaseStatus.Pending;

            return (
              <div className="space-y-4">
                <ListSelectionSync visibleRowIds={sorted.map((r) => r.id)} setSelectedRowId={setSelectedRowId} />
                <div className="flex flex-col sm:flex-row gap-3 sm:items-center sm:justify-between">
                  <Input
                    placeholder="Search document, supplier, warehouse…"
                    value={search}
                    onChange={(e) => { setSearch(e.target.value); setPage(1); }}
                    className="max-w-md"
                  />
                  <Button type="button" variant="outline" size="sm"
                    onClick={() => { setSortDesc((d) => !d); setPage(1); }}>
                    Sort: {sortDesc ? "Newest first" : "Oldest first"}
                  </Button>
                </div>

                <DocumentActionToolbar
                  hasSelection={hasSelection}
                  selectedSummary={selectedRow?.documentNo}
                  actions={[
                    {
                      id: "view", label: "View", icon: Eye, enabled: hasSelection,
                      onClick: () => selectedRow && router.push(`/dashboard/stock-purchases/${selectedRow.numericId}?mode=view`),
                    },
                    {
                      id: "edit", label: "Edit", icon: Pencil, enabled: canEdit,
                      disabledReason: !canEdit ? "Only draft purchases can be edited." : undefined,
                      onClick: () => selectedRow && canEdit && router.push(`/dashboard/stock-purchases/${selectedRow.numericId}?mode=edit`),
                    },
                    {
                      id: "delete", label: "Delete", icon: Trash2, variant: "destructive", enabled: canDelete,
                      disabledReason: !canDelete ? "Only draft purchases can be deleted." : undefined,
                      onClick: () => selectedRow && canDelete && setDeleteTarget(selectedRow),
                    },
                    {
                      id: "submit", label: "Submit", icon: Send, enabled: canSubmit,
                      disabledReason: !canSubmit ? "Only draft purchases can be submitted." : undefined,
                      onClick: async () => {
                        if (!selectedRow || !canSubmit) return;
                        try {
                          await submitStockPurchase(selectedRow.numericId);
                          toast.success("Submitted for approval");
                          void load();
                        } catch (err) { toast.error(toApiFormError(err, "Submit failed").message); }
                      },
                    },
                    {
                      id: "approve", label: "Approve", icon: Check, enabled: canApprove,
                      disabledReason: !canApprove ? "Only pending purchases can be approved." : undefined,
                      onClick: () => selectedRow && canApprove && setWorkflowConfirm({ kind: "approve", numericId: selectedRow.numericId }),
                    },
                    {
                      id: "reject", label: "Reject", icon: X, variant: "outline", enabled: canReject,
                      disabledReason: !canReject ? "Only pending purchases can be rejected." : undefined,
                      onClick: () => selectedRow && canReject && setWorkflowConfirm({ kind: "reject", numericId: selectedRow.numericId }),
                    },
                  ]}
                />

                <div className="rounded-lg border border-border overflow-x-auto">
                  <table className="w-full text-sm leading-[1.2] [&_td]:align-top [&_th]:align-top">
                    <thead>
                      <tr className="border-b border-border bg-muted/40">
                        <th className="w-[1%] py-2 px-3 font-medium" aria-label="Select" />
                        <th className="text-left py-2 px-3 font-medium">Document</th>
                        <th className="text-left py-2 px-3 font-medium">Date</th>
                        <th className="text-left py-2 px-3 font-medium">Supplier</th>
                        <th className="text-left py-2 px-3 font-medium">Source</th>
                        <th className="text-left py-2 px-3 font-medium">Currency</th>
                        <th className="text-right py-2 px-3 font-medium">Exchange Rate</th>
                        <th className="text-right py-2 px-3 font-medium">Total (foreign)</th>
                        <th className="text-right py-2 px-3 font-medium">Total (AZN)</th>
                        <th className="text-left py-2 px-3 font-medium">Warehouse</th>
                        <th className="text-left py-2 px-3 font-medium">Status</th>
                      </tr>
                    </thead>
                    <tbody>
                      {slice.length === 0 ? (
                        <tr>
                          <td colSpan={11} className="py-10 text-center text-muted-foreground">
                            No purchases match your filters.
                          </td>
                        </tr>
                      ) : (
                        slice.map((row) => {
                          const rowSelected = selectedRowId === row.id;
                          return (
                            <tr
                              key={row.id}
                              aria-selected={rowSelected}
                              className={cn(
                                "border-b border-border cursor-pointer transition-colors",
                                "hover:bg-[#f5f5f5] dark:hover:bg-muted/35",
                                rowSelected && "bg-[#e8e8e8] ring-1 ring-inset ring-primary/25 dark:bg-muted/60",
                              )}
                              onClick={() => setSelectedRowId(row.id)}
                            >
                              <td className="py-2 px-3" onClick={(e) => e.stopPropagation()}>
                                <Checkbox
                                  checked={rowSelected}
                                  onCheckedChange={(checked) => setSelectedRowId(checked === true ? row.id : null)}
                                  aria-label={`Select ${row.documentNo}`}
                                />
                              </td>
                              <td className="py-2 px-3 font-mono text-xs">{row.documentNo}</td>
                              <td className="py-2 px-3 text-muted-foreground">{row.purchaseDateLabel}</td>
                              <td className="py-2 px-3">{row.supplierName}</td>
                              <td className="py-2 px-3">
                                <Badge variant={row.isImport ? "default" : "secondary"}>
                                  {row.isImport ? "Import" : "Local"}
                                </Badge>
                              </td>
                              <td className="py-2 px-3 font-medium">{row.currencyCode}</td>
                              <td className="py-2 px-3 text-right tabular-nums">
                                {row.currencyCode === "AZN" ? "—" : row.exchangeRate.toFixed(4)}
                              </td>
                              <td className="py-2 px-3 text-right tabular-nums">
                                {row.currencyCode === "AZN"
                                  ? "—"
                                  : `${row.totalForeign.toLocaleString("az-AZ", { minimumFractionDigits: 2 })} ${row.currencyCode}`}
                              </td>
                              <td className="py-2 px-3 text-right tabular-nums font-medium">
                                {formatAzn(row.totalAzn)}
                              </td>
                              <td className="py-2 px-3">{row.warehouseName}</td>
                              <td className="py-2 px-3"><StatusBadge status={row.status} /></td>
                            </tr>
                          );
                        })
                      )}
                    </tbody>
                  </table>
                </div>

                {totalPages > 1 ? (
                  <div className="flex items-center justify-between text-sm text-muted-foreground">
                    <span>
                      Showing {(current - 1) * ITEMS_PER_PAGE + 1}–
                      {Math.min(current * ITEMS_PER_PAGE, sorted.length)} of {sorted.length}
                    </span>
                    <div className="flex gap-2">
                      <Button type="button" variant="outline" size="sm" disabled={current <= 1}
                        onClick={() => setPage((p) => Math.max(1, p - 1))}>Previous</Button>
                      <Button type="button" variant="outline" size="sm" disabled={current >= totalPages}
                        onClick={() => setPage((p) => Math.min(totalPages, p + 1))}>Next</Button>
                    </div>
                  </div>
                ) : null}
              </div>
            );
          }}
        </AdvancedTableFilters>
      )}

      <ConfirmDialog
        open={deleteTarget != null}
        onOpenChange={(o) => !o && setDeleteTarget(null)}
        title="Delete stock purchase?"
        description="Bu draft sənədi silinəcək."
        confirmLabel="Delete"
        destructive
        onConfirm={async () => {
          if (!deleteTarget) return;
          try {
            await deleteStockPurchase(deleteTarget.numericId);
            toast.success("Deleted");
            setDeleteTarget(null);
            setSelectedRowId(null);
            void load();
          } catch (e) { toast.error(toApiFormError(e, "Delete failed").message); }
        }}
      />

      <ConfirmDialog
        open={workflowConfirm != null}
        onOpenChange={(o) => !o && setWorkflowConfirm(null)}
        title={workflowConfirm?.kind === "approve" ? "Approve purchase?" : "Reject purchase?"}
        description={
          workflowConfirm?.kind === "approve"
            ? "Alış təsdiqlənəcək və anbar stoku artırılacaq."
            : "Alış rədd ediləcək."
        }
        confirmLabel={workflowConfirm?.kind === "approve" ? "Approve" : "Reject"}
        destructive={workflowConfirm?.kind === "reject"}
        onConfirm={async () => {
          if (!workflowConfirm) return;
          const { kind, numericId } = workflowConfirm;
          try {
            if (kind === "approve") { await approveStockPurchase(numericId); toast.success("Approved"); }
            else { await rejectStockPurchase(numericId); toast.success("Rejected"); }
            setWorkflowConfirm(null);
            void load();
          } catch (e) { toast.error(toApiFormError(e, kind === "approve" ? "Approve failed" : "Reject failed").message); }
        }}
      />
    </div>
  );
}
