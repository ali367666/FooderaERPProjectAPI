"use client";

import { Suspense, useState } from "react";
import { useRouter, useSearchParams } from "next/navigation";
import { KeyRound, ShieldCheck } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Label } from "@/components/ui/label";
import { importLicenseKey } from "@/lib/services/license-service";

function LicenseKeyForm() {
  const router = useRouter();
  const params = useSearchParams();
  const next = params.get("next") || "/dashboard";

  const [key, setKey] = useState("");
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [activatedUntil, setActivatedUntil] = useState<string | null>(null);

  const handleSubmit = async () => {
    setError(null);
    if (!key.trim()) {
      setError("Lisenziya açarını daxil edin.");
      return;
    }
    setSaving(true);
    try {
      const status = await importLicenseKey(key.trim());
      setActivatedUntil(status.licenseKeyExpiresAtUtc);
      setTimeout(() => router.replace(next.startsWith("/") ? next : "/dashboard"), 1500);
    } catch (e) {
      setError(e instanceof Error ? e.message : "Lisenziya açarı qəbul edilmədi.");
    } finally {
      setSaving(false);
    }
  };

  return (
    <div className="flex min-h-screen items-center justify-center bg-muted/30 p-4">
      <div className="w-full max-w-lg space-y-5 rounded-xl border bg-card p-6 shadow-sm">
        <div className="flex flex-col items-center gap-2 text-center">
          <div className="flex h-12 w-12 items-center justify-center rounded-xl bg-primary/10">
            {activatedUntil ? <ShieldCheck className="h-6 w-6 text-primary" /> : <KeyRound className="h-6 w-6 text-primary" />}
          </div>
          <h1 className="text-lg font-semibold">{activatedUntil ? "Lisenziya aktivləşdi" : "Lisenziya açarı"}</h1>
          <p className="text-sm text-muted-foreground">
            {activatedUntil
              ? `Sistem ${new Date(activatedUntil).toLocaleDateString("az-AZ")} tarixinə qədər aktivdir. Yönləndirilirsiniz…`
              : "Bu yerli quraşdırmanın lisenziya açarı yoxdur və ya vaxtı bitib. Xidmət təminatçınızdan aldığınız açarı aşağıya yapışdırın."}
          </p>
        </div>

        {!activatedUntil && (
          <form
            className="space-y-4"
            onSubmit={(e) => {
              e.preventDefault();
              void handleSubmit();
            }}
          >
            <div className="space-y-1">
              <Label htmlFor="license-key">Açar</Label>
              <textarea
                id="license-key"
                value={key}
                onChange={(e) => setKey(e.target.value)}
                placeholder="FOODERA-…"
                className="h-32 w-full rounded-md border border-input bg-background p-2 font-mono text-xs"
              />
            </div>
            {error && <p className="text-sm text-destructive">{error}</p>}
            <Button type="submit" className="w-full" disabled={saving}>
              {saving ? "Yoxlanılır…" : "Aktivləşdir"}
            </Button>
            <p className="text-center text-xs text-muted-foreground">Açar internetsiz yoxlanılır.</p>
          </form>
        )}
      </div>
    </div>
  );
}

export default function LicensePage() {
  return (
    <Suspense>
      <LicenseKeyForm />
    </Suspense>
  );
}
