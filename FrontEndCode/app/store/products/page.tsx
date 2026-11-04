"use client";

import { useCallback, useEffect, useRef, useState } from "react";
import { Download, Eye, EyeOff, Pencil, Plus, Search, Upload } from "lucide-react";
import { toast } from "sonner";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from "@/components/ui/dialog";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import BranchPricesDialog from "@/components/store/branch-prices-dialog";
import CategoriesDialog from "@/components/store/categories-dialog";
import { Textarea } from "@/components/ui/textarea";
import { useStorePermissions as usePermissionSet } from "@/lib/store/use-store-permissions";
import { formatCurrency } from "@/lib/format-currency";
import {
  UNIT_LABELS,
  UnitOfMeasure,
  downloadStoreProductsCsv,
  getStoreCategories,
  importStoreProductsCsv,
  saveStoreCategory,
  saveStoreProduct,
  setStoreProductsBonus,
  searchStoreProducts,
  type StoreCategory,
  type StoreProduct,
  type StoreProductImportResult,
  type UnitOfMeasureValue,
} from "@/lib/store/store-service";

const SELECT = "h-9 w-full rounded-md border border-input bg-background px-3 text-sm";
const PAGE_SIZE = 25;

type Form = {
  name: string;
  sku: string;
  barcodes: string;
  categoryId: number | "";
  brand: string;
  unit: UnitOfMeasureValue;
  purchasePrice: string;
  salePrice: string;
  wholesalePrice: string;
  campaignPrice: string;
  campaignFrom: string;
  campaignTo: string;
  vatPercent: string;
  minimumStock: string;
  tracksSerial: boolean;
  earnsBonus: boolean;
  isActive: boolean;
};

const EMPTY: Form = {
  name: "",
  sku: "",
  barcodes: "",
  categoryId: "",
  brand: "",
  unit: UnitOfMeasure.Piece,
  purchasePrice: "",
  salePrice: "",
  wholesalePrice: "",
  campaignPrice: "",
  campaignFrom: "",
  campaignTo: "",
  vatPercent: "",
  minimumStock: "0",
  tracksSerial: false,
  earnsBonus: true,
  isActive: true,
};

const num = (s: string): number => Number(s.replace(",", ".")) || 0;
const numOrNull = (s: string): number | null => (s.trim() === "" ? null : Number(s.replace(",", ".")));

export default function StoreProductsPage() {
  const canManage = usePermissionSet().has("StoreProduct.Manage");

  const importRef = useRef<HTMLInputElement>(null);
  const [branchPriceFor, setBranchPriceFor] = useState<StoreProduct | null>(null);
  const [importResult, setImportResult] = useState<StoreProductImportResult | null>(null);
  const [items, setItems] = useState<StoreProduct[]>([]);
  const [total, setTotal] = useState(0);
  // Purchase / sale prices and the minimum stock stay hidden until opened.
  const [showDetails, setShowDetails] = useState(false);
  const [page, setPage] = useState(1);
  const [q, setQ] = useState("");
  const [categoryFilter, setCategoryFilter] = useState<number | "">("");
  const [categories, setCategories] = useState<StoreCategory[]>([]);
  const [loading, setLoading] = useState(true);

  const [open, setOpen] = useState(false);
  const [saving, setSaving] = useState(false);
  const [editingId, setEditingId] = useState<number | null>(null);
  const [form, setForm] = useState<Form>(EMPTY);

  const [categoryOpen, setCategoryOpen] = useState(false);
  const [categoriesManageOpen, setCategoriesManageOpen] = useState(false);
  const [selected, setSelected] = useState<Set<number>>(new Set());
  const [bulkBusy, setBulkBusy] = useState(false);
  const [categoryName, setCategoryName] = useState("");

  const loadCategories = useCallback(() => {
    getStoreCategories().then(setCategories).catch(() => setCategories([]));
  }, []);

  const load = useCallback(async () => {
    setLoading(true);
    try {
      const res = await searchStoreProducts({
        q: q.trim() || undefined,
        categoryId: categoryFilter === "" ? null : categoryFilter,
        page,
        pageSize: PAGE_SIZE,
      });
      setItems(res.items);
      setTotal(res.total);
    } catch (e) {
      toast.error(e instanceof Error ? e.message : "Məhsullar yüklənmədi.");
    } finally {
      setLoading(false);
    }
  }, [q, categoryFilter, page]);

  useEffect(() => {
    loadCategories();
  }, [loadCategories]);

  // Typing in the search box waits a moment so it does not call the API on every key.
  useEffect(() => {
    const t = setTimeout(() => void load(), 250);
    return () => clearTimeout(t);
  }, [load]);

  const openDialog = (p: StoreProduct | null) => {
    setEditingId(p?.id ?? null);
    setForm(
      p
        ? {
            name: p.name,
            sku: p.sku ?? "",
            barcodes: p.barcodes.join("\n"),
            categoryId: p.categoryId ?? "",
            brand: p.brand ?? "",
            unit: p.unit,
            purchasePrice: String(p.purchasePrice),
            salePrice: String(p.salePrice),
            wholesalePrice: p.wholesalePrice == null ? "" : String(p.wholesalePrice),
            campaignPrice: p.campaignPrice == null ? "" : String(p.campaignPrice),
            campaignFrom: p.campaignFromUtc ? p.campaignFromUtc.slice(0, 10) : "",
            campaignTo: p.campaignToUtc ? p.campaignToUtc.slice(0, 10) : "",
            vatPercent: p.vatPercent == null ? "" : String(p.vatPercent),
            minimumStock: String(p.minimumStock),
            tracksSerial: p.tracksSerial,
            earnsBonus: p.earnsBonus,
            isActive: p.isActive,
          }
        : EMPTY,
    );
    setOpen(true);
  };

  const set = <K extends keyof Form>(key: K, value: Form[K]) => setForm((f) => ({ ...f, [key]: value }));

  const submit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!form.name.trim()) {
      toast.error("Məhsulun adını yazın.");
      return;
    }
    setSaving(true);
    try {
      await saveStoreProduct(editingId, {
        name: form.name.trim(),
        sku: form.sku.trim() || null,
        barcodes: form.barcodes.split(/[\n,;]+/).map((b) => b.trim()).filter(Boolean),
        categoryId: form.categoryId === "" ? null : form.categoryId,
        brand: form.brand.trim() || null,
        unit: form.unit,
        purchasePrice: num(form.purchasePrice),
        salePrice: num(form.salePrice),
        wholesalePrice: numOrNull(form.wholesalePrice),
        campaignPrice: numOrNull(form.campaignPrice),
        campaignFromUtc: form.campaignFrom ? `${form.campaignFrom}T00:00:00Z` : null,
        campaignToUtc: form.campaignTo ? `${form.campaignTo}T23:59:59Z` : null,
        vatPercent: numOrNull(form.vatPercent),
        minimumStock: num(form.minimumStock),
        tracksSerial: form.tracksSerial,
        earnsBonus: form.earnsBonus,
        imageUrl: null,
        isActive: form.isActive,
      });
      toast.success("Məhsul yazıldı");
      setOpen(false);
      void load();
    } catch (err) {
      toast.error(err instanceof Error ? err.message : "Məhsul yazılmadı.");
    } finally {
      setSaving(false);
    }
  };

  const addCategory = async () => {
    if (!categoryName.trim()) return;
    try {
      const created = await saveStoreCategory(null, { name: categoryName.trim(), parentCategoryId: null, isActive: true });
      toast.success("Kateqoriya yaradıldı");
      setCategoryName("");
      setCategoryOpen(false);
      loadCategories();
      setForm((f) => ({ ...f, categoryId: created.id }));
    } catch (err) {
      toast.error(err instanceof Error ? err.message : "Kateqoriya yazılmadı.");
    }
  };

  const toggleSelected = (id: number) =>
    setSelected((prev) => {
      const next = new Set(prev);
      if (next.has(id)) next.delete(id);
      else next.add(id);
      return next;
    });
  const allOnPage = items.length > 0 && items.every((p) => selected.has(p.id));

  // Bonus on / off for the ticked products, or for everything the current search and category show.
  const applyBonus = async (earnsBonus: boolean, scope: "selected" | "filter") => {
    setBulkBusy(true);
    try {
      const res =
        scope === "selected"
          ? await setStoreProductsBonus({ productIds: Array.from(selected), earnsBonus })
          : await setStoreProductsBonus({ term: q.trim() || undefined, categoryId: categoryFilter === "" ? null : categoryFilter, earnsBonus });
      toast.success(`${res.changed} məhsul: ${earnsBonus ? "bonus verilir" : "bonus verilmir"}.`);
      setSelected(new Set());
      void load();
    } catch (e) {
      toast.error(e instanceof Error ? e.message : "Bonus dəyişdirilmədi.");
    } finally {
      setBulkBusy(false);
    }
  };

  const pages = Math.max(1, Math.ceil(total / PAGE_SIZE));

  return (
    <div className="mx-auto max-w-6xl space-y-4 p-4">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <h1 className="text-2xl font-bold">Məhsullar</h1>
        <div className="flex gap-2">
          <Button variant="outline" onClick={() => setCategoriesManageOpen(true)}>
            Kateqoriyalar
          </Button>
          <Button variant="outline" onClick={() => void downloadStoreProductsCsv().catch((e) => toast.error(e instanceof Error ? e.message : "Fayl yüklənmədi"))}>
            <Download className="mr-1 h-4 w-4" />
            Excel/CSV ixrac
          </Button>
          {canManage && (
            <>
              <input
                ref={importRef}
                type="file"
                accept=".csv,text/csv"
                className="hidden"
                onChange={(e) => {
                  const file = e.target.files?.[0];
                  e.target.value = "";
                  if (!file) return;
                  void importStoreProductsCsv(file)
                    .then((r) => {
                      setImportResult(r);
                      void load();
                    })
                    .catch((err) => toast.error(err instanceof Error ? err.message : "İdxal alınmadı"));
                }}
              />
              <Button variant="outline" onClick={() => importRef.current?.click()}>
                <Upload className="mr-1 h-4 w-4" />
                CSV idxal
              </Button>
              <Button onClick={() => openDialog(null)}>
                <Plus className="mr-1 h-4 w-4" />
                Yeni məhsul
              </Button>
            </>
          )}
        </div>
      </div>

      <Dialog open={importResult != null} onOpenChange={(o) => !o && setImportResult(null)}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>İdxal nəticəsi</DialogTitle>
            <DialogDescription>
              Yaradıldı: {importResult?.created ?? 0}, yeniləndi: {importResult?.updated ?? 0}, xəta: {importResult?.errors.length ?? 0}. Sütunlar ixrac faylındakı kimidir; məhsul SKU, sonra barkod ilə tapılır.
            </DialogDescription>
          </DialogHeader>
          {importResult && importResult.errors.length > 0 && (
            <ul className="max-h-64 list-disc space-y-1 overflow-auto pl-5 text-sm text-red-600">
              {importResult.errors.map((e) => (
                <li key={e}>{e}</li>
              ))}
            </ul>
          )}
        </DialogContent>
      </Dialog>

      <div className="flex flex-wrap gap-2">
        <div className="relative min-w-60 flex-1">
          <Search className="absolute left-2.5 top-2.5 h-4 w-4 text-muted-foreground" />
          <Input
            className="pl-8"
            placeholder="Ad, SKU və ya barkod…"
            value={q}
            onChange={(e) => {
              setQ(e.target.value);
              setPage(1);
            }}
          />
        </div>
        <select
          className="h-9 rounded-md border border-input bg-background px-3 text-sm"
          value={categoryFilter}
          onChange={(e) => {
            setCategoryFilter(e.target.value ? Number(e.target.value) : "");
            setPage(1);
          }}
        >
          <option value="">Bütün kateqoriyalar</option>
          {categories.map((c) => (
            <option key={c.id} value={c.id}>
              {c.name}
            </option>
          ))}
        </select>
      </div>

      {canManage && (selected.size > 0 || q.trim() !== "" || categoryFilter !== "") && (
        <div className="flex flex-wrap items-center gap-2 rounded-lg border bg-card px-3 py-2 text-sm">
          <span className="text-muted-foreground">{selected.size > 0 ? `${selected.size} məhsul seçilib:` : "Bonus:"}</span>
          {selected.size > 0 && (
            <>
              <Button size="sm" variant="outline" disabled={bulkBusy} onClick={() => void applyBonus(false, "selected")}>
                Seçilənlərə bonus verilmir
              </Button>
              <Button size="sm" variant="outline" disabled={bulkBusy} onClick={() => void applyBonus(true, "selected")}>
                Seçilənlərə bonus verilir
              </Button>
              <Button size="sm" variant="ghost" onClick={() => setSelected(new Set())}>
                Seçimi təmizlə
              </Button>
            </>
          )}
          {(q.trim() !== "" || categoryFilter !== "") && (
            <>
              <span className="mx-1 text-muted-foreground">|</span>
              <Button size="sm" variant="outline" disabled={bulkBusy} onClick={() => void applyBonus(false, "filter")}>
                Filtrə uyğun hamısına bonus verilmir ({total})
              </Button>
              <Button size="sm" variant="outline" disabled={bulkBusy} onClick={() => void applyBonus(true, "filter")}>
                Filtrə uyğun hamısına bonus verilir
              </Button>
            </>
          )}
        </div>
      )}

      <div className="overflow-x-auto rounded-lg border bg-card">
        <table className="w-full text-sm">
          <thead>
            <tr className="bg-muted/40 text-left text-xs text-muted-foreground">
              {canManage && (
                <th className="w-8 px-3 py-2">
                  <input
                    type="checkbox"
                    aria-label="Hamısını seç"
                    checked={allOnPage}
                    onChange={(e) =>
                      setSelected((prev) => {
                        const next = new Set(prev);
                        for (const p of items) {
                          if (e.target.checked) next.add(p.id);
                          else next.delete(p.id);
                        }
                        return next;
                      })
                    }
                  />
                </th>
              )}
              <th className="px-3 py-2 font-medium">Ad</th>
              <th className="px-3 py-2 font-medium">Barkod / SKU</th>
              <th className="px-3 py-2 font-medium">Kateqoriya</th>
              <th className="px-3 py-2 font-medium">Tədarükçü</th>
              {showDetails && (
                <>
                  <th className="px-3 py-2 text-right font-medium">Min. qalıq</th>
                  <th className="px-3 py-2 text-right font-medium">Alış</th>
                  <th className="px-3 py-2 text-right font-medium">Satış</th>
                </>
              )}
              <th className="px-3 py-2 font-medium">Vahid</th>
              <th className="px-3 py-2 text-right">
                <Button size="sm" variant="ghost" onClick={() => setShowDetails((v) => !v)} title="Qiymətləri və minimal qalığı göstər / gizlət">
                  {showDetails ? <EyeOff className="mr-1 h-4 w-4" /> : <Eye className="mr-1 h-4 w-4" />}
                  Qiymət və qalıq
                </Button>
              </th>
            </tr>
          </thead>
          <tbody>
            {loading ? (
              <tr>
                <td colSpan={showDetails ? 11 : 8} className="px-3 py-8 text-center text-muted-foreground">
                  Yüklənir…
                </td>
              </tr>
            ) : items.length === 0 ? (
              <tr>
                <td colSpan={showDetails ? 11 : 8} className="px-3 py-8 text-center text-muted-foreground">
                  Məhsul tapılmadı.
                </td>
              </tr>
            ) : (
              items.map((p) => (
                <tr key={p.id} className="border-t">
                  {canManage && (
                    <td className="px-3 py-2">
                      <input type="checkbox" aria-label="Seç" checked={selected.has(p.id)} onChange={() => toggleSelected(p.id)} />
                    </td>
                  )}
                  <td className="px-3 py-2 font-medium">
                    {p.name} {!p.isActive && <Badge variant="outline">Passiv</Badge>}
                    {!p.earnsBonus && <Badge variant="destructive" className="ml-1">bonus yoxdur</Badge>}
                  </td>
                  <td className="px-3 py-2 text-muted-foreground">
                    {p.primaryBarcode ?? "—"}
                    {p.sku && <span className="block text-xs">SKU: {p.sku}</span>}
                  </td>
                  <td className="px-3 py-2">{p.categoryName ?? "—"}</td>
                  <td className="px-3 py-2">{p.supplierName ?? "—"}</td>
                  {showDetails && (
                    <>
                      <td className="px-3 py-2 text-right">{p.minimumStock}</td>
                      <td className="px-3 py-2 text-right">{formatCurrency(p.purchasePrice)}</td>
                      <td className="px-3 py-2 text-right font-medium">{formatCurrency(p.salePrice)}</td>
                    </>
                  )}
                  <td className="px-3 py-2">{UNIT_LABELS[p.unit]}</td>
                  <td className="px-3 py-2 text-right">
                    {canManage && (
                      <>
                        <Button size="sm" variant="ghost" onClick={() => setBranchPriceFor(p)}>
                          Filial qiymətləri
                        </Button>
                        <Button size="sm" variant="ghost" onClick={() => openDialog(p)}>
                          <Pencil className="h-4 w-4" />
                        </Button>
                      </>
                    )}
                  </td>
                </tr>
              ))
            )}
          </tbody>
        </table>
      </div>

      <div className="flex items-center justify-between text-sm text-muted-foreground">
        <span>Cəmi: {total}</span>
        <div className="flex items-center gap-2">
          <Button size="sm" variant="outline" disabled={page <= 1} onClick={() => setPage((p) => p - 1)}>
            Əvvəlki
          </Button>
          <span>
            {page} / {pages}
          </span>
          <Button size="sm" variant="outline" disabled={page >= pages} onClick={() => setPage((p) => p + 1)}>
            Növbəti
          </Button>
        </div>
      </div>

      <BranchPricesDialog product={branchPriceFor} onClose={() => setBranchPriceFor(null)} />
      <CategoriesDialog open={categoriesManageOpen} onClose={() => setCategoriesManageOpen(false)} onChanged={loadCategories} />

      <Dialog open={open} onOpenChange={(o) => !o && !saving && setOpen(false)}>
        <DialogContent className="max-h-[90vh] overflow-y-auto sm:max-w-xl">
          <DialogHeader>
            <DialogTitle>{editingId ? "Məhsulu dəyiş" : "Yeni məhsul"}</DialogTitle>
            <DialogDescription>Qiymətlər ƏDV daxil olmaqla yazılır.</DialogDescription>
          </DialogHeader>
          <form className="space-y-4" onSubmit={submit}>
            <div className="space-y-2">
              <Label htmlFor="p-name">Ad</Label>
              <Input id="p-name" value={form.name} onChange={(e) => set("name", e.target.value)} autoFocus />
            </div>
            <div className="grid grid-cols-2 gap-3">
              <div className="space-y-2">
                <Label htmlFor="p-sku">SKU</Label>
                <Input id="p-sku" value={form.sku} onChange={(e) => set("sku", e.target.value)} />
              </div>
              <div className="space-y-2">
                <Label htmlFor="p-brand">Brend</Label>
                <Input id="p-brand" value={form.brand} onChange={(e) => set("brand", e.target.value)} />
              </div>
            </div>
            <div className="space-y-2">
              <Label htmlFor="p-codes">Barkodlar (hər sətirdə bir)</Label>
              <Textarea id="p-codes" rows={2} value={form.barcodes} onChange={(e) => set("barcodes", e.target.value)} />
              <p className="text-xs text-muted-foreground">Birinci barkod əsas barkod sayılır.</p>
            </div>
            <div className="grid grid-cols-2 gap-3">
              <div className="space-y-2">
                <Label htmlFor="p-cat">Kateqoriya</Label>
                <div className="flex gap-2">
                  <select
                    id="p-cat"
                    className={SELECT}
                    value={form.categoryId}
                    onChange={(e) => set("categoryId", e.target.value ? Number(e.target.value) : "")}
                  >
                    <option value="">—</option>
                    {categories.map((c) => (
                      <option key={c.id} value={c.id}>
                        {c.name}
                      </option>
                    ))}
                  </select>
                  <Button type="button" variant="outline" size="icon" onClick={() => setCategoryOpen(true)} title="Yeni kateqoriya">
                    <Plus className="h-4 w-4" />
                  </Button>
                </div>
              </div>
              <div className="space-y-2">
                <Label htmlFor="p-unit">Vahid</Label>
                <select
                  id="p-unit"
                  className={SELECT}
                  value={form.unit}
                  onChange={(e) => set("unit", Number(e.target.value) as UnitOfMeasureValue)}
                >
                  {Object.entries(UNIT_LABELS).map(([k, label]) => (
                    <option key={k} value={k}>
                      {label}
                    </option>
                  ))}
                </select>
              </div>
            </div>
            <div className="grid grid-cols-3 gap-3">
              <div className="space-y-2">
                <Label htmlFor="p-buy">Alış qiyməti</Label>
                <Input id="p-buy" inputMode="decimal" value={form.purchasePrice} onChange={(e) => set("purchasePrice", e.target.value)} />
              </div>
              <div className="space-y-2">
                <Label htmlFor="p-sale">Satış qiyməti</Label>
                <Input id="p-sale" inputMode="decimal" value={form.salePrice} onChange={(e) => set("salePrice", e.target.value)} />
              </div>
              <div className="space-y-2">
                <Label htmlFor="p-whole">Topdan qiymət</Label>
                <Input id="p-whole" inputMode="decimal" value={form.wholesalePrice} onChange={(e) => set("wholesalePrice", e.target.value)} />
              </div>
            </div>
            <div className="grid grid-cols-3 gap-3">
              <div className="space-y-2">
                <Label htmlFor="p-camp">Kampaniya qiyməti</Label>
                <Input id="p-camp" inputMode="decimal" value={form.campaignPrice} onChange={(e) => set("campaignPrice", e.target.value)} />
              </div>
              <div className="space-y-2">
                <Label htmlFor="p-camp-from">Başlanğıc</Label>
                <Input id="p-camp-from" type="date" value={form.campaignFrom} onChange={(e) => set("campaignFrom", e.target.value)} />
              </div>
              <div className="space-y-2">
                <Label htmlFor="p-camp-to">Son</Label>
                <Input id="p-camp-to" type="date" value={form.campaignTo} onChange={(e) => set("campaignTo", e.target.value)} />
              </div>
            </div>
            <div className="grid grid-cols-2 gap-3">
              <div className="space-y-2">
                <Label htmlFor="p-vat">ƏDV %</Label>
                <Input id="p-vat" inputMode="decimal" placeholder="18" value={form.vatPercent} onChange={(e) => set("vatPercent", e.target.value)} />
              </div>
              <div className="space-y-2">
                <Label htmlFor="p-min">Minimum stok</Label>
                <Input id="p-min" inputMode="decimal" value={form.minimumStock} onChange={(e) => set("minimumStock", e.target.value)} />
              </div>
            </div>
            <label className="flex items-center gap-2 text-sm">
              <input type="checkbox" checked={form.tracksSerial} onChange={(e) => set("tracksSerial", e.target.checked)} />
              IMEI / serial ilə izlənir (hər vahid ayrıca nömrə daşıyır; telefon, texnika)
            </label>
            <label className="flex items-center gap-2 text-sm">
              <input type="checkbox" checked={form.earnsBonus} onChange={(e) => set("earnsBonus", e.target.checked)} />
              Bonus qazandırır (söndürülsə bu məhsulun məbləği bonus bazasına daxil olmur)
            </label>
            <label className="flex items-center gap-2 text-sm">
              <input type="checkbox" checked={form.isActive} onChange={(e) => set("isActive", e.target.checked)} />
              Aktiv (passiv məhsul yeni satışa əlavə olunmur, tarixçədə qalır)
            </label>
            <DialogFooter>
              <Button type="button" variant="outline" disabled={saving} onClick={() => setOpen(false)}>
                Ləğv et
              </Button>
              <Button type="submit" disabled={saving}>
                {saving ? "Yazılır…" : "Yaz"}
              </Button>
            </DialogFooter>
          </form>
        </DialogContent>
      </Dialog>

      <Dialog open={categoryOpen} onOpenChange={setCategoryOpen}>
        <DialogContent className="sm:max-w-xs">
          <DialogHeader>
            <DialogTitle>Yeni kateqoriya</DialogTitle>
          </DialogHeader>
          <Input value={categoryName} onChange={(e) => setCategoryName(e.target.value)} placeholder="Ad" autoFocus />
          <DialogFooter>
            <Button variant="outline" onClick={() => setCategoryOpen(false)}>
              Ləğv et
            </Button>
            <Button onClick={() => void addCategory()}>Yarat</Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}
