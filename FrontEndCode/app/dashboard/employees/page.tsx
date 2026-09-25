"use client";

import { useEffect, useMemo, useState } from "react";
import { AdvancedTableFilters, type TableFilterDef } from "@/components/advanced-table-filters";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { DataTable } from "@/components/data-table";
import { Input } from "@/components/ui/input";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import {
  createEmployee,
  deleteEmployee,
  getEmployees,
  getEmployeesForAllCompanies,
  updateEmployee,
  type Employee,
} from "@/lib/services/employee-service";
import {
  getDepartmentsForAllCompanies,
  type Department,
} from "@/lib/services/department-service";
import { getPositionsForAllCompanies, type Position } from "@/lib/services/position-service";
import { getRestaurants, type Restaurant } from "@/lib/services/restaurant-service";
import { ApiFormError, getFieldErrorMessage, type FieldErrors } from "@/lib/api-error";
import { useSelectedCompany } from "@/contexts/selected-company-context";
import { filterBySelectedCompany } from "@/lib/company-scope-utils";
import { resolveCompanyId } from "@/lib/resolve-company-id";
import { useIsSuperAdmin } from "@/hooks/use-auth-permissions";

type EmployeeRow = {
  id: string;
  employeeId: number;
  fullName: string;
  email: string;
  phone: string;
  departmentName: string;
  positionName: string;
  restaurantName: string;
  hireDate: string;
  hireDateIso: string;
  isActive: boolean;
  firstName: string;
  lastName: string;
  fatherName: string;
  address: string;
  departmentId: number;
  positionId: number;
  restaurantId: number;
};

type EmployeeFormState = {
  id: number | null;
  firstName: string;
  lastName: string;
  fatherName: string;
  email: string;
  phoneNumber: string;
  address: string;
  hireDate: string;
  departmentId: string;
  positionId: string;
  restaurantId: string;
};

function emptyEmployeeForm(): EmployeeFormState {
  const today = new Date().toISOString().split("T")[0] ?? "";
  return {
    id: null,
    firstName: "",
    lastName: "",
    fatherName: "",
    email: "",
    phoneNumber: "",
    address: "",
    hireDate: today,
    departmentId: "",
    positionId: "",
    restaurantId: "",
  };
}

function formatDate(dateValue?: string | null): string {
  if (!dateValue) return "-";
  const parsed = new Date(dateValue);
  if (Number.isNaN(parsed.getTime())) return "-";
  return parsed.toLocaleDateString();
}

function toFormDate(dateValue?: string | null): string {
  if (!dateValue) return "";
  const parsed = new Date(dateValue);
  if (Number.isNaN(parsed.getTime())) return "";
  return parsed.toISOString().split("T")[0] ?? "";
}

export default function EmployeesPage() {
  const { companies, companiesLoading, selectedCompanyId: scopeCompanyId } = useSelectedCompany();
  const [employees, setEmployees] = useState<Employee[]>([]);
  const [departments, setDepartments] = useState<Department[]>([]);
  const [positions, setPositions] = useState<Position[]>([]);
  const [restaurants, setRestaurants] = useState<Restaurant[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [isDialogOpen, setIsDialogOpen] = useState(false);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [isEditMode, setIsEditMode] = useState(false);
  const [form, setForm] = useState<EmployeeFormState>(emptyEmployeeForm);
  const [fieldErrors, setFieldErrors] = useState<FieldErrors>({});

  const loadEmployees = async (silent = false) => {
    try {
      if (!silent) setLoading(true);
      setError(null);
      // With no cross-company list (no Company.View — a regular tenant Admin), the backend already
      // defaults to the caller's own company; only loop per company when there's a list to loop over.
      const ids = companies.map((c) => c.id);
      const data = ids.length > 0 ? await getEmployeesForAllCompanies(ids) : await getEmployees();
      setEmployees(data);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to load employees.");
      throw err;
    } finally {
      if (!silent) setLoading(false);
    }
  };

  const loadReferenceData = async () => {
    const restaurantData = await getRestaurants();
    setRestaurants(restaurantData);

    if (companies.length === 0) {
      setDepartments([]);
      setPositions([]);
      return;
    }
    const ids = companies.map((c) => c.id);
    const [departmentData, positionData] = await Promise.all([
      getDepartmentsForAllCompanies(ids),
      getPositionsForAllCompanies(ids),
    ]);
    setDepartments(departmentData);
    setPositions(positionData);
  };

  useEffect(() => {
    if (companiesLoading) return;
    let isMounted = true;

    (async () => {
      try {
        setLoading(true);
        setError(null);
        const ids = companies.map((c) => c.id);
        const [employeeResult, departmentResult, positionResult, restaurantResult] = await Promise.allSettled([
          ids.length ? getEmployeesForAllCompanies(ids) : getEmployees(),
          ids.length ? getDepartmentsForAllCompanies(ids) : Promise.resolve([]),
          ids.length ? getPositionsForAllCompanies(ids) : Promise.resolve([]),
          getRestaurants(),
        ]);

        if (!isMounted) return;

        if (employeeResult.status === "fulfilled") {
          setEmployees(employeeResult.value);
        } else {
          throw employeeResult.reason;
        }

        if (departmentResult.status === "fulfilled") {
          setDepartments(departmentResult.value);
        } else {
          setDepartments([]);
        }

        if (positionResult.status === "fulfilled") {
          setPositions(positionResult.value);
        } else {
          setPositions([]);
        }

        if (restaurantResult.status === "fulfilled") {
          setRestaurants(restaurantResult.value);
        } else {
          setRestaurants([]);
        }
      } catch (err) {
        setError(err instanceof Error ? err.message : "Failed to load employees.");
      } finally {
        if (isMounted) setLoading(false);
      }
    })();

    return () => {
      isMounted = false;
    };
  }, [companiesLoading, companies]);

  const departmentCompanyMap = useMemo(
    () => new Map(departments.map((d) => [d.id, d.companyId])),
    [departments],
  );

  const scopedEmployees = useMemo(
    () =>
      filterBySelectedCompany(employees, scopeCompanyId, (e) =>
        departmentCompanyMap.get(e.departmentId),
      ),
    [employees, scopeCompanyId, departmentCompanyMap],
  );

  const departmentsForFilters = useMemo(() => {
    if (scopeCompanyId == null) return departments;
    return departments.filter((d) => d.companyId === scopeCompanyId);
  }, [departments, scopeCompanyId]);

  // A tenant Admin's employee creation is always scoped server-side to their OWN company (never to
  // whatever "Company filter" is picked in the toolbar), so their Add/Edit form must only ever offer
  // departments/positions from that same company — otherwise saving fails with "Department not found."
  // A SuperAdmin, however, can target any company: the backend now resolves the employee's company
  // from whichever Department they pick, so the form should offer every company's departments.
  const isSuperAdmin = useIsSuperAdmin();

  const ownCompanyId = useMemo(() => {
    try {
      return resolveCompanyId();
    } catch {
      return null;
    }
  }, []);

  const departmentsForForm = useMemo(() => {
    if (isSuperAdmin) return departments;
    if (ownCompanyId == null) return departmentsForFilters;
    return departments.filter((d) => d.companyId === ownCompanyId);
  }, [departments, isSuperAdmin, ownCompanyId, departmentsForFilters]);

  const positionsForFilters = useMemo(() => {
    const deptIds = new Set(departmentsForFilters.map((d) => d.id));
    return positions.filter((p) => deptIds.has(Number(p.departmentId ?? 0)));
  }, [positions, departmentsForFilters]);

  const filteredPositions = useMemo(() => {
    const selectedDepartmentId = Number(form.departmentId);
    if (!Number.isFinite(selectedDepartmentId) || selectedDepartmentId <= 0) {
      const pool =
        isSuperAdmin || ownCompanyId == null
          ? positions
          : positions.filter((p) => {
              const cid = departmentCompanyMap.get(Number(p.departmentId ?? 0));
              return cid === ownCompanyId;
            });
      return pool;
    }

    return positions.filter((position) => {
      const departmentId = Number(position.departmentId ?? 0);
      return departmentId === selectedDepartmentId;
    });
  }, [form.departmentId, positions, isSuperAdmin, ownCompanyId, departmentCompanyMap]);

  // Branch always belongs to the same company as the chosen Department — narrow to it once a
  // Department is picked, otherwise fall back to the same "own company vs. every company" pool.
  const restaurantsForForm = useMemo(() => {
    const selectedDepartmentId = Number(form.departmentId);
    if (Number.isFinite(selectedDepartmentId) && selectedDepartmentId > 0) {
      const cid = departmentCompanyMap.get(selectedDepartmentId);
      if (cid != null) return restaurants.filter((r) => r.companyId === cid);
    }
    if (isSuperAdmin || ownCompanyId == null) return restaurants;
    return restaurants.filter((r) => r.companyId === ownCompanyId);
  }, [restaurants, form.departmentId, departmentCompanyMap, isSuperAdmin, ownCompanyId]);

  const sortedDepartmentOptions = useMemo(
    () =>
      [...departmentsForFilters]
        .sort((a, b) => (a.name ?? "").localeCompare(b.name ?? ""))
        .map((d) => ({ value: String(d.id), label: d.name })),
    [departmentsForFilters],
  );

  const sortedPositionOptions = useMemo(
    () =>
      [...positionsForFilters]
        .sort((a, b) => (a.name ?? "").localeCompare(b.name ?? ""))
        .map((p) => {
          const pid = Number(p.id ?? p.positionId ?? 0);
          const label = String(p.name ?? p.positionName ?? "");
          return { value: String(pid), label };
        }),
    [positionsForFilters],
  );

  const rows: EmployeeRow[] = useMemo(
    () =>
      scopedEmployees.map((employee) => ({
        id: String(employee.id),
        employeeId: employee.id,
        fullName:
          employee.fullName?.trim() || `${employee.firstName} ${employee.lastName}`.trim(),
        email: employee.email?.trim() || "-",
        phone: employee.phoneNumber?.trim() || "-",
        departmentName: employee.departmentName?.trim() || "-",
        positionName: employee.positionName?.trim() || "-",
        restaurantName: employee.restaurantName?.trim() || "-",
        hireDate: formatDate(employee.hireDate),
        hireDateIso: toFormDate(employee.hireDate),
        isActive: employee.isActive,
        firstName: employee.firstName || "",
        lastName: employee.lastName || "",
        fatherName: employee.fatherName || "",
        address: (employee.address || "").trim(),
        departmentId: employee.departmentId,
        positionId: employee.positionId,
        restaurantId: employee.restaurantId,
      })),
    [scopedEmployees],
  );

  const employeeFilterDefs = useMemo<TableFilterDef<EmployeeRow>[]>(
    () => [
      {
        id: "employeeId",
        label: "ID",
        ui: "number",
        match: (row, get) => {
          const q = get("employeeId").trim().toLowerCase();
          if (!q) return true;
          return String(row.employeeId).toLowerCase().includes(q);
        },
      },
      {
        id: "fullName",
        label: "Employee Name",
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
        id: "phone",
        label: "Phone",
        ui: "text",
        match: (row, get) => {
          const q = get("phone").trim().toLowerCase();
          if (!q) return true;
          return row.phone.toLowerCase().includes(q);
        },
      },
      {
        id: "address",
        label: "Address",
        ui: "text",
        match: (row, get) => {
          const q = get("address").trim().toLowerCase();
          if (!q) return true;
          return row.address.toLowerCase().includes(q);
        },
      },
      {
        id: "department",
        label: "Department",
        ui: "select",
        options: sortedDepartmentOptions,
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
        options: sortedPositionOptions,
        match: (row, get) => {
          const v = get("position");
          if (!v) return true;
          return row.positionId === Number(v);
        },
      },
      {
        id: "hireDate",
        label: "Hire Date",
        ui: "dateRange",
        gridClassName: "min-w-[260px] max-w-full flex-[2_1_360px]",
        match: (row, get) => {
          const from = get("hireDate:from");
          const to = get("hireDate:to");
          if (!from && !to) return true;
          const raw = row.hireDateIso;
          if (!raw) return false;
          if (from && raw < from) return false;
          if (to && raw > to) return false;
          return true;
        },
      },
      {
        id: "status",
        label: "Status",
        ui: "status",
        match: (row, get) => {
          const v = get("status");
          if (v === "all") return true;
          if (v === "active") return row.isActive;
          if (v === "inactive") return !row.isActive;
          return true;
        },
      },
    ],
    [sortedDepartmentOptions, sortedPositionOptions],
  );

  const columns = [
    { key: "employeeId" as const, label: "ID" },
    { key: "fullName" as const, label: "Employee" },
    { key: "email" as const, label: "Email" },
    { key: "phone" as const, label: "Phone" },
    { key: "departmentName" as const, label: "Department" },
    { key: "positionName" as const, label: "Position" },
    { key: "restaurantName" as const, label: "Branch" },
    { key: "hireDate" as const, label: "Hire Date" },
    {
      key: "isActive" as const,
      label: "Status",
      render: (_: boolean, row: EmployeeRow) => (
        <Badge
          className={
            row.isActive
              ? "bg-emerald-100 text-emerald-800 hover:bg-emerald-100"
              : "bg-amber-100 text-amber-800 hover:bg-amber-100"
          }
        >
          {row.isActive ? "Active" : "Inactive"}
        </Badge>
      ),
    },
  ];

  const handleAdd = () => {
    setForm(emptyEmployeeForm());
    setFieldErrors({});
    setIsEditMode(false);
    setIsDialogOpen(true);
  };

  const handleEdit = (row: EmployeeRow) => {
    const employee = employees.find((item) => item.id === row.employeeId);
    if (!employee) return;

    setForm({
      id: employee.id,
      firstName: employee.firstName || "",
      lastName: employee.lastName || "",
      fatherName: employee.fatherName || "",
      email: employee.email || "",
      phoneNumber: employee.phoneNumber || "",
      address: employee.address || "",
      hireDate: toFormDate(employee.hireDate),
      departmentId: String(employee.departmentId || ""),
      positionId: String(employee.positionId || ""),
      restaurantId: String(employee.restaurantId || ""),
    });
    setFieldErrors({});
    setIsEditMode(true);
    setIsDialogOpen(true);
  };

  const handleDelete = async (row: EmployeeRow) => {
    const confirmed = window.confirm(`Delete "${row.fullName}"?`);
    if (!confirmed) return;

    try {
      setError(null);
      await deleteEmployee(row.employeeId);
      await loadEmployees(true);
    } catch (err) {
      const message = err instanceof Error ? err.message : "Failed to delete employee.";
      setError(message);
      window.alert(message);
    }
  };

  const handleSave = async () => {
    const firstName = form.firstName.trim();
    const lastName = form.lastName.trim();
    const departmentId = Number(form.departmentId);
    const positionId = Number(form.positionId);
    const restaurantId = Number(form.restaurantId);

    if (!firstName || !lastName) {
      const msg = "First name and last name are required.";
      setError(msg);
      window.alert(msg);
      return;
    }

    if (!Number.isFinite(departmentId) || departmentId <= 0) {
      const msg = "Department selection is required.";
      setError(msg);
      window.alert(msg);
      return;
    }

    if (!Number.isFinite(positionId) || positionId <= 0) {
      const msg = "Position selection is required.";
      setError(msg);
      window.alert(msg);
      return;
    }

    if (!Number.isFinite(restaurantId) || restaurantId <= 0) {
      const msg = "Branch selection is required.";
      setError(msg);
      window.alert(msg);
      return;
    }

    if (!form.hireDate) {
      const msg = "Hire date is required.";
      setError(msg);
      window.alert(msg);
      return;
    }

    const payload = {
      firstName,
      lastName,
      fatherName: form.fatherName.trim() || undefined,
      phoneNumber: form.phoneNumber.trim() || undefined,
      email: form.email.trim() || undefined,
      address: form.address.trim() || undefined,
      hireDate: new Date(form.hireDate).toISOString(),
      departmentId,
      positionId,
      restaurantId,
      userId: null,
    };

    try {
      setIsSubmitting(true);
      setError(null);
      setFieldErrors({});

      if (isEditMode && form.id) {
        await updateEmployee(form.id, payload);
      } else {
        await createEmployee(payload);
      }

      await loadEmployees(true);
      await loadReferenceData();
      setIsDialogOpen(false);
      setIsEditMode(false);
      setForm(emptyEmployeeForm());
    } catch (err) {
      if (err instanceof ApiFormError) {
        setFieldErrors(err.fieldErrors);
      }
      const message =
        err instanceof Error
          ? err.message
          : "Employee save failed due to an unexpected error.";
      setError(message);
      window.alert(message);
    } finally {
      setIsSubmitting(false);
    }
  };

  if (companiesLoading || loading) {
    return <div className="p-6 text-sm text-muted-foreground">Loading employees...</div>;
  }

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-3xl font-bold text-foreground">Employees</h1>
        <p className="text-muted-foreground mt-1">
          Manage employees, departments, and positions.
        </p>
      </div>

      {error && (
        <div className="rounded-md border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-600">
          {error}
        </div>
      )}

      <Dialog
        open={isDialogOpen}
        onOpenChange={(open) => {
          setIsDialogOpen(open);
          if (!open) {
            setIsEditMode(false);
            setForm(emptyEmployeeForm());
          }
        }}
      >
        <AdvancedTableFilters defs={employeeFilterDefs} data={rows}>
          {(filtered) => (
            <DataTable
              title="Employee List"
              columns={columns}
              data={filtered}
              idSortKey="employeeId"
              searchableFields={[
                "id",
                "fullName",
                "email",
                "phone",
                "departmentName",
                "positionName",
                "hireDate",
                "address",
              ]}
              searchPlaceholder="Search employees, email, phone, department, position..."
              onAdd={handleAdd}
              onEdit={handleEdit}
              onDelete={handleDelete}
            />
          )}
        </AdvancedTableFilters>

        <DialogContent className="sm:max-w-lg">
          <DialogHeader>
            <DialogTitle>{isEditMode ? "Edit Employee" : "Add Employee"}</DialogTitle>
            <DialogDescription>
              Enter employee details and save changes.
            </DialogDescription>
          </DialogHeader>

          <div className="grid grid-cols-1 gap-4 md:grid-cols-2">
            <div>
              <label className="mb-2 block text-sm font-medium text-foreground">
                First Name
              </label>
              <Input
                value={form.firstName}
                onChange={(e) => setForm((f) => ({ ...f, firstName: e.target.value }))}
                placeholder="Enter first name"
              />
              {getFieldErrorMessage(fieldErrors, "firstname") && (
                <p className="mt-1 text-xs text-red-600">
                  {getFieldErrorMessage(fieldErrors, "firstname")}
                </p>
              )}
            </div>

            <div>
              <label className="mb-2 block text-sm font-medium text-foreground">
                Last Name
              </label>
              <Input
                value={form.lastName}
                onChange={(e) => setForm((f) => ({ ...f, lastName: e.target.value }))}
                placeholder="Enter last name"
              />
              {getFieldErrorMessage(fieldErrors, "lastname") && (
                <p className="mt-1 text-xs text-red-600">
                  {getFieldErrorMessage(fieldErrors, "lastname")}
                </p>
              )}
            </div>

            <div>
              <label className="mb-2 block text-sm font-medium text-foreground">
                Father Name
              </label>
              <Input
                value={form.fatherName}
                onChange={(e) => setForm((f) => ({ ...f, fatherName: e.target.value }))}
                placeholder="Enter father name"
              />
            </div>

            <div>
              <label className="mb-2 block text-sm font-medium text-foreground">
                Hire Date
              </label>
              <Input
                type="date"
                value={form.hireDate}
                onChange={(e) => setForm((f) => ({ ...f, hireDate: e.target.value }))}
              />
              {getFieldErrorMessage(fieldErrors, "hiredate") && (
                <p className="mt-1 text-xs text-red-600">
                  {getFieldErrorMessage(fieldErrors, "hiredate")}
                </p>
              )}
            </div>

            <div>
              <label className="mb-2 block text-sm font-medium text-foreground">
                Email
              </label>
              <Input
                type="email"
                value={form.email}
                onChange={(e) => setForm((f) => ({ ...f, email: e.target.value }))}
                placeholder="Enter email"
              />
              {getFieldErrorMessage(fieldErrors, "email") && (
                <p className="mt-1 text-xs text-red-600">
                  {getFieldErrorMessage(fieldErrors, "email")}
                </p>
              )}
            </div>

            <div>
              <label className="mb-2 block text-sm font-medium text-foreground">
                Phone
              </label>
              <Input
                value={form.phoneNumber}
                onChange={(e) => setForm((f) => ({ ...f, phoneNumber: e.target.value }))}
                placeholder="Enter phone"
              />
            </div>

            <div className="md:col-span-2">
              <label className="mb-2 block text-sm font-medium text-foreground">
                Address
              </label>
              <Input
                value={form.address}
                onChange={(e) => setForm((f) => ({ ...f, address: e.target.value }))}
                placeholder="Enter address"
              />
            </div>

            <div>
              <label className="mb-2 block text-sm font-medium text-foreground">
                Department
              </label>
              <select
                value={form.departmentId}
                onChange={(e) =>
                  setForm((f) => ({
                    ...f,
                    departmentId: e.target.value,
                    positionId: "",
                    restaurantId: "",
                  }))
                }
                className="flex h-10 w-full rounded-md border border-input bg-background px-3 py-2 text-sm ring-offset-background"
              >
                <option value="">Select department</option>
                {departmentsForForm.map((department) => (
                  <option key={department.id} value={department.id}>
                    {department.name}
                  </option>
                ))}
              </select>
              {getFieldErrorMessage(fieldErrors, "departmentid") && (
                <p className="mt-1 text-xs text-red-600">
                  {getFieldErrorMessage(fieldErrors, "departmentid")}
                </p>
              )}
            </div>

            <div>
              <label className="mb-2 block text-sm font-medium text-foreground">
                Position
              </label>
              <select
                value={form.positionId}
                onChange={(e) => setForm((f) => ({ ...f, positionId: e.target.value }))}
                className="flex h-10 w-full rounded-md border border-input bg-background px-3 py-2 text-sm ring-offset-background"
              >
                <option value="">Select position</option>
                {filteredPositions.map((position) => (
                  <option key={String(position.id)} value={Number(position.id)}>
                    {position.name || position.positionName || "Unnamed Position"}
                  </option>
                ))}
              </select>
              {getFieldErrorMessage(fieldErrors, "positionid") && (
                <p className="mt-1 text-xs text-red-600">
                  {getFieldErrorMessage(fieldErrors, "positionid")}
                </p>
              )}
            </div>

            <div>
              <label className="mb-2 block text-sm font-medium text-foreground">
                Branch
              </label>
              <select
                value={form.restaurantId}
                onChange={(e) => setForm((f) => ({ ...f, restaurantId: e.target.value }))}
                className="flex h-10 w-full rounded-md border border-input bg-background px-3 py-2 text-sm ring-offset-background"
              >
                <option value="">Select branch</option>
                {restaurantsForForm.map((restaurant) => (
                  <option key={restaurant.id} value={restaurant.id}>
                    {restaurant.name}
                  </option>
                ))}
              </select>
              {getFieldErrorMessage(fieldErrors, "restaurantid") && (
                <p className="mt-1 text-xs text-red-600">
                  {getFieldErrorMessage(fieldErrors, "restaurantid")}
                </p>
              )}
            </div>
          </div>

          <div className="flex justify-end gap-3">
            <Button
              variant="outline"
              onClick={() => {
                setIsDialogOpen(false);
                setIsEditMode(false);
                setForm(emptyEmployeeForm());
              }}
              disabled={isSubmitting}
            >
              Cancel
            </Button>
            <Button onClick={handleSave} disabled={isSubmitting}>
              {isSubmitting ? "Saving..." : "Save"}
            </Button>
          </div>
        </DialogContent>
      </Dialog>
    </div>
  );
}
