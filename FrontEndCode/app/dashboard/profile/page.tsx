"use client";

import { useRef, useState } from "react";
import { Camera, Loader2, Trash2 } from "lucide-react";
import { toast } from "sonner";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { ProfileAvatar } from "@/components/profile/profile-avatar";
import { useMyProfile } from "@/contexts/my-profile-context";
import { removeMyAvatar, uploadMyAvatar } from "@/lib/services/profile-service";
import { roleDisplayName } from "@/lib/role-labels";

const PHOTO_TYPES = ["image/jpeg", "image/png", "image/webp"];
const MAX_PHOTO_BYTES = 5 * 1024 * 1024;

function InfoCard({ title, rows }: { title: string; rows: Array<[string, string | null | undefined]> }) {
  return (
    <Card>
      <CardHeader>
        <CardTitle>{title}</CardTitle>
      </CardHeader>
      <CardContent>
        <dl className="divide-y divide-border">
          {rows.map(([label, value]) => (
            <div key={label} className="grid grid-cols-1 gap-1 py-3 sm:grid-cols-[12rem_1fr]">
              <dt className="text-sm text-muted-foreground">{label}</dt>
              <dd className="text-sm font-medium text-foreground break-words">{value?.trim() || "—"}</dd>
            </div>
          ))}
        </dl>
      </CardContent>
    </Card>
  );
}

export default function ProfilePage() {
  const { profile, loading, error, reload, setAvatarUrl } = useMyProfile();
  const inputRef = useRef<HTMLInputElement>(null);
  const [saving, setSaving] = useState(false);

  async function handleFile(file: File | undefined) {
    if (!file) return;
    if (!PHOTO_TYPES.includes(file.type)) {
      toast.error("Yalnız JPG, PNG və ya WEBP şəkil qəbul olunur.");
      return;
    }
    if (file.size > MAX_PHOTO_BYTES) {
      toast.error("Şəkil 5MB-dan böyük ola bilməz.");
      return;
    }
    setSaving(true);
    try {
      setAvatarUrl(await uploadMyAvatar(file));
      toast.success("Şəkil yükləndi.");
    } catch (e) {
      toast.error(e instanceof Error ? e.message : "Şəkil yüklənmədi");
    } finally {
      setSaving(false);
    }
  }

  async function handleRemove() {
    setSaving(true);
    try {
      await removeMyAvatar();
      setAvatarUrl(null);
      toast.success("Şəkil silindi.");
    } catch (e) {
      toast.error(e instanceof Error ? e.message : "Şəkil silinmədi");
    } finally {
      setSaving(false);
    }
  }

  if (!profile) {
    return (
      <div className="flex h-[40vh] flex-col items-center justify-center gap-3 text-center">
        {loading ? (
          <Loader2 className="h-8 w-8 animate-spin text-muted-foreground" />
        ) : (
          <>
            <p className="text-sm text-muted-foreground">{error ?? "Profil yüklənmədi"}</p>
            <Button variant="outline" onClick={() => void reload()}>
              Yenidən cəhd et
            </Button>
          </>
        )}
      </div>
    );
  }

  const roles = profile.roles.map(roleDisplayName).join(", ");

  return (
    <div className="space-y-6">
      <h1 className="text-2xl font-bold text-foreground">Profil</h1>

      <Card>
        <CardContent className="flex flex-col items-center gap-6 sm:flex-row">
          <ProfileAvatar name={profile.fullName} url={profile.avatarUrl} className="size-28 text-3xl" />
          <div className="min-w-0 flex-1 text-center sm:text-left">
            <h2 className="text-xl font-semibold text-foreground break-words">{profile.fullName}</h2>
            <p className="text-sm text-muted-foreground">
              {[roles, profile.companyName].filter(Boolean).join(" · ")}
            </p>
            <div className="mt-4 flex flex-wrap justify-center gap-2 sm:justify-start">
              <Button onClick={() => inputRef.current?.click()} disabled={saving}>
                {saving ? <Loader2 className="animate-spin" /> : <Camera />}
                {profile.avatarUrl ? "Şəkli dəyiş" : "Şəkil yüklə"}
              </Button>
              {profile.avatarUrl && (
                <Button variant="outline" onClick={() => void handleRemove()} disabled={saving}>
                  <Trash2 />
                  Şəkli sil
                </Button>
              )}
            </div>
            <p className="mt-2 text-xs text-muted-foreground">JPG, PNG və ya WEBP, ən çox 5MB.</p>
            <input
              ref={inputRef}
              type="file"
              accept={PHOTO_TYPES.join(",")}
              className="hidden"
              onChange={(e) => {
                void handleFile(e.target.files?.[0]);
                e.target.value = "";
              }}
            />
          </div>
        </CardContent>
      </Card>

      <div className="grid gap-6 xl:grid-cols-2">
        <InfoCard
          title="Şəxsi məlumatlar"
          rows={[
            ["Ad, soyad", profile.fullName],
            ["İstifadəçi adı", profile.userName],
            ["E-poçt", profile.email],
            ["Telefon", profile.phoneNumber],
            ["İşçi kodu", profile.code],
            ["Vəzifə", profile.positionName],
            ["Şöbə", profile.departmentName],
            ["İş yeri", profile.restaurantName ?? (profile.workplaceType === 1 ? "Baş ofis" : null)],
            ["Anbar", profile.warehouseName],
            ["İşə qəbul tarixi", profile.hireDate ? new Date(profile.hireDate).toLocaleDateString("az-AZ") : null],
            ["Rol", roles],
          ]}
        />
        <InfoCard
          title="Şirkət məlumatları"
          rows={[
            ["Şirkətin adı", profile.companyName],
            ["Şirkət kodu", profile.companyCode],
            ["VÖEN", profile.companyTaxNumber],
            ["Ünvan", profile.companyAddress],
            ["Telefon", profile.companyPhoneNumber],
            ["E-poçt", profile.companyEmail],
          ]}
        />
      </div>
    </div>
  );
}
