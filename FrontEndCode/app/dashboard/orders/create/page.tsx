"use client";

import { OrderFormPage } from "@/components/orders/order-form-page";
import { hasPermission, usePermissionSet } from "@/hooks/use-auth-permissions";

export default function Page() {
  const permissions = usePermissionSet();
  const canCreate = hasPermission("Orders.Create", permissions);
  if (!canCreate) {
    return <div className="text-sm text-muted-foreground">You do not have permission to perform this action.</div>;
  }
  return <OrderFormPage variant="create" />;
}
