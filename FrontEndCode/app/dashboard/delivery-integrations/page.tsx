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
import { api } from "@/lib/api";
import { getRestaurants, type Restaurant } from "@/lib/services/restaurant-service";
import { useSelectedRestaurant } from "@/contexts/selected-restaurant-context";
import {
  createDeliveryIntegration,
  deleteDeliveryIntegration,
  deliveryProviderLabel,
  deliveryWebhookUrl,
  getDeliveryIntegrations,
  updateDeliveryIntegration,
  DeliveryProvider,
  type DeliveryIntegration,
  type DeliveryProviderValue,
} from "@/lib/services/delivery-integration-service";

const selectClass =
  "flex h-10 w-full rounded-md border border-input bg-background px-3 py-2 text-sm ring-offset-background";

type DeliveryIntegrationRow = {
  id: string;
  integrationId: number;
  name: string;
  providerLabel: string;
  externalVenueId: string;
  isActive: boolean;
};

export default function DeliveryIntegrationsPage() {
  const { selectedRestaurantId } = useSelectedRestaurant();
  const [restaurants, setRestaurants] = useState<Restaurant[]>([]);
  const [restaurantId, setRestaurantId] = useState<string>("");
  const [integrations, setIntegrations] = useState<DeliveryIntegration[]>([]);
  const [loading, setLoading] = useState(true);

  const [dialogOpen, setDialogOpen] = useState(false);
  const [saving, setSaving] = useState(false);
  const [editingId, setEditingId] = useState<number | null>(null);
  const [name, setName] = useState("");
  const [provider, setProvider] = useState<string>(String(DeliveryProvider.Wolt));
  const [externalVenueId, setExternalVenueId] = useState("");
  const [apiKey, setApiKey] = useState("");
  const [webhookSecret, setWebhookSecret] = useState("");
  const [isActive, setIsActive] = useState(true);

  useEffect(() => {
    (async () => {
      try {
        const rs = await getRestaurants();
        setRestaurants(rs);
        // Follow the top "Filial filter" (Data Seçimi) when one is picked — otherwise default to
        // the first branch, same as before.
        if (selectedRestaurantId != null && rs.some((r) => r.id === selectedRestaurantId)) {
          setRestaurantId(String(selectedRestaurantId));
        } else if (rs.length > 0) {
          setRestaurantId(String(rs[0].id));
        }
      } catch {
        setRestaurants([]);
      }
    })();
  }, [selectedRestaurantId]);

  const loadIntegrations = async (rid: number) => {
    setLoading(true);
    try {
      const data = await getDeliveryIntegrations(rid);
      setIntegrations(data);
    } catch (e) {
      toast.error(e instanceof Error ? e.message : "Çatdırılma inteqrasiyaları yüklənmədi.");
      setIntegrations([]);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    const rid = Number(restaurantId);
    if (!Number.isFinite(rid) || rid <= 0) {
      setIntegrations([]);
      setLoading(false);
      return;
    }
    void loadIntegrations(rid);
  }, [restaurantId]);

  const resetForm = () => {
    setEditingId(null);
    setName("");
    setProvider(String(DeliveryProvider.Wolt));
    setExternalVenueId("");
    setApiKey("");
    setWebhookSecret("");
    setIsActive(true);
  };

  const handleAdd = () => {
    resetForm();
    setDialogOpen(true);
  };

  const handleEdit = (row: DeliveryIntegrationRow) => {
    const target = integrations.find((d) => d.id === row.integrationId);
    if (!target) return;
    setEditingId(target.id);
    setName(target.name);
    setProvider(String(target.provider));
    setExternalVenueId(target.externalVenueId ?? "");
    setApiKey(target.apiKey ?? "");
    setWebhookSecret(target.webhookSecret ?? "");
    setIsActive(target.isActive);
    setDialogOpen(true);
  };

  const handleDelete = async (row: DeliveryIntegrationRow) => {
    if (!window.confirm(`"${row.name}" inteqrasiyasını silmək istəyirsiniz?`)) return;
    try {
      await deleteDeliveryIntegration(row.integrationId);
      toast.success("İnteqrasiya silindi.");
      await loadIntegrations(Number(restaurantId));
    } catch (e) {
      toast.error(e instanceof Error ? e.message : "İnteqrasiya silinmədi.");
    }
  };

  const handleSave = async () => {
    const rid = Number(restaurantId);
    const providerNum = Number(provider) as DeliveryProviderValue;
    if (!name.trim()) {
      toast.error("Adı vacibdir.");
      return;
    }
    setSaving(true);
    try {
      const payload = {
        restaurantId: rid,
        name: name.trim(),
        provider: providerNum,
        externalVenueId: externalVenueId.trim() || null,
        apiKey: apiKey.trim() || null,
        webhookSecret: webhookSecret.trim() || null,
        isActive,
      };
      if (editingId == null) {
        await createDeliveryIntegration(payload);
        toast.success("İnteqrasiya əlavə edildi.");
      } else {
        await updateDeliveryIntegration(editingId, payload);
        toast.success("İnteqrasiya yeniləndi.");
      }
      setDialogOpen(false);
      resetForm();
      await loadIntegrations(rid);
    } catch (e) {
      toast.error(e instanceof Error ? e.message : "Yadda saxlanılmadı.");
    } finally {
      setSaving(false);
    }
  };

  const rows: DeliveryIntegrationRow[] = useMemo(
    () =>
      integrations.map((d) => ({
        id: String(d.id),
        integrationId: d.id,
        name: d.name,
        providerLabel: deliveryProviderLabel(d.provider),
        externalVenueId: d.externalVenueId ?? "—",
        isActive: d.isActive,
      })),
    [integrations],
  );

  const columns = [
    { key: "integrationId" as const, label: "ID" },
    { key: "name" as const, label: "Ad" },
    { key: "providerLabel" as const, label: "Platforma" },
    { key: "externalVenueId" as const, label: "Venue/Store ID" },
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

  const webhookUrl =
    editingId != null
      ? deliveryWebhookUrl(api.defaults.baseURL ?? "", Number(provider) as DeliveryProviderValue, editingId)
      : null;

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-3xl font-bold text-foreground">Çatdırılma İnteqrasiyaları</h1>
        <p className="text-muted-foreground mt-1">
          Wolt, Bolt və 189 Delivery hesablarınızın açarlarını hər filial üçün ayrıca qeyd edin. Platformadan
          alınan "integrator ID"/API açarını və webhook sirrini bura yazın, sonra platformanın öz panelində
          "webhook address" sahəsinə aşağıda göstərilən linki qeyd edin.
        </p>
      </div>

      <div className="max-w-xs">
        <Label htmlFor="delivery-restaurant">Filial</Label>
        <select
          id="delivery-restaurant"
          className={selectClass + " mt-1"}
          value={restaurantId}
          onChange={(e) => setRestaurantId(e.target.value)}
        >
          <option value="">Filial seçin</option>
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
            title="İnteqrasiya siyahısı"
            columns={columns}
            data={rows}
            idSortKey="integrationId"
            searchPlaceholder="İnteqrasiya axtar…"
            searchableFields={["name", "providerLabel"]}
            onAdd={handleAdd}
            onEdit={handleEdit}
            onDelete={handleDelete}
          />

          <DialogContent className="sm:max-w-md">
            <DialogHeader>
              <DialogTitle>{editingId != null ? "İnteqrasiyanı redaktə et" : "İnteqrasiya əlavə et"}</DialogTitle>
              <DialogDescription>Platforma, adı və platformadan aldığınız açarlar.</DialogDescription>
            </DialogHeader>

            <div className="space-y-3">
              <div>
                <Label htmlFor="di-name">Ad</Label>
                <Input
                  id="di-name"
                  className="mt-1"
                  value={name}
                  onChange={(e) => setName(e.target.value)}
                  placeholder="Wolt - Nizami filialı"
                />
              </div>
              <div>
                <Label htmlFor="di-provider">Platforma</Label>
                <select
                  id="di-provider"
                  className={selectClass + " mt-1"}
                  value={provider}
                  onChange={(e) => setProvider(e.target.value)}
                >
                  <option value={String(DeliveryProvider.Wolt)}>Wolt</option>
                  <option value={String(DeliveryProvider.Bolt)}>Bolt</option>
                  <option value={String(DeliveryProvider.Delivery189)}>189 Delivery</option>
                </select>
              </div>
              <div>
                <Label htmlFor="di-venue">Venue/Store ID</Label>
                <Input
                  id="di-venue"
                  className="mt-1"
                  value={externalVenueId}
                  onChange={(e) => setExternalVenueId(e.target.value)}
                  placeholder="Platformanın verdiyi mağaza ID-si (opsional)"
                />
              </div>
              <div>
                <Label htmlFor="di-apikey">API açarı / Integrator ID</Label>
                <Input
                  id="di-apikey"
                  className="mt-1"
                  value={apiKey}
                  onChange={(e) => setApiKey(e.target.value)}
                  placeholder="Platformadan alınan açar (opsional)"
                />
              </div>
              <div>
                <Label htmlFor="di-secret">Webhook sirri (HMAC secret)</Label>
                <Input
                  id="di-secret"
                  className="mt-1"
                  value={webhookSecret}
                  onChange={(e) => setWebhookSecret(e.target.value)}
                  placeholder="Platformadan alınan webhook açarı (opsional)"
                />
              </div>
              {webhookUrl && (
                <div>
                  <Label>Webhook ünvanı (platformanın panelinə yazın)</Label>
                  <p className="mt-1 break-all rounded-md border bg-muted px-3 py-2 text-xs text-muted-foreground">
                    {webhookUrl}
                  </p>
                </div>
              )}
              <div className="flex items-center gap-2">
                <Checkbox id="di-active" checked={isActive} onCheckedChange={(v) => setIsActive(v === true)} />
                <Label htmlFor="di-active" className="text-sm font-normal">
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
