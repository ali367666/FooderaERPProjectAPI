"use client";

import { useCallback, useEffect, useMemo, useState } from "react";
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
import { getOrders, getOrderReceipt, deleteOrder, type OrderDto, type OrderReceiptDto } from "@/lib/services/order-service";
import { getPosTerminalContext, type PosTerminalContext } from "@/lib/pos-terminal-client";
import { useHasPermission } from "@/hooks/use-auth-permissions";
import { PosEmpty, PosPage, PosPageLoading, PosTileGrid, posTileClass } from "@/components/pos/pos-page";

export default function PosOrderHistoryPage() {
  const [terminal, setTerminal] = useState<PosTerminalContext | null | undefined>(undefined);
  const [orders, setOrders] = useState<OrderDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [search, setSearch] = useState("");
  const [fromDate, setFromDate] = useState("");
  const [toDate, setToDate] = useState("");
  const [receipt, setReceipt] = useState<OrderReceiptDto | null>(null);
  const [receiptOrderId, setReceiptOrderId] = useState<number | null>(null);
  const [busyId, setBusyId] = useState<number | null>(null);

  const canDeleteReceipt = useHasPermission("Pos.DeleteReceipt");

  useEffect(() => {
    setTerminal(getPosTerminalContext());
  }, []);

  const load = useCallback(async () => {
    if (!terminal?.restaurantId) return;
    setLoading(true);
    try {
      const all = await getOrders();
      setOrders(
        all
          .filter((o) => o.restaurantId === terminal.restaurantId && o.status === "paid")
          .sort((a, b) => (b.paidAt ?? "").localeCompare(a.paidAt ?? "")),
      );
    } catch (err) {
      toast.error(err instanceof Error ? err.message : "Sifarişlər yüklənə bilmədi");
    } finally {
      setLoading(false);
    }
  }, [terminal]);

  useEffect(() => {
    if (terminal === undefined) return;
    void load();
  }, [terminal, load]);

  const filtered = useMemo(() => {
    const q = search.trim().toLowerCase();
    return orders.filter((o) => {
      if (q) {
        const matchesSearch =
          o.orderNumber.toLowerCase().includes(q) ||
          o.receiptNumber?.toLowerCase().includes(q) ||
          o.tableName?.toLowerCase().includes(q);
        if (!matchesSearch) return false;
      }
      if (o.paidAt) {
        const paidDate = o.paidAt.slice(0, 10);
        if (fromDate && paidDate < fromDate) return false;
        if (toDate && paidDate > toDate) return false;
      } else if (fromDate || toDate) {
        return false;
      }
      return true;
    });
  }, [orders, search, fromDate, toDate]);

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

  if (loading || terminal === undefined) return <PosPageLoading />;

  return (
    <PosPage
      title="Köhnə qəbzlər"
      actions={
        <>
          <Input
            type="date"
            className="w-40"
            value={fromDate}
            onChange={(e) => setFromDate(e.target.value)}
            aria-label="Tarixdən"
          />
          <span className="text-sm text-muted-foreground">—</span>
          <Input
            type="date"
            className="w-40"
            value={toDate}
            onChange={(e) => setToDate(e.target.value)}
            aria-label="Tarixə qədər"
          />
          {(fromDate || toDate) && (
            <Button variant="ghost" size="sm" onClick={() => { setFromDate(""); setToDate(""); }}>
              Təmizlə
            </Button>
          )}
          <div className="relative w-64">
            <Search className="absolute left-2 top-2.5 h-4 w-4 text-muted-foreground" />
            <Input className="pl-8" placeholder="Sifariş/qəbz nömrəsi, masa…" value={search} onChange={(e) => setSearch(e.target.value)} />
          </div>
        </>
      }
    >
      {filtered.length === 0 && <PosEmpty>Sifariş tapılmadı</PosEmpty>}

      <PosTileGrid wide>
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
      </PosTileGrid>

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
