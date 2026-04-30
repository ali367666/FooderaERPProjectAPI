/**
 * Mirrors `Domain.Constants.AppPermissions` — keep values identical for JWT claim checks.
 */
export const AppPermissions = {
  MenuItemView: "Permissions.MenuItem.View",
  MenuItemCreate: "Permissions.MenuItem.Create",
  MenuItemUpdate: "Permissions.MenuItem.Update",
  MenuItemDelete: "Permissions.MenuItem.Delete",
  MenuItemRecipeView: "Permissions.MenuItemRecipe.View",
  MenuCategoryView: "Permissions.MenuCategory.View",
  MenuCategoryCreate: "Permissions.MenuCategory.Create",
  MenuCategoryUpdate: "Permissions.MenuCategory.Update",
  MenuCategoryDelete: "Permissions.MenuCategory.Delete",
  OrdersView: "Permissions.Orders.View",
  OrdersCreate: "Permissions.Orders.Create",
  OrdersAdd: "Permissions.Orders.Add",
  OrdersUpdate: "Permissions.Orders.Update",
  OrdersDelete: "Permissions.Orders.Delete",
  OrdersServe: "Permissions.Orders.Serve",
  StockRequestView: "Permissions.StockRequest.View",
  StockRequestCreate: "Permissions.StockRequest.Create",
  StockRequestUpdate: "Permissions.StockRequest.Update",
  StockRequestDelete: "Permissions.StockRequest.Delete",
  StockRequestSubmit: "Permissions.StockRequest.Submit",
  StockRequestApprove: "Permissions.StockRequest.Approve",
  StockRequestReject: "Permissions.StockRequest.Reject",
  StockRequestRecall: "Permissions.StockRequest.Recall",
  KitchenView: "Permissions.Kitchen.View",
  KitchenStartPreparing: "Permissions.Kitchen.StartPreparing",
  KitchenMarkReady: "Permissions.Kitchen.MarkReady",
  KitchenMarkServed: "Permissions.Kitchen.MarkServed",
  OrdersPay: "Permissions.Orders.Pay",
  RoleView: "Permissions.Role.View",
  UserRoleManage: "Permissions.UserRole.Manage",
} as const;

export const PERMISSIONS = {
  OrdersView: "Permissions.Orders.View",
  OrdersCreate: "Permissions.Orders.Create",
  OrdersAdd: "Permissions.Orders.Add",
  OrdersUpdate: "Permissions.Orders.Update",
  OrdersDelete: "Permissions.Orders.Delete",
  OrdersPay: "Permissions.Orders.Pay",
} as const;

export type AppPermissionValue = (typeof AppPermissions)[keyof typeof AppPermissions];
