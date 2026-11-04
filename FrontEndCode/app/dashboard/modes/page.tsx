"use client";

import { useCallback, useEffect, useState } from "react";
import { Store, UtensilsCrossed } from "lucide-react";
import { toast } from "sonner";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { getCompanies, getCompanyPaymentBanks, setCompanyKitchenApproval, setCompanyPaymentBanks, type PaymentBankStatus, setCompanyModules, setCompanySalesLock, type Company, type CompanyModules } from "@/lib/services/company-service";
import { getCompanySettingsBranding } from "@/lib/services/company-settings-service";
import { useIsSuperAdmin } from "@/hooks/use-auth-permissions";

type Row = { company: Company; modules: CompanyModules | null; salesLocked: boolean; kitchenRequired: boolean; banks: PaymentBankStatus[] };

const MODULE_LABELS: { key: keyof CompanyModules; label: string }[] = [
  { key: "moduleFilial", label: "Filial" },
  { key: "moduleAnbar", label: "Anbar" },
  { key: "moduleRezervasyon", label: "Rezervasiya" },
  { key: "moduleMasaBolge", label: "Masa/bölgə" },
  { key: "modulePaket", label: "Paket" },
  { key: "moduleOtel", label: "Otel" },
  { key: "moduleFitnes", label: "Fitnes" },
  { key: "moduleQiymetSor", label: "Qiymət sor" },
  { key: "moduleKompleks", label: "İstirahət kompleksi" },
];

/** SuperAdmin: one place to switch each company between Restoran and Mağaza rejimi and to open/close its modules. */
export default function ModesPage() {
  const isSuperAdmin = useIsSuperAdmin();
  const [rows, setRows] = useState<Row[]>([]);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState<number | null>(null);

  const load = useCallback(async () => {
    setLoading(true);
    try {
      const companies = await getCompanies();
      const withModules = await Promise.all(
        companies.map(async (company) => {
          try {
            const b = await getCompanySettingsBranding(company.id);
            const modules: CompanyModules = {
              moduleFilial: b.moduleFilial,
              moduleAnbar: b.moduleAnbar,
              moduleRezervasyon: b.moduleRezervasyon,
              moduleMasaBolge: b.moduleMasaBolge,
              modulePaket: b.modulePaket,
              moduleOtel: b.moduleOtel,
              moduleFitnes: b.moduleFitnes,
              moduleDataSecimi: b.moduleDataSecimi,
              moduleQiymetSor: b.moduleQiymetSor,
              moduleKompleks: b.moduleKompleks,
            };
            const banks = await getCompanyPaymentBanks(company.id).catch(() => [] as PaymentBankStatus[]);
            return { company, modules, salesLocked: b.salesEditLocked, kitchenRequired: b.kitchenApprovalRequired, banks } as Row;
          } catch {
            return { company, modules: null, salesLocked: false, kitchenRequired: true, banks: [] } as Row;
          }
        }),
      );
      setRows(withModules);
    } catch (e) {
      toast.error(e instanceof Error ? e.message : "Şirkətlər yüklənmədi.");
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    void load();
  }, [load]);

  const toggleSalesLock = async (row: Row) => {
    setSaving(row.company.id);
    try {
      await setCompanySalesLock(row.company.id, !row.salesLocked);
      setRows((prev) => prev.map((r) => (r.company.id === row.company.id ? { ...r, salesLocked: !row.salesLocked } : r)));
      toast.success(`${row.company.name}: satışların dəyişdirilməsi ${row.salesLocked ? "açıldı" : "bağlandı"}.`);
    } catch (e) {
      toast.error(e instanceof Error ? e.message : "Yazılmadı.");
    } finally {
      setSaving(null);
    }
  };

  const toggleBank = async (row: Row, name: string, enabled: boolean) => {
    const banks = row.banks.map((b) => (b.name === name ? { ...b, enabled } : b));
    setSaving(row.company.id);
    try {
      await setCompanyPaymentBanks(row.company.id, banks.filter((b) => b.enabled).map((b) => b.name));
      setRows((prev) => prev.map((r) => (r.company.id === row.company.id ? { ...r, banks } : r)));
      toast.success(`${row.company.name}: banklar yadda saxlanıldı.`);
    } catch (e) {
      toast.error(e instanceof Error ? e.message : "Yazılmadı.");
    } finally {
      setSaving(null);
    }
  };

  const toggleKitchenApproval = async (row: Row) => {
    setSaving(row.company.id);
    try {
      await setCompanyKitchenApproval(row.company.id, !row.kitchenRequired);
      setRows((prev) => prev.map((r) => (r.company.id === row.company.id ? { ...r, kitchenRequired: !row.kitchenRequired } : r)));
      toast.success(`${row.company.name}: mətbəx təsdiqi ${row.kitchenRequired ? "söndürüldü" : "aktiv edildi"}.`);
    } catch (e) {
      toast.error(e instanceof Error ? e.message : "Yazılmadı.");
    } finally {
      setSaving(null);
    }
  };

  const save = async (row: Row, next: CompanyModules) => {
    setSaving(row.company.id);
    try {
      await setCompanyModules(row.company.id, next);
      setRows((prev) => prev.map((r) => (r.company.id === row.company.id ? { ...r, modules: next } : r)));
      toast.success(`${row.company.name}: yadda saxlanıldı.`);
    } catch (e) {
      toast.error(e instanceof Error ? e.message : "Yazılmadı.");
    } finally {
      setSaving(null);
    }
  };

  if (!isSuperAdmin) return <p className="p-6 text-sm text-muted-foreground">Bu səhifə yalnız SuperAdmin üçündür.</p>;

  return (
    <div className="space-y-4 p-6">
      <div>
        <h1 className="text-2xl font-bold">Rejimlər</h1>
        <p className="text-sm text-muted-foreground">
          Hər şirkətin rejimini bir yerdən seçin. Restoran rejimində masa, mətbəx, rezervasiya və menyu bölmələri, Mağaza rejimində isə mağaza bölmələri (satış, stok, alış,
          müştərilər, hesabatlar) və onların datası görünür. Rejim dəyişəndə heç nə silinmir; digər rejimin datası gizlənir və geri qayıdanda yenidən görünür.
        </p>
      </div>

      {loading ? (
        <p className="text-sm text-muted-foreground">Yüklənir…</p>
      ) : (
        <div className="space-y-3">
          {rows.map((row) => {
            const m = row.modules;
            const isStore = m?.moduleDataSecimi === true;
            return (
              <div key={row.company.id} className="rounded-lg border bg-card p-4">
                <div className="flex flex-wrap items-center justify-between gap-3">
                  <div className="flex items-center gap-2">
                    <span className="font-semibold">{row.company.name}</span>
                    <span className="text-xs text-muted-foreground">{row.company.companyCode}</span>
                    {m && <Badge variant={isStore ? "default" : "secondary"}>{isStore ? "Mağaza" : "Restoran"}</Badge>}
                  </div>
                  {m ? (
                    <div className="flex gap-2">
                      <Button size="sm" variant={!isStore ? "default" : "outline"} disabled={saving === row.company.id} onClick={() => void save(row, { ...m, moduleDataSecimi: false })}>
                        <UtensilsCrossed className="mr-1 h-4 w-4" />
                        Restoran
                      </Button>
                      <Button size="sm" variant={isStore ? "default" : "outline"} disabled={saving === row.company.id} onClick={() => void save(row, { ...m, moduleDataSecimi: true })}>
                        <Store className="mr-1 h-4 w-4" />
                        Mağaza
                      </Button>
                    </div>
                  ) : (
                    <span className="text-xs text-red-600">Parametrlər yüklənmədi</span>
                  )}
                </div>
                {m && isStore && (
                  <div className="mt-3 flex flex-wrap items-center justify-between gap-2 border-t pt-3 text-sm">
                    <span>
                      Satış çeklərinin dəyişdirilməsi (ləğvi):{" "}
                      <b className={row.salesLocked ? "text-red-600" : "text-emerald-600"}>{row.salesLocked ? "bağlıdır" : "açıqdır"}</b>
                    </span>
                    <Button size="sm" variant="outline" disabled={saving === row.company.id} onClick={() => void toggleSalesLock(row)}>
                      {row.salesLocked ? "Aç" : "Bağla"}
                    </Button>
                  </div>
                )}
                {m && row.banks.length > 0 && (
                  <div className="mt-3 flex flex-wrap items-center gap-x-5 gap-y-2 border-t pt-3 text-sm">
                    <span className="font-medium">Banklar (kart ödənişi):</span>
                    {row.banks.map((b) => (
                      <label key={b.name} className="flex items-center gap-1.5">
                        <input
                          type="checkbox"
                          checked={b.enabled}
                          disabled={saving === row.company.id}
                          onChange={(e) => void toggleBank(row, b.name, e.target.checked)}
                        />
                        {b.name}
                      </label>
                    ))}
                  </div>
                )}
                {m && !isStore && (
                  <div className="mt-3 flex flex-wrap items-center justify-between gap-2 border-t pt-3 text-sm">
                    <span>
                      Mətbəx təsdiqi (qəbul edir / hazırdır statusu):{" "}
                      <b className={row.kitchenRequired ? "text-emerald-600" : "text-amber-600"}>{row.kitchenRequired ? "aktivdir" : "söndürülüb"}</b>
                      {!row.kitchenRequired && <span className="text-muted-foreground"> — sifariş yalnız mətbəxə göndərilir, ofisiant istədiyi vaxt ödəniş edə bilər</span>}
                    </span>
                    <Button size="sm" variant="outline" disabled={saving === row.company.id} onClick={() => void toggleKitchenApproval(row)}>
                      {row.kitchenRequired ? "Söndür" : "Aktiv et"}
                    </Button>
                  </div>
                )}
                {m && (
                  <div className="mt-3 flex flex-wrap gap-x-5 gap-y-2 border-t pt-3">
                    {MODULE_LABELS.map(({ key, label }) => (
                      <label key={key} className="flex items-center gap-1.5 text-sm">
                        <input
                          type="checkbox"
                          checked={m[key]}
                          disabled={saving === row.company.id}
                          onChange={(e) => void save(row, { ...m, [key]: e.target.checked })}
                        />
                        {label}
                      </label>
                    ))}
                  </div>
                )}
              </div>
            );
          })}
        </div>
      )}
    </div>
  );
}
