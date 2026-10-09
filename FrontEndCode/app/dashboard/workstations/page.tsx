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
  createWorkstation,
  deleteWorkstation,
  getWorkstations,
  updateWorkstation,
  type WorkstationItem,
} from "@/lib/services/workstation-service";

const selectClass =
  "flex h-10 w-full rounded-md border border-input bg-background px-3 py-2 text-sm ring-offset-background";

type TerminalRow = {
  id: string;
  terminalId: number;
  name: string;
  restaurantName: string;
  lastSeen: string;
  isActive: boolean;
};

function formatLastSeen(iso: string | null): string {
  if (!iso) return "—";
  const d = new Date(iso.endsWith("Z") ? iso : `${iso}Z`);
  return Number.isNaN(d.getTime()) ? "—" : d.toLocaleString("az-AZ");
}

export default function WorkstationsPage() {
  const [restaurants, setRestaurants] = useState<Restaurant[]>([]);
  const [terminals, setTerminals] = useState<WorkstationItem[]>([]);
  const [loading, setLoading] = useState(true);

  const [dialogOpen, setDialogOpen] = useState(false);
  const [saving, setSaving] = useState(false);
  const [editingId, setEditingId] = useState<number | null>(null);
  const [name, setName] = useState("");
  const [restaurantId, setRestaurantId] = useState("");
  const [isActive, setIsActive] = useState(true);

  const load = async () => {
    setLoading(true);
    try {
      setTerminals(await getWorkstations());
    } catch (e) {
      toast.error(e instanceof Error ? e.message : "Terminallar yüklənmədi.");
      setTerminals([]);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    void load();
    getRestaurants()
      .then(setRestaurants)
      .catch(() => setRestaurants([]));
  }, []);

  const resetForm = () => {
    setEditingId(null);
    setName("");
    setRestaurantId("");
    setIsActive(true);
  };

  const handleEdit = (row: TerminalRow) => {
    const target = terminals.find((t) => t.id === row.terminalId);
    if (!target) return;
    setEditingId(target.id);
    setName(target.name);
    setRestaurantId(target.restaurantId ? String(target.restaurantId) : "");
    setIsActive(target.isActive);
    setDialogOpen(true);
  };

  const handleDelete = async (row: TerminalRow) => {
    if (!window.confirm(`"${row.name}" terminalını silmək istəyirsiniz?`)) return;
    try {
      await deleteWorkstation(row.terminalId);
      toast.success("Terminal silindi.");
      await load();
    } catch (e) {
      toast.error(e instanceof Error ? e.message : "Terminal silinmədi.");
    }
  };

  const handleSave = async () => {
    if (!name.trim()) {
      toast.error("Terminalın adı vacibdir.");
      return;
    }
    setSaving(true);
    try {
      const payload = {
        name: name.trim(),
        restaurantId: restaurantId ? Number(restaurantId) : null,
        isActive,
      };
      if (editingId == null) {
        await createWorkstation(payload);
        toast.success("Terminal əlavə edildi.");
      } else {
        await updateWorkstation(editingId, payload);
        toast.success("Terminal yeniləndi.");
      }
      setDialogOpen(false);
      resetForm();
      await load();
    } catch (e) {
      toast.error(e instanceof Error ? e.message : "Yadda saxlanılmadı.");
    } finally {
      setSaving(false);
    }
  };

  const rows: TerminalRow[] = useMemo(
    () =>
      terminals.map((t) => ({
        id: String(t.id),
        terminalId: t.id,
        name: t.name,
        restaurantName: t.restaurantName ?? "Bütün filiallar",
        lastSeen: formatLastSeen(t.lastSeenAtUtc),
        isActive: t.isActive,
      })),
    [terminals],
  );

  const columns = [
    { key: "terminalId" as const, label: "ID" },
    { key: "name" as const, label: "Ad" },
    { key: "restaurantName" as const, label: "Filial" },
    { key: "lastSeen" as const, label: "Son giriş" },
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
        <h1 className="text-3xl font-bold text-foreground">POS terminalları</h1>
        <p className="text-muted-foreground mt-1">
          Hər monitora ad verin (məs: Kassa 1, Bar). Terminal quraşdırılanda siyahıdan seçilir və giriş
          ekranında bu ad görünür — problem olanda hansı monitor olduğunu bilmək üçün.
        </p>
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
            title="Terminal siyahısı"
            columns={columns}
            data={rows}
            idSortKey="terminalId"
            searchPlaceholder="Terminal axtar…"
            searchableFields={["name", "restaurantName"]}
            onAdd={() => {
              resetForm();
              setDialogOpen(true);
            }}
            onEdit={handleEdit}
            onDelete={handleDelete}
          />

          <DialogContent className="sm:max-w-md">
            <DialogHeader>
              <DialogTitle>{editingId != null ? "Terminalı redaktə et" : "Terminal əlavə et"}</DialogTitle>
              <DialogDescription>Terminalın adı və aid olduğu filial.</DialogDescription>
            </DialogHeader>

            <div className="space-y-3">
              <div>
                <Label htmlFor="pt-name">Ad</Label>
                <Input id="pt-name" className="mt-1" value={name} onChange={(e) => setName(e.target.value)} placeholder="Kassa 1" />
              </div>
              <div>
                <Label htmlFor="pt-restaurant">Filial</Label>
                <select
                  id="pt-restaurant"
                  className={selectClass + " mt-1"}
                  value={restaurantId}
                  onChange={(e) => setRestaurantId(e.target.value)}
                >
                  <option value="">Bütün filiallar</option>
                  {restaurants.map((r) => (
                    <option key={r.id} value={String(r.id)}>
                      {r.name}
                    </option>
                  ))}
                </select>
              </div>
              <div className="flex items-center gap-2">
                <Checkbox id="pt-active" checked={isActive} onCheckedChange={(v) => setIsActive(v === true)} />
                <Label htmlFor="pt-active" className="text-sm font-normal">
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
