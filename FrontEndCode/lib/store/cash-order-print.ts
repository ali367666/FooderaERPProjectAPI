import { PAYMENT_LABELS, type StoreCashOrder } from "@/lib/store/store-service";

const esc = (s: string) => s.replace(/&/g, "&amp;").replace(/</g, "&lt;").replace(/>/g, "&gt;");
const money = (n: number) => n.toLocaleString("az-AZ", { minimumFractionDigits: 2, maximumFractionDigits: 2 });

/**
 * Prints a cash order on an A5 sheet: "Kassa mədaxil orderi" (money taken from a customer) or "Kassa məxaric orderi" (money paid to a supplier),
 * with the number, date, till, party, amount, basis and the signature lines. Returns false when the browser blocked the print window.
 */
export function printCashOrder(order: StoreCashOrder, company?: string | null): boolean {
  const isIn = order.kind === "in";
  const title = isIn ? "Kassa mədaxil orderi" : "Kassa məxaric orderi";
  const subtitle = isIn ? "Alıcıdan ödəniş" : "Təchizatçıya ödəniş";
  const partyLabel = isIn ? "Alıcı" : "Təchizatçı";
  const when = new Date(order.dateUtc);

  const w = window.open("", "_blank", "width=820,height=640");
  if (!w) return false;
  w.document.write(`<!doctype html><html><head><meta charset="utf-8"><title>${esc(title)} ${esc(order.documentNo)}</title>
<style>
  @page { size: A5 landscape; margin: 10mm; }
  * { box-sizing: border-box; }
  body { font-family: Arial, Helvetica, sans-serif; font-size: 13px; color: #111; margin: 0; }
  .head { display: flex; justify-content: space-between; align-items: flex-start; border-bottom: 2px solid #111; padding-bottom: 8px; }
  h1 { font-size: 20px; margin: 0; }
  .sub { color: #555; margin-top: 2px; }
  .no { text-align: right; }
  .no b { font-size: 16px; font-family: monospace; }
  table { width: 100%; border-collapse: collapse; margin-top: 14px; }
  td { padding: 6px 4px; border-bottom: 1px solid #ccc; vertical-align: top; }
  td.k { width: 34%; color: #555; }
  .amount { font-size: 22px; font-weight: bold; }
  .rev { margin-top: 10px; color: #b00; font-weight: bold; }
  .sign { display: flex; justify-content: space-between; margin-top: 38px; }
  .sign div { width: 44%; border-top: 1px solid #111; padding-top: 4px; color: #555; text-align: center; }
</style></head><body>
<div class="head">
  <div>
    <h1>${esc(title)}</h1>
    <div class="sub">${esc(subtitle)}${company ? ` · ${esc(company)}` : ""}</div>
  </div>
  <div class="no">№ <b>${esc(order.documentNo)}</b><br>${when.toLocaleDateString("az-AZ")} ${when.toLocaleTimeString("az-AZ", { hour: "2-digit", minute: "2-digit" })}</div>
</div>
<table>
  ${order.registerName ? `<tr><td class="k">Kassa</td><td>${esc(order.registerName)}</td></tr>` : ""}
  <tr><td class="k">${partyLabel}</td><td>${esc(order.partyName)}${order.partyCode ? ` <span style="color:#777">(${esc(order.partyCode)})</span>` : ""}</td></tr>
  <tr><td class="k">Məbləğ</td><td class="amount">${money(order.amount)} AZN</td></tr>
  <tr><td class="k">Ödəniş növü</td><td>${esc(PAYMENT_LABELS[order.method] ?? String(order.method))}</td></tr>
  <tr><td class="k">Əsas</td><td>${order.basis ? esc(order.basis) : "—"}</td></tr>
  <tr><td class="k">Qeyd</td><td>${order.note ? esc(order.note) : "—"}</td></tr>
  <tr><td class="k">Müəllif</td><td>${order.authorName ? esc(order.authorName) : "—"}</td></tr>
</table>
${order.isReversed ? `<div class="rev">LƏĞV EDİLİB${order.reverseReason ? `: ${esc(order.reverseReason)}` : ""}</div>` : ""}
<div class="sign"><div>${isIn ? "Qəbul etdi (kassir)" : "Verdi (kassir)"}</div><div>${isIn ? "Verdi (alıcı)" : "Aldı (təchizatçı)"}</div></div>
<script>window.onload = function () { window.focus(); window.print(); };</script>
</body></html>`);
  w.document.close();
  return true;
}
