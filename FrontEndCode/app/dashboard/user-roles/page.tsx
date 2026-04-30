"use client";

import { useCallback, useEffect, useMemo, useState } from "react";
import { AdvancedTableFilters, type TableFilterDef } from "@/components/advanced-table-filters";
import { DataTable } from "@/components/data-table";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { ApiFormError } from "@/lib/api-error";
import { getUsers, type AppUser } from "@/lib/services/user-admin-service";
import { getRoles, type AppRole } from "@/lib/services/role-service";
import {
  assignUserRole,
  getUserRoleMappings,
  removeUserRole,
  type UserRoleRow,
} from "@/lib/services/user-role-service";
import { useSelectedCompany } from "@/contexts/selected-company-context";
import { toast } from "sonner";

type Row = UserRoleRow & { id: string };

function friendlyError(err: unknown, fallback: string): string {
  if (err instanceof ApiFormError && err.message) return err.message;
  if (err instanceof Error && err.message && !/request failed|status code/i.test(err.message)) return err.message;
  return fallback;
}

function userLabel(u: AppUser): string {
  const primary = u.fullName?.trim() || u.userName || u.email || `User #${u.id}`;
  const extra = u.email && u.email !== primary ? ` · ${u.email}` : "";
  return `${primary}${extra}`;
}

function roleLabel(r: AppRole): string {
  return r.name || `Role #${r.id}`;
}

export default function UserRolesPage() {
  const { companiesLoading, selectedCompanyId } = useSelectedCompany();
  const [mappings, setMappings] = useState<UserRoleRow[]>([]);
  const [users, setUsers] = useState<AppUser[]>([]);
  const [roles, setRoles] = useState<AppRole[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [selUserId, setSelUserId] = useState("");
  const [selRoleId, setSelRoleId] = useState("");
  const [assigning, setAssigning] = useState(false);

  const loadAll = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const [m, u, r] = await Promise.all([
        getUserRoleMappings(),
        getUsers(selectedCompanyId != null && selectedCompanyId > 0 ? selectedCompanyId : undefined),
        getRoles(),
      ]);
      setMappings(m);
      setUsers(u);
      setRoles(r);
    } catch (e) {
      setError(friendlyError(e, "Failed to load data."));
    } finally {
      setLoading(false);
    }
  }, [selectedCompanyId]);

  useEffect(() => {
    if (companiesLoading) return;
    void loadAll();
  }, [companiesLoading, loadAll]);

  const rows: Row[] = useMemo(
    () =>
      mappings.map((m) => ({
        ...m,
        id: `${m.userId}-${m.roleId}`,
      })),
    [mappings],
  );

  const filterDefs = useMemo<TableFilterDef<Row>[]>(
    () => [
      {
        id: "user",
        label: "User",
        ui: "text",
        match: (row, get) => {
          const q = get("user").trim().toLowerCase();
          if (!q) return true;
          return (
            row.userFullName.toLowerCase().includes(q) ||
            (row.userName ?? "").toLowerCase().includes(q) ||
            (row.email ?? "").toLowerCase().includes(q)
          );
        },
      },
      {
        id: "role",
        label: "Role",
        ui: "text",
        match: (row, get) => {
          const q = get("role").trim().toLowerCase();
          if (!q) return true;
          return row.roleName.toLowerCase().includes(q);
        },
      },
    ],
    [],
  );

  const columns = [
    {
      key: "userFullName" as const,
      label: "User",
      render: (_: string, row: Row) => (
        <div>
          <div className="font-medium">{row.userFullName}</div>
          <div className="text-xs text-muted-foreground">{row.email || row.userName || ""}</div>
        </div>
      ),
    },
    { key: "roleName" as const, label: "Role" },
    {
      key: "id" as const,
      label: "",
      render: (_: string, row: Row) => (
        <Button
          type="button"
          variant="outline"
          size="sm"
          onClick={() => void handleRemove(row)}
        >
          Remove
        </Button>
      ),
    },
  ];

  const handleAssign = async () => {
    const uid = Number(selUserId);
    const rid = Number(selRoleId);
    if (!Number.isFinite(uid) || uid <= 0) {
      toast.error("Select a user.");
      return;
    }
    if (!Number.isFinite(rid) || rid <= 0) {
      toast.error("Select a role.");
      return;
    }
    setAssigning(true);
    try {
      await assignUserRole(uid, rid);
      toast.success("Role assigned.");
      setSelUserId("");
      setSelRoleId("");
      await loadAll();
    } catch (e) {
      toast.error(friendlyError(e, "Could not assign role."));
    } finally {
      setAssigning(false);
    }
  };

  const handleRemove = async (row: Row) => {
    if (!window.confirm(`Remove role "${row.roleName}" from ${row.userFullName}?`)) return;
    try {
      await removeUserRole(row.userId, row.roleId);
      toast.success("Role removed.");
      await loadAll();
    } catch (e) {
      toast.error(friendlyError(e, "Could not remove role."));
    }
  };

  if (companiesLoading || loading) {
    return <div className="p-6 text-sm text-muted-foreground">Loading…</div>;
  }

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-3xl font-bold text-foreground">User roles</h1>
        <p className="text-muted-foreground mt-1">Assign or remove roles for application users.</p>
      </div>

      {error && (
        <div className="rounded-md border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-600">{error}</div>
      )}

      <Card>
        <CardHeader>
          <CardTitle className="text-base">Assign role</CardTitle>
        </CardHeader>
        <CardContent className="flex flex-col gap-3 sm:flex-row sm:items-end">
          <div className="flex-1 space-y-1">
            <label className="text-sm font-medium">User</label>
            <select
              className="flex h-10 w-full rounded-md border border-input bg-background px-3 py-2 text-sm"
              value={selUserId}
              onChange={(e) => setSelUserId(e.target.value)}
            >
              <option value="">Select user</option>
              {users
                .slice()
                .sort((a, b) => userLabel(a).localeCompare(userLabel(b)))
                .map((u) => (
                  <option key={u.id} value={String(u.id)}>
                    {userLabel(u)}
                  </option>
                ))}
            </select>
          </div>
          <div className="flex-1 space-y-1">
            <label className="text-sm font-medium">Role</label>
            <select
              className="flex h-10 w-full rounded-md border border-input bg-background px-3 py-2 text-sm"
              value={selRoleId}
              onChange={(e) => setSelRoleId(e.target.value)}
            >
              <option value="">Select role</option>
              {roles
                .slice()
                .sort((a, b) => a.name.localeCompare(b.name))
                .map((r) => (
                  <option key={r.id} value={String(r.id)}>
                    {roleLabel(r)}
                  </option>
                ))}
            </select>
          </div>
          <Button type="button" onClick={() => void handleAssign()} disabled={assigning}>
            {assigning ? "Assigning…" : "Assign"}
          </Button>
        </CardContent>
      </Card>

      <AdvancedTableFilters defs={filterDefs} data={rows}>
        {(filtered) => (
          <DataTable
            title="Current assignments"
            columns={columns}
            data={filtered}
            idSortKey="userId"
            searchPlaceholder="Search assignments…"
            searchableFields={["userFullName", "userName", "email", "roleName", "id"]}
          />
        )}
      </AdvancedTableFilters>
    </div>
  );
}
