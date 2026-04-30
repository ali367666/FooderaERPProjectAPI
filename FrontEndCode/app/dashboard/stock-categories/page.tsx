"use client";

import { useEffect, useMemo, useState } from "react";
import { useSearchParams } from "next/navigation";
import { AdvancedTableFilters, type TableFilterDef } from "@/components/advanced-table-filters";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Checkbox } from "@/components/ui/checkbox";
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
  createStockCategory,
  deleteStockCategory,
  getAllStockCategoriesForAllCompanies,
  getStockCategoryById,
  updateStockCategory,
  type StockCategory,
} from "@/lib/services/stock-category-service";
import { resolveCompanyId } from "@/lib/resolve-company-id";
import { ApiFormError, getFieldErrorMessage, type FieldErrors } from "@/lib/api-error";
import { useSelectedCompany } from "@/contexts/selected-company-context";
import { filterBySelectedCompany } from "@/lib/company-scope-utils";

type CategoryRow = {
  id: string;
  categoryId: number;
  name: string;
  description: string;
  companyId: number;
  isActive: boolean;
  statusLabel: string;
};

const selectClass =
  "flex h-10 w-full rounded-md border border-input bg-background px-3 py-2 text-sm ring-offset-background";

export default function StockCategoriesPage() {
  const searchParams = useSearchParams();
  const { companies, companiesLoading, selectedCompanyId: scopeCompanyId } = useSelectedCompany();
  const [categories, setCategories] = useState<StockCategory[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [isDialogOpen, setIsDialogOpen] = useState(false);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [isEditMode, setIsEditMode] = useState(false);
  const [editingId, setEditingId] = useState<number | null>(null);
  const [name, setName] = useState("");
  const [description, setDescription] = useState("");
  const [companyId, setCompanyId] = useState("");
  const [parentId, setParentId] = useState("");
  const [isActive, setIsActive] = useState(true);
  const [fieldErrors, setFieldErrors] = useState<FieldErrors>({});

  const loadData = async (silent = false) => {
    if (companies.length === 0) {
      setCategories([]);
      return;
    }
    try {
      if (!silent) setLoading(true);
      setError(null);
      const ids = companies.map((c) => c.id);
      const catData = await getAllStockCategoriesForAllCompanies(ids);
      setCategories(catData);
      const defaultCo = scopeCompanyId ?? companies[0]?.id ?? resolveCompanyId();
      if (!companyId) setCompanyId(String(defaultCo));
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to load stock categories.");
    } finally {
      if (!silent) setLoading(false);
    }
  };

  useEffect(() => {
    if (companiesLoading) return;
    void loadData();
  }, [companiesLoading, companies]);

  const sortedCompanyOptions = useMemo(
    () =>
      [...companies].sort((a, b) => a.name.localeCompare(b.name)).map((c) => ({ value: String(c.id), label: c.name })),
    [companies],
  );

  const parentOptionsForForm = useMemo(() => {
    const comp = Number(companyId);
    return [...categories]
      .filter((c) => c.companyId === comp && (!isEditMode || editingId == null || c.id !== editingId))
      .sort((a, b) => a.name.localeCompare(b.name))
      .map((c) => ({ value: String(c.id), label: c.name }));
  }, [categories, companyId, isEditMode, editingId]);

  const selectedId = Number(searchParams.get("selectedId") ?? "");
  const hasSelectedId = Number.isFinite(selectedId) && selectedId > 0;

  const rows: CategoryRow[] = useMemo(
    () =>
      categories.map((c) => ({
        id: String(c.id),
        categoryId: c.id,
        name: c.name,
        description: c.description?.trim() || "—",
        companyId: c.companyId,
        isActive: c.isActive,
        statusLabel: c.isActive ? "Active" : "Inactive",
      })),
    [categories],
  );

  const scopedRows = useMemo(() => {
    const filtered = filterBySelectedCompany(rows, scopeCompanyId, (r) => r.companyId);
    if (!hasSelectedId) return filtered;
    return [...filtered].sort((a, b) =>
      a.categoryId === selectedId ? -1 : b.categoryId === selectedId ? 1 : 0,
    );
  }, [rows, scopeCompanyId, hasSelectedId, selectedId]);

  const companiesForForm = useMemo(() => {
    if (scopeCompanyId == null) return companies;
    return companies.filter((c) => c.id === scopeCompanyId);
  }, [companies, scopeCompanyId]);

  const filterDefs = useMemo<TableFilterDef<CategoryRow>[]>(
    () => [
      {
        id: "categoryId",
        label: "ID",
        ui: "number",
        match: (row, get) => {
          const q = get("categoryId").trim().toLowerCase();
          if (!q) return true;
          return String(row.categoryId).toLowerCase().includes(q);
        },
      },
      {
        id: "name",
        label: "Category Name",
        ui: "text",
        match: (row, get) => {
          const q = get("name").trim().toLowerCase();
          if (!q) return true;
          return row.name.toLowerCase().includes(q);
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
    [],
  );

  const columns = [
    {
      key: "categoryId" as const,
      label: "ID",
      render: (value: number, row: CategoryRow) => (
        <span className={row.categoryId === selectedId ? "font-semibold text-primary" : ""}>
          {value}
        </span>
      ),
    },
    { key: "name" as const, label: "Category Name" },
    { key: "description" as const, label: "Description" },
    {
      key: "statusLabel" as const,
      label: "Status",
      render: (_: string, row: CategoryRow) => (
        <Badge
          className={
            row.isActive
              ? "bg-emerald-100 text-emerald-800 hover:bg-emerald-100"
              : "bg-slate-100 text-slate-800 hover:bg-slate-100"
          }
        >
          {row.statusLabel}
        </Badge>
      ),
    },
  ];

  const resetForm = () => {
    setName("");
    setDescription("");
    setCompanyId(String(scopeCompanyId ?? companies[0]?.id ?? resolveCompanyId()));
    setParentId("");
    setIsActive(true);
    setEditingId(null);
    setFieldErrors({});
    setIsEditMode(false);
  };

  const handleAdd = () => {
    resetForm();
    setIsDialogOpen(true);
  };

  const handleEdit = async (row: CategoryRow) => {
    try {
      setError(null);
      const c = await getStockCategoryById(row.categoryId);
      setEditingId(c.id);
      setName(c.name);
      setDescription(c.description?.trim() || "");
      setCompanyId(String(c.companyId));
      setParentId(c.parentId ? String(c.parentId) : "");
      setIsActive(c.isActive);
      setFieldErrors({});
      setIsEditMode(true);
      setIsDialogOpen(true);
    } catch (err) {
      const message = err instanceof Error ? err.message : "Failed to load category.";
      setError(message);
      window.alert(message);
    }
  };

  const handleDelete = async (row: CategoryRow) => {
    if (!window.confirm(`Delete stock category "${row.name}"?`)) return;
    try {
      setError(null);
      await deleteStockCategory(row.categoryId);
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
    if (!trimmed) {
      window.alert("Category name is required.");
      return;
    }
    if (!Number.isFinite(comp) || comp <= 0) {
      window.alert("Company is required.");
      return;
    }
    const parentRaw = parentId.trim();
    let parent: number | null = null;
    if (parentRaw) {
      const p = Number(parentRaw);
      if (!Number.isFinite(p) || p <= 0) {
        window.alert("Invalid parent category.");
        return;
      }
      if (isEditMode && editingId != null && p === editingId) {
        window.alert("A category cannot be its own parent.");
        return;
      }
      parent = p;
    }

    const payload = {
      name: trimmed,
      description: description.trim() || null,
      isActive,
      companyId: comp,
      parentId: parent,
    };

    try {
      setIsSubmitting(true);
      setError(null);
      setFieldErrors({});
      if (isEditMode && editingId != null) {
        await updateStockCategory(editingId, payload);
      } else {
        await createStockCategory(payload);
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
    return <div className="p-6 text-sm text-muted-foreground">Loading stock categories...</div>;
  }

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-3xl font-bold text-foreground">Stock Categories</h1>
        <p className="text-muted-foreground mt-1">Organize stock items into categories per company.</p>
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
              title="Stock Category List"
              columns={columns}
              data={filtered}
              idSortKey="categoryId"
              searchableFields={["name", "description", "statusLabel", "id"]}
              searchPlaceholder="Search categories..."
              onAdd={handleAdd}
              onEdit={handleEdit}
              onDelete={handleDelete}
            />
          )}
        </AdvancedTableFilters>

        <DialogContent className="sm:max-w-lg max-h-[90vh] overflow-y-auto">
          <DialogHeader>
            <DialogTitle>{isEditMode ? "Edit Stock Category" : "Add Stock Category"}</DialogTitle>
            <DialogDescription>Define the category name, company, and optional parent.</DialogDescription>
          </DialogHeader>

          <div className="grid grid-cols-1 gap-4">
            <div>
              <label className="mb-2 block text-sm font-medium text-foreground">Category Name</label>
              <Input value={name} onChange={(e) => setName(e.target.value)} />
              {getFieldErrorMessage(fieldErrors, "name") && (
                <p className="mt-1 text-xs text-red-600">{getFieldErrorMessage(fieldErrors, "name")}</p>
              )}
            </div>
            <div>
              <label className="mb-2 block text-sm font-medium text-foreground">Description</label>
              <Input
                value={description}
                onChange={(e) => setDescription(e.target.value)}
                placeholder="Optional"
              />
            </div>
            <div>
              <label className="mb-2 block text-sm font-medium text-foreground">Company</label>
              <select value={companyId} onChange={(e) => setCompanyId(e.target.value)} className={selectClass}>
                <option value="">Select company</option>
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
            <div>
              <label className="mb-2 block text-sm font-medium text-foreground">Parent category (optional)</label>
              <select value={parentId} onChange={(e) => setParentId(e.target.value)} className={selectClass}>
                <option value="">None (root)</option>
                {parentOptionsForForm.map((o) => (
                  <option key={o.value} value={o.value}>
                    {o.label}
                  </option>
                ))}
              </select>
            </div>
            <div className="flex items-center gap-2">
              <Checkbox
                id="cat-active"
                checked={isActive}
                onCheckedChange={(v) => setIsActive(v === true)}
              />
              <label htmlFor="cat-active" className="text-sm font-medium text-foreground cursor-pointer">
                Active
              </label>
            </div>
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
