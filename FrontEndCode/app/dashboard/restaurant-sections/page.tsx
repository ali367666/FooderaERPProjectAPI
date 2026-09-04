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
  createRestaurantSection,
  deleteRestaurantSection,
  getRestaurantSections,
  updateRestaurantSection,
  type RestaurantSection,
} from "@/lib/services/restaurant-section-service";
import {
  getRestaurantTables,
  updateTableSection,
  RestaurantTableType,
  type RestaurantTable,
  type RestaurantTableTypeValue,
} from "@/lib/services/restaurant-table-service";

function tableTypeLabel(type: RestaurantTable["type"]): string {
  if (type === RestaurantTableType.Kabinet) return "Kabinet";
  return "Masa";
}

const selectClass =
  "flex h-10 w-full rounded-md border border-input bg-background px-3 py-2 text-sm ring-offset-background";

type SectionRow = {
  id: string;
  sectionId: number;
  name: string;
  isActive: boolean;
  typeLabel: string;
};

export default function RestaurantSectionsPage() {
  const [restaurants, setRestaurants] = useState<Restaurant[]>([]);
  const [restaurantId, setRestaurantId] = useState<string>("");
  const [sections, setSections] = useState<RestaurantSection[]>([]);
  const [loading, setLoading] = useState(true);

  const [dialogOpen, setDialogOpen] = useState(false);
  const [saving, setSaving] = useState(false);
  const [editingId, setEditingId] = useState<number | null>(null);
  const [name, setName] = useState("");
  const [isActive, setIsActive] = useState(true);
  const [type, setType] = useState<RestaurantTableTypeValue>(RestaurantTableType.Masa);
  const [tables, setTables] = useState<RestaurantTable[]>([]);
  const [selectedTableIds, setSelectedTableIds] = useState<number[]>([]);
  const [initialTableIds, setInitialTableIds] = useState<number[]>([]);

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

  const loadSections = async (rid: number) => {
    setLoading(true);
    try {
      const data = await getRestaurantSections(rid);
      setSections(data);
    } catch (e) {
      toast.error(e instanceof Error ? e.message : "Bölmələr yüklənmədi.");
      setSections([]);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    const rid = Number(restaurantId);
    if (!Number.isFinite(rid) || rid <= 0) {
      setSections([]);
      setLoading(false);
      return;
    }
    void loadSections(rid);
  }, [restaurantId]);

  const resetForm = () => {
    setEditingId(null);
    setName("");
    setIsActive(true);
    setType(RestaurantTableType.Masa);
    setSelectedTableIds([]);
    setInitialTableIds([]);
  };

  const handleAdd = () => {
    resetForm();
    setDialogOpen(true);
  };

  const handleEdit = async (row: SectionRow) => {
    const target = sections.find((s) => s.id === row.sectionId);
    if (!target) return;
    setEditingId(target.id);
    setName(target.name);
    setIsActive(target.isActive);
    setType(target.type);
    try {
      const rid = Number(restaurantId);
      const allTables = await getRestaurantTables();
      const restTables = allTables.filter((t) => t.restaurantId === rid);
      setTables(restTables);
      const assigned = restTables.filter((t) => t.sectionId === target.id).map((t) => t.id);
      setSelectedTableIds(assigned);
      setInitialTableIds(assigned);
    } catch {
      setTables([]);
    }
    setDialogOpen(true);
  };

  const handleDelete = async (row: SectionRow) => {
    if (!window.confirm(`"${row.name}" bölməsini silmək istəyirsiniz?`)) return;
    try {
      await deleteRestaurantSection(row.sectionId);
      toast.success("Bölmə silindi.");
      await loadSections(Number(restaurantId));
    } catch (e) {
      toast.error(e instanceof Error ? e.message : "Bölmə silinmədi.");
    }
  };

  const handleSave = async () => {
    const rid = Number(restaurantId);
    if (!name.trim()) {
      toast.error("Bölmə adı vacibdir.");
      return;
    }
    if (!Number.isFinite(rid) || rid <= 0) {
      toast.error("Restoran seçilməlidir.");
      return;
    }
    setSaving(true);
    try {
      if (editingId == null) {
        await createRestaurantSection({ restaurantId: rid, name: name.trim(), isActive, type });
        toast.success("Bölmə əlavə edildi.");
      } else {
        await updateRestaurantSection({ id: editingId, name: name.trim(), isActive, type });
        const added = selectedTableIds.filter((id) => !initialTableIds.includes(id));
        const removed = initialTableIds.filter((id) => !selectedTableIds.includes(id));
        await Promise.all([
          ...added.map((id) => updateTableSection(id, editingId)),
          ...removed.map((id) => updateTableSection(id, null)),
        ]);
        toast.success("Bölmə yeniləndi.");
      }
      setDialogOpen(false);
      resetForm();
      await loadSections(rid);
    } catch (e) {
      toast.error(e instanceof Error ? e.message : "Yadda saxlanılmadı.");
    } finally {
      setSaving(false);
    }
  };

  const rows: SectionRow[] = useMemo(
    () =>
      sections.map((s) => ({
        id: String(s.id),
        sectionId: s.id,
        name: s.name,
        isActive: s.isActive,
        typeLabel: tableTypeLabel(s.type),
      })),
    [sections],
  );

  const columns = [
    { key: "sectionId" as const, label: "ID" },
    { key: "name" as const, label: "Ad" },
    { key: "typeLabel" as const, label: "Tip" },
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
        <h1 className="text-3xl font-bold text-foreground">Restoran Bölmələri</h1>
        <p className="text-muted-foreground mt-1">
          Pivəbar, Kabinet kimi restoran daxili bölmələri idarə edin — hər bölmənin öz masaları olur.
        </p>
      </div>

      <div className="max-w-xs">
        <Label htmlFor="section-restaurant">Restoran</Label>
        <select
          id="section-restaurant"
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
            title="Bölmələr"
            columns={columns}
            data={rows}
            idSortKey="sectionId"
            searchPlaceholder="Bölmə axtar…"
            searchableFields={["name"]}
            onAdd={handleAdd}
            onEdit={handleEdit}
            onDelete={handleDelete}
          />

          <DialogContent className="sm:max-w-md">
            <DialogHeader>
              <DialogTitle>{editingId != null ? "Bölməni redaktə et" : "Bölmə əlavə et"}</DialogTitle>
              <DialogDescription>Restoran daxilində sərbəst bölmə (Pivəbar, Kabinet və s.)</DialogDescription>
            </DialogHeader>

            <div className="space-y-3">
              <div>
                <Label htmlFor="sec-name">Ad</Label>
                <Input id="sec-name" className="mt-1" value={name} onChange={(e) => setName(e.target.value)} />
              </div>
              <div>
                <Label htmlFor="sec-type">Bölmə tipi</Label>
                <select
                  id="sec-type"
                  className={selectClass + " mt-1"}
                  value={type}
                  onChange={(e) => setType(Number(e.target.value) as RestaurantTableTypeValue)}
                >
                  <option value={String(RestaurantTableType.Masa)}>Masalar</option>
                  <option value={String(RestaurantTableType.Kabinet)}>Kabinetlər</option>
                </select>
              </div>
              <div className="flex items-center gap-2">
                <Checkbox id="sec-active" checked={isActive} onCheckedChange={(v) => setIsActive(v === true)} />
                <Label htmlFor="sec-active" className="text-sm font-normal">
                  Aktiv
                </Label>
              </div>
              {editingId != null && (
                <div>
                  <Label>Bu bölmənin masaları</Label>
                  <div className="mt-1 grid max-h-40 grid-cols-2 gap-2 overflow-y-auto rounded-md border border-input p-3">
                    {tables.filter((t) => t.type === type).map((t) => (
                      <label key={t.id} className="flex items-center gap-2 text-sm font-normal">
                        <Checkbox
                          checked={selectedTableIds.includes(t.id)}
                          onCheckedChange={(v) =>
                            setSelectedTableIds((prev) =>
                              v === true ? [...prev, t.id] : prev.filter((id) => id !== t.id),
                            )
                          }
                        />
                        {t.name}
                      </label>
                    ))}
                    {tables.filter((t) => t.type === type).length === 0 && (
                      <p className="text-sm text-muted-foreground">Bu tipdə masa/stansiya tapılmadı.</p>
                    )}
                  </div>
                </div>
              )}
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
