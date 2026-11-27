"use client";

import { useCallback, useState } from "react";
import { PackagePlus } from "lucide-react";
import { toast } from "sonner";
import { StockItemDialog } from "@/components/stock/item-dialog";
import { Button } from "@/components/ui/button";
import { useIsSuperAdmin, usePermissionSet } from "@/hooks/use-auth-permissions";
import { getRestaurants, type Restaurant } from "@/lib/services/restaurant-service";
import { createStockCategory, getAllStockCategories, type StockCategory } from "@/lib/services/stock-category-service";
import { getStockItemById, type StockItem } from "@/lib/services/stock-item-service";

/**
 * "New product" inside a document (purchase, stock request): the product is created without leaving the document and handed back to it.
 * Shown to those who may create stock items, once the document has a company.
 */
export function NewStockItemButton({ companyId, onCreated }: { companyId: number; onCreated: (item: StockItem) => void }) {
  const isSuperAdmin = useIsSuperAdmin();
  const permissions = usePermissionSet();
  const can = (p: string) => isSuperAdmin || permissions.has(p);

  const [open, setOpen] = useState(false);
  const [categories, setCategories] = useState<StockCategory[]>([]);
  const [restaurants, setRestaurants] = useState<Restaurant[]>([]);

  const loadCategories = useCallback(async () => {
    const list = await getAllStockCategories({ companyId });
    setCategories(list);
    return list;
  }, [companyId]);

  if (!can("StockItem.Create") || !companyId) return null;

  const start = () => {
    setOpen(true);
    loadCategories().catch((e) => toast.error(e instanceof Error ? e.message : "Kateqoriyalar yüklənmədi."));
    getRestaurants()
      .then((list) => setRestaurants(list.filter((r) => r.companyId === companyId)))
      .catch(() => setRestaurants([]));
  };

  const addCategory = async (name: string) => {
    await createStockCategory({ name, isActive: true, companyId, description: null, parentId: null });
    const list = await loadCategories();
    return list.find((c) => c.name.trim().toLowerCase() === name.trim().toLowerCase())?.id ?? null;
  };

  const saved = async (id?: number) => {
    if (!id) return;
    try {
      onCreated(await getStockItemById(id));
    } catch (e) {
      toast.error(e instanceof Error ? e.message : "Məhsul yaradıldı, amma sətrə əlavə olunmadı. Səhifəni yeniləyin.");
    }
  };

  return (
    <>
      <Button type="button" variant="outline" size="sm" onClick={start}>
        <PackagePlus className="mr-1 h-4 w-4" />
        Yeni məhsul
      </Button>
      <StockItemDialog
        open={open}
        itemId={null}
        companyId={companyId}
        defaultCategoryId={null}
        categories={categories}
        restaurants={restaurants}
        canDelete={false}
        onClose={() => setOpen(false)}
        onSaved={(id) => void saved(id)}
        onAddCategory={can("StockCategory.Create") ? addCategory : undefined}
      />
    </>
  );
}
