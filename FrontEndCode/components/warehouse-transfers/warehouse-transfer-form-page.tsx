"use client";

import { useCallback, useEffect, useMemo, useState } from "react";
import { useParams, useRouter, useSearchParams } from "next/navigation";
import { toast } from "sonner";
import { DocumentHeader } from "@/components/documents/document-header";
import { MasterFormCard } from "@/components/documents/master-form-card";
import { DetailLinesCard } from "@/components/documents/detail-lines-card";
import { WarehouseTransferStatusBadge } from "@/components/documents/document-status-badge";
import { WorkflowActions } from "@/components/documents/workflow-actions";
import { ConfirmDialog } from "@/components/documents/confirm-dialog";
import { Button } from "@/components/ui/button";
import { WarehouseTransferMasterForm } from "@/components/warehouse-transfers/warehouse-transfer-master-form";
import {
  WarehouseTransferLinesTable,
  type WarehouseTransferLineDraft,
} from "@/components/warehouse-transfers/warehouse-transfer-lines-table";
import { useSelectedCompany } from "@/contexts/selected-company-context";
import { toApiFormError } from "@/lib/api-error";
import { getWarehouses, WarehouseType, type Warehouse } from "@/lib/services/warehouse-service";
import { getStockItemsForCompany, type StockItem } from "@/lib/services/stock-item-service";
import {
  TransferStatus,
  approveWarehouseTransfer,
  cancelWarehouseTransfer,
  createWarehouseTransfer,
  deleteWarehouseTransfer,
  dispatchWarehouseTransfer,
  getWarehouseTransferById,
  receiveWarehouseTransfer,
  rejectWarehouseTransfer,
  submitWarehouseTransfer,
  updateWarehouseTransfer,
  type WarehouseTransferDto,
} from "@/lib/services/warehouse-transfer-service";

function newLine(): WarehouseTransferLineDraft {
  return {
    key: globalThis.crypto?.randomUUID?.() ?? `nl-${Date.now()}-${Math.random().toString(36).slice(2)}`,
    stockItemId: null,
    quantity: "",
  };
}

function formatTransferDate(iso: string): string {
  if (!iso) return "—";
  const d = new Date(iso);
  if (Number.isNaN(d.getTime())) return iso;
  return d.toLocaleString(undefined, {
    year: "numeric",
    month: "short",
    day: "numeric",
    hour: "2-digit",
    minute: "2-digit",
  });
}

type Variant = "create" | "edit";

export function WarehouseTransferFormPage({ variant }: { variant: Variant }) {
  const router = useRouter();
  const params = useParams();
  const searchParams = useSearchParams();
  const { companies, selectedCompanyId: toolbarCompanyId } = useSelectedCompany();

  const documentId = variant === "edit" ? Number(params.id) : NaN;
  const modeParam = (searchParams.get("mode") ?? "edit") as "view" | "edit";

  const [allWarehouses, setAllWarehouses] = useState<Warehouse[]>([]);
  const [stockItems, setStockItems] = useState<StockItem[]>([]);
  const [loaded, setLoaded] = useState<WarehouseTransferDto | null>(null);
  const [loading, setLoading] = useState(variant === "edit");
  const [loadError, setLoadError] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);
  const [deleteOpen, setDeleteOpen] = useState(false);

  const [companyId, setCompanyId] = useState("");
  const [fromWarehouseId, setFromWarehouseId] = useState("");
  const [toWarehouseId, setToWarehouseId] = useState("");
  const [vehicleWarehouseId, setVehicleWarehouseId] = useState("");
  const [transferDateLabel, setTransferDateLabel] = useState("");
  const [note, setNote] = useState("");
  const [lines, setLines] = useState<WarehouseTransferLineDraft[]>([newLine()]);

  const [masterErrors, setMasterErrors] = useState<{
    company?: string;
    fromWarehouse?: string;
    toWarehouse?: string;
    vehicleWarehouse?: string;
    warehousesSame?: string;
  }>({});
  const [lineErrors, setLineErrors] = useState<Record<string, string | undefined>>({});
  const [duplicateMsg, setDuplicateMsg] = useState<string | undefined>();

  const warehousesForCompany = useMemo(
    () => allWarehouses.filter((w) => String(w.companyId) === companyId),
    [allWarehouses, companyId],
  );

  const vehicleWarehouses = useMemo(
    () => warehousesForCompany.filter((w) => w.type === WarehouseType.Vehicle),
    [warehousesForCompany],
  );

  const effectiveReadOnly = useMemo(() => {
    if (variant === "create") return false;
    if (!loaded) return true;
    if (modeParam === "view") return true;
    return loaded.status !== TransferStatus.Draft;
  }, [variant, loaded, modeParam]);

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
    if (variant !== "create") return;
    if (companyId) return;
    if (toolbarCompanyId != null) setCompanyId(String(toolbarCompanyId));
  }, [variant, companyId, toolbarCompanyId]);

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
        const doc = await getWarehouseTransferById(documentId);
        if (cancelled) return;
        setLoaded(doc);
        setCompanyId(String(doc.companyId));
        setFromWarehouseId(String(doc.fromWarehouseId));
        setToWarehouseId(String(doc.toWarehouseId));
        setVehicleWarehouseId(doc.vehicleWarehouseId ? String(doc.vehicleWarehouseId) : "");
        setTransferDateLabel(formatTransferDate(doc.transferDate));
        setNote(doc.note ?? "");
        setLines(
          doc.lines.length > 0
            ? doc.lines.map((l) => ({
                key: `ex-${l.id}`,
                lineId: l.id,
                stockItemId: l.stockItemId,
                quantity: String(l.quantity),
              }))
            : [newLine()],
        );
      } catch (e) {
        if (!cancelled) setLoadError(toApiFormError(e, "Load failed").message);
      } finally {
        if (!cancelled) setLoading(false);
      }
    })();
    return () => {
      cancelled = true;
    };
  }, [variant, documentId]);

  useEffect(() => {
    if (variant !== "edit" || !loaded) return;
    if (modeParam === "edit" && loaded.status !== TransferStatus.Draft) {
      toast.message("This document is not editable; opening as view.");
      router.replace(`/dashboard/warehouse-transfers/${loaded.id}?mode=view`);
    }
  }, [variant, loaded, modeParam, router]);

  useEffect(() => {
    if (variant === "create") {
      setTransferDateLabel(`${new Date().toLocaleDateString()} (assigned on save)`);
    }
  }, [variant]);

  const validate = (): boolean => {
    const me: typeof masterErrors = {};
    const le: Record<string, string | undefined> = {};
    setDuplicateMsg(undefined);

    if (!companyId) me.company = "Company is required.";
    if (!fromWarehouseId) me.fromWarehouse = "From warehouse is required.";
    if (!toWarehouseId) me.toWarehouse = "To warehouse is required.";
    if (fromWarehouseId && toWarehouseId && fromWarehouseId === toWarehouseId) {
      me.warehousesSame = "From and to warehouses must be different.";
    }
    if (
      vehicleWarehouseId &&
      (vehicleWarehouseId === fromWarehouseId || vehicleWarehouseId === toWarehouseId)
    ) {
      me.vehicleWarehouse = "Vehicle warehouse cannot match From or To.";
    }

    const filled = lines.filter(
      (l) => l.stockItemId != null || (l.quantity !== "" && Number(l.quantity) !== 0),
    );
    if (filled.length === 0) {
      le._form = "At least one line is required.";
    }

    for (const line of lines) {
      const hasQty = line.quantity !== "" && Number.isFinite(Number(line.quantity));
      const touched = line.stockItemId != null || hasQty;
      if (!touched) continue;
      if (line.stockItemId == null) le[line.key] = "Stock item is required.";
      else if (!hasQty || Number(line.quantity) <= 0)
        le[line.key] = "Quantity must be greater than 0.";
    }

    const committed = lines
      .filter((l) => l.stockItemId != null && l.quantity !== "" && Number(l.quantity) > 0)
      .map((l) => l.stockItemId as number);
    const dup = committed.filter((id, i) => committed.indexOf(id) !== i);
    if (dup.length > 0) {
      setDuplicateMsg("Each stock item may appear only once on this transfer.");
    }

    setMasterErrors(me);
    setLineErrors(le);
    const masterOk = Object.keys(me).length === 0;
    const linesOk = Object.keys(le).length === 0 && dup.length === 0;
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
    const fw = Number(fromWarehouseId);
    const tw = Number(toWarehouseId);
    const vw =
      vehicleWarehouseId && Number(vehicleWarehouseId) > 0 ? Number(vehicleWarehouseId) : null;
    const payloadLines = buildPayloadLines();
    setSaving(true);
    try {
      if (variant === "create") {
        const id = await createWarehouseTransfer({
          companyId: cid,
          fromWarehouseId: fw,
          toWarehouseId: tw,
          vehicleWarehouseId: vw,
          note: note.trim() || null,
          lines: payloadLines.map((l) => ({ stockItemId: l.stockItemId, quantity: l.quantity })),
        });
        toast.success("Warehouse transfer created");
        if (closeAfter) router.push("/dashboard/warehouse-transfers");
        else router.push(`/dashboard/warehouse-transfers/${id}?mode=edit`);
      } else if (loaded) {
        await updateWarehouseTransfer(loaded.id, {
          fromWarehouseId: fw,
          toWarehouseId: tw,
          vehicleWarehouseId: vw,
          note: note.trim() || null,
          lines: payloadLines.map((l) => ({
            id: l.id,
            stockItemId: l.stockItemId,
            quantity: l.quantity,
          })),
        });
        toast.success("Saved");
        const doc = await getWarehouseTransferById(loaded.id);
        setLoaded(doc);
        setTransferDateLabel(formatTransferDate(doc.transferDate));
        setLines(
          doc.lines.length > 0
            ? doc.lines.map((l) => ({
                key: `ex-${l.id}`,
                lineId: l.id,
                stockItemId: l.stockItemId,
                quantity: String(l.quantity),
              }))
            : [newLine()],
        );
        if (closeAfter) router.push("/dashboard/warehouse-transfers");
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
        ? `WT-${String(loaded.id).padStart(5, "0")}`
        : "…";

  const status = loaded?.status ?? TransferStatus.Draft;

  const reloadDoc = async () => {
    if (!loaded) return;
    const doc = await getWarehouseTransferById(loaded.id);
    setLoaded(doc);
    setTransferDateLabel(formatTransferDate(doc.transferDate));
  };

  const workflow = (
    <WorkflowActions
      actions={[
        {
          key: "submit",
          label: "Submit",
          show: variant === "edit" && !!loaded && loaded.status === TransferStatus.Draft,
          disabled: saving,
          onClick: async () => {
            if (!loaded) return;
            if (
              !vehicleWarehouseId ||
              vehicleWarehouseId === fromWarehouseId ||
              vehicleWarehouseId === toWarehouseId
            ) {
              toast.error(
                "Choose a vehicle warehouse that is different from From and To before submitting.",
              );
              return;
            }
            try {
              await submitWarehouseTransfer(loaded.id);
              toast.success("Submitted");
              await reloadDoc();
              router.replace(`/dashboard/warehouse-transfers/${loaded.id}?mode=view`);
            } catch (e) {
              toast.error(toApiFormError(e, "Submit failed").message);
            }
          },
        },
        {
          key: "approve",
          label: "Approve",
          variant: "default",
          show: variant === "edit" && !!loaded && loaded.status === TransferStatus.Pending,
          disabled: saving,
          onClick: async () => {
            if (!loaded) return;
            try {
              await approveWarehouseTransfer(loaded.id);
              toast.success("Approved");
              await reloadDoc();
            } catch (e) {
              toast.error(toApiFormError(e, "Approve failed").message);
            }
          },
        },
        {
          key: "reject",
          label: "Reject",
          variant: "destructive",
          show: variant === "edit" && !!loaded && loaded.status === TransferStatus.Pending,
          disabled: saving,
          onClick: async () => {
            if (!loaded) return;
            try {
              await rejectWarehouseTransfer(loaded.id);
              toast.success("Rejected");
              await reloadDoc();
            } catch (e) {
              toast.error(toApiFormError(e, "Reject failed").message);
            }
          },
        },
        {
          key: "dispatch",
          label: "Dispatch",
          show: variant === "edit" && !!loaded && loaded.status === TransferStatus.Approved,
          disabled: saving,
          onClick: async () => {
            if (!loaded) return;
            try {
              await dispatchWarehouseTransfer(loaded.id);
              toast.success("Dispatched");
              await reloadDoc();
            } catch (e) {
              toast.error(toApiFormError(e, "Dispatch failed").message);
            }
          },
        },
        {
          key: "receive",
          label: "Receive",
          show: variant === "edit" && !!loaded && loaded.status === TransferStatus.InTransit,
          disabled: saving,
          onClick: async () => {
            if (!loaded) return;
            try {
              await receiveWarehouseTransfer(loaded.id);
              toast.success("Received");
              await reloadDoc();
            } catch (e) {
              toast.error(toApiFormError(e, "Receive failed").message);
            }
          },
        },
        {
          key: "cancel",
          label: "Cancel",
          variant: "outline",
          show:
            variant === "edit" &&
            !!loaded &&
            loaded.status !== TransferStatus.Completed &&
            loaded.status !== TransferStatus.InTransit &&
            loaded.status !== TransferStatus.Rejected &&
            loaded.status !== TransferStatus.Cancelled,
          disabled: saving,
          onClick: async () => {
            if (!loaded) return;
            try {
              await cancelWarehouseTransfer(loaded.id);
              toast.success("Cancelled");
              await reloadDoc();
            } catch (e) {
              toast.error(toApiFormError(e, "Cancel failed").message);
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
        <Button type="button" variant="outline" onClick={() => router.push("/dashboard/warehouse-transfers")}>
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
        title={variant === "create" ? "New warehouse transfer" : "Warehouse transfer"}
        documentNo={docNo}
        statusBadge={<WarehouseTransferStatusBadge status={status} />}
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
            {variant === "edit" && loaded?.status === TransferStatus.Draft ? (
              <Button type="button" variant="outline" onClick={() => setDeleteOpen(true)}>
                Delete
              </Button>
            ) : null}
            <Button
              type="button"
              variant="ghost"
              onClick={() => router.push("/dashboard/warehouse-transfers")}
            >
              Cancel
            </Button>
          </div>
        }
      />

      <MasterFormCard title="Master" description="Route, vehicle warehouse, and header note.">
        <WarehouseTransferMasterForm
          companies={companies}
          warehouses={warehousesForCompany}
          vehicleWarehouses={vehicleWarehouses}
          companyId={companyId}
          onCompanyIdChange={(v) => {
            setCompanyId(v);
            setFromWarehouseId("");
            setToWarehouseId("");
            setVehicleWarehouseId("");
          }}
          fromWarehouseId={fromWarehouseId}
          onFromWarehouseIdChange={setFromWarehouseId}
          toWarehouseId={toWarehouseId}
          onToWarehouseIdChange={setToWarehouseId}
          vehicleWarehouseId={vehicleWarehouseId}
          onVehicleWarehouseIdChange={setVehicleWarehouseId}
          transferDateDisplay={transferDateLabel}
          note={note}
          onNoteChange={setNote}
          status={status}
          readOnly={effectiveReadOnly}
          errors={masterErrors}
        />
      </MasterFormCard>

      <DetailLinesCard
        lineCount={lines.length}
        headerRight={
          !effectiveReadOnly ? (
            <span className="text-xs text-muted-foreground">Duplicate items are rejected by the API.</span>
          ) : null
        }
      >
        <WarehouseTransferLinesTable
          lines={lines}
          stockItems={stockItems}
          onChange={setLines}
          readOnly={effectiveReadOnly}
          rowErrors={lineErrors}
          duplicateMessage={duplicateMsg}
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
            await deleteWarehouseTransfer(loaded.id);
            toast.success("Deleted");
            router.push("/dashboard/warehouse-transfers");
          } catch (e) {
            toast.error(toApiFormError(e, "Delete failed").message);
          }
        }}
      />
    </div>
  );
}
