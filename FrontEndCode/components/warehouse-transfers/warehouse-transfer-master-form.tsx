"use client";

import { Label } from "@/components/ui/label";
import { Textarea } from "@/components/ui/textarea";
import { Input } from "@/components/ui/input";
import { FILTER_SELECT_CLASS } from "@/components/advanced-table-filters";
import type { Company } from "@/lib/services/company-service";
import type { Warehouse } from "@/lib/services/warehouse-service";
import { WarehouseType } from "@/lib/services/warehouse-service";
import { TransferStatus, type TransferStatusValue } from "@/lib/services/warehouse-transfer-service";
import { WarehouseTransferStatusBadge } from "@/components/documents/document-status-badge";
import { cn } from "@/lib/utils";

const selectCls = cn(FILTER_SELECT_CLASS, "h-10");

type WarehouseTransferMasterFormProps = {
  companies: Company[];
  warehouses: Warehouse[];
  vehicleWarehouses: Warehouse[];
  companyId: string;
  onCompanyIdChange: (v: string) => void;
  fromWarehouseId: string;
  onFromWarehouseIdChange: (v: string) => void;
  toWarehouseId: string;
  onToWarehouseIdChange: (v: string) => void;
  vehicleWarehouseId: string;
  onVehicleWarehouseIdChange: (v: string) => void;
  transferDateDisplay: string;
  note: string;
  onNoteChange: (v: string) => void;
  status: TransferStatusValue;
  readOnly: boolean;
  errors: {
    company?: string;
    fromWarehouse?: string;
    toWarehouse?: string;
    vehicleWarehouse?: string;
    warehousesSame?: string;
  };
};

export function WarehouseTransferMasterForm({
  companies,
  warehouses,
  vehicleWarehouses,
  companyId,
  onCompanyIdChange,
  fromWarehouseId,
  onFromWarehouseIdChange,
  toWarehouseId,
  onToWarehouseIdChange,
  vehicleWarehouseId,
  onVehicleWarehouseIdChange,
  transferDateDisplay,
  note,
  onNoteChange,
  status,
  readOnly,
  errors,
}: WarehouseTransferMasterFormProps) {
  const companyOptions = [...companies].sort((a, b) => a.name.localeCompare(b.name));
  const whOptions = [...warehouses].sort((a, b) => a.name.localeCompare(b.name));
  const vhOptions = [...vehicleWarehouses].sort((a, b) => a.name.localeCompare(b.name));

  return (
    <div className="grid gap-4 sm:grid-cols-2">
      <div className="sm:col-span-2 flex flex-wrap items-center gap-2">
        <span className="text-sm text-muted-foreground">Status</span>
        <WarehouseTransferStatusBadge status={status} />
        {status !== TransferStatus.Draft ? (
          <span className="text-xs text-muted-foreground">
            Only draft transfers can be edited in full. Use workflow actions for the next steps.
          </span>
        ) : null}
      </div>

      <div className="space-y-2">
        <Label htmlFor="wt-company">Company</Label>
        <select
          id="wt-company"
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
        <Label htmlFor="wt-transfer-date">Transfer date</Label>
        <Input id="wt-transfer-date" readOnly value={transferDateDisplay} className="h-10 bg-muted/50" />
        <p className="text-xs text-muted-foreground">
          Set by the server on create; update API does not change this field.
        </p>
      </div>

      <div className="space-y-2">
        <Label htmlFor="wt-from">From warehouse</Label>
        <select
          id="wt-from"
          className={selectCls}
          value={fromWarehouseId}
          onChange={(e) => onFromWarehouseIdChange(e.target.value)}
          disabled={readOnly || !companyId}
        >
          <option value="">{companyId ? "Select warehouse…" : "Select company first"}</option>
          {whOptions.map((w) => (
            <option key={w.id} value={String(w.id)}>
              {w.name}
            </option>
          ))}
        </select>
        {errors.fromWarehouse ? <p className="text-sm text-destructive">{errors.fromWarehouse}</p> : null}
      </div>

      <div className="space-y-2">
        <Label htmlFor="wt-to">To warehouse</Label>
        <select
          id="wt-to"
          className={selectCls}
          value={toWarehouseId}
          onChange={(e) => onToWarehouseIdChange(e.target.value)}
          disabled={readOnly || !companyId}
        >
          <option value="">{companyId ? "Select warehouse…" : "Select company first"}</option>
          {whOptions.map((w) => (
            <option key={w.id} value={String(w.id)}>
              {w.name}
            </option>
          ))}
        </select>
        {errors.toWarehouse ? <p className="text-sm text-destructive">{errors.toWarehouse}</p> : null}
      </div>

      {errors.warehousesSame ? (
        <p className="text-sm text-destructive sm:col-span-2">{errors.warehousesSame}</p>
      ) : null}

      <div className="space-y-2 sm:col-span-2">
        <Label htmlFor="wt-vehicle">Vehicle warehouse</Label>
        <select
          id="wt-vehicle"
          className={selectCls}
          value={vehicleWarehouseId}
          onChange={(e) => onVehicleWarehouseIdChange(e.target.value)}
          disabled={readOnly || !companyId}
        >
          <option value="">{companyId ? "Select vehicle warehouse…" : "Select company first"}</option>
          {vhOptions.map((w) => (
            <option key={w.id} value={String(w.id)}>
              {w.name}
            </option>
          ))}
        </select>
        {vhOptions.length === 0 && companyId ? (
          <p className="text-xs text-amber-600 dark:text-amber-500">
            No warehouses with type &quot;Vehicle&quot; for this company. Create one to submit transfers.
          </p>
        ) : null}
        {errors.vehicleWarehouse ? (
          <p className="text-sm text-destructive">{errors.vehicleWarehouse}</p>
        ) : (
          <p className="text-xs text-muted-foreground">
            Required for submit/approve/dispatch (API). Must differ from From and To.
          </p>
        )}
      </div>

      <div className="space-y-2 sm:col-span-2">
        <Label htmlFor="wt-note">Description / note</Label>
        <Textarea
          id="wt-note"
          value={note}
          onChange={(e) => onNoteChange(e.target.value)}
          disabled={readOnly}
          rows={3}
          className="resize-y min-h-[80px]"
        />
      </div>
    </div>
  );
}
