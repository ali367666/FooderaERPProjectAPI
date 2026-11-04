"use client";

import { useEffect, useState } from "react";
import { toast } from "sonner";
import { Button } from "@/components/ui/button";
import { Label } from "@/components/ui/label";
import { getRestaurants, type Restaurant } from "@/lib/services/restaurant-service";
import { getUsers, type AppUser } from "@/lib/services/user-admin-service";
import { getStoreUserAccess, saveStoreUserAccess } from "@/lib/store/store-service";

/** Limits a user to some branches. A user with no branch ticked may work in every branch. */
export default function UserAccessCard() {
  const [users, setUsers] = useState<AppUser[]>([]);
  const [branches, setBranches] = useState<Restaurant[]>([]);
  const [userId, setUserId] = useState<number | "">("");
  const [selected, setSelected] = useState<number[]>([]);
  const [saving, setSaving] = useState(false);

  useEffect(() => {
    getUsers().then(setUsers).catch(() => setUsers([]));
    getRestaurants().then(setBranches).catch(() => setBranches([]));
  }, []);

  useEffect(() => {
    if (userId === "") {
      setSelected([]);
      return;
    }
    getStoreUserAccess(userId)
      .then((a) => setSelected(a.branchIds))
      .catch(() => setSelected([]));
  }, [userId]);

  const toggle = (id: number) => setSelected((s) => (s.includes(id) ? s.filter((x) => x !== id) : [...s, id]));

  const save = async () => {
    if (userId === "") return;
    setSaving(true);
    try {
      await saveStoreUserAccess(userId, selected);
      toast.success(selected.length === 0 ? "İstifadəçi bütün filiallarda işləyə bilər." : "Filial girişi yadda saxlanıldı.");
    } catch (e) {
      toast.error(e instanceof Error ? e.message : "Yazılmadı.");
    } finally {
      setSaving(false);
    }
  };

  return (
    <div className="space-y-3 rounded-lg border bg-card p-3">
      <h2 className="text-sm font-semibold">İstifadəçinin filial girişi</h2>
      <div className="flex flex-wrap items-end gap-3">
        <div className="space-y-1">
          <Label htmlFor="ua-user">İstifadəçi</Label>
          <select
            id="ua-user"
            className="h-9 min-w-52 rounded-md border border-input bg-background px-3 text-sm"
            value={userId}
            onChange={(e) => setUserId(e.target.value ? Number(e.target.value) : "")}
          >
            <option value="">Seçin…</option>
            {users.map((u) => (
              <option key={u.id} value={u.id}>
                {u.userName ?? u.fullName}
              </option>
            ))}
          </select>
        </div>
        {userId !== "" && (
          <>
            <div className="flex flex-wrap gap-3 pb-2">
              {branches.map((b) => (
                <label key={b.id} className="flex items-center gap-1.5 text-sm">
                  <input type="checkbox" checked={selected.includes(b.id)} onChange={() => toggle(b.id)} />
                  {b.name}
                </label>
              ))}
            </div>
            <Button disabled={saving} onClick={() => void save()}>
              Yaz
            </Button>
          </>
        )}
      </div>
      <p className="text-xs text-muted-foreground">Heç bir filial seçilməyibsə istifadəçi bütün filiallarda işləyə bilər. Seçilibsə yalnız həmin filialların kassa, satış, stok və hesabatları görünür.</p>
    </div>
  );
}
