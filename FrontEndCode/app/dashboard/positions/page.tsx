"use client";

import { useEffect, useMemo, useState } from "react";
import { AdvancedTableFilters, type TableFilterDef } from "@/components/advanced-table-filters";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { DataTable } from "@/components/data-table";
import { Input } from "@/components/ui/input";
import {
  getDepartmentsForAllCompanies,
  type Department,
} from "@/lib/services/department-service";
import { type Company } from "@/lib/services/company-service";
import { useSelectedCompany } from "@/contexts/selected-company-context";
import { filterBySelectedCompany } from "@/lib/company-scope-utils";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import {
  createPosition,
  deletePosition,
  getPositionsForAllCompanies,
  updatePosition,
  type Position,
} from "@/lib/services/position-service";
import { ApiFormError, getFieldErrorMessage, type FieldErrors } from "@/lib/api-error";

/** Single canonical row: one `id` (string for DataTable) = String(DB id). No separate positionId. */
type NormalizedPosition = {
  id: string;
  name: string;
  description: string;
  departmentId: number;
  companyId: number;
  departmentName: string;
  companyName: string;
  isActive: boolean;
  statusLabel: string;
};

type PositionFormState = {
  id: number | null;
  name: string;
  description: string;
  departmentId: string;
  companyId: string;
  isActive: boolean;
};

function emptyPositionForm(): PositionFormState {
  return {
    id: null,
    name: "",
    description: "",
    departmentId: "",
    companyId: "",
    isActive: true,
  };
}

function buildNameMaps(depts: Department[], comps: Company[]) {
  return {
    departmentNameById: new Map(depts.map((d) => [d.id, d.name])),
    companyNameById: new Map(comps.map((c) => [c.id, c.name])),
  };
}

function normalizePositions(
  raw: Position[],
  departmentNameById: Map<number, string>,
  companyNameById: Map<number, string>,
): NormalizedPosition[] {
  const rows: NormalizedPosition[] = [];

  for (const item of raw) {
    const numId = Number(item.id ?? item.positionId ?? 0);
    if (!Number.isFinite(numId) || numId <= 0) continue;

    const departmentId = Number(item.departmentId ?? item.department?.id ?? 0);
    const companyId = Number(item.companyId ?? item.company?.id ?? 0);

    const departmentName =
      item.departmentName?.trim() ||
      item.department?.name?.trim() ||
      (Number.isFinite(departmentId) && departmentId > 0
        ? departmentNameById.get(departmentId) || "-"
        : "-");
    const companyName =
      item.companyName?.trim() ||
      item.company?.name?.trim() ||
      (Number.isFinite(companyId) && companyId > 0
        ? companyNameById.get(companyId) || "-"
        : "-");

    const isActive =
      typeof item.isActive === "boolean" ? item.isActive : true;

    rows.push({
      id: String(numId),
      name: item.name ?? item.positionName ?? "",
      description: item.description ?? "",
      departmentId: Number.isFinite(departmentId) ? departmentId : 0,
      companyId: Number.isFinite(companyId) ? companyId : 0,
      departmentName,
      companyName,
      isActive,
      statusLabel: isActive ? "Active" : "Inactive",
    });
  }

  return rows;
}

export default function PositionsPage() {
  const { companies, companiesLoading, selectedCompanyId: scopeCompanyId } = useSelectedCompany();
  const [positions, setPositions] = useState<NormalizedPosition[]>([]);
  const [departments, setDepartments] = useState<Department[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [isDialogOpen, setIsDialogOpen] = useState(false);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [isEditMode, setIsEditMode] = useState(false);
  const [form, setForm] = useState<PositionFormState>(emptyPositionForm);
  const [fieldErrors, setFieldErrors] = useState<FieldErrors>({});

  const formCompanyNumeric = Number(form.companyId) > 0 ? Number(form.companyId) : 0;
  const formCompanyName =
    companies.find((c) => c.id === formCompanyNumeric)?.name ||
    (formCompanyNumeric > 0 ? `Company #${formCompanyNumeric}` : "");

  const reloadPositionsData = async (options?: { silent?: boolean }) => {
    const silent = options?.silent === true;
    if (companies.length === 0) {
      setDepartments([]);
      setPositions([]);
      return;
    }
    try {
      if (!silent) setLoading(true);
      setError(null);
      const ids = companies.map((c) => c.id);
      const [deptData, rawPositions] = await Promise.all([
        getDepartmentsForAllCompanies(ids),
        getPositionsForAllCompanies(ids),
      ]);
      setDepartments(deptData);
      const { departmentNameById, companyNameById } = buildNameMaps(deptData, companies);
      const normalized = normalizePositions(rawPositions, departmentNameById, companyNameById);
      setPositions(normalized);
    } catch (err) {
      console.error(err);
      setError(err instanceof Error ? err.message : "Failed to load positions.");
      throw err;
    } finally {
      if (!silent) setLoading(false);
    }
  };

  useEffect(() => {
    if (companiesLoading) return;
    void reloadPositionsData();
  }, [companiesLoading, companies]);

  const scopedPositions = useMemo(
    () => filterBySelectedCompany(positions, scopeCompanyId, (r) => r.companyId),
    [positions, scopeCompanyId],
  );

  const handleAdd = () => {
    setForm({
      ...emptyPositionForm(),
      companyId: scopeCompanyId != null ? String(scopeCompanyId) : "",
    });
    setFieldErrors({});
    setIsEditMode(false);
    setIsDialogOpen(true);
  };

  const handleEdit = (position: NormalizedPosition) => {
    console.log("EDIT ROW:", position);
    setForm({
      id: Number(position.id),
      name: position.name || "",
      description: position.description || "",
      departmentId: String(position.departmentId || ""),
      companyId: String(position.companyId || ""),
      isActive: position.isActive ?? true,
    });
    setFieldErrors({});
    setIsEditMode(true);
    setIsDialogOpen(true);
  };

  const handleDelete = async (position: NormalizedPosition) => {
    const confirmed = window.confirm(`Delete "${position.name}"?`);
    if (!confirmed) return;

    console.log("DELETE ROW:", position);
    console.log("DELETE ID:", position.id);

    const idNum = Number(position.id);
    if (!Number.isFinite(idNum) || idNum <= 0) {
      const msg = "Invalid position id.";
      setError(msg);
      window.alert(msg);
      return;
    }

    try {
      setError(null);
      await deletePosition(idNum);
      await reloadPositionsData({ silent: true });
    } catch (err) {
      console.error(err);
      const message =
        err instanceof Error ? err.message : "Failed to delete position.";
      setError(message);
      window.alert(message);
    }
  };

  const handleSave = async () => {
    const trimmedName = form.name.trim();
    const parsedDepartmentId = Number(form.departmentId);
    const parsedCompanyId = Number(form.companyId);

    if (!trimmedName) {
      const msg = "Position name is required.";
      setError(msg);
      window.alert(msg);
      return;
    }

    if (!Number.isFinite(parsedDepartmentId) || parsedDepartmentId <= 0) {
      const msg = "Department selection is required.";
      setError(msg);
      window.alert(msg);
      return;
    }

    if (!Number.isFinite(parsedCompanyId) || parsedCompanyId <= 0) {
      const msg = "Company selection is required.";
      setError(msg);
      window.alert(msg);
      return;
    }

    const payload = {
      name: trimmedName,
      description: form.description.trim(),
      departmentId: parsedDepartmentId,
      companyId: parsedCompanyId,
      isActive: true,
    };

    console.log("FORM:", form);
    console.log("PAYLOAD:", payload);

    try {
      setIsSubmitting(true);
      setError(null);
      setFieldErrors({});

      if (isEditMode && form.id != null) {
        console.log("UPDATE MODE, ID:", form.id);
        await updatePosition(form.id, {
          name: payload.name,
          departmentId: payload.departmentId,
          description: payload.description,
          companyId: parsedCompanyId,
          isActive: payload.isActive,
        });
      } else {
        console.log("CREATE MODE");
        await createPosition({
          name: payload.name,
          departmentId: payload.departmentId,
          description: payload.description,
          companyId: parsedCompanyId,
          isActive: payload.isActive,
        });
      }

      console.log("Save success, refreshing positions...");
      await reloadPositionsData({ silent: true });

      setIsDialogOpen(false);
      setIsEditMode(false);
      setForm(emptyPositionForm());
    } catch (err) {
      console.error(err);
      if (err instanceof ApiFormError) {
        setFieldErrors(err.fieldErrors);
      }
      const message =
        err instanceof Error
          ? err.message
          : "Position save failed due to an unexpected error.";
      setError(message);
      window.alert(message);
    } finally {
      setIsSubmitting(false);
    }
  };

  const columns = [
    { key: "id" as const, label: "ID" },
    { key: "name" as const, label: "Position Name" },
    { key: "departmentName" as const, label: "Department Name" },
    { key: "description" as const, label: "Description" },
    { key: "companyName" as const, label: "Company Name" },
    {
      key: "statusLabel" as const,
      label: "Status",
      render: (_: string, row: NormalizedPosition) => (
        <Badge
          className={
            row.isActive
              ? "bg-emerald-100 text-emerald-800 hover:bg-emerald-100"
              : "bg-amber-100 text-amber-800 hover:bg-amber-100"
          }
        >
          {row.statusLabel}
        </Badge>
      ),
    },
  ];

  const departmentsForFilters = useMemo(() => {
    if (scopeCompanyId == null) return departments;
    return departments.filter((d) => d.companyId === scopeCompanyId);
  }, [departments, scopeCompanyId]);

  const companiesForForm = useMemo(() => {
    if (scopeCompanyId == null) return companies;
    return companies.filter((c) => c.id === scopeCompanyId);
  }, [companies, scopeCompanyId]);

  const departmentsForForm = useMemo(() => {
    const cid = Number(form.companyId);
    if (Number.isFinite(cid) && cid > 0) {
      return departments.filter((d) => d.companyId === cid);
    }
    if (scopeCompanyId != null) {
      return departments.filter((d) => d.companyId === scopeCompanyId);
    }
    return departments;
  }, [departments, form.companyId, scopeCompanyId]);

  const sortedDepartmentOptions = useMemo(
    () =>
      [...departmentsForFilters]
        .sort((a, b) => a.name.localeCompare(b.name))
        .map((d) => ({ value: String(d.id), label: d.name })),
    [departmentsForFilters],
  );

  const sortedCompanyOptions = useMemo(
    () =>
      [...companies]
        .sort((a, b) => a.name.localeCompare(b.name))
        .map((c) => ({ value: String(c.id), label: c.name })),
    [companies],
  );

  const positionFilterDefs = useMemo<TableFilterDef<NormalizedPosition>[]>(
    () => [
      {
        id: "positionId",
        label: "ID",
        ui: "number",
        match: (row, get) => {
          const q = get("positionId").trim().toLowerCase();
          if (!q) return true;
          return String(row.id).toLowerCase().includes(q);
        },
      },
      {
        id: "name",
        label: "Position Name",
        ui: "text",
        match: (row, get) => {
          const q = get("name").trim().toLowerCase();
          if (!q) return true;
          return row.name.toLowerCase().includes(q);
        },
      },
      {
        id: "description",
        label: "Description",
        ui: "text",
        match: (row, get) => {
          const q = get("description").trim().toLowerCase();
          if (!q) return true;
          return row.description.toLowerCase().includes(q);
        },
      },
      {
        id: "department",
        label: "Department",
        ui: "select",
        options: sortedDepartmentOptions,
        match: (row, get) => {
          const v = get("department");
          if (!v) return true;
          return row.departmentId === Number(v);
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
          if (v === "all") return true;
          if (v === "active") return row.isActive;
          if (v === "inactive") return !row.isActive;
          return true;
        },
      },
    ],
    [sortedDepartmentOptions, sortedCompanyOptions],
  );

  if (loading) {
    return (
      <div className="p-6 text-sm text-muted-foreground">
        Loading positions...
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-3xl font-bold text-foreground">Positions</h1>
        <p className="text-muted-foreground mt-1">
          Manage your organization&apos;s positions and role structure.
        </p>
      </div>

      {error && (
        <div className="rounded-md border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-600">
          {error}
        </div>
      )}

      <Dialog
        open={isDialogOpen}
        onOpenChange={(open) => {
          setIsDialogOpen(open);
          if (!open) {
            setIsEditMode(false);
            setForm(emptyPositionForm());
          }
        }}
      >
        <AdvancedTableFilters defs={positionFilterDefs} data={scopedPositions}>
          {(filtered) => (
            <DataTable
              title="Position List"
              columns={columns}
              data={filtered}
              idSortKey="id"
              searchableFields={[
                "id",
                "name",
                "description",
                "departmentName",
                "companyName",
                "statusLabel",
              ]}
              searchPlaceholder="Search positions..."
              onAdd={handleAdd}
              onEdit={handleEdit}
              onDelete={handleDelete}
            />
          )}
        </AdvancedTableFilters>

        <DialogContent className="sm:max-w-md">
          <DialogHeader>
            <DialogTitle>
              {isEditMode ? "Edit Position" : "Add Position"}
            </DialogTitle>
            <DialogDescription>
              Enter position details and save changes.
            </DialogDescription>
          </DialogHeader>

          <div className="space-y-4">
            <div>
              <label className="mb-2 block text-sm font-medium text-foreground">
                Position Name
              </label>
              <Input
                value={form.name}
                onChange={(e) =>
                  setForm((f) => ({ ...f, name: e.target.value }))
                }
                placeholder="Enter position name"
              />
              {getFieldErrorMessage(fieldErrors, "name") && (
                <p className="mt-1 text-xs text-red-600">
                  {getFieldErrorMessage(fieldErrors, "name")}
                </p>
              )}
            </div>

            <div>
              <label className="mb-2 block text-sm font-medium text-foreground">
                Department
              </label>
              <select
                value={form.departmentId}
                onChange={(e) =>
                  setForm((f) => ({ ...f, departmentId: e.target.value }))
                }
                className="flex h-10 w-full rounded-md border border-input bg-background px-3 py-2 text-sm ring-offset-background"
              >
                <option value="">Select a department</option>
                {departmentsForForm.map((department) => (
                  <option key={department.id} value={department.id}>
                    {department.name}
                  </option>
                ))}
              </select>
              {getFieldErrorMessage(fieldErrors, "departmentid") && (
                <p className="mt-1 text-xs text-red-600">
                  {getFieldErrorMessage(fieldErrors, "departmentid")}
                </p>
              )}
            </div>

            <div>
              <label className="mb-2 block text-sm font-medium text-foreground">
                Company
              </label>
              <select
                value={form.companyId}
                onChange={(e) =>
                  setForm((f) => ({ ...f, companyId: e.target.value }))
                }
                className="flex h-10 w-full rounded-md border border-input bg-background px-3 py-2 text-sm ring-offset-background"
              >
                <option value="">Select a company</option>
                {companiesForForm.map((company) => (
                  <option key={company.id} value={company.id}>
                    {company.name}
                  </option>
                ))}
                {companiesForForm.length === 0 && formCompanyNumeric > 0 && (
                  <option value={formCompanyNumeric}>{formCompanyName}</option>
                )}
              </select>
              {getFieldErrorMessage(fieldErrors, "companyid") && (
                <p className="mt-1 text-xs text-red-600">
                  {getFieldErrorMessage(fieldErrors, "companyid")}
                </p>
              )}
            </div>

            <div>
              <label className="mb-2 block text-sm font-medium text-foreground">
                Description
              </label>
              <Input
                value={form.description}
                onChange={(e) =>
                  setForm((f) => ({ ...f, description: e.target.value }))
                }
                placeholder="Enter description"
              />
              {getFieldErrorMessage(fieldErrors, "description") && (
                <p className="mt-1 text-xs text-red-600">
                  {getFieldErrorMessage(fieldErrors, "description")}
                </p>
              )}
            </div>

            <div className="flex justify-end gap-3">
              <Button
                variant="outline"
                onClick={() => {
                  setIsDialogOpen(false);
                  setIsEditMode(false);
                  setForm(emptyPositionForm());
                }}
                disabled={isSubmitting}
              >
                Cancel
              </Button>
              <Button onClick={handleSave} disabled={isSubmitting}>
                {isSubmitting ? "Saving..." : "Save"}
              </Button>
            </div>
          </div>
        </DialogContent>
      </Dialog>
    </div>
  );
}
