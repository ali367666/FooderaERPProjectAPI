"use client";

import { useEffect, useState } from "react";
import { toast } from "sonner";
import { Button } from "@/components/ui/button";
import { Label } from "@/components/ui/label";
import { getPermissions, getRolePermissionIds, updateRolePermissions, type PermissionDto } from "@/lib/services/role-permission-service";
import { getRoles, type AppRole } from "@/lib/services/role-service";
import { STORE_ROLE_TEMPLATES } from "@/lib/store/role-templates";

/** Gives a role the store permissions of a ready template (added to the role's current permissions, nothing is removed). */
export default function RoleTemplatesCard() {
  const [roles, setRoles] = useState<AppRole[]>([]);
  const [permissions, setPermissions] = useState<PermissionDto[]>([]);
  const [roleId, setRoleId] = useState<number | "">("");
  const [templateKey, setTemplateKey] = useState(STORE_ROLE_TEMPLATES[0].key);
  const [busy, setBusy] = useState(false);

  useEffect(() => {
    getRoles().then(setRoles).catch(() => setRoles([]));
    getPermissions().then(setPermissions).catch(() => setPermissions([]));
  }, []);

  const template = STORE_ROLE_TEMPLATES.find((t) => t.key === templateKey)!;

  const apply = async () => {
    if (roleId === "") return;
    setBusy(true);
    try {
      const byName = new Map(permissions.map((p) => [p.name, p.id]));
      const wanted = template.permissions.map((n) => byName.get(n)).filter((id): id is number => id != null);
      const missing = template.permissions.filter((n) => !byName.has(n));
      const current = await getRolePermissionIds(roleId);
      await updateRolePermissions(roleId, Array.from(new Set([...current, ...wanted])));
      toast.success(`“${template.label}” şablonu tətbiq edildi (${wanted.length} icazə).`);
      if (missing.length > 0) toast.warning(`Sistemdə tapılmayan icazələr: ${missing.join(", ")}`);
    } catch (e) {
      toast.error(e instanceof Error ? e.message : "Şablon tətbiq edilmədi.");
    } finally {
      setBusy(false);
    }
  };

  return (
    <div className="space-y-3 rounded-lg border bg-card p-3">
      <h2 className="text-sm font-semibold">Rol şablonları</h2>
      <div className="flex flex-wrap items-end gap-3">
        <div className="space-y-1">
          <Label htmlFor="rt-role">Rol</Label>
          <select id="rt-role" className="h-9 min-w-44 rounded-md border border-input bg-background px-3 text-sm" value={roleId} onChange={(e) => setRoleId(e.target.value ? Number(e.target.value) : "")}>
            <option value="">Seçin…</option>
            {roles.map((r) => (
              <option key={r.id} value={r.id}>
                {r.name}
              </option>
            ))}
          </select>
        </div>
        <div className="space-y-1">
          <Label htmlFor="rt-template">Şablon</Label>
          <select id="rt-template" className="h-9 min-w-44 rounded-md border border-input bg-background px-3 text-sm" value={templateKey} onChange={(e) => setTemplateKey(e.target.value)}>
            {STORE_ROLE_TEMPLATES.map((t) => (
              <option key={t.key} value={t.key}>
                {t.label}
              </option>
            ))}
          </select>
        </div>
        <Button disabled={busy || roleId === ""} onClick={() => void apply()}>
          Tətbiq et
        </Button>
      </div>
      <p className="text-xs text-muted-foreground">{template.description} Şablon rolun mövcud icazələrinə əlavə olunur, heç nə silinmir.</p>
    </div>
  );
}
