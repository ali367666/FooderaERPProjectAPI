/** Downloads rows as a CSV file Excel opens directly (";" separated, UTF-8 with BOM). */
export function downloadCsv(filename: string, rows: (string | number | null | undefined)[][]) {
  const cell = (v: string | number | null | undefined) => {
    let s = v == null ? "" : String(v);
    // a text cell starting with = + - @ would run as a formula in Excel
    if (typeof v === "string" && /^[=+\-@]/.test(s) && Number.isNaN(Number(s))) s = `'${s}`;
    return /[;"\n]/.test(s) ? `"${s.replace(/"/g, '""')}"` : s;
  };
  const text = rows.map((r) => r.map(cell).join(";")).join("\r\n");
  const blob = new Blob(["﻿" + text], { type: "text/csv;charset=utf-8" });
  const url = URL.createObjectURL(blob);
  const a = document.createElement("a");
  a.href = url;
  a.download = filename;
  a.click();
  URL.revokeObjectURL(url);
}
