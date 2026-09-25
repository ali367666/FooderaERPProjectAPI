/**
 * Receipt barcode — the order id zero-padded to 8 digits. Scanning it on the POS "Geri qaytarma"
 * page finds the sale again (the backend also accepts the full receipt number typed by hand).
 */
export function receiptBarcodeValue(orderId: number): string {
  return String(orderId).padStart(8, "0");
}

/**
 * ESC/POS commands that make a thermal printer draw the barcode itself (CODE128, number printed
 * underneath, centered). Appended to the plain-text receipt sent to network printers.
 */
export function escPosBarcode(value: string): string {
  const GS = "\x1d";
  const ESC = "\x1b";
  const data = `{B${value}`;
  return [
    `${ESC}a\x01`, // center
    `${GS}h\x50`, // height 80 dots
    `${GS}w\x02`, // module width 2
    `${GS}H\x02`, // human-readable text below
    `${GS}k\x49${String.fromCharCode(data.length)}${data}`, // CODE128
    `${ESC}a\x00`, // back to left
  ].join("");
}
