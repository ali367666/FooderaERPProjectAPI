"use client";

import { OrderListPage } from "@/components/orders/order-list-page";
import { hasPermission, usePermissionSet } from "@/hooks/use-auth-permissions";

export default function Page() {
  const permissions = usePermissionSet();
  const canViewOrders = hasPermission("Permissions.Orders.View", permissions);
  if (!canViewOrders) {
    return <div className="text-sm text-muted-foreground">You do not have permission to perform this action.</div>;
  }
  return <OrderListPage />;
}
