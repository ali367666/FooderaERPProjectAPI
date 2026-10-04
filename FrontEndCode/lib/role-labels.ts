/** System role names as stored in the backend (see Domain/Constants/AppRoles.cs). */
export const SUPER_ADMIN_ROLE = "SuperAdmin";
export const COMPANY_SUPER_ADMIN_ROLE = "CompanySuperAdmin";

const ROLE_LABELS: Record<string, string> = {
  [SUPER_ADMIN_ROLE.toLowerCase()]: "Platforma SuperAdmin",
  [COMPANY_SUPER_ADMIN_ROLE.toLowerCase()]: "Şirkət SuperAdmin",
};

/** Human-readable label for a role name; tenant-created roles are shown as-is. */
export function roleDisplayName(name: string): string {
  return ROLE_LABELS[name.trim().toLowerCase()] ?? name;
}

/**
 * Accounts only the platform SuperAdmin may edit or delete — the backend enforces this; the UI
 * just avoids offering actions that would fail.
 */
export function isProtectedAccount(roles: string[]): boolean {
  return roles.some((r) => r.trim().toLowerCase() in ROLE_LABELS);
}
