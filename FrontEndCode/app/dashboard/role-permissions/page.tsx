"use client";

import { useCallback, useEffect, useMemo, useState } from "react";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { toast } from "sonner";
import { getRoles, getRolesForAllCompanies, type AppRole } from "@/lib/services/role-service";
import { useSelectedCompany } from "@/contexts/selected-company-context";
import {
  getPermissions,
  getRolePermissionIds,
  updateRolePermissions,
  type PermissionDto,
} from "@/lib/services/role-permission-service";
import { toApiFormError } from "@/lib/api-error";
import { translatePermissionLabel, translateModuleLabel, isPermissionForMode } from "@/lib/permission-translations";
import { useIsSuperAdmin } from "@/hooks/use-auth-permissions";
import { useCompanyStoreMode } from "@/hooks/use-company-store-mode";
import { getCompanySettingsBranding } from "@/lib/services/company-settings-service";
import { getStoredToken } from "@/lib/auth-client";
import { getCompanyIdFromToken } from "@/lib/jwt-permissions";

export default function RolePermissionsPage() {
  const { companies, selectedCompanyId } = useSelectedCompany();
  const [roles, setRoles] = useState<AppRole[]>([]);
  const [permissions, setPermissions] = useState<PermissionDto[]>([]);
  const [selectedRoleId, setSelectedRoleId] = useState<number | null>(null);
  const [selectedPermissionIds, setSelectedPermissionIds] = useState<Set<number>>(new Set());
  const [loading, setLoading] = useState(true);
  const [rolePermissionsLoading, setRolePermissionsLoading] = useState(false);
  const [saving, setSaving] = useState(false);
  const storeMode = useCompanyStoreMode() === true;
  const isSuperAdmin = useIsSuperAdmin();
  const showAllModes = isSuperAdmin && selectedCompanyId == null;


  const loadData = useCallback(async () => {
    try {
      setLoading(true);
      // "All Companies" (no scope selected) means literally that for a cross-company viewer — load
      // every company's roles, not just the caller's own.
      const ids = companies.map((c) => c.id);
      const rolesPromise =
        selectedCompanyId != null
          ? getRoles(selectedCompanyId)
          : ids.length > 0
            ? getRolesForAllCompanies(ids)
            : getRoles();
      const [roleRows, permissionRows] = await Promise.all([rolesPromise, getPermissions()]);
      setRoles(roleRows);
      setPermissions(permissionRows);
      setSelectedRoleId(roleRows.some((r) => r.id === selectedRoleId) ? selectedRoleId : (roleRows[0]?.id ?? null));
    } catch (e) {
      toast.error(toApiFormError(e, "Səhifə yüklənə bilmədi").message);
    } finally {
      setLoading(false);
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [selectedCompanyId, companies]);

  const loadRolePermissions = useCallback(async () => {
    if (!selectedRoleId) {
      setSelectedPermissionIds(new Set());
      return;
    }
    try {
      setRolePermissionsLoading(true);
      const ids = await getRolePermissionIds(selectedRoleId);
      setSelectedPermissionIds(new Set(ids));
    } catch (e) {
      toast.error(toApiFormError(e, "Rolun icazələri yüklənə bilmədi").message);
    } finally {
      setRolePermissionsLoading(false);
    }
  }, [selectedRoleId]);

  useEffect(() => {
    void loadData();
  }, [loadData]);

  useEffect(() => {
    void loadRolePermissions();
  }, [loadRolePermissions]);

  const grouped = useMemo(() => {
    const map = new Map<string, PermissionDto[]>();
    for (const permission of permissions) {
      // With no company picked, the platform SuperAdmin sees every permission; otherwise only the ones of the company's mode.
      if (!(showAllModes || isPermissionForMode(permission.module, storeMode, permission.name))) continue;
      const key = permission.module || "General";
      if (!map.has(key)) map.set(key, []);
      map.get(key)!.push(permission);
    }
    return Array.from(map.entries()).sort((a, b) =>
      translateModuleLabel(a[0], storeMode).localeCompare(translateModuleLabel(b[0], storeMode), "az"),
    );
  }, [permissions, storeMode, showAllModes]);

  const togglePermission = (permissionId: number) => {
    setSelectedPermissionIds((prev) => {
      const next = new Set(prev);
      if (next.has(permissionId)) next.delete(permissionId);
      else next.add(permissionId);
      return next;
    });
  };

  // "Select all" works on what the screen lists for the company's mode (a hidden permission of the other mode is never touched).
  const setMany = (ids: number[], on: boolean) =>
    setSelectedPermissionIds((prev) => {
      const next = new Set(prev);
      for (const id of ids) {
        if (on) next.add(id);
        else next.delete(id);
      }
      return next;
    });
  const allVisibleIds = useMemo(() => grouped.flatMap(([, list]) => list.map((p) => p.id)), [grouped]);

  const save = async () => {
    if (!selectedRoleId) {
      toast.error("Zəhmət olmasa rol seçin.");
      return;
    }
    try {
      setSaving(true);
      await updateRolePermissions(selectedRoleId, Array.from(selectedPermissionIds));
      toast.success("Rolun icazələri yeniləndi.");
      await loadRolePermissions();
    } catch (e) {
      toast.error(toApiFormError(e, "İcazələr yenilənə bilmədi").message);
    } finally {
      setSaving(false);
    }
  };

  if (loading) {
    return <div className="p-6 text-sm text-muted-foreground">Yüklənir...</div>;
  }

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-3xl font-bold">Rol İcazələri</h1>
        <p className="text-muted-foreground mt-1">Hər rolun hansı əməliyyatları edə biləcəyini idarə edin</p>
      </div>

      <Card>
        <CardHeader>
          <CardTitle>Rol seçin</CardTitle>
        </CardHeader>
        <CardContent className="flex flex-wrap items-center gap-3">
          <select
            className="h-10 rounded-md border px-3 text-sm"
            value={selectedRoleId ?? ""}
            onChange={(e) => setSelectedRoleId(Number(e.target.value) || null)}
          >
            <option value="">Rol seçin</option>
            {roles.map((role) => (
              <option key={role.id} value={role.id}>
                {role.name}
              </option>
            ))}
          </select>
          <Button onClick={() => void save()} disabled={saving || rolePermissionsLoading || !selectedRoleId}>
            {saving ? "Saxlanılır..." : "İcazələri saxla"}
          </Button>
        </CardContent>
      </Card>

      {rolePermissionsLoading ? (
        <div className="rounded-md border p-4 text-sm text-muted-foreground">Rolun icazələri yüklənir...</div>
      ) : null}

      <Card>
        <CardHeader>
          <CardTitle>İcazələr</CardTitle>
        </CardHeader>
        <CardContent className="text-sm text-muted-foreground">
          {!selectedRoleId
            ? "İcazələrə baxmaq və təyin etmək üçün yuxarıdan rol seçin."
            : permissions.length === 0
              ? "Heç bir icazə tapılmadı."
              : "Modula görə icazələri seçin/silin, sonra saxlayın."}
          {selectedRoleId && allVisibleIds.length > 0 && (
            <div className="mt-3 flex flex-wrap items-center gap-2">
              <Button type="button" size="sm" variant="outline" onClick={() => setMany(allVisibleIds, true)}>
                Hamısını seç
              </Button>
              <Button type="button" size="sm" variant="outline" onClick={() => setMany(allVisibleIds, false)}>
                Hamısını sil
              </Button>
              <span className="text-xs">
                {allVisibleIds.filter((id) => selectedPermissionIds.has(id)).length} / {allVisibleIds.length} seçilib
              </span>
            </div>
          )}
        </CardContent>
      </Card>

      {selectedRoleId && grouped.map(([moduleName, modulePermissions]) => (
        <Card key={moduleName}>
          <CardHeader className="flex flex-row items-center justify-between space-y-0">
            <CardTitle>{translateModuleLabel(moduleName, storeMode)}</CardTitle>
            {(() => {
              const ids = modulePermissions.map((p) => p.id);
              const allOn = ids.every((id) => selectedPermissionIds.has(id));
              return (
                <Button type="button" size="sm" variant="ghost" onClick={() => setMany(ids, !allOn)}>
                  {allOn ? "Bölməni təmizlə" : "Bölməni seç"}
                </Button>
              );
            })()}
          </CardHeader>
          <CardContent>
            <div className="grid gap-2 md:grid-cols-2 lg:grid-cols-3">
              {modulePermissions.map((permission) => (
                <label key={permission.id} className="flex items-center gap-2 text-sm">
                  <input
                    type="checkbox"
                    checked={selectedPermissionIds.has(permission.id)}
                    onChange={() => togglePermission(permission.id)}
                  />
                  <span>
                    {translatePermissionLabel(
                      permission.name,
                      permission.displayName || `${permission.module}.${permission.action}`,
                      storeMode,
                    )}
                  </span>
                </label>
              ))}
            </div>
          </CardContent>
        </Card>
      ))}
    </div>
  );
}
