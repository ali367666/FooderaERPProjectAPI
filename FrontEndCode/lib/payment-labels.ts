/** Display names of the payment methods (Domain.Enums.PaymentMethod) — one place for the POS dialogs. */
const LABELS: Record<string, string> = { Cash: "Nağd", Card: "Kart", Credit: "Borc", Mixed: "Qarışıq" };

export function paymentMethodLabel(method: string): string {
  return LABELS[method] ?? method;
}
