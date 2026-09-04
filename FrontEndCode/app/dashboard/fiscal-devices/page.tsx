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
  createFiscalDevice,
  deleteFiscalDevice,
  getFiscalDevices,
  updateFiscalDevice,
  FiscalDeviceProvider,
  fiscalDeviceProviderLabel,
  type FiscalDevice,
  type FiscalDeviceProviderValue,
} from "@/lib/services/fiscal-device-service";

const selectClass =
  "flex h-10 w-full rounded-md border border-input bg-background px-3 py-2 text-sm ring-offset-background";

type FiscalDeviceRow = {
  id: string;
  deviceId: number;
  name: string;
  providerLabel: string;
  connectionInfo: string;
  isActive: boolean;
};

export default function FiscalDevicesPage() {
  const [restaurants, setRestaurants] = useState<Restaurant[]>([]);
  const [restaurantId, setRestaurantId] = useState<string>("");
  const [devices, setDevices] = useState<FiscalDevice[]>([]);
  const [loading, setLoading] = useState(true);

  const [dialogOpen, setDialogOpen] = useState(false);
  const [saving, setSaving] = useState(false);
  const [editingId, setEditingId] = useState<number | null>(null);
  const [name, setName] = useState("");
  const [provider, setProvider] = useState<string>(String(FiscalDeviceProvider.Other));
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
      const data = await getFiscalDevices(rid);
      setDevices(data);
    } catch (e) {
      toast.error(e instanceof Error ? e.message : "Fiskal cihazlar yüklənmədi.");
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
    setProvider(String(FiscalDeviceProvider.Other));
    setConnectionInfo("");
    setIsActive(true);
  };

  const handleAdd = () => {
    resetForm();
    setDialogOpen(true);
  };

  const handleEdit = (row: FiscalDeviceRow) => {
    const target = devices.find((d) => d.id === row.deviceId);
    if (!target) return;
    setEditingId(target.id);
    setName(target.name);
    setProvider(String(target.provider));
    setConnectionInfo(target.connectionInfo ?? "");
    setIsActive(target.isActive);
    setDialogOpen(true);
  };

  const handleDelete = async (row: FiscalDeviceRow) => {
    if (!window.confirm(`"${row.name}" fiskal cihazını silmək istəyirsiniz?`)) return;
    try {
      await deleteFiscalDevice(row.deviceId);
      toast.success("Fiskal cihaz silindi.");
      await loadDevices(Number(restaurantId));
    } catch (e) {
      toast.error(e instanceof Error ? e.message : "Fiskal cihaz silinmədi.");
    }
  };

  const handleSave = async () => {
    const rid = Number(restaurantId);
    const providerNum = Number(provider) as FiscalDeviceProviderValue;
    if (!name.trim()) {
      toast.error("Cihazın adı vacibdir.");
      return;
    }
    setSaving(true);
    try {
      const payload = {
        restaurantId: rid,
        name: name.trim(),
        provider: providerNum,
        connectionInfo: connectionInfo.trim() || null,
        isActive,
      };
      if (editingId == null) {
        await createFiscalDevice(payload);
        toast.success("Fiskal cihaz əlavə edildi.");
      } else {
        await updateFiscalDevice(editingId, payload);
        toast.success("Fiskal cihaz yeniləndi.");
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

  const rows: FiscalDeviceRow[] = useMemo(
    () =>
      devices.map((d) => ({
        id: String(d.id),
        deviceId: d.id,
        name: d.name,
        providerLabel: fiscalDeviceProviderLabel(d.provider),
        connectionInfo: d.connectionInfo ?? "—",
        isActive: d.isActive,
      })),
    [devices],
  );

  const columns = [
    { key: "deviceId" as const, label: "ID" },
    { key: "name" as const, label: "Ad" },
    { key: "providerLabel" as const, label: "Marka" },
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
        <h1 className="text-3xl font-bold text-foreground">Fiskal Kassalar</h1>
        <p className="text-muted-foreground mt-1">
          ƏDV/fiskal kassa cihazlarını qeydə alın. Hazırda yalnız qeydiyyat saxlanılır — real inteqrasiya (kassa
          ilə birbaşa rabitə) markaya görə ayrıca qurulur.
        </p>
      </div>

      <div className="max-w-xs">
        <Label htmlFor="fiscal-restaurant">Restoran</Label>
        <select
          id="fiscal-restaurant"
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
            title="Fiskal cihaz siyahısı"
            columns={columns}
            data={rows}
            idSortKey="deviceId"
            searchPlaceholder="Cihaz axtar…"
            searchableFields={["name", "providerLabel"]}
            onAdd={handleAdd}
            onEdit={handleEdit}
            onDelete={handleDelete}
          />

          <DialogContent className="sm:max-w-md">
            <DialogHeader>
              <DialogTitle>{editingId != null ? "Fiskal cihazı redaktə et" : "Fiskal cihaz əlavə et"}</DialogTitle>
              <DialogDescription>ƏDV/fiskal kassa cihazının adı, markası və bağlantı məlumatı.</DialogDescription>
            </DialogHeader>

            <div className="space-y-3">
              <div>
                <Label htmlFor="fd-name">Ad</Label>
                <Input id="fd-name" className="mt-1" value={name} onChange={(e) => setName(e.target.value)} placeholder="Kassa 1" />
              </div>
              <div>
                <Label htmlFor="fd-provider">Marka</Label>
                <select
                  id="fd-provider"
                  className={selectClass + " mt-1"}
                  value={provider}
                  onChange={(e) => setProvider(e.target.value)}
                >
                  <option value={String(FiscalDeviceProvider.Other)}>Digər / hələ seçilməyib</option>
                  <option value={String(FiscalDeviceProvider.TotalOmitech)}>Total Omitech</option>
                  <option value={String(FiscalDeviceProvider.Smartfon)}>Smartfon</option>
                  <option value={String(FiscalDeviceProvider.Caspos)}>Caspos</option>
                </select>
              </div>
              <div>
                <Label htmlFor="fd-connection">Bağlantı məlumatı</Label>
                <Input
                  id="fd-connection"
                  className="mt-1"
                  value={connectionInfo}
                  onChange={(e) => setConnectionInfo(e.target.value)}
                  placeholder="IP:port, seriya port və ya API açarı (opsional)"
                />
              </div>
              <div className="flex items-center gap-2">
                <Checkbox id="fd-active" checked={isActive} onCheckedChange={(v) => setIsActive(v === true)} />
                <Label htmlFor="fd-active" className="text-sm font-normal">
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
