"use client";

import { useEffect, useMemo, useState } from "react";
import { Download, Scale } from "lucide-react";
import { Button } from "@/components/ui/button";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { UnitOfMeasure, type MenuItem } from "@/lib/services/menu-item-service";
import { getRestaurants } from "@/lib/services/restaurant-service";
import { getScaleDevices, type ScaleDevice } from "@/lib/services/scale-device-service";

type ScaleOption = ScaleDevice & { restaurantName: string };

type PluRow = {
  plu: number;
  code: string;
  weightCode: string;
  name: string;
  pricePerKg: number;
  barcode: string;
};

const selectClass =
  "flex h-10 w-full rounded-md border border-input bg-background px-3 py-2 text-sm ring-offset-background";

export function isWeightSold(item: MenuItem): boolean {
  return item.unitId === UnitOfMeasure.Kg || item.unitId === UnitOfMeasure.Gram;
}

/**
 * Scales take a short numeric item code; the weight code is "{companyId}-{id:000000}", so the
 * part after the dash is the scale code.
 */
function scaleCode(weightCode: string): string {
  const tail = weightCode.split("-").pop() ?? weightCode;
  return tail.replace(/\D/g, "") || weightCode;
}

function toPluRows(items: MenuItem[]): PluRow[] {
  return items
    .filter((i) => i.isActive && isWeightSold(i) && i.weightCode)
    .map((i) => {
      const price = i.stationPrice ?? i.price;
      return {
        plu: i.id,
        code: scaleCode(i.weightCode!),
        weightCode: i.weightCode!,
        name: i.name,
        // Kg items are priced per kg; gram items per gram.
        pricePerKg: i.unitId === UnitOfMeasure.Gram ? price * 1000 : price,
        barcode: i.barcode ?? "",
      };
    })
    .sort((a, b) => a.plu - b.plu);
}

function csvCell(value: string | number): string {
  const text = String(value);
  return /[;"\n]/.test(text) ? `"${text.replace(/"/g, '""')}"` : text;
}

function downloadCsv(rows: PluRow[], scale: ScaleOption) {
  const header = ["PLU", "Kod", "Ad", "Qiymət (1 kq)", "Çəki kodu", "Barkod"];
  const lines = [
    header.join(";"),
    ...rows.map((r) =>
      [r.plu, r.code, r.name, r.pricePerKg.toFixed(2), r.weightCode, r.barcode].map(csvCell).join(";"),
    ),
  ];
  // BOM so Excel and most scale PC programs read the Azerbaijani letters correctly.
  const blob = new Blob(["﻿" + lines.join("\r\n")], { type: "text/csv;charset=utf-8" });
  const url = URL.createObjectURL(blob);
  const a = document.createElement("a");
  const date = new Date().toISOString().slice(0, 10);
  a.href = url;
  a.download = `terezi-${scale.name.replace(/[^\p{L}\p{N}_-]+/gu, "_")}-${date}.csv`;
  document.body.appendChild(a);
  a.click();
  a.remove();
  URL.revokeObjectURL(url);
}

type ScaleExportDialogProps = {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  items: MenuItem[];
};

/** "Tərəziyə yükləmə" — export weight-sold items as a PLU list the scale's own software imports. */
export function ScaleExportDialog({ open, onOpenChange, items }: ScaleExportDialogProps) {
  const [scales, setScales] = useState<ScaleOption[]>([]);
  const [scaleId, setScaleId] = useState("");
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    if (!open) return;
    setLoading(true);
    getRestaurants()
      .then((restaurants) =>
        Promise.all(
          restaurants.map((r) =>
            getScaleDevices(r.id)
              .then((list) => list.map((s) => ({ ...s, restaurantName: r.name })))
              .catch(() => [] as ScaleOption[]),
          ),
        ),
      )
      .then((lists) => {
        const active = lists.flat().filter((s) => s.isActive);
        setScales(active);
        setScaleId((prev) => prev || (active[0] ? String(active[0].id) : ""));
      })
      .catch(() => setScales([]))
      .finally(() => setLoading(false));
  }, [open]);

  const rows = useMemo(() => toPluRows(items), [items]);
  const selectedScale = scales.find((s) => String(s.id) === scaleId) ?? null;

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-2xl">
        <DialogHeader>
          <DialogTitle className="flex items-center gap-2">
            <Scale className="h-5 w-5" />
            Tərəziyə yükləmə
          </DialogTitle>
          <DialogDescription>
            Çəki ilə satılan aktiv məhsullar PLU siyahısı kimi yüklənir. Faylı tərəzinin öz proqramı ilə tərəziyə
            idxal edin.
          </DialogDescription>
        </DialogHeader>

        <div className="space-y-3">
          <div>
            <label className="mb-2 block text-sm font-medium text-foreground">Tərəzi</label>
            {loading ? (
              <p className="text-sm text-muted-foreground">Yüklənir…</p>
            ) : scales.length === 0 ? (
              <p className="text-sm text-muted-foreground">
                Aktiv tərəzi yoxdur — əvvəlcə "Tərəzilər" bölməsində tərəzi əlavə edin.
              </p>
            ) : (
              <select value={scaleId} onChange={(e) => setScaleId(e.target.value)} className={selectClass}>
                {scales.map((s) => (
                  <option key={s.id} value={String(s.id)}>
                    {s.name}
                    {s.brand ? ` (${s.brand})` : ""} — {s.restaurantName}
                  </option>
                ))}
              </select>
            )}
          </div>

          {rows.length === 0 ? (
            <p className="text-sm text-muted-foreground">
              Çəki ilə satılan (kq/qram) aktiv məhsul yoxdur.
            </p>
          ) : (
            <div className="max-h-80 overflow-auto rounded-md border">
              <table className="w-full text-sm">
                <thead className="sticky top-0 bg-muted">
                  <tr className="text-left">
                    <th className="px-2 py-1.5 font-medium">PLU</th>
                    <th className="px-2 py-1.5 font-medium">Kod</th>
                    <th className="px-2 py-1.5 font-medium">Ad</th>
                    <th className="px-2 py-1.5 text-right font-medium">1 kq</th>
                  </tr>
                </thead>
                <tbody>
                  {rows.map((r) => (
                    <tr key={r.plu} className="border-t">
                      <td className="px-2 py-1.5 tabular-nums">{r.plu}</td>
                      <td className="px-2 py-1.5 tabular-nums">{r.code}</td>
                      <td className="px-2 py-1.5">{r.name}</td>
                      <td className="px-2 py-1.5 text-right tabular-nums">{r.pricePerKg.toFixed(2)} ₼</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </div>

        <DialogFooter>
          <Button variant="outline" onClick={() => onOpenChange(false)}>
            Bağla
          </Button>
          <Button
            disabled={!selectedScale || rows.length === 0}
            onClick={() => selectedScale && downloadCsv(rows, selectedScale)}
          >
            <Download className="mr-2 h-4 w-4" />
            Faylı yüklə ({rows.length})
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
