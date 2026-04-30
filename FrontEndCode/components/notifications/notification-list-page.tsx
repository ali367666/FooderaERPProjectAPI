"use client";

import { useCallback, useEffect, useMemo, useState } from "react";
import Link from "next/link";
import {
  Eye,
  Trash2,
  MailOpen,
  Mail,
  ExternalLink,
} from "lucide-react";
import { AdvancedTableFilters, type TableFilterDef } from "@/components/advanced-table-filters";
import { DocumentActionToolbar } from "@/components/documents/document-action-toolbar";
import { ListSelectionSync } from "@/components/documents/list-selection-sync";
import { ConfirmDialog } from "@/components/documents/confirm-dialog";
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
import {
  deleteNotification,
  getNotificationDocumentHref,
  getNotifications,
  markNotificationRead,
  markNotificationUnread,
  type NotificationDto,
} from "@/lib/services/notification-service";
import { toApiFormError } from "@/lib/api-error";
import { useSelectedCompany } from "@/contexts/selected-company-context";
import { filterBySelectedCompany } from "@/lib/company-scope-utils";
import { toast } from "sonner";

const ITEMS_PER_PAGE = 10;

type SortMode = "newest" | "oldest" | "unread" | "idAsc" | "idDesc";

type ListRow = {
  id: string;
  numericId: number;
  title: string;
  messagePreview: string;
  typeLabel: string;
  relatedModule: string;
  relatedDoc: string;
  isRead: boolean;
  createdLabel: string;
  createdMs: number;
  raw: NotificationDto;
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

function mutationCompanyId(raw: NotificationDto): number | undefined {
  return raw.companyId > 0 ? raw.companyId : undefined;
}

function typeBadgeClass(type: string): string {
  const t = type.toLowerCase();
  if (t.includes("error") || t.includes("fail")) return "bg-destructive/15 text-destructive border-destructive/30";
  if (t.includes("warn")) return "bg-amber-100 text-amber-900 border-amber-200 dark:bg-amber-950/40 dark:text-amber-200";
  if (t.includes("success") || t.includes("approv")) return "bg-emerald-100 text-emerald-900 border-emerald-200 dark:bg-emerald-950/40 dark:text-emerald-200";
  return "bg-sky-100 text-sky-900 border-sky-200 dark:bg-sky-950/40 dark:text-sky-200";
}

export function NotificationListPage() {
  const { selectedCompanyId } = useSelectedCompany();
  const [items, setItems] = useState<NotificationDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [search, setSearch] = useState("");
  const [page, setPage] = useState(1);
  const [sortMode, setSortMode] = useState<SortMode>("newest");
  const [selectedRowId, setSelectedRowId] = useState<string | null>(null);
  const [detailOpen, setDetailOpen] = useState(false);
  const [deleteOpen, setDeleteOpen] = useState(false);

  /** Backend filter: undefined = all companies for this user; number = scoped. */
  const listCompanyFilter = useMemo(
    () => (selectedCompanyId != null && selectedCompanyId > 0 ? selectedCompanyId : undefined),
    [selectedCompanyId],
  );

  const load = useCallback(async () => {
    try {
      setLoading(true);
      setError(null);
      const data = await getNotifications(listCompanyFilter);
      setItems(data);
    } catch (e) {
      setError(toApiFormError(e, "Failed to load notifications").message);
    } finally {
      setLoading(false);
    }
  }, [listCompanyFilter]);

  useEffect(() => {
    void load();
  }, [load]);

  useEffect(() => {
    setSelectedRowId(null);
  }, [page]);

  useEffect(() => {
    if (selectedRowId == null) return;
    const still = items.some((r) => String(r.id) === selectedRowId);
    if (!still) setSelectedRowId(null);
  }, [items, selectedRowId]);

  const baseRows: ListRow[] = useMemo(
    () =>
      items.map((r) => {
        const { label, ms } = formatCreated(r.createdAtUtc);
        const mod = (r.referenceType ?? "").toLowerCase().replace(/\s+/g, "");
        const doc =
          r.referenceId != null && mod.includes("stockrequest")
            ? `SR-${String(r.referenceId).padStart(5, "0")}`
            : r.referenceId != null && r.referenceType
              ? `${r.referenceType} #${r.referenceId}`
              : r.referenceId != null
                ? `#${r.referenceId}`
                : "—";
        return {
          id: String(r.id),
          numericId: r.id,
          title: r.title,
          messagePreview: r.message.length > 120 ? `${r.message.slice(0, 117)}…` : r.message,
          typeLabel: r.type || "—",
          relatedModule: r.referenceType?.trim() || "—",
          relatedDoc: doc,
          isRead: r.isRead,
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
        id: "nid",
        label: "ID",
        ui: "text",
        match: (row, get) => {
          const q = (get("nid") ?? "").trim();
          if (!q) return true;
          return String(row.numericId).includes(q);
        },
      },
      {
        id: "titleMsg",
        label: "Title / message",
        ui: "text",
        match: (row, get) => {
          const q = (get("titleMsg") ?? "").trim().toLowerCase();
          if (!q) return true;
          return (
            row.title.toLowerCase().includes(q) || row.raw.message.toLowerCase().includes(q)
          );
        },
      },
      {
        id: "ntype",
        label: "Type",
        ui: "text",
        match: (row, get) => {
          const q = (get("ntype") ?? "").trim().toLowerCase();
          if (!q) return true;
          return row.typeLabel.toLowerCase().includes(q);
        },
      },
      {
        id: "nmodule",
        label: "Related module",
        ui: "text",
        match: (row, get) => {
          const q = (get("nmodule") ?? "").trim().toLowerCase();
          if (!q) return true;
          return row.relatedModule.toLowerCase().includes(q);
        },
      },
      {
        id: "readStatus",
        label: "Read status",
        ui: "select",
        options: [
          { value: "", label: "All" },
          { value: "unread", label: "Unread" },
          { value: "read", label: "Read" },
        ],
        match: (row, get) => {
          const v = get("readStatus");
          if (!v) return true;
          if (v === "unread") return !row.isRead;
          if (v === "read") return row.isRead;
          return true;
        },
      },
      {
        id: "ncreated",
        label: "Created",
        ui: "dateRange",
        gridClassName: "min-w-[260px] max-w-full flex-[2_1_360px]",
        match: (row, get) => {
          const from = get("ncreated:from");
          const to = get("ncreated:to");
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
    unread: "Unread first",
    idAsc: "ID ascending",
    idDesc: "ID descending",
  };

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-semibold tracking-tight">Notifications</h1>
        <p className="text-muted-foreground text-sm mt-1">
          Alerts for approvals, stock movements, transfers, and other ERP events for your account.
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
              return [
                String(row.numericId),
                row.title,
                row.raw.message,
                row.relatedDoc,
                row.relatedModule,
                row.typeLabel,
              ]
                .join(" ")
                .toLowerCase()
                .includes(q);
            });

            const sorted = [...searched].sort((a, b) => {
              switch (sortMode) {
                case "oldest":
                  return a.createdMs - b.createdMs;
                case "unread":
                  if (a.isRead !== b.isRead) return a.isRead ? 1 : -1;
                  return b.numericId - a.numericId;
                case "idAsc":
                  return a.numericId - b.numericId;
                case "idDesc":
                  return b.numericId - a.numericId;
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
                    placeholder="Search title, message, module, document…"
                    value={search}
                    onChange={(e) => {
                      setSearch(e.target.value);
                      setPage(1);
                    }}
                    className="max-w-md"
                  />
                  <div className="flex flex-wrap gap-2">
                    <Button
                      type="button"
                      variant="outline"
                      size="sm"
                      onClick={() => {
                        setSortMode((m) => {
                          const order: SortMode[] = [
                            "newest",
                            "oldest",
                            "unread",
                            "idDesc",
                            "idAsc",
                          ];
                          const i = order.indexOf(m);
                          return order[(i + 1) % order.length]!;
                        });
                        setPage(1);
                      }}
                    >
                      Sort: {sortLabel[sortMode]}
                    </Button>
                  </div>
                </div>

                <DocumentActionToolbar
                  hasSelection={hasSelection}
                  selectedSummary={selectedRow ? `#${selectedRow.numericId}` : null}
                  actions={[
                    {
                      id: "view",
                      label: "View",
                      icon: Eye,
                      enabled: hasSelection,
                      onClick: () => {
                        if (!selectedRow) return;
                        setDetailOpen(true);
                      },
                    },
                    {
                      id: "read",
                      label: "Mark read",
                      icon: MailOpen,
                      enabled: hasSelection && selectedRow && !selectedRow.isRead,
                      disabledReason: selectedRow?.isRead ? "Already read." : undefined,
                      onClick: async () => {
                        if (!selectedRow || selectedRow.isRead) return;
                        try {
                          await markNotificationRead(selectedRow.numericId, mutationCompanyId(selectedRow.raw));
                          toast.success("Marked as read");
                          void load();
                        } catch (e) {
                          toast.error(toApiFormError(e, "Update failed").message);
                        }
                      },
                    },
                    {
                      id: "unread",
                      label: "Mark unread",
                      icon: Mail,
                      enabled: hasSelection && selectedRow && selectedRow.isRead,
                      disabledReason: selectedRow && !selectedRow.isRead ? "Already unread." : undefined,
                      onClick: async () => {
                        if (!selectedRow || !selectedRow.isRead) return;
                        try {
                          await markNotificationUnread(selectedRow.numericId, mutationCompanyId(selectedRow.raw));
                          toast.success("Marked as unread");
                          void load();
                        } catch (e) {
                          toast.error(toApiFormError(e, "Update failed").message);
                        }
                      },
                    },
                    {
                      id: "delete",
                      label: "Delete",
                      icon: Trash2,
                      variant: "destructive",
                      enabled: hasSelection,
                      onClick: () => setDeleteOpen(true),
                    },
                  ]}
                />

                <div className="rounded-lg border border-border overflow-x-auto">
                  <table className="w-full text-sm leading-[1.2] [&_td]:align-top [&_th]:align-top">
                    <thead>
                      <tr className="border-b border-border bg-muted/40">
                        <th className="w-[1%] py-2 px-3 font-medium" aria-label="Select" />
                        <th className="text-right py-2 px-3 font-medium">ID</th>
                        <th className="text-left py-2 px-3 font-medium">Title</th>
                        <th className="text-left py-2 px-3 font-medium">Message</th>
                        <th className="text-left py-2 px-3 font-medium">Type</th>
                        <th className="text-left py-2 px-3 font-medium">Module</th>
                        <th className="text-left py-2 px-3 font-medium">Document</th>
                        <th className="text-left py-2 px-3 font-medium">Status</th>
                        <th className="text-left py-2 px-3 font-medium">Created</th>
                      </tr>
                    </thead>
                    <tbody>
                      {slice.length === 0 ? (
                        <tr>
                          <td colSpan={9} className="py-12 text-center text-muted-foreground">
                            No notifications match your filters.
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
                                !row.isRead && "font-medium",
                              )}
                              onClick={() => setSelectedRowId(row.id)}
                            >
                              <td className="py-2 px-3" onClick={(e) => e.stopPropagation()}>
                                <Checkbox
                                  checked={sel}
                                  onCheckedChange={(c) =>
                                    setSelectedRowId(c === true ? row.id : null)
                                  }
                                  aria-label={`Select notification ${row.numericId}`}
                                />
                              </td>
                              <td className="py-2 px-3 text-right tabular-nums text-muted-foreground">
                                {row.numericId}
                              </td>
                              <td className="py-2 px-3 max-w-[200px] truncate" title={row.title}>
                                {row.title}
                              </td>
                              <td className="py-2 px-3 max-w-[260px] truncate" title={row.raw.message}>
                                {row.messagePreview}
                              </td>
                              <td className="py-2 px-3">
                                <Badge variant="outline" className={cn("text-xs font-normal", typeBadgeClass(row.typeLabel))}>
                                  {row.typeLabel}
                                </Badge>
                              </td>
                              <td className="py-2 px-3 max-w-[140px] truncate">{row.relatedModule}</td>
                              <td className="py-2 px-3 max-w-[160px] truncate font-mono text-xs">
                                {row.relatedDoc}
                              </td>
                              <td className="py-2 px-3">
                                <Badge variant={row.isRead ? "secondary" : "default"} className="text-xs">
                                  {row.isRead ? "Read" : "Unread"}
                                </Badge>
                              </td>
                              <td className="py-2 px-3 text-muted-foreground whitespace-nowrap">
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

                <Sheet
                  open={detailOpen && selectedRow != null}
                  onOpenChange={setDetailOpen}
                >
                  <SheetContent className="w-full sm:max-w-lg overflow-y-auto">
                    {selectedRow ? (
                      <>
                        <SheetHeader>
                          <SheetTitle className="pr-8">{selectedRow.title}</SheetTitle>
                        </SheetHeader>
                        <div className="mt-4 space-y-3 text-sm">
                          <div className="flex flex-wrap gap-2">
                            <Badge variant="outline" className={typeBadgeClass(selectedRow.typeLabel)}>
                              {selectedRow.typeLabel}
                            </Badge>
                            <Badge variant={selectedRow.isRead ? "secondary" : "default"}>
                              {selectedRow.isRead ? "Read" : "Unread"}
                            </Badge>
                          </div>
                          <p className="text-muted-foreground whitespace-pre-wrap">{selectedRow.raw.message}</p>
                          <dl className="grid grid-cols-[auto_1fr] gap-x-3 gap-y-1 text-xs">
                            <dt className="text-muted-foreground">ID</dt>
                            <dd className="tabular-nums">{selectedRow.numericId}</dd>
                            <dt className="text-muted-foreground">Created</dt>
                            <dd>{selectedRow.createdLabel}</dd>
                            <dt className="text-muted-foreground">Module</dt>
                            <dd>{selectedRow.relatedModule}</dd>
                            <dt className="text-muted-foreground">Reference</dt>
                            <dd className="font-mono">{selectedRow.relatedDoc}</dd>
                          </dl>
                          {(() => {
                            const href = getNotificationDocumentHref(selectedRow.raw);
                            if (!href) return null;
                            return (
                              <Button type="button" variant="outline" size="sm" className="gap-2" asChild>
                                <Link href={href}>
                                  <ExternalLink className="size-4" />
                                  Go to document
                                </Link>
                              </Button>
                            );
                          })()}
                          <div className="flex flex-wrap gap-2 pt-2">
                            {!selectedRow.isRead ? (
                              <Button
                                type="button"
                                size="sm"
                                onClick={async () => {
                                  try {
                                    await markNotificationRead(selectedRow.numericId, mutationCompanyId(selectedRow.raw));
                                    toast.success("Marked as read");
                                    setDetailOpen(false);
                                    void load();
                                  } catch (e) {
                                    toast.error(toApiFormError(e, "Update failed").message);
                                  }
                                }}
                              >
                                Mark read
                              </Button>
                            ) : (
                              <Button
                                type="button"
                                size="sm"
                                variant="outline"
                                onClick={async () => {
                                  try {
                                    await markNotificationUnread(selectedRow.numericId, mutationCompanyId(selectedRow.raw));
                                    toast.success("Marked as unread");
                                    void load();
                                  } catch (e) {
                                    toast.error(toApiFormError(e, "Update failed").message);
                                  }
                                }}
                              >
                                Mark unread
                              </Button>
                            )}
                          </div>
                        </div>
                      </>
                    ) : null}
                  </SheetContent>
                </Sheet>
              </div>
            );
          }}
        </AdvancedTableFilters>
      )}

      <ConfirmDialog
        open={deleteOpen}
        onOpenChange={(o) => !o && setDeleteOpen(false)}
        title="Delete notification?"
        description="This removes the notification from your inbox."
        confirmLabel="Delete"
        destructive
        onConfirm={async () => {
          if (!selectedRowId) return;
          const id = Number(selectedRowId);
          if (!Number.isFinite(id)) return;
          const target = items.find((r) => String(r.id) === selectedRowId);
          try {
            await deleteNotification(id, target ? mutationCompanyId(target) : undefined);
            toast.success("Deleted");
            setDeleteOpen(false);
            setSelectedRowId(null);
            void load();
          } catch (e) {
            toast.error(toApiFormError(e, "Delete failed").message);
          }
        }}
      />
    </div>
  );
}
