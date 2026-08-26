"use client";

import { useCallback, useEffect, useMemo, useState } from "react";
import { AdvancedTableFilters, type TableFilterDef } from "@/components/advanced-table-filters";
import { DataTable } from "@/components/data-table";
import { Button } from "@/components/ui/button";
import { Checkbox } from "@/components/ui/checkbox";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { ApiFormError, getFieldErrorMessage, type FieldErrors } from "@/lib/api-error";
import { useSelectedCompany } from "@/contexts/selected-company-context";
import {
  createRole,
  deleteRoleApi,
  getRoles,
  updateRole,
  type AppRole,
} from "@/lib/services/role-service";
import {
  getPermissions,
  getRolePermissionIds,
  updateRolePermissions,
  type PermissionDto,
} from "@/lib/services/role-permission-service";
import { toast } from "sonner";

type RoleRow = {
  id: string;
  roleId: number;
  name: string;
};

function friendlyError(err: unknown, fallback: string): string {
  if (err instanceof ApiFormError && err.message) return err.message;
  if (err instanceof Error && err.message && !/request failed|status code/i.test(err.message)) return err.message;
  return fallback;
}

export default function RolesPage() {
  const { companiesLoading } = useSelectedCompany();
  const [list, setList] = useState<AppRole[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [dialogOpen, setDialogOpen] = useState(false);
  const [saving, setSaving] = useState(false);
  const [editingId, setEditingId] = useState<number | null>(null);
  const [name, setName] = useState("");
  const [fieldErrors, setFieldErrors] = useState<FieldErrors>({});

  const [permissions, setPermissions] = useState<PermissionDto[]>([]);
  const [selectedPermissionIds, setSelectedPermissionIds] = useState<Set<number>>(new Set());
  const [permissionsLoading, setPermissionsLoading] = useState(false);

  useEffect(() => {
    getPermissions()
      .then(setPermissions)
      .catch(() => setPermissions([]));
  }, []);

  const groupedPermissions = useMemo(() => {
    const map = new Map<string, PermissionDto[]>();
    for (const p of permissions) {
      const key = p.module || "Ümumi";
      if (!map.has(key)) map.set(key, []);
      map.get(key)!.push(p);
    }
    return Array.from(map.entries()).sort((a, b) => a[0].localeCompare(b[0]));
  }, [permissions]);

  const togglePermission = (id: number) => {
    setSelectedPermissionIds((prev) => {
      const next = new Set(prev);
      if (next.has(id)) next.delete(id);
      else next.add(id);
      return next;
    });
  };

  const loadRoles = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      setList(await getRoles());
    } catch (e) {
      setError(friendlyError(e, "Failed to load roles."));
      setList([]);
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    if (companiesLoading) return;
    void loadRoles();
  }, [companiesLoading, loadRoles]);

  const resetForm = () => {
    setEditingId(null);
    setName("");
    setFieldErrors({});
    setSelectedPermissionIds(new Set());
  };

  const rows: RoleRow[] = useMemo(
    () =>
      list.map((r) => ({
        id: String(r.id),
        roleId: r.id,
        name: r.name,
      })),
    [list],
  );

  const filterDefs = useMemo<TableFilterDef<RoleRow>[]>(
    () => [
      {
        id: "roleId",
        label: "ID",
        ui: "number",
        match: (row, get) => {
          const q = get("roleId").trim();
          if (!q) return true;
          return String(row.roleId).includes(q);
        },
      },
      {
        id: "name",
        label: "Role name",
        ui: "text",
        match: (row, get) => {
          const q = get("name").trim().toLowerCase();
          if (!q) return true;
          return row.name.toLowerCase().includes(q);
        },
      },
      {
        id: "status",
        label: "Status",
        ui: "status",
        match: () => true,
      },
    ],
    [],
  );

  const columns = [
    { key: "roleId" as const, label: "ID" },
    { key: "name" as const, label: "Role name" },
  ];

  const handleAdd = () => {
    resetForm();
    setDialogOpen(true);
  };

  const handleEdit = async (row: RoleRow) => {
    const r = list.find((x) => x.id === row.roleId);
    if (!r) return;
    setEditingId(r.id);
    setName(r.name);
    setFieldErrors({});
    setDialogOpen(true);
    setPermissionsLoading(true);
    try {
      const ids = await getRolePermissionIds(r.id);
      setSelectedPermissionIds(new Set(ids));
    } catch {
      setSelectedPermissionIds(new Set());
    } finally {
      setPermissionsLoading(false);
    }
  };

  const handleDelete = async (row: RoleRow) => {
    if (!window.confirm(`Delete role "${row.name}"?`)) return;
    try {
      await deleteRoleApi(row.roleId);
      toast.success("Role deleted.");
      await loadRoles();
    } catch (e) {
      toast.error(friendlyError(e, "Could not delete role."));
    }
  };

  const handleSave = async () => {
    const trimmed = name.trim();
    if (!trimmed) {
      toast.error("Role name is required.");
      return;
    }
    setSaving(true);
    setFieldErrors({});
    try {
      let roleId = editingId;
      if (roleId == null) {
        roleId = await createRole(trimmed);
        toast.success("Rol yaradıldı.");
      } else {
        await updateRole(roleId, trimmed);
        toast.success("Rol yeniləndi.");
      }
      await updateRolePermissions(roleId, Array.from(selectedPermissionIds));
      setDialogOpen(false);
      resetForm();
      await loadRoles();
    } catch (e) {
      if (e instanceof ApiFormError) setFieldErrors(e.fieldErrors);
      toast.error(friendlyError(e, "Yadda saxlanılmadı."));
    } finally {
      setSaving(false);
    }
  };

  if (companiesLoading || loading) {
    return <div className="p-6 text-sm text-muted-foreground">Loading roles…</div>;
  }

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-3xl font-bold text-foreground">Rollar</h1>
        <p className="text-muted-foreground mt-1">
          Rol yaradın və həmin rolun icazələrini elə burada seçin — sonradan istənilən vaxt dəyişə bilərsiniz.
        </p>
      </div>

      {error && (
        <div className="rounded-md border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-600">{error}</div>
      )}

      <Dialog
        open={dialogOpen}
        onOpenChange={(o) => {
          setDialogOpen(o);
          if (!o) resetForm();
        }}
      >
        <AdvancedTableFilters defs={filterDefs} data={rows}>
          {(filtered) => (
            <DataTable
              title="Role list"
              columns={columns}
              data={filtered}
              idSortKey="roleId"
              searchPlaceholder="Search roles…"
              searchableFields={["name", "id"]}
              onAdd={handleAdd}
              onEdit={handleEdit}
              onDelete={handleDelete}
            />
          )}
        </AdvancedTableFilters>

        <DialogContent className="sm:max-w-2xl max-h-[90vh] overflow-y-auto">
          <DialogHeader>
            <DialogTitle>{editingId != null ? "Rolu redaktə et" : "Yeni rol"}</DialogTitle>
            <DialogDescription>
              Rol adını daxil edin və hansı funksiyalara icazə veriləcəyini aşağıdan seçin.
            </DialogDescription>
          </DialogHeader>
          <div>
            <Label htmlFor="role-name">Rol adı</Label>
            <Input
              id="role-name"
              className="mt-1"
              value={name}
              onChange={(e) => setName(e.target.value)}
              placeholder="məs. Kassir"
            />
            {getFieldErrorMessage(fieldErrors, "name") && (
              <p className="mt-1 text-xs text-destructive">{getFieldErrorMessage(fieldErrors, "name")}</p>
            )}
          </div>

          <div className="mt-4">
            <Label>Səlahiyyətlər</Label>
            {permissionsLoading ? (
              <p className="mt-2 text-sm text-muted-foreground">Yüklənir…</p>
            ) : (
              <div className="mt-2 space-y-3">
                {groupedPermissions.map(([moduleName, modulePermissions]) => (
                  <div key={moduleName} className="rounded-md border p-3">
                    <p className="mb-2 text-xs font-semibold uppercase text-muted-foreground">{moduleName}</p>
                    <div className="grid gap-1.5 sm:grid-cols-2">
                      {modulePermissions.map((p) => (
                        <label key={p.id} className="flex items-center gap-2 text-sm font-normal">
                          <Checkbox
                            checked={selectedPermissionIds.has(p.id)}
                            onCheckedChange={() => togglePermission(p.id)}
                          />
                          {p.displayName || `${p.module}.${p.action}`}
                        </label>
                      ))}
                    </div>
                  </div>
                ))}
                {groupedPermissions.length === 0 && (
                  <p className="text-sm text-muted-foreground">Sistemdə icazə tapılmadı.</p>
                )}
              </div>
            )}
          </div>

          <div className="mt-4 flex justify-end gap-2">
            <Button variant="outline" onClick={() => setDialogOpen(false)} disabled={saving}>
              Ləğv et
            </Button>
            <Button onClick={() => void handleSave()} disabled={saving || permissionsLoading}>
              {saving ? "Saxlanılır…" : "Saxla"}
            </Button>
          </div>
        </DialogContent>
      </Dialog>
    </div>
  );
}
