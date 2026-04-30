"use client";

import { Label } from "@/components/ui/label";
import { Textarea } from "@/components/ui/textarea";
import { FILTER_SELECT_CLASS } from "@/components/advanced-table-filters";
import type { Company } from "@/lib/services/company-service";
import type { Warehouse } from "@/lib/services/warehouse-service";
import { StockRequestStatus, type StockRequestStatusValue } from "@/lib/services/stock-request-service";
import { StockRequestStatusBadge } from "@/components/documents/document-status-badge";
import { cn } from "@/lib/utils";

const selectCls = cn(FILTER_SELECT_CLASS, "h-10");

type StockRequestMasterFormProps = {
  companies: Company[];
  warehouses: Warehouse[];
  companyId: string;
  onCompanyIdChange: (v: string) => void;
  requestingWarehouseId: string;
  onRequestingWarehouseIdChange: (v: string) => void;
  supplyingWarehouseId: string;
  onSupplyingWarehouseIdChange: (v: string) => void;
  note: string;
  onNoteChange: (v: string) => void;
  status: StockRequestStatusValue;
  readOnly: boolean;
  errors: {
    company?: string;
    requestingWarehouse?: string;
    supplyingWarehouse?: string;
    warehousesSame?: string;
  };
};

export function StockRequestMasterForm({
  companies,
  warehouses,
  companyId,
  onCompanyIdChange,
  requestingWarehouseId,
  onRequestingWarehouseIdChange,
  supplyingWarehouseId,
  onSupplyingWarehouseIdChange,
  note,
  onNoteChange,
  status,
  readOnly,
  errors,
}: StockRequestMasterFormProps) {
  const companyOptions = [...companies].sort((a, b) => a.name.localeCompare(b.name));
  const whOptions = [...warehouses].sort((a, b) => a.name.localeCompare(b.name));

  return (
    <div className="grid gap-4 sm:grid-cols-2">
      <div className="sm:col-span-2 flex flex-wrap items-center gap-2">
        <span className="text-sm text-muted-foreground">Status</span>
        <StockRequestStatusBadge status={status} />
        {status !== StockRequestStatus.Draft ? (
          <span className="text-xs text-muted-foreground">
            Workflow controls the status. Edit is only allowed in Draft.
          </span>
        ) : null}
      </div>

      <div className="space-y-2">
        <Label htmlFor="sr-company">Company</Label>
        <select
          id="sr-company"
          className={selectCls}
          value={companyId}
          onChange={(e) => onCompanyIdChange(e.target.value)}
          disabled={readOnly}
        >
          <option value="">Select company…</option>
          {companyOptions.map((c) => (
            <option key={c.id} value={String(c.id)}>
              {c.name}
            </option>
          ))}
        </select>
        {errors.company ? <p className="text-sm text-destructive">{errors.company}</p> : null}
      </div>

      <div className="space-y-2">
        <Label htmlFor="sr-req-wh">Requesting warehouse</Label>
        <select
          id="sr-req-wh"
          className={selectCls}
          value={requestingWarehouseId}
          onChange={(e) => onRequestingWarehouseIdChange(e.target.value)}
          disabled={readOnly || !companyId}
        >
          <option value="">{companyId ? "Select warehouse…" : "Select company first"}</option>
          {whOptions.map((w) => (
            <option key={w.id} value={String(w.id)}>
              {w.name}
            </option>
          ))}
        </select>
        {errors.requestingWarehouse ? (
          <p className="text-sm text-destructive">{errors.requestingWarehouse}</p>
        ) : null}
      </div>

      <div className="space-y-2">
        <Label htmlFor="sr-sup-wh">Supplying warehouse</Label>
        <select
          id="sr-sup-wh"
          className={selectCls}
          value={supplyingWarehouseId}
          onChange={(e) => onSupplyingWarehouseIdChange(e.target.value)}
          disabled={readOnly || !companyId}
        >
          <option value="">{companyId ? "Select warehouse…" : "Select company first"}</option>
          {whOptions.map((w) => (
            <option key={w.id} value={String(w.id)}>
              {w.name}
            </option>
          ))}
        </select>
        {errors.supplyingWarehouse ? (
          <p className="text-sm text-destructive">{errors.supplyingWarehouse}</p>
        ) : null}
      </div>

      {errors.warehousesSame ? (
        <p className="text-sm text-destructive sm:col-span-2">{errors.warehousesSame}</p>
      ) : null}

      <div className="space-y-2 sm:col-span-2">
        <Label htmlFor="sr-note">Description / note</Label>
        <Textarea
          id="sr-note"
          value={note}
          onChange={(e) => onNoteChange(e.target.value)}
          disabled={readOnly}
          rows={3}
          placeholder="Optional notes for approvers…"
          className="resize-y min-h-[80px]"
        />
      </div>
    </div>
  );
}
