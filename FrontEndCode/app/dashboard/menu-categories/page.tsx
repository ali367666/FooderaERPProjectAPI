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
import { uploadFile } from "@/lib/services/file-service";
import { useRouter } from "next/navigation";
import { useCompanyStoreMode } from "@/hooks/use-company-store-mode";

type MenuCategoryRow = {
  id: string;
  categoryId: number;
  name: string;
  description: string;
  isActive: boolean;
  statusLabel: string;
  parentCategoryName: string;
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
  const [imageUrl, setImageUrl] = useState<string | null>(null);
  const [uploadingImage, setUploadingImage] = useState(false);
  const [isActive, setIsActive] = useState(true);
  const [isSetCategory, setIsSetCategory] = useState(false);
  const [parentCategoryId, setParentCategoryId] = useState<number | null>(null);
  const [editingId, setEditingId] = useState<number | null>(null);
  const [fieldErrors, setFieldErrors] = useState<FieldErrors>({});
  const isStoreMode = useCompanyStoreMode() === true;
  // A store keeps its products and categories in its own catalog (Mağaza → Məhsullar); this restaurant menu is not used there.
  const storeRouter = useRouter();
  useEffect(() => {
    if (isStoreMode) storeRouter.replace("/store/products");
  }, [isStoreMode, storeRouter]);
  // A store sees the same page in Azerbaijani, with product wording.
  const t = (en: string, az: string) => (isStoreMode ? az : en);

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
        parentCategoryName: c.parentCategoryName || "-",
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
        label: t("Category Name", "Kateqoriya adı"),
        ui: "text",
        match: (row, get) => {
          const q = get("name").trim().toLowerCase();
          if (!q) return true;
          return row.name.toLowerCase().includes(q);
        },
      },
      {
        id: "description",
        label: t("Description", "Təsvir"),
        ui: "text",
        match: (row, get) => {
          const q = get("description").trim().toLowerCase();
          if (!q) return true;
          return row.description.toLowerCase().includes(q);
        },
      },
      {
        id: "status",
        label: t("Status", "Status"),
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
    // eslint-disable-next-line react-hooks/exhaustive-deps
    [isStoreMode],
  );

  const columns = [
    { key: "categoryId" as const, label: "ID" },
    { key: "name" as const, label: t("Category Name", "Kateqoriya adı") },
    { key: "parentCategoryName" as const, label: t("Parent Category", "Ana kateqoriya") },
    { key: "description" as const, label: t("Description", "Təsvir") },
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
    setImageUrl(null);
    setIsActive(true);
    setIsSetCategory(false);
    setParentCategoryId(null);
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
      setImageUrl(c.imageUrl || null);
      setIsActive(c.isActive);
      setIsSetCategory(c.isSetCategory);
      setParentCategoryId(c.parentCategoryId);
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
      const message = err instanceof Error ? err.message : t("Failed to delete category.", "Kateqoriya silinmədi.");
      setError(message);
      window.alert(message);
    }
  };

  const handleSave = async () => {
    const trimmedName = name.trim();
    if (!trimmedName) {
      const msg = t("Category name is required.", "Kateqoriyanın adı yazılmalıdır.");
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
            imageUrl: imageUrl?.trim() || null,
            isActive,
            isSetCategory,
            parentCategoryId,
          },
          selectedCompanyId ?? undefined,
        );
      } else {
        await createMenuCategory({
          name: trimmedName,
          description: description.trim() || null,
          imageUrl: imageUrl?.trim() || null,
          isSetCategory,
          parentCategoryId,
        });
      }

      setIsDialogOpen(false);
      resetForm();
      await loadCategories(true);
    } catch (err) {
      if (err instanceof ApiFormError) setFieldErrors(err.fieldErrors);
      const message =
        err instanceof Error ? err.message : t("Save failed due to an unexpected error.", "Yazılmadı: gözlənilməyən xəta.");
      setError(message);
      window.alert(message);
    } finally {
      setIsSubmitting(false);
    }
  };

  if (loading) {
    return <div className="p-6 text-sm text-muted-foreground">{t("Loading menu categories...", "Kateqoriyalar yüklənir...")}</div>;
  }

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-3xl font-bold text-foreground">
          {isStoreMode ? "Məhsul Kateqoriyaları" : "Menu Categories"}
        </h1>
        <p className="text-muted-foreground mt-1">
          {t("Organize your menu into categories for each company.", "Məhsullarınızı kateqoriyalara bölün.")}
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
              title={t("Menu Category List", "Məhsul kateqoriyaları")}
              columns={columns}
              data={filtered}
              idSortKey="categoryId"
              searchableFields={["name", "description", "statusLabel", "id"]}
              searchPlaceholder={t("Search categories...", "Kateqoriya axtar...")}
              onAdd={handleAdd}
              onEdit={handleEdit}
              onDelete={handleDelete}
            />
          )}
        </AdvancedTableFilters>

        <DialogContent className="sm:max-w-md">
          <DialogHeader>
            <DialogTitle>{isEditMode ? t("Edit Menu Category", "Kateqoriyanı dəyiş") : t("Add Menu Category", "Yeni kateqoriya")}</DialogTitle>
            <DialogDescription>{t("Name and describe the category, then save.", "Kateqoriyanın adını və təsvirini yazıb yadda saxlayın.")}</DialogDescription>
          </DialogHeader>

          <div className="space-y-4">
            <div>
              <label className="mb-2 block text-sm font-medium text-foreground">{t("Name", "Ad")}</label>
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
              <label className="mb-2 block text-sm font-medium text-foreground">{t("Description", "Təsvir")}</label>
              <Input
                value={description}
                onChange={(e) => setDescription(e.target.value)}
                placeholder={t("Optional description", "İstəyə bağlı təsvir")}
              />
              {getFieldErrorMessage(fieldErrors, "description") && (
                <p className="mt-1 text-xs text-red-600">
                  {getFieldErrorMessage(fieldErrors, "description")}
                </p>
              )}
            </div>

            <div>
              <label className="mb-2 block text-sm font-medium text-foreground">Şəkil</label>
              <div className="space-y-2">
                {imageUrl && (
                  // eslint-disable-next-line @next/next/no-img-element
                  <img src={imageUrl} alt="" className="h-20 w-20 rounded border bg-muted object-cover" />
                )}
                <Input
                  type="file"
                  accept="image/png,image/jpeg,image/webp,image/svg+xml"
                  disabled={uploadingImage}
                  onChange={async (e) => {
                    const file = e.target.files?.[0];
                    if (!file) return;
                    setUploadingImage(true);
                    try {
                      const url = await uploadFile(file);
                      setImageUrl(url);
                    } catch (err) {
                      window.alert(err instanceof Error ? err.message : "Şəkil yüklənmədi");
                    } finally {
                      setUploadingImage(false);
                    }
                  }}
                />
                {uploadingImage && <p className="text-xs text-muted-foreground">Yüklənir...</p>}
              </div>
            </div>

            {isEditMode && (
              <div>
                <label className="mb-2 block text-sm font-medium text-foreground">Status</label>
                <select
                  value={isActive ? "true" : "false"}
                  onChange={(e) => setIsActive(e.target.value === "true")}
                  className={selectClass}
                >
                  <option value="true">{t("Active", "Aktiv")}</option>
                  <option value="false">{t("Inactive", "Passiv")}</option>
                </select>
              </div>
            )}

            <div>
              <label className="mb-2 block text-sm font-medium text-foreground">{t("Parent Category", "Ana kateqoriya")}</label>
              <select
                value={parentCategoryId != null ? String(parentCategoryId) : ""}
                onChange={(e) => setParentCategoryId(e.target.value ? Number(e.target.value) : null)}
                className={selectClass}
              >
                <option value="">{t("None (top-level category)", "Yoxdur (əsas kateqoriya)")}</option>
                {categories
                  .filter((c) => c.id !== editingId)
                  .map((c) => (
                    <option key={c.id} value={c.id}>
                      {c.name}
                    </option>
                  ))}
              </select>
            </div>

            {!isStoreMode && (
            <label className="flex cursor-pointer items-start gap-3 rounded-md border border-input p-3">
              <input
                type="checkbox"
                checked={isSetCategory}
                onChange={(e) => setIsSetCategory(e.target.checked)}
                className="mt-1 h-4 w-4"
              />
              <span>
                <span className="block text-sm font-medium text-foreground">Set kateqoriyası</span>
                <span className="block text-xs text-muted-foreground">
                  Bu kateqoriyadakı məhsullar set olur: ofisiant yemək və içkiləri özü seçir.
                </span>
              </span>
            </label>
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
                {t("Cancel", "Ləğv et")}
              </Button>
              <Button onClick={handleSave} disabled={isSubmitting}>
                {isSubmitting ? t("Saving...", "Yazılır...") : t("Save", "Yaz")}
              </Button>
            </div>
          </div>
        </DialogContent>
      </Dialog>
    </div>
  );
}
