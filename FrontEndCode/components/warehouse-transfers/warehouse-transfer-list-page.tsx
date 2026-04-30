"use client";

import { useCallback, useEffect, useMemo, useState } from "react";
import { useRouter } from "next/navigation";
import {
  Eye,
  Pencil,
  Plus,
  Trash2,
  Send,
  Check,
  X,
  Truck,
  PackageCheck,
  Ban,
} from "lucide-react";
import { AdvancedTableFilters, type TableFilterDef } from "@/components/advanced-table-filters";
import { DocumentActionToolbar } from "@/components/documents/document-action-toolbar";
import { ListSelectionSync } from "@/components/documents/list-selection-sync";
import { Button } from "@/components/ui/button";
import { Checkbox } from "@/components/ui/checkbox";
import { Input } from "@/components/ui/input";
import { cn } from "@/lib/utils";
import { WarehouseTransferStatusBadge } from "@/components/documents/document-status-badge";
import { ConfirmDialog } from "@/components/documents/confirm-dialog";
import { useSelectedCompany } from "@/contexts/selected-company-context";
import { filterBySelectedCompany } from "@/lib/company-scope-utils";
import {
  TransferStatus,
  deleteWarehouseTransfer,
  getWarehouseTransfers,
  submitWarehouseTransfer,
  approveWarehouseTransfer,
  rejectWarehouseTransfer,
  dispatchWarehouseTransfer,
  receiveWarehouseTransfer,
  cancelWarehouseTransfer,
  type WarehouseTransferDto,
} from "@/lib/services/warehouse-transfer-service";
import { toApiFormError } from "@/lib/api-error";
import { toast } from "sonner";

const ITEMS_PER_PAGE = 10;

function formatTransferDate(iso: string): string {
  if (!iso) return "—";
  const d = new Date(iso);
  if (Number.isNaN(d.getTime())) return iso;
  return d.toLocaleDateString(undefined, { year: "numeric", month: "short", day: "numeric" });
}

type ListRow = {
  id: string;
  numericId: number;
  documentNo: string;
  transferDateLabel: string;
  companyName: string;
  fromTo: string;
  status: WarehouseTransferDto["status"];
  note: string;
  lineCount: number;
  raw: WarehouseTransferDto;
};

export function WarehouseTransferListPage() {
  const router = useRouter();
  const { companies, selectedCompanyId: scopeCompanyId } = useSelectedCompany();
  const [items, setItems] = useState<WarehouseTransferDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [search, setSearch] = useState("");
  const [page, setPage] = useState(1);
  const [sortDesc, setSortDesc] = useState(true);
  const [selectedRowId, setSelectedRowId] = useState<string | null>(null);
  const [deleteTarget, setDeleteTarget] = useState<ListRow | null>(null);
  const [workflowConfirm, setWorkflowConfirm] = useState<
    null | { kind: "approve" | "reject" | "cancel"; numericId: number }
  >(null);

  const companyNameById = useMemo(() => new Map(companies.map((c) => [c.id, c.name])), [companies]);

  const load = useCallback(async () => {
    try {
      setLoading(true);
      setError(null);
      const data = await getWarehouseTransfers();
      setItems(data);
    } catch (e) {
      setError(toApiFormError(e, "Failed to load warehouse transfers").message);
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
        documentNo: `WT-${String(r.id).padStart(5, "0")}`,
        transferDateLabel: formatTransferDate(r.transferDate),
        companyName: companyNameById.get(r.companyId) ?? "—",
        fromTo: `${r.fromWarehouseName} → ${r.toWarehouseName}`,
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

  const filterDefs = useMemo<TableFilterDef<ListRow>[]>(() => {
    const statusOpts = [
      { value: String(TransferStatus.Draft), label: "Draft" },
      { value: String(TransferStatus.Pending), label: "Pending" },
      { value: String(TransferStatus.Approved), label: "Approved" },
      { value: String(TransferStatus.InTransit), label: "In transit" },
      { value: String(TransferStatus.Completed), label: "Completed" },
      { value: String(TransferStatus.Rejected), label: "Rejected" },
      { value: String(TransferStatus.Cancelled), label: "Cancelled" },
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
          const h = `${row.raw.fromWarehouseName} ${row.raw.toWarehouseName}`.toLowerCase();
          return h.includes(q);
        },
      },
    ];
  }, [companies]);

  return (
    <div className="space-y-6">
      <div className="flex flex-col gap-3 sm:flex-row sm:items-end sm:justify-between">
        <div>
          <h1 className="text-2xl font-semibold tracking-tight">Warehouse transfers</h1>
          <p className="text-muted-foreground text-sm mt-1">
            Master documents: move stock between warehouses (vehicle leg required for submit).
          </p>
        </div>
        <Button onClick={() => router.push("/dashboard/warehouse-transfers/new")}>
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
              return [row.documentNo, row.companyName, row.fromTo, row.note]
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

            const cancelBlocked =
              st === TransferStatus.Completed ||
              st === TransferStatus.InTransit ||
              st === TransferStatus.Rejected ||
              st === TransferStatus.Cancelled;

            const canEditRow = hasSelection && st === TransferStatus.Draft;
            const canDeleteRow = hasSelection && st === TransferStatus.Draft;
            const canSubmitRow = hasSelection && st === TransferStatus.Draft;
            const canApproveRow = hasSelection && st === TransferStatus.Pending;
            const canRejectRow = hasSelection && st === TransferStatus.Pending;
            const canDispatchRow = hasSelection && st === TransferStatus.Approved;
            const canReceiveRow = hasSelection && st === TransferStatus.InTransit;
            const canCancelRow = hasSelection && !cancelBlocked;

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
                        router.push(`/dashboard/warehouse-transfers/${selectedRow.numericId}?mode=view`);
                      },
                    },
                    {
                      id: "edit",
                      label: "Edit",
                      icon: Pencil,
                      enabled: canEditRow,
                      disabledReason:
                        st !== TransferStatus.Draft ? "Only draft transfers can be edited." : undefined,
                      onClick: () => {
                        if (!selectedRow || !canEditRow) return;
                        router.push(`/dashboard/warehouse-transfers/${selectedRow.numericId}?mode=edit`);
                      },
                    },
                    {
                      id: "delete",
                      label: "Delete",
                      icon: Trash2,
                      variant: "destructive",
                      enabled: canDeleteRow,
                      disabledReason:
                        st !== TransferStatus.Draft ? "Only draft transfers can be deleted." : undefined,
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
                        st !== TransferStatus.Draft ? "Only draft transfers can be submitted." : undefined,
                      onClick: async () => {
                        if (!selectedRow || !canSubmitRow) return;
                        try {
                          await submitWarehouseTransfer(selectedRow.numericId);
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
                        st !== TransferStatus.Pending
                          ? "Only pending transfers can be approved."
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
                        st !== TransferStatus.Pending
                          ? "Only pending transfers can be rejected."
                          : undefined,
                      onClick: () => {
                        if (!selectedRow || !canRejectRow) return;
                        setWorkflowConfirm({ kind: "reject", numericId: selectedRow.numericId });
                      },
                    },
                    {
                      id: "dispatch",
                      label: "Dispatch",
                      icon: Truck,
                      enabled: canDispatchRow,
                      disabledReason:
                        st !== TransferStatus.Approved
                          ? "Only approved transfers can be dispatched."
                          : undefined,
                      onClick: async () => {
                        if (!selectedRow || !canDispatchRow) return;
                        try {
                          await dispatchWarehouseTransfer(selectedRow.numericId);
                          toast.success("Dispatched");
                          void load();
                        } catch (err) {
                          toast.error(toApiFormError(err, "Dispatch failed").message);
                        }
                      },
                    },
                    {
                      id: "receive",
                      label: "Receive",
                      icon: PackageCheck,
                      enabled: canReceiveRow,
                      disabledReason:
                        st !== TransferStatus.InTransit
                          ? "Only in-transit transfers can be received."
                          : undefined,
                      onClick: async () => {
                        if (!selectedRow || !canReceiveRow) return;
                        try {
                          await receiveWarehouseTransfer(selectedRow.numericId);
                          toast.success("Received");
                          void load();
                        } catch (err) {
                          toast.error(toApiFormError(err, "Receive failed").message);
                        }
                      },
                    },
                    {
                      id: "cancel",
                      label: "Cancel",
                      icon: Ban,
                      variant: "outline",
                      enabled: canCancelRow,
                      disabledReason: cancelBlocked
                        ? "This transfer cannot be cancelled in its current status."
                        : undefined,
                      onClick: () => {
                        if (!selectedRow || !canCancelRow) return;
                        setWorkflowConfirm({ kind: "cancel", numericId: selectedRow.numericId });
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
                        <th className="text-left py-2 px-3 font-medium">Transfer date</th>
                        <th className="text-left py-2 px-3 font-medium">Company</th>
                        <th className="text-left py-2 px-3 font-medium">From → To</th>
                        <th className="text-left py-2 px-3 font-medium">Status</th>
                        <th className="text-left py-2 px-3 font-medium">Note</th>
                        <th className="text-right py-2 px-3 font-medium">Lines</th>
                      </tr>
                    </thead>
                    <tbody>
                      {slice.length === 0 ? (
                        <tr>
                          <td colSpan={8} className="py-10 text-center text-muted-foreground">
                            No transfers match your filters.
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
                              <td className="py-2 px-3 text-muted-foreground">{row.transferDateLabel}</td>
                              <td className="py-2 px-3">{row.companyName}</td>
                              <td className="py-2 px-3 max-w-[240px] truncate" title={row.fromTo}>
                                {row.fromTo}
                              </td>
                              <td className="py-2 px-3">
                                <WarehouseTransferStatusBadge status={row.status} />
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
        title="Delete warehouse transfer?"
        description="This removes the draft document permanently."
        confirmLabel="Delete"
        destructive
        onConfirm={async () => {
          if (!deleteTarget) return;
          try {
            await deleteWarehouseTransfer(deleteTarget.numericId);
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
        title={
          workflowConfirm?.kind === "cancel"
            ? "Cancel warehouse transfer?"
            : workflowConfirm?.kind === "reject"
              ? "Reject warehouse transfer?"
              : "Approve warehouse transfer?"
        }
        description={
          workflowConfirm?.kind === "cancel"
            ? "The transfer will be marked as cancelled."
            : workflowConfirm?.kind === "reject"
              ? "The transfer will be marked as rejected."
              : "The transfer will be marked as approved."
        }
        confirmLabel={
          workflowConfirm?.kind === "cancel"
            ? "Cancel transfer"
            : workflowConfirm?.kind === "reject"
              ? "Reject"
              : "Approve"
        }
        destructive={workflowConfirm?.kind === "reject" || workflowConfirm?.kind === "cancel"}
        onConfirm={async () => {
          if (!workflowConfirm) return;
          const { kind, numericId } = workflowConfirm;
          try {
            if (kind === "approve") {
              await approveWarehouseTransfer(numericId);
              toast.success("Approved");
            } else if (kind === "reject") {
              await rejectWarehouseTransfer(numericId);
              toast.success("Rejected");
            } else {
              await cancelWarehouseTransfer(numericId);
              toast.success("Cancelled");
            }
            setWorkflowConfirm(null);
            void load();
          } catch (e) {
            const msg =
              kind === "approve"
                ? "Approve failed"
                : kind === "reject"
                  ? "Reject failed"
                  : "Cancel failed";
            toast.error(toApiFormError(e, msg).message);
          }
        }}
      />
    </div>
  );
}
