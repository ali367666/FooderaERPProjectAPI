import axios from "axios";
import { api } from "@/lib/api";
import { toApiFormError } from "@/lib/api-error";
import type { CreateStoreSalePayload } from "@/lib/store/store-service";

/**
 * Sales made while the server cannot be reached wait here (browser storage) and are sent later.
 * Every entry keeps the sale's OperationId, so resending after a lost answer never sells twice:
 * the server returns the sale it already made.
 */

export type QueuedSaleStatus = "Pending" | "Syncing" | "Failed" | "Conflict";

export type QueuedSale = {
  operationId: string;
  payload: CreateStoreSalePayload;
  createdAt: string;
  total: number;
  status: QueuedSaleStatus;
  error: string | null;
  attempts: number;
};

const KEY = "store.offlineSales.v1";
const EVENT = "store-offline-queue";
const DEVICE_KEY = "store.deviceId.v1";

/** A stable id of this browser/till, so the server log shows which device an operation came from. */
export function getDeviceId(): string {
  try {
    let id = localStorage.getItem(DEVICE_KEY);
    if (!id) {
      id = `till-${crypto.randomUUID()}`;
      localStorage.setItem(DEVICE_KEY, id);
    }
    return id;
  } catch {
    return "till-unknown";
  }
}

/** True when the request never got an answer (no network, server down), as opposed to being refused. */
export function isNetworkError(error: unknown): boolean {
  return axios.isAxiosError(error) && !error.response;
}

function read(): QueuedSale[] {
  try {
    const raw = localStorage.getItem(KEY);
    return raw ? (JSON.parse(raw) as QueuedSale[]) : [];
  } catch {
    return [];
  }
}

function write(items: QueuedSale[]) {
  try {
    localStorage.setItem(KEY, JSON.stringify(items));
  } catch {
    // storage full or blocked: the sale stays only in memory of the caller; nothing more to do here
  }
  window.dispatchEvent(new Event(EVENT));
}

export const listQueuedSales = () => read();

export function enqueueSale(payload: CreateStoreSalePayload, total: number) {
  const items = read().filter((x) => x.operationId !== payload.operationId);
  items.push({
    operationId: payload.operationId,
    payload: { ...payload, approval: null },
    createdAt: new Date().toISOString(),
    total,
    status: "Pending",
    error: null,
    attempts: 0,
  });
  write(items);
}

export function removeQueuedSale(operationId: string) {
  write(read().filter((x) => x.operationId !== operationId));
}

/** Puts a failed or conflicting sale back to Pending so the next sync tries it again. */
export function retryQueuedSale(operationId: string) {
  write(read().map((x) => (x.operationId === operationId ? { ...x, status: "Pending", error: null } : x)));
}

export function subscribeQueue(listener: () => void) {
  window.addEventListener(EVENT, listener);
  window.addEventListener("storage", listener);
  return () => {
    window.removeEventListener(EVENT, listener);
    window.removeEventListener("storage", listener);
  };
}

let running = false;

/**
 * Sends the waiting sales one by one, oldest first. Stops at the first lost connection.
 * A refusal (400/409) marks that sale Failed / Conflict and goes on with the next one.
 * Returns how many sales were accepted.
 */
export async function syncQueuedSales(): Promise<{ synced: number; failed: number }> {
  if (running) return { synced: 0, failed: 0 };
  running = true;
  let synced = 0;
  let failed = 0;
  try {
    for (const item of read().filter((x) => x.status === "Pending" || x.status === "Syncing")) {
      const patch = (p: Partial<QueuedSale>) => write(read().map((x) => (x.operationId === item.operationId ? { ...x, ...p } : x)));
      patch({ status: "Syncing", attempts: item.attempts + 1 });
      try {
        const res = await api.post<{ operationId: string; status: number; error: string | null }[]>("/store/sync/push", {
          deviceId: getDeviceId(),
          operations: [{ operationId: item.operationId, type: "Sale", createdAtUtc: item.createdAt, payload: item.payload }],
        });
        const answer = res.data.find((r) => r.operationId === item.operationId);
        if (answer?.status === 2) {
          removeQueuedSale(item.operationId);
          synced++;
        } else {
          // Refused: it stays in the queue with the reason (never dropped silently).
          patch({ status: answer?.status === 3 ? "Conflict" : "Failed", error: answer?.error ?? "Server satışı qəbul etmədi." });
          failed++;
        }
      } catch (error) {
        if (isNetworkError(error)) {
          patch({ status: "Pending" });
          break;
        }
        const status = axios.isAxiosError(error) ? error.response?.status : undefined;
        patch({
          status: status === 409 ? "Conflict" : "Failed",
          error: toApiFormError(error, "Satış sinxronlaşmadı").message,
        });
        failed++;
      }
    }
  } finally {
    running = false;
  }
  return { synced, failed };
}
