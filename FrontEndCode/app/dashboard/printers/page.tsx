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
import { Plus, Trash2 } from "lucide-react";
import { toast } from "sonner";
import { getRestaurants, type Restaurant } from "@/lib/services/restaurant-service";
import {
  createPrinter,
  deletePrinter,
  getPrinters,
  printToPrinter,
  updatePrinter,
  type Printer,
} from "@/lib/services/printer-service";
import {
  createPrinterStationType,
  deletePrinterStationType,
  getPrinterStationTypes,
  updatePrinterStationType,
  type PrinterStationType,
} from "@/lib/services/printer-station-type-service";

const selectClass =
  "flex h-10 w-full rounded-md border border-input bg-background px-3 py-2 text-sm ring-offset-background";

type PrinterRow = {
  id: string;
  printerId: number;
  name: string;
  stationLabel: string;
  address: string;
  isActive: boolean;
  isPrimary: boolean;
};

export default function PrintersPage() {
  const [restaurants, setRestaurants] = useState<Restaurant[]>([]);
  const [restaurantId, setRestaurantId] = useState<string>("");
  const [printers, setPrinters] = useState<Printer[]>([]);
  const [loading, setLoading] = useState(true);

  const [stationTypes, setStationTypes] = useState<PrinterStationType[]>([]);
  const [stationTypesLoading, setStationTypesLoading] = useState(true);
  const [newStationName, setNewStationName] = useState("");
  const [savingStation, setSavingStation] = useState(false);
  const [editingStationId, setEditingStationId] = useState<number | null>(null);
  const [editingStationName, setEditingStationName] = useState("");

  const [dialogOpen, setDialogOpen] = useState(false);
  const [saving, setSaving] = useState(false);
  const [testingId, setTestingId] = useState<number | null>(null);
  const [editingId, setEditingId] = useState<number | null>(null);
  const [name, setName] = useState("");
  const [stationTypeId, setStationTypeId] = useState<string>("");
  const [ipAddress, setIpAddress] = useState("");
  const [port, setPort] = useState("9100");
  const [isActive, setIsActive] = useState(true);
  const [isPrimary, setIsPrimary] = useState(false);

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

  const loadStationTypes = async () => {
    setStationTypesLoading(true);
    try {
      const data = await getPrinterStationTypes();
      setStationTypes(data);
    } catch (e) {
      toast.error(e instanceof Error ? e.message : "Stansiyalar yüklənmədi.");
      setStationTypes([]);
    } finally {
      setStationTypesLoading(false);
    }
  };

  useEffect(() => {
    void loadStationTypes();
  }, []);

  const loadPrinters = async (rid: number) => {
    setLoading(true);
    try {
      const data = await getPrinters(rid);
      setPrinters(data);
    } catch (e) {
      toast.error(e instanceof Error ? e.message : "Printerlər yüklənmədi.");
      setPrinters([]);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    const rid = Number(restaurantId);
    if (!Number.isFinite(rid) || rid <= 0) {
      setPrinters([]);
      setLoading(false);
      return;
    }
    void loadPrinters(rid);
  }, [restaurantId]);

  const handleAddStation = async () => {
    const trimmed = newStationName.trim();
    if (!trimmed) {
      toast.error("Stansiya adı vacibdir.");
      return;
    }
    setSavingStation(true);
    try {
      await createPrinterStationType({ name: trimmed, isActive: true });
      setNewStationName("");
      toast.success("Stansiya əlavə edildi.");
      await loadStationTypes();
    } catch (e) {
      toast.error(e instanceof Error ? e.message : "Stansiya əlavə olunmadı.");
    } finally {
      setSavingStation(false);
    }
  };

  const startEditStation = (s: PrinterStationType) => {
    setEditingStationId(s.id);
    setEditingStationName(s.name);
  };

  const handleSaveStationEdit = async (id: number) => {
    const trimmed = editingStationName.trim();
    if (!trimmed) {
      toast.error("Stansiya adı vacibdir.");
      return;
    }
    setSavingStation(true);
    try {
      await updatePrinterStationType(id, { name: trimmed, isActive: true });
      setEditingStationId(null);
      toast.success("Stansiya yeniləndi.");
      await loadStationTypes();
    } catch (e) {
      toast.error(e instanceof Error ? e.message : "Stansiya yenilənmədi.");
    } finally {
      setSavingStation(false);
    }
  };

  const handleDeleteStation = async (s: PrinterStationType) => {
    if (!window.confirm(`"${s.name}" stansiyasını silmək istəyirsiniz?`)) return;
    try {
      await deletePrinterStationType(s.id);
      toast.success("Stansiya silindi.");
      await loadStationTypes();
    } catch (e) {
      toast.error(e instanceof Error ? e.message : "Stansiya silinmədi.");
    }
  };

  const resetForm = () => {
    setEditingId(null);
    setName("");
    setStationTypeId("");
    setIpAddress("");
    setPort("9100");
    setIsActive(true);
    setIsPrimary(false);
  };

  const handleAdd = () => {
    resetForm();
    setDialogOpen(true);
  };

  const handleEdit = async (row: PrinterRow) => {
    const target = printers.find((p) => p.id === row.printerId);
    if (!target) return;
    setEditingId(target.id);
    setName(target.name);
    setStationTypeId(String(target.stationTypeId));
    setIpAddress(target.ipAddress);
    setPort(String(target.port));
    setIsActive(target.isActive);
    setIsPrimary(target.isPrimary);
    setDialogOpen(true);
  };

  const handleDelete = async (row: PrinterRow) => {
    if (!window.confirm(`"${row.name}" printerini silmək istəyirsiniz?`)) return;
    try {
      await deletePrinter(row.printerId);
      toast.success("Printer silindi.");
      await loadPrinters(Number(restaurantId));
    } catch (e) {
      toast.error(e instanceof Error ? e.message : "Printer silinmədi.");
    }
  };

  const handleTest = async (row: PrinterRow) => {
    setTestingId(row.printerId);
    try {
      await printToPrinter(
        row.printerId,
        `--- SINAQ ÇAPI ---\n${row.name}\n${new Date().toLocaleString("az-AZ")}\n------------------`,
      );
      toast.success("Sınaq çapı göndərildi.");
    } catch (e) {
      toast.error(e instanceof Error ? e.message : "Printerə qoşulmaq mümkün olmadı.");
    } finally {
      setTestingId(null);
    }
  };

  const handleSave = async () => {
    const rid = Number(restaurantId);
    const portNum = Number(port);
    const stationId = Number(stationTypeId);
    if (!name.trim()) {
      toast.error("Printer adı vacibdir.");
      return;
    }
    if (!Number.isFinite(stationId) || stationId <= 0) {
      toast.error("Stansiya seçilməlidir.");
      return;
    }
    if (!ipAddress.trim()) {
      toast.error("IP ünvanı vacibdir.");
      return;
    }
    if (!Number.isFinite(portNum) || portNum <= 0) {
      toast.error("Port düzgün deyil.");
      return;
    }
    setSaving(true);
    try {
      const payload = { restaurantId: rid, name: name.trim(), stationTypeId: stationId, ipAddress: ipAddress.trim(), port: portNum, isActive, isPrimary };
      if (editingId == null) {
        await createPrinter(payload);
        toast.success("Printer əlavə edildi.");
      } else {
        await updatePrinter(editingId, payload);
        toast.success("Printer yeniləndi.");
      }
      setDialogOpen(false);
      resetForm();
      await loadPrinters(rid);
    } catch (e) {
      toast.error(e instanceof Error ? e.message : "Yadda saxlanılmadı.");
    } finally {
      setSaving(false);
    }
  };

  const rows: PrinterRow[] = useMemo(
    () =>
      printers.map((p) => ({
        id: String(p.id),
        printerId: p.id,
        name: p.name,
        stationLabel: p.stationTypeName,
        address: `${p.ipAddress}:${p.port}`,
        isActive: p.isActive,
        isPrimary: p.isPrimary,
      })),
    [printers],
  );

  const columns = [
    { key: "printerId" as const, label: "ID" },
    {
      key: "name" as const,
      label: "Ad",
      render: (v: string, row: PrinterRow) => (
        <span className="flex items-center gap-1.5">
          {v}
          {row.isPrimary && (
            <Badge className="bg-amber-100 text-amber-800 hover:bg-amber-100">Əsas</Badge>
          )}
        </span>
      ),
    },
    { key: "stationLabel" as const, label: "Stansiya" },
    { key: "address" as const, label: "IP:Port" },
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
    {
      key: "id" as const,
      label: "Test",
      render: (_v: string, row: PrinterRow) => (
        <Button size="sm" variant="outline" disabled={testingId === row.printerId} onClick={() => void handleTest(row)}>
          {testingId === row.printerId ? "Göndərilir…" : "Test çapı"}
        </Button>
      ),
    },
  ];

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-3xl font-bold text-foreground">Printerlər</h1>
        <p className="text-muted-foreground mt-1">
          Restoranın şəbəkə printerlərini qeydə alın — IP ünvanı ilə birbaşa çap.
        </p>
      </div>

      <div className="rounded-xl border bg-card p-4">
        <h2 className="mb-1 text-lg font-semibold">Stansiyalar</h2>
        <p className="mb-3 text-sm text-muted-foreground">
          Öz stansiya adlarınızı yaradın (məs. Mətbəx, Qəlyan, Şirniyyat) — printer əlavə edərkən bu siyahıdan seçəcəksiniz.
        </p>

        <div className="mb-3 flex flex-wrap gap-2">
          {stationTypesLoading && <p className="text-sm text-muted-foreground">Yüklənir…</p>}
          {!stationTypesLoading && stationTypes.length === 0 && (
            <p className="text-sm text-muted-foreground">Hələ stansiya yoxdur.</p>
          )}
          {stationTypes.map((s) =>
            editingStationId === s.id ? (
              <div key={s.id} className="flex items-center gap-1 rounded-full border bg-background px-2 py-1">
                <Input
                  autoFocus
                  className="h-7 w-32 text-sm"
                  value={editingStationName}
                  onChange={(e) => setEditingStationName(e.target.value)}
                  onKeyDown={(e) => e.key === "Enter" && void handleSaveStationEdit(s.id)}
                />
                <Button size="sm" className="h-7 px-2" disabled={savingStation} onClick={() => void handleSaveStationEdit(s.id)}>
                  OK
                </Button>
              </div>
            ) : (
              <span
                key={s.id}
                className="flex items-center gap-2 rounded-full border bg-muted px-3 py-1.5 text-sm"
              >
                <button type="button" onClick={() => startEditStation(s)} className="hover:underline">
                  {s.name}
                </button>
                <button
                  type="button"
                  onClick={() => void handleDeleteStation(s)}
                  className="text-muted-foreground hover:text-destructive"
                >
                  <Trash2 className="h-3.5 w-3.5" />
                </button>
              </span>
            ),
          )}
        </div>

        <div className="flex max-w-sm items-center gap-2">
          <Input
            value={newStationName}
            onChange={(e) => setNewStationName(e.target.value)}
            placeholder="Yeni stansiya adı"
            onKeyDown={(e) => e.key === "Enter" && void handleAddStation()}
          />
          <Button size="sm" disabled={savingStation} onClick={() => void handleAddStation()}>
            <Plus className="mr-1 h-4 w-4" />
            Əlavə et
          </Button>
        </div>
      </div>

      <div className="max-w-xs">
        <Label htmlFor="printer-restaurant">Restoran</Label>
        <select
          id="printer-restaurant"
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
            title="Printer siyahısı"
            columns={columns}
            data={rows}
            idSortKey="printerId"
            searchPlaceholder="Printer axtar…"
            searchableFields={["name", "address"]}
            onAdd={handleAdd}
            onEdit={handleEdit}
            onDelete={handleDelete}
          />

          <DialogContent className="sm:max-w-md">
            <DialogHeader>
              <DialogTitle>{editingId != null ? "Printeri redaktə et" : "Printer əlavə et"}</DialogTitle>
              <DialogDescription>Şəbəkə printerinin adı, stansiyası və IP ünvanı.</DialogDescription>
            </DialogHeader>

            <div className="space-y-3">
              <div>
                <Label htmlFor="pr-name">Ad</Label>
                <Input id="pr-name" className="mt-1" value={name} onChange={(e) => setName(e.target.value)} placeholder="Mətbəx printeri" />
              </div>
              <div>
                <Label htmlFor="pr-station">Stansiya</Label>
                <select
                  id="pr-station"
                  className={selectClass + " mt-1"}
                  value={stationTypeId}
                  onChange={(e) => setStationTypeId(e.target.value)}
                >
                  <option value="">Stansiya seçin</option>
                  {stationTypes.map((s) => (
                    <option key={s.id} value={String(s.id)}>
                      {s.name}
                    </option>
                  ))}
                </select>
                {stationTypes.length === 0 && (
                  <p className="mt-1 text-xs text-muted-foreground">
                    Əvvəlcə yuxarıdakı "Stansiyalar" bölümündən bir stansiya yaradın.
                  </p>
                )}
              </div>
              <div className="grid grid-cols-3 gap-2">
                <div className="col-span-2">
                  <Label htmlFor="pr-ip">IP ünvanı</Label>
                  <Input id="pr-ip" className="mt-1" value={ipAddress} onChange={(e) => setIpAddress(e.target.value)} placeholder="192.168.1.50" />
                </div>
                <div>
                  <Label htmlFor="pr-port">Port</Label>
                  <Input id="pr-port" className="mt-1" value={port} onChange={(e) => setPort(e.target.value.replace(/\D/g, ""))} />
                </div>
              </div>
              <div className="flex items-center gap-2">
                <Checkbox id="pr-active" checked={isActive} onCheckedChange={(v) => setIsActive(v === true)} />
                <Label htmlFor="pr-active" className="text-sm font-normal">
                  Aktiv
                </Label>
              </div>
              <div className="flex items-center gap-2">
                <Checkbox id="pr-primary" checked={isPrimary} onCheckedChange={(v) => setIsPrimary(v === true)} />
                <Label htmlFor="pr-primary" className="text-sm font-normal">
                  Əsas (Kassa) printer — sifariş ekranında ilk sırada göstərilir və avtomatik çapda istifadə olunur
                </Label>
              </div>
              <p className="text-xs text-muted-foreground">
                Hansı məhsulların bu printerə çap olunacağını "Menu məhsulları" bölümündə hər məhsulun öz
                formasından təyin edin.
              </p>
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
