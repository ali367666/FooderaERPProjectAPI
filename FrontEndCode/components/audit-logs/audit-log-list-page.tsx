"use client";

import { useCallback, useEffect, useMemo, useState } from "react";
import { useRouter } from "next/navigation";
import { Eye } from "lucide-react";
import { AdvancedTableFilters, type TableFilterDef } from "@/components/advanced-table-filters";
import { DocumentActionToolbar } from "@/components/documents/document-action-toolbar";
import { ListSelectionSync } from "@/components/documents/list-selection-sync";
import { Button } from "@/components/ui/button";
import { Checkbox } from "@/components/ui/checkbox";
import { Input } from "@/components/ui/input";
import { Badge } from "@/components/ui/badge";
import {
  Sheet,
  SheetContent,
  SheetHeader,
  SheetTitle,
} from "@/components/ui/sheet";
import { cn } from "@/lib/utils";
import { getAuditLogById, getAuditLogs, type AuditLogDto } from "@/lib/services/audit-log-service";
import { auditLogActorSearchText, getAuditLogActorDisplay } from "@/lib/audit-user-display";
import { toApiFormError } from "@/lib/api-error";
import { useSelectedCompany } from "@/contexts/selected-company-context";
import { filterBySelectedCompany } from "@/lib/company-scope-utils";
import { useHasPermission } from "@/hooks/use-auth-permissions";
import { toast } from "sonner";

const ITEMS_PER_PAGE = 10;
const AUDIT_VIEW = "Permissions.AuditLog.View";

type SortMode = "newest" | "oldest" | "idAsc" | "idDesc";

type ListRow = {
  id: string;
  numericId: number;
  userDisplayPrimary: string;
  userDisplaySecondary: string | null;
  userSearchText: string;
  actionType: string;
  entityName: string;
  entityId: string;
  oldSummary: string;
  newSummary: string;
  successLabel: string;
  createdLabel: string;
  createdMs: number;
  raw: AuditLogDto;
};

function formatCreated(iso: string): { label: string; ms: number } {
  if (!iso) return { label: "—", ms: 0 };
  const d = new Date(iso);
  if (Number.isNaN(d.getTime())) return { label: iso, ms: 0 };
  return {
    label: d.toLocaleString(undefined, {
      year: "numeric",
      month: "short",
      day: "numeric",
      hour: "2-digit",
      minute: "2-digit",
    }),
    ms: d.getTime(),
  };
}

function summarizeJson(s: string | null, max = 48): string {
  if (!s?.trim()) return "—";
  const t = s.trim();
  if (t.length <= max) return t;
  return `${t.slice(0, max - 1)}…`;
}

function prettyBlock(s: string | null): string {
  if (!s?.trim()) return "—";
  try {
    const o = JSON.parse(s) as unknown;
    return JSON.stringify(o, null, 2);
  } catch {
    return s;
  }
}

function getEntityRoute(entityName: string, entityId: string | number) {
  switch (entityName) {
    case "StockCategory":
      return `/dashboard/stock-categories?selectedId=${entityId}`;
    case "StockItem":
      return `/dashboard/stock-items?selectedId=${entityId}`;
    case "Warehouse":
      return `/dashboard/warehouses?selectedId=${entityId}`;
    case "Employee":
      return `/dashboard/employees/${entityId}`;
    case "Position":
      return `/dashboard/positions/${entityId}`;
    case "Department":
      return `/dashboard/departments/${entityId}`;
    case "Order":
      return `/dashboard/orders/${entityId}`;
    case "StockRequest":
      return `/dashboard/stock-requests/${entityId}`;
    case "WarehouseTransfer":
      return `/dashboard/warehouse-transfers/${entityId}`;
    default:
      return null;
  }
}

function AuditLogActorCell({
  primary,
  secondary,
  className,
}: {
  primary: string;
  secondary: string | null;
  className?: string;
}) {
  return (
    <div className={cn("flex min-w-0 flex-col gap-0 leading-tight", className)}>
      <span className="whitespace-nowrap">{primary}</span>
      {secondary ? (
        <span className="text-xs text-muted-foreground whitespace-nowrap">({secondary})</span>
      ) : null}
    </div>
  );
}

export function AuditLogListPage() {
  const router = useRouter();
  const canView = useHasPermission(AUDIT_VIEW);
  const { selectedCompanyId } = useSelectedCompany();
  const [items, setItems] = useState<AuditLogDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [search, setSearch] = useState("");
  const [page, setPage] = useState(1);
  const [sortMode, setSortMode] = useState<SortMode>("newest");
  const [selectedRowId, setSelectedRowId] = useState<string | null>(null);
  const [detailOpen, setDetailOpen] = useState(false);
  const [detailRecord, setDetailRecord] = useState<AuditLogDto | null>(null);
  const [detailLoading, setDetailLoading] = useState(false);

  const load = useCallback(async () => {
    try {
      setLoading(true);
      setError(null);
      const data = await getAuditLogs();
      setItems(data);
    } catch (e) {
      setError(toApiFormError(e, "Failed to load audit logs").message);
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    if (!canView) return;
    void load();
  }, [canView, load]);

  useEffect(() => {
    setSelectedRowId(null);
  }, [page]);

  useEffect(() => {
    if (selectedRowId == null) return;
    if (!items.some((r) => String(r.id) === selectedRowId)) setSelectedRowId(null);
  }, [items, selectedRowId]);

  const baseRows: ListRow[] = useMemo(
    () =>
      items.map((r) => {
        const { label, ms } = formatCreated(r.createdAtUtc);
        const actor = getAuditLogActorDisplay(r);
        return {
          id: String(r.id),
          numericId: r.id,
          userDisplayPrimary: actor.primary,
          userDisplaySecondary: actor.secondary,
          userSearchText: auditLogActorSearchText(r),
          actionType: r.actionType,
          entityName: r.entityName,
          entityId: r.entityId,
          oldSummary: summarizeJson(r.oldValues),
          newSummary: summarizeJson(r.newValues),
          successLabel: r.isSuccess ? "Successful" : "Failed",
          createdLabel: label,
          createdMs: ms,
          raw: r,
        };
      }),
    [items],
  );

  const scopedRows = useMemo(
    () => filterBySelectedCompany(baseRows, selectedCompanyId, (r) => r.raw.companyId),
    [baseRows, selectedCompanyId],
  );

  const filterDefs = useMemo<TableFilterDef<ListRow>[]>(
    () => [
      {
        id: "aid",
        label: "Log ID",
        ui: "text",
        match: (row, get) => {
          const q = (get("aid") ?? "").trim();
          if (!q) return true;
          return String(row.numericId).includes(q);
        },
      },
      {
        id: "auser",
        label: "User",
        ui: "text",
        match: (row, get) => {
          const q = (get("auser") ?? "").trim();
          if (!q) return true;
          const hay = row.userSearchText.toLowerCase();
          return hay.includes(q.toLowerCase());
        },
      },
      {
        id: "aaction",
        label: "Action",
        ui: "text",
        match: (row, get) => {
          const q = (get("aaction") ?? "").trim().toLowerCase();
          if (!q) return true;
          return row.actionType.toLowerCase().includes(q);
        },
      },
      {
        id: "aentity",
        label: "Module / entity",
        ui: "text",
        match: (row, get) => {
          const q = (get("aentity") ?? "").trim().toLowerCase();
          if (!q) return true;
          return row.entityName.toLowerCase().includes(q);
        },
      },
      {
        id: "adoc",
        label: "Entity ID",
        ui: "text",
        match: (row, get) => {
          const q = (get("adoc") ?? "").trim().toLowerCase();
          if (!q) return true;
          return row.entityId.toLowerCase().includes(q);
        },
      },
      {
        id: "acreated",
        label: "Created",
        ui: "dateRange",
        gridClassName: "min-w-[260px] max-w-full flex-[2_1_360px]",
        match: (row, get) => {
          const from = get("acreated:from");
          const to = get("acreated:to");
          if (!from && !to) return true;
          if (!row.createdMs) return false;
          const day = new Date(row.createdMs);
          const iso = day.toISOString().slice(0, 10);
          if (from && iso < from) return false;
          if (to && iso > to) return false;
          return true;
        },
      },
    ],
    [],
  );

  const sortLabel: Record<SortMode, string> = {
    newest: "Newest first",
    oldest: "Oldest first",
    idAsc: "ID ascending",
    idDesc: "ID descending",
  };

  const openDetail = async (row: ListRow) => {
    setDetailOpen(true);
    setDetailLoading(true);
    setDetailRecord(row.raw);
    try {
      const fresh = await getAuditLogById(row.numericId);
      setDetailRecord(fresh);
    } catch {
      /* keep list copy */
    } finally {
      setDetailLoading(false);
    }
  };

  if (!canView) {
    return (
      <div className="space-y-6">
        <div>
          <h1 className="text-2xl font-semibold tracking-tight">Audit logs</h1>
          <p className="text-muted-foreground text-sm mt-1">
            You do not have permission to view audit logs.
          </p>
        </div>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-semibold tracking-tight">Audit logs</h1>
        <p className="text-muted-foreground text-sm mt-1">
          Read-only history of important changes across the ERP (who did what, and when).
        </p>
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
              const blob = [
                row.userSearchText,
                row.actionType,
                row.entityName,
                row.entityId,
                row.raw.message,
                row.raw.oldValues ?? "",
                row.raw.newValues ?? "",
              ]
                .join(" ")
                .toLowerCase();
              return blob.includes(q);
            });

            const sorted = [...searched].sort((a, b) => {
              switch (sortMode) {
                case "oldest":
                  return a.createdMs - b.createdMs;
                case "idAsc":
                  return Number(a.numericId) - Number(b.numericId);
                case "idDesc":
                  return Number(b.numericId) - Number(a.numericId);
                case "newest":
                default:
                  return b.createdMs - a.createdMs;
              }
            });

            const totalPages = Math.max(1, Math.ceil(sorted.length / ITEMS_PER_PAGE));
            const current = Math.min(page, totalPages);
            const slice = sorted.slice((current - 1) * ITEMS_PER_PAGE, current * ITEMS_PER_PAGE);

            const selectedRow = selectedRowId
              ? (sorted.find((r) => r.id === selectedRowId) ?? null)
              : null;
            const hasSelection = selectedRow != null;

            return (
              <div className="space-y-4">
                <ListSelectionSync
                  visibleRowIds={sorted.map((r) => r.id)}
                  setSelectedRowId={setSelectedRowId}
                />

                <div className="flex flex-col gap-3 lg:flex-row lg:items-center lg:justify-between">
                  <Input
                    placeholder="Search user, action, module, entity, values…"
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
                      setSortMode((m) => {
                        const order: SortMode[] = ["newest", "oldest", "idDesc", "idAsc"];
                        const i = order.indexOf(m);
                        return order[(i + 1) % order.length]!;
                      });
                      setPage(1);
                    }}
                  >
                    Sort: {sortLabel[sortMode]}
                  </Button>
                </div>

                <DocumentActionToolbar
                  hasSelection={hasSelection}
                  selectedSummary={selectedRow ? `#${selectedRow.numericId}` : null}
                  actions={[
                    {
                      id: "view",
                      label: "View details",
                      icon: Eye,
                      enabled: hasSelection,
                      onClick: () => {
                        if (!selectedRow) return;
                        void openDetail(selectedRow);
                      },
                    },
                  ]}
                />

                <div className="rounded-lg border border-border overflow-x-auto">
                  <table className="w-full text-sm leading-[1.2] [&_td]:align-top [&_th]:align-top">
                    <thead>
                      <tr className="border-b border-border bg-muted/40">
                        <th className="w-[1%] py-2 px-3 font-medium" aria-label="Select" />
                        <th className="text-right py-2 px-3 font-medium">ID</th>
                        <th className="text-left py-2 px-3 font-medium">User</th>
                        <th className="text-left py-2 px-3 font-medium">Action</th>
                        <th className="text-left py-2 px-3 font-medium">Entity</th>
                        <th className="text-left py-2 px-3 font-medium">Entity ID</th>
                        <th className="text-left py-2 px-3 font-medium">Old</th>
                        <th className="text-left py-2 px-3 font-medium">New</th>
                        <th className="text-left py-2 px-3 font-medium">Success</th>
                        <th className="text-left py-2 px-3 font-medium">Created</th>
                      </tr>
                    </thead>
                    <tbody>
                      {slice.length === 0 ? (
                        <tr>
                          <td colSpan={10} className="py-12 text-center text-muted-foreground">
                            No audit logs match your filters.
                          </td>
                        </tr>
                      ) : (
                        slice.map((row) => {
                          const sel = selectedRowId === row.id;
                          return (
                            <tr
                              key={row.id}
                              aria-selected={sel}
                              className={cn(
                                "border-b border-border cursor-pointer transition-colors",
                                "hover:bg-[#f5f5f5] dark:hover:bg-muted/35",
                                sel && "bg-[#e8e8e8] ring-1 ring-inset ring-primary/25 dark:bg-muted/60",
                              )}
                              onClick={() => setSelectedRowId(row.id)}
                              onDoubleClick={() => void openDetail(row)}
                            >
                              <td className="py-2 px-3" onClick={(e) => e.stopPropagation()}>
                                <Checkbox
                                  checked={sel}
                                  onCheckedChange={(c) =>
                                    setSelectedRowId(c === true ? row.id : null)
                                  }
                                  aria-label={`Select log ${row.numericId}`}
                                />
                              </td>
                              <td className="py-2 px-3 text-right tabular-nums text-muted-foreground">
                                {row.numericId}
                              </td>
                              <td className="py-2 px-3">
                                <AuditLogActorCell
                                  primary={row.userDisplayPrimary}
                                  secondary={row.userDisplaySecondary}
                                />
                              </td>
                              <td className="py-2 px-3 max-w-[120px] truncate" title={row.actionType}>
                                {row.actionType}
                              </td>
                              <td className="py-2 px-3 max-w-[140px] truncate" title={row.entityName}>
                                {row.entityName}
                              </td>
                              <td className="py-2 px-3 max-w-[100px] truncate font-mono text-xs" title={row.entityId}>
                                {row.entityId}
                              </td>
                              <td className="py-2 px-3 max-w-[140px] truncate text-muted-foreground" title={row.raw.oldValues ?? ""}>
                                {row.oldSummary}
                              </td>
                              <td className="py-2 px-3 max-w-[140px] truncate text-muted-foreground" title={row.raw.newValues ?? ""}>
                                {row.newSummary}
                              </td>
                              <td className="py-2 px-3">
                                <Badge
                                  className={cn(
                                    "text-xs",
                                    row.raw.isSuccess
                                      ? "bg-green-100 text-green-800"
                                      : "bg-red-100 text-red-800",
                                  )}
                                >
                                  {row.successLabel}
                                </Badge>
                              </td>
                              <td className="py-2 px-3 text-muted-foreground whitespace-nowrap text-xs">
                                {row.createdLabel}
                              </td>
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

                <Sheet open={detailOpen} onOpenChange={setDetailOpen}>
                  <SheetContent className="w-full sm:max-w-xl overflow-y-auto">
                    <SheetHeader>
                      <SheetTitle>Audit log detail</SheetTitle>
                    </SheetHeader>
                    {detailLoading ? (
                      <p className="mt-4 text-sm text-muted-foreground">Loading…</p>
                    ) : detailRecord ? (
                      <div className="mt-4 space-y-4 text-sm">
                        {(() => {
                          const entityId = detailRecord.entityId?.trim();
                          const canNavigate = !!entityId && !!getEntityRoute(detailRecord.entityName, entityId);
                          if (!canNavigate) return null;
                          return (
                            <div className="flex justify-end">
                              <Button
                                type="button"
                                size="sm"
                                onClick={() => {
                                  const route = getEntityRoute(detailRecord.entityName, entityId!);
                                  if (!route) {
                                    toast.info("Detail page not available");
                                    return;
                                  }
                                  router.push(route);
                                }}
                              >
                                Go To Document
                              </Button>
                            </div>
                          );
                        })()}
                        <dl className="grid grid-cols-[auto_1fr] gap-x-3 gap-y-1 text-xs">
                          <dt className="text-muted-foreground">ID</dt>
                          <dd className="tabular-nums">{detailRecord.id}</dd>
                          <dt className="text-muted-foreground">User</dt>
                          <dd>
                            <AuditLogActorCell
                              className="whitespace-normal"
                              {...getAuditLogActorDisplay(detailRecord)}
                            />
                          </dd>
                          <dt className="text-muted-foreground">Action</dt>
                          <dd>{detailRecord.actionType}</dd>
                          <dt className="text-muted-foreground">Entity</dt>
                          <dd>{detailRecord.entityName}</dd>
                          <dt className="text-muted-foreground">Entity ID</dt>
                          <dd className="font-mono break-all">{detailRecord.entityId}</dd>
                          <dt className="text-muted-foreground">Company</dt>
                          <dd>{detailRecord.companyId ?? "—"}</dd>
                          <dt className="text-muted-foreground">Status</dt>
                          <dd>{detailRecord.isSuccess ? "Successful" : "Failed"}</dd>
                          <dt className="text-muted-foreground">Correlation</dt>
                          <dd className="font-mono break-all text-xs">{detailRecord.correlationId ?? "—"}</dd>
                          <dt className="text-muted-foreground">Created (UTC)</dt>
                          <dd>{formatCreated(detailRecord.createdAtUtc).label}</dd>
                        </dl>
                        <div>
                          <p className="text-xs font-medium text-muted-foreground mb-1">Message</p>
                          <p className="text-sm whitespace-pre-wrap rounded-md border bg-muted/30 p-2">
                            {detailRecord.message || "—"}
                          </p>
                        </div>
                        <div>
                          <p className="text-xs font-medium text-muted-foreground mb-1">Old values</p>
                          <pre className="max-h-48 overflow-auto rounded-md border bg-muted/30 p-2 text-xs whitespace-pre-wrap font-mono">
                            {prettyBlock(detailRecord.oldValues)}
                          </pre>
                        </div>
                        <div>
                          <p className="text-xs font-medium text-muted-foreground mb-1">New values</p>
                          <pre className="max-h-48 overflow-auto rounded-md border bg-muted/30 p-2 text-xs whitespace-pre-wrap font-mono">
                            {prettyBlock(detailRecord.newValues)}
                          </pre>
                        </div>
                      </div>
                    ) : null}
                  </SheetContent>
                </Sheet>
              </div>
            );
          }}
        </AdvancedTableFilters>
      )}
    </div>
  );
}
