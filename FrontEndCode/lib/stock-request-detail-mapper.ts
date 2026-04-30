import type { Warehouse } from "@/lib/services/warehouse-service";
import { WarehouseType } from "@/lib/services/warehouse-service";
import type { StockRequestDto } from "@/lib/services/stock-request-service";

/** Labels shown on the stock request detail / view screen (all user-facing, no IDs). */
export type StockRequestDetailLabels = {
  companyName: string;
  requestingWarehouseName: string;
  supplyingWarehouseName: string;
  /** Backend DTO does not expose a document date; use "—" until API adds one. */
  requestDateLabel: string;
  noteDisplay: string;
};

/**
 * Maps API DTO + resolved company name → stable view labels.
 * Warehouse names come from the API (`StockRequestResponse` includes both IDs and names).
 */
export function toStockRequestDetailLabels(
  dto: StockRequestDto,
  resolvedCompanyName: string,
): StockRequestDetailLabels {
  const companyName = resolvedCompanyName.trim() || "—";
  const requestingWarehouseName = dto.requestingWarehouseName?.trim() || "—";
  const supplyingWarehouseName = dto.supplyingWarehouseName?.trim() || "—";
  const note = dto.note?.trim();

  return {
    companyName,
    requestingWarehouseName,
    supplyingWarehouseName,
    requestDateLabel: "—",
    noteDisplay: note && note.length > 0 ? note : "—",
  };
}

/**
 * Ensures requesting/supplying warehouses appear in dropdown options even if
 * `/Warehouses` omits them (e.g. stale cache) — uses names from the document DTO.
 */
export function mergeWarehousesForDocument(
  warehousesInCompany: Warehouse[],
  dto: StockRequestDto,
): Warehouse[] {
  const map = new Map<number, Warehouse>(warehousesInCompany.map((w) => [w.id, w]));

  const ensure = (id: number, name: string) => {
    if (!Number.isFinite(id) || id <= 0) return;
    if (!map.has(id)) {
      map.set(id, {
        id,
        name: name.trim() || "—",
        type: WarehouseType.HeadOffice,
        companyId: dto.companyId,
        restaurantId: null,
        restaurantName: null,
        driverUserId: null,
        driverFullName: null,
      });
    }
  };

  ensure(dto.requestingWarehouseId, dto.requestingWarehouseName);
  ensure(dto.supplyingWarehouseId, dto.supplyingWarehouseName);

  return [...map.values()]
    .filter((w) => w.companyId === dto.companyId)
    .sort((a, b) => a.name.localeCompare(b.name));
}
