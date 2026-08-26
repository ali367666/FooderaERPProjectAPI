"use client";

import { useEffect, useState } from "react";
import { toast } from "sonner";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Checkbox } from "@/components/ui/checkbox";
import { Button } from "@/components/ui/button";
import {
  getCompanySettings,
  updateCompanySettings,
  type CompanySettingsInput,
} from "@/lib/services/company-settings-service";
import { uploadFile } from "@/lib/services/file-service";
import { ApiFormError } from "@/lib/api-error";

const MODULE_FIELDS: Array<{ key: keyof CompanySettingsInput; label: string }> = [
  { key: "moduleFilial", label: "Filial" },
  { key: "moduleAnbar", label: "Anbar" },
  { key: "moduleRezervasyon", label: "Rezervasiya" },
  { key: "moduleMasaBolge", label: "Masa Bölgə" },
];

const INTEGRATION_FIELDS: Array<{ key: keyof CompanySettingsInput; label: string }> = [
  { key: "integrationWolt", label: "Wolt" },
  { key: "integrationBolt", label: "Bolt" },
  { key: "integration189Delivery", label: "189 Delivery" },
];

function timeSpanToInputValue(value: string | null): string {
  if (!value) return "";
  return value.slice(0, 5);
}

function inputValueToTimeSpan(value: string): string | null {
  if (!value) return null;
  return `${value}:00`;
}

const DEFAULTS: CompanySettingsInput = {
  openingTime: null,
  moduleFilial: false,
  moduleAnbar: false,
  moduleRezervasyon: false,
  moduleMasaBolge: false,
  integrationWolt: false,
  integrationBolt: false,
  integration189Delivery: false,
  alertMilliseconds: null,
  alertRingCount: null,
  alertRingIntervalSeconds: null,
  loginLogoUrl: null,
  reportLogoUrl: null,
  wallpaperUrl: null,
  loginLocation: null,
  transparencyLevel: null,
  productColor: null,
  floorLabel: null,
  slogan: null,
  socialLinks: null,
  contactPhoneNumber: null,
  receiptFontSize: null,
  categoryFontSize: null,
  allowReceiptEditAfterPrint: true,
  waiterCanPrintCustomerReceipt: true,
};

export default function SettingsPage() {
  const [form, setForm] = useState<CompanySettingsInput>(DEFAULTS);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [uploadingField, setUploadingField] = useState<keyof CompanySettingsInput | null>(null);

  useEffect(() => {
    (async () => {
      try {
        const settings = await getCompanySettings();
        setForm(settings);
      } catch (err) {
        setError(err instanceof Error ? err.message : "Tənzimləmələr yüklənə bilmədi");
      } finally {
        setLoading(false);
      }
    })();
  }, []);

  const update = <K extends keyof CompanySettingsInput>(key: K, value: CompanySettingsInput[K]) => {
    setForm((prev) => ({ ...prev, [key]: value }));
  };

  const numberField = (key: keyof CompanySettingsInput, value: number | null) => (
    <Input
      type="number"
      value={value ?? ""}
      onChange={(e) => update(key, (e.target.value === "" ? null : Number(e.target.value)) as never)}
    />
  );

  const textField = (key: keyof CompanySettingsInput, value: string | null) => (
    <Input
      value={value ?? ""}
      onChange={(e) => update(key, (e.target.value === "" ? null : e.target.value) as never)}
    />
  );

  const handleImageUpload = async (key: keyof CompanySettingsInput, file: File | undefined) => {
    if (!file) return;
    setUploadingField(key);
    try {
      const url = await uploadFile(file);
      update(key, url as never);
    } catch (err) {
      toast.error(err instanceof Error ? err.message : "Fayl yüklənmədi");
    } finally {
      setUploadingField(null);
    }
  };

  const imageField = (key: keyof CompanySettingsInput, value: string | null) => (
    <div className="space-y-2">
      {value && (
        // eslint-disable-next-line @next/next/no-img-element
        <img src={value} alt="" className="h-16 w-auto rounded border bg-muted object-contain" />
      )}
      <Input
        type="file"
        accept="image/png,image/jpeg,image/webp,image/svg+xml"
        disabled={uploadingField === key}
        onChange={(e) => void handleImageUpload(key, e.target.files?.[0])}
      />
      {uploadingField === key && <p className="text-xs text-muted-foreground">Yüklənir...</p>}
    </div>
  );

  const handleSave = async () => {
    setSaving(true);
    setError(null);
    try {
      const updated = await updateCompanySettings(form);
      setForm(updated);
      toast.success("Tənzimləmələr saxlanıldı.");
    } catch (err) {
      if (err instanceof ApiFormError) {
        toast.error(err.message);
      } else {
        toast.error(err instanceof Error ? err.message : "Saxlanılmadı");
      }
    } finally {
      setSaving(false);
    }
  };

  if (loading) {
    return <div className="p-6 text-sm text-muted-foreground">Yüklənir...</div>;
  }

  return (
    <div className="space-y-8 pb-10">
      <div>
        <h1 className="text-3xl font-bold text-foreground">Tənzimləmələr</h1>
        <p className="mt-1 text-muted-foreground">Biznesin ümumi ayarları və qəbz dizaynı.</p>
      </div>

      {error && (
        <div className="rounded-md border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-600">{error}</div>
      )}

      {/* Ümumi */}
      <section className="space-y-4 rounded-xl border bg-card p-6">
        <h2 className="text-lg font-semibold">Ümumi</h2>

        <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 md:grid-cols-3">
          <div>
            <Label>Açılış vaxtı</Label>
            <Input
              type="time"
              className="mt-1"
              value={timeSpanToInputValue(form.openingTime)}
              onChange={(e) => update("openingTime", inputValueToTimeSpan(e.target.value))}
            />
          </div>
          <div>
            <Label>Xəbərdarlıq (milisaniyə)</Label>
            <div className="mt-1">{numberField("alertMilliseconds", form.alertMilliseconds)}</div>
          </div>
          <div>
            <Label>Xəbərdarlıq zəng sayı</Label>
            <div className="mt-1">{numberField("alertRingCount", form.alertRingCount)}</div>
          </div>
          <div>
            <Label>Xəbərdarlıq zəng aralığı (san)</Label>
            <div className="mt-1">{numberField("alertRingIntervalSeconds", form.alertRingIntervalSeconds)}</div>
          </div>
        </div>

        <div>
          <Label className="mb-2 block">Aktiv modullar</Label>
          <div className="grid grid-cols-2 gap-3 sm:grid-cols-3 md:grid-cols-4">
            {MODULE_FIELDS.map((f) => (
              <div key={f.key} className="flex items-center gap-2">
                <Checkbox
                  id={f.key}
                  checked={Boolean(form[f.key])}
                  onCheckedChange={(v) => update(f.key, (v === true) as never)}
                />
                <Label htmlFor={f.key} className="text-sm font-normal">
                  {f.label}
                </Label>
              </div>
            ))}
          </div>
        </div>

        <div>
          <Label className="mb-2 block">Çatdırılma inteqrasiyaları</Label>
          <div className="grid grid-cols-2 gap-3 sm:grid-cols-3">
            {INTEGRATION_FIELDS.map((f) => (
              <div key={f.key} className="flex items-center gap-2">
                <Checkbox
                  id={f.key}
                  checked={Boolean(form[f.key])}
                  onCheckedChange={(v) => update(f.key, (v === true) as never)}
                />
                <Label htmlFor={f.key} className="text-sm font-normal">
                  {f.label}
                </Label>
              </div>
            ))}
          </div>
        </div>
      </section>

      {/* Qəbz */}
      <section className="space-y-4 rounded-xl border bg-card p-6">
        <h2 className="text-lg font-semibold">Qəbz tənzimləmələri</h2>

        <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
          <div>
            <Label>Login loqo</Label>
            <div className="mt-1">{imageField("loginLogoUrl", form.loginLogoUrl)}</div>
          </div>
          <div>
            <Label>Rapor loqo</Label>
            <div className="mt-1">{imageField("reportLogoUrl", form.reportLogoUrl)}</div>
          </div>
          <div>
            <Label>Divar kağızı</Label>
            <div className="mt-1">{imageField("wallpaperUrl", form.wallpaperUrl)}</div>
          </div>
          <div>
            <Label>Login mövqeyi</Label>
            <div className="mt-1">{textField("loginLocation", form.loginLocation)}</div>
          </div>
          <div>
            <Label>Şəffaflıq səviyyəsi (0-100)</Label>
            <div className="mt-1">{numberField("transparencyLevel", form.transparencyLevel)}</div>
          </div>
          <div>
            <Label>Məhsul rəngi (hex)</Label>
            <div className="mt-1">{textField("productColor", form.productColor)}</div>
          </div>
          <div>
            <Label>Mərtəbə</Label>
            <div className="mt-1">{textField("floorLabel", form.floorLabel)}</div>
          </div>
          <div className="sm:col-span-2">
            <Label>Slogan</Label>
            <div className="mt-1">{textField("slogan", form.slogan)}</div>
          </div>
          <div className="sm:col-span-2">
            <Label>Sosial media linkləri</Label>
            <div className="mt-1">{textField("socialLinks", form.socialLinks)}</div>
          </div>
          <div>
            <Label>Əlaqə nömrəsi</Label>
            <div className="mt-1">{textField("contactPhoneNumber", form.contactPhoneNumber)}</div>
          </div>
          <div>
            <Label>Qəbz şrift ölçüsü</Label>
            <div className="mt-1">{numberField("receiptFontSize", form.receiptFontSize)}</div>
          </div>
          <div>
            <Label>Kateqoriya şrift ölçüsü</Label>
            <div className="mt-1">{numberField("categoryFontSize", form.categoryFontSize)}</div>
          </div>
        </div>

        <div className="space-y-2 pt-2">
          <div className="flex items-center gap-2">
            <Checkbox
              id="allowReceiptEditAfterPrint"
              checked={form.allowReceiptEditAfterPrint}
              onCheckedChange={(v) => update("allowReceiptEditAfterPrint", v === true)}
            />
            <Label htmlFor="allowReceiptEditAfterPrint" className="text-sm font-normal">
              Qəbz çıxdıqdan sonra düzəlişə icazə ver
            </Label>
          </div>
          <div className="flex items-center gap-2">
            <Checkbox
              id="waiterCanPrintCustomerReceipt"
              checked={form.waiterCanPrintCustomerReceipt}
              onCheckedChange={(v) => update("waiterCanPrintCustomerReceipt", v === true)}
            />
            <Label htmlFor="waiterCanPrintCustomerReceipt" className="text-sm font-normal">
              Ofisiant müştəri qəbzini çıxara bilsin
            </Label>
          </div>
        </div>
      </section>

      <Button onClick={() => void handleSave()} disabled={saving} className="h-11 px-8">
        {saving ? "Saxlanılır..." : "Yadda saxla"}
      </Button>
    </div>
  );
}
