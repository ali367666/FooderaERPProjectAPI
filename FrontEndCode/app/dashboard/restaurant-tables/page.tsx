"use client";

import { useEffect, useMemo, useState } from "react";
import { AdvancedTableFilters, type TableFilterDef } from "@/components/advanced-table-filters";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
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
  createRestaurantTable,
  deleteRestaurantTable,
  getRestaurantTables,
  updateRestaurantTable,
  type RestaurantTable,
} from "@/lib/services/restaurant-table-service";
import { getRestaurants, type Restaurant } from "@/lib/services/restaurant-service";
import { ApiFormError, getFieldErrorMessage, type FieldErrors } from "@/lib/api-error";
import { useSelectedCompany } from "@/contexts/selected-company-context";
import { filterBySelectedCompany } from "@/lib/company-scope-utils";

type RestaurantTableRow = {
  id: string;
  tableId: number;
  restaurantName: string;
  name: string;
  capacity: number;
  statusLabel: string;
  occupiedLabel: string;
  restaurantId: number;
  isActive: boolean;
  isOccupied: boolean;
};

type FormState = {
  id: number | null;
  restaurantId: string;
  name: string;
  capacity: string;
  isActive: boolean;
};

function emptyForm(): FormState {
  return { id: null, restaurantId: "", name: "", capacity: "", isActive: true };
}

export default function RestaurantTablesPage() {
  const { companiesLoading, selectedCompanyId: scopeCompanyId } = useSelectedCompany();
  const [tables, setTables] = useState<RestaurantTable[]>([]);
  const [restaurants, setRestaurants] = useState<Restaurant[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [isDialogOpen, setIsDialogOpen] = useState(false);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [isEditMode, setIsEditMode] = useState(false);
  const [form, setForm] = useState<FormState>(emptyForm);
  const [fieldErrors, setFieldErrors] = useState<FieldErrors>({});

  const restaurantNameById = useMemo(
    () => new Map(restaurants.map((restaurant) => [restaurant.id, restaurant.name])),
    [restaurants],
  );

  const restaurantsScoped = useMemo(() => {
    if (scopeCompanyId == null) return restaurants;
    return restaurants.filter((r) => r.companyId === scopeCompanyId);
  }, [restaurants, scopeCompanyId]);

  const sortedRestaurantOptions = useMemo(
    () =>
      [...restaurantsScoped]
        .sort((a, b) => a.name.localeCompare(b.name))
        .map((r) => ({ value: String(r.id), label: r.name })),
    [restaurantsScoped],
  );

  const restaurantCompanyMap = useMemo(
    () => new Map(restaurants.map((r) => [r.id, r.companyId])),
    [restaurants],
  );

  const loadData = async (silent = false) => {
    try {
      if (!silent) setLoading(true);
      setError(null);
      const [tableData, restaurantData] = await Promise.all([getRestaurantTables(), getRestaurants()]);
      setTables(tableData);
      setRestaurants(restaurantData);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to load restaurant tables.");
    } finally {
      if (!silent) setLoading(false);
    }
  };

  useEffect(() => {
    void loadData();
  }, []);

  const allRows = useMemo<RestaurantTableRow[]>(() => {
    return tables.map((table) => ({
      id: String(table.id),
      tableId: table.id,
      restaurantName:
        table.restaurantName?.trim() ||
        restaurantNameById.get(table.restaurantId) ||
        `Restaurant #${table.restaurantId}`,
      name: table.name,
      capacity: table.capacity,
      statusLabel: table.isActive ? "Active" : "Inactive",
      occupiedLabel: table.isOccupied ? "Occupied" : "Available",
      restaurantId: table.restaurantId,
      isActive: table.isActive,
      isOccupied: table.isOccupied,
    }));
  }, [tables, restaurantNameById]);

  const scopedRows = useMemo(
    () =>
      filterBySelectedCompany(allRows, scopeCompanyId, (row) =>
        restaurantCompanyMap.get(row.restaurantId),
      ),
    [allRows, scopeCompanyId, restaurantCompanyMap],
  );

  const restaurantTableFilterDefs = useMemo<TableFilterDef<RestaurantTableRow>[]>(
    () => [
      {
        id: "tableId",
        label: "ID",
        ui: "number",
        match: (row, get) => {
          const q = get("tableId").trim().toLowerCase();
          if (!q) return true;
          return String(row.tableId).toLowerCase().includes(q);
        },
      },
      {
        id: "restaurant",
        label: "Restaurant",
        ui: "select",
        options: sortedRestaurantOptions,
        match: (row, get) => {
          const v = get("restaurant");
          if (!v) return true;
          return row.restaurantId === Number(v);
        },
      },
      {
        id: "name",
        label: "Table No",
        ui: "text",
        match: (row, get) => {
          const q = get("name").trim().toLowerCase();
          if (!q) return true;
          return row.name.toLowerCase().includes(q);
        },
      },
      {
        id: "capacity",
        label: "Capacity",
        ui: "numberRange",
        match: (row, get) => {
          const minCap = get("capacity:min").trim();
          const maxCap = get("capacity:max").trim();
          const minN = minCap === "" ? NaN : Number(minCap);
          const maxN = maxCap === "" ? NaN : Number(maxCap);
          if (minCap !== "" && (!Number.isFinite(minN) || row.capacity < minN)) return false;
          if (maxCap !== "" && (!Number.isFinite(maxN) || row.capacity > maxN)) return false;
          return true;
        },
      },
      {
        id: "occupancy",
        label: "Occupancy",
        ui: "occupancy",
        match: (row, get) => {
          const v = get("occupancy");
          if (v === "all") return true;
          if (v === "available") return !row.isOccupied;
          if (v === "occupied") return row.isOccupied;
          return true;
        },
      },
      {
        id: "status",
        label: "Status",
        ui: "status",
        match: (row, get) => {
          const v = get("status");
          if (v === "all") return true;
          if (v === "active") return row.isActive;
          if (v === "inactive") return !row.isActive;
          return true;
        },
      },
    ],
    [sortedRestaurantOptions],
  );

  const columns = [
    { key: "tableId" as const, label: "ID" },
    { key: "restaurantName" as const, label: "Restaurant" },
    { key: "name" as const, label: "Table No" },
    { key: "capacity" as const, label: "Capacity" },
    { key: "occupiedLabel" as const, label: "Occupancy" },
    {
      key: "statusLabel" as const,
      label: "Status",
      render: (_: string, row: RestaurantTableRow) => (
        <Badge
          className={
            row.isActive
              ? "bg-emerald-100 text-emerald-800 hover:bg-emerald-100"
              : "bg-amber-100 text-amber-800 hover:bg-amber-100"
          }
        >
          {row.statusLabel}
        </Badge>
      ),
    },
  ];

  const handleAdd = () => {
    setForm(emptyForm());
    setFieldErrors({});
    setIsEditMode(false);
    setIsDialogOpen(true);
  };

  const handleEdit = (row: RestaurantTableRow) => {
    const target = tables.find((table) => table.id === row.tableId);
    if (!target) return;
    setForm({
      id: target.id,
      restaurantId: String(target.restaurantId),
      name: target.name || "",
      capacity: String(target.capacity || ""),
      isActive: target.isActive,
    });
    setFieldErrors({});
    setIsEditMode(true);
    setIsDialogOpen(true);
  };

  const handleDelete = async (row: RestaurantTableRow) => {
    if (!window.confirm(`Delete table "${row.name}"?`)) return;
    try {
      setError(null);
      await deleteRestaurantTable(row.tableId);
      await loadData(true);
    } catch (err) {
      const message = err instanceof Error ? err.message : "Failed to delete restaurant table.";
      setError(message);
      window.alert(message);
    }
  };

  const handleSave = async () => {
    const restaurantId = Number(form.restaurantId);
    const capacity = Number(form.capacity);
    if (!Number.isFinite(restaurantId) || restaurantId <= 0) {
      setError("Restaurant selection is required.");
      return;
    }
    if (!form.name.trim()) {
      setError("Table number is required.");
      return;
    }
    if (!Number.isFinite(capacity) || capacity <= 0) {
      setError("Capacity must be greater than 0.");
      return;
    }

    const payload = {
      restaurantId,
      name: form.name.trim(),
      capacity,
      isActive: form.isActive,
    };

    try {
      setIsSubmitting(true);
      setError(null);
      setFieldErrors({});
      if (isEditMode && form.id) {
        await updateRestaurantTable(form.id, payload);
      } else {
        await createRestaurantTable(payload);
      }
      setIsDialogOpen(false);
      setIsEditMode(false);
      setForm(emptyForm());
      await loadData(true);
    } catch (err) {
      if (err instanceof ApiFormError) setFieldErrors(err.fieldErrors);
      const message =
        err instanceof Error
          ? err.message
          : "Restaurant table save failed due to an unexpected error.";
      setError(message);
      window.alert(message);
    } finally {
      setIsSubmitting(false);
    }
  };

  if (companiesLoading || loading) {
    return <div className="p-6 text-sm text-muted-foreground">Loading restaurant tables...</div>;
  }

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-3xl font-bold text-foreground">Restaurant Tables</h1>
        <p className="text-muted-foreground mt-1">Manage tables with restaurant names and capacity.</p>
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
        <AdvancedTableFilters defs={restaurantTableFilterDefs} data={scopedRows}>
          {(filtered) => (
            <DataTable
              title="Restaurant Table List"
              columns={columns}
              data={filtered}
              idSortKey="tableId"
              searchableFields={[
                "id",
                "restaurantName",
                "name",
                "capacity",
                "occupiedLabel",
                "statusLabel",
              ]}
              searchPlaceholder="Search restaurant tables..."
              onAdd={handleAdd}
              onEdit={handleEdit}
              onDelete={handleDelete}
            />
          )}
        </AdvancedTableFilters>

        <DialogContent className="sm:max-w-md">
          <DialogHeader>
            <DialogTitle>{isEditMode ? "Edit Restaurant Table" : "Add Restaurant Table"}</DialogTitle>
            <DialogDescription>Set restaurant, table number, and capacity.</DialogDescription>
          </DialogHeader>

          <div className="space-y-4">
            <div>
              <label className="mb-2 block text-sm font-medium text-foreground">Restaurant</label>
              <select
                value={form.restaurantId}
                onChange={(e) => setForm((f) => ({ ...f, restaurantId: e.target.value }))}
                className="flex h-10 w-full rounded-md border border-input bg-background px-3 py-2 text-sm ring-offset-background"
              >
                <option value="">Select restaurant</option>
                {restaurantsScoped.map((restaurant) => (
                  <option key={restaurant.id} value={restaurant.id}>
                    {restaurant.name}
                  </option>
                ))}
              </select>
              {getFieldErrorMessage(fieldErrors, "restaurantid") && (
                <p className="mt-1 text-xs text-red-600">{getFieldErrorMessage(fieldErrors, "restaurantid")}</p>
              )}
            </div>

            <div>
              <label className="mb-2 block text-sm font-medium text-foreground">Table No</label>
              <Input value={form.name} onChange={(e) => setForm((f) => ({ ...f, name: e.target.value }))} />
              {getFieldErrorMessage(fieldErrors, "name") && (
                <p className="mt-1 text-xs text-red-600">{getFieldErrorMessage(fieldErrors, "name")}</p>
              )}
            </div>

            <div>
              <label className="mb-2 block text-sm font-medium text-foreground">Capacity</label>
              <Input
                type="number"
                value={form.capacity}
                onChange={(e) => setForm((f) => ({ ...f, capacity: e.target.value }))}
              />
              {getFieldErrorMessage(fieldErrors, "capacity") && (
                <p className="mt-1 text-xs text-red-600">{getFieldErrorMessage(fieldErrors, "capacity")}</p>
              )}
            </div>

            {isEditMode && (
              <div>
                <label className="mb-2 block text-sm font-medium text-foreground">Status</label>
                <select
                  value={form.isActive ? "true" : "false"}
                  onChange={(e) => setForm((f) => ({ ...f, isActive: e.target.value === "true" }))}
                  className="flex h-10 w-full rounded-md border border-input bg-background px-3 py-2 text-sm ring-offset-background"
                >
                  <option value="true">Active</option>
                  <option value="false">Inactive</option>
                </select>
              </div>
            )}

            <div className="flex justify-end gap-3">
              <Button variant="outline" onClick={() => setIsDialogOpen(false)} disabled={isSubmitting}>
                Cancel
              </Button>
              <Button onClick={handleSave} disabled={isSubmitting}>
                {isSubmitting ? "Saving..." : "Save"}
              </Button>
            </div>
          </div>
        </DialogContent>
      </Dialog>
    </div>
  );
}
