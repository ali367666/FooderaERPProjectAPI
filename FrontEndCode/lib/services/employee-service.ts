import { api } from "@/lib/api";
import { ApiFormError, toApiFormError } from "@/lib/api-error";

type ApiResponse<T> = {
  success?: boolean;
  message?: string;
  data?: T;
};

export type Employee = {
  id: number;
  fullName?: string;
  firstName: string;
  lastName: string;
  fatherName?: string | null;
  phoneNumber?: string | null;
  email?: string | null;
  address?: string | null;
  hireDate: string;
  terminationDate?: string | null;
  isActive: boolean;
  departmentId: number;
  positionId: number;
  departmentName?: string;
  positionName?: string;
  userId?: number | null;
};

export type EmployeeMutationInput = {
  firstName: string;
  lastName: string;
  fatherName?: string;
  phoneNumber?: string;
  email?: string;
  address?: string;
  hireDate: string;
  departmentId: number;
  positionId: number;
  userId?: number | null;
};

function normalizeEmployee(item: unknown): Employee | null {
  if (!item || typeof item !== "object") return null;
  const raw = item as Record<string, unknown>;

  const id = Number(raw.id ?? raw.Id);
  if (!Number.isFinite(id) || id <= 0) return null;

  const firstName = String(raw.firstName ?? raw.FirstName ?? "").trim();
  const lastName = String(raw.lastName ?? raw.LastName ?? "").trim();
  const fullNameRaw = raw.fullName ?? raw.FullName;
  const fullName =
    typeof fullNameRaw === "string" && fullNameRaw.trim().length > 0
      ? fullNameRaw.trim()
      : `${firstName} ${lastName}`.trim();

  return {
    id,
    fullName,
    firstName,
    lastName,
    fatherName: (raw.fatherName ?? raw.FatherName ?? null) as string | null,
    phoneNumber: (raw.phoneNumber ?? raw.PhoneNumber ?? null) as string | null,
    email: (raw.email ?? raw.Email ?? null) as string | null,
    address: (raw.address ?? raw.Address ?? null) as string | null,
    hireDate: String(raw.hireDate ?? raw.HireDate ?? ""),
    terminationDate: (raw.terminationDate ?? raw.TerminationDate ?? null) as
      | string
      | null,
    isActive: Boolean(raw.isActive ?? raw.IsActive ?? true),
    departmentId: Number(raw.departmentId ?? raw.DepartmentId ?? 0),
    positionId: Number(raw.positionId ?? raw.PositionId ?? 0),
    departmentName: String(raw.departmentName ?? raw.DepartmentName ?? ""),
    positionName: String(raw.positionName ?? raw.PositionName ?? ""),
    userId: (raw.userId ?? raw.UserId ?? null) as number | null,
  };
}

export async function getEmployees(): Promise<Employee[]> {
  try {
    const response = await api.get<ApiResponse<Employee[]>>("/Employees");
    const payload = response.data;

    if (payload?.success === false) {
      throw new Error(payload.message || "Failed to fetch employees.");
    }

    const list = Array.isArray(payload?.data) ? payload.data : [];
    return list
      .map((employee) => normalizeEmployee(employee))
      .filter((employee): employee is Employee => employee !== null);
  } catch (error) {
    throw toApiFormError(error, "Failed to fetch employees.");
  }
}

export async function getEmployeesByPosition(
  positionName: string,
  companyId: number,
): Promise<Employee[]> {
  try {
    const response = await api.get("/Employees/by-position", {
      params: {
        positionName: positionName?.trim(),
        companyId,
      },
    });
    console.log("waiter api raw response", response);
    const payload = response.data as unknown;

    if (
      payload &&
      typeof payload === "object" &&
      "success" in payload &&
      (payload as { success?: boolean }).success === false
    ) {
      throw new Error(
        (payload as { message?: string }).message || "Failed to fetch employees by position.",
      );
    }

    const list = Array.isArray(payload)
      ? payload
      : payload &&
          typeof payload === "object" &&
          "data" in payload &&
          Array.isArray((payload as { data?: unknown }).data)
        ? ((payload as { data: unknown[] }).data ?? [])
        : payload &&
            typeof payload === "object" &&
            "result" in payload &&
            Array.isArray((payload as { result?: unknown }).result)
          ? ((payload as { result: unknown[] }).result ?? [])
        : [];

    return list
      .map((employee) => normalizeEmployee(employee))
      .filter((employee): employee is Employee => employee !== null);
  } catch (error) {
    throw toApiFormError(error, "Failed to fetch employees by position.");
  }
}

export async function createEmployee(data: EmployeeMutationInput): Promise<number> {
  try {
    const response = await api.post<ApiResponse<number>>("/Employees", data);
    const payload = response.data;

    if (payload?.success === false) {
      throw new ApiFormError(payload.message || "Failed to create employee.");
    }

    return Number(payload?.data ?? 0);
  } catch (error) {
    throw toApiFormError(error, "Failed to create employee.");
  }
}

export async function updateEmployee(
  id: number,
  data: EmployeeMutationInput,
): Promise<void> {
  try {
    const response = await api.put<ApiResponse<unknown>>(`/Employees/${id}`, data);
    const payload = response.data;

    if (payload?.success === false) {
      throw new ApiFormError(payload.message || "Failed to update employee.");
    }
  } catch (error) {
    throw toApiFormError(error, "Failed to update employee.");
  }
}

export async function deleteEmployee(id: number): Promise<void> {
  try {
    const response = await api.delete<ApiResponse<unknown>>(`/Employees/${id}`);
    const payload = response.data;

    if (payload?.success === false) {
      throw new ApiFormError(payload.message || "Failed to delete employee.");
    }
  } catch (error) {
    throw toApiFormError(error, "Failed to delete employee.");
  }
}
