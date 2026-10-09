"use client";

import { useEffect, useRef, useState } from "react";
import { useRouter } from "next/navigation";

import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Alert, AlertDescription } from "@/components/ui/alert";
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import { ChefHat, CreditCard, Delete, Monitor, RotateCw, Store } from "lucide-react";
import { cn } from "@/lib/utils";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { persistAuthUser } from "@/lib/auth-client";
import {
  clearPosTerminalContext,
  getPosTerminalContext,
  savePosTerminalContext,
  type PosTerminalContext,
} from "@/lib/pos-terminal-client";
import {
  lookupCompanyByCode,
  posLogin,
  verifyWorkstationChangeAccess,
  type RestaurantLookupItem,
  type WorkstationLookupItem,
} from "@/lib/services/pos-auth-service";
import {
  getCompanySettingsBranding,
  type CompanySettingsBranding,
} from "@/lib/services/company-settings-service";

const MAX_CODE_LENGTH = 24;

// Hard restart for a frozen terminal: drop service-worker/HTTP caches, then reload the page.
// Terminal context and auth live in localStorage, so the terminal stays configured.
async function restartTerminal() {
  try {
    if ("caches" in window) {
      const keys = await caches.keys();
      await Promise.all(keys.map((k) => caches.delete(k)));
    }
    if ("serviceWorker" in navigator) {
      const regs = await navigator.serviceWorker.getRegistrations();
      await Promise.all(regs.map((r) => r.unregister()));
    }
  } catch {
    // Reload regardless — a failed cache cleanup must not block the restart.
  }
  window.location.reload();
}

export default function PosLoginPage() {
  const router = useRouter();

  const [terminal, setTerminal] = useState<PosTerminalContext | null>(null);
  const [terminalChecked, setTerminalChecked] = useState(false);
  const [branding, setBranding] = useState<CompanySettingsBranding | null>(null);

  useEffect(() => {
    setTerminal(getPosTerminalContext());
    setTerminalChecked(true);
  }, []);

  useEffect(() => {
    if (!terminal) {
      setBranding(null);
      return;
    }
    let cancelled = false;
    getCompanySettingsBranding(terminal.companyId)
      .then((b) => {
        if (!cancelled) setBranding(b);
      })
      .catch(() => {
        if (!cancelled) setBranding(null);
      });
    return () => {
      cancelled = true;
    };
  }, [terminal]);

  if (!terminalChecked) return null;

  const wallpaperOpacity =
    branding?.transparencyLevel != null ? Math.min(Math.max(branding.transparencyLevel, 0), 100) / 100 : 1;

  return (
    <div className="relative flex min-h-screen items-center justify-center bg-muted/30 p-4">
      {branding?.wallpaperUrl && (
        <div
          className="pointer-events-none absolute inset-0 bg-cover bg-center"
          style={{ backgroundImage: `url(${branding.wallpaperUrl})`, opacity: wallpaperOpacity }}
          aria-hidden
        />
      )}
      <Button
        type="button"
        variant="outline"
        size="sm"
        className="absolute right-4 top-4 z-20 gap-2"
        onClick={() => void restartTerminal()}
      >
        <RotateCw className="h-4 w-4" />
        Yenidən başlat
      </Button>
      <div className="relative z-10 w-full max-w-md">
        {terminal ? (
          <PosLoginView
            terminal={terminal}
            logoUrl={branding?.loginLogoUrl ?? null}
            location={branding?.loginLocation ?? null}
            onChangeTerminal={() => {
              clearPosTerminalContext();
              setTerminal(null);
            }}
            onSuccess={() => router.replace("/pos")}
          />
        ) : (
          <TerminalSetupView
            onDone={(ctx) => {
              savePosTerminalContext(ctx);
              setTerminal(ctx);
            }}
          />
        )}
      </div>
    </div>
  );
}

function TerminalSetupView({
  onDone,
}: {
  onDone: (context: PosTerminalContext) => void;
}) {
  const [companyCode, setCompanyCode] = useState("");
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [lookup, setLookup] = useState<{
    companyId: number;
    companyName: string;
    restaurants: RestaurantLookupItem[];
    workstations: WorkstationLookupItem[];
  } | null>(null);
  // Set once a branch is chosen and the admin registered workstations for it: the operator still has
  // to pick which monitor this is.
  const [pendingRestaurant, setPendingRestaurant] = useState<RestaurantLookupItem | null | undefined>(undefined);

  const handleLookup = async (e: React.FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    setError(null);
    const trimmed = companyCode.trim();
    if (!trimmed) return;

    setIsLoading(true);
    try {
      const result = await lookupCompanyByCode(trimmed);
      setLookup(result);
      if (result.restaurants.length === 0) {
        if (result.workstations.length === 0) {
          onDone({
            companyId: result.companyId,
            companyCode: trimmed,
            companyName: result.companyName,
            restaurantId: null,
            restaurantName: null,
            workstationId: null,
            workstationName: null,
          });
        } else {
          setPendingRestaurant(null);
        }
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : "Company was not found");
    } finally {
      setIsLoading(false);
    }
  };

  const workstationsFor = (restaurant: RestaurantLookupItem | null) =>
    (lookup?.workstations ?? []).filter((t) => t.restaurantId === null || t.restaurantId === restaurant?.id);

  const finishSetup = (restaurant: RestaurantLookupItem | null, workstation: WorkstationLookupItem | null) => {
    if (!lookup) return;
    onDone({
      companyId: lookup.companyId,
      companyCode: companyCode.trim(),
      companyName: lookup.companyName,
      restaurantId: restaurant?.id ?? null,
      restaurantName: restaurant?.name ?? null,
      workstationId: workstation?.id ?? null,
      workstationName: workstation?.name ?? null,
    });
  };

  const selectRestaurant = (restaurant: RestaurantLookupItem | null) => {
    if (workstationsFor(restaurant).length === 0) {
      finishSetup(restaurant, null);
      return;
    }
    setPendingRestaurant(restaurant);
  };

  return (
    <Card className="w-full max-w-md">
      <CardHeader className="text-center">
        <div className="mx-auto mb-2 flex h-12 w-12 items-center justify-center rounded-2xl bg-primary/10 text-primary">
          <Store className="h-6 w-6" />
        </div>
        <CardTitle>Terminal quraşdırılması</CardTitle>
        <CardDescription>
          {pendingRestaurant !== undefined
            ? "Bu monitorun adını seçin"
            : lookup
            ? "Bu terminalın işləyəcəyi filialı seçin"
            : "Bu terminalın aid olduğu biznesin kodunu daxil edin"}
        </CardDescription>
      </CardHeader>
      <CardContent>
        {!lookup ? (
          <form onSubmit={handleLookup} className="space-y-4">
            {error && (
              <Alert variant="destructive">
                <AlertDescription>{error}</AlertDescription>
              </Alert>
            )}
            <div className="space-y-2">
              <Label htmlFor="company-code">Biznes kodu</Label>
              <Input
                id="company-code"
                placeholder="Məs: FOODERA001"
                value={companyCode}
                onChange={(e) => setCompanyCode(e.target.value)}
                autoFocus
                required
              />
            </div>
            <Button type="submit" disabled={isLoading} className="h-11 w-full">
              {isLoading ? "Axtarılır..." : "Davam et"}
            </Button>
          </form>
        ) : pendingRestaurant !== undefined ? (
          <div className="space-y-3">
            <p className="text-sm font-medium text-foreground">
              {lookup.companyName}
              {pendingRestaurant ? ` · ${pendingRestaurant.name}` : ""}
            </p>
            <div className="grid gap-2">
              {workstationsFor(pendingRestaurant).map((workstation) => (
                <Button
                  key={workstation.id}
                  type="button"
                  variant="outline"
                  className="h-12 justify-start text-base"
                  onClick={() => finishSetup(pendingRestaurant, workstation)}
                >
                  <Monitor className="mr-2 h-4 w-4" />
                  {workstation.name}
                </Button>
              ))}
            </div>
            <Button
              type="button"
              variant="ghost"
              className="w-full text-muted-foreground"
              onClick={() => {
                if (lookup.restaurants.length === 0) {
                  setLookup(null);
                }
                setPendingRestaurant(undefined);
                setError(null);
              }}
            >
              Geri
            </Button>
          </div>
        ) : (
          <div className="space-y-3">
            <p className="text-sm font-medium text-foreground">{lookup.companyName}</p>
            <div className="grid gap-2">
              {lookup.restaurants.map((restaurant) => (
                <Button
                  key={restaurant.id}
                  type="button"
                  variant="outline"
                  className="h-12 justify-start text-base"
                  onClick={() => selectRestaurant(restaurant)}
                >
                  <Store className="mr-2 h-4 w-4" />
                  {restaurant.name}
                </Button>
              ))}
            </div>
            <Button
              type="button"
              variant="outline"
              className="w-full text-muted-foreground"
              onClick={() => selectRestaurant(null)}
            >
              Filial seçmədən davam et
            </Button>
            <Button
              type="button"
              variant="ghost"
              className="w-full text-muted-foreground"
              onClick={() => {
                setLookup(null);
                setError(null);
              }}
            >
              Geri
            </Button>
          </div>
        )}
      </CardContent>
    </Card>
  );
}

function PosLoginView({
  terminal,
  logoUrl,
  location,
  onChangeTerminal,
  onSuccess,
}: {
  terminal: PosTerminalContext;
  logoUrl: string | null;
  location: string | null;
  onChangeTerminal: () => void;
  onSuccess: () => void;
}) {
  const [code, setCode] = useState("");
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const rfidBufferRef = useRef("");
  const rfidInputRef = useRef<HTMLInputElement | null>(null);

  // Once a workstation is chosen the terminal is locked to it: moving the terminal to another
  // monitor identity needs an admin (otherwise problems could never be traced to a screen).
  const isLocked = terminal.workstationId !== null;
  const [unlockOpen, setUnlockOpen] = useState(false);
  const [unlockUser, setUnlockUser] = useState("");
  const [unlockPassword, setUnlockPassword] = useState("");
  const [unlockError, setUnlockError] = useState<string | null>(null);
  const [unlockChecking, setUnlockChecking] = useState(false);
  const unlockOpenRef = useRef(false);
  unlockOpenRef.current = unlockOpen;

  const closeUnlock = () => {
    setUnlockOpen(false);
    setUnlockUser("");
    setUnlockPassword("");
    setUnlockError(null);
  };

  const handleChangeTerminal = () => {
    if (!isLocked) {
      onChangeTerminal();
      return;
    }
    setUnlockOpen(true);
  };

  const submitUnlock = async (e: React.FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    if (!unlockUser.trim() || !unlockPassword) return;
    setUnlockChecking(true);
    setUnlockError(null);
    const allowed = await verifyWorkstationChangeAccess(unlockUser.trim(), unlockPassword);
    setUnlockChecking(false);
    if (!allowed) {
      setUnlockError("Giriş məlumatları yanlışdır və ya terminalı dəyişmək icazəniz yoxdur.");
      return;
    }
    closeUnlock();
    onChangeTerminal();
  };

  useEffect(() => {
    const focusRfid = () => {
      if (unlockOpenRef.current) return;
      rfidInputRef.current?.focus();
    };
    focusRfid();
    const interval = window.setInterval(focusRfid, 1000);
    return () => window.clearInterval(interval);
  }, []);

  const finishLogin = (result: { accessToken: string; refreshToken?: string; permissions: string[]; roles: string[] }) => {
    localStorage.setItem("token", result.accessToken);
    persistAuthUser({ roles: result.roles, permissions: result.permissions });
    if (result.refreshToken) {
      localStorage.setItem("refreshToken", result.refreshToken);
    } else {
      localStorage.removeItem("refreshToken");
    }
    onSuccess();
  };

  const submitCode = async (value: string) => {
    if (value.length < 1) return;
    setError(null);
    setIsLoading(true);
    try {
      const result = await posLogin({
        companyId: terminal.companyId,
        restaurantId: terminal.restaurantId,
        workstationId: terminal.workstationId,
        code: value,
      });
      finishLogin(result);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Kod yanlışdır");
      setCode("");
    } finally {
      setIsLoading(false);
    }
  };

  const submitRfid = async (rfidCardId: string) => {
    setError(null);
    setIsLoading(true);
    try {
      const result = await posLogin({
        companyId: terminal.companyId,
        restaurantId: terminal.restaurantId,
        rfidCardId,
      });
      finishLogin(result);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Kart tanınmadı");
    } finally {
      setIsLoading(false);
    }
  };

  const pressDigit = (digit: string) => {
    if (isLoading) return;
    setError(null);
    setCode((prev) => {
      if (prev.length >= MAX_CODE_LENGTH) return prev;
      return prev + digit;
    });
  };

  const pressBackspace = () => {
    if (isLoading) return;
    setError(null);
    setCode((prev) => prev.slice(0, -1));
  };

  return (
    <Card className="w-full max-w-sm">
      <CardHeader className="text-center">
        {logoUrl ? (
          // eslint-disable-next-line @next/next/no-img-element
          <img
            src={logoUrl}
            alt={terminal.companyName}
            className="mx-auto mb-2 h-12 w-12 rounded-2xl object-contain"
          />
        ) : (
          <div className="mx-auto mb-2 flex h-12 w-12 items-center justify-center rounded-2xl bg-primary/10 text-primary">
            <ChefHat className="h-6 w-6" />
          </div>
        )}
        <CardTitle>{terminal.companyName}</CardTitle>
        {terminal.restaurantName && (
          <CardDescription>{terminal.restaurantName}</CardDescription>
        )}
        {terminal.workstationName && (
          <p className="inline-flex items-center justify-center gap-1.5 text-sm font-medium text-foreground">
            <Monitor className="h-4 w-4 text-muted-foreground" />
            {terminal.workstationName}
          </p>
        )}
        {location && <p className="text-xs text-muted-foreground">{location}</p>}
      </CardHeader>
      <CardContent className="space-y-5">
        {error && (
          <Alert variant="destructive">
            <AlertDescription>{error}</AlertDescription>
          </Alert>
        )}

        <div className="flex items-center justify-center gap-2">
          {Array.from({ length: Math.max(code.length, 4) }).map((_, i) => (
            <span
              key={i}
              className={cn(
                "h-3 w-3 rounded-full border border-primary/40",
                i < code.length ? "bg-primary" : "bg-transparent",
              )}
            />
          ))}
        </div>

        <div className="grid grid-cols-3 gap-2">
          {["1", "2", "3", "4", "5", "6", "7", "8", "9"].map((digit) => (
            <Button
              key={digit}
              type="button"
              variant="outline"
              disabled={isLoading}
              className="h-14 text-xl font-semibold"
              onClick={() => pressDigit(digit)}
            >
              {digit}
            </Button>
          ))}
          <Button
            type="button"
            variant="ghost"
            disabled={isLoading}
            className="h-14"
            onClick={pressBackspace}
            aria-label="Sil"
          >
            <Delete className="h-5 w-5" />
          </Button>
          <Button
            type="button"
            variant="outline"
            disabled={isLoading}
            className="h-14 text-xl font-semibold"
            onClick={() => pressDigit("0")}
          >
            0
          </Button>
          <Button
            type="button"
            disabled={isLoading || code.length < 1}
            className="h-14 text-sm font-semibold"
            onClick={() => submitCode(code)}
          >
            {isLoading ? "..." : "Daxil ol"}
          </Button>
        </div>

        <div className="flex items-center justify-center gap-2 text-xs text-muted-foreground">
          <CreditCard className="h-4 w-4" />
          RFID kart oxutmaqla da giriş edə bilərsiniz
        </div>

        <input
          ref={rfidInputRef}
          type="text"
          className="sr-only"
          aria-hidden
          tabIndex={-1}
          autoComplete="off"
          onChange={(e) => {
            rfidBufferRef.current = e.target.value;
          }}
          onKeyDown={(e) => {
            if (e.key === "Enter") {
              const value = rfidBufferRef.current.trim();
              rfidBufferRef.current = "";
              if (rfidInputRef.current) rfidInputRef.current.value = "";
              if (value) void submitRfid(value);
            }
          }}
        />

        <div className="flex items-center justify-between">
          <Button
            type="button"
            variant="link"
            className="px-0 text-muted-foreground"
            onClick={handleChangeTerminal}
          >
            Terminalı dəyiş
          </Button>
          <a
            href="/login"
            className="text-sm text-muted-foreground hover:text-foreground hover:underline"
          >
            Adi giriş
          </a>
        </div>

        <Dialog open={unlockOpen} onOpenChange={(o) => (o ? setUnlockOpen(true) : closeUnlock())}>
          <DialogContent className="sm:max-w-sm">
            <DialogHeader>
              <DialogTitle>Terminalı dəyiş</DialogTitle>
              <DialogDescription>
                Bu monitor "{terminal.workstationName}" adına bağlıdır. Dəyişmək üçün admin məlumatlarını
                daxil edin.
              </DialogDescription>
            </DialogHeader>
            <form onSubmit={submitUnlock} className="space-y-3">
              {unlockError && (
                <Alert variant="destructive">
                  <AlertDescription>{unlockError}</AlertDescription>
                </Alert>
              )}
              <div className="space-y-1">
                <Label htmlFor="unlock-user">İstifadəçi adı</Label>
                <Input
                  id="unlock-user"
                  value={unlockUser}
                  onChange={(e) => setUnlockUser(e.target.value)}
                  autoComplete="off"
                  autoFocus
                />
              </div>
              <div className="space-y-1">
                <Label htmlFor="unlock-password">Şifrə / kod</Label>
                <Input
                  id="unlock-password"
                  type="password"
                  value={unlockPassword}
                  onChange={(e) => setUnlockPassword(e.target.value)}
                  autoComplete="off"
                />
              </div>
              <div className="flex justify-end gap-2">
                <Button type="button" variant="outline" onClick={closeUnlock} disabled={unlockChecking}>
                  Ləğv et
                </Button>
                <Button type="submit" disabled={unlockChecking || !unlockUser.trim() || !unlockPassword}>
                  {unlockChecking ? "Yoxlanılır…" : "Təsdiqlə"}
                </Button>
              </div>
            </form>
          </DialogContent>
        </Dialog>
      </CardContent>
    </Card>
  );
}
