import { api } from "@/lib/api";
import { readBaseResponseData, readBaseResponseList } from "@/lib/api-base-response";
import { toApiFormError } from "@/lib/api-error";

/** Matches Domain.Enums.DeliveryProvider */
export const DeliveryProvider = {
  Wolt: 1,
  Bolt: 2,
  Delivery189: 3,
} as const;

export type DeliveryProviderValue = (typeof DeliveryProvider)[keyof typeof DeliveryProvider];

export type DeliveryIntegration = {
  id: number;
  restaurantId: number;
  name: string;
  provider: DeliveryProviderValue;
  externalVenueId: string | null;
  apiKey: string | null;
  webhookSecret: string | null;
  isActive: boolean;
};

function pick<T>(o: Record<string, unknown>, camel: string, pascal: string): T | undefined {
  if (o[camel] !== undefined) return o[camel] as T;
  if (o[pascal] !== undefined) return o[pascal] as T;
  return undefined;
}

function unwrapList<T>(body: unknown): T[] {
  const list = readBaseResponseList<T>(body);
  if (list.length > 0) return list;
  if (Array.isArray(body)) return body as T[];
  return [];
}

function unwrapData<T>(body: unknown): T | null {
  const data = readBaseResponseData<T>(body);
  if (data != null) return data;
  if (body && typeof body === "object" && !("success" in (body as Record<string, unknown>))) {
    return body as T;
  }
  return null;
}

function normalizeProvider(raw: unknown): DeliveryProviderValue {
  const n = Number(raw);
  if (n === DeliveryProvider.Wolt || n === DeliveryProvider.Bolt || n === DeliveryProvider.Delivery189) {
    return n;
  }
  return DeliveryProvider.Wolt;
}

function normalize(item: unknown): DeliveryIntegration | null {
  if (!item || typeof item !== "object") return null;
  const raw = item as Record<string, unknown>;
  const id = Number(pick(raw, "id", "Id"));
  if (!Number.isFinite(id) || id <= 0) return null;
  return {
    id,
    restaurantId: Number(pick(raw, "restaurantId", "RestaurantId") ?? 0),
    name: String(pick(raw, "name", "Name") ?? ""),
    provider: normalizeProvider(pick(raw, "provider", "Provider")),
    externalVenueId: (pick(raw, "externalVenueId", "ExternalVenueId") as string | null | undefined) ?? null,
    apiKey: (pick(raw, "apiKey", "ApiKey") as string | null | undefined) ?? null,
    webhookSecret: (pick(raw, "webhookSecret", "WebhookSecret") as string | null | undefined) ?? null,
    isActive: Boolean(pick(raw, "isActive", "IsActive") ?? true),
  };
}

export type DeliveryIntegrationInput = {
  restaurantId: number;
  name: string;
  provider: DeliveryProviderValue;
  externalVenueId: string | null;
  apiKey: string | null;
  webhookSecret: string | null;
  isActive: boolean;
};

export async function getDeliveryIntegrations(restaurantId: number): Promise<DeliveryIntegration[]> {
  try {
    const response = await api.get<unknown>("/DeliveryIntegrations", { params: { restaurantId } });
    return unwrapList<unknown>(response.data).map(normalize).filter((x): x is DeliveryIntegration => x !== null);
  } catch (error) {
    throw toApiFormError(error, "Failed to fetch delivery integrations");
  }
}

export async function createDeliveryIntegration(payload: DeliveryIntegrationInput): Promise<DeliveryIntegration> {
  try {
    const response = await api.post<unknown>("/DeliveryIntegrations", payload);
    const integration = normalize(unwrapData<unknown>(response.data));
    if (!integration) throw new Error("Invalid response from server.");
    return integration;
  } catch (error) {
    throw toApiFormError(error, "Failed to create delivery integration");
  }
}

export async function updateDeliveryIntegration(id: number, payload: DeliveryIntegrationInput): Promise<DeliveryIntegration> {
  try {
    const response = await api.put<unknown>("/DeliveryIntegrations", { id, ...payload });
    const integration = normalize(unwrapData<unknown>(response.data));
    if (!integration) throw new Error("Invalid response from server.");
    return integration;
  } catch (error) {
    throw toApiFormError(error, "Failed to update delivery integration");
  }
}

export async function deleteDeliveryIntegration(id: number): Promise<void> {
  try {
    await api.delete<unknown>(`/DeliveryIntegrations/${id}`);
  } catch (error) {
    throw toApiFormError(error, "Failed to delete delivery integration");
  }
}

export function deliveryProviderLabel(value: DeliveryProviderValue): string {
  switch (value) {
    case DeliveryProvider.Wolt:
      return "Wolt";
    case DeliveryProvider.Bolt:
      return "Bolt";
    case DeliveryProvider.Delivery189:
      return "189 Delivery";
    default:
      return "Digər";
  }
}

/** Platformaya "webhook address" kimi veriləcək tam URL. */
export function deliveryWebhookUrl(apiBaseUrl: string, provider: DeliveryProviderValue, integrationId: number): string {
  const providerSlug = provider === DeliveryProvider.Wolt ? "wolt" : provider === DeliveryProvider.Bolt ? "bolt" : "189";
  return `${apiBaseUrl}/delivery-webhooks/${providerSlug}/${integrationId}`;
}
