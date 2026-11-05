"use client";

import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import { toast } from "sonner";
import { Printer, Search, Trash2 } from "lucide-react";
import { BarcodeSvg } from "@/components/barcode-svg";
import { receiptBarcodeValue } from "@/lib/receipt-barcode";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import {
  getReceipts,
  getOrderReceipt,
  deleteOrder,
  type ReceiptListItemDto,
  type OrderReceiptDto,
} from "@/lib/services/order-service";
import { getPosTerminalContext, type PosTerminalContext } from "@/lib/pos-terminal-client";
import { useHasPermission } from "@/hooks/use-auth-permissions";
import { PosEmpty, PosPage, PosPageLoading, PosTileGrid, posTileClass } from "@/components/pos/pos-page";
import { PosDateRangeFilter, formatRange, todayRange, type PosDateRange } from "@/components/pos/pos-date-range-filter";

export default function PosOrderHistoryPage() {
  const [terminal, setTerminal] = useState<PosTerminalContext | null | undefined>(undefined);
  const [orders, setOrders] = useState<ReceiptListItemDto[]>([]);
  const [total, setTotal] = useState(0);
  const [loading, setLoading] = useState(true);
  const [search, setSearch] = useState("");
  const [range, setRange] = useState<PosDateRange>(todayRange);
  const [receipt, setReceipt] = useState<OrderReceiptDto | null>(null);
  const [receiptOrderId, setReceiptOrderId] = useState<number | null>(null);
  const [busyId, setBusyId] = useState<number | null>(null);
  const loadSeq = useRef(0);

  const canDeleteReceipt = useHasPermission("Pos.DeleteReceipt");

  useEffect(() => {
    setTerminal(getPosTerminalContext());
  }, []);

  const load = useCallback(async () => {
    if (!terminal?.restaurantId) return;
    const requestId = ++loadSeq.current;
    setLoading(true);
    try {
      // The server filters by branch and paid time and follows the company's business day
      // (06:00 → 06:00 in shifts, the calendar day otherwise). "Bu gün" goes as the current business day,
      // so a sale rung up after midnight in a shift still shows under today.
      const today = todayRange();
      const isToday = range.from === today.from && range.to === today.to;
      const result = await getReceipts({
        today: isToday,
        from: range.from,
        to: range.to,
        restaurantId: terminal.restaurantId,
      });
      // Dates changed quickly: an older answer must not replace the newer range's list.
      if (requestId !== loadSeq.current) return;
      setOrders(result.items);
      setTotal(result.total);
    } catch (err) {
      if (requestId === loadSeq.current) toast.error(err instanceof Error ? err.message : "Sifarişlər yüklənə bilmədi");
    } finally {
      if (requestId === loadSeq.current) setLoading(false);
    }
  }, [terminal, range]);

  useEffect(() => {
    if (terminal === undefined) return;
    void load();
  }, [terminal, load]);

  const filtered = useMemo(() => {
    const q = search.trim().toLowerCase();
    if (!q) return orders;
    return orders.filter(
      (o) =>
        o.orderNumber.toLowerCase().includes(q) ||
        o.receiptNumber?.toLowerCase().includes(q) ||
        o.tableName?.toLowerCase().includes(q) ||
        o.waiterName?.toLowerCase().includes(q),
    );
  }, [orders, search]);

  const handleView = async (order: ReceiptListItemDto) => {
    try {
      const r = await getOrderReceipt(order.id);
      setReceiptOrderId(order.id);
      setReceipt(r);
    } catch (err) {
      toast.error(err instanceof Error ? err.message : "Qəbz yüklənmədi");
    }
  };

  const handleDelete = async (order: ReceiptListItemDto) => {
    if (!window.confirm(`${order.orderNumber} nömrəli qəbzi silmək istəyirsiniz?`)) return;
    setBusyId(order.id);
    try {
      await deleteOrder(order.id);
      toast.success("Qəbz silindi");
      await load();
    } catch (err) {
      toast.error(err instanceof Error ? err.message : "Qəbz silinmədi");
    } finally {
      setBusyId(null);
    }
  };

  // Loading shows inside the page so the date inputs stay on screen while a new range loads.
  if (terminal === undefined) return <PosPageLoading />;

  return (
    <PosPage
      title="Qəbzlər"
      actions={
        <div className="relative w-64">
          <Search className="absolute left-2 top-2.5 h-4 w-4 text-muted-foreground" />
          <Input className="pl-8" placeholder="Sifariş/qəbz nömrəsi, masa…" value={search} onChange={(e) => setSearch(e.target.value)} />
        </div>
      }
    >
      <PosDateRangeFilter value={range} onChange={setRange} />

      {!loading && orders.length > 0 && (
        <p className="text-sm text-muted-foreground">
          {orders.length} qəbz · Cəmi <span className="font-semibold tabular-nums text-foreground">{total.toFixed(2)} ₼</span>
        </p>
      )}

      {loading && <PosPageLoading />}

      {!loading && filtered.length === 0 && (
        <PosEmpty>{search.trim() ? "Sifariş tapılmadı" : `${formatRange(range)} üçün qəbz yoxdur.`}</PosEmpty>
      )}

      {!loading && <PosTileGrid wide>
        {filtered.map((o) => (
          <div key={o.id} className={posTileClass()}>
            <span className="max-w-full truncate text-lg font-bold leading-none">{o.orderNumber}</span>
            {o.receiptNumber && <span className="text-xs text-muted-foreground">Qəbz: {o.receiptNumber}</span>}
            {(o.tableName || o.waiterName) && (
              <span className="max-w-full truncate text-xs text-muted-foreground">
                {[o.tableName, o.waiterName].filter(Boolean).join(" · ")}
              </span>
            )}
            {o.paidAt && (
              <span className="text-[11px] tabular-nums text-muted-foreground">{new Date(o.paidAt).toLocaleString("az-AZ")}</span>
            )}
            <span className="text-sm font-semibold tabular-nums">{o.totalAmount.toFixed(2)} ₼</span>
            <div className="mt-2 flex flex-wrap justify-center gap-2">
              <Button size="sm" variant="outline" onClick={() => void handleView(o)}>
                <Printer className="mr-1 h-3.5 w-3.5" />
                Bax / Çap et
              </Button>
              {canDeleteReceipt && (
                <Button size="sm" variant="outline" className="text-destructive" disabled={busyId === o.id} onClick={() => void handleDelete(o)}>
                  <Trash2 className="h-3.5 w-3.5" />
                </Button>
              )}
            </div>
          </div>
        ))}
      </PosTileGrid>}

      <Dialog open={receipt !== null} onOpenChange={(o) => !o && setReceipt(null)}>
        <DialogContent className="sm:max-w-sm">
          <div id="history-receipt-print-area">
            <DialogHeader>
              <DialogTitle>Qəbz #{receipt?.receiptNumber}</DialogTitle>
              <DialogDescription>
                {receipt?.restaurantName} — {receipt?.tableName}
              </DialogDescription>
            </DialogHeader>
            <div className="space-y-1 text-sm">
              {receipt?.lines.map((line, i) => (
                <div key={i} className="flex justify-between">
                  <span>{line.quantity} × {line.menuItemName}</span>
                  <span>{line.lineTotal.toFixed(2)} ₼</span>
                </div>
              ))}
              <div className="mt-2 border-t pt-2 font-semibold">
                <div className="flex justify-between">
                  <span>Cəm</span>
                  <span>{receipt?.totalAmount.toFixed(2)} ₼</span>
                </div>
              </div>
              {receiptOrderId != null && (
                <div className="mt-3 flex justify-center">
                  <BarcodeSvg value={receiptBarcodeValue(receiptOrderId)} height={40} />
                </div>
              )}
            </div>
          </div>
          <DialogFooter>
            <Button variant="outline" onClick={() => window.print()}>
              <Printer className="mr-2 h-4 w-4" />
              Çap et
            </Button>
            <Button onClick={() => setReceipt(null)}>Bağla</Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      <style>{`
        @media print {
          body * { visibility: hidden; }
          #history-receipt-print-area, #history-receipt-print-area * { visibility: visible; }
          #history-receipt-print-area { position: fixed; top: 0; left: 0; width: 100%; padding: 16px; }
        }
      `}</style>
    </PosPage>
  );
}
