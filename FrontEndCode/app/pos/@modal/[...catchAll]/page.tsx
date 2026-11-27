/**
 * Empties the modal slot on every other POS route — without it a soft navigation out of an
 * intercepted modal (e.g. a report link inside it) would leave the modal open on the new page.
 */
export default function PosModalCatchAll() {
  return null;
}
