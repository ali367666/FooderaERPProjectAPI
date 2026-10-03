"use client";

import { useEffect, useState } from "react";
import { toast } from "sonner";
import { Copy, KeyRound, Trash2 } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Checkbox } from "@/components/ui/checkbox";
import { Badge } from "@/components/ui/badge";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import {
  createRegistrationCode,
  extendCompanyLicense,
  issueLicenseKey,
  getCompanyLicense,
  revokeDevice,
  updateCompanyLicense,
  type CompanyLicenseDetail,
} from "@/lib/services/license-service";

const selectClass = "flex h-10 w-full rounded-md border border-input bg-background px-3 py-2 text-sm";

function toDateInput(utc: string | null): string {
  return utc ? utc.slice(0, 10) : "";
}

function fromDateInput(value: string): string | null {
  // End of the chosen day, so "until 31.10" really covers the 31st.
  return value ? new Date(`${value}T23:59:59`).toISOString() : null;
}

function fmtDate(utc: string | null): string {
  return utc ? new Date(utc).toLocaleDateString("az-AZ") : "—";
}

type LicenseDialogProps = {
  company: { id: number; name: string } | null;
  onOpenChange: (open: boolean) => void;
};

/** Platform admin: Online licence, payments and registered devices of one company. */
export function LicenseDialog({ company, onOpenChange }: LicenseDialogProps) {
  const [detail, setDetail] = useState<CompanyLicenseDetail | null>(null);
  const [loading, setLoading] = useState(false);
  const [saving, setSaving] = useState(false);

  const [deploymentType, setDeploymentType] = useState<"Cloud" | "Local">("Cloud");
  const [remoteEnabled, setRemoteEnabled] = useState(false);
  const [expiresAt, setExpiresAt] = useState("");
  const [monthlyPrice, setMonthlyPrice] = useState("");
  const [offlineMode, setOfflineMode] = useState(false);

  const [extendMonths, setExtendMonths] = useState("1");
  const [extendAmount, setExtendAmount] = useState("");
  const [extendNote, setExtendNote] = useState("");
  const [newCode, setNewCode] = useState<{ code: string; expiresAtUtc: string } | null>(null);
  const [newKey, setNewKey] = useState<{ licenseKey: string; expiresAtUtc: string } | null>(null);

  const apply = (d: CompanyLicenseDetail) => {
    setDetail(d);
    setDeploymentType(d.deploymentType);
    setRemoteEnabled(d.remoteAccessEnabled);
    setExpiresAt(toDateInput(d.remoteAccessExpiresAtUtc));
    setMonthlyPrice(d.monthlyPrice == null ? "" : String(d.monthlyPrice));
    setOfflineMode(d.offlineModeEnabled);
    setExtendAmount(d.monthlyPrice == null ? "" : String(d.monthlyPrice));
  };

  useEffect(() => {
    if (!company) return;
    setNewCode(null);
    setNewKey(null);
    setExtendMonths("1");
    setExtendNote("");
    setLoading(true);
    getCompanyLicense(company.id)
      .then(apply)
      .catch((e) => toast.error(e instanceof Error ? e.message : "Lisenziya yüklənmədi"))
      .finally(() => setLoading(false));
  }, [company]);

  const run = async (action: () => Promise<void>) => {
    setSaving(true);
    try {
      await action();
    } catch (e) {
      toast.error(e instanceof Error ? e.message : "Əməliyyat alınmadı");
    } finally {
      setSaving(false);
    }
  };

  const handleSave = () =>
    run(async () => {
      if (!company) return;
      apply(
        await updateCompanyLicense(company.id, {
          deploymentType,
          remoteAccessEnabled: remoteEnabled,
          remoteAccessExpiresAtUtc: fromDateInput(expiresAt),
          offlineModeEnabled: offlineMode,
          monthlyPrice: monthlyPrice.trim() ? Number(monthlyPrice) : null,
        }),
      );
      toast.success("Lisenziya yadda saxlanıldı");
    });

  const handleExtend = () =>
    run(async () => {
      if (!company) return;
      const months = Number(extendMonths);
      apply(
        await extendCompanyLicense(company.id, {
          months,
          amount: Number(extendAmount) || 0,
          note: extendNote.trim() || null,
        }),
      );
      setExtendNote("");
      toast.success(`Online ${months} ay uzadıldı`);
    });

  const handleCode = () =>
    run(async () => {
      if (!company) return;
      setNewCode(await createRegistrationCode(company.id));
    });

  const handleIssueKey = () =>
    run(async () => {
      if (!company) return;
      setNewKey(await issueLicenseKey(company.id));
      apply(await getCompanyLicense(company.id));
    });

  const handleRevoke = (deviceId: number, name: string) =>
    run(async () => {
      if (!company || !window.confirm(`"${name}" cihazının girişi ləğv edilsin?`)) return;
      await revokeDevice(deviceId);
      apply(await getCompanyLicense(company.id));
      toast.success("Cihaz ləğv edildi");
    });

  return (
    <Dialog open={company !== null} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-2xl">
        <DialogHeader>
          <DialogTitle className="flex items-center gap-2">
            <KeyRound className="h-5 w-5" />
            Lisenziya — {company?.name}
          </DialogTitle>
          <DialogDescription>
            Online açıq olanda şirkətin istifadəçiləri istənilən cihazdan daxil ola bilər. Bağlı və ya vaxtı
            bitmiş olanda yalnız qeydiyyatlı cihazlardan.
          </DialogDescription>
        </DialogHeader>

        {loading || !detail ? (
          <p className="py-6 text-center text-sm text-muted-foreground">Yüklənir…</p>
        ) : (
          <div className="space-y-6">
            {/* Status */}
            <div className="flex flex-wrap items-center gap-2 rounded-lg border p-3 text-sm">
              {detail.remoteAccessActive ? (
                <Badge className="bg-emerald-100 text-emerald-800 hover:bg-emerald-100">
                  Online · {detail.daysLeft} gün qalıb
                </Badge>
              ) : (
                <Badge className="bg-slate-100 text-slate-700 hover:bg-slate-100">Offline</Badge>
              )}
              <span className="text-muted-foreground">
                Bitmə tarixi: {fmtDate(detail.remoteAccessExpiresAtUtc)} · Qeydiyyatlı cihaz:{" "}
                {detail.devices.filter((d) => d.isActive).length}
              </span>
            </div>

            {/* Settings */}
            <section className="space-y-3">
              <h3 className="text-sm font-semibold">Ayarlar</h3>
              <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
                <div>
                  <Label>Quraşdırma növü</Label>
                  <select
                    className={`mt-1 ${selectClass}`}
                    value={deploymentType}
                    onChange={(e) => setDeploymentType(e.target.value as "Cloud" | "Local")}
                  >
                    <option value="Cloud">Bulud (mərkəzi server)</option>
                    <option value="Local">Yerli (restoranın kompüteri)</option>
                  </select>
                </div>
                <div>
                  <Label>Aylıq qiymət (₼)</Label>
                  <Input
                    className="mt-1"
                    type="number"
                    min={0}
                    step="0.01"
                    value={monthlyPrice}
                    onChange={(e) => setMonthlyPrice(e.target.value)}
                  />
                </div>
                <div>
                  <Label>{deploymentType === "Local" ? "Ödənilmiş tarix (açar bu tarixə qədər)" : "Online bitmə tarixi"}</Label>
                  <Input className="mt-1" type="date" value={expiresAt} onChange={(e) => setExpiresAt(e.target.value)} />
                </div>
                <div className="flex flex-col justify-end gap-2 pb-1">
                  <label className="flex items-center gap-2 text-sm">
                    <Checkbox checked={remoteEnabled} onCheckedChange={(v) => setRemoteEnabled(v === true)} />
                    Online (uzaqdan giriş) açıq
                  </label>
                  <label className="flex items-center gap-2 text-sm">
                    <Checkbox checked={offlineMode} onCheckedChange={(v) => setOfflineMode(v === true)} />
                    İnternetsiz işləmə
                    <span className="text-xs text-muted-foreground">(növbəti mərhələ)</span>
                  </label>
                </div>
              </div>
              <div className="flex justify-end">
                <Button onClick={() => void handleSave()} disabled={saving}>
                  Yadda saxla
                </Button>
              </div>
            </section>

            {/* Extend */}
            <section className="space-y-3 rounded-lg border p-3">
              <h3 className="text-sm font-semibold">Ödəniş qeyd et və uzat</h3>
              <div className="grid grid-cols-1 gap-3 sm:grid-cols-3">
                <div>
                  <Label>Müddət</Label>
                  <select className={`mt-1 ${selectClass}`} value={extendMonths} onChange={(e) => setExtendMonths(e.target.value)}>
                    {[1, 3, 6, 12].map((m) => (
                      <option key={m} value={m}>
                        {m} ay
                      </option>
                    ))}
                  </select>
                </div>
                <div>
                  <Label>Ödənilən məbləğ (₼)</Label>
                  <Input
                    className="mt-1"
                    type="number"
                    min={0}
                    step="0.01"
                    value={extendAmount}
                    onChange={(e) => setExtendAmount(e.target.value)}
                  />
                </div>
                <div>
                  <Label>Qeyd</Label>
                  <Input className="mt-1" value={extendNote} onChange={(e) => setExtendNote(e.target.value)} placeholder="məs. nağd, köçürmə" />
                </div>
              </div>
              <div className="flex justify-end">
                <Button onClick={() => void handleExtend()} disabled={saving}>
                  Ödənildi, uzat
                </Button>
              </div>
              {detail.payments.length > 0 && (
                <div className="max-h-40 overflow-auto rounded-md border text-sm">
                  <table className="w-full">
                    <thead className="bg-muted text-left">
                      <tr>
                        <th className="px-2 py-1 font-medium">Tarix</th>
                        <th className="px-2 py-1 font-medium">Müddət</th>
                        <th className="px-2 py-1 text-right font-medium">Məbləğ</th>
                        <th className="px-2 py-1 font-medium">Qeyd</th>
                      </tr>
                    </thead>
                    <tbody>
                      {detail.payments.map((p) => (
                        <tr key={p.id} className="border-t">
                          <td className="px-2 py-1">{fmtDate(p.createdAtUtc)}</td>
                          <td className="px-2 py-1">
                            {p.months} ay ({fmtDate(p.periodFromUtc)} – {fmtDate(p.periodToUtc)})
                          </td>
                          <td className="px-2 py-1 text-right tabular-nums">{p.amount.toFixed(2)} ₼</td>
                          <td className="px-2 py-1 text-muted-foreground">{p.note ?? ""}</td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              )}
            </section>

            {/* Licence key — Local installations verify it without internet */}
            {deploymentType === "Local" && (
              <section className="space-y-3 rounded-lg border p-3">
                <div className="flex items-center justify-between">
                  <h3 className="text-sm font-semibold">Lisenziya açarı (yerli quraşdırma)</h3>
                  <Button variant="outline" size="sm" onClick={() => void handleIssueKey()} disabled={saving}>
                    Açar yarat
                  </Button>
                </div>
                <p className="text-xs text-muted-foreground">
                  Açar ödənilmiş tarixə qədər keçərlidir və restoranın kompüterində internetsiz yoxlanılır. Hər ödənişdən
                  sonra yeni açar yaradıb restorana göndərin. Son açarın bitməsi:{" "}
                  {fmtDate(detail.licenseKeyExpiresAtUtc)}
                </p>
                {newKey && (
                  <div className="space-y-2">
                    <textarea
                      readOnly
                      value={newKey.licenseKey}
                      className="h-24 w-full rounded-md border bg-muted p-2 font-mono text-xs"
                      onFocus={(e) => e.currentTarget.select()}
                    />
                    <div className="flex justify-end">
                      <Button
                        size="sm"
                        variant="outline"
                        onClick={() => {
                          void navigator.clipboard?.writeText(newKey.licenseKey);
                          toast.success("Açar kopyalandı");
                        }}
                      >
                        <Copy className="mr-1 h-3.5 w-3.5" />
                        Kopyala
                      </Button>
                    </div>
                  </div>
                )}
              </section>
            )}

            {/* Devices */}
            <section className="space-y-3">
              <div className="flex items-center justify-between">
                <h3 className="text-sm font-semibold">Qeydiyyatlı cihazlar</h3>
                <Button variant="outline" size="sm" onClick={() => void handleCode()} disabled={saving}>
                  Qeydiyyat kodu yarat
                </Button>
              </div>
              {newCode && (
                <div className="flex flex-wrap items-center justify-between gap-2 rounded-lg border border-emerald-200 bg-emerald-50 px-3 py-2">
                  <div>
                    <p className="font-mono text-xl font-semibold tracking-widest text-emerald-900">{newCode.code}</p>
                    <p className="text-xs text-emerald-800">
                      Birdəfəlik, {new Date(newCode.expiresAtUtc).toLocaleString("az-AZ")} tarixinə qədər keçərlidir. Restoranda
                      cihazda daxil edilməlidir.
                    </p>
                  </div>
                  <Button
                    size="sm"
                    variant="outline"
                    onClick={() => {
                      void navigator.clipboard?.writeText(newCode.code);
                      toast.success("Kod kopyalandı");
                    }}
                  >
                    <Copy className="mr-1 h-3.5 w-3.5" />
                    Kopyala
                  </Button>
                </div>
              )}
              {detail.devices.length === 0 ? (
                <p className="text-sm text-muted-foreground">Hələ cihaz qeydiyyatdan keçməyib.</p>
              ) : (
                <div className="space-y-1">
                  {detail.devices.map((d) => (
                    <div key={d.id} className="flex items-center justify-between rounded-md border px-3 py-2 text-sm">
                      <div>
                        <span className={d.isActive ? "font-medium" : "text-muted-foreground line-through"}>{d.name}</span>
                        <span className="ml-2 text-xs text-muted-foreground">
                          qeydiyyat {fmtDate(d.createdAtUtc)} · son giriş {fmtDate(d.lastSeenAtUtc)}
                        </span>
                      </div>
                      {d.isActive && (
                        <button
                          type="button"
                          title="Girişi ləğv et"
                          className="text-muted-foreground hover:text-destructive"
                          onClick={() => void handleRevoke(d.id, d.name)}
                        >
                          <Trash2 className="h-4 w-4" />
                        </button>
                      )}
                    </div>
                  ))}
                </div>
              )}
            </section>
          </div>
        )}
      </DialogContent>
    </Dialog>
  );
}
