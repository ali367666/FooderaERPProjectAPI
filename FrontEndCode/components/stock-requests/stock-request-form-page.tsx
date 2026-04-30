"use client";

import { useCallback, useEffect, useMemo, useState } from "react";
import { useParams, useRouter, useSearchParams } from "next/navigation";
import { toast } from "sonner";
import { DocumentHeader } from "@/components/documents/document-header";
import { MasterFormCard } from "@/components/documents/master-form-card";
import { DetailLinesCard } from "@/components/documents/detail-lines-card";
import { StockRequestStatusBadge } from "@/components/documents/document-status-badge";
import { WorkflowActions } from "@/components/documents/workflow-actions";
import { ConfirmDialog } from "@/components/documents/confirm-dialog";
import { Button } from "@/components/ui/button";
import { StockRequestMasterForm } from "@/components/stock-requests/stock-request-master-form";
import { StockRequestMasterReadOnly } from "@/components/stock-requests/stock-request-master-read-only";
import {
  StockRequestLinesTable,
  type StockRequestLineDraft,
} from "@/components/stock-requests/stock-request-lines-table";
import { useSelectedCompany } from "@/contexts/selected-company-context";
import { getCompanyById, type Company } from "@/lib/services/company-service";
import {
  mergeWarehousesForDocument,
  toStockRequestDetailLabels,
} from "@/lib/stock-request-detail-mapper";
import { toApiFormError } from "@/lib/api-error";
import { getWarehouses, type Warehouse } from "@/lib/services/warehouse-service";
import { getStockItemsForCompany, type StockItem } from "@/lib/services/stock-item-service";
import {
  StockRequestStatus,
  approveStockRequest,
  createStockRequest,
  deleteStockRequest,
  getStockRequestById,
  rejectStockRequest,
  submitStockRequest,
  updateStockRequest,
  type StockRequestDto,
} from "@/lib/services/stock-request-service";
import { AppPermissions } from "@/lib/app-permissions";
import { usePermissionSet } from "@/hooks/use-auth-permissions";

function newLine(): StockRequestLineDraft {
  return {
    key: globalThis.crypto?.randomUUID?.() ?? `nl-${Date.now()}-${Math.random().toString(36).slice(2)}`,
    stockItemId: null,
    quantity: "",
  };
}

type Variant = "create" | "edit";

export function StockRequestFormPage({ variant }: { variant: Variant }) {
  const router = useRouter();
  const params = useParams();
  const searchParams = useSearchParams();
  const permissions = usePermissionSet();
  const { companies, selectedCompanyId: toolbarCompanyId } = useSelectedCompany();

  const documentId = variant === "edit" ? Number(params.id) : NaN;
  const modeParam = (searchParams.get("mode") ?? "edit") as "view" | "edit";

  const [allWarehouses, setAllWarehouses] = useState<Warehouse[]>([]);
  const [stockItems, setStockItems] = useState<StockItem[]>([]);
  const [loaded, setLoaded] = useState<StockRequestDto | null>(null);
  const [loading, setLoading] = useState(variant === "edit");
  const [loadError, setLoadError] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);
  const [deleteOpen, setDeleteOpen] = useState(false);

  const [companyId, setCompanyId] = useState("");
  const [requestingWarehouseId, setRequestingWarehouseId] = useState("");
  const [supplyingWarehouseId, setSupplyingWarehouseId] = useState("");
  const [note, setNote] = useState("");
  const [lines, setLines] = useState<StockRequestLineDraft[]>([newLine()]);

  const [masterErrors, setMasterErrors] = useState<{
    company?: string;
    requestingWarehouse?: string;
    supplyingWarehouse?: string;
    warehousesSame?: string;
  }>({});
  const [lineErrors, setLineErrors] = useState<Record<string, string | undefined>>({});
  const [extraCompany, setExtraCompany] = useState<Company | null>(null);

  const companiesForMaster = useMemo(() => {
    const list = [...companies];
    if (extraCompany && !list.some((c) => c.id === extraCompany.id)) {
      list.push(extraCompany);
    }
    return list.sort((a, b) => a.name.localeCompare(b.name));
  }, [companies, extraCompany]);

  const warehousesForCompany = useMemo(() => {
    const cid = companyId ? Number(companyId) : NaN;
    const base = allWarehouses.filter(
      (w) => Number.isFinite(cid) && cid > 0 && w.companyId === cid,
    );
    if (variant === "edit" && loaded && loaded.companyId === cid) {
      return mergeWarehousesForDocument(base, loaded);
    }
    return base;
  }, [allWarehouses, companyId, loaded, variant]);

  const effectiveReadOnly = useMemo(() => {
    if (variant === "create") return false;
    if (!loaded) return true;
    if (modeParam === "view") return true;
    return loaded.status !== StockRequestStatus.Draft;
  }, [variant, loaded, modeParam]);

  /** True for existing documents shown as read-only ERP detail (view or non-draft). */
  const useReadOnlyPresentation = variant === "edit" && effectiveReadOnly;

  const detailLabels = useMemo(() => {
    if (!loaded) return null;
    const companyName =
      companiesForMaster.find((c) => c.id === loaded.companyId)?.name?.trim() || "—";
    return toStockRequestDetailLabels(loaded, companyName);
  }, [loaded, companiesForMaster]);

  const hydrateLinesFromDto = useCallback((doc: StockRequestDto) => {
    setLines(
      doc.lines.length > 0
        ? doc.lines.map((l) => ({
            key: `ex-${l.id}`,
            lineId: l.id,
            stockItemId: l.stockItemId,
            stockItemName: l.stockItemName?.trim() || undefined,
            quantity: String(l.quantity),
          }))
        : [newLine()],
    );
  }, []);

  const refreshStockItems = useCallback(async (cid: number) => {
    const list = await getStockItemsForCompany(cid);
    setStockItems(list);
  }, []);

  useEffect(() => {
    let cancelled = false;
    (async () => {
      try {
        const wh = await getWarehouses();
        if (!cancelled) setAllWarehouses(wh);
      } catch {
        if (!cancelled) toast.error("Failed to load warehouses");
      }
    })();
    return () => {
      cancelled = true;
    };
  }, []);

  useEffect(() => {
    if (variant !== "edit" || !loaded) {
      setExtraCompany(null);
      return;
    }
    const found = companies.some((c) => c.id === loaded.companyId);
    if (found) {
      setExtraCompany(null);
      return;
    }
    let cancelled = false;
    void getCompanyById(loaded.companyId)
      .then((c) => {
        if (!cancelled) setExtraCompany(c);
      })
      .catch(() => {
        if (!cancelled) setExtraCompany(null);
      });
    return () => {
      cancelled = true;
    };
  }, [variant, loaded, companies]);

  useEffect(() => {
    const cid = Number(companyId);
    if (!companyId || !Number.isFinite(cid) || cid <= 0) {
      setStockItems([]);
      return;
    }
    let cancelled = false;
    void (async () => {
      try {
        await refreshStockItems(cid);
      } catch {
        if (!cancelled) toast.error("Failed to load stock items");
      }
    })();
    return () => {
      cancelled = true;
    };
  }, [companyId, refreshStockItems]);

  useEffect(() => {
    if (variant !== "create") return;
    if (companyId) return;
    if (toolbarCompanyId != null) setCompanyId(String(toolbarCompanyId));
  }, [variant, companyId, toolbarCompanyId]);

  useEffect(() => {
    if (variant !== "edit") return;
    if (!Number.isFinite(documentId) || documentId <= 0) {
      setLoadError("Invalid document");
      setLoading(false);
      return;
    }
    let cancelled = false;
    void (async () => {
      try {
        setLoading(true);
        setLoadError(null);
        const doc = await getStockRequestById(documentId);
        if (cancelled) return;
        setLoaded(doc);
        setCompanyId(String(doc.companyId));
        setRequestingWarehouseId(String(doc.requestingWarehouseId));
        setSupplyingWarehouseId(String(doc.supplyingWarehouseId));
        setNote(doc.note ?? "");
        hydrateLinesFromDto(doc);
      } catch (e) {
        if (!cancelled) setLoadError(toApiFormError(e, "Load failed").message);
      } finally {
        if (!cancelled) setLoading(false);
      }
    })();
    return () => {
      cancelled = true;
    };
  }, [variant, documentId, hydrateLinesFromDto]);

  useEffect(() => {
    if (variant !== "edit" || !loaded) return;
    if (modeParam === "edit" && loaded.status !== StockRequestStatus.Draft) {
      toast.message("This document is not editable; opening as view.");
      router.replace(`/dashboard/stock-requests/${loaded.id}?mode=view`);
    }
  }, [variant, loaded, modeParam, router]);

  const validate = (): boolean => {
    const me: typeof masterErrors = {};
    const le: Record<string, string | undefined> = {};
    if (!companyId) me.company = "Company is required.";
    if (!requestingWarehouseId) me.requestingWarehouse = "Requesting warehouse is required.";
    if (!supplyingWarehouseId) me.supplyingWarehouse = "Supplying warehouse is required.";
    if (
      requestingWarehouseId &&
      supplyingWarehouseId &&
      requestingWarehouseId === supplyingWarehouseId
    ) {
      me.warehousesSame = "Requesting and supplying warehouses must be different.";
    }

    const nonEmptyLines = lines.filter(
      (l) => l.stockItemId != null || (l.quantity && Number(l.quantity) !== 0),
    );
    if (nonEmptyLines.length === 0) {
      le._form = "At least one line is required.";
    }

    for (const line of lines) {
      const hasQty = line.quantity !== "" && Number.isFinite(Number(line.quantity));
      const isRowTouched = line.stockItemId != null || hasQty;
      if (!isRowTouched) continue;
      if (line.stockItemId == null) le[line.key] = "Stock item is required.";
      else if (!hasQty || Number(line.quantity) <= 0)
        le[line.key] = "Quantity must be greater than 0.";
    }

    setMasterErrors(me);
    setLineErrors(le);
    const masterOk = Object.keys(me).length === 0;
    const linesOk = Object.keys(le).length === 0;
    if (!linesOk && le._form) toast.error(le._form);
    return masterOk && linesOk;
  };

  const buildPayloadLines = () => {
    return lines
      .filter((l) => l.stockItemId != null && l.quantity !== "" && Number(l.quantity) > 0)
      .map((l) => ({
        id: l.lineId,
        stockItemId: l.stockItemId as number,
        quantity: Number(l.quantity),
      }));
  };

  const handleSave = async (closeAfter: boolean) => {
    if (effectiveReadOnly) return;
    if (!validate()) return;
    const cid = Number(companyId);
    const rw = Number(requestingWarehouseId);
    const sw = Number(supplyingWarehouseId);
    const payloadLines = buildPayloadLines();
    setSaving(true);
    try {
      if (variant === "create") {
        const id = await createStockRequest({
          companyId: cid,
          requestingWarehouseId: rw,
          supplyingWarehouseId: sw,
          note: note.trim() || null,
          lines: payloadLines.map((l) => ({ stockItemId: l.stockItemId, quantity: l.quantity })),
        });
        toast.success("Stock request created");
        if (closeAfter) router.push("/dashboard/stock-requests");
        else router.push(`/dashboard/stock-requests/${id}?mode=edit`);
      } else if (loaded) {
        await updateStockRequest(loaded.id, {
          requestingWarehouseId: rw,
          supplyingWarehouseId: sw,
          note: note.trim() || null,
          lines: payloadLines.map((l) => ({
            id: l.id,
            stockItemId: l.stockItemId,
            quantity: l.quantity,
          })),
        });
        toast.success("Saved");
        const doc = await getStockRequestById(loaded.id);
        setLoaded(doc);
        hydrateLinesFromDto(doc);
        if (closeAfter) router.push("/dashboard/stock-requests");
      }
    } catch (e) {
      toast.error(toApiFormError(e, "Save failed").message);
    } finally {
      setSaving(false);
    }
  };

  const docNo =
    variant === "create"
      ? "New document"
      : loaded
        ? `SR-${String(loaded.id).padStart(5, "0")}`
        : "…";

  const status = loaded?.status ?? StockRequestStatus.Draft;

  const workflow = (
    <WorkflowActions
      actions={[
        {
          key: "submit",
          label: "Submit",
          show:
            variant === "edit" &&
            !!loaded &&
            loaded.status === StockRequestStatus.Draft &&
            permissions.has(AppPermissions.StockRequestSubmit),
          disabled: saving,
          onClick: async () => {
            if (!loaded) return;
            try {
              await submitStockRequest(loaded.id);
              toast.success("Submitted");
              const doc = await getStockRequestById(loaded.id);
              setLoaded(doc);
              router.replace(`/dashboard/stock-requests/${loaded.id}?mode=view`);
            } catch (e) {
              toast.error(toApiFormError(e, "Submit failed").message);
            }
          },
        },
        {
          key: "approve",
          label: "Approve",
          variant: "default",
          show:
            variant === "edit" &&
            !!loaded &&
            loaded.status === StockRequestStatus.Submitted &&
            permissions.has(AppPermissions.StockRequestApprove),
          disabled: saving,
          onClick: async () => {
            if (!loaded) return;
            try {
              await approveStockRequest(loaded.id);
              toast.success("Approved");
              const doc = await getStockRequestById(loaded.id);
              setLoaded(doc);
              hydrateLinesFromDto(doc);
            } catch (e) {
              toast.error(toApiFormError(e, "Approve failed").message);
            }
          },
        },
        {
          key: "reject",
          label: "Reject",
          variant: "destructive",
          show:
            variant === "edit" &&
            !!loaded &&
            loaded.status === StockRequestStatus.Submitted &&
            permissions.has(AppPermissions.StockRequestReject),
          disabled: saving,
          onClick: async () => {
            if (!loaded) return;
            try {
              await rejectStockRequest(loaded.id);
              toast.success("Rejected");
              const doc = await getStockRequestById(loaded.id);
              setLoaded(doc);
              hydrateLinesFromDto(doc);
            } catch (e) {
              toast.error(toApiFormError(e, "Reject failed").message);
            }
          },
        },
      ]}
    />
  );

  if (variant === "edit" && loadError) {
    return (
      <div className="space-y-4">
        <p className="text-destructive text-sm">{loadError}</p>
        <Button type="button" variant="outline" onClick={() => router.push("/dashboard/stock-requests")}>
          Back to list
        </Button>
      </div>
    );
  }

  if (variant === "edit" && loading) {
    return <p className="text-sm text-muted-foreground">Loading document…</p>;
  }

  return (
    <div className="space-y-6 max-w-6xl">
      <DocumentHeader
        title={variant === "create" ? "New stock request" : "Stock request"}
        documentNo={docNo}
        statusBadge={<StockRequestStatusBadge status={status} />}
        rightSlot={
          <div className="flex flex-wrap gap-2 justify-end">
            {workflow}
            {!effectiveReadOnly ? (
              <>
                <Button type="button" variant="outline" disabled={saving} onClick={() => handleSave(false)}>
                  Save
                </Button>
                <Button type="button" disabled={saving} onClick={() => handleSave(true)}>
                  Save &amp; close
                </Button>
              </>
            ) : null}
            {variant === "edit" && loaded?.status === StockRequestStatus.Draft ? (
              <Button type="button" variant="outline" onClick={() => setDeleteOpen(true)}>
                Delete
              </Button>
            ) : null}
            <Button
              type="button"
              variant="ghost"
              onClick={() => router.push("/dashboard/stock-requests")}
            >
              Cancel
            </Button>
          </div>
        }
      />

      <MasterFormCard
        title="Master"
        description={
          useReadOnlyPresentation
            ? undefined
            : "Requesting warehouse is the destination; supplying warehouse is the source (per API)."
        }
      >
        {useReadOnlyPresentation && loaded && detailLabels ? (
          <StockRequestMasterReadOnly status={status} labels={detailLabels} />
        ) : (
          <>
            <StockRequestMasterForm
              companies={companiesForMaster}
              warehouses={warehousesForCompany}
              companyId={companyId}
              onCompanyIdChange={(v) => {
                setCompanyId(v);
                setRequestingWarehouseId("");
                setSupplyingWarehouseId("");
              }}
              requestingWarehouseId={requestingWarehouseId}
              onRequestingWarehouseIdChange={setRequestingWarehouseId}
              supplyingWarehouseId={supplyingWarehouseId}
              onSupplyingWarehouseIdChange={setSupplyingWarehouseId}
              note={note}
              onNoteChange={setNote}
              status={status}
              readOnly={effectiveReadOnly}
              errors={masterErrors}
            />
            {variant === "create" && !companyId ? (
              <p className="text-xs text-muted-foreground">Select a company to enable warehouse fields.</p>
            ) : null}
          </>
        )}
      </MasterFormCard>

      <DetailLinesCard
        lineCount={lines.length}
        headerRight={
          !effectiveReadOnly ? (
            <span className="text-xs text-muted-foreground">Add items from the selected company.</span>
          ) : null
        }
      >
        <StockRequestLinesTable
          lines={lines}
          stockItems={stockItems}
          onChange={setLines}
          readOnly={effectiveReadOnly}
          rowErrors={lineErrors}
        />
      </DetailLinesCard>

      <ConfirmDialog
        open={deleteOpen}
        onOpenChange={setDeleteOpen}
        title="Delete this draft?"
        description="This cannot be undone."
        destructive
        confirmLabel="Delete"
        onConfirm={async () => {
          if (!loaded) return;
          try {
            await deleteStockRequest(loaded.id);
            toast.success("Deleted");
            router.push("/dashboard/stock-requests");
          } catch (e) {
            toast.error(toApiFormError(e, "Delete failed").message);
          }
        }}
      />
    </div>
  );
}
