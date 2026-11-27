"use client";

import { createContext, useCallback, useContext, useEffect, useMemo, useState, type ReactNode } from "react";
import { getMyProfile, type MyProfile } from "@/lib/services/profile-service";

type MyProfileContextValue = {
  profile: MyProfile | null;
  loading: boolean;
  error: string | null;
  reload: () => Promise<void>;
  setAvatarUrl: (url: string | null) => void;
};

const MyProfileContext = createContext<MyProfileContextValue | null>(null);

/** The signed-in user's own profile, loaded once and shared by the sidebar, the top-right corner and the profile page. */
export function MyProfileProvider({ children }: { children: ReactNode }) {
  const [profile, setProfile] = useState<MyProfile | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const reload = useCallback(async () => {
    setLoading(true);
    try {
      setProfile(await getMyProfile());
      setError(null);
    } catch (e) {
      setError(e instanceof Error ? e.message : "Profil yüklənmədi");
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    void reload();
  }, [reload]);

  const setAvatarUrl = useCallback((url: string | null) => {
    setProfile((p) => (p ? { ...p, avatarUrl: url } : p));
  }, []);

  const value = useMemo(
    () => ({ profile, loading, error, reload, setAvatarUrl }),
    [profile, loading, error, reload, setAvatarUrl],
  );

  return <MyProfileContext.Provider value={value}>{children}</MyProfileContext.Provider>;
}

export function useMyProfile(): MyProfileContextValue {
  const ctx = useContext(MyProfileContext);
  if (!ctx) {
    throw new Error("useMyProfile must be used within MyProfileProvider");
  }
  return ctx;
}
