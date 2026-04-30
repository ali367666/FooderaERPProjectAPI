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
  createMenuItem,
  deleteMenuItem,
  getMenuItemById,
  getMenuItems,
  PreparationType,
  preparationTypeLabel,
  updateMenuItem,
  type MenuItem,
  type PreparationTypeValue,
} from "@/lib/services/menu-item-service";
import { getMenuCategories, type MenuCategory } from "@/lib/services/menu-category-service";
import { ApiFormError, getFieldErrorMessage, type FieldErrors } from "@/lib/api-error";
import { usePermissionSet } from "@/hooks/use-auth-permissions";
import { AppPermissions } from "@/lib/app-permissions";

type MenuItemRow = {
  id: string;
  itemId: number;
  name: string;
  description: string;
  price: number;
  priceDisplay: string;
  portion: string;
  menuCategoryId: number;
  menuCategoryName: string;
  preparationType: PreparationTypeValue;
  preparationLabel: string;
  isActive: boolean;
  statusLabel: string;
};

const selectClass =
  "flex h-10 w-full rounded-md border border-input bg-background px-3 py-2 text-sm ring-offset-background";

function formatPrice(value: number): string {
  if (!Number.isFinite(value)) return "-";
  return value.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 });
}

export default function MenuItemsPage() {
  const permissions = usePermissionSet();
  const canCreate = permissions.has(AppPermissions.MenuItemCreate);
  const canUpdate = permissions.has(AppPermissions.MenuItemUpdate);
  const canDelete = permissions.has(AppPermissions.MenuItemDelete);

  const [items, setItems] = useState<MenuItem[]>([]);
  const [categories, setCategories] = useState<MenuCategory[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [isDialogOpen, setIsDialogOpen] = useState(false);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [isEditMode, setIsEditMode] = useState(false);
  const [editingId, setEditingId] = useState<number | null>(null);
  const [name, setName] = useState("");
  const [description, setDescription] = useState("");
  const [price, setPrice] = useState("");
  const [portion, setPortion] = useState("");
  const [menuCategoryId, setMenuCategoryId] = useState("");
  const [preparationType, setPreparationType] = useState<string>(String(PreparationType.Kitchen));
  const [isActive, setIsActive] = useState(true);
  const [fieldErrors, setFieldErrors] = useState<FieldErrors>({});

  const loadData = async (silent = false) => {
    try {
      if (!silent) setLoading(true);
      setError(null);
      const [itemData, categoryData] = await Promise.all([getMenuItems(), getMenuCategories()]);
      setItems(itemData);
      setCategories(categoryData);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to load menu items.");
    } finally {
      if (!silent) setLoading(false);
    }
  };

  useEffect(() => {
    void loadData();
  }, []);

  const sortedCategoryOptions = useMemo(
    () =>
      [...categories]
        .sort((a, b) => a.name.localeCompare(b.name))
        .map((c) => ({ value: String(c.id), label: c.name })),
    [categories],
  );

  const rows: MenuItemRow[] = useMemo(
    () =>
      items.map((item) => ({
        id: String(item.id),
        itemId: item.id,
        name: item.name,
        description: item.description?.trim() || "-",
        price: item.price,
        priceDisplay: formatPrice(item.price),
        portion: item.portion?.trim() || "-",
        menuCategoryId: item.menuCategoryId,
        menuCategoryName: item.menuCategoryName || `Category #${item.menuCategoryId}`,
        preparationType: item.preparationType,
        preparationLabel: preparationTypeLabel(item.preparationType),
        isActive: item.isActive,
        statusLabel: item.isActive ? "Active" : "Inactive",
      })),
    [items],
  );

  const preparationFilterOptions = useMemo(
    () => [
      { value: String(PreparationType.None), label: preparationTypeLabel(PreparationType.None) },
      { value: String(PreparationType.Kitchen), label: preparationTypeLabel(PreparationType.Kitchen) },
      { value: String(PreparationType.Bar), label: preparationTypeLabel(PreparationType.Bar) },
    ],
    [],
  );

  const filterDefs = useMemo<TableFilterDef<MenuItemRow>[]>(
    () => [
      {
        id: "itemId",
        label: "ID",
        ui: "number",
        match: (row, get) => {
          const q = get("itemId").trim().toLowerCase();
          if (!q) return true;
          return String(row.itemId).toLowerCase().includes(q);
        },
      },
      {
        id: "name",
        label: "Item Name",
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
        id: "category",
        label: "Category",
        ui: "select",
        options: sortedCategoryOptions,
        match: (row, get) => {
          const v = get("category");
          if (!v) return true;
          return row.menuCategoryId === Number(v);
        },
      },
      {
        id: "price",
        label: "Price",
        ui: "numberRange",
        match: (row, get) => {
          const minP = get("price:min").trim();
          const maxP = get("price:max").trim();
          const minN = minP === "" ? NaN : Number(minP);
          const maxN = maxP === "" ? NaN : Number(maxP);
          if (minP !== "" && (!Number.isFinite(minN) || row.price < minN)) return false;
          if (maxP !== "" && (!Number.isFinite(maxN) || row.price > maxN)) return false;
          return true;
        },
      },
      {
        id: "preparationType",
        label: "Preparation",
        ui: "select",
        options: preparationFilterOptions,
        match: (row, get) => {
          const v = get("preparationType");
          if (!v) return true;
          return row.preparationType === Number(v);
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
    [sortedCategoryOptions, preparationFilterOptions],
  );

  const columns = [
    { key: "itemId" as const, label: "ID" },
    { key: "name" as const, label: "Item Name" },
    { key: "menuCategoryName" as const, label: "Category" },
    { key: "priceDisplay" as const, label: "Price" },
    { key: "portion" as const, label: "Portion" },
    { key: "preparationLabel" as const, label: "Preparation" },
    { key: "description" as const, label: "Description" },
    {
      key: "statusLabel" as const,
      label: "Status",
      render: (_: string, row: MenuItemRow) => (
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
    setPrice("");
    setPortion("");
    setMenuCategoryId("");
    setPreparationType(String(PreparationType.Kitchen));
    setIsActive(true);
    setEditingId(null);
    setFieldErrors({});
    setIsEditMode(false);
  };

  const handleAdd = () => {
    if (!canCreate) return;
    resetForm();
    setIsDialogOpen(true);
  };

  const handleEdit = async (row: MenuItemRow) => {
    if (!canUpdate) return;
    try {
      setError(null);
      const item = await getMenuItemById(row.itemId);
      setEditingId(item.id);
      setName(item.name || "");
      setDescription(item.description || "");
      setPrice(String(item.price ?? ""));
      setPortion(item.portion || "");
      setMenuCategoryId(String(item.menuCategoryId || ""));
      setPreparationType(String(item.preparationType ?? PreparationType.Kitchen));
      setIsActive(item.isActive);
      setFieldErrors({});
      setIsEditMode(true);
      setIsDialogOpen(true);
    } catch (err) {
      const message = err instanceof Error ? err.message : "Failed to load menu item.";
      setError(message);
      window.alert(message);
    }
  };

  const handleDelete = async (row: MenuItemRow) => {
    if (!canDelete) return;
    if (!window.confirm(`Delete menu item "${row.name}"?`)) return;
    try {
      setError(null);
      await deleteMenuItem(row.itemId);
      await loadData(true);
    } catch (err) {
      const message = err instanceof Error ? err.message : "Failed to delete menu item.";
      setError(message);
      window.alert(message);
    }
  };

  const handleSave = async () => {
    const trimmedName = name.trim();
    const categoryId = Number(menuCategoryId);
    const priceNum = Number(price);

    if (!trimmedName) {
      const msg = "Item name is required.";
      setError(msg);
      window.alert(msg);
      return;
    }
    if (!Number.isFinite(categoryId) || categoryId <= 0) {
      const msg = "Category is required.";
      setError(msg);
      window.alert(msg);
      return;
    }
    if (!Number.isFinite(priceNum) || priceNum < 0) {
      const msg = "Enter a valid price.";
      setError(msg);
      window.alert(msg);
      return;
    }

    const prep = Number(preparationType) as PreparationTypeValue;
    const prepOk =
      prep === PreparationType.None || prep === PreparationType.Kitchen || prep === PreparationType.Bar;
    if (!prepOk) {
      const msg = "Select a preparation type.";
      setError(msg);
      window.alert(msg);
      return;
    }

    try {
      setIsSubmitting(true);
      setError(null);
      setFieldErrors({});

      if (isEditMode && editingId != null) {
        await updateMenuItem(editingId, {
          name: trimmedName,
          description: description.trim() || null,
          price: priceNum,
          portion: portion.trim() || null,
          menuCategoryId: categoryId,
          preparationType: prep,
          isActive,
        });
      } else {
        await createMenuItem({
          name: trimmedName,
          description: description.trim() || null,
          price: priceNum,
          portion: portion.trim() || null,
          menuCategoryId: categoryId,
          preparationType: prep,
        });
      }

      setIsDialogOpen(false);
      resetForm();
      await loadData(true);
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
    return <div className="p-6 text-sm text-muted-foreground">Loading menu items...</div>;
  }

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-3xl font-bold text-foreground">Menu Items</h1>
        <p className="text-muted-foreground mt-1">
          Manage dishes and products on your menu, linked to categories.
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
              title="Menu Item List"
              columns={columns}
              data={filtered}
              idSortKey="itemId"
              searchableFields={[
                "name",
                "description",
                "menuCategoryName",
                "portion",
                "preparationLabel",
                "priceDisplay",
                "statusLabel",
                "id",
              ]}
              searchPlaceholder="Search menu items..."
              onAdd={canCreate ? handleAdd : undefined}
              onEdit={canUpdate ? handleEdit : undefined}
              onDelete={canDelete ? handleDelete : undefined}
            />
          )}
        </AdvancedTableFilters>

        <DialogContent className="sm:max-w-lg">
          <DialogHeader>
            <DialogTitle>{isEditMode ? "Edit Menu Item" : "Add Menu Item"}</DialogTitle>
            <DialogDescription>
              Set name, category, price, and how the item is prepared.
            </DialogDescription>
          </DialogHeader>

          <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
            <div className="sm:col-span-2">
              <label className="mb-2 block text-sm font-medium text-foreground">Name</label>
              <Input value={name} onChange={(e) => setName(e.target.value)} placeholder="Item name" />
              {getFieldErrorMessage(fieldErrors, "name") && (
                <p className="mt-1 text-xs text-red-600">{getFieldErrorMessage(fieldErrors, "name")}</p>
              )}
            </div>

            <div className="sm:col-span-2">
              <label className="mb-2 block text-sm font-medium text-foreground">Category</label>
              <select
                value={menuCategoryId}
                onChange={(e) => setMenuCategoryId(e.target.value)}
                className={selectClass}
              >
                <option value="">Select category</option>
                {categories.map((c) => (
                  <option key={c.id} value={String(c.id)}>
                    {c.name}
                  </option>
                ))}
              </select>
              {getFieldErrorMessage(fieldErrors, "menucategoryid") && (
                <p className="mt-1 text-xs text-red-600">
                  {getFieldErrorMessage(fieldErrors, "menucategoryid")}
                </p>
              )}
            </div>

            <div>
              <label className="mb-2 block text-sm font-medium text-foreground">Price</label>
              <Input
                type="number"
                min={0}
                step="0.01"
                value={price}
                onChange={(e) => setPrice(e.target.value)}
              />
              {getFieldErrorMessage(fieldErrors, "price") && (
                <p className="mt-1 text-xs text-red-600">{getFieldErrorMessage(fieldErrors, "price")}</p>
              )}
            </div>

            <div>
              <label className="mb-2 block text-sm font-medium text-foreground">Portion</label>
              <Input
                value={portion}
                onChange={(e) => setPortion(e.target.value)}
                placeholder="e.g. 250g"
              />
            </div>

            <div>
              <label className="mb-2 block text-sm font-medium text-foreground">Preparation</label>
              <select
                value={preparationType}
                onChange={(e) => setPreparationType(e.target.value)}
                className={selectClass}
              >
                <option value={String(PreparationType.None)}>{preparationTypeLabel(PreparationType.None)}</option>
                <option value={String(PreparationType.Kitchen)}>
                  {preparationTypeLabel(PreparationType.Kitchen)}
                </option>
                <option value={String(PreparationType.Bar)}>{preparationTypeLabel(PreparationType.Bar)}</option>
              </select>
              {getFieldErrorMessage(fieldErrors, "preparationtype") && (
                <p className="mt-1 text-xs text-red-600">
                  {getFieldErrorMessage(fieldErrors, "preparationtype")}
                </p>
              )}
            </div>

            <div className="sm:col-span-2">
              <label className="mb-2 block text-sm font-medium text-foreground">Description</label>
              <Input
                value={description}
                onChange={(e) => setDescription(e.target.value)}
                placeholder="Optional"
              />
            </div>

            {isEditMode && (
              <div className="sm:col-span-2">
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
