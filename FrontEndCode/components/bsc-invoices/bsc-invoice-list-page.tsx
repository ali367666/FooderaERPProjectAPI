"use client";

import { useCallback, useEffect, useMemo, useState } from "react";
import { Eye } from "lucide-react";
import { AdvancedTableFilters, type TableFilterDef } from "@/components/advanced-table-filters";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Badge } from "@/components/ui/badge";
import { cn } from "@/lib/utils";
import { getBscInvoices, type BscInvoiceMDto } from "@/lib/services/bsc-invoice-service";
import { toApiFormError } from "@/lib/api-error";
import { BscInvoiceDetailDialog } from "./bsc-invoice-detail-dialog";

const ITEMS_PER_PAGE = 15;

function formatDate(iso: string): string {
  if (!iso) return "—";
  const d = new Date(iso);
  return Number.isNaN(d.getTime()) ? iso : d.toLocaleDateString("az-AZ", { year: "numeric", month: "2-digit", day: "2-digit" });
}

function formatAzn(val: number): string {
  return val.toLocaleString("az-AZ", { minimumFractionDigits: 2, maximumFractionDigits: 2 }) + " ₼";
}

type ListRow = {
  id: string;
  numericId: number;
  bscInvoiceMId: number;
  docNo: string;
  docDate: string;
  entityId: number | null;
  branchId: number | null;
  coId: number | null;
  amt: number;
  amtVat: number;
  purchaseSales: number | null;
  raw: BscInvoiceMDto;
};

export function BscInvoiceListPage() {
  const [items, setItems] = useState<BscInvoiceMDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [search, setSearch] = useState("");
  const [page, setPage] = useState(1);
  const [sortDesc, setSortDesc] = useState(true);
  const [selectedRowId, setSelectedRowId] = useState<string | null>(null);
  const [detailOpen, setDetailOpen] = useState(false);
  const [detailRow, setDetailRow] = useState<BscInvoiceMDto | null>(null);

  const load = useCallback(async () => {
    try {
      setLoading(true);
      setError(null);
      const data = await getBscInvoices();
      setItems(data);
    } catch (e) {
      setError(toApiFormError(e, "Failed to load BSC invoices").message);
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => { void load(); }, [load]);
  useEffect(() => { setSelectedRowId(null); }, [page]);

  const baseRows: ListRow[] = useMemo(
    () =>
      items.map((r) => ({
        id: String(r.id),
        numericId: r.id,
        bscInvoiceMId: r.bscInvoiceMId,
        docNo: r.docNo ?? "—",
        docDate: formatDate(r.docDate),
        entityId: r.entityId,
        branchId: r.branchId,
        coId: r.coId,
        amt: r.amt,
        amtVat: r.amtVat,
        purchaseSales: r.purchaseSales,
        raw: r,
      })),
    [items],
  );

  const filterDefs = useMemo<TableFilterDef<ListRow>[]>(() => [
    {
      id: "purchaseSales",
      label: "Tip",
      ui: "select",
      options: [
        { value: "1", label: "Satış" },
        { value: "2", label: "Alış" },
      ],
      match: (row, get) => {
        const v = get("purchaseSales");
        return !v || String(row.purchaseSales) === v;
      },
    },
  ], []);

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-semibold tracking-tight">BSC Invoice</h1>
        <p className="text-muted-foreground text-sm mt-1">
          BSC test bazasından sinxronizasiya edilmiş faktura məlumatları.
        </p>
      </div>

      {error ? (
        <div className="rounded-md border border-destructive/40 bg-destructive/10 px-4 py-3 text-sm text-destructive">
          {error}
        </div>
      ) : null}

      {loading ? (
        <p className="text-sm text-muted-foreground">Yüklənir…</p>
      ) : (
        <AdvancedTableFilters defs={filterDefs} data={baseRows} title="Filtrlər">
          {(filtered) => {
            const searched = filtered.filter((row) => {
              const q = search.trim().toLowerCase();
              if (!q) return true;
              return [row.docNo, String(row.bscInvoiceMId), String(row.entityId ?? "")]
                .join(" ").toLowerCase().includes(q);
            });
            const sorted = [...searched].sort((a, b) =>
              sortDesc ? b.bscInvoiceMId - a.bscInvoiceMId : a.bscInvoiceMId - b.bscInvoiceMId,
            );
            const totalPages = Math.max(1, Math.ceil(sorted.length / ITEMS_PER_PAGE));
            const current = Math.min(page, totalPages);
            const slice = sorted.slice((current - 1) * ITEMS_PER_PAGE, current * ITEMS_PER_PAGE);
            const selectedRow = selectedRowId ? (sorted.find((r) => r.id === selectedRowId) ?? null) : null;

            return (
              <div className="space-y-4">
                <div className="flex flex-col sm:flex-row gap-3 sm:items-center sm:justify-between">
                  <Input
                    placeholder="Sənəd nömrəsi, entity ID ilə axtarın…"
                    value={search}
                    onChange={(e) => { setSearch(e.target.value); setPage(1); }}
                    className="max-w-md"
                  />
                  <div className="flex gap-2">
                    <Button type="button" variant="outline" size="sm"
                      onClick={() => { setSortDesc((d) => !d); setPage(1); }}>
                      Sıralama: {sortDesc ? "Ən yeni" : "Ən köhnə"}
                    </Button>
                    {selectedRow && (
                      <Button type="button" size="sm" variant="outline"
                        onClick={() => { setDetailRow(selectedRow.raw); setDetailOpen(true); }}>
                        <Eye className="h-4 w-4 mr-1" /> Detallar
                      </Button>
                    )}
                  </div>
                </div>

                <div className="rounded-lg border border-border overflow-x-auto">
                  <table className="w-full text-sm leading-[1.2] [&_td]:align-top [&_th]:align-top">
                    <thead>
                      <tr className="border-b border-border bg-muted/40">
                        <th className="text-left py-2 px-3 font-medium">BSC ID</th>
                        <th className="text-left py-2 px-3 font-medium">Sənəd №</th>
                        <th className="text-left py-2 px-3 font-medium">Tarix</th>
                        <th className="text-left py-2 px-3 font-medium">Entity ID</th>
                        <th className="text-left py-2 px-3 font-medium">Filial ID</th>
                        <th className="text-left py-2 px-3 font-medium">Tip</th>
                        <th className="text-right py-2 px-3 font-medium">Məbləğ</th>
                        <th className="text-right py-2 px-3 font-medium">ƏDV</th>
                        <th className="text-right py-2 px-3 font-medium">Cəmi</th>
                      </tr>
                    </thead>
                    <tbody>
                      {slice.length === 0 ? (
                        <tr>
                          <td colSpan={9} className="py-10 text-center text-muted-foreground">
                            Heç bir faktura tapılmadı.
                          </td>
                        </tr>
                      ) : (
                        slice.map((row) => {
                          const rowSelected = selectedRowId === row.id;
                          return (
                            <tr
                              key={row.id}
                              aria-selected={rowSelected}
                              className={cn(
                                "border-b border-border cursor-pointer transition-colors",
                                "hover:bg-[#f5f5f5] dark:hover:bg-muted/35",
                                rowSelected && "bg-[#e8e8e8] ring-1 ring-inset ring-primary/25 dark:bg-muted/60",
                              )}
                              onClick={() => setSelectedRowId(row.id)}
                              onDoubleClick={() => { setDetailRow(row.raw); setDetailOpen(true); }}
                            >
                              <td className="py-2 px-3 font-mono text-xs">{row.bscInvoiceMId}</td>
                              <td className="py-2 px-3 font-mono text-xs">{row.docNo}</td>
                              <td className="py-2 px-3 text-muted-foreground">{row.docDate}</td>
                              <td className="py-2 px-3">{row.entityId ?? "—"}</td>
                              <td className="py-2 px-3">{row.branchId ?? "—"}</td>
                              <td className="py-2 px-3">
                                <Badge variant={row.purchaseSales === 1 ? "default" : "secondary"}>
                                  {row.purchaseSales === 1 ? "Satış" : row.purchaseSales === 2 ? "Alış" : "—"}
                                </Badge>
                              </td>
                              <td className="py-2 px-3 text-right tabular-nums">{formatAzn(row.amt)}</td>
                              <td className="py-2 px-3 text-right tabular-nums">{formatAzn(row.amtVat)}</td>
                              <td className="py-2 px-3 text-right tabular-nums font-medium">
                                {formatAzn(row.amt + row.amtVat)}
                              </td>
                            </tr>
                          );
                        })
                      )}
                    </tbody>
                  </table>
                </div>

                {totalPages > 1 ? (
                  <div className="flex items-center justify-between text-sm text-muted-foreground">
                    <span>
                      {(current - 1) * ITEMS_PER_PAGE + 1}–
                      {Math.min(current * ITEMS_PER_PAGE, sorted.length)} / {sorted.length}
                    </span>
                    <div className="flex gap-2">
                      <Button type="button" variant="outline" size="sm" disabled={current <= 1}
                        onClick={() => setPage((p) => Math.max(1, p - 1))}>Əvvəlki</Button>
                      <Button type="button" variant="outline" size="sm" disabled={current >= totalPages}
                        onClick={() => setPage((p) => Math.min(totalPages, p + 1))}>Növbəti</Button>
                    </div>
                  </div>
                ) : null}
              </div>
            );
          }}
        </AdvancedTableFilters>
      )}

      <BscInvoiceDetailDialog
        open={detailOpen}
        onOpenChange={setDetailOpen}
        invoice={detailRow}
      />
    </div>
  );
}
