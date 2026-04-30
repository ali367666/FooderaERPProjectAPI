import axios from "axios";
import { api } from "@/lib/api";
import { ApiFormError, toApiFormError } from "@/lib/api-error";
import { fetchListsPerCompany } from "@/lib/company-scope-utils";

export type Position = {
  id: number | string;
  positionId?: number | string;
  name?: string;
  positionName?: string;
  description?: string | null;
  departmentId?: number | string;
  companyId?: number | string;
  departmentName?: string;
  companyName?: string;
  department?: { id?: number | string; name?: string };
  company?: { id?: number | string; name?: string };
  status?: string | boolean;
  isActive?: boolean;
};

type ApiResponse<T> = {
  success?: boolean;
  message?: string;
  data?: T;
  items?: T;
};

export type PositionMutationInput = {
  name: string;
  departmentId: number;
  description?: string;
  companyId?: number;
  isActive?: boolean;
};

const TEMP_COMPANY_ID_FALLBACK = 5;

/** Numeric id for PUT/DELETE `/Positions/{id}` */
export function getPositionNumericId(
  position: Pick<Position, "id" | "positionId">,
): number | null {
  const raw = position.id ?? position.positionId;
  if (raw === undefined || raw === null) return null;
  const n = typeof raw === "number" ? raw : Number(String(raw).trim());
  if (!Number.isFinite(n) || n <= 0) return null;
  return Math.trunc(n);
}

function resolveCompanyId(explicitCompanyId?: number): number {
  if (typeof explicitCompanyId === "number" && explicitCompanyId > 0) {
    return explicitCompanyId;
  }

  if (typeof window !== "undefined") {
    const storedCompanyId = localStorage.getItem("companyId");
    const parsedCompanyId = storedCompanyId ? Number(storedCompanyId) : NaN;
    if (Number.isFinite(parsedCompanyId) && parsedCompanyId > 0) {
      return parsedCompanyId;
    }
  }

  return TEMP_COMPANY_ID_FALLBACK;
}

function pickFirst<T>(...values: Array<T | undefined | null>): T | undefined {
  for (const value of values) {
    if (value !== undefined && value !== null) return value;
  }
  return undefined;
}

function getNestedValue(
  source: Record<string, unknown>,
  key: string,
  nestedKey: string,
): unknown {
  const candidate = source[key];
  if (!candidate || typeof candidate !== "object") return undefined;
  return (candidate as Record<string, unknown>)[nestedKey];
}

function pickNestedEntity(
  raw: Record<string, unknown>,
  camel: string,
  pascal: string,
): { name?: string } | undefined {
  const node = raw[camel] ?? raw[pascal];
  if (!node || typeof node !== "object") return undefined;
  const n = pickFirst(
    (node as Record<string, unknown>).name,
    (node as Record<string, unknown>).Name,
  );
  if (n == null) return undefined;
  return { name: typeof n === "string" ? n : String(n) };
}

function normalizePosition(item: unknown): Position | null {
  if (!item || typeof item !== "object") return null;
  const raw = item as Record<string, unknown>;

  const id = pickFirst(
    raw.id,
    raw.Id,
    raw.positionId,
    raw.PositionId,
  ) as string | number | undefined;
  const name = pickFirst(raw.name, raw.Name);
  const positionNameOnly = pickFirst(raw.positionName, raw.PositionName);
  const description = pickFirst(
    raw.description,
    raw.Description,
  ) as string | null | undefined;
  const departmentId = pickFirst(
    raw.departmentId,
    raw.DepartmentId,
    getNestedValue(raw, "department", "id"),
    getNestedValue(raw, "Department", "id"),
  ) as string | number | undefined;
  const department = pickNestedEntity(raw, "department", "Department");
  const departmentName = pickFirst(
    raw.departmentName,
    raw.DepartmentName,
    getNestedValue(raw, "department", "name"),
    getNestedValue(raw, "Department", "name"),
    department?.name,
  ) as string | undefined;
  const companyId = pickFirst(
    raw.companyId,
    raw.CompanyId,
    getNestedValue(raw, "company", "id"),
    getNestedValue(raw, "Company", "id"),
  ) as string | number | undefined;
  const company = pickNestedEntity(raw, "company", "Company");
  const companyName = pickFirst(
    raw.companyName,
    raw.CompanyName,
    getNestedValue(raw, "company", "name"),
    getNestedValue(raw, "Company", "name"),
    company?.name,
  ) as string | undefined;
  const status = pickFirst(raw.status, raw.Status) as string | boolean | undefined;
  const isActive = pickFirst(
    raw.isActive,
    raw.IsActive,
  ) as boolean | undefined;

  if (id === undefined || id === null) {
    return null;
  }

  const primaryName = pickFirst(name, positionNameOnly);
  const nameStr =
    primaryName == null
      ? ""
      : typeof primaryName === "string"
        ? primaryName
        : String(primaryName);

  const positionNameStr =
    positionNameOnly == null
      ? undefined
      : typeof positionNameOnly === "string"
        ? positionNameOnly
        : String(positionNameOnly);

  const positionIdAlias = pickFirst(
    raw.positionId,
    raw.PositionId,
  ) as string | number | undefined;

  return {
    id,
    positionId: positionIdAlias,
    name: nameStr,
    positionName: positionNameStr,
    description: typeof description === "string" ? description : undefined,
    departmentId,
    departmentName,
    department,
    companyId,
    companyName,
    company,
    status,
    isActive,
  };
}

export async function getPositions(companyId?: number): Promise<Position[]> {
  try {
    const effectiveCompanyId = resolveCompanyId(companyId);
    const response = await api.get("/Positions", {
      params: { companyId: effectiveCompanyId },
    });

    console.log("Positions API raw:", response.data);

    const body = response.data as {
      success?: boolean;
      Success?: boolean;
      data?: unknown;
      Data?: unknown;
    } | null;

    if (!body || typeof body !== "object") {
      console.log("Parsed positions:", []);
      return [];
    }

    if (body.success === false || body.Success === false) {
      const message =
        (body as { message?: string; Message?: string }).message ||
        (body as { Message?: string }).Message ||
        "Failed to fetch positions";
      throw new Error(message);
    }

    const rawList = body.data ?? body.Data;
    const positions = Array.isArray(rawList) ? rawList : [];
    console.log("Parsed positions:", positions);

    return positions
      .map((item) => normalizePosition(item))
      .filter((item): item is Position => item !== null);
  } catch (error) {
    throw toApiFormError(error, "Failed to fetch positions");
  }
}

export async function getPositionsForAllCompanies(companyIds: number[]): Promise<Position[]> {
  return fetchListsPerCompany(companyIds, (id) => getPositions(id));
}

export async function createPosition(data: PositionMutationInput): Promise<void> {
  const payload = {
    name: data.name,
    departmentId: data.departmentId,
    description: data.description || "",
    companyId: resolveCompanyId(data.companyId),
    isActive: typeof data.isActive === "boolean" ? data.isActive : true,
  };

  try {
    console.log("CREATE POSITION PAYLOAD:", payload);
    const response = await api.post<ApiResponse<unknown>>("/Positions", payload);
    const responsePayload = response.data;

    if (responsePayload?.success === false) {
      throw new ApiFormError(responsePayload.message || "Failed to create position");
    }
  } catch (error) {
    throw toApiFormError(error, "Failed to create position");
  }
}

function toNumericRouteId(id: string | number): number {
  const n = typeof id === "number" ? id : Number(String(id).trim());
  if (!Number.isFinite(n) || n <= 0) {
    throw new Error("Invalid position id for update.");
  }
  return Math.trunc(n);
}

export async function updatePosition(
  id: string | number,
  data: PositionMutationInput,
): Promise<void> {
  const numericId = toNumericRouteId(id);
  const endpointPath = `/Positions/${numericId}`;
  const payload = {
    name: data.name,
    departmentId: data.departmentId,
    description: data.description || "",
  };

  console.log("UPDATE endpoint:", endpointPath);
  console.log("UPDATE payload:", payload);

  try {
    const response = await api.put<ApiResponse<unknown>>(endpointPath, payload);
    const responsePayload = response.data;

    if (responsePayload?.success === false) {
      throw new ApiFormError(responsePayload.message || "Failed to update position");
    }
  } catch (error) {
    if (axios.isAxiosError(error)) {
      console.error("updatePosition failed:", {
        endpoint: error.config?.baseURL
          ? `${error.config.baseURL}${error.config.url ?? endpointPath}`
          : endpointPath,
        method: error.config?.method ?? "put",
        requestPayload: payload,
        status: error.response?.status,
        responseData: error.response?.data,
      });
    }
    throw toApiFormError(error, "Failed to update position");
  }
}

export async function deletePosition(id: string | number): Promise<void> {
  const numericId = toNumericRouteId(id);
  const endpointPath = `/Positions/${numericId}`;
  console.log("DELETE endpoint:", endpointPath);

  try {
    const response = await api.delete<ApiResponse<unknown>>(endpointPath);
    const responsePayload = response.data;

    if (responsePayload?.success === false) {
      throw new ApiFormError(responsePayload.message || "Failed to delete position");
    }
  } catch (error) {
    if (axios.isAxiosError(error)) {
      console.error("deletePosition failed:", {
        endpoint: endpointPath,
        status: error.response?.status,
        responseData: error.response?.data,
      });
    }
    throw toApiFormError(error, "Failed to delete position");
  }
}
