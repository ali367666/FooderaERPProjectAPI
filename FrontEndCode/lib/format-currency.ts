export function formatCurrency(value?: number | null) {
  if (value === undefined || value === null) return "0.00 AZN";
  return `${Number(value).toFixed(2)} AZN`;
}
