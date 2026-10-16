/**
 * Customer-receipt layout shared by the POS (printed text + on-screen dialog) and the live
 * preview on the settings page, so both always look the same.
 */

export type ReceiptSortMode = "order" | "name" | "category";

export type ReceiptLikeLine = {
  menuItemName: string;
  menuCategoryId?: number | null;
  quantity: number;
  lineTotal: number;
};

/** A category heading (only in "category" mode) or a product line. */
export type ReceiptRow<T extends ReceiptLikeLine> = { kind: "header"; title: string } | { kind: "line"; line: T };

/** Characters per line on a thermal printer: 58 mm → 32, 80 mm → 48. */
export function receiptCharsPerLine(paperWidth: number | null | undefined): number {
  return paperWidth === 58 ? 32 : 48;
}

export function arrangeReceiptLines<T extends ReceiptLikeLine>(
  lines: T[],
  mode: string | null | undefined,
  categoryName: (id: number | null | undefined) => string,
): ReceiptRow<T>[] {
  if (mode === "name") {
    return [...lines]
      .sort((a, b) => a.menuItemName.localeCompare(b.menuItemName, "az"))
      .map((line) => ({ kind: "line", line }));
  }

  if (mode === "category") {
    // Categories keep the order they first appear in; items stay in order-entry order inside each.
    const groups = new Map<string, T[]>();
    for (const line of lines) {
      const title = categoryName(line.menuCategoryId) || "Digər";
      const bucket = groups.get(title) ?? [];
      bucket.push(line);
      groups.set(title, bucket);
    }
    const rows: ReceiptRow<T>[] = [];
    for (const [title, bucket] of groups) {
      rows.push({ kind: "header", title });
      for (const line of bucket) rows.push({ kind: "line", line });
    }
    return rows;
  }

  return lines.map((line) => ({ kind: "line", line }));
}

/** "2 x Plov ........ 12.00 ₼" fitted to the paper width. */
export function formatReceiptTextLine(quantity: number, name: string, total: number, width: number): string {
  const amount = `${total.toFixed(2)} ₼`;
  const left = `${quantity} x ${name}`;
  const room = width - amount.length - 1;
  const fitted = left.length > room ? left.slice(0, Math.max(0, room - 1)) + "…" : left;
  return fitted.padEnd(room) + " " + amount;
}

export function centerText(text: string, width: number): string {
  if (text.length >= width) return text;
  const pad = Math.floor((width - text.length) / 2);
  return " ".repeat(pad) + text;
}
