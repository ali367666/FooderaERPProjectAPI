"use client";

import { useEffect, useMemo, useState } from "react";
import { useParams, useRouter } from "next/navigation";
import { toast } from "sonner";
import { DocumentHeader } from "@/components/documents/document-header";
import { MasterFormCard } from "@/components/documents/master-form-card";
import { DetailLinesCard } from "@/components/documents/detail-lines-card";
import { ConfirmDialog } from "@/components/documents/confirm-dialog";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { Input } from "@/components/ui/input";
import { useSelectedCompany } from "@/contexts/selected-company-context";
import { getWarehouses, type Warehouse } from "@/lib/services/warehouse-service";
import { getStockItemsForAllCompanies, UnitOfMeasure, unitLabel, type StockItem } from "@/lib/services/stock-item-service";
import {
  approveWarehouseStockDocument,
  createWarehouseStockDocument,
  deleteWarehouseStockDocument,
  getWarehouseStockDocumentById,
  updateWarehouseStockDocument,
  WarehouseStockDocumentStatus,
  warehouseStockDocumentStatusLabel,
  type WarehouseStockDocumentDetail,
} from "@/lib/services/warehouse-stock-service";
import { Trash2, Plus } from "lucide-react";

const selectClass =
  "flex h-9 w-full rounded-md border border-input bg-background px-2 py-1 text-sm ring-offset-background";

export type WarehouseStockLineDraft = {
  key: string;
  stockItemId: string;
  quantity: string;
  unitId: string;
};

function newLine(): WarehouseStockLineDraft {
  return {
    key: globalThis.crypto?.randomUUID?.() ?? `ln-${Date.now()}-${Math.random().toString(36).slice(2)}`,
    stockItemId: "",
    quantity: "",
    unitId: String(UnitOfMeasure.Piece),
  };
}

const UNIT_OPTIONS = [
  UnitOfMeasure.Piece,
  UnitOfMeasure.Kg,
  UnitOfMeasure.Gram,
  UnitOfMeasure.Liter,
  UnitOfMeasure.Ml,
] as const;

type Variant = "create" | "edit";

export function WarehouseStockDocumentFormPage({ variant }: { variant: Variant }) {
  const router = useRouter();
  const params = useParams();
  const { companies, companiesLoading, selectedCompanyId: scopeCompanyId } = useSelectedCompany();

  const documentId = variant === "edit" ? Number(params.id) : NaN;

  const [warehouses, setWarehouses] = useState<Warehouse[]>([]);
  const [stockItems, setStockItems] = useState<StockItem[]>([]);
  const [loaded, setLoaded] = useState<WarehouseStockDocumentDetail | null>(null);
  const [loading, setLoading] = useState(variant === "edit");
  const [saving, setSaving] = useState(false);
  const [deleteOpen, setDeleteOpen] = useState(false);

  const [warehouseId, setWarehouseId] = useState("");
  const [lines, setLines] = useState<WarehouseStockLineDraft[]>([newLine()]);
  const [lineErrors, setLineErrors] = useState<Record<string, string | undefined>>({});

  const isApproved = variant === "edit" && loaded?.status === WarehouseStockDocumentStatus.Approved;

  const warehousesScoped = useMemo(() => {
    if (scopeCompanyId == null) return warehouses;
    return warehouses.filter((w) => w.companyId === scopeCompanyId);
  }, [warehouses, scopeCompanyId]);

  const stockItemsScoped = useMemo(() => {
    if (scopeCompanyId == null) return stockItems;
    return stockItems.filter((s) => s.companyId === scopeCompanyId);
  }, [stockItems, scopeCompanyId]);

  const stockItemById = useMemo(() => new Map(stockItemsScoped.map((s) => [s.id, s])), [stockItemsScoped]);

  useEffect(() => {
    if (companiesLoading || companies.length === 0) return;
    let cancelled = false;
    (async () => {
      try {
        const ids = companies.map((c) => c.id);
        const [wh, items] = await Promise.all([getWarehouses(), getStockItemsForAllCompanies(ids)]);
        if (!cancelled) {
          setWarehouses(wh);
          setStockItems(items);
        }
      } catch (e) {
        if (!cancelled) toast.error(e instanceof Error ? e.message : "Failed to load reference data");
      }
    })();
    return () => {
      cancelled = true;
    };
  }, [companiesLoading, companies]);

  useEffect(() => {
    if (variant !== "edit" || !Number.isFinite(documentId) || documentId <= 0) return;
    let cancelled = false;
    (async () => {
      try {
        setLoading(true);
        const doc = await getWarehouseStockDocumentById(documentId);
        if (cancelled) return;
        setLoaded(doc);
        setWarehouseId(String(doc.warehouseId));
        setLines(
          doc.lines.length > 0
            ? doc.lines.map((l) => ({
                key: `loaded-${l.id}`,
                stockItemId: String(l.stockItemId),
                quantity: String(l.quantity),
                unitId: String(l.unitId),
              }))
            : [newLine()],
        );
        setLineErrors({});
      } catch (e) {
        if (!cancelled) toast.error(e instanceof Error ? e.message : "Failed to load document");
      } finally {
        if (!cancelled) setLoading(false);
      }
    })();
    return () => {
      cancelled = true;
    };
  }, [variant, documentId]);

  const addRow = () => {
    setLines((prev) => [...prev, newLine()]);
  };

  const removeRow = (key: string) => {
    setLines((prev) => (prev.length <= 1 ? prev : prev.filter((l) => l.key !== key)));
    setLineErrors((er) => {
      const next = { ...er };
      delete next[key];
      return next;
    });
  };

  const setLineField = (key: string, patch: Partial<WarehouseStockLineDraft>) => {
    setLines((prev) =>
      prev.map((l) => {
        if (l.key !== key) return l;
        const next = { ...l, ...patch };
        if (patch.stockItemId !== undefined && patch.stockItemId !== "") {
          const sid = Number(patch.stockItemId);
          const item = stockItemById.get(sid);
          if (item) next.unitId = String(item.unit);
        }
        return next;
      }),
    );
  };

  const validate = (): boolean => {
    const next: Record<string, string | undefined> = {};
    let ok = true;
    if (!warehouseId || Number(warehouseId) <= 0) {
      toast.error("Warehouse is required.");
      ok = false;
    }
    lines.forEach((l) => {
      const sid = Number(l.stockItemId);
      if (!Number.isFinite(sid) || sid <= 0) {
        next[l.key] = "Stock item required";
        ok = false;
        return;
      }
      const qty = Number(l.quantity);
      if (!Number.isFinite(qty) || qty <= 0) {
        next[l.key] = "Quantity must be greater than zero";
        ok = false;
      }
    });
    setLineErrors(next);
    return ok;
  };

  const buildPayloadLines = () =>
    lines.map((l) => ({
      stockItemId: Number(l.stockItemId),
      quantity: Number(l.quantity),
      unitId: Number(l.unitId),
    }));

  const handleSave = async () => {
    if (isApproved) return;
    if (!validate()) return;
    const wid = Number(warehouseId);
    try {
      setSaving(true);
      if (variant === "create") {
        const id = await createWarehouseStockDocument({ warehouseId: wid, lines: buildPayloadLines() });
        toast.success("Document saved");
        router.push(`/dashboard/warehouse-stock-documents/${id}`);
        return;
      }
      await updateWarehouseStockDocument(documentId, { warehouseId: wid, lines: buildPayloadLines() });
      toast.success("Document updated");
      const doc = await getWarehouseStockDocumentById(documentId);
      setLoaded(doc);
    } catch (e) {
      toast.error(e instanceof Error ? e.message : "Save failed");
    } finally {
      setSaving(false);
    }
  };

  const handleApprove = async () => {
    if (variant !== "edit" || !loaded || loaded.status !== WarehouseStockDocumentStatus.Draft) return;
    try {
      setSaving(true);
      await approveWarehouseStockDocument(documentId);
      toast.success("Document approved; stock movements posted.");
      const doc = await getWarehouseStockDocumentById(documentId);
      setLoaded(doc);
    } catch (e) {
      toast.error(e instanceof Error ? e.message : "Approve failed");
    } finally {
      setSaving(false);
    }
  };

  const handleDelete = async () => {
    if (variant !== "edit" || isApproved) return;
    try {
      await deleteWarehouseStockDocument(documentId);
      toast.success("Document deleted");
      router.push("/dashboard/warehouse-stock-documents");
    } catch (e) {
      toast.error(e instanceof Error ? e.message : "Delete failed");
    }
  };

  if (companiesLoading || loading) {
    return <div className="p-6 text-sm text-muted-foreground">Loading…</div>;
  }

  const docNo = variant === "edit" && loaded ? loaded.documentNo : "—";
  const subtitle =
    variant === "edit" && loaded?.createdAtUtc
      ? `Created ${new Date(loaded.createdAtUtc).toLocaleString()}`
      : "Enter warehouse and stock lines, then save as one document.";

  return (
    <div className="mx-auto max-w-5xl space-y-6">
      <DocumentHeader
        title={variant === "create" ? "New warehouse stock" : "Warehouse stock document"}
        documentNo={docNo}
        statusBadge={
          <Badge variant="outline" className="font-normal">
            {variant === "edit" && loaded
              ? warehouseStockDocumentStatusLabel(loaded.status)
              : "Draft"}
          </Badge>
        }
        subtitle={subtitle}
        rightSlot={
          <div className="flex flex-wrap gap-2">
            <Button type="button" variant="outline" size="sm" onClick={() => router.push("/dashboard/warehouse-stock-documents")}>
              Back to list
            </Button>
            {variant === "edit" && !isApproved ? (
              <Button type="button" variant="destructive" size="sm" onClick={() => setDeleteOpen(true)}>
                Delete
              </Button>
            ) : null}
            {variant === "edit" && !isApproved ? (
              <Button type="button" variant="secondary" size="sm" onClick={() => void handleApprove()} disabled={saving}>
                Approve
              </Button>
            ) : null}
            {!isApproved ? (
              <Button type="button" size="sm" onClick={() => void handleSave()} disabled={saving}>
                {saving ? "Saving…" : "Save"}
              </Button>
            ) : null}
          </div>
        }
      />

      <MasterFormCard
        title="Warehouse"
        description={
          isApproved
            ? "Approved documents are read-only. Quantities were posted to stock movements."
            : "One document per warehouse posting. Approve to post quantities to inventory."
        }
      >
        <div>
          <label className="mb-1 block text-xs font-medium text-muted-foreground">Warehouse</label>
          <select
            value={warehouseId}
            onChange={(e) => setWarehouseId(e.target.value)}
            className={selectClass}
            disabled={isApproved}
          >
            <option value="">Select warehouse</option>
            {warehousesScoped.map((w) => (
              <option key={w.id} value={String(w.id)}>
                {w.name}
              </option>
            ))}
          </select>
        </div>
      </MasterFormCard>

      <DetailLinesCard
        title="Lines"
        lineCount={lines.length}
        headerRight={
          !isApproved ? (
            <Button type="button" variant="outline" size="sm" className="h-8 gap-1" onClick={addRow}>
              <Plus className="h-3.5 w-3.5" />
              Add line
            </Button>
          ) : null
        }
      >
        <div className="overflow-x-auto rounded-md border border-border">
          <table className="w-full min-w-[640px] border-collapse text-sm">
            <thead>
              <tr className="border-b border-border bg-muted/50 text-left text-xs text-muted-foreground">
                <th className="px-2 py-2 font-medium w-8">#</th>
                <th className="px-2 py-2 font-medium">Stock item</th>
                <th className="px-2 py-2 font-medium w-28">Quantity</th>
                <th className="px-2 py-2 font-medium w-32">Unit</th>
                <th className="px-2 py-2 font-medium w-10" />
              </tr>
            </thead>
            <tbody>
              {lines.map((line, idx) => (
                <tr key={line.key} className="border-b border-border/80">
                  <td className="px-2 py-1.5 text-xs text-muted-foreground">{idx + 1}</td>
                  <td className="px-2 py-1.5">
                    <select
                      value={line.stockItemId}
                      onChange={(e) => setLineField(line.key, { stockItemId: e.target.value })}
                      className={selectClass}
                      disabled={isApproved}
                    >
                      <option value="">Select item</option>
                      {stockItemsScoped.map((s) => (
                        <option key={s.id} value={String(s.id)}>
                          {s.name}
                        </option>
                      ))}
                    </select>
                    {lineErrors[line.key] ? (
                      <p className="mt-0.5 text-xs text-destructive">{lineErrors[line.key]}</p>
                    ) : null}
                  </td>
                  <td className="px-2 py-1.5">
                    <Input
                      type="number"
                      min={0}
                      step="any"
                      className="h-9"
                      value={line.quantity}
                      onChange={(e) => setLineField(line.key, { quantity: e.target.value })}
                      disabled={isApproved}
                    />
                  </td>
                  <td className="px-2 py-1.5">
                    <select
                      value={line.unitId}
                      onChange={(e) => setLineField(line.key, { unitId: e.target.value })}
                      className={selectClass}
                      disabled={isApproved}
                    >
                      {UNIT_OPTIONS.map((u) => (
                        <option key={u} value={String(u)}>
                          {unitLabel(u)}
                        </option>
                      ))}
                    </select>
                  </td>
                  <td className="px-2 py-1.5">
                    <Button
                      type="button"
                      variant="ghost"
                      size="icon"
                      className="h-8 w-8 text-muted-foreground hover:text-destructive"
                      disabled={isApproved || lines.length <= 1}
                      onClick={() => removeRow(line.key)}
                      aria-label="Remove line"
                    >
                      <Trash2 className="h-4 w-4" />
                    </Button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </DetailLinesCard>

      <ConfirmDialog
        open={deleteOpen}
        onOpenChange={setDeleteOpen}
        title="Delete document?"
        description="This removes the warehouse stock document and all its lines."
        confirmLabel="Delete"
        destructive
        onConfirm={() => void handleDelete()}
      />
    </div>
  );
}
