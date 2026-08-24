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
import { ChefHat, CreditCard, Delete, Store } from "lucide-react";
import { cn } from "@/lib/utils";
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
  type RestaurantLookupItem,
} from "@/lib/services/pos-auth-service";
import {
  getCompanySettingsBranding,
  type CompanySettingsBranding,
} from "@/lib/services/company-settings-service";

const MAX_CODE_LENGTH = 8;

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
  } | null>(null);

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
        onDone({
          companyId: result.companyId,
          companyCode: trimmed,
          companyName: result.companyName,
          restaurantId: null,
          restaurantName: null,
        });
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : "Company was not found");
    } finally {
      setIsLoading(false);
    }
  };

  const selectRestaurant = (restaurant: RestaurantLookupItem | null) => {
    if (!lookup) return;
    onDone({
      companyId: lookup.companyId,
      companyCode: companyCode.trim(),
      companyName: lookup.companyName,
      restaurantId: restaurant?.id ?? null,
      restaurantName: restaurant?.name ?? null,
    });
  };

  return (
    <Card className="w-full max-w-md">
      <CardHeader className="text-center">
        <div className="mx-auto mb-2 flex h-12 w-12 items-center justify-center rounded-2xl bg-primary/10 text-primary">
          <Store className="h-6 w-6" />
        </div>
        <CardTitle>Terminal quraşdırılması</CardTitle>
        <CardDescription>
          {lookup
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

  useEffect(() => {
    const focusRfid = () => rfidInputRef.current?.focus();
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
    if (value.length !== 4 && value.length !== 8) return;
    setError(null);
    setIsLoading(true);
    try {
      const result = await posLogin({
        companyId: terminal.companyId,
        restaurantId: terminal.restaurantId,
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
            disabled={isLoading || (code.length !== 4 && code.length !== 8)}
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
            onClick={onChangeTerminal}
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
      </CardContent>
    </Card>
  );
}
