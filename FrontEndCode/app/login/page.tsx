"use client";

import React, { useEffect, useState } from "react";
import { useRouter } from "next/navigation";

import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Card } from "@/components/ui/card";
import { Alert, AlertDescription } from "@/components/ui/alert";
import { ChefHat, Eye, EyeOff } from "lucide-react";
import { persistAuth, subscribeToToken } from "@/lib/auth-client";

const LOGIN_URL =
  process.env.NEXT_PUBLIC_AUTH_LOGIN_URL ??
  "https://localhost:7145/api/Auth/login";

type JsonObject = Record<string, unknown>;

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
  const msg = json.message;
  if (typeof msg === "string" && msg.trim()) return msg;
  const errors = json.errors;
  if (Array.isArray(errors) && errors.length > 0) {
    const first = errors[0];
    if (typeof first === "string") return first;
  }
  return fallback;
}

function extractTokens(json: unknown): { token: string; refreshToken?: string } | null {
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

  return { token, refreshToken };
}

export default function LoginPage() {
  const router = useRouter();

  const [emailOrUserName, setEmailOrUserName] = useState("");
  const [password, setPassword] = useState("");
  const [showPassword, setShowPassword] = useState(false);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (loading) return;
    if (token) {
      router.replace("/dashboard");
    }
  }, [token, loading, router]);

  const handleLogin = async (e: React.SyntheticEvent<HTMLFormElement>) => {
    e.preventDefault();

    setError(null);
    setIsLoading(true);

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

      persistAuth(auth.token, auth.refreshToken);
      router.push("/dashboard");
    } catch {
      setError(
        "Could not reach the server. Check that the API is running and CORS allows this origin.",
      );
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <div className="min-h-screen flex items-center justify-center bg-gray-100">
      <Card className="w-full max-w-md p-6 space-y-4">
        <div className="text-center">
          <ChefHat className="mx-auto mb-2" />
          <h1 className="text-xl font-bold">Foodera ERP</h1>
        </div>

        <form onSubmit={handleLogin} className="space-y-4">
          {error && (
            <Alert variant="destructive">
              <AlertDescription>{error}</AlertDescription>
            </Alert>
          )}

          <Input
            placeholder="Email or username"
            autoComplete="username"
            value={emailOrUserName}
            onChange={(e) => setEmailOrUserName(e.target.value)}
            required
          />

          <div className="relative">
            <Input
              type={showPassword ? "text" : "password"}
              placeholder="Password"
              autoComplete="current-password"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              required
            />

            <button
              type="button"
              onClick={() => setShowPassword(!showPassword)}
              className="absolute right-2 top-2"
              aria-label={showPassword ? "Hide password" : "Show password"}
            >
              {showPassword ? <EyeOff size={18} /> : <Eye size={18} />}
            </button>
          </div>

          <Button type="submit" disabled={isLoading} className="w-full">
            {isLoading ? "Signing in…" : "Login"}
          </Button>
        </form>
      </Card>
    </div>
  );
}
