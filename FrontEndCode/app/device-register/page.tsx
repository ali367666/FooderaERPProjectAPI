"use client";

import { Suspense, useState } from "react";
import { useRouter, useSearchParams } from "next/navigation";
import { MonitorSmartphone, ShieldCheck } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { registerDevice } from "@/lib/services/license-service";
import { getDeviceName, storeDevice } from "@/lib/device-key";

function DeviceRegisterForm() {
  const router = useRouter();
  const params = useSearchParams();
  const next = params.get("next") || "/dashboard";

  const [code, setCode] = useState("");
  const [name, setName] = useState(getDeviceName() ?? "");
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [done, setDone] = useState(false);

  const handleSubmit = async () => {
    setError(null);
    if (!code.trim() || !name.trim()) {
      setError("Kodu və cihazın adını daxil edin.");
      return;
    }
    setSaving(true);
    try {
      const result = await registerDevice(code.trim(), name.trim());
      storeDevice(result.deviceKey, result.deviceName);
      setDone(true);
      setTimeout(() => router.replace(next.startsWith("/") ? next : "/dashboard"), 1200);
    } catch (e) {
      setError(e instanceof Error ? e.message : "Cihaz qeydiyyatdan keçmədi.");
    } finally {
      setSaving(false);
    }
  };

  return (
    <div className="flex min-h-screen items-center justify-center bg-muted/30 p-4">
      <div className="w-full max-w-md space-y-5 rounded-xl border bg-card p-6 shadow-sm">
        <div className="flex flex-col items-center gap-2 text-center">
          <div className="flex h-12 w-12 items-center justify-center rounded-xl bg-primary/10">
            {done ? <ShieldCheck className="h-6 w-6 text-primary" /> : <MonitorSmartphone className="h-6 w-6 text-primary" />}
          </div>
          <h1 className="text-lg font-semibold">{done ? "Cihaz qeydiyyatdan keçdi" : "Cihaz qeydiyyatı"}</h1>
          <p className="text-sm text-muted-foreground">
            {done
              ? "Yönləndirilirsiniz…"
              : "Bu cihazdan sistemə giriş üçün icazə yoxdur. Şirkətiniz Offline rejimdədir — sistemə yalnız restoranın qeydiyyatlı cihazlarından daxil olmaq olar. Sizə verilmiş birdəfəlik kodu daxil edin."}
          </p>
        </div>

        {!done && (
          <form
            className="space-y-4"
            onSubmit={(e) => {
              e.preventDefault();
              void handleSubmit();
            }}
          >
            <div className="space-y-1">
              <Label htmlFor="reg-code">Qeydiyyat kodu</Label>
              <Input
                id="reg-code"
                value={code}
                onChange={(e) => setCode(e.target.value.toUpperCase())}
                placeholder="XXXX-XXXX"
                autoComplete="off"
                className="text-center font-mono text-lg tracking-widest"
              />
            </div>
            <div className="space-y-1">
              <Label htmlFor="reg-name">Cihazın adı</Label>
              <Input
                id="reg-name"
                value={name}
                onChange={(e) => setName(e.target.value)}
                placeholder="məs. Kassa 1, Ofisiant planşeti"
              />
            </div>
            {error && <p className="text-sm text-destructive">{error}</p>}
            <Button type="submit" className="w-full" disabled={saving}>
              {saving ? "Yoxlanılır…" : "Qeydiyyatdan keçir"}
            </Button>
            <p className="text-center text-xs text-muted-foreground">
              Kodu xidmət təminatçınızdan alın. Kod birdəfəlikdir və 24 saat keçərlidir.
            </p>
          </form>
        )}
      </div>
    </div>
  );
}

export default function DeviceRegisterPage() {
  return (
    <Suspense>
      <DeviceRegisterForm />
    </Suspense>
  );
}
