import axios from "axios";
import { api } from "@/lib/api";
import { toApiFormError } from "@/lib/api-error";
import { findInOfflineCatalog, searchOfflineCatalog } from "@/lib/store/offline-catalog";

/** Store (mağaza) mode API: its own catalog, stock, tills, shifts and sales under /store. */

export const UnitOfMeasure = { Piece: 1, Kg: 2, Gram: 3, Liter: 4, Ml: 5 } as const;
export type UnitOfMeasureValue = (typeof UnitOfMeasure)[keyof typeof UnitOfMeasure];

export const UNIT_LABELS: Record<number, string> = { 1: "ədəd", 2: "kq", 3: "qram", 4: "litr", 5: "ml" };

export const StorePaymentMethod = { Cash: 1, Card: 2, Bonus: 3, GiftCard: 4, Debt: 5, ReturnCredit: 6, BankTransfer: 7 } as const;
export type StorePaymentMethodValue = (typeof StorePaymentMethod)[keyof typeof StorePaymentMethod];
export const PAYMENT_LABELS: Record<number, string> = {
  7: "Bank köçürməsi",
  1: "Nağd",
  2: "Kart",
  3: "Bonus",
  4: "Hədiyyə kartı",
  5: "Borc",
  6: "Dəyişdirmə krediti",
};

export const StoreSaleStatus = { Completed: 1, Cancelled: 2 } as const;
export const StoreShiftStatus = { Open: 1, Closed: 2 } as const;
export const StoreCashMovementType = { In: 1, Out: 2 } as const;

export const StoreStockMovementType = {
  AdjustmentIncrease: 7,
  AdjustmentDecrease: 8,
  Waste: 9,
  InternalUse: 10,
} as const;
export type AdjustTypeValue = (typeof StoreStockMovementType)[keyof typeof StoreStockMovementType];

export const MOVEMENT_LABELS: Record<number, string> = {
  1: "Alış",
  2: "Satış",
  3: "Satış qaytarması",
  4: "Alış qaytarması",
  5: "Transfer çıxış",
  6: "Transfer giriş",
  7: "Stok artırma",
  8: "Stok azaltma",
  9: "Zay",
  10: "Daxili istifadə",
  11: "Satış ləğvi",
};

export type StoreSettings = {
  allowNegativeStock: boolean;
  maxDiscountPercent: number;
  bonusEarnPercent: number;
  bonusMaxPaymentPercent: number;
  bonusExpiryDays: number;
  fiscalEnabled: boolean;
  receiptWidthMm: number;
  receiptHeader: string | null;
  receiptFooter: string | null;
  allowReceiptlessSale: boolean;
  quickSlotCount: number;
};

export type StoreCategory = { id: number; name: string; parentCategoryId: number | null; isActive: boolean; earnsBonus: boolean };

export type StoreProduct = {
  /** Set when a weighed-goods label was scanned: the weight/amount in the product's unit. */
  scannedQuantity?: number | null;
  /** What the till charges now: the campaign price while it runs, otherwise the sale price. */
  effectivePrice: number;
  campaignPrice: number | null;
  campaignFromUtc: string | null;
  campaignToUtc: string | null;
  id: number;
  name: string;
  sku: string | null;
  primaryBarcode: string | null;
  barcodes: string[];
  categoryId: number | null;
  categoryName: string | null;
  brand: string | null;
  unit: UnitOfMeasureValue;
  purchasePrice: number;
  salePrice: number;
  wholesalePrice: number | null;
  vatPercent: number | null;
  averageCost: number;
  minimumStock: number;
  tracksSerial: boolean;
  earnsBonus: boolean;
  imageUrl: string | null;
  isActive: boolean;
  stock: number | null;
  /** Supplier of the latest confirmed purchase (product list only). */
  supplierName?: string | null;
};

export type SaveStoreProductPayload = {
  name: string;
  sku: string | null;
  barcodes: string[];
  categoryId: number | null;
  brand: string | null;
  unit: UnitOfMeasureValue;
  purchasePrice: number;
  salePrice: number;
  wholesalePrice: number | null;
  campaignPrice: number | null;
  campaignFromUtc: string | null;
  campaignToUtc: string | null;
  vatPercent: number | null;
  minimumStock: number;
  tracksSerial: boolean;
  earnsBonus: boolean;
  imageUrl: string | null;
  isActive: boolean;
};

export type StoreStockBalance = {
  warehouseId: number;
  warehouseName: string;
  productId: number;
  productName: string;
  sku: string | null;
  primaryBarcode: string | null;
  unit: UnitOfMeasureValue;
  quantity: number;
  minimumStock: number;
  isLow: boolean;
  averageCost: number;
  value: number;
};

export type StoreStockMovement = {
  id: number;
  createdAtUtc: string;
  createdByUserName: string | null;
  warehouseId: number;
  warehouseName: string;
  productId: number;
  productName: string;
  type: number;
  typeName: string;
  quantity: number;
  balanceBefore: number;
  balanceAfter: number;
  unitCost: number;
  documentNo: string | null;
  reason: string | null;
};

export type StoreCashRegister = {
  id: number;
  restaurantId: number;
  restaurantName: string;
  warehouseId: number;
  warehouseName: string;
  name: string;
  isActive: boolean;
  openShiftId: number | null;
  openShiftCashierName: string | null;
};

export type StoreShift = {
  id: number;
  storeCashRegisterId: number;
  registerName: string;
  restaurantId: number;
  cashierUserId: number;
  cashierName: string | null;
  status: number;
  openedAtUtc: string;
  openingCash: number;
  closedAtUtc: string | null;
  expectedCash: number | null;
  actualCash: number | null;
  cashDifference: number | null;
};

export type StoreShiftReport = {
  shift: StoreShift;
  isFinal: boolean;
  salesCount: number;
  grossSales: number;
  discounts: number;
  netSales: number;
  vat: number;
  cancelledCount: number;
  cancelledTotal: number;
  payments: { method: number; amount: number }[];
  cashIn: number;
  cashOut: number;
  customerPaymentsCash: number;
  customerPaymentsCard: number;
  returnsCount: number;
  returnsTotal: number;
  cashRefunds: number;
  cardRefunds: number;
  cashMovements: {
    id: number;
    createdAtUtc: string;
    type: number;
    amount: number;
    reason: string;
    createdByUserName: string | null;
  }[];
  expectedCash: number;
};

/** A manager's credentials, sent when the cashier lacks the permission for a step. */
export type StoreApproval = { code: string; reason?: string | null };

export const StoreReturnCondition = { Sellable: 1, Waste: 2, Inspect: 3 } as const;
export type StoreReturnConditionValue = (typeof StoreReturnCondition)[keyof typeof StoreReturnCondition];
export const RETURN_CONDITION_LABELS: Record<number, string> = {
  1: "Satışa yararlı",
  2: "Yararsız (zay)",
  3: "Yoxlanılmalıdır",
};

export const StoreSerialStatus = { InStock: 1, Sold: 2, WrittenOff: 3, Inspection: 4 } as const;
export const SERIAL_STATUS_LABELS: Record<number, string> = { 1: "Stokda", 2: "Satılıb", 3: "Silinib", 4: "Yoxlamada", 5: "Yolda" };
export const SERIAL_EVENT_LABELS: Record<number, string> = {
  1: "Qəbul",
  2: "Satış",
  3: "Qaytarma",
  4: "Silinmə",
  5: "Transfer çıxış",
  6: "Transfer giriş",
  7: "Servis",
};

export type StoreQuickSlot = {
  slotNumber: number;
  productId: number | null;
  productName: string | null;
  salePrice: number | null;
  unit: UnitOfMeasureValue | null;
  label: string | null;
  color: string | null;
};

export type HeldCartLine = { productId: number; quantity: number; discountAmount: number; serialNumbers?: string[] | null };

export type StoreHeldSale = {
  id: number;
  title: string | null;
  createdAtUtc: string;
  itemCount: number;
  total: number;
  cashierUserId: number;
  cashierName: string | null;
  lines: HeldCartLine[];
};

export type StoreSerialUnit = {
  id: number;
  serialNumber: string;
  productId: number;
  productName: string;
  warehouseId: number | null;
  warehouseName: string | null;
  status: number;
  soldAtUtc: string | null;
  history: { type: number; createdAtUtc: string; documentType: string | null; documentNo: string | null; note: string | null }[];
};

export type StoreReturnableLine = {
  saleLineId: number;
  productId: number;
  productName: string;
  tracksSerial: boolean;
  soldQuantity: number;
  returnedQuantity: number;
  returnableQuantity: number;
  unitAmount: number;
  soldSerials: string[];
};

export type StoreSaleForReturn = { sale: StoreSale; lines: StoreReturnableLine[] };

export type CreateStoreReturnPayload = {
  operationId: string;
  storeSaleId: number;
  reason: string;
  refundMethod: StorePaymentMethodValue;
  lines: { saleLineId: number; quantity: number; condition: StoreReturnConditionValue; serialNumbers?: string[] | null }[];
  approval?: StoreApproval | null;
};

export type StoreReturn = {
  id: number;
  documentNo: string;
  createdAtUtc: string;
  storeSaleId: number;
  saleDocumentNo: string;
  registerName: string | null;
  cashierName: string | null;
  reason: string;
  totalAmount: number;
  refundAmount: number;
  refundMethod: number;
  debtCreditAmount: number;
  bonusRefundAmount: number;
  bonusClawbackAmount: number;
  exchangeSaleId: number | null;
  lines: { productId: number; productName: string; quantity: number; amount: number; condition: number }[];
};

export type StoreSale = {
  id: number;
  documentNo: string;
  operationId: string;
  status: number;
  createdAtUtc: string;
  restaurantId: number;
  branchName: string | null;
  storeCashRegisterId: number;
  registerName: string | null;
  storeCashShiftId: number;
  cashierName: string | null;
  customerId: number | null;
  customerName: string | null;
  isReceiptless: boolean;
  fiscalStatus: number;
  fiscalReceiptNo: string | null;
  fiscalError: string | null;
  bonusEarned: number;
  bonusSpent: number;
  debtAmount: number;
  subTotal: number;
  discountTotal: number;
  vatTotal: number;
  total: number;
  paidTotal: number;
  changeAmount: number;
  note: string | null;
  cancelledAtUtc: string | null;
  cancelReason: string | null;
  lines: {
    lineId: number;
    productId: number;
    productName: string;
    barcode: string | null;
    quantity: number;
    unitPrice: number;
    listPrice: number;
    priceChangeReason: string | null;
    serialNumbers: string[];
    discountAmount: number;
    vatPercent: number;
    vatAmount: number;
    lineTotal: number;
  }[];
  payments: { method: number; amount: number; reference: string | null }[];
};

export type StoreSaleListItem = {
  id: number;
  documentNo: string;
  status: number;
  createdAtUtc: string;
  cashierName: string | null;
  registerName: string | null;
  total: number;
  lineCount: number;
};

export type CreateStoreSalePayload = {
  operationId: string;
  receiptless?: boolean;
  storeCashRegisterId: number;
  customerId?: number | null;
  lines: {
    productId: number;
    quantity: number;
    discountAmount: number;
    overridePrice?: number | null;
    priceChangeReason?: string | null;
    serialNumbers?: string[] | null;
  }[];
  payments: { method: StorePaymentMethodValue; amount: number; reference?: string | null }[];
  note?: string | null;
  approval?: StoreApproval | null;
};

async function call<T>(fn: () => Promise<{ data: T }>, fallback: string): Promise<T> {
  try {
    return (await fn()).data;
  } catch (error) {
    throw toApiFormError(error, fallback);
  }
}

// ---- Catalog --------------------------------------------------------------------------------

export const getStoreCategories = () =>
  call(() => api.get<StoreCategory[]>("/store/categories"), "Kateqoriyalar yüklənmədi");

export const saveStoreCategory = (id: number | null, body: { name: string; parentCategoryId: number | null; isActive: boolean; earnsBonus?: boolean }) =>
  call(
    () => (id ? api.put<StoreCategory>(`/store/categories/${id}`, body) : api.post<StoreCategory>("/store/categories", body)),
    "Kateqoriya yazılmadı",
  );

export const searchStoreProducts = (params: {
  q?: string;
  categoryId?: number | null;
  isActive?: boolean;
  page?: number;
  pageSize?: number;
  warehouseId?: number | null;
}) =>
  call(
    () =>
      api.get<{ total: number; items: StoreProduct[] }>("/store/products", {
        params: { ...params, categoryId: params.categoryId || undefined, warehouseId: params.warehouseId || undefined },
      }),
    "Məhsullar yüklənmədi",
  );

/** True when the request got no answer at all (no network / server down), as opposed to being refused. */
const unreachable = (error: unknown) => axios.isAxiosError(error) && !error.response;

export const posSearchProducts = async (params: { q?: string; categoryId?: number | null; warehouseId?: number | null; limit?: number }) => {
  try {
    return (
      await api.get<{ total: number; items: StoreProduct[] }>("/store/products/pos-search", {
        params: { ...params, categoryId: params.categoryId || undefined, warehouseId: params.warehouseId || undefined },
      })
    ).data;
  } catch (error) {
    // Offline: the till's own copy of the catalog answers.
    if (unreachable(error)) {
      const items = await searchOfflineCatalog(params.q, params.categoryId);
      if (items.length > 0) return { total: items.length, items };
    }
    throw toApiFormError(error, "Məhsullar yüklənmədi");
  }
};

export const findStoreProductByCode = async (code: string, warehouseId?: number | null) => {
  try {
    return (
      await api.get<StoreProduct>(`/store/products/by-code/${encodeURIComponent(code)}`, {
        params: { warehouseId: warehouseId || undefined },
      })
    ).data;
  } catch (error) {
    if (unreachable(error)) {
      const product = await findInOfflineCatalog(code);
      if (product) return product;
    }
    throw toApiFormError(error, "Məhsul tapılmadı");
  }
};

export const setStoreProductsBonus = (body: { productIds?: number[] | null; term?: string; categoryId?: number | null; earnsBonus: boolean }) =>
  call(() => api.post<{ changed: number }>("/store/products/bonus", body), "Bonus dəyişdirilmədi");

export const saveStoreProduct = (id: number | null, body: SaveStoreProductPayload) =>
  call(
    () => (id ? api.put<StoreProduct>(`/store/products/${id}`, body) : api.post<StoreProduct>("/store/products", body)),
    "Məhsul yazılmadı",
  );

// ---- Stock ----------------------------------------------------------------------------------

export const getStoreStock = (params: { warehouseId?: number | null; q?: string; lowOnly?: boolean }) =>
  call(
    () => api.get<StoreStockBalance[]>("/store/stock", { params: { ...params, warehouseId: params.warehouseId || undefined } }),
    "Stok yüklənmədi",
  );

export const getStoreMovements = (params: { warehouseId?: number | null; productId?: number | null; from?: string; to?: string }) =>
  call(
    () =>
      api.get<StoreStockMovement[]>("/store/stock/movements", {
        params: {
          warehouseId: params.warehouseId || undefined,
          productId: params.productId || undefined,
          from: params.from || undefined,
          to: params.to || undefined,
        },
      }),
    "Stok hərəkətləri yüklənmədi",
  );

export const adjustStoreStock = (body: {
  warehouseId: number;
  productId: number;
  type: AdjustTypeValue;
  quantity: number;
  unitCost: number | null;
  reason: string;
  serialNumbers?: string[] | null;
}) => call(() => api.post<StoreStockBalance>("/store/stock/adjust", body), "Stok düzəlişi yazılmadı");

export const getStoreSettings = () =>
  call(() => api.get<StoreSettings>("/store/settings"), "Parametrlər yüklənmədi");

export const updateStoreSettings = (body: StoreSettings) =>
  call(() => api.put<StoreSettings>("/store/settings", body), "Parametrlər yazılmadı");

// ---- Tills and shifts -----------------------------------------------------------------------

export const getStoreRegisters = (restaurantId?: number | null) =>
  call(
    () => api.get<StoreCashRegister[]>("/store/registers", { params: { restaurantId: restaurantId || undefined } }),
    "Kassalar yüklənmədi",
  );

export const saveStoreRegister = (
  id: number | null,
  body: { restaurantId: number; warehouseId: number; name: string; isActive: boolean },
) =>
  call(
    () => (id ? api.put<StoreCashRegister>(`/store/registers/${id}`, body) : api.post<StoreCashRegister>("/store/registers", body)),
    "Kassa yazılmadı",
  );

/** The current user's open shift, or null (the API answers 204). */
export async function getMyStoreShift(): Promise<StoreShift | null> {
  try {
    const res = await api.get<StoreShift | "">("/store/shifts/mine");
    return res.status === 204 || !res.data ? null : (res.data as StoreShift);
  } catch (error) {
    throw toApiFormError(error, "Növbə yüklənmədi");
  }
}

export const openStoreShift = (storeCashRegisterId: number, openingCash: number) =>
  call(() => api.post<StoreShift>("/store/shifts/open", { storeCashRegisterId, openingCash }), "Növbə açılmadı");

export const getStoreShifts = (params: { registerId?: number | null; from?: string; to?: string }) =>
  call(
    () =>
      api.get<StoreShift[]>("/store/shifts", {
        params: { registerId: params.registerId || undefined, from: params.from || undefined, to: params.to || undefined },
      }),
    "Növbələr yüklənmədi",
  );

export const getStoreShiftReport = (id: number) =>
  call(() => api.get<StoreShiftReport>(`/store/shifts/${id}/report`), "Hesabat yüklənmədi");

export const closeStoreShift = (id: number, actualCash: number, note?: string | null) =>
  call(() => api.post<StoreShiftReport>(`/store/shifts/${id}/close`, { actualCash, note: note ?? null }), "Növbə bağlanmadı");

export const addStoreCashMovement = (id: number, body: { type: number; amount: number; reason: string; approval?: StoreApproval | null }) =>
  call(() => api.post<StoreShiftReport>(`/store/shifts/${id}/cash-movements`, body), "Əməliyyat yazılmadı");

// ---- Sales ----------------------------------------------------------------------------------

/** Thrown when a sale could not reach the server at all (no connection), so the caller may keep it for later. */
export class StoreOfflineError extends Error {
  constructor() {
    super("Serverlə bağlantı yoxdur.");
    this.name = "StoreOfflineError";
  }
}

export const createStoreSale = async (payload: CreateStoreSalePayload) => {
  try {
    return (await api.post<StoreSale>("/store/sales", payload)).data;
  } catch (error) {
    if (axios.isAxiosError(error) && !error.response) throw new StoreOfflineError();
    throw toApiFormError(error, "Satış tamamlanmadı");
  }
};

export const searchStoreSales = (params: {
  from?: string;
  to?: string;
  shiftId?: number | null;
  status?: number | null;
  documentNo?: string;
  page?: number;
  pageSize?: number;
}) =>
  call(
    () =>
      api.get<{ total: number; items: StoreSaleListItem[] }>("/store/sales", {
        params: {
          ...params,
          shiftId: params.shiftId || undefined,
          status: params.status || undefined,
          from: params.from || undefined,
          to: params.to || undefined,
          documentNo: params.documentNo || undefined,
        },
      }),
    "Satışlar yüklənmədi",
  );

export const getStoreSale = (id: number) => call(() => api.get<StoreSale>(`/store/sales/${id}`), "Satış yüklənmədi");

export const cancelStoreSale = (id: number, reason: string, approval?: StoreApproval | null) =>
  call(() => api.post<StoreSale>(`/store/sales/${id}/cancel`, { reason, approval: approval ?? null }), "Satış ləğv edilmədi");

// ---- Quick slots and held sales -------------------------------------------------------------

export const getStoreQuickSlots = (registerId: number) =>
  call(() => api.get<StoreQuickSlot[]>(`/store/registers/${registerId}/quick-slots`), "Sürətli düymələr yüklənmədi");

export const saveStoreQuickSlot = (
  registerId: number,
  body: { slotNumber: number; productId: number | null; label?: string | null; color?: string | null },
) => call(() => api.put<StoreQuickSlot[]>(`/store/registers/${registerId}/quick-slots`, body), "Düymə yazılmadı");

export const getStoreHeldSales = (registerId: number) =>
  call(() => api.get<StoreHeldSale[]>(`/store/registers/${registerId}/held-sales`), "Saxlanılan satışlar yüklənmədi");

export const holdStoreSale = (body: { storeCashRegisterId: number; title?: string | null; lines: HeldCartLine[] }) =>
  call(() => api.post<StoreHeldSale>("/store/held-sales", body), "Satış saxlanmadı");

export const resumeStoreHeldSale = (id: number) =>
  call(() => api.post<StoreHeldSale>(`/store/held-sales/${id}/resume`), "Saxlanılan satış açılmadı");

export const discardStoreHeldSale = (id: number) =>
  call(() => api.delete(`/store/held-sales/${id}`), "Saxlanılan satış silinmədi");

export const getStoreProduct = (id: number, warehouseId?: number | null) =>
  call(() => api.get<StoreProduct>(`/store/products/${id}`, { params: { warehouseId: warehouseId || undefined } }), "Məhsul yüklənmədi");

// ---- Serial / IMEI --------------------------------------------------------------------------

export const searchStoreSerials = (params: { q?: string; productId?: number | null; warehouseId?: number | null; status?: number | null }) =>
  call(
    () =>
      api.get<StoreSerialUnit[]>("/store/serials", {
        params: {
          q: params.q || undefined,
          productId: params.productId || undefined,
          warehouseId: params.warehouseId || undefined,
          status: params.status || undefined,
        },
      }),
    "Serial nömrələr yüklənmədi",
  );

export const getStoreSerial = (id: number) =>
  call(() => api.get<StoreSerialUnit>(`/store/serials/${id}`), "Serial tarixçə yüklənmədi");

// ---- Returns and exchange -------------------------------------------------------------------

export const findStoreSaleForReturn = (documentNo: string) =>
  call(() => api.get<StoreSaleForReturn>("/store/returns/find-sale", { params: { documentNo } }), "Çek tapılmadı");

export const createStoreReturn = (payload: CreateStoreReturnPayload) =>
  call(() => api.post<StoreReturn>("/store/returns", payload), "Qaytarma yazılmadı");

export const createStoreExchange = (payload: { return: CreateStoreReturnPayload; newSale: CreateStoreSalePayload }) =>
  call(() => api.post<{ return: StoreReturn; sale: StoreSale }>("/store/returns/exchange", payload), "Dəyişdirmə yazılmadı");

export const searchStoreReturns = (params: { from?: string; to?: string }) =>
  call(
    () => api.get<StoreReturn[]>("/store/returns", { params: { from: params.from || undefined, to: params.to || undefined } }),
    "Qaytarmalar yüklənmədi",
  );

// ---- Suppliers, purchases, ledger -----------------------------------------------------------

export const StorePurchaseStatus = { Draft: 0, Confirmed: 1, Cancelled: 2 } as const;
export const PURCHASE_STATUS_LABELS: Record<number, string> = { 0: "Qaralama", 1: "Təsdiqlənib", 2: "Ləğv edilib" };

export const LEDGER_TYPE_LABELS: Record<number, string> = {
  1: "Başlanğıc qalıq",
  2: "Alış",
  3: "Alış qaytarması",
  4: "Ödəniş",
  5: "Düzəliş",
  6: "Alış ləğvi",
  7: "Borca satış",
  8: "Müştəri ödənişi",
  9: "Satış qaytarması",
};

export type StoreSupplier = {
  id: number;
  name: string;
  legalName: string | null;
  phone: string | null;
  email: string | null;
  address: string | null;
  taxNumber: string | null;
  note: string | null;
  paymentTermDays: number;
  isActive: boolean;
  /** What the store owes; negative means an advance paid to the supplier. */
  balance: number;
};

export type SaveStoreSupplierPayload = {
  name: string;
  legalName: string | null;
  phone: string | null;
  email: string | null;
  address: string | null;
  taxNumber: string | null;
  note: string | null;
  paymentTermDays: number;
  isActive: boolean;
  openingBalance?: number | null;
};

export type StoreLedgerEntry = {
  id: number;
  entryDate: string;
  type: number;
  typeName: string;
  amount: number;
  runningBalance: number;
  documentType: string | null;
  documentId: number | null;
  documentNo: string | null;
  note: string | null;
};

export type StoreSupplierPayment = {
  id: number;
  documentNo: string;
  paymentDate: string;
  amount: number;
  method: number;
  reference: string | null;
  note: string | null;
  allocated: number;
  advance: number;
  isReversed: boolean;
  reverseReason: string | null;
  allocations: { purchaseId: number; purchaseDocumentNo: string; amount: number }[];
};

export type StoreSupplierStatement = {
  supplier: StoreSupplier;
  openingBalance: number;
  totalIncrease: number;
  totalDecrease: number;
  closingBalance: number;
  entries: StoreLedgerEntry[];
  payments: StoreSupplierPayment[];
};

export type StoreOpenPurchase = {
  purchaseId: number;
  documentNo: string;
  supplierInvoiceNo: string | null;
  purchaseDate: string;
  dueDate: string;
  total: number;
  paid: number;
  returned: number;
  outstanding: number;
  daysOverdue: number;
};

export type StoreSupplierDebt = {
  supplierId: number;
  supplierName: string;
  balance: number;
  advance: number;
  notDue: number;
  overdue1To30: number;
  overdue31To60: number;
  overdue61To90: number;
  overdue90Plus: number;
  totalOverdue: number;
  lastPaymentDate: string | null;
  openPurchases: StoreOpenPurchase[];
};

export type StoreSupplierDebtReport = {
  totalOwed: number;
  totalOverdue: number;
  totalAdvance: number;
  suppliers: StoreSupplierDebt[];
};

export type SaveStorePurchasePayload = {
  supplierId: number;
  warehouseId: number;
  supplierInvoiceNo: string | null;
  purchaseDate: string | null;
  dueDate: string | null;
  note: string | null;
  lines: {
    productId: number;
    quantity: number;
    unitPrice: number;
    discountPercent: number;
    vatPercent?: number | null;
    serialNumbers?: string[] | null;
  }[];
};

export type StorePurchase = {
  id: number;
  documentNo: string;
  supplierId: number;
  supplierName: string;
  warehouseId: number;
  warehouseName: string;
  supplierInvoiceNo: string | null;
  purchaseDate: string;
  dueDate: string | null;
  status: number;
  note: string | null;
  total: number;
  vatTotal: number;
  paid: number;
  returned: number;
  outstanding: number;
  createdAtUtc: string;
  createdByName: string | null;
  confirmedAtUtc: string | null;
  confirmedByName: string | null;
  cancelledAtUtc: string | null;
  cancelReason: string | null;
  lines: {
    id: number;
    productId: number;
    productName: string;
    tracksSerial: boolean;
    quantity: number;
    unitPrice: number;
    discountPercent: number;
    vatPercent: number;
    lineTotal: number;
    netUnitCost: number;
    returnedQuantity: number;
    returnableQuantity: number;
    serialNumbers: string[];
  }[];
  returns: {
    id: number;
    documentNo: string;
    createdAtUtc: string;
    reason: string;
    total: number;
    lines: { productName: string; quantity: number; amount: number }[];
  }[];
};

export type StorePurchaseListItem = {
  id: number;
  documentNo: string;
  supplierName: string;
  supplierInvoiceNo: string | null;
  purchaseDate: string;
  status: number;
  total: number;
  paid: number;
  outstanding: number;
};

export type StorePriceHistoryEntry = {
  priceType: number;
  priceTypeName: string;
  oldPrice: number | null;
  newPrice: number;
  effectiveFromUtc: string;
  changedByName: string | null;
  reason: string | null;
};

export const getStoreSuppliers = (includeInactive = false) =>
  call(() => api.get<StoreSupplier[]>("/store/suppliers", { params: { includeInactive } }), "Təchizatçılar yüklənmədi");

export const saveStoreSupplier = (id: number | null, body: SaveStoreSupplierPayload) =>
  call(
    () => (id ? api.put<StoreSupplier>(`/store/suppliers/${id}`, body) : api.post<StoreSupplier>("/store/suppliers", body)),
    "Təchizatçı yazılmadı",
  );

export const getStoreSupplierStatement = (id: number, params: { from?: string; to?: string }) =>
  call(
    () => api.get<StoreSupplierStatement>(`/store/suppliers/${id}/statement`, { params: { from: params.from || undefined, to: params.to || undefined } }),
    "Hesab çıxarışı yüklənmədi",
  );

export const getStoreSupplierDebts = () =>
  call(() => api.get<StoreSupplierDebtReport>("/store/suppliers/debts"), "Borc hesabatı yüklənmədi");

export const payStoreSupplier = (body: {
  supplierId: number;
  amount: number;
  method: number;
  reference?: string | null;
  note?: string | null;
  allocations?: { purchaseId: number; amount: number }[] | null;
}) => call(() => api.post<StoreSupplierPayment>("/store/supplier-payments", body), "Ödəniş yazılmadı");

export const searchStorePurchases = (params: {
  supplierId?: number | null;
  status?: number | null;
  from?: string;
  to?: string;
  q?: string;
  page?: number;
  pageSize?: number;
}) =>
  call(
    () =>
      api.get<{ total: number; items: StorePurchaseListItem[] }>("/store/purchases", {
        params: {
          ...params,
          supplierId: params.supplierId || undefined,
          status: params.status == null ? undefined : params.status,
          from: params.from || undefined,
          to: params.to || undefined,
          q: params.q || undefined,
        },
      }),
    "Alışlar yüklənmədi",
  );

export const getStorePurchase = (id: number) => call(() => api.get<StorePurchase>(`/store/purchases/${id}`), "Alış yüklənmədi");

export const saveStorePurchase = (id: number | null, body: SaveStorePurchasePayload) =>
  call(
    () => (id ? api.put<StorePurchase>(`/store/purchases/${id}`, body) : api.post<StorePurchase>("/store/purchases", body)),
    "Alış yazılmadı",
  );

export const confirmStorePurchase = (id: number, payment?: { amount: number; method: number; reference?: string | null } | null) =>
  call(() => api.post<StorePurchase>(`/store/purchases/${id}/confirm`, { payment: payment ?? null }), "Alış təsdiqlənmədi");

export const cancelStorePurchase = (id: number, reason: string) =>
  call(() => api.post<StorePurchase>(`/store/purchases/${id}/cancel`, { reason }), "Alış ləğv edilmədi");

export const returnStorePurchase = (
  id: number,
  body: { reason: string; lines: { purchaseLineId: number; quantity: number; serialNumbers?: string[] | null }[] },
) => call(() => api.post<StorePurchase>(`/store/purchases/${id}/return`, body), "Qaytarma yazılmadı");

export const getStorePriceHistory = (productId: number) =>
  call(() => api.get<StorePriceHistoryEntry[]>(`/store/products/${productId}/price-history`), "Qiymət tarixçəsi yüklənmədi");

// ---- Inventory (stock count) and transfers ---------------------------------------------------

export const StoreInventoryStatus = { Draft: 0, Confirmed: 1, Cancelled: 2 } as const;
export const INVENTORY_STATUS_LABELS: Record<number, string> = { 0: "Sayım gedir", 1: "Təsdiqlənib", 2: "Ləğv edilib" };

export const StoreTransferStatus = { Draft: 0, Pending: 1, Shipped: 2, PartiallyReceived: 3, Received: 4, Cancelled: 5 } as const;
export const TRANSFER_STATUS_LABELS: Record<number, string> = {
  0: "Qaralama",
  1: "Göndərişə hazır",
  2: "Yolda",
  3: "Qismən qəbul",
  4: "Qəbul edilib",
  5: "Ləğv edilib",
};

export type StoreInventoryLine = {
  id: number;
  productId: number;
  productName: string;
  barcode: string | null;
  unit: UnitOfMeasureValue;
  expectedQuantity: number;
  countedQuantity: number | null;
  difference: number;
  unitCost: number;
  differenceValue: number;
  reason: string | null;
};

export type StoreInventory = {
  id: number;
  documentNo: string;
  warehouseId: number;
  warehouseName: string;
  categoryId: number | null;
  categoryName: string | null;
  status: number;
  note: string | null;
  createdAtUtc: string;
  confirmedAtUtc: string | null;
  confirmedByName: string | null;
  lineCount: number;
  countedCount: number;
  differenceCount: number;
  surplusValue: number;
  shortageValue: number;
  lines: StoreInventoryLine[];
};

export type StoreTransferLine = {
  id: number;
  productId: number;
  productName: string;
  tracksSerial: boolean;
  quantity: number;
  shippedQuantity: number;
  receivedQuantity: number;
  shortageQuantity: number;
  inTransitQuantity: number;
  serialNumbers: string[];
  receivedSerialNumbers: string[];
};

export type StoreTransfer = {
  id: number;
  documentNo: string;
  fromWarehouseId: number;
  fromWarehouseName: string;
  toWarehouseId: number;
  toWarehouseName: string;
  status: number;
  note: string | null;
  createdAtUtc: string;
  createdByName: string | null;
  shippedAtUtc: string | null;
  shippedByName: string | null;
  receivedAtUtc: string | null;
  receivedByName: string | null;
  lineCount: number;
  inTransitValue: number;
  lines: StoreTransferLine[];
};

export const getStoreInventories = (params: { warehouseId?: number | null; status?: number | null }) =>
  call(
    () => api.get<StoreInventory[]>("/store/inventories", { params: { warehouseId: params.warehouseId || undefined, status: params.status == null ? undefined : params.status } }),
    "İnventarizasiyalar yüklənmədi",
  );

export const getStoreInventory = (id: number) => call(() => api.get<StoreInventory>(`/store/inventories/${id}`), "İnventarizasiya yüklənmədi");

export const startStoreInventory = (body: { warehouseId: number; categoryId?: number | null; note?: string | null }) =>
  call(() => api.post<StoreInventory>("/store/inventories", body), "İnventarizasiya açılmadı");

export const saveStoreInventoryCounts = (id: number, lines: { lineId: number; countedQuantity: number | null; reason?: string | null }[]) =>
  call(() => api.put<StoreInventory>(`/store/inventories/${id}/counts`, { lines }), "Say yazılmadı");

export const confirmStoreInventory = (id: number) =>
  call(() => api.post<StoreInventory>(`/store/inventories/${id}/confirm`), "İnventarizasiya təsdiqlənmədi");

export const cancelStoreInventory = (id: number) =>
  call(() => api.post<StoreInventory>(`/store/inventories/${id}/cancel`), "İnventarizasiya ləğv edilmədi");

export const getStoreTransfers = (params: { status?: number | null; warehouseId?: number | null; from?: string; to?: string }) =>
  call(
    () =>
      api.get<StoreTransfer[]>("/store/transfers", {
        params: {
          status: params.status == null ? undefined : params.status,
          warehouseId: params.warehouseId || undefined,
          from: params.from || undefined,
          to: params.to || undefined,
        },
      }),
    "Transferlər yüklənmədi",
  );

export const getStoreTransfer = (id: number) => call(() => api.get<StoreTransfer>(`/store/transfers/${id}`), "Transfer yüklənmədi");

export const saveStoreTransfer = (
  id: number | null,
  body: {
    fromWarehouseId: number;
    toWarehouseId: number;
    note?: string | null;
    lines: { productId: number; quantity: number; serialNumbers?: string[] | null }[];
  },
) =>
  call(
    () => (id ? api.put<StoreTransfer>(`/store/transfers/${id}`, body) : api.post<StoreTransfer>("/store/transfers", body)),
    "Transfer yazılmadı",
  );

export const submitStoreTransfer = (id: number) => call(() => api.post<StoreTransfer>(`/store/transfers/${id}/submit`), "Transfer təqdim edilmədi");
export const shipStoreTransfer = (id: number) => call(() => api.post<StoreTransfer>(`/store/transfers/${id}/ship`), "Transfer göndərilmədi");
export const cancelStoreTransfer = (id: number) => call(() => api.post<StoreTransfer>(`/store/transfers/${id}/cancel`), "Transfer ləğv edilmədi");

export const receiveStoreTransfer = (
  id: number,
  body: { lines: { lineId: number; quantity: number; serialNumbers?: string[] | null }[]; closeWithShortage: boolean },
) => call(() => api.post<StoreTransfer>(`/store/transfers/${id}/receive`, body), "Transfer qəbul edilmədi");

// ---- Customers, bonus and credit -------------------------------------------------------------

export type StoreCustomer = {
  id: number;
  code: string;
  name: string;
  phone: string | null;
  email: string | null;
  note: string | null;
  creditLimit: number;
  /** This customer's own bonus percent; null = the company's normal percent. */
  bonusPercentOverride: number | null;
  /** The bonus percent shown at the till (the company's normal one). */
  bonusPercentShown: number;
  isActive: boolean;
  /** What the customer owes the store. */
  debt: number;
  bonusBalance: number;
  creditAvailable: number;
};

export type StoreBonusEntry = {
  id: number;
  createdAtUtc: string;
  type: number;
  typeName: string;
  amount: number;
  runningBalance: number;
  documentNo: string | null;
  note: string | null;
};

export const BONUS_TYPE_LABELS: Record<number, string> = {
  1: "Qazanılıb",
  2: "Xərclənib",
  3: "Geri alınıb (qaytarma/ləğv)",
  4: "Geri qaytarılıb",
  5: "Düzəliş",
};

export type StoreCustomerStatement = {
  customer: StoreCustomer;
  debtEntries: StoreLedgerEntry[];
  bonusEntries: StoreBonusEntry[];
  payments: {
    id: number;
    documentNo: string;
    createdAtUtc: string;
    amount: number;
    method: number;
    reference: string | null;
    note: string | null;
    isReversed: boolean;
    reverseReason: string | null;
  }[];
};

export type SaveStoreCustomerPayload = {
  name: string;
  phone: string | null;
  email: string | null;
  note: string | null;
  creditLimit: number;
  isActive: boolean;
  code?: string | null;
  bonusPercentOverride?: number | null;
};

export const searchStoreCustomers = (q: string, includeInactive = false) =>
  call(() => api.get<StoreCustomer[]>("/store/customers", { params: { q: q || undefined, includeInactive } }), "Müştərilər yüklənmədi");

export const findStoreCustomer = (term: string) =>
  call(() => api.get<StoreCustomer>("/store/customers/find", { params: { term } }), "Müştəri tapılmadı");

export const saveStoreCustomer = (id: number | null, body: SaveStoreCustomerPayload) =>
  call(
    () => (id ? api.put<StoreCustomer>(`/store/customers/${id}`, body) : api.post<StoreCustomer>("/store/customers", body)),
    "Müştəri yazılmadı",
  );

export const getStoreCustomerStatement = (id: number, params: { from?: string; to?: string } = {}) =>
  call(
    () => api.get<StoreCustomerStatement>(`/store/customers/${id}/statement`, { params: { from: params.from || undefined, to: params.to || undefined } }),
    "Müştəri hesabı yüklənmədi",
  );

export const payStoreCustomerDebt = (body: { customerId: number; amount: number; method: number; reference?: string | null; note?: string | null }) =>
  call(() => api.post<StoreCustomerStatement>("/store/customer-payments", body), "Ödəniş yazılmadı");

export const adjustStoreBonus = (body: { customerId: number; amount: number; reason: string }) =>
  call(() => api.post<StoreCustomer>("/store/bonus/adjustments", body), "Bonus düzəlişi yazılmadı");

// ---- Reports and alerts ----------------------------------------------------------------------

export type StoreReportRow = {
  key: string;
  name: string;
  count: number;
  quantity: number;
  amount: number;
  cost: number;
  profit: number;
};

export type StoreSalesReport = {
  from: string;
  to: string;
  salesCount: number;
  grossSales: number;
  discounts: number;
  netSales: number;
  vat: number;
  costOfGoods: number;
  averageReceipt: number;
  cancelledCount: number;
  cancelledTotal: number;
  returnsCount: number;
  returnsTotal: number;
  returnedCost: number;
  netAfterReturns: number;
  profit: number;
  byDay: StoreReportRow[];
  byProduct: StoreReportRow[];
  byCategory: StoreReportRow[];
  byCashier: StoreReportRow[];
  byRegister: StoreReportRow[];
  byPayment: StoreReportRow[];
  unsold: { productId: number; productName: string; categoryName: string | null; stock: number }[];
};

export type StoreStockReport = {
  totalValue: number;
  lowStockCount: number;
  rows: StoreStockBalance[];
  belowMinimum: StoreStockBalance[];
  byWarehouse: StoreReportRow[];
  movementsByType: StoreReportRow[];
};

export type StoreCashReport = {
  totalIn: number;
  totalOut: number;
  totalDifference: number;
  movements: { id: number; createdAtUtc: string; registerName: string; type: number; amount: number; reason: string; userName: string | null }[];
  shifts: {
    shiftId: number;
    registerName: string;
    cashierName: string | null;
    openedAtUtc: string;
    closedAtUtc: string | null;
    expectedCash: number | null;
    actualCash: number | null;
    cashDifference: number | null;
  }[];
};

export type StoreCustomerDebtReport = {
  totalDebt: number;
  totalOver30: number;
  customers: {
    customerId: number;
    customerName: string;
    debt: number;
    age0To30: number;
    age31To60: number;
    age61To90: number;
    age90Plus: number;
    oldestOpenDate: string | null;
  }[];
};

export type StoreBonusReport = {
  totalEarned: number;
  totalSpent: number;
  totalReversed: number;
  customers: { customerId: number; customerName: string; earned: number; spent: number; reversed: number; refunded: number; adjusted: number; expired: number; net: number; opening: number; increase: number; decrease: number; closing: number; current: number }[];
};

export type StoreAlert = { kind: string; title: string; message: string; severity: "info" | "warning" | "danger"; link: string | null };

export const getStoreSalesReport = (params: {
  from: string;
  to: string;
  restaurantId?: number | null;
  warehouseId?: number | null;
  cashierUserId?: number | null;
  categoryId?: number | null;
  productId?: number | null;
  includeUnsold?: boolean;
}) =>
  call(
    () =>
      api.get<StoreSalesReport>("/store/reports/sales", {
        params: {
          ...params,
          restaurantId: params.restaurantId || undefined,
          warehouseId: params.warehouseId || undefined,
          cashierUserId: params.cashierUserId || undefined,
          categoryId: params.categoryId || undefined,
          productId: params.productId || undefined,
        },
      }),
    "Satış hesabatı yüklənmədi",
  );

export const getStoreStockReport = (params: { from: string; to: string; warehouseId?: number | null; categoryId?: number | null }) =>
  call(
    () => api.get<StoreStockReport>("/store/reports/stock", { params: { ...params, warehouseId: params.warehouseId || undefined, categoryId: params.categoryId || undefined } }),
    "Stok hesabatı yüklənmədi",
  );

export const getStoreCashReport = (params: { from: string; to: string }) =>
  call(() => api.get<StoreCashReport>("/store/reports/cash", { params }), "Kassa hesabatı yüklənmədi");

export const getStoreCustomerDebtReport = () =>
  call(() => api.get<StoreCustomerDebtReport>("/store/reports/customer-debts"), "Müştəri borc hesabatı yüklənmədi");

export const getStoreBonusReport = (params: { from: string; to: string }) =>
  call(() => api.get<StoreBonusReport>("/store/reports/bonus", { params }), "Bonus hesabatı yüklənmədi");

export const getStoreAlerts = () => call(() => api.get<StoreAlert[]>("/store/alerts"), "Bildirişlər yüklənmədi");

// ---- Product CSV ----------------------------------------------------------------------------

export type StoreProductImportResult = { created: number; updated: number; errors: string[] };

/** Downloads every product as a CSV file. */
export async function downloadStoreProductsCsv() {
  try {
    const res = await api.get<Blob>("/store/products/export", { responseType: "blob" });
    const url = URL.createObjectURL(res.data);
    const a = document.createElement("a");
    a.href = url;
    a.download = "mehsullar.csv";
    a.click();
    URL.revokeObjectURL(url);
  } catch (error) {
    throw toApiFormError(error, "Fayl yüklənmədi");
  }
}

export const importStoreProductsCsv = (file: File) => {
  const form = new FormData();
  form.append("file", file);
  return call(() => api.post<StoreProductImportResult>("/store/products/import", form), "İdxal alınmadı");
};

export const reopenStoreShift = (id: number, reason: string) =>
  call(() => api.post<StoreShift>(`/store/shifts/${id}/reopen`, { reason }), "Növbə yenidən açılmadı");

export const FISCAL_LABELS: Record<number, string> = { 0: "Tələb olunmur", 1: "Gözləyir", 2: "Təsdiqlənib", 3: "Xəta" };

export const retryStoreFiscal = (saleId: number) =>
  call(() => api.post<StoreSale>(`/store/sales/${saleId}/fiscal-retry`, {}), "Fiskal cihaza göndərilmədi");

export type StoreSyncOperation = {
  operationId: string;
  status: number;
  documentNo: string | null;
  entityId: number | null;
  error: string | null;
  attemptCount: number;
  processedAtUtc: string | null;
  deviceId: string | null;
  type: string | null;
  createdAtUtc: string;
};

export const SYNC_STATUS_LABELS: Record<number, string> = { 0: "Gözləyir", 1: "Göndərilir", 2: "Sinxronlaşıb", 3: "Konflikt", 4: "Xəta" };

export const getStoreSyncOperations = (params: { status?: number | null; deviceId?: string }) =>
  call(
    () => api.get<StoreSyncOperation[]>("/store/sync/operations", { params: { status: params.status ?? undefined, deviceId: params.deviceId || undefined } }),
    "Sinxronizasiya jurnalı yüklənmədi",
  );

export type StoreBranchPrice = { branchId: number; salePrice: number };

export const getStoreBranchPrices = (productId: number) =>
  call(() => api.get<StoreBranchPrice[]>(`/store/products/${productId}/branch-prices`), "Filial qiymətləri yüklənmədi");

export const saveStoreBranchPrices = (productId: number, prices: StoreBranchPrice[]) =>
  call(() => api.put<StoreBranchPrice[]>(`/store/products/${productId}/branch-prices`, { prices }), "Filial qiymətləri yazılmadı");

export const getStoreUserAccess = (userId: number) =>
  call(() => api.get<{ userId: number; branchIds: number[] }>(`/store/user-access/${userId}`), "Giriş məlumatı yüklənmədi");

export const saveStoreUserAccess = (userId: number, branchIds: number[]) =>
  call(() => api.put<{ userId: number; branchIds: number[] }>(`/store/user-access/${userId}`, { branchIds }), "Giriş yazılmadı");

export const reverseStoreCustomerPayment = (id: number, reason: string) =>
  call(() => api.post<StoreCustomerStatement>(`/store/customer-payments/${id}/reverse`, { reason }), "Ödəniş ləğv edilmədi");

export const reverseStoreSupplierPayment = (id: number, reason: string) =>
  call(() => api.post<StoreSupplierPayment>(`/store/supplier-payments/${id}/reverse`, { reason }), "Ödəniş ləğv edilmədi");

/** Asks whether the cashier may take a product out of the cart (or clear it); a manager's credentials are sent when the cashier lacks the permission. */
export const checkStoreApproval = (action: "RemoveLine" | "ClearCart", detail: string | null, approval: StoreApproval | null) =>
  call(() => api.post<{ approved: boolean; byManager: boolean }>("/store/approvals/check", { action, detail, approval }), "Təsdiq alınmadı");

// ---- Grouped sales report (1C style) ---------------------------------------------------------

export type StoreSalesMatrixGroup = "day" | "customer" | "register" | "supplier" | "product" | "document";
export type StoreSalesMatrixField = "quantity" | "turnover" | "grossTurnover" | "cost" | "profit" | "profitPercent";

export type StoreSalesMatrixNode = {
  key: string;
  name: string;
  group: StoreSalesMatrixGroup | null;
  quantity: number;
  turnover: number;
  grossTurnover: number;
  cost: number;
  profit: number;
  profitPercent: number;
  children: StoreSalesMatrixNode[];
};

export type StoreSalesMatrixRequest = {
  from: string;
  to: string;
  groupBy: StoreSalesMatrixGroup[];
  customerIds?: number[];
  registerIds?: number[];
  supplierIds?: number[];
  productIds?: number[];
  sortBy?: StoreSalesMatrixField | null;
  sortDesc?: boolean;
};

export type StoreSalesMatrixReport = { from: string; to: string; groupBy: StoreSalesMatrixGroup[]; total: StoreSalesMatrixNode };

export const getStoreSalesMatrix = (body: StoreSalesMatrixRequest) =>
  call(() => api.post<StoreSalesMatrixReport>("/store/reports/sales-matrix", body), "Satış hesabatı yüklənmədi");

// ---- Stock by warehouse (opening, in, out, closing) -------------------------------------------

export type StoreStockSummaryRow = {
  productId: number;
  productName: string;
  barcode: string | null;
  purchasePrice: number;
  salePrice: number;
  openingQty: number;
  openingValue: number;
  inQty: number;
  inValue: number;
  outQty: number;
  outValue: number;
  closingQty: number;
  closingValue: number;
  saleAmount: number;
};

export type StoreStockSummary = {
  from: string;
  to: string;
  total: StoreStockSummaryRow;
  warehouses: { warehouseId: number; warehouseName: string; total: StoreStockSummaryRow; rows: StoreStockSummaryRow[] }[];
};

export const getStoreStockSummary = (params: { from: string; to: string; warehouseId?: number | null; categoryId?: number | null }) =>
  call(
    () =>
      api.get<StoreStockSummary>("/store/reports/stock-summary", {
        params: { ...params, warehouseId: params.warehouseId || undefined, categoryId: params.categoryId || undefined },
      }),
    "Stok hesabatı yüklənmədi",
  );

// ---- Product rating --------------------------------------------------------------------------

export type StoreProductRating = {
  from: string;
  to: string;
  least: boolean;
  productCount: number;
  totalQuantity: number;
  totalAmount: number;
  rows: { rank: number; productId: number; sku: string | null; barcode: string | null; productName: string; salePrice: number; quantity: number; amount: number }[];
};

export const getStoreProductRating = (params: { from: string; to: string; least?: boolean; take?: number }) =>
  call(() => api.get<StoreProductRating>("/store/reports/product-rating", { params }), "Reytinq yüklənmədi");

// ---- Cash by till ----------------------------------------------------------------------------

export type StoreCashSummaryRow = { registerId: number; registerName: string; opening: number; income: number; outcome: number; closing: number };

export type StoreCashSummary = { from: string; to: string; total: StoreCashSummaryRow; registers: StoreCashSummaryRow[] };

export const getStoreCashSummary = (params: { from: string; to: string }) =>
  call(() => api.get<StoreCashSummary>("/store/reports/cash-summary", { params }), "Kassa hesabatı yüklənmədi");
