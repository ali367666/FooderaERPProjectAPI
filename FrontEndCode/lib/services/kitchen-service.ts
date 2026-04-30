import { api } from "@/lib/api";
import { ApiFormError } from "@/lib/api-error";
import { toApiFormError } from "@/lib/api-error";

export type KitchenLineStatus = "Pending" | "InPreparation" | "Ready" | "Served";

export type KitchenLineDto = {
  orderLineId: number;
  orderId: number;
  orderNumber: string;
  tableId: number;
  tableName: string | null;
  restaurantName: string | null;
  menuItemId: number;
  menuItemName: string;
  quantity: number;
  note: string | null;
  orderStatus: string;
  kitchenStatus: KitchenLineStatus;
  createdAt: string;
};

export type KitchenOrderGroupDto = {
  orderId: number;
  orderNumber: string;
  tableId: number;
  tableName: string | null;
  restaurantName: string | null;
  orderStatus: string;
  createdAt: string;
  lines: KitchenLineDto[];
};

function asRecord(input: unknown): Record<string, unknown> {
  return (input && typeof input === "object" ? input : {}) as Record<string, unknown>;
}

function normalizeKitchenStatus(raw: unknown): KitchenLineStatus {
  const numeric = Number(raw);
  if (Number.isFinite(numeric)) {
    if (numeric === 0 || numeric === 1) return "Pending";
    if (numeric === 2) return "InPreparation";
    if (numeric === 3) return "Ready";
    if (numeric >= 4) return "Served";
  }

  const status = String(raw ?? "").toLowerCase();
  if (status === "pending") return "Pending";
  if (status === "ready") return "Ready";
  if (status === "served") return "Served";
  if (
    status === "inprogress" ||
    status === "preparing" ||
    status === "inpreparation" ||
    status === "processing"
  ) {
    return "InPreparation";
  }
  return "Pending";
}

function normalizeLine(raw: unknown): KitchenLineDto | null {
  const o = asRecord(raw);
  const orderLineId = Number(o.orderLineId ?? o.OrderLineId);
  if (!Number.isFinite(orderLineId)) return null;
  return {
    orderLineId,
    orderId: Number(o.orderId ?? o.OrderId ?? 0),
    orderNumber: String(o.orderNumber ?? o.OrderNumber ?? ""),
    tableId: Number(o.tableId ?? o.TableId ?? 0),
    tableName: String(o.tableName ?? o.TableName ?? "") || null,
    restaurantName: String(o.restaurantName ?? o.RestaurantName ?? "") || null,
    menuItemId: Number(o.menuItemId ?? o.MenuItemId ?? 0),
    menuItemName: String(o.menuItemName ?? o.MenuItemName ?? ""),
    quantity: Number(o.quantity ?? o.Quantity ?? 0),
    note: (o.note ?? o.Note ?? null) as string | null,
    orderStatus: String(o.orderStatus ?? o.OrderStatus ?? ""),
    kitchenStatus: normalizeKitchenStatus(
      o.orderLineStatus ??
        o.OrderLineStatus ??
        o.kitchenStatus ??
        o.KitchenStatus ??
        o.status ??
        o.Status,
    ),
    createdAt: String(o.createdAt ?? o.CreatedAt ?? o.openedAt ?? o.OpenedAt ?? ""),
  };
}

export async function getKitchenOrders(restaurantId: number): Promise<KitchenOrderGroupDto[]> {
  try {
    if (!Number.isFinite(restaurantId) || restaurantId <= 0) {
      throw new ApiFormError("Please select restaurant");
    }

    const response = await api.get<unknown>(`/Kitchen/${restaurantId}`);
    const list = Array.isArray(response.data) ? response.data : [];
    const lines = list.map(normalizeLine).filter((x): x is KitchenLineDto => x !== null);
    const map = new Map<number, KitchenOrderGroupDto>();

    for (const line of lines) {
      const current = map.get(line.orderId);
      if (!current) {
        map.set(line.orderId, {
          orderId: line.orderId,
          orderNumber: line.orderNumber,
          tableId: line.tableId,
          tableName: line.tableName,
          restaurantName: line.restaurantName,
          orderStatus: line.orderStatus,
          createdAt: line.createdAt,
          lines: [line],
        });
        continue;
      }

      current.lines.push(line);
      current.tableId = line.tableId;
      current.orderStatus = line.orderStatus;
      if (line.createdAt < current.createdAt) current.createdAt = line.createdAt;
    }

    return Array.from(map.values()).sort((a, b) => a.createdAt.localeCompare(b.createdAt));
  } catch (error) {
    throw toApiFormError(error, "Failed to load kitchen orders");
  }
}

async function postLineAction(orderLineId: number, action: "start" | "ready") {
  try {
    if (!Number.isFinite(orderLineId) || orderLineId <= 0) {
      throw new ApiFormError("Invalid kitchen order line");
    }
    await api.put(`/Kitchen/${action}/${orderLineId}`);
  } catch (error) {
    throw toApiFormError(error, `Failed to ${action} for kitchen line`);
  }
}

export function startPreparingKitchenLine(orderLineId: number) {
  return postLineAction(orderLineId, "start");
}

export function markKitchenLineReady(orderLineId: number) {
  return postLineAction(orderLineId, "ready");
}
