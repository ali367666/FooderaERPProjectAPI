"use client";

import { useCallback, useEffect, useMemo, useState } from "react";
import { useRouter } from "next/navigation";
import { Eye, Pencil, Plus, Trash2, Send, Check, X, Ban, Wallet, Undo2 } from "lucide-react";
import { AdvancedTableFilters, type TableFilterDef } from "@/components/advanced-table-filters";
import { DocumentActionToolbar } from "@/components/documents/document-action-toolbar";
import { ListSelectionSync } from "@/components/documents/list-selection-sync";
import { Button } from "@/components/ui/button";
import { Checkbox } from "@/components/ui/checkbox";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from "@/components/ui/dialog";
import { Badge } from "@/components/ui/badge";
import { cn } from "@/lib/utils";
import { ConfirmDialog } from "@/components/documents/confirm-dialog";
import { useSelectedCompany } from "@/contexts/selected-company-context";
import { filterBySelectedCompany } from "@/lib/company-scope-utils";
import {
  StockPurchaseStatus,
  CURRENCY_LABELS,
  deleteStockPurchase,
  getStockPurchases,
  submitStockPurchase,
  approveStockPurchase,
  rejectStockPurchase,
  cancelStockPurchase,
  payStockPurchase,
  returnStockPurchase,
  type StockPurchaseDto,
} from "@/lib/services/stock-purchase-service";
import { toApiFormError } from "@/lib/api-error";
import { toast } from "sonner";

const ITEMS_PER_PAGE = 10;

function formatDate(iso: string): string {
  if (!iso) return "—";
  const d = new Date(iso);
  return Number.isNaN(d.getTime()) ? iso : d.toLocaleDateString(undefined, { year: "numeric", month: "short", day: "numeric" });
}

function formatAzn(val: number): string {
  return val.toLocaleString("az-AZ", { minimumFractionDigits: 2, maximumFractionDigits: 2 }) + " ₼";
}

function StatusBadge({ status }: { status: number }) {
  const map: Record<number, { label: string; variant: "default" | "secondary" | "destructive" | "outline" }> = {
    [StockPurchaseStatus.Draft]:     { label: "Draft",     variant: "secondary" },
    [StockPurchaseStatus.Pending]:   { label: "Pending",   variant: "default" },
    [StockPurchaseStatus.Approved]:  { label: "Approved",  variant: "default" },
    [StockPurchaseStatus.Completed]: { label: "Completed", variant: "default" },
    [StockPurchaseStatus.Rejected]:  { label: "Rejected",  variant: "destructive" },
    [StockPurchaseStatus.Cancelled]: { label: "Cancelled", variant: "outline" },
  };
  const info = map[status] ?? { label: String(status), variant: "outline" };
  return <Badge variant={info.variant}>{info.label}</Badge>;
}

type ListRow = {
  id: string;
  numericId: number;
  documentNo: string;
  companyId: number;
  counterpartyName: string;
  isImport: boolean;
  currencyCode: string;
  exchangeRate: number;
  totalForeign: number;
  totalAzn: number;
  purchaseDateLabel: string;
  warehouseName: string;
  status: number;
  note: string;
  raw: StockPurchaseDto;
};

export function StockPurchaseListPage() {
  const router = useRouter();
  const { companies, selectedCompanyId: scopeCompanyId } = useSelectedCompany();
  const [items, setItems] = useState<StockPurchaseDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [search, setSearch] = useState("");
  const [page, setPage] = useState(1);
  const [sortDesc, setSortDesc] = useState(true);
  const [selectedRowId, setSelectedRowId] = useState<string | null>(null);
  const [deleteTarget, setDeleteTarget] = useState<ListRow | null>(null);
  const [payTarget, setPayTarget] = useState<{ numericId: number; documentNo: string; remaining: number } | null>(null);
  const [payAmount, setPayAmount] = useState("");
  const [payNote, setPayNote] = useState("");
  const [paying, setPaying] = useState(false);
  const [returnTarget, setReturnTarget] = useState<StockPurchaseDto | null>(null);
  const [returnQty, setReturnQty] = useState<Record<number, string>>({});
  const [returnNote, setReturnNote] = useState("");
  const [returning, setReturning] = useState(false);
  const [workflowConfirm, setWorkflowConfirm] = useState<
    null | { kind: "approve" | "reject" | "cancel"; numericId: number }
  >(null);

  const load = useCallback(async () => {
    try {
      setLoading(true);
      setError(null);
      const data = await getStockPurchases();
      setItems(data);
    } catch (e) {
      setError(toApiFormError(e, "Failed to load stock purchases").message);
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
        documentNo: r.documentNo,
        companyId: r.companyId,
        counterpartyName: r.counterpartyName,
        isImport: r.isImport,
        currencyCode: r.currencyCode,
        exchangeRate: r.exchangeRate,
        totalForeign: r.totalForeign,
        totalAzn: r.totalAzn,
        purchaseDateLabel: formatDate(r.purchaseDate),
        warehouseName: r.warehouseName,
        status: r.status,
        note: r.note?.trim() || "—",
        raw: r,
      })),
    [items],
  );

  const scopedRows = useMemo(
    () => filterBySelectedCompany(baseRows, scopeCompanyId, (r) => r.raw.companyId),
    [baseRows, scopeCompanyId],
  );

  const filterDefs = useMemo<TableFilterDef<ListRow>[]>(() => {
    const statusOpts = [
      { value: String(StockPurchaseStatus.Draft),     label: "Draft" },
      { value: String(StockPurchaseStatus.Pending),   label: "Pending" },
      { value: String(StockPurchaseStatus.Completed), label: "Completed" },
      { value: String(StockPurchaseStatus.Rejected),  label: "Rejected" },
      { value: String(StockPurchaseStatus.Cancelled), label: "Cancelled" },
    ];
    const companyOpts = [...companies]
      .sort((a, b) => a.name.localeCompare(b.name))
      .map((c) => ({ value: String(c.id), label: c.name }));
    return [
      {
        id: "status", label: "Status", ui: "select", options: statusOpts,
        match: (row, get) => { const v = get("status"); return !v || String(row.status) === v; },
      },
      {
        id: "company", label: "Company", ui: "select", options: companyOpts,
        match: (row, get) => { const v = get("company"); return !v || String(row.raw.companyId) === v; },
      },
      {
        id: "import", label: "Source", ui: "select",
        options: [{ value: "true", label: "Import (xarici)" }, { value: "false", label: "Local (daxili)" }],
        match: (row, get) => {
          const v = get("import");
          return !v || String(row.isImport) === v;
        },
      },
    ];
  }, [companies]);

  return (
    <div className="space-y-6">
      <div className="flex flex-col gap-3 sm:flex-row sm:items-end sm:justify-between">
        <div>
          <h1 className="text-2xl font-semibold tracking-tight">Stock Purchases</h1>
          <p className="text-muted-foreground text-sm mt-1">
            Baş ofis tərəfindən daxili və ya xarici mal alışları. Xarici alışlar CBAR məzənnəsi ilə AZN-ə çevrilir.
          </p>
        </div>
        <Button onClick={() => router.push("/dashboard/stock-purchases/new")}>
          <Plus className="h-4 w-4 mr-2" />
          New purchase
        </Button>
      </div>

      {error ? (
        <div className="rounded-md border border-destructive/40 bg-destructive/10 px-4 py-3 text-sm text-destructive">{error}</div>
      ) : null}

      {loading ? (
        <p className="text-sm text-muted-foreground">Loading…</p>
      ) : (
        <AdvancedTableFilters defs={filterDefs} data={scopedRows} title="Filters">
          {(filtered) => {
            const searched = filtered.filter((row) => {
              const q = search.trim().toLowerCase();
              if (!q) return true;
              return [row.documentNo, row.counterpartyName, row.warehouseName, row.note]
                .join(" ").toLowerCase().includes(q);
            });
            const sorted = [...searched].sort((a, b) =>
              sortDesc ? b.numericId - a.numericId : a.numericId - b.numericId,
            );
            const totalPages = Math.max(1, Math.ceil(sorted.length / ITEMS_PER_PAGE));
            const current = Math.min(page, totalPages);
            const slice = sorted.slice((current - 1) * ITEMS_PER_PAGE, current * ITEMS_PER_PAGE);
            const selectedRow = selectedRowId ? (sorted.find((r) => r.id === selectedRowId) ?? null) : null;
            const hasSelection = selectedRow != null;
            const st = selectedRow?.status;

            const canEdit   = hasSelection && st === StockPurchaseStatus.Draft;
            const canDelete = hasSelection && st === StockPurchaseStatus.Draft;
            const canSubmit = hasSelection && st === StockPurchaseStatus.Draft;
            const canApprove = hasSelection && st === StockPurchaseStatus.Pending;
            const canReject  = hasSelection && st === StockPurchaseStatus.Pending;
            const canCancel  = hasSelection && st === StockPurchaseStatus.Completed;
            const remaining  = selectedRow?.raw.remainingAzn ?? 0;
            const canReturn  = hasSelection && st === StockPurchaseStatus.Completed
              && (selectedRow?.raw.lines ?? []).some((l) => (l.returnableQuantity ?? l.quantity) > 0);
            const canPay     = hasSelection && st === StockPurchaseStatus.Completed && remaining > 0;

            return (
              <div className="space-y-4">
                <ListSelectionSync visibleRowIds={sorted.map((r) => r.id)} setSelectedRowId={setSelectedRowId} />
                <div className="flex flex-col sm:flex-row gap-3 sm:items-center sm:justify-between">
                  <Input
                    placeholder="Search document, supplier, warehouse…"
                    value={search}
                    onChange={(e) => { setSearch(e.target.value); setPage(1); }}
                    className="max-w-md"
                  />
                  <Button type="button" variant="outline" size="sm"
                    onClick={() => { setSortDesc((d) => !d); setPage(1); }}>
                    Sort: {sortDesc ? "Newest first" : "Oldest first"}
                  </Button>
                </div>

                <DocumentActionToolbar
                  hasSelection={hasSelection}
                  selectedSummary={selectedRow?.documentNo}
                  actions={[
                    {
                      id: "view", label: "View", icon: Eye, enabled: hasSelection,
                      onClick: () => selectedRow && router.push(`/dashboard/stock-purchases/${selectedRow.numericId}?mode=view`),
                    },
                    {
                      id: "edit", label: "Edit", icon: Pencil, enabled: canEdit,
                      disabledReason: !canEdit ? "Only draft purchases can be edited." : undefined,
                      onClick: () => selectedRow && canEdit && router.push(`/dashboard/stock-purchases/${selectedRow.numericId}?mode=edit`),
                    },
                    {
                      id: "delete", label: "Delete", icon: Trash2, variant: "destructive", enabled: canDelete,
                      disabledReason: !canDelete ? "Only draft purchases can be deleted." : undefined,
                      onClick: () => selectedRow && canDelete && setDeleteTarget(selectedRow),
                    },
                    {
                      id: "submit", label: "Submit", icon: Send, enabled: canSubmit,
                      disabledReason: !canSubmit ? "Only draft purchases can be submitted." : undefined,
                      onClick: async () => {
                        if (!selectedRow || !canSubmit) return;
                        try {
                          await submitStockPurchase(selectedRow.numericId);
                          toast.success("Submitted for approval");
                          void load();
                        } catch (err) { toast.error(toApiFormError(err, "Submit failed").message); }
                      },
                    },
                    {
                      id: "approve", label: "Approve", icon: Check, enabled: canApprove,
                      disabledReason: !canApprove ? "Only pending purchases can be approved." : undefined,
                      onClick: () => selectedRow && canApprove && setWorkflowConfirm({ kind: "approve", numericId: selectedRow.numericId }),
                    },
                    {
                      id: "reject", label: "Reject", icon: X, variant: "outline", enabled: canReject,
                      disabledReason: !canReject ? "Only pending purchases can be rejected." : undefined,
                      onClick: () => selectedRow && canReject && setWorkflowConfirm({ kind: "reject", numericId: selectedRow.numericId }),
                    },
                    {
                      id: "pay", label: "Ödəniş", icon: Wallet, enabled: canPay,
                      disabledReason: !canPay ? "Yalnız borcu olan təsdiqlənmiş alışa ödəniş yazılır." : undefined,
                      onClick: () => {
                        if (!selectedRow || !canPay) return;
                        setPayTarget({ numericId: selectedRow.numericId, documentNo: selectedRow.documentNo, remaining });
                        setPayAmount(remaining.toFixed(2));
                        setPayNote("");
                      },
                    },
                    {
                      id: "return", label: "Qaytar", icon: Undo2, variant: "outline", enabled: canReturn,
                      disabledReason: !canReturn ? "Yalnız təsdiqlənmiş alışdan, qaytarıla bilən mal qalıbsa qaytarmaq olar." : undefined,
                      onClick: () => {
                        if (!selectedRow || !canReturn) return;
                        setReturnTarget(selectedRow.raw);
                        setReturnQty({});
                        setReturnNote("");
                      },
                    },
                    {
                      id: "cancel", label: "Ləğv et", icon: Ban, variant: "outline", enabled: canCancel,
                      disabledReason: !canCancel ? "Yalnız təsdiqlənmiş alış ləğv edilə bilər." : undefined,
                      onClick: () => selectedRow && canCancel && setWorkflowConfirm({ kind: "cancel", numericId: selectedRow.numericId }),
                    },
                  ]}
                />

                <div className="rounded-lg border border-border overflow-x-auto">
                  <table className="w-full text-sm leading-[1.2] [&_td]:align-top [&_th]:align-top">
                    <thead>
                      <tr className="border-b border-border bg-muted/40">
                        <th className="w-[1%] py-2 px-3 font-medium" aria-label="Select" />
                        <th className="text-left py-2 px-3 font-medium">Document</th>
                        <th className="text-left py-2 px-3 font-medium">Date</th>
                        <th className="text-left py-2 px-3 font-medium">Konturagent</th>
                        <th className="text-left py-2 px-3 font-medium">Source</th>
                        <th className="text-left py-2 px-3 font-medium">Currency</th>
                        <th className="text-right py-2 px-3 font-medium">Exchange Rate</th>
                        <th className="text-right py-2 px-3 font-medium">Total (foreign)</th>
                        <th className="text-right py-2 px-3 font-medium">Total (AZN)</th>
                        <th className="text-right py-2 px-3 font-medium">Ödənilən</th>
                        <th className="text-right py-2 px-3 font-medium">Qalan borc</th>
                        <th className="text-left py-2 px-3 font-medium">Warehouse</th>
                        <th className="text-left py-2 px-3 font-medium">Status</th>
                      </tr>
                    </thead>
                    <tbody>
                      {slice.length === 0 ? (
                        <tr>
                          <td colSpan={13} className="py-10 text-center text-muted-foreground">
                            No purchases match your filters.
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
                            >
                              <td className="py-2 px-3" onClick={(e) => e.stopPropagation()}>
                                <Checkbox
                                  checked={rowSelected}
                                  onCheckedChange={(checked) => setSelectedRowId(checked === true ? row.id : null)}
                                  aria-label={`Select ${row.documentNo}`}
                                />
                              </td>
                              <td className="py-2 px-3 font-mono text-xs">{row.documentNo}</td>
                              <td className="py-2 px-3 text-muted-foreground">{row.purchaseDateLabel}</td>
                              <td className="py-2 px-3">{row.counterpartyName}</td>
                              <td className="py-2 px-3">
                                <Badge variant={row.isImport ? "default" : "secondary"}>
                                  {row.isImport ? "Import" : "Local"}
                                </Badge>
                              </td>
                              <td className="py-2 px-3 font-medium">{row.currencyCode}</td>
                              <td className="py-2 px-3 text-right tabular-nums">
                                {row.currencyCode === "AZN" ? "—" : row.exchangeRate.toFixed(4)}
                              </td>
                              <td className="py-2 px-3 text-right tabular-nums">
                                {row.currencyCode === "AZN"
                                  ? "—"
                                  : `${row.totalForeign.toLocaleString("az-AZ", { minimumFractionDigits: 2 })} ${row.currencyCode}`}
                              </td>
                              <td className="py-2 px-3 text-right tabular-nums font-medium">
                                {formatAzn(row.totalAzn)}
                                {(row.raw.returnedAzn ?? 0) > 0 && (
                                  <span className="block text-xs font-normal text-muted-foreground">
                                    qaytarılan: {formatAzn(row.raw.returnedAzn ?? 0)}
                                  </span>
                                )}
                              </td>
                              <td className="py-2 px-3 text-right tabular-nums">
                                {row.status === StockPurchaseStatus.Completed ? formatAzn(row.raw.paidAmountAzn ?? 0) : "—"}
                              </td>
                              <td
                                className={cn(
                                  "py-2 px-3 text-right tabular-nums font-medium",
                                  (row.raw.remainingAzn ?? 0) > 0 && "text-red-600",
                                )}
                              >
                                {row.status === StockPurchaseStatus.Completed ? formatAzn(row.raw.remainingAzn ?? 0) : "—"}
                              </td>
                              <td className="py-2 px-3">{row.warehouseName}</td>
                              <td className="py-2 px-3"><StatusBadge status={row.status} /></td>
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
                      Showing {(current - 1) * ITEMS_PER_PAGE + 1}–
                      {Math.min(current * ITEMS_PER_PAGE, sorted.length)} of {sorted.length}
                    </span>
                    <div className="flex gap-2">
                      <Button type="button" variant="outline" size="sm" disabled={current <= 1}
                        onClick={() => setPage((p) => Math.max(1, p - 1))}>Previous</Button>
                      <Button type="button" variant="outline" size="sm" disabled={current >= totalPages}
                        onClick={() => setPage((p) => Math.min(totalPages, p + 1))}>Next</Button>
                    </div>
                  </div>
                ) : null}
              </div>
            );
          }}
        </AdvancedTableFilters>
      )}

      <Dialog open={returnTarget != null} onOpenChange={(o) => !o && !returning && setReturnTarget(null)}>
        <DialogContent className="sm:max-w-lg">
          <DialogHeader>
            <DialogTitle>Təchizatçıya qaytarma</DialogTitle>
            <DialogDescription>
              {returnTarget?.documentNo} — qaytarılan mal anbardan çıxır, alışın dəyəri və borc azalır.
            </DialogDescription>
          </DialogHeader>
          <form
            className="space-y-4"
            onSubmit={async (e) => {
              e.preventDefault();
              if (!returnTarget) return;
              const lines = returnTarget.lines
                .map((l) => ({ stockPurchaseLineId: l.id, quantity: Number((returnQty[l.id] ?? "").replace(",", ".")) }))
                .filter((l) => Number.isFinite(l.quantity) && l.quantity > 0);
              if (lines.length === 0) {
                toast.error("Qaytarılacaq miqdarı yazın.");
                return;
              }
              const tooMuch = returnTarget.lines.find((l) => {
                const q = Number((returnQty[l.id] ?? "").replace(",", "."));
                return q > (l.returnableQuantity ?? l.quantity) + 0.0001;
              });
              if (tooMuch) {
                toast.error(`"${tooMuch.stockItemName}" üçün ən çox ${tooMuch.returnableQuantity ?? tooMuch.quantity} qaytarmaq olar.`);
                return;
              }
              setReturning(true);
              try {
                const message = await returnStockPurchase(returnTarget.id, lines, returnNote.trim() || null);
                toast.success(message ?? "Mal qaytarıldı");
                setReturnTarget(null);
                void load();
              } catch (err) {
                toast.error(toApiFormError(err, "Qaytarma yazılmadı").message);
              } finally {
                setReturning(false);
              }
            }}
          >
            <div className="max-h-72 overflow-y-auto rounded border">
              <table className="w-full text-sm">
                <thead>
                  <tr className="bg-muted/40 text-left text-xs text-muted-foreground">
                    <th className="px-3 py-2 font-medium">Məhsul</th>
                    <th className="px-3 py-2 text-right font-medium">Qaytarıla bilər</th>
                    <th className="px-3 py-2 text-right font-medium">Qaytarılır</th>
                  </tr>
                </thead>
                <tbody>
                  {(returnTarget?.lines ?? []).map((l) => {
                    const max = l.returnableQuantity ?? l.quantity;
                    return (
                      <tr key={l.id} className="border-t">
                        <td className="px-3 py-2">
                          {l.stockItemName}
                          <span className="block text-xs text-muted-foreground">
                            alınıb: {l.quantity} × {formatAzn(l.unitPriceAzn)}
                          </span>
                        </td>
                        <td className="px-3 py-2 text-right tabular-nums">{max}</td>
                        <td className="px-3 py-2 text-right">
                          <Input
                            className="ml-auto h-8 w-24 text-right"
                            inputMode="decimal"
                            disabled={max <= 0}
                            value={returnQty[l.id] ?? ""}
                            onChange={(e) => setReturnQty((prev) => ({ ...prev, [l.id]: e.target.value }))}
                          />
                        </td>
                      </tr>
                    );
                  })}
                </tbody>
              </table>
            </div>
            <div className="space-y-2">
              <Label htmlFor="return-note">Qeyd (istəyə bağlı)</Label>
              <Input id="return-note" value={returnNote} onChange={(e) => setReturnNote(e.target.value)} />
            </div>
            <DialogFooter>
              <Button type="button" variant="outline" disabled={returning} onClick={() => setReturnTarget(null)}>
                Ləğv et
              </Button>
              <Button type="submit" disabled={returning}>
                {returning ? "Yazılır…" : "Qaytar"}
              </Button>
            </DialogFooter>
          </form>
        </DialogContent>
      </Dialog>

      <Dialog open={payTarget != null} onOpenChange={(o) => !o && !paying && setPayTarget(null)}>
        <DialogContent className="sm:max-w-sm">
          <DialogHeader>
            <DialogTitle>Təchizatçıya ödəniş</DialogTitle>
            <DialogDescription>
              {payTarget?.documentNo} — qalan borc: {payTarget ? formatAzn(payTarget.remaining) : ""}
            </DialogDescription>
          </DialogHeader>
          <form
            className="space-y-4"
            onSubmit={async (e) => {
              e.preventDefault();
              if (!payTarget) return;
              const amount = Number(payAmount.replace(",", "."));
              if (!Number.isFinite(amount) || amount <= 0) {
                toast.error("Məbləğ düzgün deyil.");
                return;
              }
              if (amount > payTarget.remaining + 0.001) {
                toast.error(`Ödəniş qalan borcdan (${formatAzn(payTarget.remaining)}) çox ola bilməz.`);
                return;
              }
              setPaying(true);
              try {
                const message = await payStockPurchase(payTarget.numericId, amount, payNote.trim() || null);
                toast.success(message ?? "Ödəniş yazıldı");
                setPayTarget(null);
                void load();
              } catch (err) {
                toast.error(toApiFormError(err, "Ödəniş yazılmadı").message);
              } finally {
                setPaying(false);
              }
            }}
          >
            <div className="space-y-2">
              <Label htmlFor="pay-amount">Məbləğ (AZN)</Label>
              <Input
                id="pay-amount"
                inputMode="decimal"
                autoFocus
                value={payAmount}
                onChange={(e) => setPayAmount(e.target.value)}
                onFocus={(e) => e.currentTarget.select()}
              />
            </div>
            <div className="space-y-2">
              <Label htmlFor="pay-note">Qeyd (istəyə bağlı)</Label>
              <Input id="pay-note" value={payNote} onChange={(e) => setPayNote(e.target.value)} />
            </div>
            <DialogFooter>
              <Button type="button" variant="outline" disabled={paying} onClick={() => setPayTarget(null)}>
                Ləğv et
              </Button>
              <Button type="submit" disabled={paying}>
                {paying ? "Yazılır…" : "Ödəniş yaz"}
              </Button>
            </DialogFooter>
          </form>
        </DialogContent>
      </Dialog>

      <ConfirmDialog
        open={deleteTarget != null}
        onOpenChange={(o) => !o && setDeleteTarget(null)}
        title="Delete stock purchase?"
        description="Bu draft sənədi silinəcək."
        confirmLabel="Delete"
        destructive
        onConfirm={async () => {
          if (!deleteTarget) return;
          try {
            await deleteStockPurchase(deleteTarget.numericId);
            toast.success("Deleted");
            setDeleteTarget(null);
            setSelectedRowId(null);
            void load();
          } catch (e) { toast.error(toApiFormError(e, "Delete failed").message); }
        }}
      />

      <ConfirmDialog
        open={workflowConfirm != null}
        onOpenChange={(o) => !o && setWorkflowConfirm(null)}
        title={
          workflowConfirm?.kind === "approve" ? "Approve purchase?"
          : workflowConfirm?.kind === "cancel" ? "Alış ləğv edilsin?"
          : "Reject purchase?"
        }
        description={
          workflowConfirm?.kind === "approve"
            ? "Alış təsdiqlənəcək və anbar stoku artırılacaq."
            : workflowConfirm?.kind === "cancel"
              ? "Alış ləğv ediləcək və miqdarlar anbardan çıxarılacaq. Mal artıq satılıbsa və ya istifadə olunubsa, ləğv mümkün olmaz."
              : "Alış rədd ediləcək."
        }
        confirmLabel={
          workflowConfirm?.kind === "approve" ? "Approve"
          : workflowConfirm?.kind === "cancel" ? "Ləğv et"
          : "Reject"
        }
        destructive={workflowConfirm?.kind === "reject" || workflowConfirm?.kind === "cancel"}
        onConfirm={async () => {
          if (!workflowConfirm) return;
          const { kind, numericId } = workflowConfirm;
          try {
            if (kind === "approve") { await approveStockPurchase(numericId); toast.success("Approved"); }
            else if (kind === "cancel") { toast.success((await cancelStockPurchase(numericId)) ?? "Ləğv edildi"); }
            else { await rejectStockPurchase(numericId); toast.success("Rejected"); }
            setWorkflowConfirm(null);
            void load();
          } catch (e) { toast.error(toApiFormError(e, kind === "approve" ? "Approve failed" : kind === "cancel" ? "Ləğv alınmadı" : "Reject failed").message); }
        }}
      />
    </div>
  );
}
