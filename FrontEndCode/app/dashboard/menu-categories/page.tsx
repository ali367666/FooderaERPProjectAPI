"use client";

import { useEffect, useMemo, useState } from "react";
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
  createMenuCategory,
  deleteMenuCategory,
  getMenuCategories,
  getMenuCategoryById,
  updateMenuCategory,
  type MenuCategory,
} from "@/lib/services/menu-category-service";
import { ApiFormError, getFieldErrorMessage, type FieldErrors } from "@/lib/api-error";
import { useSelectedCompany } from "@/contexts/selected-company-context";

type MenuCategoryRow = {
  id: string;
  categoryId: number;
  name: string;
  description: string;
  isActive: boolean;
  statusLabel: string;
};

const selectClass =
  "flex h-10 w-full rounded-md border border-input bg-background px-3 py-2 text-sm ring-offset-background";

export default function MenuCategoriesPage() {
  const { selectedCompanyId } = useSelectedCompany();
  const [categories, setCategories] = useState<MenuCategory[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [isDialogOpen, setIsDialogOpen] = useState(false);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [isEditMode, setIsEditMode] = useState(false);
  const [name, setName] = useState("");
  const [description, setDescription] = useState("");
  const [isActive, setIsActive] = useState(true);
  const [editingId, setEditingId] = useState<number | null>(null);
  const [fieldErrors, setFieldErrors] = useState<FieldErrors>({});

  const loadCategories = async (silent = false) => {
    try {
      if (!silent) setLoading(true);
      setError(null);
      const data = await getMenuCategories();
      setCategories(data);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to load menu categories.");
    } finally {
      if (!silent) setLoading(false);
    }
  };

  useEffect(() => {
    void loadCategories();
  }, []);

  const rows: MenuCategoryRow[] = useMemo(
    () =>
      categories.map((c) => ({
        id: String(c.id),
        categoryId: c.id,
        name: c.name,
        description: c.description?.trim() || "-",
        isActive: c.isActive,
        statusLabel: c.isActive ? "Active" : "Inactive",
      })),
    [categories],
  );

  const filterDefs = useMemo<TableFilterDef<MenuCategoryRow>[]>(
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
    { key: "categoryId" as const, label: "ID" },
    { key: "name" as const, label: "Category Name" },
    { key: "description" as const, label: "Description" },
    {
      key: "statusLabel" as const,
      label: "Status",
      render: (_: string, row: MenuCategoryRow) => (
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

  const resetForm = () => {
    setName("");
    setDescription("");
    setIsActive(true);
    setEditingId(null);
    setFieldErrors({});
    setIsEditMode(false);
  };

  const handleAdd = () => {
    resetForm();
    setIsDialogOpen(true);
  };

  const handleEdit = async (row: MenuCategoryRow) => {
    try {
      setError(null);
      const c = await getMenuCategoryById(row.categoryId, selectedCompanyId ?? undefined);
      setEditingId(c.id);
      setName(c.name || "");
      setDescription(c.description || "");
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

  const handleDelete = async (row: MenuCategoryRow) => {
    if (!window.confirm(`Delete category "${row.name}"?`)) return;
    try {
      setError(null);
      await deleteMenuCategory(row.categoryId, selectedCompanyId ?? undefined);
      await loadCategories(true);
    } catch (err) {
      const message = err instanceof Error ? err.message : "Failed to delete category.";
      setError(message);
      window.alert(message);
    }
  };

  const handleSave = async () => {
    const trimmedName = name.trim();
    if (!trimmedName) {
      const msg = "Category name is required.";
      setError(msg);
      window.alert(msg);
      return;
    }

    try {
      setIsSubmitting(true);
      setError(null);
      setFieldErrors({});

      if (isEditMode && editingId != null) {
        await updateMenuCategory(
          editingId,
          {
            name: trimmedName,
            description: description.trim() || null,
            isActive,
          },
          selectedCompanyId ?? undefined,
        );
      } else {
        await createMenuCategory({
          name: trimmedName,
          description: description.trim() || null,
        });
      }

      setIsDialogOpen(false);
      resetForm();
      await loadCategories(true);
    } catch (err) {
      if (err instanceof ApiFormError) setFieldErrors(err.fieldErrors);
      const message =
        err instanceof Error ? err.message : "Save failed due to an unexpected error.";
      setError(message);
      window.alert(message);
    } finally {
      setIsSubmitting(false);
    }
  };

  if (loading) {
    return <div className="p-6 text-sm text-muted-foreground">Loading menu categories...</div>;
  }

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-3xl font-bold text-foreground">Menu Categories</h1>
        <p className="text-muted-foreground mt-1">
          Organize your menu into categories for each company.
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
          if (!open) resetForm();
        }}
      >
        <AdvancedTableFilters defs={filterDefs} data={rows}>
          {(filtered) => (
            <DataTable
              title="Menu Category List"
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

        <DialogContent className="sm:max-w-md">
          <DialogHeader>
            <DialogTitle>{isEditMode ? "Edit Menu Category" : "Add Menu Category"}</DialogTitle>
            <DialogDescription>Name and describe the category, then save.</DialogDescription>
          </DialogHeader>

          <div className="space-y-4">
            <div>
              <label className="mb-2 block text-sm font-medium text-foreground">Name</label>
              <Input
                value={name}
                onChange={(e) => setName(e.target.value)}
                placeholder="Category name"
              />
              {getFieldErrorMessage(fieldErrors, "name") && (
                <p className="mt-1 text-xs text-red-600">{getFieldErrorMessage(fieldErrors, "name")}</p>
              )}
            </div>

            <div>
              <label className="mb-2 block text-sm font-medium text-foreground">Description</label>
              <Input
                value={description}
                onChange={(e) => setDescription(e.target.value)}
                placeholder="Optional description"
              />
              {getFieldErrorMessage(fieldErrors, "description") && (
                <p className="mt-1 text-xs text-red-600">
                  {getFieldErrorMessage(fieldErrors, "description")}
                </p>
              )}
            </div>

            {isEditMode && (
              <div>
                <label className="mb-2 block text-sm font-medium text-foreground">Status</label>
                <select
                  value={isActive ? "true" : "false"}
                  onChange={(e) => setIsActive(e.target.value === "true")}
                  className={selectClass}
                >
                  <option value="true">Active</option>
                  <option value="false">Inactive</option>
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
