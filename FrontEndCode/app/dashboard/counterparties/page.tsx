"use client";

import { useEffect, useMemo, useState } from "react";
import { AdvancedTableFilters, type TableFilterDef } from "@/components/advanced-table-filters";
import { DataTable } from "@/components/data-table";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Checkbox } from "@/components/ui/checkbox";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Plus, Trash2 } from "lucide-react";
import { toast } from "sonner";
import {
  adjustCounterpartyDebt,
  createCounterparty,
  deleteCounterparty,
  getCounterparties,
  updateCounterparty,
  type Counterparty,
} from "@/lib/services/counterparty-service";
import {
  createCounterpartyCategory,
  deleteCounterpartyCategory,
  getCounterpartyCategories,
  updateCounterpartyCategory,
  type CounterpartyCategory,
} from "@/lib/services/counterparty-category-service";

const selectClass =
  "flex h-10 w-full rounded-md border border-input bg-background px-3 py-2 text-sm ring-offset-background";

type CounterpartyRow = {
  id: string;
  counterpartyId: number;
  name: string;
  phoneNumber: string;
  categoryLabel: string;
  categoryId: number;
  isActive: boolean;
  debt: number;
};

export default function CounterpartiesPage() {
  const [list, setList] = useState<Counterparty[]>([]);
  const [loading, setLoading] = useState(true);

  const [categories, setCategories] = useState<CounterpartyCategory[]>([]);
  const [categoriesLoading, setCategoriesLoading] = useState(true);
  const [newCategoryName, setNewCategoryName] = useState("");
  const [savingCategory, setSavingCategory] = useState(false);
  const [editingCategoryId, setEditingCategoryId] = useState<number | null>(null);
  const [editingCategoryName, setEditingCategoryName] = useState("");

  const [dialogOpen, setDialogOpen] = useState(false);
  const [saving, setSaving] = useState(false);
  const [editingId, setEditingId] = useState<number | null>(null);
  const [name, setName] = useState("");
  const [phoneNumber, setPhoneNumber] = useState("");
  const [categoryId, setCategoryId] = useState<string>("");
  const [isActive, setIsActive] = useState(true);

  const [debtDialogOpen, setDebtDialogOpen] = useState(false);
  const [debtTarget, setDebtTarget] = useState<Counterparty | null>(null);
  const [debtInput, setDebtInput] = useState("0");
  const [savingDebt, setSavingDebt] = useState(false);

  const load = async () => {
    setLoading(true);
    try {
      const data = await getCounterparties();
      setList(data);
    } catch (e) {
      toast.error(e instanceof Error ? e.message : "Konturagentlər yüklənmədi.");
      setList([]);
    } finally {
      setLoading(false);
    }
  };

  const loadCategories = async () => {
    setCategoriesLoading(true);
    try {
      const data = await getCounterpartyCategories();
      setCategories(data);
    } catch (e) {
      toast.error(e instanceof Error ? e.message : "Kateqoriyalar yüklənmədi.");
      setCategories([]);
    } finally {
      setCategoriesLoading(false);
    }
  };

  useEffect(() => {
    void load();
    void loadCategories();
  }, []);

  const handleAddCategory = async () => {
    const trimmed = newCategoryName.trim();
    if (!trimmed) {
      toast.error("Kateqoriya adı vacibdir.");
      return;
    }
    setSavingCategory(true);
    try {
      await createCounterpartyCategory({ name: trimmed, isActive: true });
      setNewCategoryName("");
      toast.success("Kateqoriya əlavə edildi.");
      await loadCategories();
    } catch (e) {
      toast.error(e instanceof Error ? e.message : "Kateqoriya əlavə olunmadı.");
    } finally {
      setSavingCategory(false);
    }
  };

  const startEditCategory = (c: CounterpartyCategory) => {
    setEditingCategoryId(c.id);
    setEditingCategoryName(c.name);
  };

  const handleSaveCategoryEdit = async (id: number) => {
    const trimmed = editingCategoryName.trim();
    if (!trimmed) {
      toast.error("Kateqoriya adı vacibdir.");
      return;
    }
    setSavingCategory(true);
    try {
      await updateCounterpartyCategory(id, { name: trimmed, isActive: true });
      setEditingCategoryId(null);
      toast.success("Kateqoriya yeniləndi.");
      await loadCategories();
    } catch (e) {
      toast.error(e instanceof Error ? e.message : "Kateqoriya yenilənmədi.");
    } finally {
      setSavingCategory(false);
    }
  };

  const handleDeleteCategory = async (c: CounterpartyCategory) => {
    if (!window.confirm(`"${c.name}" kateqoriyasını silmək istəyirsiniz?`)) return;
    try {
      await deleteCounterpartyCategory(c.id);
      toast.success("Kateqoriya silindi.");
      await loadCategories();
    } catch (e) {
      toast.error(e instanceof Error ? e.message : "Kateqoriya silinmədi.");
    }
  };

  const resetForm = () => {
    setEditingId(null);
    setName("");
    setPhoneNumber("");
    setCategoryId("");
    setIsActive(true);
  };

  const handleAdd = () => {
    resetForm();
    setDialogOpen(true);
  };

  const handleEdit = (row: CounterpartyRow) => {
    const target = list.find((c) => c.id === row.counterpartyId);
    if (!target) return;
    setEditingId(target.id);
    setName(target.name);
    setPhoneNumber(target.phoneNumber ?? "");
    setCategoryId(String(target.categoryId));
    setIsActive(target.isActive);
    setDialogOpen(true);
  };

  const handleDelete = async (row: CounterpartyRow) => {
    if (!window.confirm(`"${row.name}" konturagentini silmək istəyirsiniz?`)) return;
    try {
      await deleteCounterparty(row.counterpartyId);
      toast.success("Konturagent silindi.");
      await load();
    } catch (e) {
      toast.error(e instanceof Error ? e.message : "Silinmədi.");
    }
  };

  const handleSave = async () => {
    if (!name.trim()) {
      toast.error("Ad vacibdir.");
      return;
    }
    const catId = Number(categoryId);
    if (!Number.isFinite(catId) || catId <= 0) {
      toast.error("Kateqoriya seçilməlidir.");
      return;
    }
    setSaving(true);
    try {
      const payload = {
        name: name.trim(),
        phoneNumber: phoneNumber.trim() || null,
        categoryId: catId,
        isActive,
      };
      if (editingId == null) {
        await createCounterparty(payload);
        toast.success("Konturagent əlavə edildi.");
      } else {
        await updateCounterparty(editingId, payload);
        toast.success("Konturagent yeniləndi.");
      }
      setDialogOpen(false);
      resetForm();
      await load();
    } catch (e) {
      toast.error(e instanceof Error ? e.message : "Yadda saxlanılmadı.");
    } finally {
      setSaving(false);
    }
  };

  const openDebtDialog = (row: CounterpartyRow) => {
    const target = list.find((c) => c.id === row.counterpartyId);
    if (!target) return;
    setDebtTarget(target);
    setDebtInput(String(target.currentDebtAmount));
    setDebtDialogOpen(true);
  };

  const handleSaveDebt = async () => {
    if (!debtTarget) return;
    const amount = Number(debtInput);
    if (!Number.isFinite(amount)) {
      toast.error("Məbləğ düzgün deyil.");
      return;
    }
    setSavingDebt(true);
    try {
      await adjustCounterpartyDebt(debtTarget.id, amount);
      toast.success("Borc yeniləndi.");
      setDebtDialogOpen(false);
      await load();
    } catch (e) {
      toast.error(e instanceof Error ? e.message : "Borc yenilənmədi.");
    } finally {
      setSavingDebt(false);
    }
  };

  const rows: CounterpartyRow[] = useMemo(
    () =>
      list.map((c) => ({
        id: String(c.id),
        counterpartyId: c.id,
        name: c.name,
        phoneNumber: c.phoneNumber ?? "—",
        categoryLabel: c.categoryName || `#${c.categoryId}`,
        categoryId: c.categoryId,
        isActive: c.isActive,
        debt: c.currentDebtAmount,
      })),
    [list],
  );

  const categoryOptions = useMemo(
    () => categories.map((c) => ({ value: String(c.id), label: c.name })),
    [categories],
  );

  const filterDefs = useMemo<TableFilterDef<CounterpartyRow>[]>(
    () => [
      {
        id: "name",
        label: "Ad",
        ui: "text",
        match: (row, get) => {
          const q = get("name").trim().toLowerCase();
          if (!q) return true;
          return row.name.toLowerCase().includes(q);
        },
      },
      {
        id: "category",
        label: "Kateqoriya",
        ui: "select",
        options: categoryOptions,
        match: (row, get) => {
          const v = get("category");
          if (!v) return true;
          return String(row.categoryId) === v;
        },
      },
      {
        id: "status",
        label: "Status",
        ui: "status",
        match: (row, get) => {
          const v = get("status");
          if (v === "all" || !v) return true;
          if (v === "active") return row.isActive;
          if (v === "inactive") return !row.isActive;
          return true;
        },
      },
    ],
    [categoryOptions],
  );

  const columns = [
    { key: "name" as const, label: "Ad" },
    { key: "phoneNumber" as const, label: "Telefon" },
    { key: "categoryLabel" as const, label: "Kateqoriya" },
    {
      key: "debt" as const,
      label: "Borc",
      render: (v: number, row: CounterpartyRow) => (
        <button
          type="button"
          onClick={() => openDebtDialog(row)}
          className={v > 0 ? "font-semibold text-destructive hover:underline" : "text-muted-foreground hover:underline"}
        >
          {v.toFixed(2)} ₼
        </button>
      ),
    },
    {
      key: "isActive" as const,
      label: "Status",
      render: (v: boolean) => (
        <Badge className={v ? "bg-emerald-100 text-emerald-800 hover:bg-emerald-100" : "bg-slate-200 text-slate-800 hover:bg-slate-200"}>
          {v ? "Aktiv" : "Passiv"}
        </Badge>
      ),
    },
  ];

  if (loading) {
    return <div className="p-6 text-sm text-muted-foreground">Yüklənir…</div>;
  }

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-3xl font-bold text-foreground">Konturagentlər</h1>
        <p className="text-muted-foreground mt-1">Təchizatçılar və digər tərəfdaşlar — borc izləməsi ilə.</p>
      </div>

      <div className="rounded-xl border bg-card p-4">
        <h2 className="mb-1 text-lg font-semibold">Kateqoriyalar</h2>
        <p className="mb-3 text-sm text-muted-foreground">
          Öz kateqoriyalarınızı yaradın (məs. Təchizatçı, Bank, Şöbə) — konturagent əlavə edərkən bu siyahıdan seçəcəksiniz.
        </p>

        <div className="mb-3 flex flex-wrap gap-2">
          {categoriesLoading && <p className="text-sm text-muted-foreground">Yüklənir…</p>}
          {!categoriesLoading && categories.length === 0 && (
            <p className="text-sm text-muted-foreground">Hələ kateqoriya yoxdur.</p>
          )}
          {categories.map((c) =>
            editingCategoryId === c.id ? (
              <div key={c.id} className="flex items-center gap-1 rounded-full border bg-background px-2 py-1">
                <Input
                  autoFocus
                  className="h-7 w-32 text-sm"
                  value={editingCategoryName}
                  onChange={(e) => setEditingCategoryName(e.target.value)}
                  onKeyDown={(e) => e.key === "Enter" && void handleSaveCategoryEdit(c.id)}
                />
                <Button size="sm" className="h-7 px-2" disabled={savingCategory} onClick={() => void handleSaveCategoryEdit(c.id)}>
                  OK
                </Button>
              </div>
            ) : (
              <span key={c.id} className="flex items-center gap-2 rounded-full border bg-muted px-3 py-1.5 text-sm">
                <button type="button" onClick={() => startEditCategory(c)} className="hover:underline">
                  {c.name}
                </button>
                <button
                  type="button"
                  onClick={() => void handleDeleteCategory(c)}
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
            value={newCategoryName}
            onChange={(e) => setNewCategoryName(e.target.value)}
            placeholder="Yeni kateqoriya adı"
            onKeyDown={(e) => e.key === "Enter" && void handleAddCategory()}
          />
          <Button size="sm" disabled={savingCategory} onClick={() => void handleAddCategory()}>
            <Plus className="mr-1 h-4 w-4" />
            Əlavə et
          </Button>
        </div>
      </div>

      <Dialog
        open={dialogOpen}
        onOpenChange={(o) => {
          setDialogOpen(o);
          if (!o) resetForm();
        }}
      >
        <AdvancedTableFilters defs={filterDefs} data={rows}>
          {(filtered) => (
            <DataTable
              title="Konturagent siyahısı"
              columns={columns}
              data={filtered}
              idSortKey="counterpartyId"
              searchPlaceholder="Ad axtar…"
              searchableFields={["name", "phoneNumber"]}
              onAdd={handleAdd}
              onEdit={handleEdit}
              onDelete={handleDelete}
            />
          )}
        </AdvancedTableFilters>

        <DialogContent className="sm:max-w-md">
          <DialogHeader>
            <DialogTitle>{editingId != null ? "Konturagenti redaktə et" : "Konturagent əlavə et"}</DialogTitle>
            <DialogDescription>Ad, telefon və kateqoriya.</DialogDescription>
          </DialogHeader>

          <div className="space-y-3">
            <div>
              <Label htmlFor="cp-name">Ad</Label>
              <Input id="cp-name" className="mt-1" value={name} onChange={(e) => setName(e.target.value)} />
            </div>
            <div>
              <Label htmlFor="cp-phone">Telefon</Label>
              <Input id="cp-phone" className="mt-1" value={phoneNumber} onChange={(e) => setPhoneNumber(e.target.value)} placeholder="Opsional" />
            </div>
            <div>
              <Label htmlFor="cp-category">Kateqoriya</Label>
              <select
                id="cp-category"
                className={selectClass + " mt-1"}
                value={categoryId}
                onChange={(e) => setCategoryId(e.target.value)}
              >
                <option value="">Kateqoriya seçin</option>
                {categories.map((c) => (
                  <option key={c.id} value={String(c.id)}>
                    {c.name}
                  </option>
                ))}
              </select>
              {categories.length === 0 && (
                <p className="mt-1 text-xs text-muted-foreground">
                  Əvvəlcə yuxarıdakı "Kateqoriyalar" bölümündən bir kateqoriya yaradın.
                </p>
              )}
            </div>
            <div className="flex items-center gap-2">
              <Checkbox id="cp-active" checked={isActive} onCheckedChange={(v) => setIsActive(v === true)} />
              <Label htmlFor="cp-active" className="text-sm font-normal">
                Aktiv
              </Label>
            </div>
          </div>

          <div className="mt-4 flex justify-end gap-2">
            <Button variant="outline" onClick={() => setDialogOpen(false)} disabled={saving}>
              Ləğv et
            </Button>
            <Button onClick={() => void handleSave()} disabled={saving}>
              {saving ? "Saxlanılır…" : "Saxla"}
            </Button>
          </div>
        </DialogContent>
      </Dialog>

      <Dialog open={debtDialogOpen} onOpenChange={setDebtDialogOpen}>
        <DialogContent className="sm:max-w-xs">
          <DialogHeader>
            <DialogTitle>Borcu düzəlt</DialogTitle>
            <DialogDescription>{debtTarget?.name}</DialogDescription>
          </DialogHeader>
          <div>
            <Label htmlFor="cp-debt">Cari borc (₼)</Label>
            <Input id="cp-debt" type="number" step="0.01" className="mt-1" value={debtInput} onChange={(e) => setDebtInput(e.target.value)} />
          </div>
          <div className="mt-4 flex justify-end gap-2">
            <Button variant="outline" onClick={() => setDebtDialogOpen(false)} disabled={savingDebt}>
              Ləğv et
            </Button>
            <Button onClick={() => void handleSaveDebt()} disabled={savingDebt}>
              {savingDebt ? "Saxlanılır…" : "Saxla"}
            </Button>
          </div>
        </DialogContent>
      </Dialog>
    </div>
  );
}
