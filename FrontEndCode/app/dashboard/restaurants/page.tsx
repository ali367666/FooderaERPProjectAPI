"use client";

import { useEffect, useMemo, useState } from "react";
import { QRCodeSVG } from "qrcode.react";
import { AdvancedTableFilters, type TableFilterDef } from "@/components/advanced-table-filters";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Checkbox } from "@/components/ui/checkbox";
import { Label } from "@/components/ui/label";
import { DataTable } from "@/components/data-table";
import { Input } from "@/components/ui/input";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import {
  createRestaurant,
  deleteRestaurant,
  getRestaurants,
  updateRestaurant,
  type Restaurant,
} from "@/lib/services/restaurant-service";
import { ApiFormError, getFieldErrorMessage, type FieldErrors } from "@/lib/api-error";
import { useSelectedCompany } from "@/contexts/selected-company-context";
import { filterBySelectedCompany } from "@/lib/company-scope-utils";
import { getCompanySettingsBranding } from "@/lib/services/company-settings-service";
import {
  getRestaurantModules,
  setRestaurantModules,
  type RestaurantModules,
} from "@/lib/services/restaurant-settings-service";
import { defaultFormCompanyId } from "@/lib/resolve-company-id";

type RestaurantFormState = {
  id: number | null;
  name: string;
  description: string;
  address: string;
  phone: string;
  email: string;
  companyId: string;
};

function emptyForm(): RestaurantFormState {
  return {
    id: null,
    name: "",
    description: "",
    address: "",
    phone: "",
    email: "",
    companyId: "",
  };
}

type RestaurantRow = {
  id: string;
  restaurantId: number;
  name: string;
  companyId: number;
  companyName: string;
  description: string;
  address: string;
  phone: string;
};

export default function RestaurantsPage() {
  const { companies, companiesLoading, selectedCompanyId: scopeCompanyId } = useSelectedCompany();
  const [restaurants, setRestaurants] = useState<Restaurant[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [isDialogOpen, setIsDialogOpen] = useState(false);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [form, setForm] = useState<RestaurantFormState>(emptyForm);
  const [isEditMode, setIsEditMode] = useState(false);
  const [fieldErrors, setFieldErrors] = useState<FieldErrors>({});
  const [qrTarget, setQrTarget] = useState<RestaurantRow | null>(null);
  const [storeModeByCompany, setStoreModeByCompany] = useState<Record<number, boolean>>({});
  const [komplexModeByCompany, setKomplexModeByCompany] = useState<Record<number, boolean>>({});
  const [moduleTarget, setModuleTarget] = useState<RestaurantRow | null>(null);
  const [moduleForm, setModuleForm] = useState<RestaurantModules | null>(null);
  const [moduleFormLoading, setModuleFormLoading] = useState(false);
  const [moduleFormSaving, setModuleFormSaving] = useState(false);

  const loadData = async (silent = false) => {
    try {
      if (!silent) setLoading(true);
      setError(null);
      const restaurantData = await getRestaurants();
      setRestaurants(restaurantData);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to load branches.");
    } finally {
      if (!silent) setLoading(false);
    }
  };

  useEffect(() => {
    void loadData();
  }, []);

  const sortedCompanyOptions = useMemo(
    () =>
      [...companies]
        .sort((a, b) => a.name.localeCompare(b.name))
        .map((c) => ({ value: String(c.id), label: c.name })),
    [companies],
  );

  const rows = useMemo<RestaurantRow[]>(
    () =>
      restaurants.map((restaurant) => ({
        id: String(restaurant.id),
        restaurantId: restaurant.id,
        name: restaurant.name,
        companyId: restaurant.companyId,
        companyName:
          restaurant.companyName ||
          companies.find((c) => c.id === restaurant.companyId)?.name ||
          `Company #${restaurant.companyId}`,
        description: restaurant.description || "-",
        address: restaurant.address || "-",
        phone: restaurant.phone || "-",
      })),
    [restaurants, companies],
  );

  // "QR göstər" yalnız restoran/menyu iş axını olan şirkətlərə aiddir — Mağaza (moduleDataSecimi)
  // rejimindəki şirkətlərin filiallarında ictimai menyu anlayışı yoxdur, ona görə düymə gizlədilir.
  useEffect(() => {
    const missingCompanyIds = Array.from(new Set(rows.map((r) => r.companyId))).filter(
      (id) => !(id in storeModeByCompany),
    );
    if (missingCompanyIds.length === 0) return;

    let cancelled = false;
    (async () => {
      const entries = await Promise.all(
        missingCompanyIds.map(async (companyId) => {
          try {
            const branding = await getCompanySettingsBranding(companyId);
            return [companyId, branding.moduleDataSecimi, branding.moduleKompleks] as const;
          } catch {
            return [companyId, false, false] as const;
          }
        }),
      );
      if (!cancelled) {
        setStoreModeByCompany((prev) => {
          const next = { ...prev };
          for (const [companyId, isStoreMode] of entries) next[companyId] = isStoreMode;
          return next;
        });
        setKomplexModeByCompany((prev) => {
          const next = { ...prev };
          for (const [companyId, , isKompleks] of entries) next[companyId] = isKompleks;
          return next;
        });
      }
    })();

    return () => {
      cancelled = true;
    };
  }, [rows, storeModeByCompany]);

  const scopedRows = useMemo(
    () => filterBySelectedCompany(rows, scopeCompanyId, (r) => r.companyId),
    [rows, scopeCompanyId],
  );

  const companiesForForm = useMemo(() => {
    if (scopeCompanyId == null) return companies;
    return companies.filter((c) => c.id === scopeCompanyId);
  }, [companies, scopeCompanyId]);

  const restaurantFilterDefs = useMemo<TableFilterDef<RestaurantRow>[]>(
    () => [
      {
        id: "restaurantId",
        label: "ID",
        ui: "number",
        match: (row, get) => {
          const q = get("restaurantId").trim().toLowerCase();
          if (!q) return true;
          return String(row.restaurantId).toLowerCase().includes(q);
        },
      },
      {
        id: "name",
        label: "Branch Name",
        ui: "text",
        match: (row, get) => {
          const q = get("name").trim().toLowerCase();
          if (!q) return true;
          return row.name.toLowerCase().includes(q);
        },
      },
      {
        id: "company",
        label: "Company",
        ui: "select",
        options: sortedCompanyOptions,
        match: (row, get) => {
          const v = get("company");
          if (!v) return true;
          return row.companyId === Number(v);
        },
      },
      {
        id: "description",
        label: "Description",
        ui: "text",
        match: (row, get) => {
          const q = get("description").trim().toLowerCase();
          if (!q) return true;
          return row.description.toLowerCase().includes(q);
        },
      },
      {
        id: "address",
        label: "Address",
        ui: "text",
        match: (row, get) => {
          const q = get("address").trim().toLowerCase();
          if (!q) return true;
          return row.address.toLowerCase().includes(q);
        },
      },
      {
        id: "phone",
        label: "Phone",
        ui: "text",
        match: (row, get) => {
          const q = get("phone").trim().toLowerCase();
          if (!q) return true;
          return row.phone.toLowerCase().includes(q);
        },
      },
      {
        id: "status",
        label: "Status",
        ui: "status",
        match: (_row, get) => {
          const v = get("status");
          return v === "all" || v === "active";
        },
      },
    ],
    [sortedCompanyOptions],
  );

  const columns = [
    { key: "restaurantId" as const, label: "ID" },
    { key: "name" as const, label: "Branch Name" },
    { key: "companyName" as const, label: "Company" },
    { key: "phone" as const, label: "Phone" },
    { key: "address" as const, label: "Address" },
    {
      key: "id" as const,
      label: "Status",
      render: () => (
        <Badge className="bg-emerald-100 text-emerald-800 hover:bg-emerald-100">
          Active
        </Badge>
      ),
    },
    {
      key: "companyId" as const,
      label: "QR Menu",
      render: (_v: number, row: RestaurantRow) =>
        storeModeByCompany[row.companyId] ? (
          <span className="text-xs text-muted-foreground">-</span>
        ) : (
          <Button variant="outline" size="sm" onClick={() => setQrTarget(row)}>
            QR göstər
          </Button>
        ),
    },
    {
      key: "description" as const,
      label: "Modules",
      render: (_v: string, row: RestaurantRow) =>
        komplexModeByCompany[row.companyId] ? (
          <Button variant="outline" size="sm" onClick={() => void handleOpenModules(row)}>
            Modullar
          </Button>
        ) : (
          <span className="text-xs text-muted-foreground">-</span>
        ),
    },
  ];

  const handleOpenModules = async (row: RestaurantRow) => {
    setModuleTarget(row);
    setModuleFormLoading(true);
    try {
      const modules = await getRestaurantModules(row.restaurantId);
      setModuleForm(modules);
    } catch (err) {
      window.alert(err instanceof Error ? err.message : "Failed to load branch modules.");
      setModuleForm(null);
    } finally {
      setModuleFormLoading(false);
    }
  };

  const handleSaveModules = async () => {
    if (!moduleTarget || !moduleForm) return;
    setModuleFormSaving(true);
    try {
      await setRestaurantModules(moduleTarget.restaurantId, moduleForm);
      setModuleTarget(null);
      setModuleForm(null);
    } catch (err) {
      window.alert(err instanceof Error ? err.message : "Failed to save branch modules.");
    } finally {
      setModuleFormSaving(false);
    }
  };

  const handleAdd = () => {
    setForm({ ...emptyForm(), companyId: defaultFormCompanyId(companies, scopeCompanyId) });
    setFieldErrors({});
    setIsEditMode(false);
    setIsDialogOpen(true);
  };

  const handleEdit = (row: RestaurantRow) => {
    const restaurant = restaurants.find((r) => r.id === row.restaurantId);
    if (!restaurant) return;

    setForm({
      id: restaurant.id,
      name: restaurant.name || "",
      description: restaurant.description || "",
      address: restaurant.address || "",
      phone: restaurant.phone || "",
      email: restaurant.email || "",
      companyId: String(restaurant.companyId || ""),
    });
    setFieldErrors({});
    setIsEditMode(true);
    setIsDialogOpen(true);
  };

  const handleDelete = async (row: RestaurantRow) => {
    if (!window.confirm(`Delete "${row.name}"?`)) return;
    try {
      setError(null);
      await deleteRestaurant(row.restaurantId);
      await loadData(true);
    } catch (err) {
      const message = err instanceof Error ? err.message : "Failed to delete branch.";
      setError(message);
      window.alert(message);
    }
  };

  const handleSave = async () => {
    const companyId = Number(form.companyId);
    if (!form.name.trim()) {
      const msg = "Branch name is required.";
      setError(msg);
      return;
    }
    if (!Number.isFinite(companyId) || companyId <= 0) {
      const msg = "Company selection is required.";
      setError(msg);
      return;
    }

    const payload = {
      name: form.name.trim(),
      description: form.description.trim() || undefined,
      address: form.address.trim() || undefined,
      phone: form.phone.trim() || undefined,
      email: form.email.trim() || undefined,
      companyId,
    };

    try {
      setIsSubmitting(true);
      setError(null);
      setFieldErrors({});
      if (isEditMode && form.id) {
        await updateRestaurant(form.id, payload);
      } else {
        await createRestaurant(payload);
      }
      setIsDialogOpen(false);
      setIsEditMode(false);
      setForm(emptyForm());
      await loadData(true);
    } catch (err) {
      if (err instanceof ApiFormError) setFieldErrors(err.fieldErrors);
      const message =
        err instanceof Error ? err.message : "Branch save failed due to an unexpected error.";
      setError(message);
      window.alert(message);
    } finally {
      setIsSubmitting(false);
    }
  };

  if (companiesLoading || loading) {
    return <div className="p-6 text-sm text-muted-foreground">Loading branches...</div>;
  }

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-3xl font-bold text-foreground">Branches</h1>
        <p className="text-muted-foreground mt-1">Manage branches.</p>
      </div>

      {error && (
        <div className="rounded-md border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-600">
          {error}
        </div>
      )}

      <Dialog
        open={isDialogOpen}
        onOpenChange={(open) => {
          setIsDialogOpen(open);
          if (!open) {
            setIsEditMode(false);
            setForm(emptyForm());
          }
        }}
      >
        <AdvancedTableFilters defs={restaurantFilterDefs} data={scopedRows}>
          {(filtered) => (
            <DataTable
              title="Branch List"
              columns={columns}
              data={filtered}
              idSortKey="restaurantId"
              searchableFields={["name", "companyName", "address", "phone", "description", "id"]}
              searchPlaceholder="Search branches..."
              onAdd={handleAdd}
              onEdit={handleEdit}
              onDelete={handleDelete}
            />
          )}
        </AdvancedTableFilters>

        <DialogContent className="sm:max-w-lg">
          <DialogHeader>
            <DialogTitle>{isEditMode ? "Edit Branch" : "Add Branch"}</DialogTitle>
            <DialogDescription>Enter branch details and save changes.</DialogDescription>
          </DialogHeader>

          <div className="grid grid-cols-1 gap-4 md:grid-cols-2">
            <div>
              <label className="mb-2 block text-sm font-medium text-foreground">Branch Name</label>
              <Input value={form.name} onChange={(e) => setForm((f) => ({ ...f, name: e.target.value }))} />
              {getFieldErrorMessage(fieldErrors, "name") && (
                <p className="mt-1 text-xs text-red-600">{getFieldErrorMessage(fieldErrors, "name")}</p>
              )}
            </div>
            <div>
              <label className="mb-2 block text-sm font-medium text-foreground">Company</label>
              <select
                value={form.companyId}
                onChange={(e) => setForm((f) => ({ ...f, companyId: e.target.value }))}
                className="flex h-10 w-full rounded-md border border-input bg-background px-3 py-2 text-sm ring-offset-background"
              >
                <option value="">Select company</option>
                {companiesForForm.map((company) => (
                  <option key={company.id} value={company.id}>
                    {company.name}
                  </option>
                ))}
              </select>
              {getFieldErrorMessage(fieldErrors, "companyid") && (
                <p className="mt-1 text-xs text-red-600">{getFieldErrorMessage(fieldErrors, "companyid")}</p>
              )}
            </div>
            <div>
              <label className="mb-2 block text-sm font-medium text-foreground">Phone</label>
              <Input value={form.phone} onChange={(e) => setForm((f) => ({ ...f, phone: e.target.value }))} />
            </div>
            <div>
              <label className="mb-2 block text-sm font-medium text-foreground">Email</label>
              <Input value={form.email} onChange={(e) => setForm((f) => ({ ...f, email: e.target.value }))} />
            </div>
            <div className="md:col-span-2">
              <label className="mb-2 block text-sm font-medium text-foreground">Address</label>
              <Input value={form.address} onChange={(e) => setForm((f) => ({ ...f, address: e.target.value }))} />
            </div>
            <div className="md:col-span-2">
              <label className="mb-2 block text-sm font-medium text-foreground">Description</label>
              <Input
                value={form.description}
                onChange={(e) => setForm((f) => ({ ...f, description: e.target.value }))}
              />
            </div>
          </div>

          <div className="flex justify-end gap-3">
            <Button variant="outline" onClick={() => setIsDialogOpen(false)} disabled={isSubmitting}>
              Cancel
            </Button>
            <Button onClick={handleSave} disabled={isSubmitting}>
              {isSubmitting ? "Saving..." : "Save"}
            </Button>
          </div>
        </DialogContent>
      </Dialog>

      <Dialog open={qrTarget != null} onOpenChange={(open) => !open && setQrTarget(null)}>
        <DialogContent className="sm:max-w-sm">
          <DialogHeader>
            <DialogTitle>{qrTarget?.name} — QR Menu</DialogTitle>
            <DialogDescription>Bu kodu masalara/qapıya çap edib qoya bilərsiniz.</DialogDescription>
          </DialogHeader>
          {qrTarget && (
            <div id="qr-menu-print-area" className="flex flex-col items-center gap-3 py-2">
              <QRCodeSVG
                value={`${typeof window !== "undefined" ? window.location.origin : ""}/menu/${qrTarget.restaurantId}`}
                size={220}
              />
              <p className="text-center text-sm font-medium text-foreground">{qrTarget.name}</p>
              <p className="break-all text-center text-xs text-muted-foreground">
                {typeof window !== "undefined" ? window.location.origin : ""}/menu/{qrTarget.restaurantId}
              </p>
            </div>
          )}
          <div className="flex justify-end gap-2">
            <Button variant="outline" onClick={() => setQrTarget(null)}>
              Bağla
            </Button>
            <Button onClick={() => window.print()}>Çap et</Button>
          </div>
        </DialogContent>
      </Dialog>

      <Dialog
        open={moduleTarget != null}
        onOpenChange={(open) => {
          if (!open) {
            setModuleTarget(null);
            setModuleForm(null);
          }
        }}
      >
        <DialogContent className="sm:max-w-md">
          <DialogHeader>
            <DialogTitle>{moduleTarget?.name} — Modullar</DialogTitle>
            <DialogDescription>
              Bu filialın öz aktiv modulları — ana şirkətin ümumi modullarının üzərinə yazılır.
            </DialogDescription>
          </DialogHeader>
          {moduleFormLoading && <p className="text-sm text-muted-foreground">Yüklənir...</p>}
          {!moduleFormLoading && moduleForm && (
            <div className="grid grid-cols-2 gap-3">
              {(
                [
                  { key: "moduleAnbar", label: "Anbar" },
                  { key: "moduleRezervasyon", label: "Rezervasiya" },
                  { key: "moduleMasaBolge", label: "Masa Bölgə" },
                  { key: "modulePaket", label: "Paket" },
                  { key: "moduleOtel", label: "Otel" },
                  { key: "moduleFitnes", label: "Fitnes" },
                  { key: "moduleDataSecimi", label: "Mağaza" },
                  { key: "moduleQiymetSor", label: "Qiymət Sor" },
                ] as Array<{ key: keyof RestaurantModules; label: string }>
              ).map((f) => (
                <label key={f.key} className="flex items-center gap-2 text-sm">
                  <Checkbox
                    checked={moduleForm[f.key]}
                    onCheckedChange={(v) =>
                      setModuleForm((prev) => (prev ? { ...prev, [f.key]: v === true } : prev))
                    }
                  />
                  <Label className="cursor-pointer font-normal">{f.label}</Label>
                </label>
              ))}
            </div>
          )}
          <div className="flex justify-end gap-2">
            <Button
              variant="outline"
              onClick={() => {
                setModuleTarget(null);
                setModuleForm(null);
              }}
              disabled={moduleFormSaving}
            >
              Ləğv et
            </Button>
            <Button onClick={() => void handleSaveModules()} disabled={moduleFormSaving || moduleFormLoading}>
              {moduleFormSaving ? "Saxlanılır..." : "Saxla"}
            </Button>
          </div>
        </DialogContent>
      </Dialog>

      <style>{`
        @media print {
          body * { visibility: hidden; }
          #qr-menu-print-area, #qr-menu-print-area * { visibility: visible; }
          #qr-menu-print-area {
            display: flex !important;
            position: fixed; top: 0; left: 0; width: 100%;
            align-items: center; justify-content: center; flex-direction: column;
          }
        }
      `}</style>
    </div>
  );
}
