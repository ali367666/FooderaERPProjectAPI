"use client";

import { useCallback, useEffect, useState } from "react";
import { Plus, Trash2 } from "lucide-react";
import { toast } from "sonner";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from "@/components/ui/dialog";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Textarea } from "@/components/ui/textarea";
import { useStorePermissions as usePermissionSet } from "@/lib/store/use-store-permissions";
import { formatCurrency } from "@/lib/format-currency";
import { cn } from "@/lib/utils";
import { getWarehouses, type Warehouse } from "@/lib/services/warehouse-service";
import {
  StoreTransferStatus,
  TRANSFER_STATUS_LABELS,
  cancelStoreTransfer,
  getStoreTransfer,
  getStoreTransfers,
  receiveStoreTransfer,
  saveStoreTransfer,
  searchStoreProducts,
  searchStoreSerials,
  shipStoreTransfer,
  submitStoreTransfer,
  type StoreProduct,
  type StoreTransfer,
} from "@/lib/store/store-service";

const SELECT = "h-9 w-full rounded-md border border-input bg-background px-3 text-sm";
const num = (s: string) => Number(s.replace(",", ".")) || 0;
const fmtDate = (v: string) => new Date(v).toLocaleDateString("az-AZ");

type EditLine = { product: StoreProduct; quantity: string; serials: string };

function statusBadge(status: number) {
  const variant =
    status === StoreTransferStatus.Received
      ? "default"
      : status === StoreTransferStatus.Cancelled
        ? "destructive"
        : status === StoreTransferStatus.Shipped || status === StoreTransferStatus.PartiallyReceived
          ? "outline"
          : "secondary";
  return <Badge variant={variant}>{TRANSFER_STATUS_LABELS[status] ?? status}</Badge>;
}

export default function StoreTransfersPage() {
  const permissions = usePermissionSet();
  const canManage = permissions.has("StoreTransfer.Manage");
  const canShip = permissions.has("StoreTransfer.Ship");
  const canReceive = permissions.has("StoreTransfer.Receive");

  const [warehouses, setWarehouses] = useState<Warehouse[]>([]);
  const [statusFilter, setStatusFilter] = useState<number | "">("");
  const [list, setList] = useState<StoreTransfer[]>([]);
  const [loading, setLoading] = useState(true);

  const [editOpen, setEditOpen] = useState(false);
  const [editingId, setEditingId] = useState<number | null>(null);
  const [fromId, setFromId] = useState<number | "">("");
  const [toId, setToId] = useState<number | "">("");
  const [note, setNote] = useState("");
  const [lines, setLines] = useState<EditLine[]>([]);
  const [term, setTerm] = useState("");
  const [options, setOptions] = useState<StoreProduct[]>([]);
  const [saving, setSaving] = useState(false);
  const [availableSerials, setAvailableSerials] = useState<Record<number, string[]>>({});

  const [detail, setDetail] = useState<StoreTransfer | null>(null);
  const [recvQty, setRecvQty] = useState<Record<number, string>>({});
  const [recvSerials, setRecvSerials] = useState<Record<number, string[]>>({});
  const [busy, setBusy] = useState(false);

  useEffect(() => {
    getWarehouses().then(setWarehouses).catch(() => setWarehouses([]));
  }, []);

  const load = useCallback(async () => {
    setLoading(true);
    try {
      setList(await getStoreTransfers({ status: statusFilter === "" ? null : statusFilter }));
    } catch (e) {
      toast.error(e instanceof Error ? e.message : "Yüklənmədi.");
    } finally {
      setLoading(false);
    }
  }, [statusFilter]);

  useEffect(() => {
    void load();
  }, [load]);

  useEffect(() => {
    if (!editOpen || term.trim().length < 1) {
      setOptions([]);
      return;
    }
    const t = setTimeout(() => {
      searchStoreProducts({ q: term.trim(), pageSize: 8, isActive: true, warehouseId: fromId === "" ? null : fromId })
        .then((r) => setOptions(r.items))
        .catch(() => setOptions([]));
    }, 200);
    return () => clearTimeout(t);
  }, [editOpen, term, fromId]);

  const openNew = () => {
    setEditingId(null);
    setFromId("");
    setToId("");
    setNote("");
    setLines([]);
    setTerm("");
    setAvailableSerials({});
    setEditOpen(true);
  };

  const openEdit = (t: StoreTransfer) => {
    setEditingId(t.id);
    setFromId(t.fromWarehouseId);
    setToId(t.toWarehouseId);
    setNote(t.note ?? "");
    setLines(
      t.lines.map((l) => ({
        product: { id: l.productId, name: l.productName, tracksSerial: l.tracksSerial, stock: null } as StoreProduct,
        quantity: String(l.quantity),
        serials: l.serialNumbers.join("\n"),
      })),
    );
    setTerm("");
    setAvailableSerials({});
    setDetail(null);
    setEditOpen(true);
  };

  const addProduct = async (p: StoreProduct) => {
    setLines((prev) => (prev.some((l) => l.product.id === p.id) ? prev : [...prev, { product: p, quantity: "1", serials: "" }]));
    setTerm("");
    if (p.tracksSerial && fromId !== "") {
      try {
        const units = await searchStoreSerials({ productId: p.id, warehouseId: fromId, status: 1 });
        setAvailableSerials((prev) => ({ ...prev, [p.id]: units.map((u) => u.serialNumber) }));
      } catch {
        // the numbers can still be typed
      }
    }
  };

  const submitDraft = async () => {
    if (fromId === "" || toId === "") {
      toast.error("Göndərən və qəbul edən anbarı seçin.");
      return;
    }
    if (lines.length === 0) {
      toast.error("Ən azı bir məhsul əlavə edin.");
      return;
    }
    setSaving(true);
    try {
      const saved = await saveStoreTransfer(editingId, {
        fromWarehouseId: fromId,
        toWarehouseId: toId,
        note: note.trim() || null,
        lines: lines.map((l) => {
          const serials = l.product.tracksSerial ? l.serials.split(/[\n,;]+/).map((x) => x.trim()).filter(Boolean) : null;
          return { productId: l.product.id, quantity: serials ? serials.length : num(l.quantity), serialNumbers: serials };
        }),
      });
      toast.success(`Transfer yazıldı: ${saved.documentNo}`);
      setEditOpen(false);
      setDetail(saved);
      void load();
    } catch (e) {
      toast.error(e instanceof Error ? e.message : "Yazılmadı.");
    } finally {
      setSaving(false);
    }
  };

  const openDetail = async (id: number) => {
    try {
      setDetail(await getStoreTransfer(id));
      setRecvQty({});
      setRecvSerials({});
    } catch (e) {
      toast.error(e instanceof Error ? e.message : "Yüklənmədi.");
    }
  };

  const act = async (fn: () => Promise<StoreTransfer>, ok: string) => {
    setBusy(true);
    try {
      setDetail(await fn());
      toast.success(ok);
      void load();
    } catch (e) {
      toast.error(e instanceof Error ? e.message : "Əməliyyat alınmadı.");
    } finally {
      setBusy(false);
    }
  };

  const receive = (closeWithShortage: boolean) => {
    if (!detail) return Promise.resolve();
    const body = detail.lines
      .map((l) => ({
        lineId: l.id,
        quantity: l.tracksSerial ? (recvSerials[l.id]?.length ?? 0) : num(recvQty[l.id] ?? ""),
        serialNumbers: l.tracksSerial ? (recvSerials[l.id] ?? []) : null,
      }))
      .filter((l) => l.quantity > 0);
    return act(
      () => receiveStoreTransfer(detail.id, { lines: body, closeWithShortage }),
      closeWithShortage ? "Transfer çatışmazlıqla bağlandı" : "Qəbul yazıldı",
    );
  };

  const inTransitTotal = list.reduce((s, t) => s + t.inTransitValue, 0);

  return (
    <div className="mx-auto max-w-6xl space-y-4 p-4">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div>
          <h1 className="text-2xl font-bold">Anbarlar arası transfer</h1>
          <p className="text-sm text-muted-foreground">Göndərilən mal yolda sayılır; təyinat anbarı qəbul edəndə onun stokuna yazılır.</p>
        </div>
        {canManage && (
          <Button onClick={openNew}>
            <Plus className="mr-1 h-4 w-4" />
            Yeni transfer
          </Button>
        )}
      </div>

      <div className="flex flex-wrap items-center gap-3">
        <select
          className="h-9 rounded-md border border-input bg-background px-3 text-sm"
          value={statusFilter}
          onChange={(e) => setStatusFilter(e.target.value === "" ? "" : Number(e.target.value))}
        >
          <option value="">Bütün statuslar</option>
          {Object.entries(TRANSFER_STATUS_LABELS).map(([k, label]) => (
            <option key={k} value={k}>
              {label}
            </option>
          ))}
        </select>
        {inTransitTotal > 0 && (
          <span className="text-sm text-muted-foreground">
            Yolda olan malın dəyəri: <span className="font-semibold text-foreground">{formatCurrency(inTransitTotal)}</span>
          </span>
        )}
      </div>

      <div className="overflow-x-auto rounded-lg border bg-card">
        <table className="w-full text-sm">
          <thead>
            <tr className="bg-muted/40 text-left text-xs text-muted-foreground">
              <th className="px-3 py-2 font-medium">Sənəd</th>
              <th className="px-3 py-2 font-medium">Tarix</th>
              <th className="px-3 py-2 font-medium">Haradan</th>
              <th className="px-3 py-2 font-medium">Hara</th>
              <th className="px-3 py-2 font-medium">Status</th>
              <th className="px-3 py-2 text-right font-medium">Sətir</th>
              <th className="px-3 py-2 text-right font-medium">Yolda (AZN)</th>
            </tr>
          </thead>
          <tbody>
            {loading ? (
              <tr>
                <td colSpan={7} className="px-3 py-8 text-center text-muted-foreground">
                  Yüklənir…
                </td>
              </tr>
            ) : list.length === 0 ? (
              <tr>
                <td colSpan={7} className="px-3 py-8 text-center text-muted-foreground">
                  Transfer yoxdur.
                </td>
              </tr>
            ) : (
              list.map((t) => (
                <tr key={t.id} className="cursor-pointer border-t hover:bg-muted/40" onClick={() => void openDetail(t.id)}>
                  <td className="px-3 py-2 font-medium">{t.documentNo}</td>
                  <td className="px-3 py-2">{fmtDate(t.createdAtUtc)}</td>
                  <td className="px-3 py-2">{t.fromWarehouseName}</td>
                  <td className="px-3 py-2">{t.toWarehouseName}</td>
                  <td className="px-3 py-2">{statusBadge(t.status)}</td>
                  <td className="px-3 py-2 text-right">{t.lineCount}</td>
                  <td className="px-3 py-2 text-right">{t.inTransitValue > 0 ? formatCurrency(t.inTransitValue) : "—"}</td>
                </tr>
              ))
            )}
          </tbody>
        </table>
      </div>

      {/* Editor */}
      <Dialog open={editOpen} onOpenChange={(o) => !o && !saving && setEditOpen(false)}>
        <DialogContent className="max-h-[92vh] overflow-y-auto sm:max-w-3xl">
          <DialogHeader>
            <DialogTitle>{editingId ? "Transferi dəyiş" : "Yeni transfer"}</DialogTitle>
            <DialogDescription>Göndərənin stokunda olan məhsullar görünür.</DialogDescription>
          </DialogHeader>
          <div className="space-y-3">
            <div className="grid gap-3 sm:grid-cols-2">
              <div className="space-y-1">
                <Label htmlFor="tr-from">Haradan</Label>
                <select id="tr-from" className={SELECT} value={fromId} onChange={(e) => setFromId(e.target.value ? Number(e.target.value) : "")}>
                  <option value="">Seçin…</option>
                  {warehouses.map((w) => (
                    <option key={w.id} value={w.id}>
                      {w.name}
                    </option>
                  ))}
                </select>
              </div>
              <div className="space-y-1">
                <Label htmlFor="tr-to">Hara</Label>
                <select id="tr-to" className={SELECT} value={toId} onChange={(e) => setToId(e.target.value ? Number(e.target.value) : "")}>
                  <option value="">Seçin…</option>
                  {warehouses.filter((w) => w.id !== fromId).map((w) => (
                    <option key={w.id} value={w.id}>
                      {w.name}
                    </option>
                  ))}
                </select>
              </div>
            </div>
            <Input placeholder="Qeyd" value={note} onChange={(e) => setNote(e.target.value)} />
            <Input placeholder="Məhsul əlavə et: ad, SKU və ya barkod…" value={term} onChange={(e) => setTerm(e.target.value)} />
            {options.length > 0 && (
              <div className="max-h-40 overflow-y-auto rounded-md border">
                {options.map((p) => (
                  <button type="button" key={p.id} className="flex w-full justify-between px-3 py-1.5 text-left text-sm hover:bg-muted" onClick={() => void addProduct(p)}>
                    <span>{p.name}</span>
                    {p.stock != null && <span className="text-xs text-muted-foreground">stokda: {p.stock}</span>}
                  </button>
                ))}
              </div>
            )}
            {lines.map((l) => (
              <div key={l.product.id} className="flex items-start gap-2 rounded-md border p-2 text-sm">
                <span className="flex-1">
                  {l.product.name}
                  {l.product.tracksSerial && (
                    <>
                      <Textarea
                        className="mt-1 text-xs"
                        rows={2}
                        placeholder="Göndərilən IMEI / serial (hər sətirdə bir)"
                        value={l.serials}
                        onChange={(e) => setLines((prev) => prev.map((x) => (x.product.id === l.product.id ? { ...x, serials: e.target.value } : x)))}
                      />
                      {availableSerials[l.product.id]?.length ? (
                        <span className="text-xs text-muted-foreground">Stokda: {availableSerials[l.product.id].slice(0, 6).join(", ")}…</span>
                      ) : null}
                    </>
                  )}
                </span>
                {!l.product.tracksSerial && (
                  <Input
                    className="h-8 w-24 text-right"
                    inputMode="decimal"
                    value={l.quantity}
                    onChange={(e) => setLines((prev) => prev.map((x) => (x.product.id === l.product.id ? { ...x, quantity: e.target.value } : x)))}
                  />
                )}
                <Button size="icon" variant="ghost" className="h-8 w-8" onClick={() => setLines((prev) => prev.filter((x) => x.product.id !== l.product.id))}>
                  <Trash2 className="h-4 w-4" />
                </Button>
              </div>
            ))}
          </div>
          <DialogFooter>
            <Button variant="outline" disabled={saving} onClick={() => setEditOpen(false)}>
              Ləğv et
            </Button>
            <Button disabled={saving} onClick={() => void submitDraft()}>
              {saving ? "Yazılır…" : "Yaz"}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* Detail */}
      <Dialog open={detail != null} onOpenChange={(o) => !o && setDetail(null)}>
        <DialogContent className="max-h-[92vh] overflow-y-auto sm:max-w-3xl">
          <DialogHeader>
            <DialogTitle className="flex items-center gap-2">
              {detail?.documentNo} {detail && statusBadge(detail.status)}
            </DialogTitle>
            <DialogDescription>
              {detail?.fromWarehouseName} → {detail?.toWarehouseName}
              {detail?.shippedAtUtc && ` — göndərildi: ${detail.shippedByName ?? "—"}, ${fmtDate(detail.shippedAtUtc)}`}
              {detail?.receivedAtUtc && ` — qəbul: ${detail.receivedByName ?? "—"}, ${fmtDate(detail.receivedAtUtc)}`}
            </DialogDescription>
          </DialogHeader>
          {detail && (
            <div className="space-y-3">
              <div className="overflow-x-auto rounded-md border">
                <table className="w-full text-sm">
                  <thead>
                    <tr className="bg-muted/40 text-left text-xs text-muted-foreground">
                      <th className="px-2 py-2 font-medium">Məhsul</th>
                      <th className="px-2 py-2 text-right font-medium">İstənilən</th>
                      <th className="px-2 py-2 text-right font-medium">Göndərilən</th>
                      <th className="px-2 py-2 text-right font-medium">Qəbul edilən</th>
                      <th className="px-2 py-2 text-right font-medium">Yolda</th>
                      {(detail.status === StoreTransferStatus.Shipped || detail.status === StoreTransferStatus.PartiallyReceived) && canReceive && (
                        <th className="px-2 py-2 font-medium">İndi qəbul</th>
                      )}
                    </tr>
                  </thead>
                  <tbody>
                    {detail.lines.map((l) => (
                      <tr key={l.id} className="border-t align-top">
                        <td className="px-2 py-2">
                          {l.productName}
                          {l.serialNumbers.length > 0 && <span className="block text-xs text-muted-foreground">IMEI: {l.serialNumbers.join(", ")}</span>}
                          {l.shortageQuantity > 0 && <span className="block text-xs text-red-600">çatışmazlıq: {l.shortageQuantity}</span>}
                        </td>
                        <td className="px-2 py-2 text-right">{l.quantity}</td>
                        <td className="px-2 py-2 text-right">{l.shippedQuantity}</td>
                        <td className="px-2 py-2 text-right">{l.receivedQuantity}</td>
                        <td className={cn("px-2 py-2 text-right", l.inTransitQuantity > 0 && "font-medium text-amber-600")}>{l.inTransitQuantity}</td>
                        {(detail.status === StoreTransferStatus.Shipped || detail.status === StoreTransferStatus.PartiallyReceived) && canReceive && (
                          <td className="px-2 py-2">
                            {l.inTransitQuantity > 0 &&
                              (l.tracksSerial ? (
                                <div className="space-y-0.5">
                                  {l.serialNumbers
                                    .filter((s) => !l.receivedSerialNumbers.includes(s))
                                    .map((s) => (
                                      <label key={s} className="flex items-center gap-1 text-xs">
                                        <input
                                          type="checkbox"
                                          checked={recvSerials[l.id]?.includes(s) ?? false}
                                          onChange={(e) =>
                                            setRecvSerials((prev) => ({
                                              ...prev,
                                              [l.id]: e.target.checked ? [...(prev[l.id] ?? []), s] : (prev[l.id] ?? []).filter((x) => x !== s),
                                            }))
                                          }
                                        />
                                        {s}
                                      </label>
                                    ))}
                                </div>
                              ) : (
                                <Input
                                  className="h-8 w-24"
                                  inputMode="decimal"
                                  placeholder={`≤ ${l.inTransitQuantity}`}
                                  value={recvQty[l.id] ?? ""}
                                  onChange={(e) => setRecvQty((prev) => ({ ...prev, [l.id]: e.target.value }))}
                                />
                              ))}
                          </td>
                        )}
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>

              <div className="flex flex-wrap gap-2">
                {detail.status === StoreTransferStatus.Draft && canManage && (
                  <>
                    <Button variant="outline" onClick={() => openEdit(detail)}>
                      Dəyiş
                    </Button>
                    <Button variant="outline" disabled={busy} onClick={() => void act(() => submitStoreTransfer(detail.id), "Transfer göndərişə hazırdır")}>
                      Göndərişə təqdim et
                    </Button>
                  </>
                )}
                {(detail.status === StoreTransferStatus.Draft || detail.status === StoreTransferStatus.Pending) && canShip && (
                  <Button disabled={busy} onClick={() => void act(() => shipStoreTransfer(detail.id), "Göndərildi: mal göndərən anbarın stokundan çıxdı")}>
                    Göndər
                  </Button>
                )}
                {(detail.status === StoreTransferStatus.Draft || detail.status === StoreTransferStatus.Pending) && canManage && (
                  <Button variant="destructive" disabled={busy} onClick={() => void act(() => cancelStoreTransfer(detail.id), "Transfer ləğv edildi")}>
                    Ləğv et
                  </Button>
                )}
                {(detail.status === StoreTransferStatus.Shipped || detail.status === StoreTransferStatus.PartiallyReceived) && canReceive && (
                  <>
                    <Button disabled={busy} onClick={() => void receive(false)}>
                      Qəbul et
                    </Button>
                    <Button variant="outline" disabled={busy} onClick={() => void receive(true)}>
                      Çatışmazlıqla bağla
                    </Button>
                  </>
                )}
              </div>
              {detail.note && <p className="text-sm text-muted-foreground">Qeyd: {detail.note}</p>}
            </div>
          )}
        </DialogContent>
      </Dialog>
    </div>
  );
}
