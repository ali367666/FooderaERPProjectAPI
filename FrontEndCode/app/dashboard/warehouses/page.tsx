"use client";

import { useEffect, useMemo, useState } from "react";
import { useSearchParams } from "next/navigation";
import { AdvancedTableFilters, type TableFilterDef } from "@/components/advanced-table-filters";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { DataTable } from "@/components/data-table";
import { Input } from "@/components/ui/input";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import {
  createWarehouse,
  deleteWarehouse,
  getWarehouseById,
  getWarehouses,
  WarehouseType,
  warehouseTypeLabel,
  updateWarehouse,
  type Warehouse,
  type WarehouseFormValues,
  type WarehouseTypeValue,
} from "@/lib/services/warehouse-service";
import { getRestaurants, type Restaurant } from "@/lib/services/restaurant-service";
import { getEmployees, getEmployeesByPosition, type Employee } from "@/lib/services/employee-service";
import { resolveCompanyId } from "@/lib/resolve-company-id";
import { ApiFormError, getFieldErrorMessage, type FieldErrors } from "@/lib/api-error";
import { useSelectedCompany } from "@/contexts/selected-company-context";
import { filterBySelectedCompany } from "@/lib/company-scope-utils";

type WarehouseRow = {
  id: string;
  warehouseId: number;
  name: string;
  location: string;
  companyId: number;
  companyName: string;
  isActive: boolean;
  statusLabel: string;
};

const selectClass =
  "flex h-10 w-full rounded-md border border-input bg-background px-3 py-2 text-sm ring-offset-background";

const TYPE_OPTIONS = [WarehouseType.HeadOffice, WarehouseType.Restaurant, WarehouseType.Vehicle] as const;

function formatEmployeeName(e: Employee): string {
  const fromFull = e.fullName?.trim();
  if (fromFull) return fromFull;
  return `${e.firstName} ${e.lastName}`.trim() || `Employee #${e.id}`;
}

/** Prefer positions that look like driver; if none, use any employee with a linked app user. */
const DRIVER_POSITION_RE = /driver|sürücü|şof|chauffeur|courier|delivery/i;

type DriverListOption = { userId: number; label: string };

function buildDriverListOptions(employees: Employee[]): DriverListOption[] {
  const withUser = employees.filter(
    (e): e is Employee & { userId: number } => e.userId != null && e.userId > 0,
  );
  const preferred = withUser.filter((e) => DRIVER_POSITION_RE.test((e.positionName ?? "").toLowerCase()));
  const list = preferred.length > 0 ? preferred : withUser;
  const sorted = [...list].sort((a, b) =>
    formatEmployeeName(a).localeCompare(formatEmployeeName(b), undefined, { sensitivity: "base" }),
  );
  const seen = new Set<number>();
  const out: DriverListOption[] = [];
  for (const e of sorted) {
    if (seen.has(e.userId)) continue;
    seen.add(e.userId);
    out.push({ userId: e.userId, label: formatEmployeeName(e) });
  }
  return out;
}

function normalizeDriverFieldError(message: string | null | undefined): string | null {
  if (!message) return null;
  const t = message.toLowerCase();
  if (t.includes("driveruser") && (t.includes("required") || t.includes("must") || t.includes("not"))) {
    return "Please select a driver for vehicle warehouse.";
  }
  if (t.includes("driver user") && t.includes("required")) {
    return "Please select a driver for vehicle warehouse.";
  }
  return message;
}

export default function WarehousesPage() {
  const searchParams = useSearchParams();
  const { companies, companiesLoading, selectedCompanyId: scopeCompanyId } = useSelectedCompany();
  const [warehouses, setWarehouses] = useState<Warehouse[]>([]);
  const [restaurants, setRestaurants] = useState<Restaurant[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [isDialogOpen, setIsDialogOpen] = useState(false);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [isEditMode, setIsEditMode] = useState(false);
  const [editingId, setEditingId] = useState<number | null>(null);
  const [name, setName] = useState("");
  const [type, setType] = useState<string>(String(WarehouseType.HeadOffice));
  const [companyId, setCompanyId] = useState("");
  const [restaurantId, setRestaurantId] = useState("");
  const [responsibleEmployeeId, setResponsibleEmployeeId] = useState("");
  const [driverUserId, setDriverUserId] = useState("");
  const [employees, setEmployees] = useState<Employee[]>([]);
  const [employeesLoading, setEmployeesLoading] = useState(false);
  const [employeesError, setEmployeesError] = useState<string | null>(null);
  const [responsibleEmployees, setResponsibleEmployees] = useState<Employee[]>([]);
  const [responsibleEmployeesLoading, setResponsibleEmployeesLoading] = useState(false);
  const [responsibleEmployeesError, setResponsibleEmployeesError] = useState<string | null>(null);
  const [fieldErrors, setFieldErrors] = useState<FieldErrors>({});

  const isVehicle = Number(type) === WarehouseType.Vehicle;
  const driverOptions = useMemo(() => buildDriverListOptions(employees), [employees]);
  const driverFieldMessage = useMemo(
    () => normalizeDriverFieldError(getFieldErrorMessage(fieldErrors, "driveruserid")),
    [fieldErrors],
  );

  const companyNameById = useMemo(
    () => new Map(companies.map((c) => [c.id, c.name])),
    [companies],
  );

  const loadData = async (silent = false) => {
    try {
      if (!silent) setLoading(true);
      setError(null);
      const [whData, restData] = await Promise.all([getWarehouses(), getRestaurants()]);
      setWarehouses(whData);
      setRestaurants(restData);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to load warehouses.");
    } finally {
      if (!silent) setLoading(false);
    }
  };

  useEffect(() => {
    void loadData();
  }, []);

  useEffect(() => {
    if (!isDialogOpen) return;
    let cancelled = false;
    (async () => {
      setEmployeesLoading(true);
      setEmployeesError(null);
      setResponsibleEmployeesLoading(true);
      setResponsibleEmployeesError(null);
      try {
        const [list, anbardarEmployees] = await Promise.all([
          getEmployees(),
          getEmployeesByPosition("Anbardar", Number(companyId) > 0 ? Number(companyId) : (scopeCompanyId ?? 0)),
        ]);
        if (!cancelled) {
          setEmployees(
            [...list].sort((a, b) => formatEmployeeName(a).localeCompare(formatEmployeeName(b), undefined, { sensitivity: "base" })),
          );
          setResponsibleEmployees(
            [...anbardarEmployees].sort((a, b) =>
              formatEmployeeName(a).localeCompare(formatEmployeeName(b), undefined, { sensitivity: "base" }),
            ),
          );
        }
      } catch (err) {
        if (!cancelled) {
          setEmployees([]);
          setResponsibleEmployees([]);
          setEmployeesError(err instanceof Error ? err.message : "Failed to load employees.");
          setResponsibleEmployeesError(
            err instanceof Error ? err.message : "Failed to load warehouse keeper employees.",
          );
        }
      } finally {
        if (!cancelled) {
          setEmployeesLoading(false);
          setResponsibleEmployeesLoading(false);
        }
      }
    })();
    return () => {
      cancelled = true;
    };
  }, [isDialogOpen]);

  const sortedCompanyOptions = useMemo(
    () =>
      [...companies].sort((a, b) => a.name.localeCompare(b.name)).map((c) => ({ value: String(c.id), label: c.name })),
    [companies],
  );

  const selectedId = Number(searchParams.get("selectedId") ?? "");
  const hasSelectedId = Number.isFinite(selectedId) && selectedId > 0;

  const rows: WarehouseRow[] = useMemo(
    () =>
      warehouses.map((w) => ({
        id: String(w.id),
        warehouseId: w.id,
        name: w.name,
        location: w.restaurantName?.trim() || "—",
        companyId: w.companyId,
        companyName: companyNameById.get(w.companyId) || `Company #${w.companyId}`,
        isActive: true,
        statusLabel: "Active",
      })),
    [warehouses, companyNameById],
  );

  const scopedRows = useMemo(() => {
    const filtered = filterBySelectedCompany(rows, scopeCompanyId, (r) => r.companyId);
    if (!hasSelectedId) return filtered;
    return [...filtered].sort((a, b) =>
      a.warehouseId === selectedId ? -1 : b.warehouseId === selectedId ? 1 : 0,
    );
  }, [rows, scopeCompanyId, hasSelectedId, selectedId]);

  const companiesForForm = useMemo(() => {
    if (scopeCompanyId == null) return companies;
    return companies.filter((c) => c.id === scopeCompanyId);
  }, [companies, scopeCompanyId]);

  const restaurantsForForm = useMemo(() => {
    const cid = Number(companyId);
    if (Number.isFinite(cid) && cid > 0) {
      return restaurants.filter((r) => r.companyId === cid);
    }
    if (scopeCompanyId != null) {
      return restaurants.filter((r) => r.companyId === scopeCompanyId);
    }
    return restaurants;
  }, [restaurants, companyId, scopeCompanyId]);

  const filterDefs = useMemo<TableFilterDef<WarehouseRow>[]>(
    () => [
      {
        id: "warehouseId",
        label: "ID",
        ui: "number",
        match: (row, get) => {
          const q = get("warehouseId").trim().toLowerCase();
          if (!q) return true;
          return String(row.warehouseId).toLowerCase().includes(q);
        },
      },
      {
        id: "name",
        label: "Warehouse Name",
        ui: "text",
        match: (row, get) => {
          const q = get("name").trim().toLowerCase();
          if (!q) return true;
          return row.name.toLowerCase().includes(q);
        },
      },
      {
        id: "company",
        label: "Company",
        ui: "select",
        options: sortedCompanyOptions,
        match: (row, get) => {
          const v = get("company");
          if (!v) return true;
          return row.companyId === Number(v);
        },
      },
      {
        id: "status",
        label: "Status",
        ui: "status",
        match: (row, get) => {
          const v = get("status");
          if (v === "all" || v === "active") return row.isActive;
          if (v === "inactive") return !row.isActive;
          return true;
        },
      },
    ],
    [sortedCompanyOptions],
  );

  const columns = [
    {
      key: "warehouseId" as const,
      label: "ID",
      render: (value: number, row: WarehouseRow) => (
        <span className={row.warehouseId === selectedId ? "font-semibold text-primary" : ""}>
          {value}
        </span>
      ),
    },
    { key: "name" as const, label: "Warehouse Name" },
    { key: "companyName" as const, label: "Company" },
    { key: "location" as const, label: "Location / Address" },
    {
      key: "statusLabel" as const,
      label: "Status",
      render: (_: string, row: WarehouseRow) => (
        <Badge className="bg-emerald-100 text-emerald-800 hover:bg-emerald-100">{row.statusLabel}</Badge>
      ),
    },
  ];

  const resetForm = () => {
    setName("");
    setType(String(WarehouseType.HeadOffice));
    setCompanyId(
      String(scopeCompanyId ?? companies[0]?.id ?? resolveCompanyId()),
    );
    setRestaurantId("");
    setResponsibleEmployeeId("");
    setDriverUserId("");
    setEditingId(null);
    setFieldErrors({});
    setIsEditMode(false);
  };

  const handleAdd = () => {
    resetForm();
    setIsDialogOpen(true);
  };

  const handleEdit = async (row: WarehouseRow) => {
    try {
      setError(null);
      const res = await getWarehouseById(row.warehouseId);
      setEditingId(res.id);
      setName(res.name);
      setType(String(res.type));
      setCompanyId(String(res.companyId));
      setRestaurantId(res.restaurantId ? String(res.restaurantId) : "");
      setResponsibleEmployeeId(
        res.responsibleEmployeeId != null ? String(res.responsibleEmployeeId) : "",
      );
      setDriverUserId(res.driverUserId ? String(res.driverUserId) : "");
      setFieldErrors({});
      setIsEditMode(true);
      setIsDialogOpen(true);
    } catch (err) {
      const message = err instanceof Error ? err.message : "Failed to load warehouse.";
      setError(message);
      window.alert(message);
    }
  };

  const handleDelete = async (row: WarehouseRow) => {
    if (!window.confirm(`Delete warehouse "${row.name}"?`)) return;
    try {
      setError(null);
      await deleteWarehouse(row.warehouseId);
      await loadData(true);
    } catch (err) {
      const message = err instanceof Error ? err.message : "Failed to delete.";
      setError(message);
      window.alert(message);
    }
  };

  const handleSave = async () => {
    const trimmed = name.trim();
    const comp = Number(companyId);
    const t = Number(type) as WarehouseTypeValue;
    if (!trimmed) {
      window.alert("Name is required.");
      return;
    }
    if (!Number.isFinite(comp) || comp <= 0) {
      window.alert("Company is required.");
      return;
    }
    const resEmpRaw = responsibleEmployeeId.trim();
    const resEmpNum = resEmpRaw ? Number(resEmpRaw) : NaN;
    const isVehicleT = t === WarehouseType.Vehicle;
    const driverRaw = driverUserId.trim();
    const driverNum = driverRaw ? Number(driverRaw) : NaN;

    if (isVehicleT) {
      if (!Number.isFinite(driverNum) || driverNum <= 0) {
        window.alert("Please select a driver for vehicle warehouse.");
        return;
      }
    }

    const payload: WarehouseFormValues = {
      name: trimmed,
      type: t,
      companyId: comp,
      restaurantId: restaurantId ? Number(restaurantId) : null,
      responsibleEmployeeId: Number.isFinite(resEmpNum) && resEmpNum > 0 ? resEmpNum : null,
      driverUserId: isVehicleT ? driverNum : null,
    };

    try {
      setIsSubmitting(true);
      setError(null);
      setFieldErrors({});
      if (isEditMode && editingId != null) {
        await updateWarehouse(editingId, payload);
      } else {
        await createWarehouse(payload);
      }
      setIsDialogOpen(false);
      resetForm();
      await loadData(true);
    } catch (err) {
      if (err instanceof ApiFormError) setFieldErrors(err.fieldErrors);
      const message = err instanceof Error ? err.message : "Save failed.";
      setError(message);
      window.alert(message);
    } finally {
      setIsSubmitting(false);
    }
  };

  if (companiesLoading || loading) {
    return <div className="p-6 text-sm text-muted-foreground">Loading warehouses...</div>;
  }

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-3xl font-bold text-foreground">Warehouses</h1>
        <p className="text-muted-foreground mt-1">Manage storage locations linked to companies and restaurants.</p>
      </div>

      {error && (
        <div className="rounded-md border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-600">{error}</div>
      )}

      <Dialog
        open={isDialogOpen}
        onOpenChange={(open) => {
          setIsDialogOpen(open);
          if (!open) resetForm();
        }}
      >
        <AdvancedTableFilters defs={filterDefs} data={scopedRows}>
          {(filtered) => (
            <DataTable
              title="Warehouse List"
              columns={columns}
              data={filtered}
              idSortKey="warehouseId"
              searchableFields={["name", "location", "companyName", "id"]}
              searchPlaceholder="Search warehouses..."
              onAdd={handleAdd}
              onEdit={handleEdit}
              onDelete={handleDelete}
            />
          )}
        </AdvancedTableFilters>

        <DialogContent className="sm:max-w-lg">
          <DialogHeader>
            <DialogTitle>{isEditMode ? "Edit Warehouse" : "Add Warehouse"}</DialogTitle>
            <DialogDescription>Configure warehouse type and company assignment.</DialogDescription>
          </DialogHeader>

          <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
            <div className="sm:col-span-2">
              <label className="mb-2 block text-sm font-medium text-foreground">Name</label>
              <Input value={name} onChange={(e) => setName(e.target.value)} />
              {getFieldErrorMessage(fieldErrors, "name") && (
                <p className="mt-1 text-xs text-red-600">{getFieldErrorMessage(fieldErrors, "name")}</p>
              )}
            </div>
            <div>
              <label className="mb-2 block text-sm font-medium text-foreground">Type</label>
              <select value={type} onChange={(e) => setType(e.target.value)} className={selectClass}>
                {TYPE_OPTIONS.map((opt) => (
                  <option key={opt} value={String(opt)}>
                    {warehouseTypeLabel(opt)}
                  </option>
                ))}
              </select>
            </div>
            <div>
              <label className="mb-2 block text-sm font-medium text-foreground">Company</label>
              <select value={companyId} onChange={(e) => setCompanyId(e.target.value)} className={selectClass}>
                <option value="">Select</option>
                {companiesForForm.map((c) => (
                  <option key={c.id} value={String(c.id)}>
                    {c.name}
                  </option>
                ))}
              </select>
              {getFieldErrorMessage(fieldErrors, "companyid") && (
                <p className="mt-1 text-xs text-red-600">{getFieldErrorMessage(fieldErrors, "companyid")}</p>
              )}
            </div>
            <div className="sm:col-span-2">
              <label className="mb-2 block text-sm font-medium text-foreground">Restaurant (optional)</label>
              <select value={restaurantId} onChange={(e) => setRestaurantId(e.target.value)} className={selectClass}>
                <option value="">None</option>
                {restaurantsForForm.map((r) => (
                  <option key={r.id} value={String(r.id)}>
                    {r.name}
                  </option>
                ))}
              </select>
            </div>
            <div className="sm:col-span-2">
              <label
                className="mb-2 block text-sm font-medium text-foreground"
                htmlFor="warehouse-responsible-employee"
              >
                Responsible Employee
              </label>
              <select
                id="warehouse-responsible-employee"
                value={responsibleEmployeeId}
                onChange={(e) => setResponsibleEmployeeId(e.target.value)}
                className={selectClass}
                disabled={responsibleEmployeesLoading}
              >
                {responsibleEmployeesLoading ? (
                  <option value="">Loading...</option>
                ) : responsibleEmployeesError ? (
                  <>
                    {responsibleEmployeeId ? (
                      <option value={responsibleEmployeeId}>
                        Manager #{responsibleEmployeeId} (list unavailable)
                      </option>
                    ) : null}
                    <option value="">{responsibleEmployeesError}</option>
                  </>
                ) : responsibleEmployees.length === 0 ? (
                  <option value="">No Anbardar employees found</option>
                ) : (
                  <>
                    <option value="">None</option>
                    {responsibleEmployees.map((e) => (
                      <option key={e.id} value={String(e.id)}>
                        {formatEmployeeName(e)}
                      </option>
                    ))}
                  </>
                )}
              </select>
              {getFieldErrorMessage(fieldErrors, "responsibleemployeeid") && (
                <p className="mt-1 text-xs text-red-600">
                  {getFieldErrorMessage(fieldErrors, "responsibleemployeeid")}
                </p>
              )}
            </div>
            {isVehicle && (
              <div className="sm:col-span-2">
                <label
                  className="mb-2 block text-sm font-medium text-foreground"
                  htmlFor="warehouse-driver"
                >
                  Driver
                </label>
                <select
                  id="warehouse-driver"
                  value={driverUserId}
                  onChange={(e) => setDriverUserId(e.target.value)}
                  className={selectClass}
                  disabled={employeesLoading}
                >
                  {employeesLoading ? (
                    <option value="">Loading...</option>
                  ) : employeesError ? (
                    <>
                      {driverUserId ? (
                        <option value={driverUserId}>
                          Driver #{driverUserId} (list unavailable)
                        </option>
                      ) : null}
                      <option value="">{employeesError}</option>
                    </>
                  ) : driverOptions.length === 0 ? (
                    driverUserId ? (
                      <option value={driverUserId}>
                        Current driver (user #{driverUserId})
                      </option>
                    ) : (
                      <option value="">
                        No drivers found — employees need a linked app user account
                      </option>
                    )
                  ) : (
                    <>
                      <option value="">Select driver</option>
                      {driverUserId &&
                        !driverOptions.some(
                          (d) => String(d.userId) === driverUserId,
                        ) && (
                        <option value={driverUserId}>
                          Current driver (user #{driverUserId})
                        </option>
                      )}
                      {driverOptions.map((d) => (
                        <option key={d.userId} value={String(d.userId)}>
                          {d.label}
                        </option>
                      ))}
                    </>
                  )}
                </select>
                {driverFieldMessage && (
                  <p className="mt-1 text-xs text-red-600">{driverFieldMessage}</p>
                )}
              </div>
            )}
          </div>

          <div className="mt-6 flex justify-end gap-3">
            <Button
              variant="outline"
              onClick={() => {
                setIsDialogOpen(false);
                resetForm();
              }}
              disabled={isSubmitting}
            >
              Cancel
            </Button>
            <Button onClick={handleSave} disabled={isSubmitting}>
              {isSubmitting ? "Saving..." : "Save"}
            </Button>
          </div>
        </DialogContent>
      </Dialog>
    </div>
  );
}
