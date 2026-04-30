/** Display strings for audit log actor (aligned with API user fields). */

export type AuditLogActorFields = {
  userId: number | null;
  userFullName: string | null;
  userEmail: string | null;
};

export type AuditLogActorDisplay = {
  primary: string;
  secondary: string | null;
};

export function getAuditLogActorDisplay(log: AuditLogActorFields): AuditLogActorDisplay {
  const fullName = log.userFullName?.trim() || null;
  const email = log.userEmail?.trim() || null;

  if (log.userId == null) {
    return { primary: "System", secondary: null };
  }

  if (fullName && email) {
    const secondary = fullName.toLowerCase() !== email.toLowerCase() ? email : null;
    return { primary: fullName, secondary };
  }

  if (fullName) return { primary: fullName, secondary: null };
  if (email) return { primary: email, secondary: null };

  return { primary: "Unknown User", secondary: null };
}

export function auditLogActorSearchText(log: AuditLogActorFields): string {
  const { primary, secondary } = getAuditLogActorDisplay(log);
  const idPart = log.userId != null ? String(log.userId) : "";
  return [primary, secondary, idPart, log.userFullName ?? "", log.userEmail ?? ""].filter(Boolean).join(" ");
}
