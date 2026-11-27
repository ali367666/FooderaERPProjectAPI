"use client";

import { useState } from "react";
import { toast } from "sonner";
import { Button } from "@/components/ui/button";
import { Dialog, DialogContent, DialogDescription, DialogHeader, DialogTitle } from "@/components/ui/dialog";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { createRestaurantTablesBulk, type RestaurantTable } from "@/lib/services/restaurant-table-service";
import type { Restaurant } from "@/lib/services/restaurant-service";
import type { RestaurantSection } from "@/lib/services/restaurant-section-service";

const MAX_COUNT = 200;
const SELECT_CLASS = "flex h-10 w-full rounded-md border border-input bg-background px-3 py-2 text-sm";

type Props = {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  restaurants: Restaurant[];
  sections: RestaurantSection[];
  tables: RestaurantTable[];
  onCreated: () => Promise<void>;
};

/** The same naming rule as the server, to show the names before they are created. */
function previewNames(existing: string[], prefix: string, count: number, start: number | null): string[] {
  const head = prefix.trim() ? `${prefix.trim()} ` : "";
  const taken = new Set(existing.map((n) => n.trim().toLowerCase()));
  let next = start;
  if (next == null) {
    let highest = 0;
    for (const name of taken) {
      if (!name.startsWith(head.toLowerCase())) continue;
      const rest = name.slice(head.length);
      if (/^\d+$/.test(rest)) highest = Math.max(highest, Number(rest));
    }
    next = highest + 1;
  }
  const names: string[] = [];
  for (; names.length < count; next++) {
    const name = `${head}${next}`;
    if (!taken.has(name.toLowerCase())) names.push(name);
  }
  return names;
}

export function BulkTablesDialog({ open, onOpenChange, restaurants, sections, tables, onCreated }: Props) {
  const [restaurantId, setRestaurantId] = useState("");
  const [sectionId, setSectionId] = useState("");
  const [count, setCount] = useState("20");
  const [prefix, setPrefix] = useState("Masa");
  const [startNumber, setStartNumber] = useState("");
  const [capacity, setCapacity] = useState("");
  const [saving, setSaving] = useState(false);

  const branchSections = sections.filter((s) => s.restaurantId === Number(restaurantId) && s.isActive);

  const countNum = Number(count);
  const startNum = startNumber.trim() === "" ? null : Number(startNumber);
  const capacityNum = capacity.trim() === "" ? null : Number(capacity);
  const countOk = Number.isInteger(countNum) && countNum >= 1 && countNum <= MAX_COUNT;
  const startOk = startNum == null || (Number.isInteger(startNum) && startNum >= 1);
  const capacityOk = capacityNum == null || (Number.isInteger(capacityNum) && capacityNum >= 0);
  const valid = Number(restaurantId) > 0 && countOk && startOk && capacityOk;

  const preview = valid
    ? previewNames(
        tables
          .filter((t) => t.restaurantId === Number(restaurantId) && (t.sectionId ?? 0) === Number(sectionId || 0))
          .map((t) => t.name),
        prefix,
        countNum,
        startNum,
      )
    : [];

  async function handleCreate() {
    setSaving(true);
    try {
      const names = await createRestaurantTablesBulk({
        restaurantId: Number(restaurantId),
        sectionId: Number(sectionId || 0),
        count: countNum,
        prefix: prefix.trim(),
        startNumber: startNum,
        capacity: capacityNum,
      });
      toast.success(`${names.length} masa yaradıldı.`);
      onOpenChange(false);
      await onCreated();
    } catch (e) {
      toast.error(e instanceof Error ? e.message : "Masalar yaradılmadı");
    } finally {
      setSaving(false);
    }
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-md">
        <DialogHeader>
          <DialogTitle>Toplu masa yarat</DialogTitle>
          <DialogDescription>Adlar avtomatik sıra ilə verilir, masalar zal planında sıra ilə düzülür.</DialogDescription>
        </DialogHeader>

        <div className="space-y-4">
          <div className="space-y-2">
            <Label htmlFor="bulk-branch">Filial</Label>
            <select
              id="bulk-branch"
              value={restaurantId}
              onChange={(e) => {
                setRestaurantId(e.target.value);
                setSectionId("");
              }}
              className={SELECT_CLASS}
            >
              <option value="">Filial seçin</option>
              {restaurants.map((r) => (
                <option key={r.id} value={r.id}>
                  {r.name}
                </option>
              ))}
            </select>
          </div>

          <div className="space-y-2">
            <Label htmlFor="bulk-section">Bölmə</Label>
            <select
              id="bulk-section"
              value={sectionId}
              onChange={(e) => setSectionId(e.target.value)}
              disabled={!restaurantId}
              className={SELECT_CLASS}
            >
              <option value="">Bölməsiz</option>
              {branchSections.map((s) => (
                <option key={s.id} value={s.id}>
                  {s.name}
                  {s.isBanquet ? " (banket)" : ""}
                </option>
              ))}
            </select>
          </div>

          <div className="grid grid-cols-2 gap-3">
            <div className="space-y-2">
              <Label htmlFor="bulk-count">Masa sayı</Label>
              <Input id="bulk-count" type="number" min={1} max={MAX_COUNT} value={count} onChange={(e) => setCount(e.target.value)} />
            </div>
            <div className="space-y-2">
              <Label htmlFor="bulk-prefix">Adın sözü</Label>
              <Input id="bulk-prefix" maxLength={80} value={prefix} onChange={(e) => setPrefix(e.target.value)} placeholder="Boş = yalnız nömrə" />
            </div>
            <div className="space-y-2">
              <Label htmlFor="bulk-start">Başlanğıc nömrə</Label>
              <Input id="bulk-start" type="number" min={1} value={startNumber} onChange={(e) => setStartNumber(e.target.value)} placeholder="Avtomatik" />
            </div>
            <div className="space-y-2">
              <Label htmlFor="bulk-capacity">Tutum (istəyə görə)</Label>
              <Input id="bulk-capacity" type="number" min={0} value={capacity} onChange={(e) => setCapacity(e.target.value)} />
            </div>
          </div>

          {!countOk && <p className="text-xs text-red-600">Masa sayı 1 ilə {MAX_COUNT} arasında olmalıdır.</p>}
          {preview.length > 0 && (
            <p className="rounded-md bg-muted px-3 py-2 text-sm">
              {preview.length} masa:{" "}
              <span className="font-medium">
                {preview.length > 3 ? `${preview[0]}, ${preview[1]} … ${preview[preview.length - 1]}` : preview.join(", ")}
              </span>
            </p>
          )}

          <div className="flex justify-end gap-3">
            <Button variant="outline" onClick={() => onOpenChange(false)} disabled={saving}>
              Ləğv et
            </Button>
            <Button onClick={() => void handleCreate()} disabled={!valid || saving}>
              {saving ? "Yaradılır..." : "Yarat"}
            </Button>
          </div>
        </div>
      </DialogContent>
    </Dialog>
  );
}
