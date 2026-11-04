"use client";

import { useCallback, useEffect, useState } from "react";
import { ChevronLeft, ChevronRight, LayoutGrid, MoreHorizontal, Pencil, Plus, Search } from "lucide-react";
import { toast } from "sonner";
import { Badge } from "@/components/ui/badge";
import RoleTemplatesCard from "@/components/store/role-templates-card";
import UserAccessCard from "@/components/store/user-access-card";
import { Button } from "@/components/ui/button";
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from "@/components/ui/dialog";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { useStorePermissions as usePermissionSet } from "@/lib/store/use-store-permissions";
import { formatCurrency } from "@/lib/format-currency";
import { getRestaurants, type Restaurant } from "@/lib/services/restaurant-service";
import { getWarehouses, type Warehouse } from "@/lib/services/warehouse-service";
import {
  getStoreQuickSlots,
  getStoreRegisters,
  getStoreSettings,
  saveStoreQuickSlot,
  saveStoreRegister,
  searchStoreProducts,
  updateStoreSettings,
  type StoreCashRegister,
  type StoreProduct,
  type StoreQuickSlot,
} from "@/lib/store/store-service";

const SELECT = "h-9 w-full rounded-md border border-input bg-background px-3 text-sm";
const COLORS = ["#2563eb", "#16a34a", "#dc2626", "#d97706", "#7c3aed", "#0891b2", "#db2777", "#475569"];

export default function StoreRegistersPage() {
  const permissions = usePermissionSet();
  const canSettings = permissions.has("StoreSettings.Manage");

  const [registers, setRegisters] = useState<StoreCashRegister[]>([]);
  const [branches, setBranches] = useState<Restaurant[]>([]);
  const [warehouses, setWarehouses] = useState<Warehouse[]>([]);
  const [loading, setLoading] = useState(true);
  const [allowNegative, setAllowNegative] = useState(false);
  const [fiscalEnabled, setFiscalEnabled] = useState(false);
  const [receiptWidth, setReceiptWidth] = useState(80);
  const [receiptHeader, setReceiptHeader] = useState("");
  const [receiptFooter, setReceiptFooter] = useState("");
  const [allowReceiptless, setAllowReceiptless] = useState(false);
  const [quickSlotCount, setQuickSlotCount] = useState(50);
  const [maxDiscount, setMaxDiscount] = useState("10");
  const [bonusEarn, setBonusEarn] = useState("0");
  const [bonusMax, setBonusMax] = useState("50");
  const [bonusExpiry, setBonusExpiry] = useState("0");

  const [open, setOpen] = useState(false);
  const [saving, setSaving] = useState(false);
  const [editingId, setEditingId] = useState<number | null>(null);
  const [name, setName] = useState("");
  const [restaurantId, setRestaurantId] = useState<number | "">("");
  const [warehouseId, setWarehouseId] = useState<number | "">("");
  const [isActive, setIsActive] = useState(true);

  // Quick-sale buttons
  const [slotRegister, setSlotRegister] = useState<StoreCashRegister | null>(null);
  const [slotPage, setSlotPage] = useState(0);
  const [slotLoading, setSlotLoading] = useState(false);
  const [slots, setSlots] = useState<StoreQuickSlot[]>([]);
  const [slotEdit, setSlotEdit] = useState<StoreQuickSlot | null>(null);
  const [slotTerm, setSlotTerm] = useState("");
  const [slotOptions, setSlotOptions] = useState<StoreProduct[]>([]);
  const [slotProduct, setSlotProduct] = useState<StoreProduct | null>(null);
  const [slotLabel, setSlotLabel] = useState("");
  const [slotColor, setSlotColor] = useState("");

  const load = useCallback(async () => {
    setLoading(true);
    try {
      setRegisters(await getStoreRegisters());
      getStoreSettings()
        .then((s) => {
          setAllowNegative(s.allowNegativeStock);
          setFiscalEnabled(s.fiscalEnabled);
          setReceiptWidth(s.receiptWidthMm);
          setReceiptHeader(s.receiptHeader ?? "");
          setReceiptFooter(s.receiptFooter ?? "");
          setAllowReceiptless(s.allowReceiptlessSale);
          setQuickSlotCount(s.quickSlotCount);
          setMaxDiscount(String(s.maxDiscountPercent));
          setBonusEarn(String(s.bonusEarnPercent));
          setBonusMax(String(s.bonusMaxPaymentPercent));
          setBonusExpiry(String(s.bonusExpiryDays));
        })
        .catch(() => undefined);
    } catch (e) {
      toast.error(e instanceof Error ? e.message : "Kassalar yüklənmədi.");
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    void load();
    getRestaurants().then(setBranches).catch(() => setBranches([]));
    getWarehouses().then(setWarehouses).catch(() => setWarehouses([]));
  }, [load]);

  const openDialog = (r: StoreCashRegister | null) => {
    setEditingId(r?.id ?? null);
    setName(r?.name ?? "");
    setRestaurantId(r?.restaurantId ?? "");
    setWarehouseId(r?.warehouseId ?? "");
    setIsActive(r?.isActive ?? true);
    setOpen(true);
  };

  const submit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!name.trim() || restaurantId === "" || warehouseId === "") {
      toast.error("Ad, filial və anbar seçilməlidir.");
      return;
    }
    setSaving(true);
    try {
      await saveStoreRegister(editingId, { name: name.trim(), restaurantId, warehouseId, isActive });
      toast.success("Kassa yazıldı");
      setOpen(false);
      void load();
    } catch (err) {
      toast.error(err instanceof Error ? err.message : "Kassa yazılmadı.");
    } finally {
      setSaving(false);
    }
  };

  const saveSettings = async () => {
    const limit = Number(maxDiscount.replace(",", "."));
    const earn = Number(bonusEarn.replace(",", "."));
    const max = Number(bonusMax.replace(",", "."));
    if (![limit, earn, max].every((v) => Number.isFinite(v) && v >= 0 && v <= 100)) {
      toast.error("Faizlər 0 ilə 100 arasında olmalıdır.");
      return;
    }
    try {
      await updateStoreSettings({ quickSlotCount: Math.max(1, Math.floor(Number(quickSlotCount) || 50)), receiptWidthMm: receiptWidth, receiptHeader: receiptHeader.trim() || null, receiptFooter: receiptFooter.trim() || null, allowReceiptlessSale: allowReceiptless, fiscalEnabled, allowNegativeStock: allowNegative, maxDiscountPercent: limit, bonusEarnPercent: earn, bonusMaxPaymentPercent: max, bonusExpiryDays: Math.max(0, Math.floor(Number(bonusExpiry) || 0)) });
      toast.success("Qaydalar yadda saxlanıldı");
    } catch (err) {
      toast.error(err instanceof Error ? err.message : "Yazılmadı.");
    }
  };

  // ---- Quick slots ------------------------------------------------------------------------

  // The quick buttons are one shared set for all tills (100 buttons, 2 pages of 50); any till can be used to reach them.
  const openSlots = async (r: StoreCashRegister) => {
    try {
      setSlots(await getStoreQuickSlots(r.id));
      setSlotPage(0);
      setSlotRegister(r);
    } catch (e) {
      toast.error(e instanceof Error ? e.message : "Düymələr yüklənmədi.");
    }
  };

  const editSlot = (s: StoreQuickSlot) => {
    setSlotEdit(s);
    setSlotTerm("");
    setSlotOptions([]);
    setSlotProduct(null);
    setSlotLabel(s.label ?? "");
    setSlotColor(s.color ?? "");
  };

  const searchSlotProducts = (all: boolean) => {
    setSlotLoading(true);
    searchStoreProducts({ q: all ? undefined : slotTerm.trim() || undefined, pageSize: 1000, isActive: true })
      .then((r) => setSlotOptions(r.items))
      .catch(() => setSlotOptions([]))
      .finally(() => setSlotLoading(false));
  };

  useEffect(() => {
    if (!slotEdit || slotProduct || slotTerm.trim().length < 1) {
      setSlotOptions([]);
      return;
    }
    const t = setTimeout(() => {
      searchStoreProducts({ q: slotTerm.trim(), pageSize: 50, isActive: true })
        .then((r) => setSlotOptions(r.items))
        .catch(() => setSlotOptions([]));
    }, 200);
    return () => clearTimeout(t);
  }, [slotEdit, slotTerm, slotProduct]);

  const saveSlot = async (clear: boolean) => {
    if (!slotRegister || !slotEdit) return;
    const productId = clear ? null : (slotProduct?.id ?? slotEdit.productId);
    if (!clear && productId == null) {
      toast.error("Məhsulu seçin.");
      return;
    }
    try {
      setSlots(
        await saveStoreQuickSlot(slotRegister.id, {
          slotNumber: slotEdit.slotNumber,
          productId,
          label: clear ? null : slotLabel.trim() || null,
          color: clear ? null : slotColor || null,
        }),
      );
      setSlotEdit(null);
    } catch (e) {
      toast.error(e instanceof Error ? e.message : "Düymə yazılmadı.");
    }
  };

  const branchWarehouses = warehouses.filter((w) => w.restaurantId === restaurantId);

  return (
    <div className="mx-auto max-w-4xl space-y-6 p-4">
      <div className="flex items-start justify-between gap-3">
        <div>
          <h1 className="text-2xl font-bold">Kassalar</h1>
          <p className="text-sm text-muted-foreground">Hər kassa bir filiala və satış etdiyi bir anbara bağlıdır.</p>
        </div>
        <div className="flex gap-2">
          <Button variant="outline" disabled={registers.length === 0} onClick={() => registers[0] && void openSlots(registers[0])}>
            <LayoutGrid className="mr-1 h-4 w-4" />
            Sürətli düymələr ({quickSlotCount})
          </Button>
          <Button onClick={() => openDialog(null)}>
            <Plus className="mr-1 h-4 w-4" />
            Yeni kassa
          </Button>
        </div>
      </div>

      {loading ? (
        <p className="text-sm text-muted-foreground">Yüklənir…</p>
      ) : registers.length === 0 ? (
        <p className="text-sm text-muted-foreground">Hələ kassa yoxdur. Satışa başlamaq üçün ən azı bir kassa yaradın.</p>
      ) : (
        <div className="overflow-x-auto rounded-lg border bg-card">
          <table className="w-full text-sm">
            <thead>
              <tr className="bg-muted/40 text-left text-xs text-muted-foreground">
                <th className="px-3 py-2 font-medium">Kassa</th>
                <th className="px-3 py-2 font-medium">Filial</th>
                <th className="px-3 py-2 font-medium">Anbar</th>
                <th className="px-3 py-2 font-medium">Növbə</th>
                <th className="px-3 py-2" />
              </tr>
            </thead>
            <tbody>
              {registers.map((r) => (
                <tr key={r.id} className="border-t">
                  <td className="px-3 py-2 font-medium">
                    {r.name} {!r.isActive && <Badge variant="outline">Passiv</Badge>}
                  </td>
                  <td className="px-3 py-2">{r.restaurantName}</td>
                  <td className="px-3 py-2">{r.warehouseName}</td>
                  <td className="px-3 py-2">
                    {r.openShiftId ? <Badge>Açıq — {r.openShiftCashierName ?? "kassir"}</Badge> : <span className="text-muted-foreground">Bağlı</span>}
                  </td>
                  <td className="px-3 py-2 text-right">
                    <Button size="sm" variant="ghost" onClick={() => openDialog(r)}>
                      <Pencil className="h-4 w-4" />
                    </Button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {canSettings && (
        <div className="space-y-3 rounded-lg border bg-card p-4">
          <h2 className="font-semibold">Satış qaydaları</h2>
          <label className="flex items-center gap-2 text-sm">
            <input type="checkbox" checked={fiscalEnabled} onChange={(e) => setFiscalEnabled(e.target.checked)} />
            Satışlar fiskal cihaza göndərilsin (cihaz adapteri qoşulmayıbsa satış yenə də keçir, statusu “Xəta” olur və sonra yenidən göndərilir)
          </label>
          <label className="flex items-center gap-2 text-sm">
            <input type="checkbox" checked={allowNegative} onChange={(e) => setAllowNegative(e.target.checked)} />
            Mənfi stoka icazə ver (stokda olmayan məhsulu da satmaq olar)
          </label>
          <p className="-mt-2 text-xs text-muted-foreground">Standart: söndürülüb — stokdan çox satış bloklanır.</p>
          <div className="flex items-end gap-2">
            <div className="space-y-1">
              <Label htmlFor="max-disc">Menecer təsdiqi olmadan maksimum endirim (%)</Label>
              <Input id="max-disc" className="w-32" inputMode="decimal" value={maxDiscount} onChange={(e) => setMaxDiscount(e.target.value)} />
            </div>
            <div className="space-y-1">
              <Label htmlFor="bonus-exp">Bonusun müddəti (gün, 0 = sonsuz)</Label>
              <Input id="bonus-exp" className="w-28" inputMode="numeric" value={bonusExpiry} onChange={(e) => setBonusExpiry(e.target.value)} />
            </div>
            <Button onClick={() => void saveSettings()}>Yaz</Button>
          </div>
          <p className="text-xs text-muted-foreground">
            Satışın ümumi endirimi bu faizi aşanda menecerin istifadəçi adı və şifrəsi soruşulur.
          </p>
          <div className="flex flex-wrap items-end gap-3 border-t pt-3">
            <div className="space-y-1">
              <Label htmlFor="bonus-earn">Bonus qazanma (%)</Label>
              <Input id="bonus-earn" className="w-28" inputMode="decimal" value={bonusEarn} onChange={(e) => setBonusEarn(e.target.value)} />
            </div>
            <div className="space-y-1">
              <Label htmlFor="bonus-max">Bonusla maksimum ödəniş (%)</Label>
              <Input id="bonus-max" className="w-28" inputMode="decimal" value={bonusMax} onChange={(e) => setBonusMax(e.target.value)} />
            </div>
            <Button onClick={() => void saveSettings()}>Yaz</Button>
          </div>
          <div className="flex flex-wrap items-end gap-3 border-t pt-3">
            <div className="space-y-1">
              <Label htmlFor="qs-count">Sürətli düymələrin ümumi sayı</Label>
              <Input id="qs-count" className="w-32" inputMode="numeric" value={quickSlotCount} onChange={(e) => setQuickSlotCount(Number(e.target.value.replace(/\D/g, "")) || 0)} />
            </div>
            <div className="space-y-1">
              <Label htmlFor="rc-width">Çek eni</Label>
              <select id="rc-width" className="h-9 rounded-md border border-input bg-background px-3 text-sm" value={receiptWidth} onChange={(e) => setReceiptWidth(Number(e.target.value))}>
                <option value={80}>80 mm</option>
                <option value={58}>58 mm</option>
              </select>
            </div>
            <div className="space-y-1">
              <Label htmlFor="rc-head">Çekin başlığı</Label>
              <Input id="rc-head" className="w-64" value={receiptHeader} onChange={(e) => setReceiptHeader(e.target.value)} />
            </div>
            <div className="space-y-1">
              <Label htmlFor="rc-foot">Çekin sonu</Label>
              <Input id="rc-foot" className="w-64" value={receiptFooter} onChange={(e) => setReceiptFooter(e.target.value)} />
            </div>
            <label className="flex items-center gap-2 pb-2 text-sm">
              <input type="checkbox" checked={allowReceiptless} onChange={(e) => setAllowReceiptless(e.target.checked)} />
              Çeksiz satışa icazə ver (ayrıca “Çeksiz satış” icazəsi olan kassir üçün)
            </label>
            <Button onClick={() => void saveSettings()}>Yaz</Button>
          </div>
          <RoleTemplatesCard />
          <UserAccessCard />
          <p className="text-xs text-muted-foreground">
            Bonus müştərinin həqiqətən ödədiyi məbləğdən qazanılır (0 = bonus söndürülüb). 1 bonus = 1 AZN.
          </p>
        </div>
      )}

      <Dialog open={open} onOpenChange={(o) => !o && !saving && setOpen(false)}>
        <DialogContent className="sm:max-w-sm">
          <DialogHeader>
            <DialogTitle>{editingId ? "Kassanı dəyiş" : "Yeni kassa"}</DialogTitle>
            <DialogDescription>Kassa seçilən anbardan satış edir.</DialogDescription>
          </DialogHeader>
          <form className="space-y-4" onSubmit={submit}>
            <div className="space-y-2">
              <Label htmlFor="reg-name">Ad</Label>
              <Input id="reg-name" value={name} onChange={(e) => setName(e.target.value)} placeholder="Kassa 1" autoFocus />
            </div>
            <div className="space-y-2">
              <Label htmlFor="reg-branch">Filial</Label>
              <select
                id="reg-branch"
                className={SELECT}
                value={restaurantId}
                onChange={(e) => {
                  setRestaurantId(e.target.value ? Number(e.target.value) : "");
                  setWarehouseId("");
                }}
              >
                <option value="">Seçin…</option>
                {branches.map((b) => (
                  <option key={b.id} value={b.id}>
                    {b.name}
                  </option>
                ))}
              </select>
            </div>
            <div className="space-y-2">
              <Label htmlFor="reg-wh">Anbar</Label>
              <select
                id="reg-wh"
                className={SELECT}
                value={warehouseId}
                disabled={restaurantId === ""}
                onChange={(e) => setWarehouseId(e.target.value ? Number(e.target.value) : "")}
              >
                <option value="">{restaurantId === "" ? "Əvvəl filial seçin" : "Seçin…"}</option>
                {branchWarehouses.map((w) => (
                  <option key={w.id} value={w.id}>
                    {w.name}
                  </option>
                ))}
              </select>
              {restaurantId !== "" && branchWarehouses.length === 0 && (
                <p className="text-xs text-amber-600">Bu filialın anbarı yoxdur. Əvvəlcə Anbarlar səhifəsində yaradın.</p>
              )}
            </div>
            <label className="flex items-center gap-2 text-sm">
              <input type="checkbox" checked={isActive} onChange={(e) => setIsActive(e.target.checked)} />
              Aktiv
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

      {/* 50 quick-sale buttons */}
      <Dialog open={slotRegister != null} onOpenChange={(o) => !o && setSlotRegister(null)}>
        <DialogContent className="max-h-[90vh] overflow-y-auto sm:max-w-3xl">
          <DialogHeader>
            <DialogTitle>Sürətli düymələr (bütün kassalar üçün ortaq) — {slotPage * 50 + 1}–{Math.min(slots.length, slotPage * 50 + 50)}</DialogTitle>
            <DialogDescription>Düyməyə basıb məhsul təyin edin, ad və rəng verin. Kassada bir toxunuşla səbətə düşür.</DialogDescription>
          </DialogHeader>
          <div className="flex items-center gap-2">
            <div className="grid flex-1 grid-cols-5 gap-2 sm:grid-cols-10">
            {slots.filter((s) => s.slotNumber > slotPage * 50 && s.slotNumber <= slotPage * 50 + 50).map((s) => (
              <button
                key={s.slotNumber}
                onClick={() => editSlot(s)}
                style={s.productId && s.color ? { backgroundColor: s.color, color: "#fff" } : undefined}
                className="flex h-16 flex-col items-center justify-center rounded-md border p-1 text-center text-[11px] leading-tight hover:opacity-90"
                title={s.productName ?? "Boş"}
              >
                <span className="text-[10px] opacity-60">{s.slotNumber}</span>
                <span className="line-clamp-2">{s.productId ? s.label || s.productName : "—"}</span>
              </button>
            ))}
            </div>
            <div className="flex shrink-0 flex-col gap-2">
              <Button type="button" variant="outline" size="icon" disabled={slotPage === 0} onClick={() => setSlotPage((p) => Math.max(0, p - 1))} aria-label="Əvvəlki 50">
                <ChevronLeft className="h-5 w-5" />
              </Button>
              <Button type="button" variant="outline" size="icon" disabled={slotPage >= Math.ceil(slots.length / 50) - 1} onClick={() => setSlotPage((p) => p + 1)} aria-label="Növbəti 50">
                <ChevronRight className="h-5 w-5" />
              </Button>
            </div>
          </div>
        </DialogContent>
      </Dialog>

      <Dialog open={slotEdit != null} onOpenChange={(o) => !o && setSlotEdit(null)}>
        <DialogContent className="sm:max-w-sm">
          <DialogHeader>
            <DialogTitle>Düymə № {slotEdit?.slotNumber}</DialogTitle>
            <DialogDescription>
              {slotEdit?.productId ? `İndi: ${slotEdit.productName} (${formatCurrency(slotEdit.salePrice ?? 0)})` : "Boşdur"}
            </DialogDescription>
          </DialogHeader>
          <div className="space-y-3">
            {slotProduct ? (
              <div className="flex items-center justify-between rounded-md border px-3 py-2 text-sm">
                <span>{slotProduct.name}</span>
                <Button size="sm" variant="ghost" onClick={() => setSlotProduct(null)}>
                  Dəyiş
                </Button>
              </div>
            ) : (
              <>
                <div className="flex gap-1">
                  <Input
                    placeholder="Yeni məhsul: ad, SKU və ya barkod…"
                    value={slotTerm}
                    onChange={(e) => setSlotTerm(e.target.value)}
                    onKeyDown={(e) => e.key === "Enter" && searchSlotProducts(false)}
                    autoFocus
                  />
                  <Button type="button" variant="outline" size="icon" title="Bütün məhsullar" aria-label="Bütün məhsullar" disabled={slotLoading} onClick={() => searchSlotProducts(true)}>
                    <MoreHorizontal className="h-4 w-4" />
                  </Button>
                  <Button type="button" variant="outline" size="icon" title="Axtar" aria-label="Axtar" disabled={slotLoading} onClick={() => searchSlotProducts(false)}>
                    <Search className="h-4 w-4" />
                  </Button>
                </div>
                {slotOptions.length > 0 && (
                  <div className="max-h-64 overflow-y-auto rounded-md border">
                    {slotOptions.map((p) => (
                      <button
                        type="button"
                        key={p.id}
                        className="block w-full px-3 py-1.5 text-left text-sm hover:bg-muted"
                        onClick={() => setSlotProduct(p)}
                      >
                        {p.name} <span className="text-xs text-muted-foreground">{formatCurrency(p.salePrice)}</span>
                      </button>
                    ))}
                  </div>
                )}
              </>
            )}
            <div className="space-y-1">
              <Label htmlFor="slot-label">Düymədəki ad (istəyə bağlı)</Label>
              <Input id="slot-label" value={slotLabel} onChange={(e) => setSlotLabel(e.target.value)} />
            </div>
            <div className="space-y-1">
              <Label>Rəng</Label>
              <div className="flex flex-wrap gap-2">
                <button
                  type="button"
                  onClick={() => setSlotColor("")}
                  className={`h-7 w-7 rounded-full border text-xs ${slotColor === "" ? "ring-2 ring-primary" : ""}`}
                  title="Rəngsiz"
                >
                  ✕
                </button>
                {COLORS.map((c) => (
                  <button
                    type="button"
                    key={c}
                    onClick={() => setSlotColor(c)}
                    style={{ backgroundColor: c }}
                    className={`h-7 w-7 rounded-full ${slotColor === c ? "ring-2 ring-primary ring-offset-2" : ""}`}
                  />
                ))}
              </div>
            </div>
          </div>
          <DialogFooter>
            {slotEdit?.productId && (
              <Button variant="outline" onClick={() => void saveSlot(true)}>
                Boşalt
              </Button>
            )}
            <Button onClick={() => void saveSlot(false)}>Yaz</Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}
