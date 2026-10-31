"use client";

import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import { toast } from "sonner";
import { addDays, parseISO } from "date-fns";
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
import { getOrders, getOrderReceipt, deleteOrder, type OrderDto, type OrderReceiptDto } from "@/lib/services/order-service";
import { getPosTerminalContext, type PosTerminalContext } from "@/lib/pos-terminal-client";
import { useHasPermission } from "@/hooks/use-auth-permissions";
import { PosEmpty, PosPage, PosPageLoading, PosTileGrid, posTileClass } from "@/components/pos/pos-page";
import { PosDateRangeFilter, formatRange, todayRange, type PosDateRange } from "@/components/pos/pos-date-range-filter";

export default function PosOrderHistoryPage() {
  const [terminal, setTerminal] = useState<PosTerminalContext | null | undefined>(undefined);
  const [orders, setOrders] = useState<OrderDto[]>([]);
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
      const all = await getOrders({ status: "Paid" });
      // Dates changed quickly: an older answer must not replace the newer range's list.
      if (requestId !== loadSeq.current) return;
      // The date range is the terminal's local days: [from 00:00, day after "to" 00:00).
      const fromMs = parseISO(range.from).getTime();
      const toMs = addDays(parseISO(range.to), 1).getTime();
      setOrders(
        all
          .filter((o) => o.restaurantId === terminal.restaurantId && o.status === "paid")
          .filter((o) => {
            if (!o.paidAt) return false;
            const paidMs = new Date(o.paidAt).getTime();
            return paidMs >= fromMs && paidMs < toMs;
          })
          .sort((a, b) => (b.paidAt ?? "").localeCompare(a.paidAt ?? "")),
      );
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
        o.tableName?.toLowerCase().includes(q),
    );
  }, [orders, search]);

  const handleView = async (order: OrderDto) => {
    try {
      const r = await getOrderReceipt(order.id);
      setReceiptOrderId(order.id);
      setReceipt(r);
    } catch (err) {
      toast.error(err instanceof Error ? err.message : "Qəbz yüklənmədi");
    }
  };

  const handleDelete = async (order: OrderDto) => {
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
      title="Köhnə qəbzlər"
      actions={
        <div className="relative w-64">
          <Search className="absolute left-2 top-2.5 h-4 w-4 text-muted-foreground" />
          <Input className="pl-8" placeholder="Sifariş/qəbz nömrəsi, masa…" value={search} onChange={(e) => setSearch(e.target.value)} />
        </div>
      }
    >
      <PosDateRangeFilter value={range} onChange={setRange} />

      {loading && <PosPageLoading />}

      {!loading && filtered.length === 0 && (
        <PosEmpty>{search.trim() ? "Sifariş tapılmadı" : `${formatRange(range)} üçün qəbz yoxdur.`}</PosEmpty>
      )}

      {!loading && <PosTileGrid wide>
        {filtered.map((o) => (
          <div key={o.id} className={posTileClass()}>
            <span className="max-w-full truncate text-lg font-bold leading-none">{o.orderNumber}</span>
            {o.receiptNumber && <span className="text-xs text-muted-foreground">Qəbz: {o.receiptNumber}</span>}
            {o.tableName && <span className="max-w-full truncate text-xs text-muted-foreground">{o.tableName}</span>}
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
