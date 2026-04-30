"use client";

import React, { useEffect, useRef, useState } from "react";
import { useRouter } from "next/navigation";

import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Alert, AlertDescription } from "@/components/ui/alert";
import { Checkbox } from "@/components/ui/checkbox";
import { Label } from "@/components/ui/label";
import {
  ChefHat,
  Eye,
  EyeOff,
  Leaf,
  ShoppingBasket,
  Utensils,
  UtensilsCrossed,
} from "lucide-react";
import { cn } from "@/lib/utils";
import { getPermissionClaimsFromToken, getRoleClaimsFromToken } from "@/lib/jwt-permissions";
import { persistAuthUser } from "@/lib/auth-client";

const LOGIN_URL =
  process.env.NEXT_PUBLIC_AUTH_LOGIN_URL ??
  "https://localhost:7145/api/Auth/login";

const LS_REMEMBER = "foodera_login_remember";
const LS_EMAIL = "foodera_login_email";
const LS_PASSWORD = "foodera_login_password";

const BASE_BG = "#063f2f";

type JsonObject = Record<string, unknown>;

function lerp(a: number, b: number, t: number) {
  return a + (b - a) * t;
}

/** Food-inspired cursor glow: left = orange/gold, center = cream/olive, right = red/emerald */
function glowRgbaFromNormX(nx: number): string {
  const t = Math.max(0, Math.min(1, nx));
  let r: number;
  let g: number;
  let b: number;
  const a = 0.32;

  if (t < 0.5) {
    const u = t / 0.5;
    const r1 = lerp(234, 86, u);
    const g1 = lerp(88, 200, u);
    const b1 = lerp(12, 100, u);
    const r2 = 253;
    const g2 = 246;
    const b2 = 227;
    r = lerp(r1, r2, u * u);
    g = lerp(g1, g2, u);
    b = lerp(b1, b2, u);
  } else {
    const u = (t - 0.5) / 0.5;
    r = lerp(253, 4, u);
    g = lerp(246, 120, u);
    b = lerp(227, 100, u);
  }

  return `rgba(${r | 0},${g | 0},${b | 0},${a})`;
}

function isObject(value: unknown): value is JsonObject {
  return value !== null && typeof value === "object" && !Array.isArray(value);
}

async function parseResponseBody(res: Response): Promise<unknown> {
  const text = await res.text();
  if (!text.trim()) return null;

  try {
    return JSON.parse(text) as unknown;
  } catch {
    return null;
  }
}

function errorMessageFromPayload(json: unknown, fallback: string): string {
  if (!isObject(json)) return fallback;

  const message = json.message;
  if (typeof message === "string" && message.trim()) return message;

  const errors = json.errors;
  if (Array.isArray(errors) && errors.length > 0) {
    const first = errors[0];
    if (typeof first === "string" && first.trim()) return first;
  }

  return fallback;
}

function extractTokens(
  json: unknown,
): { token: string; refreshToken?: string; permissions?: string[]; roles?: string[] } | null {
  if (!isObject(json)) return null;
  if (json.success === false) return null;

  const data = isObject(json.data) ? json.data : json;

  const token =
    (typeof data.accessToken === "string" && data.accessToken) ||
    (typeof data.token === "string" && data.token) ||
    undefined;

  if (!token) return null;

  const refreshToken =
    typeof data.refreshToken === "string" ? data.refreshToken : undefined;

  const permissions = Array.isArray(data.permissions)
    ? data.permissions.filter((x): x is string => typeof x === "string")
    : undefined;
  const roles = Array.isArray(data.roles)
    ? data.roles.filter((x): x is string => typeof x === "string")
    : undefined;

  return { token, refreshToken, permissions, roles };
}

export default function LoginPage() {
  const router = useRouter();

  const [emailOrUserName, setEmailOrUserName] = useState("");
  const [password, setPassword] = useState("");
  const [rememberMe, setRememberMe] = useState(false);
  const [showPassword, setShowPassword] = useState(false);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const [mouseX, setMouseX] = useState(0);
  const [mouseY, setMouseY] = useState(0);
  const [viewport, setViewport] = useState({ w: 0, h: 0 });

  const target = useRef({ x: 0, y: 0 });
  const smooth = useRef({ x: 0, y: 0 });
  const rafId = useRef(0);
  const glowEl = useRef<HTMLDivElement | null>(null);

  const applyGlowStyle = useRef(() => {
    const el = glowEl.current;
    if (!el) return;
    const w = window.innerWidth || 1;
    const nx = target.current.x / w;
    const c = glowRgbaFromNormX(nx);
    const { x, y } = smooth.current;
    el.style.background = `radial-gradient(circle 48vmin at ${x}px ${y}px, ${c} 0%, transparent 40%)`;
  });

  useEffect(() => {
    if (typeof window === "undefined") return;
    const sync = () => {
      const w = window.innerWidth;
      const h = window.innerHeight;
      setViewport({ w, h });
    };
    sync();
    const w = window.innerWidth;
    const h = window.innerHeight;
    const cx = w / 2;
    const cy = h / 2;
    target.current = { x: cx, y: cy };
    smooth.current = { x: cx, y: cy };
    setMouseX(cx);
    setMouseY(cy);
    window.addEventListener("resize", sync, { passive: true });
    requestAnimationFrame(() => applyGlowStyle.current());
    return () => window.removeEventListener("resize", sync);
  }, []);

  useEffect(() => {
    const onPointer = (x: number, y: number) => {
      setMouseX(x);
      setMouseY(y);
      target.current = { x, y };
    };
    const onMove = (e: MouseEvent) => onPointer(e.clientX, e.clientY);
    const onTouch = (e: TouchEvent) => {
      if (e.touches.length) {
        onPointer(e.touches[0].clientX, e.touches[0].clientY);
      }
    };
    window.addEventListener("mousemove", onMove, { passive: true });
    window.addEventListener("touchmove", onTouch, { passive: true });
    return () => {
      window.removeEventListener("mousemove", onMove);
      window.removeEventListener("touchmove", onTouch);
    };
  }, []);

  useEffect(() => {
    if (typeof window === "undefined") return;

    const tick = () => {
      rafId.current = requestAnimationFrame(tick);
      const k = 0.1;
      smooth.current.x += (target.current.x - smooth.current.x) * k;
      smooth.current.y += (target.current.y - smooth.current.y) * k;
      applyGlowStyle.current();
    };
    rafId.current = requestAnimationFrame(tick);
    return () => cancelAnimationFrame(rafId.current);
  }, []);

  useEffect(() => {
    const token = localStorage.getItem("token");
    if (token) {
      router.replace("/dashboard");
    }
  }, [router]);

  useEffect(() => {
    if (typeof window === "undefined") return;
    if (localStorage.getItem(LS_REMEMBER) === "1") {
      setRememberMe(true);
      const storedEmail = localStorage.getItem(LS_EMAIL);
      const storedPassword = localStorage.getItem(LS_PASSWORD);
      if (storedEmail) setEmailOrUserName(storedEmail);
      if (storedPassword) setPassword(storedPassword);
    }
  }, []);

  const handleLogin = async (e: React.FormEvent<HTMLFormElement>) => {
    e.preventDefault();

    setError(null);
    setIsLoading(true);

    if (!rememberMe) {
      localStorage.removeItem(LS_REMEMBER);
      localStorage.removeItem(LS_EMAIL);
      localStorage.removeItem(LS_PASSWORD);
    }

    try {
      const res = await fetch(LOGIN_URL, {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify({
          emailOrUserName: emailOrUserName.trim(),
          password,
        }),
      });

      const json = await parseResponseBody(res);

      if (!res.ok) {
        setError(
          errorMessageFromPayload(
            json,
            `Sign in failed (${res.status} ${res.statusText || ""})`.trim(),
          ),
        );
        return;
      }

      if (isObject(json) && json.success === false) {
        setError(errorMessageFromPayload(json, "Sign in failed"));
        return;
      }

      const auth = extractTokens(json);

      if (!auth) {
        setError("Invalid response from server");
        return;
      }

      if (rememberMe) {
        localStorage.setItem(LS_REMEMBER, "1");
        localStorage.setItem(LS_EMAIL, emailOrUserName.trim());
        localStorage.setItem(LS_PASSWORD, password);
      }

      localStorage.setItem("token", auth.token);
      const permissions = auth.permissions ?? getPermissionClaimsFromToken(auth.token);
      const roles = auth.roles ?? getRoleClaimsFromToken(auth.token);
      const user = { roles, permissions };
      persistAuthUser(user);
      console.log("AUTH USER:", user);
      console.log("AUTH PERMISSIONS:", user?.permissions);
      console.log("User permissions:", permissions);

      if (auth.refreshToken) {
        localStorage.setItem("refreshToken", auth.refreshToken);
      } else {
        localStorage.removeItem("refreshToken");
      }

      router.replace("/dashboard");
    } catch {
      setError(
        "Could not reach the server. Check that the API is running and CORS allows this origin.",
      );
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <div className="relative min-h-screen w-full overflow-hidden">
      <div
        className="pointer-events-none absolute inset-0 z-0"
        style={{ backgroundColor: BASE_BG }}
        aria-hidden
      />

      <div
        className="pointer-events-none absolute -left-1/4 -top-1/4 z-[1] h-[55vmin] w-[55vmin] rounded-full bg-orange-500/25 blur-3xl"
        style={{
          animation:
            "login-blob-drift 18s ease-in-out infinite, login-blob-pulse 10s ease-in-out infinite",
        }}
        aria-hidden
      />
      <div
        className="pointer-events-none absolute -bottom-1/3 -right-1/4 z-[1] h-[60vmin] w-[60vmin] rounded-full bg-emerald-500/20 blur-3xl"
        style={{
          animation: "login-blob-drift-reverse 22s ease-in-out infinite",
        }}
        aria-hidden
      />
      <div
        className="pointer-events-none absolute -right-1/5 -top-1/5 z-[1] h-[50vmin] w-[50vmin] rounded-full bg-amber-400/15 blur-3xl"
        style={{ animation: "login-blob-drift 15s ease-in-out infinite" }}
        aria-hidden
      />
      <div
        className="pointer-events-none absolute right-0 top-0 z-[1] h-[40vmin] w-[40vmin] rounded-full bg-red-500/12 blur-3xl"
        style={{ animation: "login-blob-drift-reverse 20s ease-in-out infinite" }}
        aria-hidden
      />

      <div
        className="pointer-events-none absolute inset-0 z-[2] overflow-hidden text-white/[0.05]"
        aria-hidden
      >
        <div
          className="absolute inset-0 will-change-transform"
          style={{ animation: "login-icon-drift 14s ease-in-out infinite" }}
        >
          <div
            className="absolute inset-0 will-change-transform transition-transform duration-150 ease-out"
            style={
              viewport.w > 0 && viewport.h > 0
                ? {
                    transform: `translate3d(${(mouseX - viewport.w / 2) * 0.005}px, ${(mouseY - viewport.h / 2) * 0.005}px, 0)`,
                  }
                : undefined
            }
          >
            <UtensilsCrossed
              className="absolute left-[6%] top-[12%] h-14 w-14"
              strokeWidth={0.8}
            />
            <Utensils
              className="absolute bottom-[20%] left-[8%] h-12 w-12"
              strokeWidth={0.8}
            />
            <Leaf
              className="absolute right-[10%] top-[22%] h-10 w-10"
              strokeWidth={0.8}
            />
            <ShoppingBasket
              className="absolute bottom-[12%] right-[12%] h-12 w-12"
              strokeWidth={0.8}
            />
            <ChefHat
              className="absolute left-1/2 top-[6%] h-10 w-10 -translate-x-1/2"
              strokeWidth={0.8}
            />
          </div>
        </div>
      </div>

      <div
        ref={glowEl}
        className="pointer-events-none absolute inset-0 z-[3] will-change-[background]"
        style={{
          background: `radial-gradient(circle 48vmin at 50% 50%, rgba(253, 186, 116, 0.32) 0%, transparent 40%)`,
        }}
        aria-hidden
      />

      <div
        className="pointer-events-none absolute inset-0 z-10 bg-[#0b3d2e]/25 backdrop-blur-sm"
        aria-hidden
      />

      <div className="relative z-20 flex min-h-screen items-center justify-center p-4 sm:p-6">
        <div
          className={cn(
            "w-full max-w-md space-y-5 rounded-2xl border border-white/30 p-6 shadow-2xl backdrop-blur-xl sm:p-8",
            "bg-white/90 dark:bg-slate-950/80",
            "ring-1 ring-white/20",
          )}
        >
          <div className="text-center">
            <div className="mx-auto mb-3 flex h-14 w-14 items-center justify-center rounded-2xl bg-primary/10 text-primary">
              <ChefHat className="h-8 w-8" />
            </div>
            <h1 className="text-2xl font-bold tracking-tight text-foreground">
              Foodera ERP
            </h1>
            <p className="mt-1 text-sm text-muted-foreground">
              Sign in to your account
            </p>
          </div>

          <form onSubmit={handleLogin} className="space-y-4">
            {error && (
              <Alert variant="destructive">
                <AlertDescription>{error}</AlertDescription>
              </Alert>
            )}

            <div className="space-y-2">
              <Label htmlFor="login-email" className="text-sm">
                Email or username
              </Label>
              <Input
                id="login-email"
                placeholder="Email or username"
                autoComplete="username"
                value={emailOrUserName}
                onChange={(e) => setEmailOrUserName(e.target.value)}
                className="h-10 bg-white/80 dark:bg-slate-900/50"
                required
              />
            </div>

            <div className="space-y-2">
              <Label htmlFor="login-password" className="text-sm">
                Password
              </Label>
              <div className="relative">
                <Input
                  id="login-password"
                  type={showPassword ? "text" : "password"}
                  placeholder="Password"
                  autoComplete="current-password"
                  value={password}
                  onChange={(e) => setPassword(e.target.value)}
                  className="h-10 bg-white/80 pr-10 dark:bg-slate-900/50"
                  required
                />

                <button
                  type="button"
                  onClick={() => setShowPassword((prev) => !prev)}
                  className="absolute right-2 top-1/2 -translate-y-1/2 rounded p-1 text-muted-foreground hover:text-foreground"
                  aria-label={showPassword ? "Hide password" : "Show password"}
                >
                  {showPassword ? <EyeOff size={18} /> : <Eye size={18} />}
                </button>
              </div>
            </div>

            <div className="flex items-center gap-2 pt-0.5">
              <Checkbox
                id="remember-me"
                checked={rememberMe}
                onCheckedChange={(v) => setRememberMe(v === true)}
              />
              <Label
                htmlFor="remember-me"
                className="cursor-pointer text-sm font-normal text-foreground"
              >
                Remember me
              </Label>
            </div>

            <Button
              type="submit"
              disabled={isLoading}
              className="h-10 w-full font-medium"
            >
              {isLoading ? "Signing in..." : "Login"}
            </Button>
          </form>
        </div>
      </div>

    </div>
  );
}
