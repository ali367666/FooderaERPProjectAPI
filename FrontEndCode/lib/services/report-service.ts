import { api } from "@/lib/api";
import { toApiFormError } from "@/lib/api-error";

/** Backend `ReportPeriod`; a business day runs 06:00 → 06:00 next morning. */
export enum ReportPeriod {
  Today = 1,
  ThisWeek = 2,
  ThisMonth = 3,
  Custom = 4,
}

export type BusinessReportSections = {
  productSales: boolean;
  productSalesByCategory: boolean;
  counterpartyDetails: boolean;
  expenses: boolean;
  openChecks: boolean;
  gifts: boolean;
  cancellations: boolean;
  tableSales: boolean;
  stockOperations: boolean;
  stockBalance: boolean;
};

export type BusinessReportRequest = {
  period: ReportPeriod;
  /** yyyy-MM-dd — only for a custom period. */
  from?: string;
  to?: string;
  restaurantId?: number | null;
} & BusinessReportSections;

export type BusinessReportProductLine = {
  menuItemId: number;
  name: string;
  quantity: number;
  revenue: number;
  percent: number;
};

export type BusinessReportCategoryGroup = {
  categoryId: number;
  categoryName: string;
  quantity: number;
  revenue: number;
  percent: number;
  products: BusinessReportProductLine[];
};

export type BusinessReportCounterpartyLine = {
  counterpartyId: number;
  name: string;
  creditSales: number;
  debtAdded: number;
  paid: number;
  currentDebt: number;
};

export type BusinessReportExpenseLine = {
  id: number;
  createdAt: string;
  amount: number;
  reason: string | null;
  createdBy: string | null;
};

export type BusinessReportOpenCheck = {
  orderId: number;
  orderNumber: string;
  tableId: number;
  tableName: string;
  waiterName: string;
  guestCount: number | null;
  openedAt: string;
  amount: number;
};

export type BusinessReportGift = {
  orderId: number;
  orderNumber: string;
  receiptNumber: string | null;
  tableName: string;
  menuItemName: string;
  quantity: number;
  value: number;
  giftedAt: string | null;
  giftedBy: string;
};

export type BusinessReportCancellation = {
  orderId: number;
  cancelledAt: string;
  tableName: string;
  orderNumber: string;
  isWholeOrder: boolean;
  menuItemName: string | null;
  quantity: number;
  amount: number;
  reason: string;
  note: string | null;
  beforeKitchen: boolean;
  cancelledBy: string;
};

export type BusinessReportTableLine = {
  tableId: number;
  tableName: string;
  orderCount: number;
  revenue: number;
};

export type BusinessReportStockOperationLine = {
  stockItemId: number;
  stockItemName: string;
  unit: string;
  received: number;
  transferIn: number;
  transferOut: number;
  consumed: number;
  returned: number;
};

export type BusinessReportStockBalanceLine = {
  warehouseId: number;
  warehouseName: string;
  stockItemId: number;
  stockItemName: string;
  unit: string;
  quantity: number;
};

export type BusinessReport = {
  period: string;
  fromLocal: string;
  toLocal: string;
  fromUtc: string;
  toUtc: string;
  restaurantId: number | null;
  summary: {
    checkCount: number;
    cancelledCount: number;
    cancelledAmount: number;
    openCheckCount: number;
    openCheckAmount: number;
    giftCount: number;
    giftAmount: number;
    expenses: number;
    counterpartyCharges: number;
    currentIncomeCash: number;
    currentIncomeBank: number;
  };
  sales: {
    counter: number;
    table: number;
    package: number;
    discount: number;
    gift: number;
    total: number;
  };
  income: {
    cash: number;
    bank: number;
    bankBreakdown: { bankName: string; amount: number }[];
    total: number;
    cashIn: number;
    cashOut: number;
    cashReturns: number;
    cashRemaining: number;
  };
  /** Each list is null when its section was not requested. */
  productSales: BusinessReportProductLine[] | null;
  productSalesByCategory: BusinessReportCategoryGroup[] | null;
  counterpartyDetails: BusinessReportCounterpartyLine[] | null;
  expenses: BusinessReportExpenseLine[] | null;
  openChecks: BusinessReportOpenCheck[] | null;
  gifts: BusinessReportGift[] | null;
  cancellations: BusinessReportCancellation[] | null;
  tableSales: BusinessReportTableLine[] | null;
  stockOperations: BusinessReportStockOperationLine[] | null;
  stockBalance: BusinessReportStockBalanceLine[] | null;
};

export async function getBusinessReport(request: BusinessReportRequest): Promise<BusinessReport> {
  try {
    const { restaurantId, from, to, ...rest } = request;
    const res = await api.get<BusinessReport>("/Reports/business", {
      params: {
        ...rest,
        ...(from ? { from } : {}),
        ...(to ? { to } : {}),
        ...(restaurantId ? { restaurantId } : {}),
      },
    });
    return res.data;
  } catch (e) {
    throw toApiFormError(e, "Hesabat yüklənmədi");
  }
}
