"use client";

import { useEffect, useMemo, useState } from "react";
import { DataTable } from "@/components/data-table";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Checkbox } from "@/components/ui/checkbox";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { toast } from "sonner";
import { getRestaurants, type Restaurant } from "@/lib/services/restaurant-service";
import {
  createRestaurantTable,
  deleteRestaurantTable,
  getRestaurantTables,
  updateRestaurantTable,
  RestaurantTableType,
  type RestaurantTable,
  type RestaurantTableTypeValue,
} from "@/lib/services/restaurant-table-service";

const selectClass =
  "flex h-10 w-full rounded-md border border-input bg-background px-3 py-2 text-sm ring-offset-background";

function typeLabel(_value: RestaurantTableTypeValue): string {
  return "Kabinet";
}

type StationRow = {
  id: string;
  stationId: number;
  name: string;
  typeLabel: string;
  hourlyRateLabel: string;
  isActive: boolean;
  isOccupied: boolean;
};

export default function CabinetStationsPage() {
  const [restaurants, setRestaurants] = useState<Restaurant[]>([]);
  const [restaurantId, setRestaurantId] = useState<string>("");
  const [stations, setStations] = useState<RestaurantTable[]>([]);
  const [loading, setLoading] = useState(true);

  const [dialogOpen, setDialogOpen] = useState(false);
  const [saving, setSaving] = useState(false);
  const [editingId, setEditingId] = useState<number | null>(null);
  const [name, setName] = useState("");
  const [type, setType] = useState<string>(String(RestaurantTableType.Kabinet));
  const [capacity, setCapacity] = useState("1");
  const [hourlyRate, setHourlyRate] = useState("");
  const [isActive, setIsActive] = useState(true);

  useEffect(() => {
    (async () => {
      try {
        const rs = await getRestaurants();
        setRestaurants(rs);
        if (rs.length > 0) setRestaurantId(String(rs[0].id));
      } catch {
        setRestaurants([]);
      }
    })();
  }, []);

  const loadStations = async (rid: number) => {
    setLoading(true);
    try {
      const all = await getRestaurantTables();
      setStations(all.filter((t) => t.restaurantId === rid && t.type === RestaurantTableType.Kabinet));
    } catch (e) {
      toast.error(e instanceof Error ? e.message : "Stansiyalar yüklənmədi.");
      setStations([]);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    const rid = Number(restaurantId);
    if (!Number.isFinite(rid) || rid <= 0) {
      setStations([]);
      setLoading(false);
      return;
    }
    void loadStations(rid);
  }, [restaurantId]);

  const resetForm = () => {
    setEditingId(null);
    setName("");
    setType(String(RestaurantTableType.Kabinet));
    setCapacity("1");
    setHourlyRate("");
    setIsActive(true);
  };

  const handleAdd = () => {
    resetForm();
    setDialogOpen(true);
  };

  const handleEdit = (row: StationRow) => {
    const target = stations.find((s) => s.id === row.stationId);
    if (!target) return;
    setEditingId(target.id);
    setName(target.name);
    setType(String(target.type));
    setCapacity(String(target.capacity || 1));
    setHourlyRate(target.hourlyRate != null ? String(target.hourlyRate) : "");
    setIsActive(target.isActive);
    setDialogOpen(true);
  };

  const handleDelete = async (row: StationRow) => {
    if (!window.confirm(`"${row.name}" stansiyasını silmək istəyirsiniz?`)) return;
    try {
      await deleteRestaurantTable(row.stationId);
      toast.success("Stansiya silindi.");
      await loadStations(Number(restaurantId));
    } catch (e) {
      toast.error(e instanceof Error ? e.message : "Stansiya silinmədi.");
    }
  };

  const handleSave = async () => {
    const rid = Number(restaurantId);
    const capacityNum = Number(capacity) || 1;
    const rateNum = hourlyRate.trim() === "" ? null : Number(hourlyRate);
    if (!name.trim()) {
      toast.error("Ad vacibdir.");
      return;
    }
    if (rateNum == null || !Number.isFinite(rateNum) || rateNum <= 0) {
      toast.error("Saatlıq qiymət vacibdir və 0-dan böyük olmalıdır.");
      return;
    }
    setSaving(true);
    try {
      const payload = {
        restaurantId: rid,
        name: name.trim(),
        capacity: capacityNum,
        isActive,
        hourlyRate: rateNum,
        type: Number(type) as RestaurantTableTypeValue,
      };
      if (editingId == null) {
        await createRestaurantTable(payload);
        toast.success("Stansiya əlavə edildi.");
      } else {
        await updateRestaurantTable(editingId, payload);
        toast.success("Stansiya yeniləndi.");
      }
      setDialogOpen(false);
      resetForm();
      await loadStations(rid);
    } catch (e) {
      toast.error(e instanceof Error ? e.message : "Yadda saxlanılmadı.");
    } finally {
      setSaving(false);
    }
  };

  const rows: StationRow[] = useMemo(
    () =>
      stations.map((s) => ({
        id: String(s.id),
        stationId: s.id,
        name: s.name,
        typeLabel: typeLabel(s.type),
        hourlyRateLabel: s.hourlyRate != null ? `${s.hourlyRate.toFixed(2)} ₼/saat` : "—",
        isActive: s.isActive,
        isOccupied: s.isOccupied,
      })),
    [stations],
  );

  const columns = [
    { key: "stationId" as const, label: "ID" },
    { key: "name" as const, label: "Ad" },
    { key: "typeLabel" as const, label: "Tip" },
    { key: "hourlyRateLabel" as const, label: "Saatlıq qiymət" },
    {
      key: "isOccupied" as const,
      label: "Vəziyyət",
      render: (v: boolean) => (
        <Badge className={v ? "bg-amber-100 text-amber-800 hover:bg-amber-100" : "bg-emerald-100 text-emerald-800 hover:bg-emerald-100"}>
          {v ? "Məşğuldur" : "Boşdur"}
        </Badge>
      ),
    },
    {
      key: "isActive" as const,
      label: "Status",
      render: (v: boolean) => (
        <Badge className={v ? "bg-emerald-100 text-emerald-800 hover:bg-emerald-100" : "bg-slate-200 text-slate-800 hover:bg-slate-200"}>
          {v ? "Aktiv" : "Passiv"}
        </Badge>
      ),
    },
  ];

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-3xl font-bold text-foreground">Kabinetlər</h1>
        <p className="text-muted-foreground mt-1">
          Saatlıq icarə ilə işləyən kabinetləri buradan idarə edin — adi masalardan ayrıdır. Kabinet daxilində
          PlayStation kimi əlavə xidmətlər lazımdırsa, onları "Menyu Elementləri" bölməsində zaman əsaslı məhsul
          olaraq əlavə edib sifarişə daxil edə bilərsiniz. POS-da ayrıca bölmə kimi göstərmək üçün "Restoran
          Bölmələri" səhifəsində bir bölmə yaradıb bu kabinetləri ora bağlayın.
        </p>
      </div>

      <div className="max-w-xs">
        <Label htmlFor="station-restaurant">Restoran</Label>
        <select
          id="station-restaurant"
          className={selectClass + " mt-1"}
          value={restaurantId}
          onChange={(e) => setRestaurantId(e.target.value)}
        >
          <option value="">Restoran seçin</option>
          {restaurants.map((r) => (
            <option key={r.id} value={String(r.id)}>
              {r.name}
            </option>
          ))}
        </select>
      </div>

      {loading ? (
        <div className="text-sm text-muted-foreground">Yüklənir…</div>
      ) : (
        <Dialog
          open={dialogOpen}
          onOpenChange={(o) => {
            setDialogOpen(o);
            if (!o) resetForm();
          }}
        >
          <DataTable
            title="Stansiya siyahısı"
            columns={columns}
            data={rows}
            idSortKey="stationId"
            searchPlaceholder="Stansiya axtar…"
            searchableFields={["name", "typeLabel"]}
            onAdd={handleAdd}
            onEdit={handleEdit}
            onDelete={handleDelete}
          />

          <DialogContent className="sm:max-w-md">
            <DialogHeader>
              <DialogTitle>{editingId != null ? "Kabineti redaktə et" : "Kabinet əlavə et"}</DialogTitle>
              <DialogDescription>Kabinetin adı, tutumu və saatlıq qiyməti.</DialogDescription>
            </DialogHeader>

            <div className="space-y-3">
              <div>
                <Label htmlFor="st-name">Ad</Label>
                <Input id="st-name" className="mt-1" value={name} onChange={(e) => setName(e.target.value)} placeholder="Kabinet 1" />
              </div>
              <div>
                <Label htmlFor="st-hourly">Saatlıq qiymət</Label>
                <Input
                  id="st-hourly"
                  className="mt-1"
                  type="number"
                  min={0}
                  step="0.01"
                  value={hourlyRate}
                  onChange={(e) => setHourlyRate(e.target.value)}
                  placeholder="məs. 15.00"
                />
              </div>
              <div>
                <Label htmlFor="st-capacity">Tutum (nəfər)</Label>
                <Input
                  id="st-capacity"
                  className="mt-1"
                  type="number"
                  min={1}
                  value={capacity}
                  onChange={(e) => setCapacity(e.target.value)}
                />
              </div>
              <div className="flex items-center gap-2">
                <Checkbox id="st-active" checked={isActive} onCheckedChange={(v) => setIsActive(v === true)} />
                <Label htmlFor="st-active" className="text-sm font-normal">
                  Aktiv
                </Label>
              </div>
            </div>

            <div className="mt-4 flex justify-end gap-2">
              <Button variant="outline" onClick={() => setDialogOpen(false)} disabled={saving}>
                Ləğv et
              </Button>
              <Button onClick={() => void handleSave()} disabled={saving}>
                {saving ? "Saxlanılır…" : "Saxla"}
              </Button>
            </div>
          </DialogContent>
        </Dialog>
      )}
    </div>
  );
}
