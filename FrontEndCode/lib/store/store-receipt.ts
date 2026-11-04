import JsBarcode from "jsbarcode";
import { PAYMENT_LABELS, type StoreSale, type StoreShiftReport } from "@/lib/store/store-service";

const money = (n: number) => n.toFixed(2);

/** The company's receipt rules (paper width, header and footer lines); set once by the store layout. */
let receiptConfig = { widthMm: 80, header: null as string | null, footer: null as string | null };

export function configureReceipt(settings: { receiptWidthMm: number; receiptHeader: string | null; receiptFooter: string | null }) {
  receiptConfig = { widthMm: settings.receiptWidthMm === 58 ? 58 : 80, header: settings.receiptHeader, footer: settings.receiptFooter };
}

function escapeHtml(s: string): string {
  return s.replace(/&/g, "&amp;").replace(/</g, "&lt;").replace(/>/g, "&gt;");
}

/** The receipt number as a CODE128 barcode (SVG markup): scanning it on the Qaytarma screen finds the sale again. */
function receiptBarcodeSvg(value: string): string {
  try {
    const svg = document.createElementNS("http://www.w3.org/2000/svg", "svg");
    JsBarcode(svg, value, { format: "CODE128", height: 44, width: 1.6, displayValue: true, fontSize: 12, margin: 0 });
    return `<div class="c">${svg.outerHTML}</div>`;
  } catch {
    return "";
  }
}

function openPrintWindow(title: string, body: string): boolean {
  const w = window.open("", "_blank", "width=420,height=640");
  if (!w) return false;
  w.document.write(`<!doctype html><html><head><meta charset="utf-8" /><title>${escapeHtml(title)}</title>
<style>
  @page { margin: 4mm; }
  body { font-family: monospace; font-size: 12px; width: ${receiptConfig.widthMm - 8}mm; margin: 0 auto; }
  h1 { font-size: 15px; text-align: center; margin: 4px 0; }
  .c { text-align: center; }
  .row { display: flex; justify-content: space-between; gap: 8px; }
  .sub { color: #444; font-size: 11px; }
  hr { border: 0; border-top: 1px dashed #000; margin: 6px 0; }
  .b { font-weight: bold; }
</style></head><body>${body}</body></html>`);
  w.document.close();
  w.focus();
  w.print();
  w.close();
  return true;
}

/** Prints the sale as a receipt; a copy printed later is marked DUBLİKAT. Returns false when the browser blocked the print window. */
export function printStoreSale(sale: StoreSale, companyName?: string | null, options?: { duplicate?: boolean }): boolean {
  const lines = sale.lines
    .map(
      (l) => `<div>${escapeHtml(l.productName)}</div>
<div class="row sub"><span>${l.quantity} × ${money(l.unitPrice)}${l.discountAmount > 0 ? ` − ${money(l.discountAmount)}` : ""}</span><span>${money(l.lineTotal)}</span></div>${l.serialNumbers.length > 0 ? `<div class="sub">IMEI: ${escapeHtml(l.serialNumbers.join(", "))}</div>` : ""}`,
    )
    .join("");
  const payments = sale.payments
    .map((p) => `<div class="row"><span>${PAYMENT_LABELS[p.method] ?? p.method}</span><span>${money(p.amount)}</span></div>`)
    .join("");

  return openPrintWindow(
    sale.documentNo,
    `<h1>${escapeHtml(companyName ?? sale.branchName ?? "Mağaza")}</h1>
${receiptConfig.header ? `<div class="c sub">${escapeHtml(receiptConfig.header)}</div>` : ""}
${options?.duplicate ? '<div class="c b">DUBLİKAT</div>' : ""}
<div class="c sub">${escapeHtml(sale.branchName ?? "")}${sale.registerName ? ` — ${escapeHtml(sale.registerName)}` : ""}</div>
<hr />
<div class="row"><span>Çek №</span><span>${escapeHtml(sale.documentNo)}</span></div>
<div class="row"><span>Tarix</span><span>${new Date(sale.createdAtUtc).toLocaleString("az-AZ")}</span></div>
<div class="row"><span>Kassir</span><span>${escapeHtml(sale.cashierName ?? "—")}</span></div>
${sale.customerName ? `<div class="row"><span>Müştəri</span><span>${escapeHtml(sale.customerName)}</span></div>` : ""}
${sale.status === 2 ? '<div class="c b">LƏĞV EDİLİB</div>' : ""}
<hr />
${lines}
<hr />
${sale.discountTotal > 0 ? `<div class="row"><span>Endirim</span><span>−${money(sale.discountTotal)}</span></div>` : ""}
${sale.vatTotal > 0 ? `<div class="row"><span>ƏDV daxil</span><span>${money(sale.vatTotal)}</span></div>` : ""}
<div class="row b"><span>YEKUN</span><span>${money(sale.total)} AZN</span></div>
<hr />
${payments}
${sale.changeAmount > 0 ? `<div class="row"><span>Qaytarılan pul</span><span>${money(sale.changeAmount)}</span></div>` : ""}
${sale.bonusEarned > 0 ? `<div class="row"><span>Qazanılan bonus</span><span>${money(sale.bonusEarned)}</span></div>` : ""}
${sale.debtAmount > 0 ? `<div class="row"><span>Borca yazıldı</span><span>${money(sale.debtAmount)}</span></div>` : ""}
<hr />
${sale.status === 2 ? "" : receiptBarcodeSvg(sale.documentNo)}
<div class="c">${escapeHtml(receiptConfig.footer ?? "Təşəkkür edirik!")}</div>`,
  );
}

/** Prints an X-Report (open shift) or Z-Report (closed shift). */
export function printStoreShiftReport(report: StoreShiftReport): boolean {
  const s = report.shift;
  const payments = report.payments
    .map((p) => `<div class="row"><span>${PAYMENT_LABELS[p.method] ?? p.method}</span><span>${money(p.amount)}</span></div>`)
    .join("");

  return openPrintWindow(
    report.isFinal ? "Z-Report" : "X-Report",
    `<h1>${report.isFinal ? "Z-HESABAT" : "X-HESABAT"}</h1>
<div class="c sub">${escapeHtml(s.registerName)}</div>
<hr />
<div class="row"><span>Kassir</span><span>${escapeHtml(s.cashierName ?? "—")}</span></div>
<div class="row"><span>Açılış</span><span>${new Date(s.openedAtUtc).toLocaleString("az-AZ")}</span></div>
${s.closedAtUtc ? `<div class="row"><span>Bağlanış</span><span>${new Date(s.closedAtUtc).toLocaleString("az-AZ")}</span></div>` : ""}
<hr />
<div class="row"><span>Satış sayı</span><span>${report.salesCount}</span></div>
<div class="row"><span>Ümumi satış</span><span>${money(report.grossSales)}</span></div>
<div class="row"><span>Endirim</span><span>${money(report.discounts)}</span></div>
<div class="row b"><span>Xalis satış</span><span>${money(report.netSales)}</span></div>
<div class="row"><span>ƏDV</span><span>${money(report.vat)}</span></div>
<div class="row"><span>Ləğv (${report.cancelledCount})</span><span>${money(report.cancelledTotal)}</span></div>
<div class="row"><span>Qaytarma (${report.returnsCount})</span><span>${money(report.returnsTotal)}</span></div>
<hr />
${payments}
<hr />
<div class="row"><span>İlkin nağd</span><span>${money(s.openingCash)}</span></div>
<div class="row"><span>Mədaxil</span><span>${money(report.cashIn)}</span></div>
<div class="row"><span>Məxaric</span><span>${money(report.cashOut)}</span></div>
<div class="row"><span>Nağd qaytarma</span><span>${money(report.cashRefunds)}</span></div>
<div class="row b"><span>Gözlənilən nağd</span><span>${money(report.expectedCash)}</span></div>
${s.actualCash != null ? `<div class="row"><span>Faktiki nağd</span><span>${money(s.actualCash)}</span></div>
<div class="row b"><span>Fərq</span><span>${money(s.cashDifference ?? 0)}</span></div>` : ""}`,
  );
}
