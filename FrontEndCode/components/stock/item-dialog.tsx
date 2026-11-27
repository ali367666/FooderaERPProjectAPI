"use client";

import { useEffect, useRef, useState } from "react";
import { ChevronDown, ChevronRight } from "lucide-react";
import { toast } from "sonner";
import { Button } from "@/components/ui/button";
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from "@/components/ui/dialog";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import type { Restaurant } from "@/lib/services/restaurant-service";
import type { StockCategory } from "@/lib/services/stock-category-service";
import {
  StockItemType,
  UnitOfMeasure,
  createStockItem,
  deleteStockItem,
  getStockItemById,
  lookupStockItemBarcode,
  updateStockItem,
  type StockItemTypeValue,
  type UnitOfMeasureValue,
} from "@/lib/services/stock-item-service";

export const TYPE_LABELS: Record<number, string> = {
  [StockItemType.RawMaterial]: "Xammal",
  [StockItemType.FinishedGood]: "Hazır məhsul",
  [StockItemType.NonFood]: "Ləvazimat",
};
export const UNIT_LABELS: Record<number, string> = {
  [UnitOfMeasure.Piece]: "ədəd",
  [UnitOfMeasure.Kg]: "kq",
  [UnitOfMeasure.Gram]: "qram",
  [UnitOfMeasure.Liter]: "litr",
  [UnitOfMeasure.Ml]: "ml",
};

const SELECT_CLASS = "h-9 w-full rounded-md border border-input bg-background px-3 text-sm";

type Form = {
  barcode: string;
  name: string;
  categoryId: number | "";
  type: number;
  unit: number;
  salePrice: string;
  restaurantId: number | "";
};

/**
 * Adding or editing a stock item. Only the name, category, kind and unit are asked for; the sale price and the branch are under "Əlavə".
 * A scanned barcode fills in what the system already knows about the product.
 */
export function StockItemDialog({
  open,
  itemId,
  companyId,
  defaultCategoryId,
  categories,
  restaurants,
  canDelete,
  onClose,
  onSaved,
  onAddCategory,
}: {
  open: boolean;
  /** null: a new item. */
  itemId: number | null;
  companyId: number;
  defaultCategoryId: number | null;
  categories: StockCategory[];
  restaurants: Restaurant[];
  canDelete: boolean;
  onClose: () => void;
  /** Called after saving; for a new item with its id. */
  onSaved: (createdId?: number) => void;
  /** When given, a category can be added from this window (where there is no category tree next to it); returns the new category's id. */
  onAddCategory?: (name: string) => Promise<number | null>;
}) {
  const empty = (): Form => ({
    barcode: "",
    name: "",
    categoryId: defaultCategoryId ?? "",
    type: StockItemType.RawMaterial,
    unit: UnitOfMeasure.Piece,
    salePrice: "",
    restaurantId: "",
  });
  const [form, setForm] = useState<Form>(empty);
  const [more, setMore] = useState(false);
  const [loading, setLoading] = useState(false);
  const [saving, setSaving] = useState(false);
  // The name of a category being added from this window; null while not adding one.
  const [newCategory, setNewCategory] = useState<string | null>(null);
  // The last barcode looked up, so a scanner's Enter followed by a blur asks once.
  const lastLookedUp = useRef("");
  const patch = (p: Partial<Form>) => setForm((prev) => ({ ...prev, ...p }));

  useEffect(() => {
    if (!open) return;
    lastLookedUp.current = "";
    setForm(empty());
    setMore(false);
    setNewCategory(null);
    if (itemId == null) return;
    setLoading(true);
    getStockItemById(itemId)
      .then((item) => {
        setForm({
          barcode: item.barcode ?? "",
          name: item.name,
          categoryId: item.categoryId,
          type: item.type,
          unit: item.unit,
          salePrice: item.salePrice != null ? String(item.salePrice) : "",
          restaurantId: item.restaurantId ?? "",
        });
        setMore(item.salePrice != null || item.restaurantId != null);
      })
      .catch((e) => {
        toast.error(e instanceof Error ? e.message : "Məhsul yüklənmədi.");
        onClose();
      })
      .finally(() => setLoading(false));
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [open, itemId]);

  const lookupBarcode = async () => {
    const code = form.barcode.trim();
    if (itemId != null || code.length < 6 || lastLookedUp.current === code) return;
    lastLookedUp.current = code;
    try {
      const found = await lookupStockItemBarcode(code, companyId);
      if (!found.found) return;
      if (found.existsInCompany) {
        toast.warning(`Bu barkodla məhsul artıq var: ${found.name}`);
        return;
      }
      setForm((prev) => ({
        ...prev,
        name: prev.name.trim() ? prev.name : (found.name ?? ""),
        type: found.type ?? prev.type,
        unit: found.unit ?? prev.unit,
      }));
      toast.success("Məhsul məlumatı sistem bazasından gətirildi — yoxlayın.");
    } catch {
      // Without the lookup the form is simply filled by hand.
    }
  };

  const save = async () => {
    if (!form.name.trim()) return void toast.error("Məhsulun adını yazın.");
    if (form.categoryId === "") return void toast.error("Kateqoriyanı seçin.");
    const price = form.salePrice.trim() ? Number(form.salePrice.replace(",", ".")) : null;
    if (price != null && (!Number.isFinite(price) || price < 0)) return void toast.error("Satış qiyməti düzgün deyil.");
    const payload = {
      name: form.name,
      barcode: form.barcode || null,
      type: form.type as StockItemTypeValue,
      unit: form.unit as UnitOfMeasureValue,
      categoryId: form.categoryId,
      companyId,
      restaurantId: form.restaurantId === "" ? null : form.restaurantId,
      salePrice: price,
    };
    setSaving(true);
    try {
      let createdId: number | undefined;
      if (itemId == null) createdId = await createStockItem(payload);
      else await updateStockItem(itemId, payload);
      toast.success(itemId == null ? "Məhsul əlavə olundu." : "Məhsul yazıldı.");
      onSaved(createdId);
      onClose();
    } catch (e) {
      toast.error(e instanceof Error ? e.message : "Məhsul yazılmadı.");
    } finally {
      setSaving(false);
    }
  };

  const addCategory = async () => {
    const name = (newCategory ?? "").trim();
    if (!name || !onAddCategory) return;
    setSaving(true);
    try {
      const id = await onAddCategory(name);
      if (id != null) patch({ categoryId: id });
      setNewCategory(null);
    } catch (e) {
      toast.error(e instanceof Error ? e.message : "Kateqoriya yazılmadı.");
    } finally {
      setSaving(false);
    }
  };

  const remove = async () => {
    if (itemId == null || !window.confirm(`“${form.name}” silinsin?`)) return;
    setSaving(true);
    try {
      await deleteStockItem(itemId);
      onSaved();
      onClose();
    } catch (e) {
      toast.error(e instanceof Error ? e.message : "Məhsul silinmədi.");
    } finally {
      setSaving(false);
    }
  };

  return (
    <Dialog open={open} onOpenChange={(o) => !o && !saving && onClose()}>
      <DialogContent className="max-h-[90vh] overflow-y-auto sm:max-w-md">
        <DialogHeader>
          <DialogTitle>{itemId == null ? "Yeni məhsul" : "Məhsulu düzəlt"}</DialogTitle>
          <DialogDescription>Ad, kateqoriya, növ və vahid kifayətdir.</DialogDescription>
        </DialogHeader>
        {loading ? (
          <p className="py-8 text-center text-sm text-muted-foreground">Yüklənir…</p>
        ) : (
          <div className="space-y-3">
            <div className="space-y-1">
              <Label htmlFor="si-barcode">Barkod (istəyə bağlı)</Label>
              <Input
                id="si-barcode"
                autoComplete="off"
                placeholder="Barkodu oxudun"
                value={form.barcode}
                onChange={(e) => {
                  lastLookedUp.current = "";
                  patch({ barcode: e.target.value });
                }}
                // A scanner types the digits and presses Enter: that fills the form instead of submitting it.
                onKeyDown={(e) => {
                  if (e.key === "Enter") {
                    e.preventDefault();
                    void lookupBarcode();
                  }
                }}
                onBlur={() => void lookupBarcode()}
              />
            </div>
            <div className="space-y-1">
              <Label htmlFor="si-name">Ad</Label>
              <Input id="si-name" value={form.name} onChange={(e) => patch({ name: e.target.value })} />
            </div>
            <div className="space-y-1">
              <Label htmlFor="si-cat">Kateqoriya</Label>
              <select id="si-cat" className={SELECT_CLASS} value={form.categoryId} onChange={(e) => patch({ categoryId: e.target.value ? Number(e.target.value) : "" })}>
                <option value="">Seçin</option>
                {categories.map((c) => (
                  <option key={c.id} value={c.id}>
                    {c.name}
                    {!c.isActive ? " (deaktiv)" : ""}
                  </option>
                ))}
              </select>
              {categories.length === 0 && !onAddCategory && <p className="text-xs text-amber-600">Əvvəlcə soldakı “+” ilə kateqoriya yaradın.</p>}
              {onAddCategory &&
                (newCategory === null ? (
                  <button type="button" className="text-xs text-primary" onClick={() => setNewCategory("")}>
                    + Yeni kateqoriya
                  </button>
                ) : (
                  <div className="flex gap-2">
                    <Input
                      autoFocus
                      aria-label="Yeni kateqoriyanın adı"
                      placeholder="Kateqoriyanın adı"
                      value={newCategory}
                      onChange={(e) => setNewCategory(e.target.value)}
                      onKeyDown={(e) => {
                        if (e.key === "Enter") {
                          e.preventDefault();
                          void addCategory();
                        }
                      }}
                    />
                    <Button type="button" variant="outline" disabled={saving || !newCategory.trim()} onClick={() => void addCategory()}>
                      Əlavə et
                    </Button>
                  </div>
                ))}
            </div>
            <div className="grid grid-cols-2 gap-3">
              <div className="space-y-1">
                <Label htmlFor="si-type">Növ</Label>
                <select id="si-type" className={SELECT_CLASS} value={form.type} onChange={(e) => patch({ type: Number(e.target.value) })}>
                  {Object.entries(TYPE_LABELS).map(([v, label]) => (
                    <option key={v} value={v}>
                      {label}
                    </option>
                  ))}
                </select>
              </div>
              <div className="space-y-1">
                <Label htmlFor="si-unit">Vahid</Label>
                <select id="si-unit" className={SELECT_CLASS} value={form.unit} onChange={(e) => patch({ unit: Number(e.target.value) })}>
                  {Object.entries(UNIT_LABELS).map(([v, label]) => (
                    <option key={v} value={v}>
                      {label}
                    </option>
                  ))}
                </select>
              </div>
            </div>
            <button type="button" className="flex items-center gap-1 text-sm text-primary" onClick={() => setMore((m) => !m)}>
              {more ? <ChevronDown className="h-4 w-4" /> : <ChevronRight className="h-4 w-4" />}
              Əlavə
            </button>
            {more && (
              <div className="space-y-3 rounded-md border p-3">
                <div className="space-y-1">
                  <Label htmlFor="si-price">Satış qiyməti</Label>
                  <Input id="si-price" inputMode="decimal" value={form.salePrice} onChange={(e) => patch({ salePrice: e.target.value })} />
                  <p className="text-xs text-muted-foreground">Bu məhsula bağlı menyu maddəsinin Dəzgah qiyməti boşdursa, POS-da bu qiymət işlədilir.</p>
                </div>
                <div className="space-y-1">
                  <Label htmlFor="si-branch">Filial</Label>
                  <select id="si-branch" className={SELECT_CLASS} value={form.restaurantId} onChange={(e) => patch({ restaurantId: e.target.value ? Number(e.target.value) : "" })}>
                    <option value="">Hamısı</option>
                    {restaurants.map((r) => (
                      <option key={r.id} value={r.id}>
                        {r.name}
                      </option>
                    ))}
                  </select>
                </div>
              </div>
            )}
          </div>
        )}
        <DialogFooter className="gap-2">
          {itemId != null && canDelete && (
            <Button variant="destructive" className="mr-auto" disabled={saving || loading} onClick={() => void remove()}>
              Sil
            </Button>
          )}
          <Button variant="outline" disabled={saving} onClick={onClose}>
            Ləğv et
          </Button>
          <Button disabled={saving || loading} onClick={() => void save()}>
            Yadda saxla
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
