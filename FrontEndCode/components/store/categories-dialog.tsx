"use client";

import { useCallback, useEffect, useState } from "react";
import { toast } from "sonner";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Dialog, DialogContent, DialogDescription, DialogHeader, DialogTitle } from "@/components/ui/dialog";
import { Input } from "@/components/ui/input";
import { getStoreCategories, saveStoreCategory, type StoreCategory } from "@/lib/store/store-service";

/** The product categories, and which of them give no bonus (their products never earn bonus). */
export default function CategoriesDialog({ open, onClose, onChanged }: { open: boolean; onClose: () => void; onChanged: () => void }) {
  const [categories, setCategories] = useState<StoreCategory[]>([]);
  const [name, setName] = useState("");
  const [noBonus, setNoBonus] = useState(false);

  const load = useCallback(() => {
    getStoreCategories().then(setCategories).catch(() => setCategories([]));
  }, []);

  useEffect(() => {
    if (open) load();
  }, [open, load]);

  const save = async (c: StoreCategory, patch: Partial<StoreCategory>) => {
    try {
      await saveStoreCategory(c.id, { name: c.name, parentCategoryId: c.parentCategoryId, isActive: c.isActive, earnsBonus: c.earnsBonus, ...patch });
      load();
      onChanged();
    } catch (e) {
      toast.error(e instanceof Error ? e.message : "Yazılmadı.");
    }
  };

  const add = async () => {
    if (!name.trim()) return;
    try {
      await saveStoreCategory(null, { name: name.trim(), parentCategoryId: null, isActive: true, earnsBonus: !noBonus });
      setName("");
      setNoBonus(false);
      load();
      onChanged();
    } catch (e) {
      toast.error(e instanceof Error ? e.message : "Kateqoriya yazılmadı.");
    }
  };

  return (
    <Dialog open={open} onOpenChange={(o) => !o && onClose()}>
      <DialogContent className="sm:max-w-lg">
        <DialogHeader>
          <DialogTitle>Kateqoriyalar</DialogTitle>
          <DialogDescription>
            "Bonus verilmir" işarələnmiş kateqoriyadakı (və onun alt kateqoriyalarındakı) məhsullardan müştəriyə bonus yazılmır.
          </DialogDescription>
        </DialogHeader>

        <div className="max-h-72 space-y-1 overflow-auto">
          {categories.length === 0 ? (
            <p className="py-4 text-center text-sm text-muted-foreground">Kateqoriya yoxdur.</p>
          ) : (
            categories.map((c) => (
              <div key={c.id} className="flex items-center justify-between gap-2 rounded-md border px-3 py-2 text-sm">
                <span>
                  {c.name} {!c.earnsBonus && <Badge variant="destructive">bonus yoxdur</Badge>}
                </span>
                <label className="flex items-center gap-1.5 text-xs">
                  <input type="checkbox" checked={!c.earnsBonus} onChange={(e) => void save(c, { earnsBonus: !e.target.checked })} />
                  Bonus verilmir
                </label>
              </div>
            ))
          )}
        </div>

        <div className="flex flex-wrap items-center gap-2 border-t pt-3">
          <Input className="min-w-40 flex-1" placeholder="Yeni kateqoriya adı" value={name} onChange={(e) => setName(e.target.value)} />
          <label className="flex items-center gap-1.5 text-xs">
            <input type="checkbox" checked={noBonus} onChange={(e) => setNoBonus(e.target.checked)} />
            Bonus verilmir
          </label>
          <Button onClick={() => void add()} disabled={!name.trim()}>
            Yarat
          </Button>
        </div>
      </DialogContent>
    </Dialog>
  );
}
