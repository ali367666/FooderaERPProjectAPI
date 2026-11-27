"use client";

import { useMemo, useState } from "react";
import { ChevronDown, ChevronRight, Pencil, Plus } from "lucide-react";
import { toast } from "sonner";
import { Button } from "@/components/ui/button";
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from "@/components/ui/dialog";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { cn } from "@/lib/utils";
import { createStockCategory, deleteStockCategory, updateStockCategory, type StockCategory } from "@/lib/services/stock-category-service";

const SELECT_CLASS = "h-9 w-full rounded-md border border-input bg-background px-3 text-sm";

/** A category and everything under it. */
export function categoryWithChildren(categories: StockCategory[], id: number): Set<number> {
  const ids = new Set<number>([id]);
  let grew = true;
  while (grew) {
    grew = false;
    for (const c of categories) {
      if (c.parentId != null && ids.has(c.parentId) && !ids.has(c.id)) {
        ids.add(c.id);
        grew = true;
      }
    }
  }
  return ids;
}

type Form = { id: number | null; name: string; parentId: number | ""; isActive: boolean; description: string };

/** The category tree on the left of the stock page: pick a category to filter, add, rename, move or delete it right here. */
export function CategoryPanel({
  companyId,
  categories,
  counts,
  total,
  selected,
  onSelect,
  onChanged,
  canCreate,
  canUpdate,
  canDelete,
}: {
  companyId: number;
  categories: StockCategory[];
  /** Number of items directly in each category. */
  counts: Record<number, number>;
  total: number;
  selected: number | null;
  onSelect: (id: number | null) => void;
  onChanged: () => void;
  canCreate: boolean;
  canUpdate: boolean;
  canDelete: boolean;
}) {
  const [closed, setClosed] = useState<Set<number>>(new Set());
  const [form, setForm] = useState<Form | null>(null);
  const [saving, setSaving] = useState(false);

  // Children by parent; a category whose parent is not in the list is shown at the top level.
  const tree = useMemo(() => {
    const known = new Set(categories.map((c) => c.id));
    const map = new Map<number, StockCategory[]>();
    for (const c of categories) {
      const parent = c.parentId != null && known.has(c.parentId) ? c.parentId : 0;
      map.set(parent, [...(map.get(parent) ?? []), c]);
    }
    for (const list of map.values()) list.sort((a, b) => a.name.localeCompare(b.name, "az"));
    return map;
  }, [categories]);

  const countOf = (id: number) => [...categoryWithChildren(categories, id)].reduce((s, x) => s + (counts[x] ?? 0), 0);

  const startNew = () => setForm({ id: null, name: "", parentId: selected ?? "", isActive: true, description: "" });
  const startEdit = (c: StockCategory) => setForm({ id: c.id, name: c.name, parentId: c.parentId ?? "", isActive: c.isActive, description: c.description ?? "" });

  const save = async () => {
    if (!form) return;
    if (!form.name.trim()) return void toast.error("Kateqoriyanın adını yazın.");
    setSaving(true);
    try {
      const payload = {
        name: form.name,
        description: form.description || null,
        isActive: form.isActive,
        companyId,
        parentId: form.parentId === "" ? null : form.parentId,
      };
      if (form.id == null) await createStockCategory(payload);
      else await updateStockCategory(form.id, payload);
      setForm(null);
      onChanged();
    } catch (e) {
      toast.error(e instanceof Error ? e.message : "Kateqoriya yazılmadı.");
    } finally {
      setSaving(false);
    }
  };

  const remove = async () => {
    if (!form?.id) return;
    if (!window.confirm(`“${form.name}” kateqoriyası silinsin?`)) return;
    setSaving(true);
    try {
      await deleteStockCategory(form.id);
      if (selected === form.id) onSelect(null);
      setForm(null);
      onChanged();
    } catch (e) {
      toast.error(e instanceof Error ? e.message : "Kateqoriya silinmədi.");
    } finally {
      setSaving(false);
    }
  };

  const renderLevel = (parent: number, depth: number): React.ReactNode =>
    (tree.get(parent) ?? []).map((c) => {
      const children = tree.get(c.id) ?? [];
      const open = !closed.has(c.id);
      return (
        <div key={c.id}>
          <div className={cn("group flex items-center gap-1 rounded-md pr-1 text-sm", selected === c.id ? "bg-primary text-primary-foreground" : "hover:bg-muted")} style={{ paddingLeft: depth * 14 }}>
            {children.length > 0 ? (
              <button
                type="button"
                aria-label={open ? "Bağla" : "Aç"}
                className="p-1"
                onClick={() =>
                  setClosed((prev) => {
                    const next = new Set(prev);
                    if (next.has(c.id)) next.delete(c.id);
                    else next.add(c.id);
                    return next;
                  })
                }
              >
                {open ? <ChevronDown className="h-3.5 w-3.5" /> : <ChevronRight className="h-3.5 w-3.5" />}
              </button>
            ) : (
              <span className="w-[22px]" />
            )}
            <button type="button" className={cn("flex-1 truncate py-1.5 text-left", !c.isActive && "line-through opacity-60")} onClick={() => onSelect(c.id)}>
              {c.name}
            </button>
            {canUpdate && (
              <button type="button" aria-label={`${c.name}: düzəlt`} className="p-1 opacity-0 focus:opacity-100 group-hover:opacity-100" onClick={() => startEdit(c)}>
                <Pencil className="h-3.5 w-3.5" />
              </button>
            )}
            <span className={cn("min-w-6 text-right text-xs", selected === c.id ? "" : "text-muted-foreground")}>{countOf(c.id)}</span>
          </div>
          {open && children.length > 0 && renderLevel(c.id, depth + 1)}
        </div>
      );
    });

  // A category cannot be moved under itself or one of its own children.
  const parentOptions = form?.id == null ? categories : categories.filter((c) => !categoryWithChildren(categories, form.id!).has(c.id));

  return (
    <div className="space-y-2 rounded-lg border bg-card p-3">
      <div className="flex items-center justify-between">
        <h2 className="text-sm font-semibold">Kateqoriyalar</h2>
        {canCreate && (
          <Button size="icon" variant="ghost" className="h-7 w-7" aria-label="Yeni kateqoriya" onClick={startNew}>
            <Plus className="h-4 w-4" />
          </Button>
        )}
      </div>
      <button
        type="button"
        onClick={() => onSelect(null)}
        className={cn("flex w-full items-center justify-between rounded-md px-2 py-1.5 text-sm", selected === null ? "bg-primary text-primary-foreground" : "hover:bg-muted")}
      >
        <span>Hamısı</span>
        <span className="text-xs">{total}</span>
      </button>
      <div className="max-h-[60vh] overflow-y-auto">
        {categories.length === 0 ? <p className="px-2 py-4 text-center text-xs text-muted-foreground">Hələ kateqoriya yoxdur. “+” ilə yaradın.</p> : renderLevel(0, 0)}
      </div>

      <Dialog open={form != null} onOpenChange={(o) => !o && !saving && setForm(null)}>
        <DialogContent className="sm:max-w-sm">
          <DialogHeader>
            <DialogTitle>{form?.id == null ? "Yeni kateqoriya" : "Kateqoriyanı düzəlt"}</DialogTitle>
            <DialogDescription>Ad kifayətdir; istəsəniz başqa kateqoriyanın altına qoyun.</DialogDescription>
          </DialogHeader>
          {form && (
            <div className="space-y-3">
              <div className="space-y-1">
                <Label htmlFor="cat-name">Ad</Label>
                <Input id="cat-name" autoFocus value={form.name} onChange={(e) => setForm({ ...form, name: e.target.value })} onKeyDown={(e) => e.key === "Enter" && void save()} />
              </div>
              <div className="space-y-1">
                <Label htmlFor="cat-parent">Üst kateqoriya</Label>
                <select id="cat-parent" className={SELECT_CLASS} value={form.parentId} onChange={(e) => setForm({ ...form, parentId: e.target.value ? Number(e.target.value) : "" })}>
                  <option value="">Yoxdur</option>
                  {parentOptions.map((c) => (
                    <option key={c.id} value={c.id}>
                      {c.name}
                    </option>
                  ))}
                </select>
              </div>
              {form.id != null && (
                <label className="flex items-center gap-2 text-sm">
                  <input type="checkbox" checked={form.isActive} onChange={(e) => setForm({ ...form, isActive: e.target.checked })} />
                  Aktivdir
                </label>
              )}
            </div>
          )}
          <DialogFooter className="gap-2">
            {form?.id != null && canDelete && (
              <Button variant="destructive" className="mr-auto" disabled={saving} onClick={() => void remove()}>
                Sil
              </Button>
            )}
            <Button variant="outline" disabled={saving} onClick={() => setForm(null)}>
              Ləğv et
            </Button>
            <Button disabled={saving} onClick={() => void save()}>
              Yadda saxla
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}
