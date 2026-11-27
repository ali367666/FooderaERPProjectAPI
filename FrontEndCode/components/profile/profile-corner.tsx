"use client";

import Link from "next/link";
import { ProfileAvatar } from "@/components/profile/profile-avatar";
import { useMyProfile } from "@/contexts/my-profile-context";

/** Top-right corner of the dashboard: photo and name, opening the profile page. */
export function ProfileCorner() {
  const { profile } = useMyProfile();
  const name = profile?.fullName?.trim() || "Profil";

  return (
    <Link
      href="/dashboard/profile"
      className="flex items-center gap-2 rounded-full border border-border bg-card py-1 pl-1 pr-4 hover:bg-muted transition-colors"
      title="Profil"
    >
      <ProfileAvatar name={name} url={profile?.avatarUrl ?? null} className="size-9" />
      <span className="max-w-48 truncate text-sm font-medium text-foreground">{name}</span>
    </Link>
  );
}
