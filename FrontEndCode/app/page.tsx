"use client";

import React, { useState } from "react";
import { useRouter } from "next/navigation";

import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Card } from "@/components/ui/card";
import { Alert, AlertDescription } from "@/components/ui/alert";
import { ChefHat, Eye, EyeOff } from "lucide-react";

const LOGIN_URL = "https://localhost:7145/api/auth/login";

export default function LoginPage() {
  const router = useRouter();

  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [showPassword, setShowPassword] = useState(false);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const handleLogin = async (e: React.FormEvent<HTMLFormElement>) => {
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
          emailOrUserName: email,
          password,
        }),
      });

      const data = await res.json();

      if (!res.ok) {
        setError(data?.message || "Login failed");
        return;
      }

      const token = data.token || data?.data?.token;

      if (!token) {
        setError("Token tapılmadı");
        return;
      }

      localStorage.setItem("token", token);

      router.push("/dashboard");
    } catch (err) {
      console.error(err);
      setError("Backend-ə qoşulmaq olmadı (CORS və ya HTTPS problemi)");
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
            placeholder="Email"
            value={email}
            onChange={(e) => setEmail(e.target.value)}
          />

          <div className="relative">
            <Input
              type={showPassword ? "text" : "password"}
              placeholder="Password"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
            />

            <button
              type="button"
              onClick={() => setShowPassword(!showPassword)}
              className="absolute right-2 top-2"
            >
              {showPassword ? <EyeOff size={18} /> : <Eye size={18} />}
            </button>
          </div>

          <Button type="submit" disabled={isLoading}>
            {isLoading ? "Loading..." : "Login"}
          </Button>

        </form>

      </Card>
    </div>
  );
}