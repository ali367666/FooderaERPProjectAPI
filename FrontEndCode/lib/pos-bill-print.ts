import { getOrderReceipt, markBillPrinted } from "@/lib/services/order-service";
import { getPrinters, printImageToPrinter, type Printer } from "@/lib/services/printer-service";
import { getMenuCategories } from "@/lib/services/menu-category-service";
import type { CompanySettingsBranding } from "@/lib/services/company-settings-service";
import { canvasToRaster, designFromBranding, renderReceipt } from "@/lib/receipt-render";
import { escPosBarcode, receiptBarcodeValue } from "@/lib/receipt-barcode";

/** The restaurant's receipt printer: the primary one, else the first active printer. */
export function pickReceiptPrinter(printers: Printer[]): Printer | null {
  const active = printers.filter((p) => p.isActive);
  return active.find((p) => p.isPrimary) ?? active[0] ?? null;
}

/**
 * Prints the customer's bill ("çek") for an order straight to the receipt printer — the same
 * path as the order screen's "Qəbz çap et": the bill is recorded (which locks it when the
 * company uses bill locking), the receipt is drawn as an image and sent to the printer.
 * Returns the printer's name.
 */
export async function printBillForOrder(
  orderId: number,
  restaurantId: number,
  branding: CompanySettingsBranding | null,
): Promise<string> {
  const [printers, categories] = await Promise.all([
    getPrinters(restaurantId),
    getMenuCategories().catch(() => []),
  ]);
  const printer = pickReceiptPrinter(printers);
  if (!printer) throw new Error("Çek çıxarmaq üçün aktiv printer tapılmadı.");

  await markBillPrinted(orderId);
  const receipt = await getOrderReceipt(orderId);
  const categoryName = (id: number | null | undefined) =>
    id == null ? "" : categories.find((c) => c.id === id)?.name ?? "";
  const canvas = await renderReceipt(receipt, designFromBranding(branding), categoryName);
  const trailer = `\n${escPosBarcode(receiptBarcodeValue(orderId))}`;
  await printImageToPrinter(printer.id, canvasToRaster(canvas), trailer);
  return printer.name;
}
