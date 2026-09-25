"use client";

import { useEffect, useState, type ReactNode } from "react";
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
import { useSelectedCompany } from "@/contexts/selected-company-context";

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
  modulePaket: false,
  moduleOtel: false,
  moduleFitnes: false,
  moduleDataSecimi: false,
  moduleQiymetSor: false,
  integrationWolt: false,
  integrationBolt: false,
  integration189Delivery: false,
  alertMilliseconds: null,
  alertRingCount: null,
  alertRingIntervalSeconds: null,
  tableTimeWarningMinutes: null,
  loginLogoUrl: null,
  reportLogoUrl: null,
  wallpaperUrl: null,
  loginLocation: null,
  transparencyLevel: null,
  productColor: null,
  floorLabel: null,
  logoSize: null,
  slogan: null,
  socialLinks: null,
  contactPhoneNumber: null,
  receiptFontSize: null,
  categoryFontSize: null,
  receiptRestaurantNameFontSize: null,
  allowReceiptEditAfterPrint: true,
  waiterCanPrintCustomerReceipt: true,
  printAutoOnPayment: false,
  printKitchenOnPayment: false,
  printShowPreview: true,
  printGroupQuantities: true,
  printKitchenGroupQuantities: false,
  receiptShowTime: true,
  receiptShowWaiterName: true,
  receiptShowTableName: true,
  receiptShowOrderNumber: true,
  receiptShowPaymentMethod: true,
  printAskBeforeAutoPrint: false,
  receiptSimpleMode: false,
  receiptSimpleShowOrderNumber: false,
  receiptSimpleShowWaiterName: false,
  receiptSimpleShowTime: false,
  receiptSimpleShowPaymentMethod: false,
  receiptSimpleShowVat: false,
  receiptSimpleShowFooter: false,
  printKitchenShowBusinessName: true,
  receiptShowBusinessName: true,
  printKitchenOnHold: false,
  printTransferDocAuto: false,
  printTransferDocDouble: false,
  printChiefCopy: false,
  askGuestCountOnOpen: false,
  singleWaiterMode: false,
  defaultVatPercent: null,
};

const PRINT_TOGGLE_FIELDS: Array<{ key: keyof CompanySettingsInput; label: string }> = [
  { key: "printAutoOnPayment", label: "Ödəniş bitəndə qəbz avtomatik çap olunsun" },
  { key: "printKitchenOnPayment", label: "Ödənişdə mətbəx çapı avtomatik göndərilsin" },
  { key: "printShowPreview", label: "Çapdan əvvəl önizləmə göstər" },
  { key: "printGroupQuantities", label: "Qəbzdə eyni məhsulun miqdarını qruplaşdır" },
  { key: "printKitchenGroupQuantities", label: "Mətbəx qəbzində eyni məhsulun miqdarını qruplaşdır" },
  { key: "printAskBeforeAutoPrint", label: "Avtomatik çapdan əvvəl təsdiq soruş" },
  { key: "printKitchenShowBusinessName", label: "Mətbəx çapında biznes adı" },
  { key: "receiptShowBusinessName", label: "Adisyonda (müştəri qəbzində) biznes adı" },
  { key: "printKitchenOnHold", label: "Gözlətmədə mətbəx çapı (sifariş gözləmədə olsa da mətbəxə göndərilsin)" },
  { key: "printTransferDocAuto", label: "Köçürmə sənədi avtomatik (masa köçürüləndə mətbəxə çap)" },
  { key: "printTransferDocDouble", label: "Köçürmə sənədinin ikili çapı" },
  { key: "printChiefCopy", label: "ChiefPrint (mətbəx çeklərinin nüsxəsi şef printerinə)" },
  { key: "receiptSimpleMode", label: "Sadə qəbz rejimi aktiv olsun" },
];

const RECEIPT_FIELD_TOGGLES: Array<{ key: keyof CompanySettingsInput; label: string }> = [
  { key: "receiptShowTime", label: "Vaxt" },
  { key: "receiptShowWaiterName", label: "Ofisiant adı" },
  { key: "receiptShowTableName", label: "Masa adı" },
  { key: "receiptShowOrderNumber", label: "Sifariş nömrəsi" },
  { key: "receiptShowPaymentMethod", label: "Ödəniş üsulu" },
];

const RECEIPT_SIMPLE_FIELD_TOGGLES: Array<{ key: keyof CompanySettingsInput; label: string }> = [
  { key: "receiptSimpleShowTime", label: "Vaxt" },
  { key: "receiptSimpleShowWaiterName", label: "Ofisiant adı" },
  { key: "receiptSimpleShowOrderNumber", label: "Sifariş nömrəsi" },
  { key: "receiptSimpleShowPaymentMethod", label: "Ödəniş üsulu" },
  { key: "receiptSimpleShowVat", label: "ƏDV sətri" },
  { key: "receiptSimpleShowFooter", label: "Slogan / əlaqə / sosial media" },
];

export default function SettingsPage() {
  const { selectedCompanyId } = useSelectedCompany();
  const [form, setForm] = useState<CompanySettingsInput>(DEFAULTS);
  const [savedForm, setSavedForm] = useState<CompanySettingsInput>(DEFAULTS);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);
  const [confirmingField, setConfirmingField] = useState<keyof CompanySettingsInput | null>(null);
  const [uploadingField, setUploadingField] = useState<keyof CompanySettingsInput | null>(null);

  useEffect(() => {
    (async () => {
      try {
        setLoading(true);
        // SuperAdmin editing a specific company (via the "Company filter") sees THAT company's
        // settings — a tenant Admin always gets their own regardless (backend enforces this too).
        const settings = await getCompanySettings(selectedCompanyId ?? undefined);
        setForm(settings);
        setSavedForm(settings);
      } catch (err) {
        setError(err instanceof Error ? err.message : "Tənzimləmələr yüklənə bilmədi");
      } finally {
        setLoading(false);
      }
    })();
  }, [selectedCompanyId]);

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
      const updated = await updateCompanySettings(form, selectedCompanyId ?? undefined);
      setForm(updated);
      setSavedForm(updated);
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

  // Login Logo/Rapor Logo/Divar kağızı/Login mövqeyi/Şəffaflıq/Məhsul rəngi/Mərtəbə/Logo ölçüsü each
  // get their own confirm/cancel affordance instead of relying on the page-wide "Yadda saxla" button.
  // The backend only exposes a whole-object update, so "confirm" on one field submits every currently
  // pending change — but "cancel" reverts only that one field, leaving other pending edits untouched.
  const isFieldDirty = (key: keyof CompanySettingsInput) => form[key] !== savedForm[key];

  const confirmField = async (key: keyof CompanySettingsInput) => {
    setConfirmingField(key);
    setError(null);
    try {
      const updated = await updateCompanySettings(form, selectedCompanyId ?? undefined);
      setForm(updated);
      setSavedForm(updated);
      toast.success("Dəyişiklik təsdiqləndi.");
    } catch (err) {
      if (err instanceof ApiFormError) {
        toast.error(err.message);
      } else {
        toast.error(err instanceof Error ? err.message : "Təsdiqlənmədi");
      }
    } finally {
      setConfirmingField(null);
    }
  };

  const cancelField = (key: keyof CompanySettingsInput) => {
    setForm((prev) => ({ ...prev, [key]: savedForm[key] }));
  };

  const confirmableField = (key: keyof CompanySettingsInput, control: ReactNode) => (
    <div>
      {control}
      {isFieldDirty(key) && (
        <div className="mt-1.5 flex gap-2">
          <Button
            type="button"
            size="sm"
            variant="outline"
            className="h-7 px-2 text-xs"
            disabled={confirmingField === key}
            onClick={() => void confirmField(key)}
          >
            {confirmingField === key ? "Təsdiqlənir…" : "Təsdiq et"}
          </Button>
          <Button
            type="button"
            size="sm"
            variant="ghost"
            className="h-7 px-2 text-xs"
            disabled={confirmingField === key}
            onClick={() => cancelField(key)}
          >
            İmtina et
          </Button>
        </div>
      )}
    </div>
  );

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
          <div>
            <Label>Masa vaxt xəbərdarlığı (dəqiqə)</Label>
            <div className="mt-1">{numberField("tableTimeWarningMinutes", form.tableTimeWarningMinutes)}</div>
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

        <p className="text-xs text-muted-foreground">
          Bu bölmədəki hər sahə öz təsdiq/imtina düymələri ilə ayrıca saxlanılır — dəyişiklik edən kimi aşağıda
          "Təsdiq et" / "İmtina et" görünəcək.
        </p>

        <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
          <div>
            <Label>Login loqo</Label>
            {confirmableField("loginLogoUrl", <div className="mt-1">{imageField("loginLogoUrl", form.loginLogoUrl)}</div>)}
          </div>
          <div>
            <Label>Rapor loqo</Label>
            {confirmableField("reportLogoUrl", <div className="mt-1">{imageField("reportLogoUrl", form.reportLogoUrl)}</div>)}
          </div>
          <div>
            <Label>Divar kağızı</Label>
            {confirmableField("wallpaperUrl", <div className="mt-1">{imageField("wallpaperUrl", form.wallpaperUrl)}</div>)}
          </div>
          <div>
            <Label>Login mövqeyi</Label>
            {confirmableField("loginLocation", <div className="mt-1">{textField("loginLocation", form.loginLocation)}</div>)}
          </div>
          <div>
            <Label>Şəffaflıq səviyyəsi (0-100)</Label>
            {confirmableField(
              "transparencyLevel",
              <div className="mt-1">{numberField("transparencyLevel", form.transparencyLevel)}</div>,
            )}
          </div>
          <div>
            <Label>Məhsul rəngi (hex)</Label>
            {confirmableField("productColor", <div className="mt-1">{textField("productColor", form.productColor)}</div>)}
          </div>
          <div>
            <Label>Mərtəbə</Label>
            {confirmableField("floorLabel", <div className="mt-1">{textField("floorLabel", form.floorLabel)}</div>)}
          </div>
          <div>
            <Label>Logo ölçüsü (px)</Label>
            {confirmableField("logoSize", <div className="mt-1">{numberField("logoSize", form.logoSize)}</div>)}
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
          <div>
            <Label>Filial adı şrift ölçüsü (qəbzdə)</Label>
            <div className="mt-1">
              {numberField("receiptRestaurantNameFontSize", form.receiptRestaurantNameFontSize)}
            </div>
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

      {/* Çap paneli */}
      <section className="space-y-4 rounded-xl border bg-card p-6">
        <h2 className="text-lg font-semibold">Çap paneli</h2>

        <div className="space-y-2">
          {PRINT_TOGGLE_FIELDS.map((f) => (
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

        <div>
          <Label className="mb-2 block">Sadə qəbzdə görünsün</Label>
          <p className="mb-2 text-xs text-muted-foreground">
            "Sadə qəbz rejimi" aktiv olanda qəbzdə restoran adı, məhsullar və cəm həmişə görünür — aşağıdakılardan
            hansını əlavə olaraq göstərmək istəyirsinizsə seçin (bunlar adi qəbzdəki seçimlərdən tamamilə
            asılı deyil, ayrıca tənzimlənir).
          </p>
          <div className="grid grid-cols-2 gap-3 sm:grid-cols-3">
            {RECEIPT_SIMPLE_FIELD_TOGGLES.map((f) => (
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
          <Label className="mb-2 block">Qəbzdə görünsün</Label>
          <p className="mb-2 text-xs text-muted-foreground">
            Bu sahələrdən hansını müştəri qəbzində göstərmək istəmirsinizsə, işarəni götürün.
          </p>
          <div className="grid grid-cols-2 gap-3 sm:grid-cols-3">
            {RECEIPT_FIELD_TOGGLES.map((f) => (
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

      {/* POS-1 paneli */}
      <section className="space-y-4 rounded-xl border bg-card p-6">
        <h2 className="text-lg font-semibold">POS-1 paneli</h2>
        <div className="flex items-center gap-2">
          <Checkbox
            id="askGuestCountOnOpen"
            checked={form.askGuestCountOnOpen}
            onCheckedChange={(v) => update("askGuestCountOnOpen", v === true)}
          />
          <Label htmlFor="askGuestCountOnOpen" className="text-sm font-normal">
            Masa açılanda qonaq (kişi) sayını soruş
          </Label>
        </div>
        <div>
          <div className="flex items-center gap-2">
            <Checkbox
              id="singleWaiterMode"
              checked={form.singleWaiterMode}
              onCheckedChange={(v) => update("singleWaiterMode", v === true)}
            />
            <Label htmlFor="singleWaiterMode" className="text-sm font-normal">
              Tək ofisiant rejimi
            </Label>
          </div>
          <p className="mt-1 text-xs text-muted-foreground">
            Açıqsa, bir masanı yalnız onu açan ofisiant görə/redaktə edə bilər — "bütün masaları gör" icazəsi olan
            menecer də daxil, heç kim başqasının masasına müdaxilə edə bilməz.
          </p>
        </div>
      </section>

      {/* ƏDV */}
      <section className="space-y-4 rounded-xl border bg-card p-6">
        <h2 className="text-lg font-semibold">ƏDV</h2>
        <div>
          <Label>Ümumi ƏDV faizi (%)</Label>
          <p className="mb-1 mt-0.5 text-xs text-muted-foreground">
            Məhsulun özündə ƏDV faizi ayrıca göstərilməyibsə, bu dəyər istifadə olunur. Qiymətlərə ƏDV daxil hesab
            edilir, qəbzdə ayrıca sətir kimi göstərilir.
          </p>
          <div className="mt-1 max-w-[160px]">{numberField("defaultVatPercent", form.defaultVatPercent)}</div>
        </div>
      </section>

      <Button onClick={() => void handleSave()} disabled={saving} className="h-11 px-8">
        {saving ? "Saxlanılır..." : "Yadda saxla"}
      </Button>
    </div>
  );
}
