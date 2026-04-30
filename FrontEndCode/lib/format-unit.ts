export function formatUnit(unit?: string | number) {
  if (unit === undefined || unit === null) return "";

  const value = String(unit).toLowerCase();

  if (value === "kilogram" || value === "kg" || value === "1") return "kg";
  if (value === "gram" || value === "g" || value === "2") return "g";
  if (value === "liter" || value === "litre" || value === "l" || value === "3") return "L";
  if (value === "milliliter" || value === "ml" || value === "4") return "ml";
  if (value === "piece" || value === "pieces" || value === "unit" || value === "pcs" || value === "5") return "pcs";

  return String(unit);
}
