import type { StoreExpense } from "@/lib/store/store-service";

const esc = (s: string) => s.replace(/&/g, "&amp;").replace(/</g, "&lt;").replace(/>/g, "&gt;");
const money = (n: number) => n.toLocaleString("az-AZ", { minimumFractionDigits: 2, maximumFractionDigits: 2 });

/** Prints an expense document (digər xərclər) on an A5 sheet: number, date, the lines (till, to whom, item, amount) and the signature lines. */
export function printStoreExpense(doc: StoreExpense, company?: string | null): boolean {
  const when = new Date(doc.documentDate);
  const w = window.open("", "_blank", "width=860,height=680");
  if (!w) return false;

  const rows = doc.lines
    .map(
      (l, i) => `<tr>
  <td>${i + 1}</td><td>${esc(l.registerName)}</td><td>${l.payee ? esc(l.payee) : "—"}</td><td>${esc(l.expenseItemName)}</td>
  <td class="r">${money(l.amount)}</td><td>${l.note ? esc(l.note) : ""}</td></tr>`,
    )
    .join("");

  w.document.write(`<!doctype html><html><head><meta charset="utf-8"><title>Xərc sənədi ${esc(doc.documentNo)}</title>
<style>
  @page { size: A5 landscape; margin: 10mm; }
  * { box-sizing: border-box; }
  body { font-family: Arial, Helvetica, sans-serif; font-size: 12px; color: #111; margin: 0; }
  .head { display: flex; justify-content: space-between; align-items: flex-start; border-bottom: 2px solid #111; padding-bottom: 8px; }
  h1 { font-size: 19px; margin: 0; }
  .sub { color: #555; margin-top: 2px; }
  .no { text-align: right; }
  .no b { font-size: 15px; font-family: monospace; }
  table { width: 100%; border-collapse: collapse; margin-top: 12px; }
  th, td { padding: 5px 6px; border: 1px solid #bbb; text-align: left; vertical-align: top; }
  th { background: #f1f1f1; font-weight: 600; }
  td.r, th.r { text-align: right; }
  tfoot td { font-weight: bold; }
  .rev { margin-top: 8px; color: #b00; font-weight: bold; }
  .sign { display: flex; justify-content: space-between; margin-top: 34px; }
  .sign div { width: 44%; border-top: 1px solid #111; padding-top: 4px; color: #555; text-align: center; }
</style></head><body>
<div class="head">
  <div>
    <h1>Digər xərclər</h1>
    <div class="sub">Kassadan ödənilən xərclər${company ? ` · ${esc(company)}` : ""}</div>
  </div>
  <div class="no">№ <b>${esc(doc.documentNo)}</b><br>${when.toLocaleDateString("az-AZ")} ${when.toLocaleTimeString("az-AZ", { hour: "2-digit", minute: "2-digit" })}${doc.authorName ? `<br>Müəllif: ${esc(doc.authorName)}` : ""}</div>
</div>
<table>
  <thead><tr><th>N</th><th>Kassa</th><th>Kimə</th><th>Xərc maddəsi</th><th class="r">Məbləğ</th><th>Qeyd</th></tr></thead>
  <tbody>${rows}</tbody>
  <tfoot><tr><td colspan="4">Cəmi</td><td class="r">${money(doc.total)}</td><td></td></tr></tfoot>
</table>
${doc.note ? `<p>Qeyd: ${esc(doc.note)}</p>` : ""}
${doc.isCancelled ? `<div class="rev">LƏĞV EDİLİB${doc.cancelReason ? `: ${esc(doc.cancelReason)}` : ""}</div>` : ""}
<div class="sign"><div>Verdi (kassir)</div><div>Təsdiq etdi</div></div>
<script>window.onload = function () { window.focus(); window.print(); };</script>
</body></html>`);
  w.document.close();
  return true;
}
