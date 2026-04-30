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
  createStockItem,
  deleteStockItem,
  getStockItemById,
  getStockItemsForAllCompanies,
  StockItemType,
  stockItemTypeLabel,
  UnitOfMeasure,
  unitLabel,
  updateStockItem,
  type StockItem,
  type StockItemTypeValue,
  type UnitOfMeasureValue,
} from "@/lib/services/stock-item-service";
import {
  getAllStockCategoriesForAllCompanies,
  type StockCategory,
} from "@/lib/services/stock-category-service";
import { getRestaurants, type Restaurant } from "@/lib/services/restaurant-service";
import { resolveCompanyId } from "@/lib/resolve-company-id";
import { ApiFormError, getFieldErrorMessage, type FieldErrors } from "@/lib/api-error";
import { useSelectedCompany } from "@/contexts/selected-company-context";
import { filterBySelectedCompany } from "@/lib/company-scope-utils";

type StockItemRow = {
  id: string;
  stockItemId: number;
  name: string;
  unitLabel: string;
  categoryId: number;
  categoryName: string;
  companyId: number;
  description: string;
  isActive: boolean;
  statusLabel: string;
};

const selectClass =
  "flex h-10 w-full rounded-md border border-input bg-background px-3 py-2 text-sm ring-offset-background";

const UNIT_OPTIONS = [
  UnitOfMeasure.Piece,
  UnitOfMeasure.Kg,
  UnitOfMeasure.Gram,
  UnitOfMeasure.Liter,
  UnitOfMeasure.Ml,
] as const;

const TYPE_OPTIONS = [
  StockItemType.RawMaterial,
  StockItemType.FinishedGood,
  StockItemType.NonFood,
] as const;

export default function StockItemsPage() {
  const searchParams = useSearchParams();
  const { companies, companiesLoading, selectedCompanyId: scopeCompanyId } = useSelectedCompany();
  const [items, setItems] = useState<StockItem[]>([]);
  const [categories, setCategories] = useState<StockCategory[]>([]);
  const [restaurants, setRestaurants] = useState<Restaurant[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [isDialogOpen, setIsDialogOpen] = useState(false);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [isEditMode, setIsEditMode] = useState(false);
  const [editingId, setEditingId] = useState<number | null>(null);
  const [name, setName] = useState("");
  const [barcode, setBarcode] = useState("");
  const [type, setType] = useState<string>(String(StockItemType.RawMaterial));
  const [unit, setUnit] = useState<string>(String(UnitOfMeasure.Piece));
  const [categoryId, setCategoryId] = useState("");
  const [companyId, setCompanyId] = useState("");
  const [restaurantId, setRestaurantId] = useState("");
  const [fieldErrors, setFieldErrors] = useState<FieldErrors>({});

  const loadData = async (silent = false) => {
    if (companies.length === 0) {
      setItems([]);
      setCategories([]);
      setRestaurants([]);
      return;
    }
    try {
      if (!silent) setLoading(true);
      setError(null);
      const ids = companies.map((c) => c.id);
      const [itemData, categoryData, restaurantData] = await Promise.all([
        getStockItemsForAllCompanies(ids),
        getAllStockCategoriesForAllCompanies(ids),
        getRestaurants(),
      ]);
      setItems(itemData);
      setCategories(categoryData);
      setRestaurants(restaurantData);
      const defaultCo = scopeCompanyId ?? companies[0]?.id ?? resolveCompanyId();
      if (!companyId) setCompanyId(String(defaultCo));
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to load stock items.");
    } finally {
      if (!silent) setLoading(false);
    }
  };

  useEffect(() => {
    if (companiesLoading) return;
    void loadData();
  }, [companiesLoading, companies]);

  const categoriesScoped = useMemo(() => {
    if (scopeCompanyId == null) return categories;
    return categories.filter((c) => c.companyId === scopeCompanyId);
  }, [categories, scopeCompanyId]);

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

  const sortedCategoryOptions = useMemo(
    () =>
      [...categoriesScoped]
        .sort((a, b) => a.name.localeCompare(b.name))
        .map((c) => ({ value: String(c.id), label: c.name })),
    [categoriesScoped],
  );

  const unitFilterOptions = useMemo(
    () => UNIT_OPTIONS.map((u) => ({ value: String(u), label: unitLabel(u) })),
    [],
  );

  const selectedId = Number(searchParams.get("selectedId") ?? "");
  const hasSelectedId = Number.isFinite(selectedId) && selectedId > 0;

  const rows: StockItemRow[] = useMemo(
    () =>
      items.map((item) => ({
        id: String(item.id),
        stockItemId: item.id,
        name: item.name,
        unitLabel: unitLabel(item.unit),
        categoryId: item.categoryId,
        categoryName: item.categoryName || `Category #${item.categoryId}`,
        companyId: item.companyId,
        description: "—",
        isActive: true,
        statusLabel: "Active",
      })),
    [items],
  );

  const scopedRows = useMemo(() => {
    const filtered = filterBySelectedCompany(rows, scopeCompanyId, (r) => r.companyId);
    if (!hasSelectedId) return filtered;
    return [...filtered].sort((a, b) =>
      a.stockItemId === selectedId ? -1 : b.stockItemId === selectedId ? 1 : 0,
    );
  }, [rows, scopeCompanyId, hasSelectedId, selectedId]);

  const filterDefs = useMemo<TableFilterDef<StockItemRow>[]>(
    () => [
      {
        id: "stockItemId",
        label: "ID",
        ui: "number",
        match: (row, get) => {
          const q = get("stockItemId").trim().toLowerCase();
          if (!q) return true;
          return String(row.stockItemId).toLowerCase().includes(q);
        },
      },
      {
        id: "name",
        label: "Name",
        ui: "text",
        match: (row, get) => {
          const q = get("name").trim().toLowerCase();
          if (!q) return true;
          return row.name.toLowerCase().includes(q);
        },
      },
      {
        id: "unit",
        label: "Unit",
        ui: "select",
        options: unitFilterOptions,
        match: (row, get) => {
          const v = get("unit");
          if (!v) return true;
          return row.unitLabel === unitLabel(Number(v) as UnitOfMeasureValue);
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
          return row.categoryId === Number(v);
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
    [sortedCategoryOptions, unitFilterOptions],
  );

  const columns = [
    {
      key: "stockItemId" as const,
      label: "ID",
      render: (value: number, row: StockItemRow) => (
        <span className={row.stockItemId === selectedId ? "font-semibold text-primary" : ""}>
          {value}
        </span>
      ),
    },
    { key: "name" as const, label: "Stock Item Name" },
    { key: "categoryName" as const, label: "Stock Category" },
    { key: "unitLabel" as const, label: "Unit" },
    { key: "description" as const, label: "Description" },
    {
      key: "statusLabel" as const,
      label: "Status",
      render: (_: string, row: StockItemRow) => (
        <Badge className="bg-emerald-100 text-emerald-800 hover:bg-emerald-100">{row.statusLabel}</Badge>
      ),
    },
  ];

  const resetForm = () => {
    setName("");
    setBarcode("");
    setType(String(StockItemType.RawMaterial));
    setUnit(String(UnitOfMeasure.Piece));
    setCategoryId("");
    setRestaurantId("");
    setCompanyId(String(scopeCompanyId ?? companies[0]?.id ?? resolveCompanyId()));
    setEditingId(null);
    setFieldErrors({});
    setIsEditMode(false);
  };

  const handleAdd = () => {
    resetForm();
    setIsDialogOpen(true);
  };

  const handleEdit = async (row: StockItemRow) => {
    try {
      setError(null);
      const item = await getStockItemById(row.stockItemId);
      setEditingId(item.id);
      setName(item.name);
      setBarcode(item.barcode || "");
      setType(String(item.type));
      setUnit(String(item.unit));
      setCategoryId(String(item.categoryId));
      setCompanyId(String(item.companyId || resolveCompanyId()));
      setRestaurantId(item.restaurantId ? String(item.restaurantId) : "");
      setFieldErrors({});
      setIsEditMode(true);
      setIsDialogOpen(true);
    } catch (err) {
      const message = err instanceof Error ? err.message : "Failed to load stock item.";
      setError(message);
      window.alert(message);
    }
  };

  const handleDelete = async (row: StockItemRow) => {
    if (!window.confirm(`Delete stock item "${row.name}"?`)) return;
    try {
      setError(null);
      await deleteStockItem(row.stockItemId);
      await loadData(true);
    } catch (err) {
      const message = err instanceof Error ? err.message : "Failed to delete.";
      setError(message);
      window.alert(message);
    }
  };

  const handleSave = async () => {
    const trimmed = name.trim();
    const cat = Number(categoryId);
    const comp = Number(companyId);
    const t = Number(type) as StockItemTypeValue;
    const u = Number(unit) as UnitOfMeasureValue;
    if (!trimmed) {
      window.alert("Name is required.");
      return;
    }
    if (!Number.isFinite(cat) || cat <= 0) {
      window.alert("Category is required.");
      return;
    }
    if (!Number.isFinite(comp) || comp <= 0) {
      window.alert("Company is required.");
      return;
    }

    const payload = {
      name: trimmed,
      barcode: barcode.trim() || null,
      type: t,
      unit: u,
      categoryId: cat,
      companyId: comp,
      restaurantId: restaurantId ? Number(restaurantId) : null,
    };

    try {
      setIsSubmitting(true);
      setError(null);
      setFieldErrors({});
      if (isEditMode && editingId != null) {
        await updateStockItem(editingId, payload);
      } else {
        await createStockItem(payload);
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
    return <div className="p-6 text-sm text-muted-foreground">Loading stock items...</div>;
  }

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-3xl font-bold text-foreground">Stock Items</h1>
        <p className="text-muted-foreground mt-1">
          Master data for inventory items, stock categories, and units of measure.
        </p>
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
              title="Stock Item List"
              columns={columns}
              data={filtered}
              idSortKey="stockItemId"
              searchableFields={["name", "categoryName", "unitLabel", "description", "id"]}
              searchPlaceholder="Search stock items..."
              onAdd={handleAdd}
              onEdit={handleEdit}
              onDelete={handleDelete}
            />
          )}
        </AdvancedTableFilters>

        <DialogContent className="sm:max-w-lg max-h-[90vh] overflow-y-auto">
          <DialogHeader>
            <DialogTitle>{isEditMode ? "Edit Stock Item" : "Add Stock Item"}</DialogTitle>
            <DialogDescription>Define the item, unit, and category for stock tracking.</DialogDescription>
          </DialogHeader>

          <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
            <div className="sm:col-span-2">
              <label className="mb-2 block text-sm font-medium text-foreground">Name</label>
              <Input value={name} onChange={(e) => setName(e.target.value)} />
              {getFieldErrorMessage(fieldErrors, "name") && (
                <p className="mt-1 text-xs text-red-600">{getFieldErrorMessage(fieldErrors, "name")}</p>
              )}
            </div>
            <div className="sm:col-span-2">
              <label className="mb-2 block text-sm font-medium text-foreground">Barcode</label>
              <Input value={barcode} onChange={(e) => setBarcode(e.target.value)} placeholder="Optional" />
            </div>
            <div>
              <label className="mb-2 block text-sm font-medium text-foreground">Type</label>
              <select value={type} onChange={(e) => setType(e.target.value)} className={selectClass}>
                {TYPE_OPTIONS.map((opt) => (
                  <option key={opt} value={String(opt)}>
                    {stockItemTypeLabel(opt)}
                  </option>
                ))}
              </select>
            </div>
            <div>
              <label className="mb-2 block text-sm font-medium text-foreground">Unit</label>
              <select value={unit} onChange={(e) => setUnit(e.target.value)} className={selectClass}>
                {UNIT_OPTIONS.map((opt) => (
                  <option key={opt} value={String(opt)}>
                    {unitLabel(opt)}
                  </option>
                ))}
              </select>
            </div>
            <div className="sm:col-span-2">
              <label className="mb-2 block text-sm font-medium text-foreground">Category</label>
              <select value={categoryId} onChange={(e) => setCategoryId(e.target.value)} className={selectClass}>
                <option value="">Select category</option>
                {categoriesScoped.map((c) => (
                  <option key={c.id} value={String(c.id)}>
                    {c.name}
                    {!c.isActive ? " (inactive)" : ""}
                  </option>
                ))}
              </select>
              {getFieldErrorMessage(fieldErrors, "categoryid") && (
                <p className="mt-1 text-xs text-red-600">{getFieldErrorMessage(fieldErrors, "categoryid")}</p>
              )}
            </div>
            <div className="sm:col-span-2">
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
