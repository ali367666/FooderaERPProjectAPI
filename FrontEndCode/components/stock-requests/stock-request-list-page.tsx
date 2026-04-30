"use client";

import { useCallback, useEffect, useMemo, useState } from "react";
import { useRouter } from "next/navigation";
import { Eye, Pencil, Plus, Trash2, Send, Check, X } from "lucide-react";
import { AdvancedTableFilters, type TableFilterDef } from "@/components/advanced-table-filters";
import { DocumentActionToolbar } from "@/components/documents/document-action-toolbar";
import { Button } from "@/components/ui/button";
import { Checkbox } from "@/components/ui/checkbox";
import { Input } from "@/components/ui/input";
import { cn } from "@/lib/utils";
import { StockRequestStatusBadge } from "@/components/documents/document-status-badge";
import { ConfirmDialog } from "@/components/documents/confirm-dialog";
import { ListSelectionSync } from "@/components/documents/list-selection-sync";
import { useSelectedCompany } from "@/contexts/selected-company-context";
import { filterBySelectedCompany } from "@/lib/company-scope-utils";
import {
  StockRequestStatus,
  deleteStockRequest,
  getStockRequests,
  submitStockRequest,
  approveStockRequest,
  rejectStockRequest,
  type StockRequestDto,
} from "@/lib/services/stock-request-service";
import { toApiFormError } from "@/lib/api-error";
import { AppPermissions } from "@/lib/app-permissions";
import { usePermissionSet } from "@/hooks/use-auth-permissions";
import { toast } from "sonner";

const ITEMS_PER_PAGE = 10;

type ListRow = {
  id: string;
  numericId: number;
  documentNo: string;
  requestDateLabel: string;
  companyName: string;
  warehouseSummary: string;
  requestTypeLabel: string;
  status: StockRequestDto["status"];
  note: string;
  lineCount: number;
  raw: StockRequestDto;
};

export function StockRequestListPage() {
  const router = useRouter();
  const permissions = usePermissionSet();
  const { companies, selectedCompanyId: scopeCompanyId } = useSelectedCompany();
  const [items, setItems] = useState<StockRequestDto[]>([]);
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

  const companyNameById = useMemo(() => new Map(companies.map((c) => [c.id, c.name])), [companies]);

  const load = useCallback(async () => {
    try {
      setLoading(true);
      setError(null);
      const data = await getStockRequests();
      setItems(data);
    } catch (e) {
      setError(toApiFormError(e, "Failed to load stock requests").message);
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    void load();
  }, [load]);

  useEffect(() => {
    setSelectedRowId(null);
  }, [page]);

  useEffect(() => {
    if (selectedRowId == null) return;
    const stillThere = items.some((r) => String(r.id) === selectedRowId);
    if (!stillThere) setSelectedRowId(null);
  }, [items, selectedRowId]);

  const baseRows: ListRow[] = useMemo(
    () =>
      items.map((r) => ({
        id: String(r.id),
        numericId: r.id,
        documentNo: `SR-${String(r.id).padStart(5, "0")}`,
        requestDateLabel: "—",
        companyName: companyNameById.get(r.companyId) ?? "—",
        warehouseSummary: `${r.supplyingWarehouseName} → ${r.requestingWarehouseName}`,
        requestTypeLabel: "Inter-warehouse",
        status: r.status,
        note: r.note?.trim() || "—",
        lineCount: r.lines?.length ?? 0,
        raw: r,
      })),
    [items, companyNameById],
  );

  const scopedRows = useMemo(
    () => filterBySelectedCompany(baseRows, scopeCompanyId, (r) => r.raw.companyId),
    [baseRows, scopeCompanyId],
  );

  const canUpdate = permissions.has(AppPermissions.StockRequestUpdate);
  const canDelete = permissions.has(AppPermissions.StockRequestDelete);
  const canSubmit = permissions.has(AppPermissions.StockRequestSubmit);
  const canApprove = permissions.has(AppPermissions.StockRequestApprove);
  const canReject = permissions.has(AppPermissions.StockRequestReject);

  const filterDefs = useMemo<TableFilterDef<ListRow>[]>(() => {
    const statusOpts = [
      { value: String(StockRequestStatus.Draft), label: "Draft" },
      { value: String(StockRequestStatus.Submitted), label: "Submitted" },
      { value: String(StockRequestStatus.Approved), label: "Approved" },
      { value: String(StockRequestStatus.Rejected), label: "Rejected" },
      { value: String(StockRequestStatus.Cancelled), label: "Cancelled" },
      { value: String(StockRequestStatus.Fulfilled), label: "Fulfilled" },
    ];
    const companyOpts = [...companies]
      .sort((a, b) => a.name.localeCompare(b.name))
      .map((c) => ({ value: String(c.id), label: c.name }));
    return [
      {
        id: "status",
        label: "Status",
        ui: "select",
        options: statusOpts,
        match: (row, get) => {
          const v = get("status");
          if (!v) return true;
          return String(row.status) === v;
        },
      },
      {
        id: "company",
        label: "Company",
        ui: "select",
        options: companyOpts,
        match: (row, get) => {
          const v = get("company");
          if (!v) return true;
          return String(row.raw.companyId) === v;
        },
      },
      {
        id: "warehouse",
        label: "Warehouse (name)",
        ui: "text",
        match: (row, get) => {
          const q = (get("warehouse") ?? "").trim().toLowerCase();
          if (!q) return true;
          const h =
            `${row.raw.requestingWarehouseName} ${row.raw.supplyingWarehouseName}`.toLowerCase();
          return h.includes(q);
        },
      },
    ];
  }, [companies]);

  return (
    <div className="space-y-6">
      <div className="flex flex-col gap-3 sm:flex-row sm:items-end sm:justify-between">
        <div>
          <h1 className="text-2xl font-semibold tracking-tight">Stock requests</h1>
          <p className="text-muted-foreground text-sm mt-1">
            Master documents: request stock from a supplying warehouse to a requesting warehouse.
          </p>
        </div>
        <Button onClick={() => router.push("/dashboard/stock-requests/new")}>
          <Plus className="h-4 w-4 mr-2" />
          Create new
        </Button>
      </div>

      {error ? (
        <div className="rounded-md border border-destructive/40 bg-destructive/10 px-4 py-3 text-sm text-destructive">
          {error}
        </div>
      ) : null}

      {loading ? (
        <p className="text-sm text-muted-foreground">Loading…</p>
      ) : (
        <AdvancedTableFilters defs={filterDefs} data={scopedRows} title="Filters">
          {(filtered) => {
            const searched = filtered.filter((row) => {
              const q = search.trim().toLowerCase();
              if (!q) return true;
              return [
                row.documentNo,
                row.companyName,
                row.warehouseSummary,
                row.note,
                row.requestTypeLabel,
              ]
                .join(" ")
                .toLowerCase()
                .includes(q);
            });
            const sorted = [...searched].sort((a, b) =>
              sortDesc ? b.numericId - a.numericId : a.numericId - b.numericId,
            );
            const totalPages = Math.max(1, Math.ceil(sorted.length / ITEMS_PER_PAGE));
            const current = Math.min(page, totalPages);
            const slice = sorted.slice((current - 1) * ITEMS_PER_PAGE, current * ITEMS_PER_PAGE);
            const selectedRow = selectedRowId
              ? (sorted.find((r) => r.id === selectedRowId) ?? null)
              : null;
            const hasSelection = selectedRow != null;
            const st = selectedRow?.status;

            const canEditRow =
              hasSelection &&
              st === StockRequestStatus.Draft &&
              canUpdate;
            const canDeleteRow =
              hasSelection && st === StockRequestStatus.Draft && canDelete;
            const canSubmitRow =
              hasSelection && st === StockRequestStatus.Draft && canSubmit;
            const canApproveRow =
              hasSelection &&
              st === StockRequestStatus.Submitted &&
              canApprove;
            const canRejectRow =
              hasSelection &&
              st === StockRequestStatus.Submitted &&
              canReject;

            return (
              <div className="space-y-4">
                <ListSelectionSync
                  visibleRowIds={sorted.map((r) => r.id)}
                  setSelectedRowId={setSelectedRowId}
                />
                <div className="flex flex-col sm:flex-row gap-3 sm:items-center sm:justify-between">
                  <Input
                    placeholder="Search document no, company, warehouses, note…"
                    value={search}
                    onChange={(e) => {
                      setSearch(e.target.value);
                      setPage(1);
                    }}
                    className="max-w-md"
                  />
                  <Button
                    type="button"
                    variant="outline"
                    size="sm"
                    onClick={() => {
                      setSortDesc((d) => !d);
                      setPage(1);
                    }}
                  >
                    Sort: {sortDesc ? "Newest first" : "Oldest first"}
                  </Button>
                </div>

                <DocumentActionToolbar
                  hasSelection={hasSelection}
                  selectedSummary={selectedRow?.documentNo}
                  actions={[
                    {
                      id: "view",
                      label: "View",
                      icon: Eye,
                      enabled: hasSelection,
                      onClick: () => {
                        if (!selectedRow) return;
                        router.push(`/dashboard/stock-requests/${selectedRow.numericId}?mode=view`);
                      },
                    },
                    {
                      id: "edit",
                      label: "Edit",
                      icon: Pencil,
                      enabled: canEditRow,
                      disabledReason:
                        st !== StockRequestStatus.Draft
                          ? "Only draft requests can be edited."
                          : !canUpdate
                            ? "You do not have permission to edit stock requests."
                            : undefined,
                      onClick: () => {
                        if (!selectedRow || !canEditRow) return;
                        router.push(`/dashboard/stock-requests/${selectedRow.numericId}?mode=edit`);
                      },
                    },
                    {
                      id: "delete",
                      label: "Delete",
                      icon: Trash2,
                      variant: "destructive",
                      enabled: canDeleteRow,
                      disabledReason:
                        st !== StockRequestStatus.Draft
                          ? "Only draft requests can be deleted."
                          : !canDelete
                            ? "You do not have permission to delete stock requests."
                            : undefined,
                      onClick: () => {
                        if (!selectedRow || !canDeleteRow) return;
                        setDeleteTarget(selectedRow);
                      },
                    },
                    {
                      id: "submit",
                      label: "Submit",
                      icon: Send,
                      enabled: canSubmitRow,
                      disabledReason:
                        st !== StockRequestStatus.Draft
                          ? "Only draft requests can be submitted."
                          : !canSubmit
                            ? "You do not have permission to submit stock requests."
                            : undefined,
                      onClick: async () => {
                        if (!selectedRow || !canSubmitRow) return;
                        try {
                          await submitStockRequest(selectedRow.numericId);
                          toast.success("Submitted");
                          void load();
                        } catch (err) {
                          toast.error(toApiFormError(err, "Submit failed").message);
                        }
                      },
                    },
                    {
                      id: "approve",
                      label: "Approve",
                      icon: Check,
                      enabled: canApproveRow,
                      disabledReason:
                        st !== StockRequestStatus.Submitted
                          ? "Only submitted requests can be approved."
                          : !canApprove
                            ? "You do not have permission to approve stock requests."
                            : undefined,
                      onClick: () => {
                        if (!selectedRow || !canApproveRow) return;
                        setWorkflowConfirm({ kind: "approve", numericId: selectedRow.numericId });
                      },
                    },
                    {
                      id: "reject",
                      label: "Reject",
                      icon: X,
                      variant: "outline",
                      enabled: canRejectRow,
                      disabledReason:
                        st !== StockRequestStatus.Submitted
                          ? "Only submitted requests can be rejected."
                          : !canReject
                            ? "You do not have permission to reject stock requests."
                            : undefined,
                      onClick: () => {
                        if (!selectedRow || !canRejectRow) return;
                        setWorkflowConfirm({ kind: "reject", numericId: selectedRow.numericId });
                      },
                    },
                  ]}
                />

                <div className="rounded-lg border border-border overflow-x-auto">
                  <table className="w-full text-sm leading-[1.2] [&_td]:align-top [&_th]:align-top">
                    <thead>
                      <tr className="border-b border-border bg-muted/40">
                        <th className="w-[1%] py-2 px-3 font-medium" aria-label="Select row" />
                        <th className="text-left py-2 px-3 font-medium">Document</th>
                        <th className="text-left py-2 px-3 font-medium">Request date</th>
                        <th className="text-left py-2 px-3 font-medium">Company</th>
                        <th className="text-left py-2 px-3 font-medium">Warehouses</th>
                        <th className="text-left py-2 px-3 font-medium">Type</th>
                        <th className="text-left py-2 px-3 font-medium">Status</th>
                        <th className="text-left py-2 px-3 font-medium">Note</th>
                        <th className="text-right py-2 px-3 font-medium">Lines</th>
                      </tr>
                    </thead>
                    <tbody>
                      {slice.length === 0 ? (
                        <tr>
                          <td colSpan={9} className="py-10 text-center text-muted-foreground">
                            No stock requests match your filters.
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
                                rowSelected &&
                                  "bg-[#e8e8e8] ring-1 ring-inset ring-primary/25 dark:bg-muted/60",
                              )}
                              onClick={() => setSelectedRowId(row.id)}
                            >
                              <td className="py-2 px-3" onClick={(e) => e.stopPropagation()}>
                                <Checkbox
                                  checked={rowSelected}
                                  onCheckedChange={(checked) => {
                                    setSelectedRowId(checked === true ? row.id : null);
                                  }}
                                  aria-label={`Select ${row.documentNo}`}
                                />
                              </td>
                              <td className="py-2 px-3 font-mono text-xs">{row.documentNo}</td>
                              <td className="py-2 px-3 text-muted-foreground">{row.requestDateLabel}</td>
                              <td className="py-2 px-3">{row.companyName}</td>
                              <td className="py-2 px-3 max-w-[220px] truncate" title={row.warehouseSummary}>
                                {row.warehouseSummary}
                              </td>
                              <td className="py-2 px-3">{row.requestTypeLabel}</td>
                              <td className="py-2 px-3">
                                <StockRequestStatusBadge status={row.status} />
                              </td>
                              <td className="py-2 px-3 max-w-[180px] truncate" title={row.note}>
                                {row.note}
                              </td>
                              <td className="py-2 px-3 text-right tabular-nums">{row.lineCount}</td>
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
                      <Button
                        type="button"
                        variant="outline"
                        size="sm"
                        disabled={current <= 1}
                        onClick={() => setPage((p) => Math.max(1, p - 1))}
                      >
                        Previous
                      </Button>
                      <Button
                        type="button"
                        variant="outline"
                        size="sm"
                        disabled={current >= totalPages}
                        onClick={() => setPage((p) => Math.min(totalPages, p + 1))}
                      >
                        Next
                      </Button>
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
        title="Delete stock request?"
        description="This removes the draft document permanently."
        confirmLabel="Delete"
        destructive
        onConfirm={async () => {
          if (!deleteTarget) return;
          try {
            await deleteStockRequest(deleteTarget.numericId);
            toast.success("Deleted");
            setDeleteTarget(null);
            setSelectedRowId(null);
            void load();
          } catch (e) {
            toast.error(toApiFormError(e, "Delete failed").message);
          }
        }}
      />

      <ConfirmDialog
        open={workflowConfirm != null}
        onOpenChange={(o) => !o && setWorkflowConfirm(null)}
        title={workflowConfirm?.kind === "reject" ? "Reject stock request?" : "Approve stock request?"}
        description={
          workflowConfirm?.kind === "reject"
            ? "The request will be marked as rejected."
            : "The request will be marked as approved."
        }
        confirmLabel={workflowConfirm?.kind === "reject" ? "Reject" : "Approve"}
        destructive={workflowConfirm?.kind === "reject"}
        onConfirm={async () => {
          if (!workflowConfirm) return;
          const { kind, numericId } = workflowConfirm;
          try {
            if (kind === "approve") {
              await approveStockRequest(numericId);
              toast.success("Approved");
            } else {
              await rejectStockRequest(numericId);
              toast.success("Rejected");
            }
            setWorkflowConfirm(null);
            void load();
          } catch (e) {
            toast.error(
              toApiFormError(e, kind === "approve" ? "Approve failed" : "Reject failed").message,
            );
          }
        }}
      />
    </div>
  );
}
