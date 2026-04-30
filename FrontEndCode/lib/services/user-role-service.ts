import { api } from "@/lib/api";
import { assertApiSuccess, readBaseResponseList } from "@/lib/api-base-response";
import { toApiFormError } from "@/lib/api-error";

export type UserRoleRow = {
  userId: number;
  userFullName: string;
  userName: string | null;
  email: string | null;
  roleId: number;
  roleName: string;
};

function normalizeRow(item: unknown): UserRoleRow | null {
  if (!item || typeof item !== "object") return null;
  const o = item as Record<string, unknown>;
  return {
    userId: Number(o.userId ?? o.UserId ?? 0),
    userFullName: String(o.userFullName ?? o.UserFullName ?? ""),
    userName: (o.userName ?? o.UserName ?? null) as string | null,
    email: (o.email ?? o.Email ?? null) as string | null,
    roleId: Number(o.roleId ?? o.RoleId ?? 0),
    roleName: String(o.roleName ?? o.RoleName ?? ""),
  };
}

export async function getUserRoleMappings(): Promise<UserRoleRow[]> {
  try {
    const response = await api.get<unknown>("/UserRoles");
    assertApiSuccess(response.data);
    return readBaseResponseList<unknown>(response.data)
      .map((r) => normalizeRow(r))
      .filter((r): r is UserRoleRow => r !== null && r.userId > 0 && r.roleId > 0);
  } catch (e) {
    throw toApiFormError(e, "Failed to load user roles.");
  }
}

export async function assignUserRole(userId: number, roleId: number): Promise<void> {
  try {
    const response = await api.post<unknown>("/UserRoles/assign", { userId, roleId });
    assertApiSuccess(response.data);
  } catch (e) {
    throw toApiFormError(e, "Could not assign role.");
  }
}

export async function removeUserRole(userId: number, roleId: number): Promise<void> {
  try {
    const response = await api.post<unknown>("/UserRoles/remove", { userId, roleId });
    assertApiSuccess(response.data);
  } catch (e) {
    throw toApiFormError(e, "Could not remove role.");
  }
}
