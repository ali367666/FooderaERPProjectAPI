import { PAYMENT_LABELS, UNIT_LABELS, type StoreSale } from "@/lib/store/store-service";

const esc = (s: string) => s.replace(/&/g, "&amp;").replace(/</g, "&lt;").replace(/>/g, "&gt;");
const money = (n: number) => n.toLocaleString("az-AZ", { minimumFractionDigits: 2, maximumFractionDigits: 2 });

/** Prints a wholesale sale as an invoice (qaimə) on an A4 sheet: customer, lines, totals, what was paid and what stays on credit. */
export function printWholesaleInvoice(sale: StoreSale, units: Record<number, number>, company?: string | null): boolean {
  const when = new Date(sale.createdAtUtc);
  const w = window.open("", "_blank", "width=900,height=760");
  if (!w) return false;

  const rows = sale.lines
    .map(
      (l, i) => `<tr>
  <td>${i + 1}</td><td>${esc(l.productName)}${l.barcode ? ` <span class="m">${esc(l.barcode)}</span>` : ""}</td>
  <td class="r">${l.quantity}</td><td>${esc(UNIT_LABELS[units[l.productId] ?? 1] ?? "ədəd")}</td>
  <td class="r">${money(l.unitPrice)}</td><td class="r">${l.discountAmount > 0 ? money(l.discountAmount) : "—"}</td><td class="r">${money(l.lineTotal)}</td></tr>`,
    )
    .join("");

  const paid = sale.payments
    .filter((p) => p.method !== 5)
    .map((p) => `<div>${esc(PAYMENT_LABELS[p.method] ?? String(p.method))}: <b>${money(p.amount)}</b></div>`)
    .join("");

  w.document.write(`<!doctype html><html><head><meta charset="utf-8"><title>Qaimə ${esc(sale.documentNo)}</title>
<style>
  @page { size: A4; margin: 14mm; }
  * { box-sizing: border-box; }
  body { font-family: Arial, Helvetica, sans-serif; font-size: 13px; color: #111; margin: 0; }
  .head { display: flex; justify-content: space-between; align-items: flex-start; border-bottom: 2px solid #111; padding-bottom: 10px; }
  h1 { font-size: 22px; margin: 0; }
  .sub { color: #555; margin-top: 3px; }
  .no { text-align: right; }
  .no b { font-size: 16px; font-family: monospace; }
  .who { margin-top: 12px; display: grid; grid-template-columns: 8rem 1fr; gap: 4px 10px; }
  .who span:nth-child(odd) { color: #555; }
  table { width: 100%; border-collapse: collapse; margin-top: 14px; }
  th, td { padding: 6px 7px; border: 1px solid #bbb; text-align: left; vertical-align: top; }
  th { background: #f1f1f1; font-weight: 600; }
  .r { text-align: right; }
  .m { color: #888; font-size: 11px; }
  .totals { margin-top: 12px; margin-left: auto; width: 17rem; }
  .totals div { display: flex; justify-content: space-between; padding: 3px 0; }
  .totals .big { font-size: 16px; font-weight: bold; border-top: 2px solid #111; margin-top: 4px; padding-top: 6px; }
  .debt { color: #b45309; font-weight: bold; }
  .sign { display: flex; justify-content: space-between; margin-top: 54px; }
  .sign div { width: 44%; border-top: 1px solid #111; padding-top: 4px; color: #555; text-align: center; }
</style></head><body>
<div class="head">
  <div><h1>Qaimə (topdan satış)</h1><div class="sub">${company ? esc(company) : ""}</div></div>
  <div class="no">№ <b>${esc(sale.documentNo)}</b><br>${when.toLocaleDateString("az-AZ")} ${when.toLocaleTimeString("az-AZ", { hour: "2-digit", minute: "2-digit" })}</div>
</div>
<div class="who">
  <span>Alıcı</span><span>${sale.customerName ? esc(sale.customerName) : "Nağd alıcı"}</span>
  ${sale.registerName ? `<span>Kassa</span><span>${esc(sale.registerName)}</span>` : ""}
  ${sale.cashierName ? `<span>Müəllif</span><span>${esc(sale.cashierName)}</span>` : ""}
</div>
<table>
  <thead><tr><th>N</th><th>Məhsul</th><th class="r">Miqdar</th><th>Vahid</th><th class="r">Qiymət</th><th class="r">Endirim</th><th class="r">Məbləğ</th></tr></thead>
  <tbody>${rows}</tbody>
</table>
<div class="totals">
  ${sale.discountTotal > 0 ? `<div><span>Endirim</span><span>${money(sale.discountTotal)}</span></div>` : ""}
  <div class="big"><span>Cəmi</span><span>${money(sale.total)} AZN</span></div>
  ${paid ? `<div style="display:block;margin-top:6px">${paid}</div>` : ""}
  ${sale.debtAmount > 0 ? `<div class="debt"><span>Borca</span><span>${money(sale.debtAmount)}</span></div>` : ""}
</div>
${sale.note ? `<p>Qeyd: ${esc(sale.note)}</p>` : ""}
<div class="sign"><div>Təhvil verdi</div><div>Təhvil aldı</div></div>
<script>window.onload = function () { window.focus(); window.print(); };</script>
</body></html>`);
  w.document.close();
  return true;
}
