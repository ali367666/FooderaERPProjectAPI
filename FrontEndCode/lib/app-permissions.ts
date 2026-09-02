/**
 * Mirrors `Domain.Constants.AppPermissions` — keep values identical for JWT claim checks.
 */
export const AppPermissions = {
  MenuItemView: "MenuItem.View",
  MenuItemCreate: "MenuItem.Create",
  MenuItemUpdate: "MenuItem.Update",
  MenuItemDelete: "MenuItem.Delete",
  MenuItemRecipeView: "MenuItemRecipe.View",
  MenuCategoryView: "MenuCategory.View",
  MenuCategoryCreate: "MenuCategory.Create",
  MenuCategoryUpdate: "MenuCategory.Update",
  MenuCategoryDelete: "MenuCategory.Delete",
  OrdersView: "Orders.View",
  OrdersCreate: "Orders.Create",
  OrdersAdd: "Orders.Add",
  OrdersUpdate: "Orders.Update",
  OrdersServe: "Orders.Serve",
  StockRequestView: "StockRequest.View",
  StockRequestCreate: "StockRequest.Create",
  StockRequestUpdate: "StockRequest.Update",
  StockRequestDelete: "StockRequest.Delete",
  StockRequestSubmit: "StockRequest.Submit",
  StockRequestApprove: "StockRequest.Approve",
  StockRequestReject: "StockRequest.Reject",
  StockRequestRecall: "StockRequest.Recall",
  KitchenView: "Kitchen.View",
  KitchenStartPreparing: "Kitchen.StartPreparing",
  KitchenMarkReady: "Kitchen.MarkReady",
  KitchenMarkServed: "Kitchen.MarkServed",
  OrdersPay: "Orders.Pay",
  RoleView: "Role.View",
  UserRoleManage: "UserRole.Manage",
} as const;

export const PERMISSIONS = {
  OrdersView: "Orders.View",
  OrdersCreate: "Orders.Create",
  OrdersAdd: "Orders.Add",
  OrdersUpdate: "Orders.Update",
  OrdersPay: "Orders.Pay",
} as const;

export type AppPermissionValue = (typeof AppPermissions)[keyof typeof AppPermissions];
