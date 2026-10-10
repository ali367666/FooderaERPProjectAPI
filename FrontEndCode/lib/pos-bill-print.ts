import { getOrderReceipt, markBillPrinted } from "@/lib/services/order-service";
import { getPrinters, printImageToPrinter, type Printer } from "@/lib/services/printer-service";
import { getMenuCategories } from "@/lib/services/menu-category-service";
import type { CompanySettingsBranding } from "@/lib/services/company-settings-service";
import { canvasToRaster, designFromBranding, renderReceipt } from "@/lib/receipt-render";
import { escPosBarcode, receiptBarcodeValue } from "@/lib/receipt-barcode";
import { getFiscalMode } from "@/lib/pos-fiscal-mode";

/** The restaurant's receipt printer: the primary one, else the first active printer. */
export function pickReceiptPrinter(printers: Printer[]): Printer | null {
  const active = printers.filter((p) => p.isActive);
  return active.find((p) => p.isPrimary) ?? active[0] ?? null;
}

/**
 * "Hesab" — prints the customer's pre-check straight to the receipt printer, the same path as the
 * order screen's "Qəbz çap et": the bill is recorded (which locks it when the company uses bill
 * locking) but the order stays open. Returns the printer's name.
 */
export async function printBillForOrder(
  orderId: number,
  restaurantId: number,
  branding: CompanySettingsBranding | null,
): Promise<string> {
  return sendReceipt(orderId, restaurantId, branding, true);
}

/** Prints the final receipt of an order that was just paid. Returns the printer's name. */
export async function printReceiptForOrder(
  orderId: number,
  restaurantId: number,
  branding: CompanySettingsBranding | null,
): Promise<string> {
  return sendReceipt(orderId, restaurantId, branding, false);
}

async function sendReceipt(
  orderId: number,
  restaurantId: number,
  branding: CompanySettingsBranding | null,
  recordBill: boolean,
): Promise<string> {
  const [printers, categories] = await Promise.all([
    getPrinters(restaurantId),
    getMenuCategories().catch(() => []),
  ]);
  const printer = pickReceiptPrinter(printers);
  if (!printer) throw new Error("Çek çıxarmaq üçün aktiv printer tapılmadı.");

  if (recordBill) await markBillPrinted(orderId);
  // A bill not yet paid prints as fiscal or ordinary depending on the lamp; a paid one uses what was stored.
  const receipt = await getOrderReceipt(orderId, getFiscalMode());
  const categoryName = (id: number | null | undefined) =>
    id == null ? "" : categories.find((c) => c.id === id)?.name ?? "";
  const canvas = await renderReceipt(receipt, designFromBranding(branding), categoryName);
  const trailer = `\n${escPosBarcode(receiptBarcodeValue(orderId))}`;
  await printImageToPrinter(printer.id, canvasToRaster(canvas), trailer);
  return printer.name;
}
