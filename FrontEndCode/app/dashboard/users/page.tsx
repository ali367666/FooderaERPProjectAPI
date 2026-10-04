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
import { defaultFormCompanyId } from "@/lib/resolve-company-id";
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
import { getDepartmentsForAllCompanies, type Department } from "@/lib/services/department-service";
import { getPositionsForAllCompanies, type Position } from "@/lib/services/position-service";
import { getRestaurants, type Restaurant } from "@/lib/services/restaurant-service";
import { getRoles, type AppRole } from "@/lib/services/role-service";
import { getWarehouses, type Warehouse } from "@/lib/services/warehouse-service";
import { isProtectedAccount, roleDisplayName } from "@/lib/role-labels";
import { useIsSuperAdmin } from "@/hooks/use-auth-permissions";
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
  protectedAccount: boolean;
  /** From the linked employee — users without one have no department/position. */
  departmentId: number | null;
  departmentName: string;
  positionId: number | null;
  positionName: string;
};

/** Today's month and day (MMdd) — the suffix staff add to their code for the admin panel. */
function todayMonthDay(): string {
  const now = new Date();
  return String(now.getMonth() + 1).padStart(2, "0") + String(now.getDate()).padStart(2, "0");
}

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
  const isSuperAdmin = useIsSuperAdmin();
  const [list, setList] = useState<AppUser[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [dialogOpen, setDialogOpen] = useState(false);
  const [saving, setSaving] = useState(false);
  const [editingId, setEditingId] = useState<number | null>(null);
  const [fieldErrors, setFieldErrors] = useState<FieldErrors>({});

  const [userName, setUserName] = useState("");
  const [password, setPassword] = useState("");
  /** Editing a SuperAdmin account — those keep a real password instead of the code login. */
  const [editingProtected, setEditingProtected] = useState(false);
  const [phoneNumber, setPhoneNumber] = useState("");
  const [isActive, setIsActive] = useState(true);
  const [companyId, setCompanyId] = useState("");
  const [employeeId, setEmployeeId] = useState("");
  const [code, setCode] = useState("");
  const [rfidCardId, setRfidCardId] = useState("");
  const [canAccessAdminPanel, setCanAccessAdminPanel] = useState(true);
  const [canAccessFrontOffice, setCanAccessFrontOffice] = useState(false);
  const [workplaceType, setWorkplaceType] = useState("1");
  const [restaurantId, setRestaurantId] = useState("");
  const [warehouseId, setWarehouseId] = useState("");
  const [selectedRoleIds, setSelectedRoleIds] = useState<number[]>([]);
  const [pendingRoleNames, setPendingRoleNames] = useState<string[] | null>(null);

  const [employees, setEmployees] = useState<Employee[]>([]);
  const [empLoading, setEmpLoading] = useState(false);
  const [restaurants, setRestaurants] = useState<Restaurant[]>([]);
  const [restLoading, setRestLoading] = useState(false);
  const [warehouses, setWarehouses] = useState<Warehouse[]>([]);
  const [warehousesLoading, setWarehousesLoading] = useState(false);
  const [roles, setRoles] = useState<AppRole[]>([]);
  const [rolesLoading, setRolesLoading] = useState(false);

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

  // Filter options list every department/position of the visible companies, the same as on the
  // Employees page — not just the ones already linked to a user.
  const [departments, setDepartments] = useState<Department[]>([]);
  const [positions, setPositions] = useState<Position[]>([]);
  useEffect(() => {
    if (companiesLoading || companies.length === 0) return;
    let cancelled = false;
    const ids = companies.map((c) => c.id);
    Promise.all([getDepartmentsForAllCompanies(ids), getPositionsForAllCompanies(ids)])
      .then(([d, p]) => {
        if (cancelled) return;
        setDepartments(d);
        setPositions(p);
      })
      .catch(() => {
        if (cancelled) return;
        setDepartments([]);
        setPositions([]);
      });
    return () => {
      cancelled = true;
    };
  }, [companiesLoading, companies]);

  useEffect(() => {
    if (!dialogOpen) return;
    let c = false;
    (async () => {
      setEmpLoading(true);
      setRestLoading(true);
      try {
        const em = await getEmployees();
        if (!c) setEmployees(em);
      } catch {
        if (!c) setEmployees([]);
      } finally {
        if (!c) setEmpLoading(false);
      }
      try {
        const rs = await getRestaurants();
        if (!c) setRestaurants(rs);
      } catch {
        if (!c) setRestaurants([]);
      } finally {
        if (!c) setRestLoading(false);
      }
      setWarehousesLoading(true);
      try {
        const wh = await getWarehouses();
        if (!c) setWarehouses(wh);
      } catch {
        if (!c) setWarehouses([]);
      } finally {
        if (!c) setWarehousesLoading(false);
      }
    })();
    return () => {
      c = true;
    };
  }, [dialogOpen]);

  // Roles belong to a specific company — reload whenever the form's target company changes (a
  // SuperAdmin creating a user for a different tenant must see THAT tenant's roles, not their own).
  useEffect(() => {
    if (!dialogOpen) return;
    let cancelled = false;
    const targetCompanyId = Number(companyId) > 0 ? Number(companyId) : undefined;
    setRolesLoading(true);
    (async () => {
      try {
        const rl = await getRoles(targetCompanyId);
        if (!cancelled) setRoles(rl);
      } catch {
        if (!cancelled) setRoles([]);
      } finally {
        if (!cancelled) setRolesLoading(false);
      }
    })();
    return () => {
      cancelled = true;
    };
  }, [dialogOpen, companyId]);

  useEffect(() => {
    if (!pendingRoleNames || roles.length === 0) return;
    const ids = roles.filter((r) => pendingRoleNames.includes(r.name)).map((r) => r.id);
    setSelectedRoleIds(ids);
    setPendingRoleNames(null);
  }, [roles, pendingRoleNames]);

  const companiesForForm = useMemo(
    () =>
      selectedCompanyId == null ? companies : companies.filter((c) => c.id === selectedCompanyId),
    [companies, selectedCompanyId],
  );

  const employeesForCompany = useMemo(() => employees, [employees]);

  const restaurantsForCompany = useMemo(
    () => restaurants.filter((r) => String(r.companyId) === companyId),
    [restaurants, companyId],
  );

  const warehousesForCompany = useMemo(
    () => warehouses.filter((w) => String(w.companyId) === companyId),
    [warehouses, companyId],
  );

  const resetForm = () => {
    setEditingId(null);
    setUserName("");
    setPassword("");
    setEditingProtected(false);
    setPhoneNumber("");
    setIsActive(true);
    setCompanyId(defaultFormCompanyId(companies, selectedCompanyId));
    setEmployeeId("");
    setCode("");
    setRfidCardId("");
    setCanAccessAdminPanel(true);
    setCanAccessFrontOffice(false);
    setWorkplaceType("1");
    setRestaurantId("");
    setWarehouseId("");
    setSelectedRoleIds([]);
    setPendingRoleNames(null);
    setFieldErrors({});
  };

  const handleAdd = () => {
    resetForm();
    setDialogOpen(true);
  };

  const handleEdit = async (row: UserRow) => {
    if (row.protectedAccount && !isSuperAdmin) {
      toast.error("Bu hesabı yalnız platforma SuperAdmin-i redaktə edə bilər.");
      return;
    }
    try {
      setFieldErrors({});
      const u = await getUserById(row.userId);
      setEditingId(u.id);
      setUserName(u.userName || "");
      setPassword("");
      setEditingProtected(row.protectedAccount);
      setPhoneNumber(u.phoneNumber || "");
      setIsActive(u.isActive);
      setCompanyId(String(u.companyId));
      setEmployeeId(u.linkedEmployeeId != null ? String(u.linkedEmployeeId) : "");
      setCode(u.code || "");
      setRfidCardId(u.rfidCardId || "");
      setCanAccessAdminPanel(u.canAccessAdminPanel);
      setCanAccessFrontOffice(u.canAccessFrontOffice);
      setWorkplaceType(String(u.workplaceType || 1));
      setRestaurantId(u.restaurantId != null ? String(u.restaurantId) : "");
      setWarehouseId(u.warehouseId != null ? String(u.warehouseId) : "");
      setPendingRoleNames(u.roles);
      setDialogOpen(true);
    } catch (e) {
      toast.error(friendlyError(e, "Could not load user."));
    }
  };

  const handleDelete = async (row: UserRow) => {
    if (row.protectedAccount && !isSuperAdmin) {
      toast.error("Bu hesabı yalnız platforma SuperAdmin-i silə bilər.");
      return;
    }
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
    if (!userName.trim()) {
      toast.error("İstifadəçi adı tələb olunur.");
      return;
    }
    if (!Number.isFinite(comp) || comp <= 0) {
      toast.error("Company is required.");
      return;
    }
    if (code.trim() && !/^\d{4}$/.test(code.trim())) {
      toast.error("Kod dəqiq 4 rəqəm olmalıdır.");
      return;
    }
    if (!editingProtected && !code.trim() && (canAccessAdminPanel || canAccessFrontOffice)) {
      toast.error("POS və ya admin panel girişi üçün 4 rəqəmli kod tələb olunur.");
      return;
    }
    if (workplaceType === "2" && !restaurantId) {
      toast.error("Branch is required for a branch-scoped user.");
      return;
    }
    setSaving(true);
    setError(null);
    setFieldErrors({});
    try {
      const empN = employeeId ? Number(employeeId) : NaN;
      // Full name and email are no longer asked for — the backend keeps existing values and
      // shows staff by username.
      const payload = {
        userName: userName.trim(),
        phoneNumber: phoneNumber.trim() || null,
        isActive,
        companyId: comp,
        employeeId: Number.isFinite(empN) && empN > 0 ? empN : null,
        code: code.trim() || null,
        rfidCardId: rfidCardId.trim() || null,
        canAccessAdminPanel,
        canAccessFrontOffice,
        workplaceType: Number(workplaceType),
        restaurantId: workplaceType === "2" && restaurantId ? Number(restaurantId) : null,
        warehouseId: warehouseId ? Number(warehouseId) : null,
        roleIds: selectedRoleIds,
      };
      if (editingId == null) {
        await createUser(payload);
        toast.success("User created.");
      } else {
        await updateUser(editingId, {
          ...payload,
          // Only the SuperAdmin accounts still sign in with a real password.
          password: editingProtected ? password.trim() || undefined : undefined,
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

  const employeeById = useMemo(() => new Map(employees.map((e) => [e.id, e])), [employees]);

  const rows: UserRow[] = useMemo(
    () =>
      list.map((u) => {
        const emp = u.linkedEmployeeId != null ? employeeById.get(u.linkedEmployeeId) : undefined;
        return {
        id: String(u.id),
        userId: u.id,
        fullName: u.fullName,
        userName: u.userName || "—",
        email: u.email || "—",
        rolesLabel: u.roles.length ? u.roles.map(roleDisplayName).join(", ") : "—",
        isActive: u.isActive,
        companyId: u.companyId,
        companyName: u.companyName || companyNameById.get(u.companyId) || `Company #${u.companyId}`,
        statusLabel: u.isActive ? "Active" : "Inactive",
        protectedAccount: isProtectedAccount(u.roles),
        departmentId: emp?.departmentId || null,
        departmentName: emp?.departmentName || "—",
        positionId: emp?.positionId || null,
        positionName: emp?.positionName || "—",
        };
      }),
    [list, companyNameById, employeeById],
  );

  const departmentOptions = useMemo(
    () =>
      departments
        .filter((d) => selectedCompanyId == null || d.companyId === selectedCompanyId)
        .sort((a, b) => (a.name ?? "").localeCompare(b.name ?? ""))
        .map((d) => ({ value: String(d.id), label: d.name })),
    [departments, selectedCompanyId],
  );
  const positionOptions = useMemo(
    () =>
      positions
        .map((p) => ({
          id: Number(p.id ?? p.positionId ?? 0),
          name: String(p.name ?? p.positionName ?? ""),
          companyId: p.companyId,
        }))
        .filter((p) => p.id > 0 && (selectedCompanyId == null || p.companyId === selectedCompanyId))
        .sort((a, b) => a.name.localeCompare(b.name))
        .map((p) => ({ value: String(p.id), label: p.name })),
    [positions, selectedCompanyId],
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
        id: "department",
        label: "Department",
        ui: "select",
        options: departmentOptions,
        match: (row, get) => {
          const v = get("department");
          if (!v) return true;
          return row.departmentId === Number(v);
        },
      },
      {
        id: "position",
        label: "Position",
        ui: "select",
        options: positionOptions,
        match: (row, get) => {
          const v = get("position");
          if (!v) return true;
          return row.positionId === Number(v);
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
    [companyOptions, departmentOptions, positionOptions],
  );

  const columns = [
    { key: "userId" as const, label: "ID" },
    { key: "fullName" as const, label: "Full name" },
    { key: "userName" as const, label: "Username" },
    { key: "rolesLabel" as const, label: "Roles" },
    { key: "departmentName" as const, label: "Department" },
    { key: "positionName" as const, label: "Position" },
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
              searchableFields={["fullName", "userName", "rolesLabel", "departmentName", "positionName", "companyName", "id"]}
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
              <Label htmlFor="u-username">İstifadəçi adı</Label>
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
            {editingProtected && (
              <div className="sm:col-span-2">
                <Label htmlFor="u-newpassword">Yeni şifrə (könüllü)</Label>
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
                  // Roles are company-specific — a role checked for the previous company is
                  // meaningless (and silently dropped) once the target company changes.
                  setSelectedRoleIds([]);
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
            <div>
              <Label htmlFor="u-workplace">Workplace</Label>
              <select
                id="u-workplace"
                className={selectClass + " mt-1"}
                value={workplaceType}
                onChange={(e) => {
                  setWorkplaceType(e.target.value);
                  if (e.target.value !== "2") setRestaurantId("");
                }}
              >
                <option value="1">Head office</option>
                <option value="2">Branch</option>
              </select>
            </div>
            {workplaceType === "2" && (
              <div>
                <Label htmlFor="u-restaurant">Branch</Label>
                <select
                  id="u-restaurant"
                  className={selectClass + " mt-1"}
                  value={restaurantId}
                  onChange={(e) => setRestaurantId(e.target.value)}
                  disabled={restLoading}
                >
                  <option value="">{restLoading ? "Loading…" : "Select a branch"}</option>
                  {restaurantsForCompany.map((r) => (
                    <option key={r.id} value={String(r.id)}>
                      {r.name}
                    </option>
                  ))}
                </select>
              </div>
            )}
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
            <div>
              <Label htmlFor="u-code">Kod (4 rəqəm)</Label>
              <Input
                id="u-code"
                className="mt-1"
                value={code}
                onChange={(e) => setCode(e.target.value.replace(/\D/g, "").slice(0, 4))}
                placeholder="e.g. 0002"
                inputMode="numeric"
                maxLength={4}
                autoComplete="off"
              />
              {getFieldErrorMessage(fieldErrors, "code") && (
                <p className="mt-1 text-xs text-destructive">{getFieldErrorMessage(fieldErrors, "code")}</p>
              )}
            </div>
            <div>
              <Label htmlFor="u-rfid">RFID card id</Label>
              <Input
                id="u-rfid"
                className="mt-1"
                value={rfidCardId}
                onChange={(e) => setRfidCardId(e.target.value)}
                placeholder="Optional"
                autoComplete="off"
              />
              {getFieldErrorMessage(fieldErrors, "rfidCardId") && (
                <p className="mt-1 text-xs text-destructive">{getFieldErrorMessage(fieldErrors, "rfidCardId")}</p>
              )}
            </div>
            <div className="sm:col-span-2">
              <Label htmlFor="u-access">Giriş</Label>
              {/* A staff member works either on the POS or in the admin panel — never both. */}
              <select
                id="u-access"
                className={selectClass + " mt-1"}
                value={canAccessAdminPanel ? "admin" : "pos"}
                onChange={(e) => {
                  const admin = e.target.value === "admin";
                  setCanAccessAdminPanel(admin);
                  setCanAccessFrontOffice(!admin);
                }}
              >
                <option value="pos">POS</option>
                <option value="admin">Admin panel</option>
              </select>
              {!editingProtected && (
                <p className="mt-1 text-xs text-muted-foreground">
                  {canAccessAdminPanel ? (
                    <>
                      İstifadəçi adı və şifrə yerinə kod + bu günün ayı və günü — bu gün{" "}
                      <span className="font-mono">{(code || "1234") + todayMonthDay()}</span>.
                    </>
                  ) : (
                    <>
                      POS-da 4 rəqəmli kod ilə: <span className="font-mono">{code || "1234"}</span>.
                    </>
                  )}
                </p>
              )}
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
            <div className="sm:col-span-2">
              <Label htmlFor="u-warehouse">Anbar (depo)</Label>
              <select
                id="u-warehouse"
                className={selectClass + " mt-1"}
                value={warehouseId}
                onChange={(e) => setWarehouseId(e.target.value)}
                disabled={warehousesLoading}
              >
                <option value="">{warehousesLoading ? "Loading…" : "Yoxdur"}</option>
                {warehousesForCompany.map((w) => (
                  <option key={w.id} value={String(w.id)}>
                    {w.name}
                  </option>
                ))}
              </select>
            </div>
            <div className="sm:col-span-2">
              <Label htmlFor="u-role">Rol</Label>
              <select
                id="u-role"
                className={selectClass + " mt-1"}
                value={String(selectedRoleIds.find((id) => roles.some((r) => r.id === id)) ?? "")}
                onChange={(e) => setSelectedRoleIds(e.target.value ? [Number(e.target.value)] : [])}
                disabled={rolesLoading}
              >
                <option value="">{rolesLoading ? "Loading…" : roles.length === 0 ? "Rol tapılmadı" : "Rol seçin"}</option>
                {roles.map((r) => (
                  <option key={r.id} value={String(r.id)}>
                    {roleDisplayName(r.name)}
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
