import { api } from "@/lib/api";
import { toApiFormError } from "@/lib/api-error";

/** Matches Domain.Enums.CashMovementType */
export const CashMovementType = {
  Deposit: 1,
  Withdrawal: 2,
} as const;

export type CashMovementTypeValue = (typeof CashMovementType)[keyof typeof CashMovementType];

export type CashMovement = {
  id: number;
  restaurantId: number;
  type: CashMovementTypeValue;
  amount: number;
  reason: string | null;
  createdByUserId: number | null;
  createdByUserName: string | null;
  createdAtUtc: string;
};

function pick<T>(o: Record<string, unknown>, camel: string, pascal: string): T | undefined {
  if (o[camel] !== undefined) return o[camel] as T;
  if (o[pascal] !== undefined) return o[pascal] as T;
  return undefined;
}

function normalizeType(raw: unknown): CashMovementTypeValue {
  const n = Number(raw);
  return n === CashMovementType.Withdrawal ? CashMovementType.Withdrawal : CashMovementType.Deposit;
}

function normalize(item: unknown): CashMovement | null {
  if (!item || typeof item !== "object") return null;
  const raw = item as Record<string, unknown>;
  const id = Number(pick(raw, "id", "Id"));
  if (!Number.isFinite(id) || id <= 0) return null;
  return {
    id,
    restaurantId: Number(pick(raw, "restaurantId", "RestaurantId") ?? 0),
    type: normalizeType(pick(raw, "type", "Type")),
    amount: Number(pick(raw, "amount", "Amount") ?? 0),
    reason: (pick(raw, "reason", "Reason") as string | null | undefined) ?? null,
    createdByUserId: (() => {
      const v = pick(raw, "createdByUserId", "CreatedByUserId");
      return v == null ? null : Number(v);
    })(),
    createdByUserName: (pick(raw, "createdByUserName", "CreatedByUserName") as string | null | undefined) ?? null,
    createdAtUtc: String(pick(raw, "createdAtUtc", "CreatedAtUtc") ?? ""),
  };
}

export async function getCashMovements(restaurantId: number, from: Date, to: Date): Promise<CashMovement[]> {
  try {
    const response = await api.get<unknown>("/CashMovements", {
      params: { restaurantId, from: from.toISOString(), to: to.toISOString() },
    });
    const list = Array.isArray(response.data) ? response.data : [];
    return list.map(normalize).filter((x): x is CashMovement => x !== null);
  } catch (error) {
    throw toApiFormError(error, "Failed to fetch cash movements");
  }
}

export async function createCashMovement(payload: {
  restaurantId: number;
  type: CashMovementTypeValue;
  amount: number;
  reason?: string | null;
}): Promise<CashMovement> {
  try {
    const response = await api.post<unknown>("/CashMovements", payload);
    const item = normalize(response.data);
    if (!item) throw new Error("Invalid response from server.");
    return item;
  } catch (error) {
    throw toApiFormError(error, "Failed to create cash movement");
  }
}
