"use client";

import { useEffect, useState } from "react";
import { toast } from "sonner";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { useHasPermission, useIsSuperAdmin } from "@/hooks/use-auth-permissions";
import {
  deleteCompanyEmailSettings,
  getCompanyEmailSettings,
  saveCompanyEmailSettings,
  sendCompanyTestEmail,
  type CompanyEmailSettings as Settings,
} from "@/lib/services/company-email-service";

const message = (e: unknown, fallback: string) => (e instanceof Error ? e.message : fallback);

/** The company's own Gmail account that receipts are sent from. */
export function CompanyEmailSettings() {
  const isSuperAdmin = useIsSuperAdmin();
  const hasUpdate = useHasPermission("CompanySettings.Update");
  const canEdit = isSuperAdmin || hasUpdate;

  const [saved, setSaved] = useState<Settings | null>(null);
  const [loadError, setLoadError] = useState<string | null>(null);
  const [email, setEmail] = useState("");
  const [senderName, setSenderName] = useState("");
  const [appPassword, setAppPassword] = useState("");
  const [testTo, setTestTo] = useState("");
  const [busy, setBusy] = useState<"save" | "test" | "delete" | null>(null);

  const show = (settings: Settings) => {
    setSaved(settings);
    setEmail(settings.email ?? "");
    setSenderName(settings.senderName ?? "");
    setAppPassword("");
  };

  useEffect(() => {
    let cancelled = false;
    getCompanyEmailSettings()
      .then((settings) => !cancelled && show(settings))
      .catch((e) => !cancelled && setLoadError(message(e, "Poçt ayarları yüklənmədi")));
    return () => {
      cancelled = true;
    };
  }, []);

  const changed =
    saved != null &&
    (email.trim() !== (saved.email ?? "") || senderName.trim() !== (saved.senderName ?? "") || appPassword.trim() !== "");

  async function run(kind: "save" | "test" | "delete", action: () => Promise<void>, failure: string) {
    setBusy(kind);
    try {
      await action();
    } catch (e) {
      toast.error(message(e, failure));
    } finally {
      setBusy(null);
    }
  }

  const save = () =>
    run(
      "save",
      async () => {
        show(
          await saveCompanyEmailSettings({
            email: email.trim(),
            senderName: senderName.trim() || null,
            appPassword: appPassword.trim() || null,
          }),
        );
        toast.success("Poçt ayarları yadda saxlanıldı.");
      },
      "Poçt ayarları yadda saxlanmadı",
    );

  const sendTest = () =>
    run(
      "test",
      async () => {
        await sendCompanyTestEmail(testTo.trim());
        toast.success("Test məktubu göndərildi.");
      },
      "Test məktubu göndərilmədi",
    );

  const remove = () => {
    if (!window.confirm("Poçt ayarları silinsin? Çekləri e-poçtla göndərmək mümkün olmayacaq.")) return;
    void run(
      "delete",
      async () => {
        await deleteCompanyEmailSettings();
        show({ isConfigured: false, email: null, senderName: null });
        toast.success("Poçt ayarları silindi.");
      },
      "Poçt ayarları silinmədi",
    );
  };

  return (
    <section className="space-y-4 rounded-xl border bg-card p-6">
      <div>
        <h2 className="text-lg font-semibold">E-poçt (Gmail)</h2>
        <p className="mt-0.5 text-xs text-muted-foreground">
          Çeklər bu ünvandan göndərilir. Adi Gmail parolu işləmir: Google hesabında iki mərhələli təsdiqi açın və{" "}
          <a
            href="https://myaccount.google.com/apppasswords"
            target="_blank"
            rel="noreferrer"
            className="text-primary underline"
          >
            App Password
          </a>{" "}
          yaradın.
        </p>
      </div>

      {loadError ? (
        <p className="text-sm text-red-600">{loadError}</p>
      ) : saved == null ? (
        <p className="text-sm text-muted-foreground">Yüklənir…</p>
      ) : (
        <>
          <form
            className="grid gap-4 md:grid-cols-3"
            onSubmit={(e) => {
              e.preventDefault();
              void save();
            }}
          >
            <div>
              <Label htmlFor="mail-address">Gmail ünvanı</Label>
              <Input
                id="mail-address"
                type="email"
                className="mt-1"
                autoComplete="off"
                disabled={!canEdit}
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                placeholder="restoran@gmail.com"
              />
            </div>
            <div>
              <Label htmlFor="mail-password">App Password</Label>
              <Input
                id="mail-password"
                type="password"
                className="mt-1"
                autoComplete="new-password"
                disabled={!canEdit}
                value={appPassword}
                onChange={(e) => setAppPassword(e.target.value)}
                placeholder={saved.isConfigured ? "Yazılıb — dəyişmək üçün yenisini yazın" : "16 simvolluq parol"}
              />
            </div>
            <div>
              <Label htmlFor="mail-sender">Göndərən adı (istəyə görə)</Label>
              <Input
                id="mail-sender"
                className="mt-1"
                maxLength={100}
                disabled={!canEdit}
                value={senderName}
                onChange={(e) => setSenderName(e.target.value)}
                placeholder="Boş olsa, şirkətin adı"
              />
            </div>
            {canEdit && (
              <div className="flex flex-wrap gap-2 md:col-span-3">
                <Button type="submit" disabled={busy != null || !changed || !email.trim()}>
                  {busy === "save" ? "Saxlanılır..." : "Gmail ayarını saxla"}
                </Button>
                {saved.isConfigured && (
                  <Button type="button" variant="outline" disabled={busy != null} onClick={remove}>
                    Ayarı sil
                  </Button>
                )}
              </div>
            )}
          </form>

          {canEdit && saved.isConfigured && (
            <div className="flex flex-wrap items-end gap-2 border-t pt-4">
              <div className="w-full max-w-sm">
                <Label htmlFor="mail-test">Test məktubu göndəriləcək ünvan</Label>
                <Input id="mail-test" type="email" className="mt-1" value={testTo} onChange={(e) => setTestTo(e.target.value)} />
              </div>
              <Button variant="outline" disabled={busy != null || changed || !testTo.trim()} onClick={() => void sendTest()}>
                {busy === "test" ? "Göndərilir..." : "Test məktubu göndər"}
              </Button>
              {changed && <p className="text-xs text-muted-foreground">Əvvəlcə dəyişiklikləri saxlayın.</p>}
            </div>
          )}
        </>
      )}
    </section>
  );
}
