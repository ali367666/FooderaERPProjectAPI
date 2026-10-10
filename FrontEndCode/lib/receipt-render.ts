/**
 * Customer receipt drawn as a picture.
 *
 * Thermal printers print text through single-byte code pages, and none of them contains "ə" — so
 * the receipt is drawn on a canvas with a normal font (every Azerbaijani letter works, the logo and
 * font sizes too) and sent to the printer as an ESC/POS raster image. The same canvas is the
 * on-screen preview, so what you see is what prints.
 */
import type { CompanySettingsBranding } from "@/lib/services/company-settings-service";
import type { OrderReceiptDto, OrderReceiptLineDto } from "@/lib/services/order-service";
import { arrangeReceiptLines } from "@/lib/receipt-format";

export type ReceiptDesign = {
  paperWidth: number;
  showLogo: boolean;
  logoUrl: string | null;
  /** % of the paper width. */
  logoWidth: number;
  showBusinessName: boolean;
  headerText: string | null;
  socialLinks: string | null;
  contactPhone: string | null;
  slogan: string | null;
  socialPosition: "top" | "bottom";
  /** Slogan / phone / social links (off in "simple" receipt mode unless enabled there). */
  showExtras: boolean;
  showVat: boolean;
  showOrderNumber: boolean;
  showTableName: boolean;
  showWaiterName: boolean;
  showTime: boolean;
  showPaymentMethod: boolean;
  /** Body font size, px on the printed image. */
  fontSize: number;
  nameFontSize: number;
  giftNote: string | null;
  giftNoteFontSize: number;
  footerText: string | null;
  sortMode: string;
  groupQuantities: boolean;
};

const PAYMENT_LABELS: Record<string, string> = { Cash: "Nağd", Card: "Kart", Credit: "Borc (nisyə)" };
const GIFT_SUFFIX = " (Hədiyyə)";
const FONT = '"Segoe UI", Arial, "Noto Sans", sans-serif';
const MARGIN = 8;

export function designFromBranding(b: CompanySettingsBranding | null | undefined): ReceiptDesign {
  // "Sadə çek" mode has its own set of show/hide switches.
  const simple = b?.receiptSimpleMode === true;
  const show = (normal: boolean | undefined, simpleFlag: boolean | undefined) =>
    simple ? simpleFlag === true : normal !== false;
  return {
    paperWidth: b?.receiptPaperWidth === 58 ? 58 : 80,
    showLogo: b?.receiptShowLogo !== false,
    logoUrl: b?.reportLogoUrl || b?.loginLogoUrl || null,
    logoWidth: b?.receiptLogoWidth ?? 50,
    showBusinessName: b?.receiptShowBusinessName !== false,
    headerText: b?.receiptHeaderText ?? null,
    socialLinks: b?.socialLinks ?? null,
    contactPhone: b?.contactPhoneNumber ?? null,
    slogan: b?.slogan ?? null,
    socialPosition: b?.receiptSocialPosition === "bottom" ? "bottom" : "top",
    showExtras: simple ? b?.receiptSimpleShowFooter === true : true,
    showVat: simple ? b?.receiptSimpleShowVat === true : true,
    showOrderNumber: show(b?.receiptShowOrderNumber, b?.receiptSimpleShowOrderNumber),
    showTableName: b?.receiptShowTableName !== false,
    showWaiterName: show(b?.receiptShowWaiterName, b?.receiptSimpleShowWaiterName),
    showTime: show(b?.receiptShowTime, b?.receiptSimpleShowTime),
    showPaymentMethod: show(b?.receiptShowPaymentMethod, b?.receiptSimpleShowPaymentMethod),
    fontSize: clampFont(b?.receiptFontSize, 22),
    nameFontSize: clampFont(b?.receiptRestaurantNameFontSize, 32),
    giftNote: b?.receiptGiftNote ?? null,
    giftNoteFontSize: clampFont(b?.receiptGiftNoteFontSize, 18),
    footerText: b?.receiptFooterText ?? null,
    sortMode: b?.receiptSortMode ?? "order",
    groupQuantities: b?.printGroupQuantities !== false,
  };
}

function clampFont(value: number | null | undefined, fallback: number): number {
  const v = Number(value);
  return Number.isFinite(v) && v > 0 ? Math.min(60, Math.max(12, v)) : fallback;
}

/** Printable width in dots: 80 mm → 576, 58 mm → 384 (both multiples of 8). */
export function paperDots(paperWidth: number): number {
  return paperWidth === 58 ? 384 : 576;
}

function money(n: number): string {
  return `${n.toFixed(2)} ₼`;
}

function splitList(text: string | null | undefined): string[] {
  return (text ?? "")
    .split(/[\n,]+/)
    .map((s) => s.trim())
    .filter(Boolean);
}

function loadImage(url: string): Promise<HTMLImageElement | null> {
  return new Promise((resolve) => {
    const img = new Image();
    img.crossOrigin = "anonymous";
    const timer = window.setTimeout(() => resolve(null), 4000);
    img.onload = () => {
      window.clearTimeout(timer);
      resolve(img);
    };
    img.onerror = () => {
      window.clearTimeout(timer);
      resolve(null);
    };
    img.src = url;
  });
}

function groupLines(lines: OrderReceiptLineDto[], group: boolean): OrderReceiptLineDto[] {
  if (!group) return lines;
  const out: OrderReceiptLineDto[] = [];
  const index = new Map<string, number>();
  for (const line of lines) {
    const key = `${line.menuItemName}__${line.unitPrice}__${line.isGift}__${line.paidEarlier}`;
    const i = index.get(key);
    if (i === undefined) {
      index.set(key, out.length);
      out.push({ ...line });
    } else {
      out[i].quantity += line.quantity;
      out[i].lineTotal += line.lineTotal;
      out[i].vatAmount += line.vatAmount;
    }
  }
  return out;
}

/** Floyd–Steinberg dithering of one area to pure black/white — keeps logos readable on thermal paper. */
function ditherArea(ctx: CanvasRenderingContext2D, x: number, y: number, w: number, h: number) {
  if (w <= 0 || h <= 0) return;
  const image = ctx.getImageData(x, y, w, h);
  const d = image.data;
  const gray = new Float32Array(w * h);
  for (let i = 0; i < w * h; i++) {
    const a = d[i * 4 + 3] / 255;
    // transparent pixels count as white paper
    gray[i] = (0.299 * d[i * 4] + 0.587 * d[i * 4 + 1] + 0.114 * d[i * 4 + 2]) * a + 255 * (1 - a);
  }
  for (let py = 0; py < h; py++) {
    for (let px = 0; px < w; px++) {
      const i = py * w + px;
      const value = gray[i] < 128 ? 0 : 255;
      const err = gray[i] - value;
      gray[i] = value;
      if (px + 1 < w) gray[i + 1] += (err * 7) / 16;
      if (py + 1 < h) {
        if (px > 0) gray[i + w - 1] += (err * 3) / 16;
        gray[i + w] += (err * 5) / 16;
        if (px + 1 < w) gray[i + w + 1] += err / 16;
      }
    }
  }
  for (let i = 0; i < w * h; i++) {
    d[i * 4] = d[i * 4 + 1] = d[i * 4 + 2] = gray[i];
    d[i * 4 + 3] = 255;
  }
  ctx.putImageData(image, x, y);
}

/** Draws the receipt. Needs a browser (canvas). */
export async function renderReceipt(
  receipt: OrderReceiptDto,
  design: ReceiptDesign,
  categoryName: (id: number | null | undefined) => string = () => "",
  /** `preCheck`: the customer's pre-check ("Müştəri qəbzi") — marked, so it is not mistaken for the final receipt. */
  opts: { preCheck?: boolean } = {},
): Promise<HTMLCanvasElement> {
  const width = paperDots(design.paperWidth);
  const inner = width - MARGIN * 2;
  const canvas = document.createElement("canvas");
  canvas.width = width;
  canvas.height = 8000; // cropped to the used height at the end
  const ctx = canvas.getContext("2d", { willReadFrequently: true })!;
  ctx.fillStyle = "#fff";
  ctx.fillRect(0, 0, width, canvas.height);
  ctx.fillStyle = "#000";
  ctx.textBaseline = "top";

  let y = MARGIN;
  const fs = design.fontSize;
  const font = (size: number, weight = "normal", style = "normal") => `${style} ${weight} ${size}px ${FONT}`;

  const wrap = (text: string, maxWidth: number): string[] => {
    const out: string[] = [];
    for (const paragraph of text.split("\n")) {
      let current = "";
      for (const word of paragraph.split(/\s+/).filter(Boolean)) {
        const next = current ? `${current} ${word}` : word;
        if (ctx.measureText(next).width <= maxWidth || !current) current = next;
        else {
          out.push(current);
          current = word;
        }
      }
      out.push(current);
    }
    return out;
  };

  const center = (text: string, size: number, weight = "normal", style = "normal") => {
    ctx.font = font(size, weight, style);
    for (const line of wrap(text, inner)) {
      ctx.fillText(line, (width - ctx.measureText(line).width) / 2, y);
      y += Math.round(size * 1.25);
    }
  };
  const leftRight = (left: string, right: string, size = fs, weight = "normal") => {
    ctx.font = font(size, weight);
    const rightWidth = ctx.measureText(right).width;
    const leftLines = wrap(left, inner - rightWidth - 12);
    leftLines.forEach((line, i) => {
      ctx.fillText(line, MARGIN, y);
      if (i === leftLines.length - 1) ctx.fillText(right, width - MARGIN - rightWidth, y);
      y += Math.round(size * 1.25);
    });
  };
  const separator = (dashed = true) => {
    y += 4;
    ctx.save();
    ctx.lineWidth = 2;
    if (dashed) ctx.setLineDash([6, 4]);
    ctx.beginPath();
    ctx.moveTo(MARGIN, y);
    ctx.lineTo(width - MARGIN, y);
    ctx.stroke();
    ctx.restore();
    y += 8;
  };
  const socialItems = design.showExtras
    ? [...(design.contactPhone ? [design.contactPhone] : []), ...splitList(design.socialLinks)]
    : [];
  const socials = () => {
    for (const s of socialItems) center(s, Math.round(fs * 0.85));
  };

  // ── Logo
  if (design.showLogo && design.logoUrl) {
    const img = await loadImage(design.logoUrl);
    if (img && img.naturalWidth > 0) {
      const w = Math.round((width * Math.min(100, Math.max(20, design.logoWidth))) / 100);
      const h = Math.round((img.naturalHeight / img.naturalWidth) * w);
      const x = Math.round((width - w) / 2);
      // Dithered on a scratch canvas first: a logo served without CORS taints whatever it is drawn
      // on, and a tainted receipt canvas could no longer be turned into printer data.
      const scratch = document.createElement("canvas");
      scratch.width = w;
      scratch.height = h;
      const sctx = scratch.getContext("2d", { willReadFrequently: true })!;
      sctx.drawImage(img, 0, 0, w, h);
      try {
        ditherArea(sctx, 0, 0, w, h);
        ctx.drawImage(scratch, x, y);
        y += h + 8;
      } catch {
        // cross-origin logo without CORS — print without it rather than fail
      }
    }
  }

  // ── Name, header, address, socials (top)
  if (design.showBusinessName) center(receipt.restaurantName, design.nameFontSize, "bold");
  if (design.headerText) center(design.headerText, fs);
  if (receipt.restaurantAddress) center(receipt.restaurantAddress, Math.round(fs * 0.9));
  if (design.socialPosition === "top") socials();
  separator();

  // ── Where and when
  const pad = (n: number) => String(n).padStart(2, "0");
  const dt = (v: string | null) => {
    if (!v) return "—";
    const d = new Date(v);
    return `${pad(d.getDate())}.${pad(d.getMonth() + 1)}.${d.getFullYear()} ${pad(d.getHours())}:${pad(d.getMinutes())}`;
  };
  const small = Math.round(fs * 0.95);
  if (design.showOrderNumber) leftRight("Çek:", receipt.receiptNumber || receipt.orderNumber, small);
  if (design.showTableName) {
    if (receipt.sectionName) leftRight("Zal:", receipt.sectionName, small);
    leftRight("Masa:", receipt.tableName, small);
  }
  if (design.showWaiterName) leftRight("Ofisiant:", receipt.waiterName, small);
  if (design.showTime) {
    leftRight("Gəliş:", dt(receipt.openedAt), small);
    if (receipt.closedAt) leftRight("Çıxış:", dt(receipt.closedAt), small);
  }
  separator();

  // ── Products as a table: Məhsul | Miqdar | Qiymət | Cəmi — each product on one row, its values
  // under the column headings; a long name wraps inside its own column. A gift says "Hədiyyə"
  // in the Cəmi column.
  const totalRight = width - MARGIN;
  const totalW = Math.round(inner * 0.22);
  const priceRight = totalRight - totalW - 6;
  const priceW = Math.round(inner * 0.2);
  const qtyRight = priceRight - priceW - 6;
  const qtyW = Math.round(inner * 0.12);
  const nameW = qtyRight - qtyW - 6 - MARGIN;
  const rightText = (text: string, right: number) => ctx.fillText(text, right - ctx.measureText(text).width, y);
  /** Right-aligned cell that shrinks its font until the text fits the column (58 mm paper). */
  const cell = (text: string, right: number, maxWidth: number, weight = "normal") => {
    let size = fs;
    ctx.font = font(size, weight);
    while (size > 12 && ctx.measureText(text).width > maxWidth) {
      size -= 1;
      ctx.font = font(size, weight);
    }
    rightText(text, right);
    ctx.font = font(fs);
  };
  const num = (n: number) => n.toFixed(2);

  if (opts.preCheck) {
    center("ÖN ÇEK — ödəniş deyil", Math.round(fs * 1.1), "bold");
    y += 4;
  }
  ctx.font = font(Math.round(fs * 0.9), "bold");
  ctx.fillText("Məhsul", MARGIN, y);
  rightText("Miqdar", qtyRight);
  rightText("Qiymət", priceRight);
  rightText("Cəmi", totalRight);
  y += Math.round(fs * 1.15);
  separator();

  const rows = arrangeReceiptLines(groupLines(receipt.lines, design.groupQuantities), design.sortMode, categoryName);
  for (const row of rows) {
    if (row.kind === "header") {
      ctx.font = font(fs, "bold");
      ctx.fillText(row.title, MARGIN, y);
      y += Math.round(fs * 1.3);
      continue;
    }
    const line = row.line;
    const name = line.isGift && line.menuItemName.endsWith(GIFT_SUFFIX)
      ? line.menuItemName.slice(0, -GIFT_SUFFIX.length)
      : line.menuItemName;
    ctx.font = font(fs);
    const nameLines = wrap(name, nameW);
    nameLines.forEach((l, i) => {
      ctx.fillText(l, MARGIN, y);
      if (i === 0) {
        cell(String(line.quantity), qtyRight, qtyW);
        cell(num(line.unitPrice), priceRight, priceW);
        if (line.isGift) cell("Hədiyyə", totalRight, totalW, "bold");
        else if (line.paidEarlier) cell("Ödənilib", totalRight, totalW, "bold");
        else cell(num(line.lineTotal), totalRight, totalW);
      }
      y += Math.round(fs * 1.25);
    });
    if (line.isGift && design.giftNote) {
      ctx.font = font(design.giftNoteFontSize, "normal", "italic");
      for (const l of wrap(design.giftNote, inner - 16)) {
        ctx.fillText(l, MARGIN + 16, y);
        y += Math.round(design.giftNoteFontSize * 1.2);
      }
    }
    y += 2;
  }
  separator();

  // ── Totals — written once: without extras the sum is the total; otherwise sum, extras, then total
  const hasExtras = receipt.serviceChargeAmount > 0 || receipt.discountAmount > 0 || receipt.tableRentalAmount > 0;
  const bigFs = Math.round(fs * 1.25);
  if (!hasExtras) {
    leftRight("YEKUN MƏBLƏĞ", money(receipt.grandTotal), bigFs, "bold");
  } else {
    leftRight("Ümumi məbləğ", money(receipt.totalAmount), fs);
    if (receipt.discountAmount > 0) leftRight("Endirim", `−${money(receipt.discountAmount)}`, fs);
    if (receipt.serviceChargeAmount > 0) {
      const base = receipt.totalAmount - receipt.discountAmount;
      const pct = base > 0 ? Math.round((receipt.serviceChargeAmount / base) * 1000) / 10 : 0;
      leftRight(`Servis haqqı${pct > 0 ? ` (${pct}%)` : ""}`, money(receipt.serviceChargeAmount), fs);
    }
    if (receipt.tableRentalAmount > 0) leftRight("Masa icarəsi", money(receipt.tableRentalAmount), fs);
    leftRight("YEKUN MƏBLƏĞ", money(receipt.grandTotal), bigFs, "bold");
  }
  if (design.showVat && receipt.vatAmount > 0) leftRight("ƏDV daxildir", money(receipt.vatAmount), Math.round(fs * 0.85));
  if (design.showPaymentMethod && receipt.paidAt) {
    // Under the grand total: how much was paid in cash and how much by card.
    if (receipt.cashPaidAmount > 0) leftRight("Nağd ödəniş", money(receipt.cashPaidAmount), fs);
    if (receipt.cardPaidAmount > 0) leftRight("Kart ödəniş", money(receipt.cardPaidAmount), fs);
    if (receipt.creditPaidAmount > 0) leftRight("Borc (nisyə)", money(receipt.creditPaidAmount), fs);
    leftRight("Kassa", receipt.isFiscal ? "Fiskal (vergi kassası)" : "Adi kassa", small);
    const mixed = receipt.paymentMethod === "Mixed";
    leftRight("Ödəniş", mixed ? "Qarışıq" : PAYMENT_LABELS[receipt.paymentMethod] ?? receipt.paymentMethod, small);
    if (receipt.paymentMethod === "Cash" && receipt.paidAmount > 0) {
      leftRight("Alınan", money(receipt.paidAmount), small);
      leftRight("Qalıq", money(receipt.changeAmount), small);
    } else if (mixed && receipt.changeAmount > 0) {
      leftRight("Alınan", money(receipt.paidAmount), small);
      leftRight("Qalıq", money(receipt.changeAmount), small);
    }
  }

  // ── Socials (bottom) and the closing note
  if (design.socialPosition === "bottom" && socialItems.length > 0) {
    separator();
    socials();
  }
  if (design.showExtras && design.slogan) {
    y += 8;
    center(design.slogan, fs, "bold");
  }
  if (design.footerText) {
    y += 8;
    center(design.footerText, fs, "normal", "italic");
  }
  y += MARGIN;

  // Crop to the used height.
  const out = document.createElement("canvas");
  out.width = width;
  out.height = Math.ceil(y);
  out.getContext("2d")!.drawImage(canvas, 0, 0);
  return out;
}

/** Packs the canvas into 1-bit rows (MSB first, 1 = black) for ESC/POS GS v 0. */
export function canvasToRaster(canvas: HTMLCanvasElement): { widthDots: number; height: number; data: string } {
  const widthDots = Math.floor(canvas.width / 8) * 8;
  const height = canvas.height;
  const pixels = canvas.getContext("2d")!.getImageData(0, 0, widthDots, height).data;
  const bytesPerRow = widthDots / 8;
  const bits = new Uint8Array(bytesPerRow * height);
  for (let y = 0; y < height; y++) {
    for (let x = 0; x < widthDots; x++) {
      const i = (y * widthDots + x) * 4;
      const lum = 0.299 * pixels[i] + 0.587 * pixels[i + 1] + 0.114 * pixels[i + 2];
      if (lum < 150) bits[y * bytesPerRow + (x >> 3)] |= 0x80 >> (x & 7);
    }
  }
  let binary = "";
  const chunk = 0x8000;
  for (let i = 0; i < bits.length; i += chunk) {
    binary += String.fromCharCode(...bits.subarray(i, i + chunk));
  }
  return { widthDots, height, data: btoa(binary) };
}
