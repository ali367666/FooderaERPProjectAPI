"use client";

import { useEffect, useMemo, useState } from "react";
import { AdvancedTableFilters, type TableFilterDef } from "@/components/advanced-table-filters";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { DataTable } from "@/components/data-table";
import { Input } from "@/components/ui/input";
import { Checkbox } from "@/components/ui/checkbox";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { Plus, Trash2, X } from "lucide-react";
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
  getMenuItemSetComponents,
  getMenuItemStockInfo,
  PreparationType,
  UnitOfMeasure,
  preparationTypeLabel,
  unitLabel,
  updateMenuItem,
  type MenuItem,
  type MenuItemStockInfo,
  type PreparationTypeValue,
  type SetComponentInput,
  type UnitOfMeasureValue,
} from "@/lib/services/menu-item-service";
import { getStockItems, type StockItem } from "@/lib/services/stock-item-service";
import {
  createMenuItemType,
  deleteMenuItemType,
  getMenuItemTypes,
  updateMenuItemType,
  type MenuItemTypeOption,
} from "@/lib/services/menu-item-type-service";
import {
  createMenuCategory,
  getMenuCategories,
  type MenuCategory,
} from "@/lib/services/menu-category-service";
import { getRestaurants } from "@/lib/services/restaurant-service";
import { getPrinters, type Printer } from "@/lib/services/printer-service";
import { BarcodeSvg } from "@/components/barcode-svg";
import { ApiFormError, getFieldErrorMessage, type FieldErrors } from "@/lib/api-error";
import { usePermissionSet, useHasPermission } from "@/hooks/use-auth-permissions";
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
  itemTypeLabel: string;
  isSet: boolean;
  isActive: boolean;
  statusLabel: string;
};

const selectClass =
  "flex h-10 w-full rounded-md border border-input bg-background px-3 py-2 text-sm ring-offset-background";

function formatPrice(value: number): string {
  if (!Number.isFinite(value)) return "-";
  return value.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 });
}

function toNumberOrNull(value: string): number | null {
  const trimmed = value.trim();
  if (!trimmed) return null;
  const n = Number(trimmed);
  return Number.isFinite(n) ? n : null;
}

export default function MenuItemsPage() {
  const permissions = usePermissionSet();
  const canCreate = permissions.has(AppPermissions.MenuItemCreate);
  const canUpdate = permissions.has(AppPermissions.MenuItemUpdate);
  const canDelete = permissions.has(AppPermissions.MenuItemDelete);
  const canOverridePrice = useHasPermission("Pos.OverridePrice");

  const [items, setItems] = useState<MenuItem[]>([]);
  const [categories, setCategories] = useState<MenuCategory[]>([]);
  const [stockItems, setStockItems] = useState<StockItem[]>([]);
  const [printerOptions, setPrinterOptions] = useState<(Printer & { restaurantName: string })[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const [stockItemId, setStockItemId] = useState("");
  const [stockInfo, setStockInfo] = useState<MenuItemStockInfo | null>(null);
  const [stockInfoLoading, setStockInfoLoading] = useState(false);

  const [itemTypes, setItemTypes] = useState<MenuItemTypeOption[]>([]);
  const [itemTypesLoading, setItemTypesLoading] = useState(true);
  const [newItemTypeName, setNewItemTypeName] = useState("");
  const [savingItemType, setSavingItemType] = useState(false);
  const [editingItemTypeId, setEditingItemTypeId] = useState<number | null>(null);
  const [editingItemTypeName, setEditingItemTypeName] = useState("");
  const [isDialogOpen, setIsDialogOpen] = useState(false);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [isEditMode, setIsEditMode] = useState(false);
  const [editingId, setEditingId] = useState<number | null>(null);
  const [fieldErrors, setFieldErrors] = useState<FieldErrors>({});

  // Ümumi
  const [name, setName] = useState("");
  const [description, setDescription] = useState("");
  const [menuCategoryId, setMenuCategoryId] = useState("");
  const [preparationType, setPreparationType] = useState<string>(String(PreparationType.Kitchen));
  const [itemTypeId, setItemTypeId] = useState("");
  const [unitId, setUnitId] = useState<string>(String(UnitOfMeasure.Piece));
  const [vatPercent, setVatPercent] = useState("");
  const [barcode, setBarcode] = useState("");
  const [weightCode, setWeightCode] = useState<string | null>(null);
  const [resetWeightCode, setResetWeightCode] = useState(false);
  const [portion, setPortion] = useState("");
  const [isActive, setIsActive] = useState(true);

  // Qiymətlər
  const [price, setPrice] = useState("");
  const [stationPrice, setStationPrice] = useState("");
  const [purchasePrice, setPurchasePrice] = useState("");
  const [packagePrice, setPackagePrice] = useState("");
  const [specialPrice1, setSpecialPrice1] = useState("");
  const [specialPrice2, setSpecialPrice2] = useState("");
  const [specialPrice3, setSpecialPrice3] = useState("");
  const [specialPrice4, setSpecialPrice4] = useState("");
  const [specialPrice5, setSpecialPrice5] = useState("");

  // Davranış
  const [hideFromPosSearch, setHideFromPosSearch] = useState(false);
  const [hideBarcode, setHideBarcode] = useState(false);
  const [excludeFromDiscount, setExcludeFromDiscount] = useState(false);
  const [skipTaxCalculation, setSkipTaxCalculation] = useState(false);
  const [isTimeBased, setIsTimeBased] = useState(false);
  const [allowQuantityPromptOverride, setAllowQuantityPromptOverride] = useState(false);
  const [printerId, setPrinterId] = useState("");

  // SET (bundle)
  const [isSet, setIsSet] = useState(false);
  const [setComponents, setSetComponents] = useState<SetComponentInput[]>([]);
  const [setComponentNames, setSetComponentNames] = useState<Record<number, string>>({});
  const [newComponentId, setNewComponentId] = useState("");
  const [newComponentQty, setNewComponentQty] = useState("1");

  // Inline category creation
  const [isCategoryDialogOpen, setIsCategoryDialogOpen] = useState(false);
  const [newCategoryName, setNewCategoryName] = useState("");
  const [newCategoryParentId, setNewCategoryParentId] = useState("");
  const [isCategorySubmitting, setIsCategorySubmitting] = useState(false);

  const loadData = async (silent = false) => {
    try {
      if (!silent) setLoading(true);
      setError(null);
      const [itemData, categoryData, stockItemData] = await Promise.all([
        getMenuItems(),
        getMenuCategories(),
        getStockItems().catch(() => []),
      ]);
      setItems(itemData);
      setCategories(categoryData);
      setStockItems(stockItemData);

      try {
        const restaurants = await getRestaurants();
        const perRestaurant = await Promise.all(
          restaurants.map((r) =>
            getPrinters(r.id)
              .then((list) => list.map((p) => ({ ...p, restaurantName: r.name })))
              .catch(() => []),
          ),
        );
        setPrinterOptions(perRestaurant.flat());
      } catch {
        setPrinterOptions([]);
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to load menu items.");
    } finally {
      if (!silent) setLoading(false);
    }
  };

  const loadItemTypes = async () => {
    setItemTypesLoading(true);
    try {
      const data = await getMenuItemTypes();
      setItemTypes(data);
    } catch (err) {
      setItemTypes([]);
    } finally {
      setItemTypesLoading(false);
    }
  };

  useEffect(() => {
    void loadData();
    void loadItemTypes();
  }, []);

  const handleAddItemType = async () => {
    const trimmed = newItemTypeName.trim();
    if (!trimmed) return;
    setSavingItemType(true);
    try {
      await createMenuItemType({ name: trimmed, isActive: true });
      setNewItemTypeName("");
      await loadItemTypes();
    } catch (err) {
      window.alert(err instanceof Error ? err.message : "Növ əlavə olunmadı.");
    } finally {
      setSavingItemType(false);
    }
  };

  const startEditItemType = (t: MenuItemTypeOption) => {
    setEditingItemTypeId(t.id);
    setEditingItemTypeName(t.name);
  };

  const handleSaveItemTypeEdit = async (id: number) => {
    const trimmed = editingItemTypeName.trim();
    if (!trimmed) return;
    setSavingItemType(true);
    try {
      await updateMenuItemType(id, { name: trimmed, isActive: true });
      setEditingItemTypeId(null);
      await loadItemTypes();
    } catch (err) {
      window.alert(err instanceof Error ? err.message : "Növ yenilənmədi.");
    } finally {
      setSavingItemType(false);
    }
  };

  const handleDeleteItemType = async (t: MenuItemTypeOption) => {
    if (!window.confirm(`"${t.name}" növünü silmək istəyirsiniz?`)) return;
    try {
      await deleteMenuItemType(t.id);
      await loadItemTypes();
    } catch (err) {
      window.alert(err instanceof Error ? err.message : "Növ silinmədi.");
    }
  };

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
        itemTypeLabel: item.itemTypeName || `#${item.itemTypeId}`,
        isSet: item.isSet,
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
    { key: "itemTypeLabel" as const, label: "Type" },
    { key: "priceDisplay" as const, label: "Price" },
    { key: "portion" as const, label: "Portion" },
    { key: "preparationLabel" as const, label: "Preparation" },
    {
      key: "isSet" as const,
      label: "SET",
      render: (value: boolean) =>
        value ? <Badge className="bg-blue-100 text-blue-800 hover:bg-blue-100">SET</Badge> : "-",
    },
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
    setMenuCategoryId("");
    setPreparationType(String(PreparationType.Kitchen));
    setItemTypeId("");
    setUnitId(String(UnitOfMeasure.Piece));
    setVatPercent("");
    setBarcode("");
    setWeightCode(null);
    setResetWeightCode(false);
    setPortion("");
    setIsActive(true);

    setPrice("");
    setStationPrice("");
    setPurchasePrice("");
    setPackagePrice("");
    setSpecialPrice1("");
    setSpecialPrice2("");
    setSpecialPrice3("");
    setSpecialPrice4("");
    setSpecialPrice5("");

    setHideFromPosSearch(false);
    setHideBarcode(false);
    setExcludeFromDiscount(false);
    setSkipTaxCalculation(false);
    setIsTimeBased(false);
    setAllowQuantityPromptOverride(false);
    setPrinterId("");

    setIsSet(false);
    setSetComponents([]);
    setSetComponentNames({});
    setNewComponentId("");
    setNewComponentQty("1");

    setStockItemId("");
    setStockInfo(null);

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
      setMenuCategoryId(String(item.menuCategoryId || ""));
      setPreparationType(String(item.preparationType ?? PreparationType.Kitchen));
      setItemTypeId(item.itemTypeId ? String(item.itemTypeId) : "");
      setUnitId(String(item.unitId));
      setVatPercent(item.vatPercent != null ? String(item.vatPercent) : "");
      setBarcode(item.barcode || "");
      setWeightCode(item.weightCode);
      setResetWeightCode(false);
      setPortion(item.portion || "");
      setIsActive(item.isActive);

      setPrice(String(item.price ?? ""));
      setStationPrice(item.stationPrice != null ? String(item.stationPrice) : "");
      setPurchasePrice(item.purchasePrice != null ? String(item.purchasePrice) : "");
      setPackagePrice(item.packagePrice != null ? String(item.packagePrice) : "");
      setSpecialPrice1(item.specialPrice1 != null ? String(item.specialPrice1) : "");
      setSpecialPrice2(item.specialPrice2 != null ? String(item.specialPrice2) : "");
      setSpecialPrice3(item.specialPrice3 != null ? String(item.specialPrice3) : "");
      setSpecialPrice4(item.specialPrice4 != null ? String(item.specialPrice4) : "");
      setSpecialPrice5(item.specialPrice5 != null ? String(item.specialPrice5) : "");

      setHideFromPosSearch(item.hideFromPosSearch);
      setHideBarcode(item.hideBarcode);
      setExcludeFromDiscount(item.excludeFromDiscount);
      setSkipTaxCalculation(item.skipTaxCalculation);
      setIsTimeBased(item.isTimeBased);
      setAllowQuantityPromptOverride(item.allowQuantityPromptOverride);
      setPrinterId(item.printerId ? String(item.printerId) : "");

      setIsSet(item.isSet);
      if (item.isSet) {
        const components = await getMenuItemSetComponents(item.id);
        setSetComponents(components.map((c) => ({ componentMenuItemId: c.componentMenuItemId, quantity: c.quantity })));
        setSetComponentNames(
          Object.fromEntries(components.map((c) => [c.componentMenuItemId, c.componentMenuItemName])),
        );
      } else {
        setSetComponents([]);
        setSetComponentNames({});
      }
      setNewComponentId("");
      setNewComponentQty("1");

      setStockItemId(item.stockItemId ? String(item.stockItemId) : "");
      setStockInfo(null);
      setStockInfoLoading(true);
      getMenuItemStockInfo(item.id)
        .then(setStockInfo)
        .catch(() => setStockInfo(null))
        .finally(() => setStockInfoLoading(false));

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

  const handleAddSetComponent = () => {
    const componentId = Number(newComponentId);
    const qty = Number(newComponentQty);
    if (!Number.isFinite(componentId) || componentId <= 0) return;
    if (!Number.isFinite(qty) || qty <= 0) return;
    if (editingId != null && componentId === editingId) return;
    if (setComponents.some((c) => c.componentMenuItemId === componentId)) return;

    const component = items.find((i) => i.id === componentId);
    setSetComponents((prev) => [...prev, { componentMenuItemId: componentId, quantity: qty }]);
    setSetComponentNames((prev) => ({ ...prev, [componentId]: component?.name ?? `#${componentId}` }));
    setNewComponentId("");
    setNewComponentQty("1");
  };

  const handleRemoveSetComponent = (componentMenuItemId: number) => {
    setSetComponents((prev) => prev.filter((c) => c.componentMenuItemId !== componentMenuItemId));
  };

  const handleCreateCategory = async () => {
    const trimmed = newCategoryName.trim();
    if (!trimmed) return;
    try {
      setIsCategorySubmitting(true);
      await createMenuCategory({
        name: trimmed,
        parentCategoryId: newCategoryParentId ? Number(newCategoryParentId) : null,
      });
      const refreshed = await getMenuCategories();
      setCategories(refreshed);
      const created = refreshed.find((c) => c.name === trimmed);
      if (created) setMenuCategoryId(String(created.id));
      setNewCategoryName("");
      setNewCategoryParentId("");
      setIsCategoryDialogOpen(false);
    } catch (err) {
      const message = err instanceof Error ? err.message : "Failed to create category.";
      window.alert(message);
    } finally {
      setIsCategorySubmitting(false);
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
    const itemTypeIdNum = Number(itemTypeId);
    if (!Number.isFinite(itemTypeIdNum) || itemTypeIdNum <= 0) {
      const msg = "Item type is required.";
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

    const unit = Number(unitId) as UnitOfMeasureValue;

    const basePayload = {
      name: trimmedName,
      description: description.trim() || null,
      price: priceNum,
      portion: portion.trim() || null,
      menuCategoryId: categoryId,
      preparationType: prep,

      itemTypeId: itemTypeIdNum,
      unitId: unit,
      vatPercent: toNumberOrNull(vatPercent),
      barcode: barcode.trim() || null,

      stationPrice: toNumberOrNull(stationPrice),
      purchasePrice: toNumberOrNull(purchasePrice),
      packagePrice: toNumberOrNull(packagePrice),
      specialPrice1: toNumberOrNull(specialPrice1),
      specialPrice2: toNumberOrNull(specialPrice2),
      specialPrice3: toNumberOrNull(specialPrice3),
      specialPrice4: toNumberOrNull(specialPrice4),
      specialPrice5: toNumberOrNull(specialPrice5),

      hideFromPosSearch,
      hideBarcode,
      excludeFromDiscount,
      skipTaxCalculation,
      isTimeBased,
      allowQuantityPromptOverride,
      printerId: printerId ? Number(printerId) : null,

      isSet,

      stockItemId: stockItemId ? Number(stockItemId) : null,
    };

    try {
      setIsSubmitting(true);
      setError(null);
      setFieldErrors({});

      if (isEditMode && editingId != null) {
        await updateMenuItem(editingId, {
          ...basePayload,
          isActive,
          resetWeightCode,
          setComponents: isSet ? setComponents : [],
        });
      } else {
        await createMenuItem(basePayload);
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

      <div className="rounded-xl border bg-card p-4">
        <h2 className="mb-1 text-lg font-semibold">Məhsul növləri</h2>
        <p className="mb-3 text-sm text-muted-foreground">
          Öz növlərinizi yaradın (məs. Məhsul, Xammal, Yarımfabrikat) — məhsul yaradarkən bu siyahıdan seçəcəksiniz.
        </p>

        <div className="mb-3 flex flex-wrap gap-2">
          {itemTypesLoading && <p className="text-sm text-muted-foreground">Yüklənir…</p>}
          {!itemTypesLoading && itemTypes.length === 0 && (
            <p className="text-sm text-muted-foreground">Hələ növ yoxdur.</p>
          )}
          {itemTypes.map((t) =>
            editingItemTypeId === t.id ? (
              <div key={t.id} className="flex items-center gap-1 rounded-full border bg-background px-2 py-1">
                <Input
                  autoFocus
                  className="h-7 w-32 text-sm"
                  value={editingItemTypeName}
                  onChange={(e) => setEditingItemTypeName(e.target.value)}
                  onKeyDown={(e) => e.key === "Enter" && void handleSaveItemTypeEdit(t.id)}
                />
                <Button size="sm" className="h-7 px-2" disabled={savingItemType} onClick={() => void handleSaveItemTypeEdit(t.id)}>
                  OK
                </Button>
              </div>
            ) : (
              <span key={t.id} className="flex items-center gap-2 rounded-full border bg-muted px-3 py-1.5 text-sm">
                <button type="button" onClick={() => startEditItemType(t)} className="hover:underline">
                  {t.name}
                </button>
                <button
                  type="button"
                  onClick={() => void handleDeleteItemType(t)}
                  className="text-muted-foreground hover:text-destructive"
                >
                  <Trash2 className="h-3.5 w-3.5" />
                </button>
              </span>
            ),
          )}
        </div>

        <div className="flex max-w-sm items-center gap-2">
          <Input
            value={newItemTypeName}
            onChange={(e) => setNewItemTypeName(e.target.value)}
            placeholder="Yeni növ adı"
            onKeyDown={(e) => e.key === "Enter" && void handleAddItemType()}
          />
          <Button size="sm" disabled={savingItemType} onClick={() => void handleAddItemType()}>
            <Plus className="mr-1 h-4 w-4" />
            Əlavə et
          </Button>
        </div>
      </div>

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

        <DialogContent className="sm:max-w-2xl max-h-[85vh] overflow-y-auto">
          <DialogHeader>
            <DialogTitle>{isEditMode ? "Edit Menu Item" : "Add Menu Item"}</DialogTitle>
            <DialogDescription>
              Set the item&apos;s general info, prices, and POS behaviour.
            </DialogDescription>
          </DialogHeader>

          <Tabs defaultValue="general" className="w-full">
            <TabsList>
              <TabsTrigger value="general">Ümumi</TabsTrigger>
              <TabsTrigger value="prices">Qiymətlər</TabsTrigger>
              <TabsTrigger value="behavior">Davranış</TabsTrigger>
              <TabsTrigger value="set">SET</TabsTrigger>
              <TabsTrigger value="stock">Cari stok</TabsTrigger>
            </TabsList>

            <TabsContent value="general" className="space-y-4 pt-2">
              <div>
                <label className="mb-2 block text-sm font-medium text-foreground">Name</label>
                <Input value={name} onChange={(e) => setName(e.target.value)} placeholder="Item name" />
                {getFieldErrorMessage(fieldErrors, "name") && (
                  <p className="mt-1 text-xs text-red-600">{getFieldErrorMessage(fieldErrors, "name")}</p>
                )}
              </div>

              <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
                <div>
                  <label className="mb-2 flex items-center justify-between text-sm font-medium text-foreground">
                    Category
                    <button
                      type="button"
                      onClick={() => setIsCategoryDialogOpen(true)}
                      className="flex items-center gap-1 text-xs font-medium text-primary hover:underline"
                    >
                      <Plus className="h-3 w-3" /> New
                    </button>
                  </label>
                  <select
                    value={menuCategoryId}
                    onChange={(e) => setMenuCategoryId(e.target.value)}
                    className={selectClass}
                  >
                    <option value="">Select category</option>
                    {categories.map((c) => (
                      <option key={c.id} value={String(c.id)}>
                        {c.parentCategoryName ? `${c.parentCategoryName} / ${c.name}` : c.name}
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
                  <label className="mb-2 block text-sm font-medium text-foreground">Item Type</label>
                  <select
                    value={itemTypeId}
                    onChange={(e) => setItemTypeId(e.target.value)}
                    className={selectClass}
                  >
                    <option value="">Select type</option>
                    {itemTypes.map((t) => (
                      <option key={t.id} value={String(t.id)}>
                        {t.name}
                      </option>
                    ))}
                  </select>
                  {itemTypes.length === 0 && (
                    <p className="mt-1 text-xs text-muted-foreground">
                      Əvvəlcə yuxarıdakı "Məhsul növləri" bölümündən bir növ yaradın.
                    </p>
                  )}
                </div>

                <div>
                  <label className="mb-2 block text-sm font-medium text-foreground">Unit</label>
                  <select value={unitId} onChange={(e) => setUnitId(e.target.value)} className={selectClass}>
                    <option value={String(UnitOfMeasure.Piece)}>{unitLabel(UnitOfMeasure.Piece)}</option>
                    <option value={String(UnitOfMeasure.Kg)}>{unitLabel(UnitOfMeasure.Kg)}</option>
                    <option value={String(UnitOfMeasure.Gram)}>{unitLabel(UnitOfMeasure.Gram)}</option>
                    <option value={String(UnitOfMeasure.Liter)}>{unitLabel(UnitOfMeasure.Liter)}</option>
                    <option value={String(UnitOfMeasure.Ml)}>{unitLabel(UnitOfMeasure.Ml)}</option>
                  </select>
                </div>

                <div>
                  <label className="mb-2 block text-sm font-medium text-foreground">VAT (%)</label>
                  <Input
                    type="number"
                    min={0}
                    step="0.01"
                    value={vatPercent}
                    onChange={(e) => setVatPercent(e.target.value)}
                    placeholder="Optional"
                  />
                </div>

                <div>
                  <label className="mb-2 block text-sm font-medium text-foreground">Barcode</label>
                  <Input value={barcode} onChange={(e) => setBarcode(e.target.value)} placeholder="Optional" />
                </div>

                <div>
                  <label className="mb-2 block text-sm font-medium text-foreground">Portion</label>
                  <Input value={portion} onChange={(e) => setPortion(e.target.value)} placeholder="e.g. 250g" />
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
                </div>

                {isEditMode && (
                  <div>
                    <label className="mb-2 block text-sm font-medium text-foreground">Weight Code</label>
                    <div className="flex items-center gap-2">
                      <Input value={weightCode ?? ""} disabled className="bg-muted" />
                      <Button
                        type="button"
                        variant="outline"
                        size="sm"
                        onClick={() => setResetWeightCode(true)}
                        disabled={resetWeightCode}
                      >
                        {resetWeightCode ? "Yenilənəcək" : "Sıfırla"}
                      </Button>
                    </div>
                  </div>
                )}

                {isEditMode && (barcode.trim() || weightCode) && (
                  <div className="sm:col-span-2">
                    <label className="mb-2 block text-sm font-medium text-foreground">Barkod</label>
                    <div className="flex items-center gap-3 rounded-md border p-3">
                      <BarcodeSvg value={barcode.trim() || weightCode || ""} height={45} />
                      <Button type="button" variant="outline" size="sm" onClick={() => window.print()}>
                        Etiketi çap et
                      </Button>
                    </div>
                  </div>
                )}

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
              </div>

              <div>
                <label className="mb-2 block text-sm font-medium text-foreground">Description</label>
                <Input
                  value={description}
                  onChange={(e) => setDescription(e.target.value)}
                  placeholder="Optional"
                />
              </div>
            </TabsContent>

            <TabsContent value="prices" className="space-y-4 pt-2">
              {isEditMode && !canOverridePrice && (
                <p className="text-xs text-muted-foreground">
                  Qalıcı qiymətə müdaxilə icazəniz olmadığı üçün bu sahələr yalnız oxuna bilər.
                </p>
              )}
              <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
                <div>
                  <label className="mb-2 block text-sm font-medium text-foreground">Price (satış)</label>
                  <Input
                    type="number"
                    min={0}
                    step="0.01"
                    value={price}
                    onChange={(e) => setPrice(e.target.value)}
                    disabled={isEditMode && !canOverridePrice}
                  />
                  {getFieldErrorMessage(fieldErrors, "price") && (
                    <p className="mt-1 text-xs text-red-600">{getFieldErrorMessage(fieldErrors, "price")}</p>
                  )}
                </div>
                <div>
                  <label className="mb-2 block text-sm font-medium text-foreground">Dəzgah qiyməti</label>
                  <Input
                    type="number"
                    min={0}
                    step="0.01"
                    value={stationPrice}
                    onChange={(e) => setStationPrice(e.target.value)}
                    placeholder="Optional"
                    disabled={isEditMode && !canOverridePrice}
                  />
                </div>
                <div>
                  <label className="mb-2 block text-sm font-medium text-foreground">Alış qiyməti</label>
                  <Input
                    type="number"
                    min={0}
                    step="0.0001"
                    value={purchasePrice}
                    onChange={(e) => setPurchasePrice(e.target.value)}
                    placeholder="Optional"
                    disabled={isEditMode && !canOverridePrice}
                  />
                </div>
                <div>
                  <label className="mb-2 block text-sm font-medium text-foreground">Paket qiyməti</label>
                  <Input
                    type="number"
                    min={0}
                    step="0.01"
                    value={packagePrice}
                    onChange={(e) => setPackagePrice(e.target.value)}
                    placeholder="Optional"
                    disabled={isEditMode && !canOverridePrice}
                  />
                </div>
                <div>
                  <label className="mb-2 block text-sm font-medium text-foreground">Özəl qiymət 1</label>
                  <Input type="number" min={0} step="0.01" value={specialPrice1} onChange={(e) => setSpecialPrice1(e.target.value)} placeholder="Optional" disabled={isEditMode && !canOverridePrice} />
                </div>
                <div>
                  <label className="mb-2 block text-sm font-medium text-foreground">Özəl qiymət 2</label>
                  <Input type="number" min={0} step="0.01" value={specialPrice2} onChange={(e) => setSpecialPrice2(e.target.value)} placeholder="Optional" disabled={isEditMode && !canOverridePrice} />
                </div>
                <div>
                  <label className="mb-2 block text-sm font-medium text-foreground">Özəl qiymət 3</label>
                  <Input type="number" min={0} step="0.01" value={specialPrice3} onChange={(e) => setSpecialPrice3(e.target.value)} placeholder="Optional" disabled={isEditMode && !canOverridePrice} />
                </div>
                <div>
                  <label className="mb-2 block text-sm font-medium text-foreground">Özəl qiymət 4</label>
                  <Input type="number" min={0} step="0.01" value={specialPrice4} onChange={(e) => setSpecialPrice4(e.target.value)} placeholder="Optional" disabled={isEditMode && !canOverridePrice} />
                </div>
                <div>
                  <label className="mb-2 block text-sm font-medium text-foreground">Özəl qiymət 5</label>
                  <Input type="number" min={0} step="0.01" value={specialPrice5} onChange={(e) => setSpecialPrice5(e.target.value)} placeholder="Optional" disabled={isEditMode && !canOverridePrice} />
                </div>
              </div>
            </TabsContent>

            <TabsContent value="behavior" className="space-y-3 pt-2">
              <label className="flex items-center gap-2 text-sm text-foreground">
                <Checkbox checked={hideFromPosSearch} onCheckedChange={(v) => setHideFromPosSearch(v === true)} />
                POS axtarışında gizlət
              </label>
              <label className="flex items-center gap-2 text-sm text-foreground">
                <Checkbox checked={hideBarcode} onCheckedChange={(v) => setHideBarcode(v === true)} />
                Barkodu gizlət
              </label>
              <label className="flex items-center gap-2 text-sm text-foreground">
                <Checkbox checked={excludeFromDiscount} onCheckedChange={(v) => setExcludeFromDiscount(v === true)} />
                Endirimdən istisna et
              </label>
              <label className="flex items-center gap-2 text-sm text-foreground">
                <Checkbox checked={skipTaxCalculation} onCheckedChange={(v) => setSkipTaxCalculation(v === true)} />
                Vergi hesablamasını keç
              </label>
              <label className="flex items-center gap-2 text-sm text-foreground">
                <Checkbox checked={isTimeBased} onCheckedChange={(v) => setIsTimeBased(v === true)} />
                Zaman əsaslı məhsul
              </label>
              <label className="flex items-center gap-2 text-sm text-foreground">
                <Checkbox
                  checked={allowQuantityPromptOverride}
                  onCheckedChange={(v) => setAllowQuantityPromptOverride(v === true)}
                />
                Satışda miqdar soruşulsun
              </label>

              <div className="pt-2">
                <label className="mb-2 block text-sm font-medium text-foreground">
                  Printer (hardan çıxarılsın)
                </label>
                <select value={printerId} onChange={(e) => setPrinterId(e.target.value)} className={selectClass}>
                  <option value="">Printer seçilməyib</option>
                  {printerOptions.map((p) => (
                    <option key={p.id} value={String(p.id)}>
                      {printerOptions.some((x) => x.id !== p.id && x.restaurantName !== p.restaurantName)
                        ? `${p.name} (${p.restaurantName})`
                        : p.name}
                    </option>
                  ))}
                </select>
                {printerOptions.length === 0 && (
                  <p className="mt-1 text-xs text-muted-foreground">
                    Hələ printer yoxdur — əvvəlcə "Printerlər" bölümündən əlavə edin.
                  </p>
                )}
              </div>
            </TabsContent>

            <TabsContent value="set" className="space-y-4 pt-2">
              <label className="flex items-center gap-2 text-sm text-foreground">
                <Checkbox checked={isSet} onCheckedChange={(v) => setIsSet(v === true)} />
                Bu, SET (paket) məhsuldur
              </label>

              {isSet && !isEditMode && (
                <p className="text-xs text-muted-foreground">
                  Əvvəlcə məhsulu yaradın, sonra redaktə edərək tərkib məhsulları əlavə edin.
                </p>
              )}

              {isSet && isEditMode && (
                <div className="space-y-3">
                  <div className="flex items-end gap-2">
                    <div className="flex-1">
                      <label className="mb-2 block text-sm font-medium text-foreground">Tərkib məhsulu</label>
                      <select
                        value={newComponentId}
                        onChange={(e) => setNewComponentId(e.target.value)}
                        className={selectClass}
                      >
                        <option value="">Seçin</option>
                        {items
                          .filter((i) => i.id !== editingId && !i.isSet)
                          .map((i) => (
                            <option key={i.id} value={String(i.id)}>
                              {i.name}
                            </option>
                          ))}
                      </select>
                    </div>
                    <div className="w-24">
                      <label className="mb-2 block text-sm font-medium text-foreground">Say</label>
                      <Input
                        type="number"
                        min={1}
                        value={newComponentQty}
                        onChange={(e) => setNewComponentQty(e.target.value)}
                      />
                    </div>
                    <Button type="button" variant="outline" onClick={handleAddSetComponent}>
                      Əlavə et
                    </Button>
                  </div>

                  {setComponents.length === 0 ? (
                    <p className="text-sm text-muted-foreground">Hələ tərkib məhsulu əlavə edilməyib.</p>
                  ) : (
                    <div className="space-y-2">
                      {setComponents.map((c) => (
                        <div
                          key={c.componentMenuItemId}
                          className="flex items-center justify-between rounded-md border px-3 py-2 text-sm"
                        >
                          <span>
                            {setComponentNames[c.componentMenuItemId] ?? `#${c.componentMenuItemId}`} × {c.quantity}
                          </span>
                          <button
                            type="button"
                            onClick={() => handleRemoveSetComponent(c.componentMenuItemId)}
                            className="text-muted-foreground hover:text-destructive"
                          >
                            <X className="h-4 w-4" />
                          </button>
                        </div>
                      ))}
                    </div>
                  )}
                </div>
              )}
            </TabsContent>

            <TabsContent value="stock" className="space-y-4 pt-2">
              <div>
                <label className="mb-2 block text-sm font-medium text-foreground">Bağlı anbar məhsulu</label>
                <select
                  value={stockItemId}
                  onChange={(e) => setStockItemId(e.target.value)}
                  className={selectClass}
                >
                  <option value="">Yoxdur (bağlama)</option>
                  {stockItems.map((s) => (
                    <option key={s.id} value={String(s.id)}>
                      {s.name}
                    </option>
                  ))}
                </select>
                <p className="mt-1 text-xs text-muted-foreground">
                  Xammal kimi işlədilən məhsullar üçün birbaşa anbar qalığını göstərmək üçün seçin.
                </p>
              </div>

              {!isEditMode && (
                <p className="text-xs text-muted-foreground">
                  Əvvəlcə məhsulu yaradın, sonra redaktə edərək cari stok məlumatını görün.
                </p>
              )}

              {isEditMode && (
                <div className="space-y-4">
                  {stockInfoLoading && <p className="text-sm text-muted-foreground">Yüklənir…</p>}

                  {!stockInfoLoading && stockInfo?.stockItemId != null && (
                    <div>
                      <h4 className="mb-2 text-sm font-medium text-foreground">
                        Birbaşa anbar qalığı ({stockInfo.stockItemName})
                      </h4>
                      {stockInfo.directBalances.length === 0 ? (
                        <p className="text-sm text-muted-foreground">Heç bir anbarda qeyd yoxdur.</p>
                      ) : (
                        <div className="space-y-1">
                          {stockInfo.directBalances.map((b) => (
                            <div key={b.warehouseId} className="flex items-center justify-between rounded-md border px-3 py-2 text-sm">
                              <span>{b.warehouseName}</span>
                              <span className="font-semibold">{b.quantity}</span>
                            </div>
                          ))}
                        </div>
                      )}
                    </div>
                  )}

                  {!stockInfoLoading && (stockInfo?.recipeMakeablePortions.length ?? 0) > 0 && (
                    <div>
                      <h4 className="mb-2 text-sm font-medium text-foreground">Reseptə görə hazırlana bilən porsiya</h4>
                      <div className="space-y-1">
                        {stockInfo!.recipeMakeablePortions.map((b) => (
                          <div key={b.warehouseId} className="flex items-center justify-between rounded-md border px-3 py-2 text-sm">
                            <span>{b.warehouseName}</span>
                            <span className="font-semibold">{b.quantity} porsiya</span>
                          </div>
                        ))}
                      </div>
                    </div>
                  )}

                  {!stockInfoLoading &&
                    stockInfo?.stockItemId == null &&
                    (stockInfo?.recipeMakeablePortions.length ?? 0) === 0 && (
                      <p className="text-sm text-muted-foreground">
                        Bu məhsul üçün nə birbaşa anbar əlaqəsi, nə də resept var — Menu Item Recipe bölümündən resept
                        əlavə edə bilərsiniz.
                      </p>
                    )}
                </div>
              )}
            </TabsContent>
          </Tabs>

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

      <Dialog
        open={isCategoryDialogOpen}
        onOpenChange={(open) => {
          setIsCategoryDialogOpen(open);
          if (!open) {
            setNewCategoryName("");
            setNewCategoryParentId("");
          }
        }}
      >
        <DialogContent className="sm:max-w-sm">
          <DialogHeader>
            <DialogTitle>New Category</DialogTitle>
            <DialogDescription>Quickly create a category without leaving this form.</DialogDescription>
          </DialogHeader>
          <div className="space-y-4">
            <div>
              <label className="mb-2 block text-sm font-medium text-foreground">Name</label>
              <Input
                value={newCategoryName}
                onChange={(e) => setNewCategoryName(e.target.value)}
                placeholder="Category name"
              />
            </div>
            <div>
              <label className="mb-2 block text-sm font-medium text-foreground">Parent Category</label>
              <select
                value={newCategoryParentId}
                onChange={(e) => setNewCategoryParentId(e.target.value)}
                className={selectClass}
              >
                <option value="">None (top-level category)</option>
                {categories.map((c) => (
                  <option key={c.id} value={String(c.id)}>
                    {c.name}
                  </option>
                ))}
              </select>
            </div>
          </div>
          <div className="mt-4 flex justify-end gap-3">
            <Button variant="outline" onClick={() => setIsCategoryDialogOpen(false)} disabled={isCategorySubmitting}>
              Cancel
            </Button>
            <Button onClick={handleCreateCategory} disabled={isCategorySubmitting || !newCategoryName.trim()}>
              {isCategorySubmitting ? "Saving..." : "Create"}
            </Button>
          </div>
        </DialogContent>
      </Dialog>

      <div id="menu-item-label-print" className="hidden flex-col items-center gap-1 p-4 text-center">
        <span className="text-sm font-semibold">{name}</span>
        <span className="text-xs">{formatPrice(Number(price) || 0)} ₼</span>
        <BarcodeSvg value={barcode.trim() || weightCode || ""} height={45} />
      </div>
      <style>{`
        @media print {
          body * { visibility: hidden; }
          #menu-item-label-print, #menu-item-label-print * { visibility: visible; }
          #menu-item-label-print {
            display: flex !important;
            position: fixed; top: 0; left: 0; width: 100%;
          }
        }
      `}</style>
    </div>
  );
}
