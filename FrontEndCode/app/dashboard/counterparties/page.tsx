"use client";

import { useEffect, useMemo, useState } from "react";
import { AdvancedTableFilters, type TableFilterDef } from "@/components/advanced-table-filters";
import { DataTable } from "@/components/data-table";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { RadioGroup, RadioGroupItem } from "@/components/ui/radio-group";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { CounterpartyDebtDialog } from "@/components/counterparties/counterparty-debt-dialog";
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
  getCounterpartyCategories,
  type CounterpartyCategory,
} from "@/lib/services/counterparty-category-service";

const FIXED_CATEGORY_ORDER = [
  "tədarükçü",
  "pərakəndə",
  "satıcı/topdan",
  "bank",
  "əsas tədarükçü",
  "şöbə",
  "personal",
];

const fixedCategoryIndex = (name: string) => FIXED_CATEGORY_ORDER.indexOf(name.trim().toLocaleLowerCase("az"));

type CounterpartyRow = {
  id: string;
  counterpartyId: number;
  name: string;
  phoneNumber: string;
  categoryLabel: string;
  categoryId: number;
  isActive: boolean;
  isBank: boolean;
  debt: number;
};

export default function CounterpartiesPage() {
  const [list, setList] = useState<Counterparty[]>([]);
  const [loading, setLoading] = useState(true);

  const [categories, setCategories] = useState<CounterpartyCategory[]>([]);
  const [categoriesLoading, setCategoriesLoading] = useState(true);
  // Passive counterparties (no longer delivering) stay out of the active list; open one and tick
  // "Aktiv" to bring it back.
  const [statusView, setStatusView] = useState<"active" | "passive">("active");
  const [categoryView, setCategoryView] = useState<string>("all");
  // The row picked in the list: the Aktiv / Passiv choice below the list filters acts on it.
  const [selectedId, setSelectedId] = useState<number | null>(null);

  const [dialogOpen, setDialogOpen] = useState(false);
  const [saving, setSaving] = useState(false);
  const [confirmStep, setConfirmStep] = useState(false);
  const [editingId, setEditingId] = useState<number | null>(null);
  const [name, setName] = useState("");
  const [phoneNumber, setPhoneNumber] = useState("");
  const [categoryId, setCategoryId] = useState<string>("");
  const [isActive, setIsActive] = useState(true);
  // The fixed payment banks keep their name and category; only "Aktiv" can change.
  const [editingIsBank, setEditingIsBank] = useState(false);

  const [debtDialogOpen, setDebtDialogOpen] = useState(false);
  const [debtTarget, setDebtTarget] = useState<Counterparty | null>(null);
  const [debtInput, setDebtInput] = useState("0");
  const [savingDebt, setSavingDebt] = useState(false);
  const [debtConfirmStep, setDebtConfirmStep] = useState(false);
  // The debt dialog: add debt and see the history (the correction dialog below opens from it).
  const [historyTarget, setHistoryTarget] = useState<Counterparty | null>(null);

  const load = async () => {
    setLoading(true);
    try {
      const data = await getCounterparties("all");
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

  const resetForm = () => {
    setEditingId(null);
    setName("");
    setPhoneNumber("");
    setCategoryId("");
    setIsActive(true);
    setEditingIsBank(false);
    setConfirmStep(false);
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
    setEditingIsBank(target.isBank);
    setConfirmStep(false);
    setDialogOpen(true);
  };

  // Applies the Aktiv / Passiv choice to the counterparty picked in the list.
  const handleSetActive = async (next: boolean) => {
    if (!selected || selected.isActive === next) return;
    const question = next
      ? `"${selected.name}" yenidən aktiv edilsin?`
      : `"${selected.name}" passiv edilsin? Siyahılarda görünməyəcək, sonra yenidən aktiv etmək olar.`;
    if (!window.confirm(question)) return;
    try {
      await updateCounterparty(selected.id, {
        name: selected.name,
        phoneNumber: selected.phoneNumber,
        categoryId: selected.categoryId,
        isActive: next,
      });
      toast.success(next ? "Konturagent aktiv edildi." : "Konturagent passiv edildi.");
      await load();
    } catch (e) {
      toast.error(e instanceof Error ? e.message : "Status dəyişdirilmədi.");
    }
  };

  const handleDelete = async (row: CounterpartyRow) => {
    if (row.isBank) {
      toast.error("Sabit bank silinə bilməz.");
      return;
    }
    if (!window.confirm(`"${row.name}" konturagentini silmək istəyirsiniz?`)) return;
    try {
      await deleteCounterparty(row.counterpartyId);
      toast.success("Konturagent silindi.");
      await load();
    } catch (e) {
      toast.error(e instanceof Error ? e.message : "Silinmədi.");
    }
  };

  const categoryNameById = (id: string) => categories.find((c) => String(c.id) === id)?.name ?? "";

  const proceedToConfirm = () => {
    if (!name.trim()) {
      toast.error("Ad vacibdir.");
      return;
    }
    const catId = Number(categoryId);
    if (!Number.isFinite(catId) || catId <= 0) {
      toast.error("Kateqoriya seçilməlidir.");
      return;
    }
    setConfirmStep(true);
  };

  const handleSave = async () => {
    const catId = Number(categoryId);
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

  const openHistoryDialog = (row: CounterpartyRow) => {
    const target = list.find((c) => c.id === row.counterpartyId);
    if (target) setHistoryTarget(target);
  };

  const openDebtDialog = (row: CounterpartyRow) => {
    const target = list.find((c) => c.id === row.counterpartyId);
    if (!target) return;
    setDebtTarget(target);
    setDebtInput(String(target.currentDebtAmount));
    setDebtConfirmStep(false);
    setDebtDialogOpen(true);
  };

  const proceedToDebtConfirm = () => {
    const amount = Number(debtInput);
    if (!Number.isFinite(amount)) {
      toast.error("Məbləğ düzgün deyil.");
      return;
    }
    setDebtConfirmStep(true);
  };

  const handleSaveDebt = async () => {
    if (!debtTarget) return;
    const amount = Number(debtInput);
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
        isBank: c.isBank,
        debt: c.currentDebtAmount,
      })),
    [list],
  );

  // Only the fixed categories are offered; a counterparty still sitting in an old custom one keeps it visible.
  const categoryChoices = useMemo(
    () =>
      categories
        .filter((c) => fixedCategoryIndex(c.name) >= 0 || String(c.id) === categoryId)
        .sort((a, b) => {
          const ia = fixedCategoryIndex(a.name);
          const ib = fixedCategoryIndex(b.name);
          return (ia < 0 ? 99 : ia) - (ib < 0 ? 99 : ib);
        }),
    [categories, categoryId],
  );
  const filterCategories = useMemo(() => categoryChoices.filter((c) => fixedCategoryIndex(c.name) >= 0), [categoryChoices]);
  const selected = list.find((c) => c.id === selectedId) ?? null;

  const activeCount = rows.filter((r) => r.isActive).length;
  const passiveCount = rows.length - activeCount;
  const visibleRows = useMemo(
    () =>
      rows.filter(
        (r) =>
          (statusView === "active" ? r.isActive : !r.isActive) &&
          (categoryView === "all" || String(r.categoryId) === categoryView),
      ),
    [rows, statusView, categoryView],
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
    ],
    [],
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
          onClick={() => openHistoryDialog(row)}
          title="Borc və tarixçə"
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

      <div className="space-y-3 rounded-md border bg-card p-4">
        <div className="flex flex-wrap items-center gap-x-6 gap-y-2">
          <span className="w-44 text-sm font-medium">Siyahı</span>
          <RadioGroup
            className="flex flex-wrap gap-x-6 gap-y-2"
            value={statusView}
            onValueChange={(v) => setStatusView(v as "active" | "passive")}
          >
            <div className="flex items-center gap-2">
              <RadioGroupItem id="view-active" value="active" />
              <Label htmlFor="view-active" className="font-normal">Aktiv ({activeCount})</Label>
            </div>
            <div className="flex items-center gap-2">
              <RadioGroupItem id="view-passive" value="passive" />
              <Label htmlFor="view-passive" className="font-normal">Passiv ({passiveCount})</Label>
            </div>
          </RadioGroup>
        </div>

        <div className="flex flex-wrap items-center gap-x-6 gap-y-2">
          <span className="w-44 text-sm font-medium">Kateqoriya</span>
          <RadioGroup className="flex flex-wrap gap-x-6 gap-y-2" value={categoryView} onValueChange={setCategoryView}>
            <div className="flex items-center gap-2">
              <RadioGroupItem id="view-cat-all" value="all" />
              <Label htmlFor="view-cat-all" className="font-normal">Hamısı</Label>
            </div>
            {filterCategories.map((c) => (
              <div key={c.id} className="flex items-center gap-2">
                <RadioGroupItem id={`view-cat-${c.id}`} value={String(c.id)} />
                <Label htmlFor={`view-cat-${c.id}`} className="font-normal">{c.name}</Label>
              </div>
            ))}
          </RadioGroup>
        </div>

        <div className="flex flex-wrap items-center gap-x-6 gap-y-2 border-t pt-3">
          <span className="w-44 text-sm font-medium">
            Seçilmiş konturagent
            <span className="block text-xs font-normal text-muted-foreground">
              {selected ? selected.name : "Siyahıdan birini seçin"}
            </span>
          </span>
          <RadioGroup
            className="flex flex-wrap gap-x-6 gap-y-2"
            value={selected ? (selected.isActive ? "active" : "passive") : ""}
            onValueChange={(v) => void handleSetActive(v === "active")}
            disabled={!selected}
          >
            <div className="flex items-center gap-2">
              <RadioGroupItem id="sel-active" value="active" />
              <Label htmlFor="sel-active" className="font-normal">Aktiv</Label>
            </div>
            <div className="flex items-center gap-2">
              <RadioGroupItem id="sel-passive" value="passive" />
              <Label htmlFor="sel-passive" className="font-normal">Passiv</Label>
            </div>
          </RadioGroup>
          {selected && (
            <span className="text-xs text-muted-foreground">
              Passiv edilən siyahılarda görünmür; "Passiv" siyahısından yenidən aktiv edə bilərsiniz.
            </span>
          )}
        </div>
      </div>

      <Dialog
        open={dialogOpen}
        onOpenChange={(o) => {
          setDialogOpen(o);
          if (!o) resetForm();
        }}
      >
        <AdvancedTableFilters defs={filterDefs} data={visibleRows}>
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
              onRowClick={(row) => setSelectedId(row.counterpartyId)}
              selectedId={selectedId == null ? null : String(selectedId)}
            />
          )}
        </AdvancedTableFilters>

        <DialogContent className="sm:max-w-lg">
          <DialogHeader>
            <DialogTitle>
              {confirmStep
                ? "Yoxlayın və təsdiqləyin"
                : editingId != null
                  ? "Konturagenti redaktə et"
                  : "Konturagent əlavə et"}
            </DialogTitle>
            <DialogDescription>
              {confirmStep ? "Aşağıdakı məlumatlar saxlanılacaq." : "Ad, telefon, status və kateqoriya."}
            </DialogDescription>
          </DialogHeader>

          {confirmStep ? (
            <div className="space-y-2 rounded-md border p-3 text-sm">
              <div className="flex justify-between">
                <span className="text-muted-foreground">Ad</span>
                <span className="font-medium">{name.trim()}</span>
              </div>
              <div className="flex justify-between">
                <span className="text-muted-foreground">Telefon</span>
                <span className="font-medium">{phoneNumber.trim() || "—"}</span>
              </div>
              <div className="flex justify-between">
                <span className="text-muted-foreground">Kateqoriya</span>
                <span className="font-medium">{categoryNameById(categoryId) || "—"}</span>
              </div>
              <div className="flex justify-between">
                <span className="text-muted-foreground">Status</span>
                <span className="font-medium">{isActive ? "Aktiv" : "Passiv"}</span>
              </div>
            </div>
          ) : (
            <div className="space-y-3">
              <div>
                <Label htmlFor="cp-name">Ad</Label>
                <Input
                  id="cp-name"
                  className="mt-1"
                  value={name}
                  disabled={editingIsBank}
                  onChange={(e) => setName(e.target.value)}
                />
                {editingIsBank && (
                  <p className="mt-1 text-xs text-muted-foreground">
                    Sabit bank POS-da kart ödənişində göstərilir. Adı və kateqoriyası dəyişdirilə bilməz, silinə bilməz.
                  </p>
                )}
              </div>
              <div>
                <Label htmlFor="cp-phone">Telefon</Label>
                <Input id="cp-phone" className="mt-1" value={phoneNumber} onChange={(e) => setPhoneNumber(e.target.value)} placeholder="Opsional" />
              </div>
              <div className="grid grid-cols-2 gap-3">
                <div className="rounded-md border p-3">
                  <p className="mb-2 text-sm font-medium">Status</p>
                  <RadioGroup
                    value={isActive ? "active" : "passive"}
                    onValueChange={(v) => setIsActive(v === "active")}
                  >
                    <div className="flex items-center gap-2">
                      <RadioGroupItem id="cp-status-active" value="active" />
                      <Label htmlFor="cp-status-active" className="font-normal">Aktiv</Label>
                    </div>
                    <div className="flex items-center gap-2">
                      <RadioGroupItem id="cp-status-passive" value="passive" />
                      <Label htmlFor="cp-status-passive" className="font-normal">Passiv</Label>
                    </div>
                  </RadioGroup>
                  <p className="mt-3 text-xs text-muted-foreground">
                    Passiv olanda siyahılarda görünmür. Lazım olanda burdan yenidən aktiv edə bilərsiniz.
                  </p>
                </div>

                <div className="rounded-md border p-3">
                  <p className="mb-2 text-sm font-medium">Kateqoriya</p>
                  <RadioGroup value={categoryId} onValueChange={setCategoryId} disabled={editingIsBank}>
                    {categoryChoices.map((c) => (
                      <div key={c.id} className="flex items-center gap-2">
                        <RadioGroupItem id={`cp-category-${c.id}`} value={String(c.id)} />
                        <Label htmlFor={`cp-category-${c.id}`} className="font-normal">{c.name}</Label>
                      </div>
                    ))}
                  </RadioGroup>
                  {categories.length === 0 && (
                    <p className="text-xs text-muted-foreground">Kateqoriyalar yüklənir…</p>
                  )}
                </div>
              </div>
            </div>
          )}

          <div className="mt-4 flex justify-end gap-2">
            {confirmStep ? (
              <>
                <Button variant="outline" onClick={() => setConfirmStep(false)} disabled={saving}>
                  İmtina et
                </Button>
                <Button onClick={() => void handleSave()} disabled={saving}>
                  {saving ? "Saxlanılır…" : "Təsdiqlə"}
                </Button>
              </>
            ) : (
              <>
                <Button variant="outline" onClick={() => setDialogOpen(false)} disabled={saving}>
                  Ləğv et
                </Button>
                <Button onClick={proceedToConfirm} disabled={saving}>
                  Davam et
                </Button>
              </>
            )}
          </div>
        </DialogContent>
      </Dialog>

      <CounterpartyDebtDialog
        open={historyTarget !== null}
        onOpenChange={(o) => !o && setHistoryTarget(null)}
        counterparty={historyTarget}
        onChanged={() => void load()}
        onAdjust={(c) => {
          setDebtTarget(c);
          setDebtInput(String(c.currentDebtAmount));
          setDebtConfirmStep(false);
          setDebtDialogOpen(true);
        }}
      />

      <Dialog open={debtDialogOpen} onOpenChange={setDebtDialogOpen}>
        <DialogContent className="sm:max-w-xs">
          <DialogHeader>
            <DialogTitle>{debtConfirmStep ? "Borc dəyişikliyini təsdiqləyin" : "Borcu düzəlt"}</DialogTitle>
            <DialogDescription>{debtTarget?.name}</DialogDescription>
          </DialogHeader>
          {debtConfirmStep ? (
            <div className="rounded-md border p-3 text-sm">
              <div className="flex justify-between">
                <span className="text-muted-foreground">Yeni cari borc</span>
                <span className="font-medium">{Number(debtInput).toFixed(2)} ₼</span>
              </div>
            </div>
          ) : (
            <div>
              <Label htmlFor="cp-debt">Cari borc (₼)</Label>
              <Input id="cp-debt" type="number" step="0.01" className="mt-1" value={debtInput} onChange={(e) => setDebtInput(e.target.value)} />
            </div>
          )}
          <div className="mt-4 flex justify-end gap-2">
            {debtConfirmStep ? (
              <>
                <Button variant="outline" onClick={() => setDebtConfirmStep(false)} disabled={savingDebt}>
                  İmtina et
                </Button>
                <Button onClick={() => void handleSaveDebt()} disabled={savingDebt}>
                  {savingDebt ? "Saxlanılır…" : "Təsdiqlə"}
                </Button>
              </>
            ) : (
              <>
                <Button variant="outline" onClick={() => setDebtDialogOpen(false)} disabled={savingDebt}>
                  Ləğv et
                </Button>
                <Button onClick={proceedToDebtConfirm} disabled={savingDebt}>
                  Davam et
                </Button>
              </>
            )}
          </div>
        </DialogContent>
      </Dialog>
    </div>
  );
}
