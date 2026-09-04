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
  createScaleDevice,
  deleteScaleDevice,
  getScaleDevices,
  updateScaleDevice,
  type ScaleDevice,
} from "@/lib/services/scale-device-service";

const selectClass =
  "flex h-10 w-full rounded-md border border-input bg-background px-3 py-2 text-sm ring-offset-background";

type ScaleDeviceRow = {
  id: string;
  deviceId: number;
  name: string;
  brand: string;
  connectionInfo: string;
  isActive: boolean;
};

export default function ScaleDevicesPage() {
  const [restaurants, setRestaurants] = useState<Restaurant[]>([]);
  const [restaurantId, setRestaurantId] = useState<string>("");
  const [devices, setDevices] = useState<ScaleDevice[]>([]);
  const [loading, setLoading] = useState(true);

  const [dialogOpen, setDialogOpen] = useState(false);
  const [saving, setSaving] = useState(false);
  const [editingId, setEditingId] = useState<number | null>(null);
  const [name, setName] = useState("");
  const [brand, setBrand] = useState("");
  const [connectionInfo, setConnectionInfo] = useState("");
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

  const loadDevices = async (rid: number) => {
    setLoading(true);
    try {
      const data = await getScaleDevices(rid);
      setDevices(data);
    } catch (e) {
      toast.error(e instanceof Error ? e.message : "Tərəzilər yüklənmədi.");
      setDevices([]);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    const rid = Number(restaurantId);
    if (!Number.isFinite(rid) || rid <= 0) {
      setDevices([]);
      setLoading(false);
      return;
    }
    void loadDevices(rid);
  }, [restaurantId]);

  const resetForm = () => {
    setEditingId(null);
    setName("");
    setBrand("");
    setConnectionInfo("");
    setIsActive(true);
  };

  const handleAdd = () => {
    resetForm();
    setDialogOpen(true);
  };

  const handleEdit = (row: ScaleDeviceRow) => {
    const target = devices.find((d) => d.id === row.deviceId);
    if (!target) return;
    setEditingId(target.id);
    setName(target.name);
    setBrand(target.brand ?? "");
    setConnectionInfo(target.connectionInfo ?? "");
    setIsActive(target.isActive);
    setDialogOpen(true);
  };

  const handleDelete = async (row: ScaleDeviceRow) => {
    if (!window.confirm(`"${row.name}" tərəzisini silmək istəyirsiniz?`)) return;
    try {
      await deleteScaleDevice(row.deviceId);
      toast.success("Tərəzi silindi.");
      await loadDevices(Number(restaurantId));
    } catch (e) {
      toast.error(e instanceof Error ? e.message : "Tərəzi silinmədi.");
    }
  };

  const handleSave = async () => {
    const rid = Number(restaurantId);
    if (!name.trim()) {
      toast.error("Tərəzinin adı vacibdir.");
      return;
    }
    setSaving(true);
    try {
      const payload = {
        restaurantId: rid,
        name: name.trim(),
        brand: brand.trim() || null,
        connectionInfo: connectionInfo.trim() || null,
        isActive,
      };
      if (editingId == null) {
        await createScaleDevice(payload);
        toast.success("Tərəzi əlavə edildi.");
      } else {
        await updateScaleDevice(editingId, payload);
        toast.success("Tərəzi yeniləndi.");
      }
      setDialogOpen(false);
      resetForm();
      await loadDevices(rid);
    } catch (e) {
      toast.error(e instanceof Error ? e.message : "Yadda saxlanılmadı.");
    } finally {
      setSaving(false);
    }
  };

  const rows: ScaleDeviceRow[] = useMemo(
    () =>
      devices.map((d) => ({
        id: String(d.id),
        deviceId: d.id,
        name: d.name,
        brand: d.brand ?? "—",
        connectionInfo: d.connectionInfo ?? "—",
        isActive: d.isActive,
      })),
    [devices],
  );

  const columns = [
    { key: "deviceId" as const, label: "ID" },
    { key: "name" as const, label: "Ad" },
    { key: "brand" as const, label: "Marka" },
    { key: "connectionInfo" as const, label: "Bağlantı" },
    {
      key: "isActive" as const,
      label: "Status",
      render: (v: boolean) => (
        <Badge
          className={v ? "bg-emerald-100 text-emerald-800 hover:bg-emerald-100" : "bg-slate-200 text-slate-800 hover:bg-slate-200"}
        >
          {v ? "Aktiv" : "Passiv"}
        </Badge>
      ),
    },
  ];

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-3xl font-bold text-foreground">Tərəzilər</h1>
        <p className="text-muted-foreground mt-1">
          Çəki tərəzilərini qeydə alın. Hazırda yalnız qeydiyyat saxlanılır — real inteqrasiya (tərəzidən çəki
          oxuma) markaya görə ayrıca qurulur.
        </p>
      </div>

      <div className="max-w-xs">
        <Label htmlFor="scale-restaurant">Restoran</Label>
        <select
          id="scale-restaurant"
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
            title="Tərəzi siyahısı"
            columns={columns}
            data={rows}
            idSortKey="deviceId"
            searchPlaceholder="Tərəzi axtar…"
            searchableFields={["name", "brand"]}
            onAdd={handleAdd}
            onEdit={handleEdit}
            onDelete={handleDelete}
          />

          <DialogContent className="sm:max-w-md">
            <DialogHeader>
              <DialogTitle>{editingId != null ? "Tərəzini redaktə et" : "Tərəzi əlavə et"}</DialogTitle>
              <DialogDescription>Çəki tərəzisinin adı, markası və bağlantı məlumatı.</DialogDescription>
            </DialogHeader>

            <div className="space-y-3">
              <div>
                <Label htmlFor="sd-name">Ad</Label>
                <Input id="sd-name" className="mt-1" value={name} onChange={(e) => setName(e.target.value)} placeholder="Tərəzi 1" />
              </div>
              <div>
                <Label htmlFor="sd-brand">Marka/Model</Label>
                <Input
                  id="sd-brand"
                  className="mt-1"
                  value={brand}
                  onChange={(e) => setBrand(e.target.value)}
                  placeholder="Optional"
                />
              </div>
              <div>
                <Label htmlFor="sd-connection">Bağlantı məlumatı</Label>
                <Input
                  id="sd-connection"
                  className="mt-1"
                  value={connectionInfo}
                  onChange={(e) => setConnectionInfo(e.target.value)}
                  placeholder="IP:port, seriya port və s. (opsional)"
                />
              </div>
              <div className="flex items-center gap-2">
                <Checkbox id="sd-active" checked={isActive} onCheckedChange={(v) => setIsActive(v === true)} />
                <Label htmlFor="sd-active" className="text-sm font-normal">
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
