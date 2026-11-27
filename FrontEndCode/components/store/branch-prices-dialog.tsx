"use client";

import { useEffect, useState } from "react";
import { toast } from "sonner";
import { Button } from "@/components/ui/button";
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from "@/components/ui/dialog";
import { Input } from "@/components/ui/input";
import { getRestaurants, type Restaurant } from "@/lib/services/restaurant-service";
import { getStoreBranchPrices, saveStoreBranchPrices, type StoreProduct } from "@/lib/store/store-service";

/** A product's own sale price per branch; an empty field means the branch uses the normal sale price. */
export default function BranchPricesDialog({ product, onClose }: { product: StoreProduct | null; onClose: () => void }) {
  const [branches, setBranches] = useState<Restaurant[]>([]);
  const [values, setValues] = useState<Record<number, string>>({});
  const [saving, setSaving] = useState(false);

  useEffect(() => {
    if (!product) return;
    getRestaurants().then(setBranches).catch(() => setBranches([]));
    getStoreBranchPrices(product.id)
      .then((rows) => setValues(Object.fromEntries(rows.map((r) => [r.branchId, String(r.salePrice)]))))
      .catch(() => setValues({}));
  }, [product]);

  const save = async () => {
    if (!product) return;
    const prices = Object.entries(values)
      .map(([branchId, v]) => ({ branchId: Number(branchId), salePrice: Number(v.replace(",", ".")) }))
      .filter((p) => values[p.branchId].trim() !== "");
    if (prices.some((p) => !Number.isFinite(p.salePrice) || p.salePrice <= 0)) {
      toast.error("Qiymət 0-dan böyük rəqəm olmalıdır.");
      return;
    }
    setSaving(true);
    try {
      await saveStoreBranchPrices(product.id, prices);
      toast.success("Filial qiymətləri yadda saxlanıldı.");
      onClose();
    } catch (e) {
      toast.error(e instanceof Error ? e.message : "Yazılmadı.");
    } finally {
      setSaving(false);
    }
  };

  return (
    <Dialog open={product != null} onOpenChange={(o) => !o && onClose()}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>Filial qiymətləri — {product?.name}</DialogTitle>
          <DialogDescription>Normal satış qiyməti: {product?.salePrice}. Kampaniya qiyməti işləyirsə o üstün gəlir.</DialogDescription>
        </DialogHeader>
        <div className="space-y-2">
          {branches.map((b) => (
            <div key={b.id} className="flex items-center justify-between gap-3">
              <span className="text-sm">{b.name}</span>
              <Input
                className="w-32"
                inputMode="decimal"
                placeholder="normal"
                value={values[b.id] ?? ""}
                onChange={(e) => setValues((v) => ({ ...v, [b.id]: e.target.value }))}
              />
            </div>
          ))}
        </div>
        <DialogFooter>
          <Button variant="outline" onClick={onClose}>
            Bağla
          </Button>
          <Button disabled={saving} onClick={() => void save()}>
            Yaz
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
