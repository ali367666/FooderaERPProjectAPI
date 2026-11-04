import { api } from "@/lib/api";
import type { StoreProduct, StoreSettings } from "@/lib/store/store-service";

/**
 * The till's offline copy of the catalog: active products with prices, barcodes and the warehouse's stock as of the last
 * refresh, plus the store rules. Kept in IndexedDB (a catalog can be far bigger than localStorage allows). While offline the
 * scan and search fall back to it; the stock shown is only as fresh as the last refresh, and the server decides at sync time.
 */

type Snapshot = {
  savedAt: string;
  warehouseId: number | null;
  settings: StoreSettings;
  products: StoreProduct[];
};

const DB = "store-offline";
const STORE = "kv";
const KEY = "catalog";

function open(): Promise<IDBDatabase> {
  return new Promise((resolve, reject) => {
    const req = indexedDB.open(DB, 1);
    req.onupgradeneeded = () => req.result.createObjectStore(STORE);
    req.onsuccess = () => resolve(req.result);
    req.onerror = () => reject(req.error);
  });
}

async function readSnapshot(): Promise<Snapshot | null> {
  try {
    const db = await open();
    return await new Promise<Snapshot | null>((resolve, reject) => {
      const req = db.transaction(STORE).objectStore(STORE).get(KEY);
      req.onsuccess = () => resolve((req.result as Snapshot) ?? null);
      req.onerror = () => reject(req.error);
    });
  } catch {
    return null;
  }
}

async function writeSnapshot(snapshot: Snapshot): Promise<void> {
  const db = await open();
  await new Promise<void>((resolve, reject) => {
    const tx = db.transaction(STORE, "readwrite");
    tx.objectStore(STORE).put(snapshot, KEY);
    tx.oncomplete = () => resolve();
    tx.onerror = () => reject(tx.error);
  });
}

/** Downloads the catalog for the till's warehouse and keeps it. Returns the time it was taken, or null when it could not be. */
export async function refreshOfflineCatalog(warehouseId: number | null): Promise<string | null> {
  try {
    const res = await api.get<{ serverTimeUtc: string; settings: StoreSettings; products: StoreProduct[] }>("/store/sync/pull", {
      params: { warehouseId: warehouseId || undefined },
    });
    await writeSnapshot({ savedAt: res.data.serverTimeUtc, warehouseId, settings: res.data.settings, products: res.data.products });
    return res.data.serverTimeUtc;
  } catch {
    return null;
  }
}

/** When the offline catalog was last refreshed (ISO), or null when there is none. */
export async function getOfflineCatalogTime(): Promise<string | null> {
  return (await readSnapshot())?.savedAt ?? null;
}

export async function getOfflineSettings(): Promise<StoreSettings | null> {
  return (await readSnapshot())?.settings ?? null;
}

/** The product with this barcode or SKU, from the offline copy. A weighed-goods label (13 digits starting with 2) is understood too. */
export async function findInOfflineCatalog(code: string): Promise<StoreProduct | null> {
  const snap = await readSnapshot();
  if (!snap) return null;
  const c = code.trim().toLowerCase();
  const byCode = (x: string) => snap.products.find((p) => p.sku?.toLowerCase() === x || p.barcodes.some((b) => b.toLowerCase() === x));

  const direct = byCode(c);
  if (direct) return direct;

  if (/^2\d{12}$/.test(c)) {
    const item = c.slice(1, 6);
    const product = byCode(item) ?? byCode(item.replace(/^0+/, ""));
    if (product) {
      const amount = Number(c.slice(6, 11));
      const kgOrLitre = product.unit === 2 || product.unit === 4;
      return { ...product, scannedQuantity: kgOrLitre ? amount / 1000 : amount };
    }
  }
  return null;
}

/** Name (anywhere in it), SKU or barcode search in the offline copy. */
export async function searchOfflineCatalog(q: string | undefined, categoryId: number | null | undefined): Promise<StoreProduct[]> {
  const snap = await readSnapshot();
  if (!snap) return [];
  const t = (q ?? "").trim().toLowerCase();
  return snap.products
    .filter((p) => (!categoryId || p.categoryId === categoryId))
    .filter((p) => !t || p.name.toLowerCase().includes(t) || p.sku?.toLowerCase().includes(t) || p.barcodes.some((b) => b.toLowerCase().includes(t)))
    .slice(0, 2000);
}
