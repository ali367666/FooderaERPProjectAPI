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
import {
  ArrowLeftRight,
  CalendarDays,
  ChefHat,
  Clock3,
  CreditCard,
  Delete,
  Headset,
  LogIn,
  MapPin,
  Maximize,
  Minimize,
  Monitor,
  Power,
  Settings,
  ShieldAlert,
  ShieldCheck,
  Store,
  Tag,
  Zap,
} from "lucide-react";
import { format } from "date-fns";
import { toast } from "sonner";
import { cn } from "@/lib/utils";
import { Toaster } from "@/components/ui/sonner";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
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
// Shown on the login screen's "Servis xidməti" tile.
const SERVICE_PHONE = "+994 55 200 08 97";
// The PIN field shows one dot per digit up to this many, then a "+n" counter.
const MAX_VISIBLE_DOTS = 12;
const APP_VERSION = process.env.NEXT_PUBLIC_APP_VERSION ?? null;

function useNow(intervalMs = 1000) {
  const [now, setNow] = useState(() => new Date());
  useEffect(() => {
    const id = window.setInterval(() => setNow(new Date()), intervalMs);
    return () => window.clearInterval(id);
  }, [intervalMs]);
  return now;
}

function useFullscreen() {
  const [isFullscreen, setIsFullscreen] = useState(false);
  useEffect(() => {
    const sync = () => setIsFullscreen(Boolean(document.fullscreenElement));
    sync();
    document.addEventListener("fullscreenchange", sync);
    return () => document.removeEventListener("fullscreenchange", sync);
  }, []);
  const toggle = () => {
    // Some browsers (iPad Safari) have no Fullscreen API at all — report it instead of throwing.
    const action = document.fullscreenElement
      ? document.exitFullscreen?.()
      : document.documentElement.requestFullscreen?.();
    if (!action) {
      toast.error("Bu brauzer tam ekran rejimini dəstəkləmir");
      return;
    }
    action.catch(() => toast.error("Tam ekran rejimi dəyişdirilə bilmədi"));
  };
  return { isFullscreen, toggle };
}

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
    <div className="relative min-h-dvh overflow-hidden bg-[#0b3d2e] bg-[radial-gradient(ellipse_at_top_left,#16573f_0%,#0b3d2e_45%,#06261c_100%)]">
      <Toaster richColors closeButton position="top-center" />
      {branding?.wallpaperUrl && (
        <div
          className="pointer-events-none absolute inset-0 bg-cover bg-center"
          style={{ backgroundImage: `url(${branding.wallpaperUrl})`, opacity: wallpaperOpacity }}
          aria-hidden
        />
      )}
      {terminal ? (
        <PosLoginView
          terminal={terminal}
          logoUrl={branding?.loginLogoUrl ?? null}
          location={branding?.loginLocation ?? null}
          licenseDaysLeft={branding?.licenseDaysLeft ?? null}
          onChangeTerminal={() => {
            clearPosTerminalContext();
            setTerminal(null);
          }}
          onSuccess={() => router.replace("/pos")}
        />
      ) : (
        <div className="relative z-10 flex min-h-dvh items-center justify-center p-4">
          <TerminalSetupView
            onDone={(ctx) => {
              savePosTerminalContext(ctx);
              setTerminal(ctx);
            }}
          />
        </div>
      )}
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
  licenseDaysLeft,
  onChangeTerminal,
  onSuccess,
}: {
  terminal: PosTerminalContext;
  logoUrl: string | null;
  location: string | null;
  licenseDaysLeft: number | null;
  onChangeTerminal: () => void;
  onSuccess: () => void;
}) {
  const [code, setCode] = useState("");
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const rfidBufferRef = useRef("");
  const rfidInputRef = useRef<HTMLInputElement | null>(null);
  const now = useNow();
  const fullscreen = useFullscreen();

  const [settingsOpen, setSettingsOpen] = useState(false);
  const [exitOpen, setExitOpen] = useState(false);
  const [isRestarting, setIsRestarting] = useState(false);

  // Once a workstation is chosen the terminal is locked to it: moving the terminal to another
  // monitor identity needs an admin (otherwise problems could never be traced to a screen).
  const isLocked = terminal.workstationId !== null;
  const [unlockOpen, setUnlockOpen] = useState(false);
  const [unlockUser, setUnlockUser] = useState("");
  const [unlockPassword, setUnlockPassword] = useState("");
  const [unlockError, setUnlockError] = useState<string | null>(null);
  const [unlockChecking, setUnlockChecking] = useState(false);
  // While any dialog is open the RFID input must not steal focus from it.
  const dialogOpenRef = useRef(false);
  dialogOpenRef.current = unlockOpen || settingsOpen || exitOpen;

  const closeUnlock = () => {
    setUnlockOpen(false);
    setUnlockUser("");
    setUnlockPassword("");
    setUnlockError(null);
  };

  const handleChangeTerminal = () => {
    setSettingsOpen(false);
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
      if (dialogOpenRef.current) return;
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

  const pressClear = () => {
    if (isLoading) return;
    setError(null);
    setCode("");
  };

  const speedUp = () => {
    setIsRestarting(true);
    void restartTerminal();
  };

  const exitProgram = async () => {
    setExitOpen(false);
    if (document.fullscreenElement) {
      await document.exitFullscreen().catch(() => undefined);
    }
    window.close();
    // Browsers only let scripts close windows they opened themselves; a kiosk/desktop shell can.
    window.setTimeout(() => {
      if (!window.closed) {
        toast.info("Brauzer bu pəncərəni bağlamağa icazə vermir. Pəncərəni əl ilə bağlayın.");
      }
    }, 300);
  };

  const restaurantLabel = terminal.companyName;
  const visibleDots = Math.min(code.length, MAX_VISIBLE_DOTS);
  const hiddenDots = code.length - visibleDots;

  return (
    <div className="relative z-10 flex min-h-dvh flex-col lg:h-dvh">
      <main className="grid flex-1 gap-4 p-4 lg:min-h-0 lg:grid-cols-[minmax(0,1fr)_minmax(360px,440px)] lg:gap-6 lg:p-6">
        {/* Left: logo display + licence / service */}
        <section className="order-2 flex min-h-0 flex-col gap-4 rounded-3xl bg-card/95 p-4 shadow-2xl ring-1 ring-black/5 backdrop-blur lg:order-1 lg:p-6">
          <div className="relative flex min-h-48 flex-1 flex-col items-center justify-center gap-4 overflow-hidden rounded-2xl border border-dashed border-border bg-muted/40 p-6">
            {logoUrl ? (
              // eslint-disable-next-line @next/next/no-img-element
              <img
                src={logoUrl}
                alt={terminal.companyName}
                className="max-h-full w-full max-w-xl flex-1 object-contain lg:max-h-[52vh]"
              />
            ) : (
              <div className="flex flex-col items-center gap-3 text-center">
                <div className="flex h-24 w-24 items-center justify-center rounded-3xl bg-primary text-primary-foreground shadow-lg lg:h-32 lg:w-32">
                  <ChefHat className="h-12 w-12 lg:h-16 lg:w-16" />
                </div>
                <p className="text-2xl font-bold tracking-tight text-foreground lg:text-3xl">Foodera</p>
              </div>
            )}
            <div className="text-center">
              <p className="text-base font-semibold text-foreground lg:text-lg">{terminal.companyName}</p>
              {location && (
                <p className="mt-0.5 inline-flex items-center gap-1 text-sm text-muted-foreground">
                  <MapPin className="h-3.5 w-3.5" />
                  {location}
                </p>
              )}
            </div>
          </div>

          <div className="flex flex-col gap-3 sm:flex-row sm:items-stretch sm:justify-between">
            <LicenseTile daysLeft={licenseDaysLeft} />
            <div className="inline-flex items-center gap-3 rounded-xl border bg-background px-4 py-3">
              <span className="flex h-9 w-9 items-center justify-center rounded-lg bg-primary/10 text-primary">
                <Headset className="h-5 w-5" />
              </span>
              <span className="text-sm leading-tight">
                <span className="block font-semibold text-foreground">Servis xidməti</span>
                <span className="block select-all font-bold tabular-nums tracking-wide text-primary">{SERVICE_PHONE}</span>
              </span>
            </div>
          </div>
        </section>

        {/* Right: PIN login */}
        <section className="order-1 flex min-h-0 flex-col rounded-3xl lg:justify-center bg-card/95 p-5 shadow-2xl ring-1 ring-black/5 backdrop-blur lg:order-2 lg:p-6">
          <header className="mb-4 text-center">
            <h1 className="text-3xl font-black tracking-tight text-foreground lg:text-4xl">
              POS<span className="mx-1 font-light text-primary/50">/</span>
              <span className="text-primary">PRO</span>
            </h1>
            <p className="mt-0.5 text-sm font-medium uppercase tracking-[0.2em] text-muted-foreground">
              Restoran System
            </p>
          </header>

          {error && (
            <Alert variant="destructive" className="mb-3 py-2">
              <AlertDescription>{error}</AlertDescription>
            </Alert>
          )}

          <div className="mb-4 flex gap-2">
            <div
              className={cn(
                "flex h-14 min-w-0 flex-1 items-center gap-2 rounded-xl border-2 bg-background px-4 transition-colors",
                error ? "border-destructive/60" : code ? "border-primary/60" : "border-border",
              )}
              aria-label={`Daxil edilmiş rəqəm sayı: ${code.length}`}
              role="status"
            >
              {code.length === 0 ? (
                <span className="truncate text-sm text-muted-foreground">PIN kodu daxil edin</span>
              ) : (
                <div className="flex min-w-0 flex-1 items-center gap-2">
                  {Array.from({ length: visibleDots }).map((_, i) => (
                    <span key={i} className="h-3 w-3 shrink-0 rounded-full bg-foreground" />
                  ))}
                  {hiddenDots > 0 && (
                    <span className="text-xs font-medium text-muted-foreground">+{hiddenDots}</span>
                  )}
                </div>
              )}
              {code.length > 0 && (
                <button
                  type="button"
                  onMouseDown={(e) => e.preventDefault()}
                  onClick={pressBackspace}
                  disabled={isLoading}
                  className="ml-auto flex h-9 w-9 shrink-0 items-center justify-center rounded-lg text-muted-foreground transition-colors hover:bg-muted hover:text-foreground disabled:opacity-50"
                  aria-label="Sil"
                  title="Son rəqəmi sil"
                >
                  <Delete className="h-5 w-5" />
                </button>
              )}
            </div>
            <KeyButton
              onClick={pressClear}
              disabled={isLoading || code.length === 0}
              className="h-14 w-14 shrink-0 border-amber-300 bg-amber-50 text-xl font-bold text-amber-700 hover:bg-amber-100 dark:border-amber-500/40 dark:bg-amber-500/10 dark:text-amber-400"
              aria-label="Təmizlə"
              title="Hamısını təmizlə"
            >
              C
            </KeyButton>
          </div>

          <div className="grid grid-cols-3 gap-2.5 lg:gap-3">
            {["7", "8", "9", "4", "5", "6", "1", "2", "3"].map((digit) => (
              <KeyButton key={digit} disabled={isLoading} onClick={() => pressDigit(digit)}>
                {digit}
              </KeyButton>
            ))}
            <KeyButton
              onClick={() => setSettingsOpen(true)}
              className="flex-col gap-0.5 bg-muted/60 text-muted-foreground hover:text-foreground"
              aria-label="Ayarlar"
            >
              <Settings className="h-5 w-5" />
              <span className="text-[11px] font-medium">Ayarlar</span>
            </KeyButton>
            <KeyButton disabled={isLoading} onClick={() => pressDigit("0")}>
              0
            </KeyButton>
            <KeyButton
              onClick={() => setExitOpen(true)}
              className="bg-muted/60 text-destructive hover:bg-destructive/10"
              aria-label="Çıxış"
              title="Proqramdan çıxış"
            >
              <Power className="h-6 w-6" />
            </KeyButton>
          </div>

          <Button
            type="button"
            onMouseDown={(e) => e.preventDefault()}
            disabled={isLoading || code.length < 1}
            className="mt-3 h-[clamp(3.5rem,8vh,4.75rem)] w-full rounded-2xl text-2xl font-bold tracking-widest shadow-md lg:mt-4"
            onClick={() => submitCode(code)}
          >
            {isLoading ? "..." : "OK"}
          </Button>

          <div className="mt-3 flex items-center justify-center gap-2 text-xs text-muted-foreground">
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
        </section>
      </main>

      {/* Bottom information bar */}
      <footer className="flex flex-wrap items-stretch gap-px border-t border-white/10 bg-black/40 text-white backdrop-blur-md">
        <InfoSegment icon={<Store className="h-4 w-4" />} label="Restoran" value={restaurantLabel} />
        <InfoSegment
          icon={<Monitor className="h-4 w-4" />}
          label="Workstation"
          value={terminal.workstationName ?? "Təyin olunmayıb"}
        />
        <InfoSegment icon={<Tag className="h-4 w-4" />} label="Versiya" value={APP_VERSION ?? "—"} />
        <div className="flex items-center px-3 py-2">
          <button
            type="button"
            onClick={speedUp}
            disabled={isRestarting}
            className="inline-flex items-center gap-2 rounded-lg border border-white/20 bg-white/10 px-3 py-2 text-sm font-medium transition-colors hover:bg-white/20 active:scale-[0.98] disabled:opacity-60"
            title="Keşi təmizləyib proqramı yenidən yükləyir"
          >
            <Zap className="h-4 w-4 text-amber-300" />
            {isRestarting ? "Yenilənir…" : "Proqramı sürətləndir"}
          </button>
        </div>
        <div className="ml-auto flex items-stretch">
          <InfoSegment
            icon={<CalendarDays className="h-4 w-4" />}
            label="Tarix"
            value={format(now, "dd.MM.yyyy")}
          />
          <InfoSegment
            icon={<Clock3 className="h-4 w-4" />}
            label="Saat"
            value={format(now, "HH:mm")}
            valueClassName="text-lg tabular-nums"
          />
        </div>
      </footer>

      {/* Ayarlar */}
      <Dialog open={settingsOpen} onOpenChange={setSettingsOpen}>
        <DialogContent className="sm:max-w-sm">
          <DialogHeader>
            <DialogTitle>Ayarlar</DialogTitle>
            <DialogDescription>Terminal ayarları</DialogDescription>
          </DialogHeader>
          <div className="grid gap-2">
            <Button
              type="button"
              variant="outline"
              className="h-12 justify-start gap-3 text-base"
              onClick={fullscreen.toggle}
            >
              {fullscreen.isFullscreen ? <Minimize className="h-4 w-4" /> : <Maximize className="h-4 w-4" />}
              {fullscreen.isFullscreen ? "Tam ekrandan çıx" : "Tam ekran"}
            </Button>
            <Button
              type="button"
              variant="outline"
              className="h-12 justify-start gap-3 text-base"
              onClick={handleChangeTerminal}
            >
              <ArrowLeftRight className="h-4 w-4" />
              Terminalı dəyiş
            </Button>
            <Button asChild variant="outline" className="h-12 justify-start gap-3 text-base">
              <a href="/login">
                <LogIn className="h-4 w-4" />
                Adi giriş
              </a>
            </Button>
          </div>
        </DialogContent>
      </Dialog>

      {/* Çıxış */}
      <Dialog open={exitOpen} onOpenChange={setExitOpen}>
        <DialogContent className="sm:max-w-sm">
          <DialogHeader>
            <DialogTitle>Proqramdan çıxış</DialogTitle>
            <DialogDescription>Proqram bağlansın?</DialogDescription>
          </DialogHeader>
          <DialogFooter>
            <Button type="button" variant="outline" onClick={() => setExitOpen(false)}>
              Ləğv et
            </Button>
            <Button type="button" variant="destructive" onClick={() => void exitProgram()}>
              <Power className="h-4 w-4" />
              Çıxış
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

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
    </div>
  );
}

/** Keypad key. Mouse-down doesn't take focus so the hidden RFID input keeps listening. */
function KeyButton({ className, ...props }: React.ComponentProps<"button">) {
  return (
    <button
      type="button"
      onMouseDown={(e) => e.preventDefault()}
      className={cn(
        "flex h-[clamp(3.25rem,8vh,4.5rem)] select-none items-center justify-center rounded-2xl border bg-background text-2xl font-semibold text-foreground shadow-sm transition-all",
        "hover:bg-muted active:scale-[0.96] active:shadow-none",
        "focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring",
        "disabled:pointer-events-none disabled:opacity-50",
        className,
      )}
      {...props}
    />
  );
}

function LicenseTile({ daysLeft }: { daysLeft: number | null }) {
  const expired = daysLeft != null && daysLeft <= 0;
  const soon = daysLeft != null && !expired && daysLeft <= 7;
  return (
    <div
      className={cn(
        "inline-flex items-center gap-3 rounded-xl border px-4 py-3",
        daysLeft == null && "bg-background",
        expired && "border-red-200 bg-red-50 text-red-800 dark:border-red-500/30 dark:bg-red-500/10 dark:text-red-300",
        soon && "border-amber-200 bg-amber-50 text-amber-800 dark:border-amber-500/30 dark:bg-amber-500/10 dark:text-amber-300",
        daysLeft != null && !expired && !soon &&
        "border-emerald-200 bg-emerald-50 text-emerald-800 dark:border-emerald-500/30 dark:bg-emerald-500/10 dark:text-emerald-300",
      )}
      title={daysLeft == null ? "Lisenziya məlumatı əlçatan deyil" : undefined}
    >
      <span className="flex h-9 w-9 items-center justify-center rounded-lg bg-current/10">
        {expired || soon ? <ShieldAlert className="h-5 w-5" /> : <ShieldCheck className="h-5 w-5" />}
      </span>
      <span className="text-sm leading-tight">
        <span className="block font-semibold">Lisenziya</span>
        <span className={cn(daysLeft == null && "text-muted-foreground")}>
          {expired ? "Bitib" : `Son ${daysLeft ?? "—"} gün`}
        </span>
      </span>
    </div>
  );
}

function InfoSegment({
  icon,
  label,
  value,
  valueClassName,
}: {
  icon: React.ReactNode;
  label: string;
  value: string;
  valueClassName?: string;
}) {
  return (
    <div className="flex min-w-0 items-center gap-2.5 border-r border-white/10 px-4 py-2 last:border-r-0">
      <span className="text-white/60">{icon}</span>
      <div className="min-w-0 leading-tight">
        <p className="text-[10px] font-medium uppercase tracking-wider text-white/50">{label}</p>
        <p className={cn("max-w-[14rem] truncate text-sm font-semibold", valueClassName)} title={value}>
          {value}
        </p>
      </div>
    </div>
  );
}
