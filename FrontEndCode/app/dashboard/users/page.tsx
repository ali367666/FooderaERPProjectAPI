"use client";

import { useCallback, useEffect, useMemo, useState } from "react";
import { AdvancedTableFilters, type TableFilterDef } from "@/components/advanced-table-filters";
import { DataTable } from "@/components/data-table";
import { Badge } from "@/components/ui/badge";
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
import { filterBySelectedCompany } from "@/lib/company-scope-utils";
import {
  createUser,
  deleteUserApi,
  getUserById,
  getUsers,
  updateUser,
  type AppUser,
} from "@/lib/services/user-admin-service";
import { getEmployees, type Employee } from "@/lib/services/employee-service";
import { toast } from "sonner";

const selectClass =
  "flex h-10 w-full rounded-md border border-input bg-background px-3 py-2 text-sm ring-offset-background";

type UserRow = {
  id: string;
  userId: number;
  fullName: string;
  userName: string;
  email: string;
  rolesLabel: string;
  isActive: boolean;
  companyId: number;
  companyName: string;
  statusLabel: string;
};

function formatEmployeeName(e: Employee): string {
  const n = e.fullName?.trim();
  if (n) return n;
  return `${e.firstName} ${e.lastName}`.trim() || `Employee #${e.id}`;
}

function friendlyError(err: unknown, fallback: string): string {
  if (err instanceof ApiFormError) {
    if (err.message && !/request failed|status code/i.test(err.message)) return err.message;
  }
  if (err instanceof Error && err.message) {
    if (!/request failed|status code/i.test(err.message)) return err.message;
  }
  return fallback;
}

export default function UsersPage() {
  const { companies, companiesLoading, selectedCompanyId } = useSelectedCompany();
  const [list, setList] = useState<AppUser[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [dialogOpen, setDialogOpen] = useState(false);
  const [saving, setSaving] = useState(false);
  const [editingId, setEditingId] = useState<number | null>(null);
  const [fieldErrors, setFieldErrors] = useState<FieldErrors>({});

  const [fullName, setFullName] = useState("");
  const [userName, setUserName] = useState("");
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [phoneNumber, setPhoneNumber] = useState("");
  const [isActive, setIsActive] = useState(true);
  const [companyId, setCompanyId] = useState("");
  const [employeeId, setEmployeeId] = useState("");

  const [employees, setEmployees] = useState<Employee[]>([]);
  const [empLoading, setEmpLoading] = useState(false);

  const companyNameById = useMemo(
    () => new Map(companies.map((c) => [c.id, c.name])),
    [companies],
  );

  const loadUsers = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const data = await getUsers(
        selectedCompanyId != null && selectedCompanyId > 0 ? selectedCompanyId : undefined,
      );
      setList(data);
    } catch (e) {
      setError(friendlyError(e, "Failed to load users."));
      setList([]);
    } finally {
      setLoading(false);
    }
  }, [selectedCompanyId]);

  useEffect(() => {
    if (companiesLoading) return;
    void loadUsers();
  }, [companiesLoading, loadUsers]);

  useEffect(() => {
    if (!dialogOpen) return;
    let c = false;
    (async () => {
      setEmpLoading(true);
      try {
        const em = await getEmployees();
        if (!c) setEmployees(em);
      } catch {
        if (!c) setEmployees([]);
      } finally {
        if (!c) setEmpLoading(false);
      }
    })();
    return () => {
      c = true;
    };
  }, [dialogOpen]);

  const companiesForForm = useMemo(
    () =>
      selectedCompanyId == null ? companies : companies.filter((c) => c.id === selectedCompanyId),
    [companies, selectedCompanyId],
  );

  const employeesForCompany = useMemo(() => employees, [employees]);

  const resetForm = () => {
    setEditingId(null);
    setFullName("");
    setUserName("");
    setEmail("");
    setPassword("");
    setPhoneNumber("");
    setIsActive(true);
    setCompanyId(
      String(selectedCompanyId ?? companies[0]?.id ?? ""),
    );
    setEmployeeId("");
    setFieldErrors({});
  };

  const handleAdd = () => {
    resetForm();
    setDialogOpen(true);
  };

  const handleEdit = async (row: UserRow) => {
    try {
      setFieldErrors({});
      const u = await getUserById(row.userId);
      setEditingId(u.id);
      setFullName(u.fullName);
      setUserName(u.userName || "");
      setEmail(u.email || "");
      setPassword("");
      setPhoneNumber(u.phoneNumber || "");
      setIsActive(u.isActive);
      setCompanyId(String(u.companyId));
      setEmployeeId(u.linkedEmployeeId != null ? String(u.linkedEmployeeId) : "");
      setDialogOpen(true);
    } catch (e) {
      toast.error(friendlyError(e, "Could not load user."));
    }
  };

  const handleDelete = async (row: UserRow) => {
    if (!window.confirm(`Delete user "${row.fullName}"?`)) return;
    try {
      await deleteUserApi(row.userId);
      toast.success("User deleted.");
      await loadUsers();
    } catch (e) {
      toast.error(friendlyError(e, "Could not delete user."));
    }
  };

  const handleSave = async () => {
    const comp = Number(companyId);
    if (!fullName.trim()) {
      toast.error("Full name is required.");
      return;
    }
    if (!userName.trim() || !email.trim()) {
      toast.error("Username and email are required.");
      return;
    }
    if (!Number.isFinite(comp) || comp <= 0) {
      toast.error("Company is required.");
      return;
    }
    if (editingId == null && !password.trim()) {
      toast.error("Password is required for a new user.");
      return;
    }
    setSaving(true);
    setError(null);
    setFieldErrors({});
    try {
      const empN = employeeId ? Number(employeeId) : NaN;
      const payload = {
        fullName: fullName.trim(),
        userName: userName.trim(),
        email: email.trim(),
        phoneNumber: phoneNumber.trim() || null,
        isActive,
        companyId: comp,
        employeeId: Number.isFinite(empN) && empN > 0 ? empN : null,
      };
      if (editingId == null) {
        await createUser({ ...payload, password: password.trim() });
        toast.success("User created.");
      } else {
        await updateUser(editingId, {
          ...payload,
          password: password.trim() || undefined,
        });
        toast.success("User updated.");
      }
      setDialogOpen(false);
      resetForm();
      await loadUsers();
    } catch (e) {
      if (e instanceof ApiFormError) setFieldErrors(e.fieldErrors);
      toast.error(friendlyError(e, "Save failed."));
    } finally {
      setSaving(false);
    }
  };

  const rows: UserRow[] = useMemo(
    () =>
      list.map((u) => ({
        id: String(u.id),
        userId: u.id,
        fullName: u.fullName,
        userName: u.userName || "—",
        email: u.email || "—",
        rolesLabel: u.roles.length ? u.roles.join(", ") : "—",
        isActive: u.isActive,
        companyId: u.companyId,
        companyName: u.companyName || companyNameById.get(u.companyId) || `Company #${u.companyId}`,
        statusLabel: u.isActive ? "Active" : "Inactive",
      })),
    [list, companyNameById],
  );

  const scopedRows = useMemo(
    () => filterBySelectedCompany(rows, selectedCompanyId, (r) => r.companyId),
    [rows, selectedCompanyId],
  );

  const companyOptions = useMemo(
    () =>
      [...companies]
        .sort((a, b) => a.name.localeCompare(b.name))
        .map((c) => ({ value: String(c.id), label: c.name })),
    [companies],
  );

  const filterDefs = useMemo<TableFilterDef<UserRow>[]>(
    () => [
      {
        id: "userId",
        label: "ID",
        ui: "number",
        match: (row, get) => {
          const q = get("userId").trim();
          if (!q) return true;
          return String(row.userId).includes(q);
        },
      },
      {
        id: "fullName",
        label: "Name",
        ui: "text",
        match: (row, get) => {
          const q = get("fullName").trim().toLowerCase();
          if (!q) return true;
          return row.fullName.toLowerCase().includes(q);
        },
      },
      {
        id: "email",
        label: "Email",
        ui: "text",
        match: (row, get) => {
          const q = get("email").trim().toLowerCase();
          if (!q) return true;
          return row.email.toLowerCase().includes(q);
        },
      },
      {
        id: "company",
        label: "Company",
        ui: "select",
        options: companyOptions,
        match: (row, get) => {
          const v = get("company");
          if (!v) return true;
          return row.companyId === Number(v);
        },
      },
      {
        id: "status",
        label: "Status",
        ui: "status",
        match: (row, get) => {
          const v = get("status");
          if (v === "all" || v === "active") return row.isActive;
          if (v === "inactive") return !row.isActive;
          return true;
        },
      },
    ],
    [companyOptions],
  );

  const columns = [
    { key: "userId" as const, label: "ID" },
    { key: "fullName" as const, label: "Full name" },
    { key: "userName" as const, label: "Username" },
    { key: "email" as const, label: "Email" },
    { key: "rolesLabel" as const, label: "Roles" },
    {
      key: "statusLabel" as const,
      label: "Status",
      render: (v: string, row: UserRow) => (
        <Badge
          className={
            row.isActive
              ? "bg-emerald-100 text-emerald-800 hover:bg-emerald-100"
              : "bg-slate-200 text-slate-800 hover:bg-slate-200"
          }
        >
          {v}
        </Badge>
      ),
    },
    { key: "companyName" as const, label: "Company" },
  ];

  if (companiesLoading || loading) {
    return <div className="p-6 text-sm text-muted-foreground">Loading users…</div>;
  }

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-3xl font-bold text-foreground">Users</h1>
        <p className="text-muted-foreground mt-1">Manage application users, access, and company assignment.</p>
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
        <AdvancedTableFilters defs={filterDefs} data={scopedRows}>
          {(filtered) => (
            <DataTable
              title="User list"
              columns={columns}
              data={filtered}
              idSortKey="userId"
              searchPlaceholder="Search users…"
              searchableFields={["fullName", "userName", "email", "rolesLabel", "companyName", "id"]}
              onAdd={handleAdd}
              onEdit={handleEdit}
              onDelete={handleDelete}
            />
          )}
        </AdvancedTableFilters>

        <DialogContent className="sm:max-w-lg max-h-[90vh] overflow-y-auto">
          <DialogHeader>
            <DialogTitle>{editingId != null ? "Edit user" : "Add user"}</DialogTitle>
            <DialogDescription>Assign credentials, company, and optional employee link.</DialogDescription>
          </DialogHeader>

          <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
            <div className="sm:col-span-2">
              <Label htmlFor="u-fullname">Full name</Label>
              <Input
                id="u-fullname"
                className="mt-1"
                value={fullName}
                onChange={(e) => setFullName(e.target.value)}
              />
              {getFieldErrorMessage(fieldErrors, "fullname", "fullName") && (
                <p className="mt-1 text-xs text-destructive">
                  {getFieldErrorMessage(fieldErrors, "fullname", "fullName")}
                </p>
              )}
            </div>
            <div>
              <Label htmlFor="u-username">Username</Label>
              <Input
                id="u-username"
                className="mt-1"
                value={userName}
                onChange={(e) => setUserName(e.target.value)}
                autoComplete="off"
              />
              {getFieldErrorMessage(fieldErrors, "username", "userName") && (
                <p className="mt-1 text-xs text-destructive">
                  {getFieldErrorMessage(fieldErrors, "username", "userName")}
                </p>
              )}
            </div>
            <div>
              <Label htmlFor="u-email">Email</Label>
              <Input
                id="u-email"
                type="email"
                className="mt-1"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
              />
              {getFieldErrorMessage(fieldErrors, "email") && (
                <p className="mt-1 text-xs text-destructive">{getFieldErrorMessage(fieldErrors, "email")}</p>
              )}
            </div>
            {editingId == null && (
              <div className="sm:col-span-2">
                <Label htmlFor="u-password">Password</Label>
                <Input
                  id="u-password"
                  type="password"
                  className="mt-1"
                  value={password}
                  onChange={(e) => setPassword(e.target.value)}
                  autoComplete="new-password"
                />
                {getFieldErrorMessage(fieldErrors, "password") && (
                  <p className="mt-1 text-xs text-destructive">{getFieldErrorMessage(fieldErrors, "password")}</p>
                )}
              </div>
            )}
            {editingId != null && (
              <div className="sm:col-span-2">
                <Label htmlFor="u-newpassword">New password (optional)</Label>
                <Input
                  id="u-newpassword"
                  type="password"
                  className="mt-1"
                  value={password}
                  onChange={(e) => setPassword(e.target.value)}
                  autoComplete="new-password"
                />
                {getFieldErrorMessage(fieldErrors, "password") && (
                  <p className="mt-1 text-xs text-destructive">{getFieldErrorMessage(fieldErrors, "password")}</p>
                )}
              </div>
            )}
            <div className="sm:col-span-2">
              <Label htmlFor="u-phone">Phone</Label>
              <Input
                id="u-phone"
                className="mt-1"
                value={phoneNumber}
                onChange={(e) => setPhoneNumber(e.target.value)}
              />
            </div>
            <div className="sm:col-span-2">
              <Label htmlFor="u-company">Company</Label>
              <select
                id="u-company"
                className={selectClass + " mt-1"}
                value={companyId}
                onChange={(e) => {
                  setCompanyId(e.target.value);
                  setEmployeeId("");
                }}
              >
                <option value="">Select company</option>
                {companiesForForm.map((c) => (
                  <option key={c.id} value={String(c.id)}>
                    {c.name}
                  </option>
                ))}
              </select>
            </div>
            <div className="sm:col-span-2 flex items-center gap-2 pt-1">
              <Checkbox
                id="u-active"
                checked={isActive}
                onCheckedChange={(v) => setIsActive(v === true)}
              />
              <Label htmlFor="u-active" className="text-sm font-normal">
                Account active
              </Label>
            </div>
            <div className="sm:col-span-2">
              <Label htmlFor="u-emp">Linked employee (optional)</Label>
              <select
                id="u-emp"
                className={selectClass + " mt-1"}
                value={employeeId}
                onChange={(e) => setEmployeeId(e.target.value)}
                disabled={empLoading}
              >
                <option value="">{empLoading ? "Loading…" : "None"}</option>
                {employeesForCompany
                  .slice()
                  .sort((a, b) => formatEmployeeName(a).localeCompare(formatEmployeeName(b)))
                  .map((e) => (
                    <option key={e.id} value={String(e.id)}>
                      {formatEmployeeName(e)}
                    </option>
                  ))}
              </select>
            </div>
          </div>

          <div className="mt-4 flex justify-end gap-2">
            <Button variant="outline" onClick={() => setDialogOpen(false)} disabled={saving}>
              Cancel
            </Button>
            <Button onClick={() => void handleSave()} disabled={saving}>
              {saving ? "Saving…" : "Save"}
            </Button>
          </div>
        </DialogContent>
      </Dialog>
    </div>
  );
}
