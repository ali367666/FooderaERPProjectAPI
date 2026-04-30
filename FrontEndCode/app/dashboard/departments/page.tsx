"use client";

import { useEffect, useMemo, useState } from "react";
import { AdvancedTableFilters, type TableFilterDef } from "@/components/advanced-table-filters";
import { DataTable } from "@/components/data-table";
import {
  createDepartment,
  deleteDepartment,
  getDepartmentsForAllCompanies,
  updateDepartment,
  type Department,
} from "@/lib/services/department-service";
import { Badge } from "@/components/ui/badge";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { Input } from "@/components/ui/input";
import { Button } from "@/components/ui/button";
import { ApiFormError, getFieldErrorMessage, type FieldErrors } from "@/lib/api-error";
import { useSelectedCompany } from "@/contexts/selected-company-context";
import { filterBySelectedCompany } from "@/lib/company-scope-utils";

type DepartmentRow = {
  id: string;
  departmentId: number;
  name: string;
  description: string;
  companyId: number;
  companyName: string;
};

export default function DepartmentsPage() {
  const { companies, companiesLoading, selectedCompanyId } = useSelectedCompany();
  const [depts, setDepts] = useState<Department[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [isDialogOpen, setIsDialogOpen] = useState(false);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [editingDepartment, setEditingDepartment] = useState<Department | null>(null);
  const [name, setName] = useState("");
  const [description, setDescription] = useState("");
  const [formCompanyId, setFormCompanyId] = useState("");
  const [fieldErrors, setFieldErrors] = useState<FieldErrors>({});

  const resetForm = () => {
    setEditingDepartment(null);
    setName("");
    setDescription("");
    setFormCompanyId(
      selectedCompanyId != null
        ? String(selectedCompanyId)
        : companies[0]
          ? String(companies[0].id)
          : "",
    );
    setFieldErrors({});
  };

  const loadDepartments = async () => {
    if (companies.length === 0) {
      setDepts([]);
      return;
    }
    try {
      setLoading(true);
      setError(null);
      const ids = companies.map((c) => c.id);
      const data = await getDepartmentsForAllCompanies(ids);
      setDepts(data);
    } catch (err) {
      console.error(err);
      setError(err instanceof Error ? err.message : "Failed to load departments.");
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    if (companiesLoading) return;
    void loadDepartments();
  }, [companiesLoading, companies]);

  const handleAdd = () => {
    resetForm();
    setIsDialogOpen(true);
  };

  const companiesForFormSelect = useMemo(() => {
    if (selectedCompanyId == null) return companies;
    return companies.filter((c) => c.id === selectedCompanyId);
  }, [companies, selectedCompanyId]);

  const handleEdit = (row: DepartmentRow) => {
    const target = depts.find((d) => d.id === row.departmentId);
    if (!target) return;

    setEditingDepartment(target);
    setName(target.name);
    setDescription(target.description || "");
    setFormCompanyId(String(target.companyId));
    setIsDialogOpen(true);
  };

  const handleDelete = async (row: DepartmentRow) => {
    const confirmed = window.confirm(`Delete "${row.name}"?`);
    if (!confirmed) return;

    try {
      setError(null);
      await deleteDepartment(row.departmentId, row.companyId);
      await loadDepartments();
    } catch (err) {
      console.error(err);
      const message = err instanceof Error ? err.message : "Failed to delete department.";
      setError(message);
      window.alert(message);
    }
  };

  const handleSave = async () => {
    const trimmedName = name.trim();
    if (!trimmedName) {
      const msg = "Department name is required.";
      setError(msg);
      window.alert(msg);
      return;
    }

    try {
      setIsSubmitting(true);
      setError(null);
      setFieldErrors({});

      if (editingDepartment) {
        await updateDepartment(editingDepartment.id, {
          name: trimmedName,
          description: description.trim(),
          companyId: editingDepartment.companyId,
        });
      } else {
        const cid = Number(formCompanyId);
        if (!Number.isFinite(cid) || cid <= 0) {
          window.alert("Company is required.");
          setIsSubmitting(false);
          return;
        }
        await createDepartment({
          name: trimmedName,
          description: description.trim(),
          companyId: cid,
        });
      }

      setIsDialogOpen(false);
      resetForm();
      await loadDepartments();
    } catch (err) {
      console.error(err);
      if (err instanceof ApiFormError) {
        setFieldErrors(err.fieldErrors);
      }
      const message =
        err instanceof Error
          ? err.message
          : "Department save failed due to an unexpected error.";
      setError(message);
      window.alert(message);
    } finally {
      setIsSubmitting(false);
    }
  };

  const companyNameById = useMemo(
    () => new Map(companies.map((c) => [c.id, c.name])),
    [companies],
  );

  const sortedCompanyOptions = useMemo(
    () =>
      [...companies]
        .sort((a, b) => a.name.localeCompare(b.name))
        .map((c) => ({ value: String(c.id), label: c.name })),
    [companies],
  );

  const rows: DepartmentRow[] = useMemo(
    () =>
      depts.map((d) => ({
        id: String(d.id),
        departmentId: d.id,
        name: d.name,
        description: d.description || "-",
        companyId: d.companyId,
        companyName: companyNameById.get(d.companyId) || `Company #${d.companyId}`,
      })),
    [depts, companyNameById],
  );

  const scopedRows = useMemo(
    () => filterBySelectedCompany(rows, selectedCompanyId, (r) => r.companyId),
    [rows, selectedCompanyId],
  );

  const departmentFilterDefs = useMemo<TableFilterDef<DepartmentRow>[]>(
    () => [
      {
        id: "departmentId",
        label: "ID",
        ui: "number",
        match: (row, get) => {
          const q = get("departmentId").trim().toLowerCase();
          if (!q) return true;
          return String(row.departmentId).toLowerCase().includes(q);
        },
      },
      {
        id: "name",
        label: "Department Name",
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
        match: (_row, get) => {
          const v = get("status");
          return v === "all" || v === "active";
        },
      },
    ],
    [sortedCompanyOptions],
  );

  const columns = [
    {
      key: "departmentId" as const,
      label: "ID",
    },
    {
      key: "name" as const,
      label: "Department Name",
    },
    {
      key: "description" as const,
      label: "Description",
    },
    {
      key: "companyName" as const,
      label: "Company",
    },
    {
      key: "id" as const,
      label: "Status",
      render: () => (
        <Badge className="bg-emerald-100 text-emerald-800 hover:bg-emerald-100">
          Active
        </Badge>
      ),
    },
  ];

  if (companiesLoading || loading) {
    return <div className="p-6 text-sm text-muted-foreground">Loading departments...</div>;
  }

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-3xl font-bold text-foreground">Departments</h1>
        <p className="text-muted-foreground mt-1">
          Manage your organization&apos;s departments and structure.
        </p>
      </div>

      {error && (
        <div className="rounded-md border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-600">
          {error}
        </div>
      )}

      <Dialog open={isDialogOpen} onOpenChange={setIsDialogOpen}>
        <AdvancedTableFilters defs={departmentFilterDefs} data={scopedRows}>
          {(filtered) => (
            <DataTable
              title="Department List"
              columns={columns}
              data={filtered}
              idSortKey="departmentId"
              searchableFields={["name", "description", "companyName", "id"]}
              searchPlaceholder="Search departments..."
              onAdd={handleAdd}
              onEdit={handleEdit}
              onDelete={handleDelete}
            />
          )}
        </AdvancedTableFilters>

        <DialogContent className="sm:max-w-md">
          <DialogHeader>
            <DialogTitle>
              {editingDepartment ? "Edit Department" : "Add Department"}
            </DialogTitle>
            <DialogDescription>
              Enter department details and save changes.
            </DialogDescription>
          </DialogHeader>

          <div className="space-y-4">
            <div>
              <label className="mb-2 block text-sm font-medium text-foreground">
                Department Name
              </label>
              <Input
                value={name}
                onChange={(e) => setName(e.target.value)}
                placeholder="Enter department name"
              />
              {getFieldErrorMessage(fieldErrors, "name") && (
                <p className="mt-1 text-xs text-red-600">
                  {getFieldErrorMessage(fieldErrors, "name")}
                </p>
              )}
            </div>

            <div>
              <label className="mb-2 block text-sm font-medium text-foreground">
                Description
              </label>
              <Input
                value={description}
                onChange={(e) => setDescription(e.target.value)}
                placeholder="Enter description"
              />
              {getFieldErrorMessage(fieldErrors, "description") && (
                <p className="mt-1 text-xs text-red-600">
                  {getFieldErrorMessage(fieldErrors, "description")}
                </p>
              )}
            </div>

            {!editingDepartment && (
              <div>
                <label className="mb-2 block text-sm font-medium text-foreground">Company</label>
                <select
                  className="flex h-10 w-full rounded-md border border-input bg-background px-3 py-2 text-sm ring-offset-background"
                  value={formCompanyId}
                  onChange={(e) => setFormCompanyId(e.target.value)}
                >
                  <option value="">Select company</option>
                  {companiesForFormSelect.map((c) => (
                    <option key={c.id} value={String(c.id)}>
                      {c.name}
                    </option>
                  ))}
                </select>
              </div>
            )}

            <div className="flex justify-end gap-3">
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
          </div>
        </DialogContent>
      </Dialog>
    </div>
  );
}