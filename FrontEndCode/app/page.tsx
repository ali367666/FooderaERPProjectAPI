"use client";

import { useState, type FormEvent } from "react";
import { useRouter } from "next/navigation";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Card } from "@/components/ui/card";
import { Alert, AlertDescription } from "@/components/ui/alert";
import { ChefHat, Eye, EyeOff } from "lucide-react";

const LOGIN_URL =
  process.env.NEXT_PUBLIC_AUTH_LOGIN_URL ??
  "https://localhost:7145/api/auth/login";

type LoginSuccess = {
  token: string;
  refreshToken?: string;
  userId?: number;
  fullName?: string;
};

function parseLoginJson(json: unknown): LoginSuccess | { error: string } {
  if (!json || typeof json !== "object") {
    return { error: "Invalid response from server" };
  }

  const root = json as Record<string, unknown>;

  if (root.success === false) {
    const msg =
      typeof root.message === "string" && root.message
        ? root.message
        : "Sign in failed";
    return { error: msg };
  }

  const data =
    root.data && typeof root.data === "object"
      ? (root.data as Record<string, unknown>)
      : root;

  const token =
    (typeof data.token === "string" && data.token) ||
    (typeof data.accessToken === "string" && data.accessToken) ||
    undefined;

  if (!token) {
    return { error: "Invalid response from server" };
  }

  const refreshToken =
    typeof data.refreshToken === "string" ? data.refreshToken : undefined;
  const userId = typeof data.userId === "number" ? data.userId : undefined;
  const fullName =
    typeof data.fullName === "string" ? data.fullName : undefined;

  return { token, refreshToken, userId, fullName };
}

function errorMessageFromJson(json: unknown, status: number): string {
  if (!json || typeof json !== "object") {
    return `Sign in failed (${status})`;
  }

  const o = json as Record<string, unknown>;

  if (typeof o.message === "string" && o.message) return o.message;

  if (Array.isArray(o.errors) && o.errors.length > 0) {
    const first = o.errors[0];
    if (typeof first === "string") return first;
  }

  return `Sign in failed (${status})`;
}

export default function LoginPage() {
  const router = useRouter();
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [showPassword, setShowPassword] = useState(false);
  const [rememberMe, setRememberMe] = useState(false);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const handleLogin = async (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    setError(null);
    setIsLoading(true);

    try {
      const res = await fetch(LOGIN_URL, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          emailOrUserName: email,
          password,
        }),
      });

      let json: unknown = null;

      try {
        json = await res.json();
      } catch {
        json = null;
      }

      if (!res.ok) {
        setError(errorMessageFromJson(json, res.status));
        return;
      }

      const parsed = parseLoginJson(json);

      if ("error" in parsed) {
        setError(parsed.error);
        return;
      }

      localStorage.setItem("token", parsed.token);

      if (parsed.refreshToken) {
        localStorage.setItem("refreshToken", parsed.refreshToken);
      }

      if (parsed.userId != null) {
        localStorage.setItem("userId", String(parsed.userId));
      }

      if (parsed.fullName) {
        localStorage.setItem("fullName", parsed.fullName);
      }

      if (rememberMe) {
        localStorage.setItem("rememberMe", "true");
      } else {
        localStorage.removeItem("rememberMe");
      }

      router.push("/dashboard");
    } catch (err) {
      setError(
        err instanceof Error ? err.message : "Network error. Please try again."
      );
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <div className="min-h-screen bg-gradient-to-br from-primary via-background to-secondary flex items-center justify-center p-4">
      <div className="absolute inset-0 overflow-hidden pointer-events-none">
        <div className="absolute -top-40 -right-40 w-80 h-80 bg-accent/10 rounded-full blur-3xl" />
        <div className="absolute -bottom-40 -left-40 w-80 h-80 bg-secondary/10 rounded-full blur-3xl" />
      </div>

      <Card className="relative w-full max-w-md p-8 border-0 shadow-xl bg-card/95 backdrop-blur-sm">
        <div className="flex justify-center mb-8">
          <div className="w-12 h-12 rounded-lg bg-primary flex items-center justify-center">
            <ChefHat size={28} className="text-primary-foreground" />
          </div>
        </div>

        <div className="text-center mb-8">
          <h1 className="text-3xl font-bold text-foreground">Foodera ERP</h1>
          <p className="text-muted-foreground mt-2">
            Enterprise Resource Planning System
          </p>
        </div>

        <form onSubmit={handleLogin} className="space-y-6">
          {error ? (
            <Alert variant="destructive" className="text-left">
              <AlertDescription>{error}</AlertDescription>
            </Alert>
          ) : null}

          <div>
            <label htmlFor="email" className="block text-sm font-medium text-foreground mb-2">
              Email Address
            </label>
            <Input
              id="email"
              type="email"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              placeholder="you@example.com"
              required
              className="bg-background border-border focus:ring-primary"
            />
          </div>

          <div>
            <label htmlFor="password" className="block text-sm font-medium text-foreground mb-2">
              Password
            </label>
            <div className="relative">
              <Input
                id="password"
                type={showPassword ? "text" : "password"}
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                placeholder="Enter your password"
                required
                className="bg-background border-border focus:ring-primary pr-10"
              />
              <button
                type="button"
                onClick={() => setShowPassword(!showPassword)}
                className="absolute right-3 top-1/2 -translate-y-1/2 text-muted-foreground hover:text-foreground"
              >
                {showPassword ? <EyeOff size={18} /> : <Eye size={18} />}
              </button>
            </div>
          </div>

          <div className="flex items-center justify-between text-sm">
            <label className="flex items-center gap-2 cursor-pointer">
              <input
                type="checkbox"
                checked={rememberMe}
                onChange={(e) => setRememberMe(e.target.checked)}
                className="w-4 h-4 rounded border-border text-primary accent-primary"
              />
              <span className="text-muted-foreground">Remember me</span>
            </label>
            <a href="#" className="text-primary hover:text-primary/90 font-medium">
              Forgot Password?
            </a>
          </div>

          <Button
            type="submit"
            disabled={isLoading}
            className="w-full bg-primary text-primary-foreground hover:bg-primary/90 h-11 font-semibold"
          >
            {isLoading ? "Signing in..." : "Sign In"}
          </Button>
        </form>

        <div className="mt-6 p-4 bg-muted rounded-lg text-sm text-muted-foreground">
          <p className="font-semibold text-foreground mb-2">Demo Credentials:</p>
          <p>Email: <span className="font-mono text-xs">demo@foodera.com</span></p>
          <p>Password: <span className="font-mono text-xs">password123</span></p>
        </div>

        <p className="text-center text-xs text-muted-foreground mt-6">
          © 2024 Foodera ERP. All rights reserved.
        </p>
      </Card>
    </div>
  );
}