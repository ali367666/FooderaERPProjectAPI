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
  createCompany,
  deleteCompany,
  getCompanyById,
  getCompanies,
  updateCompany,
  type Company,
  type CompanyMutationInput,
} from "@/lib/services/company-service";
import { ApiFormError, getFieldErrorMessage, type FieldErrors } from "@/lib/api-error";

type CompanyRow = {
  id: string;
  companyId: number;
  name: string;
  companyCode: string;
  email: string;
  phone: string;
  address: string;
  description: string;
};

type CompanyFormState = {
  id: number | null;
  companyCode: string;
  name: string;
  email: string;
  primaryPhoneNumber: string;
  secondaryPhoneNumber: string;
  address: string;
  description: string;
  taxNumber: string;
  taxOfficeCode: string;
  country: string;
};

const COUNTRY_OPTIONS = [
  { value: 1, label: "Azerbaijan", code: "AZE" },
  { value: 2, label: "Turkey", code: "TUR" },
  { value: 3, label: "Germany", code: "GER" },
  { value: 4, label: "USA", code: "USA" },
] as const;

function getCountryValueFromCode(code?: string | null): string {
  if (!code) return "";
  const normalized = code.trim().toUpperCase();
  const fromCode = COUNTRY_OPTIONS.find((option) => option.code === normalized);
  if (fromCode) return String(fromCode.value);
  const numeric = Number(normalized);
  if (Number.isFinite(numeric) && COUNTRY_OPTIONS.some((o) => o.value === numeric)) {
    return String(numeric);
  }
  if (normalized === "DEU") return "3";
  return "";
}

function emptyCompanyForm(): CompanyFormState {
  return {
    id: null,
    companyCode: "",
    name: "",
    email: "",
    primaryPhoneNumber: "",
    secondaryPhoneNumber: "",
    address: "",
    description: "",
    taxNumber: "",
    taxOfficeCode: "",
    country: "",
  };
}

export default function CompaniesPage() {
  const [companies, setCompanies] = useState<Company[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [isDialogOpen, setIsDialogOpen] = useState(false);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [isEditMode, setIsEditMode] = useState(false);
  const [form, setForm] = useState<CompanyFormState>(emptyCompanyForm);
  const [fieldErrors, setFieldErrors] = useState<FieldErrors>({});

  const loadCompanies = async (silent = false) => {
    try {
      if (!silent) setLoading(true);
      setError(null);
      const data = await getCompanies();
      setCompanies(data);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to load companies.");
      throw err;
    } finally {
      if (!silent) setLoading(false);
    }
  };

  useEffect(() => {
    let isMounted = true;
    (async () => {
      try {
        setLoading(true);
        setError(null);
        const data = await getCompanies();
        if (isMounted) setCompanies(data);
      } catch (err) {
        if (isMounted) {
          setError(err instanceof Error ? err.message : "Failed to load companies.");
        }
      } finally {
        if (isMounted) setLoading(false);
      }
    })();

    return () => {
      isMounted = false;
    };
  }, []);

  const rows: CompanyRow[] = useMemo(
    () =>
      companies.map((company) => ({
        id: String(company.id),
        companyId: company.id,
        name: company.name,
        companyCode: company.companyCode || "-",
        email: company.email || "-",
        phone: company.primaryPhoneNumber || company.secondaryPhoneNumber || "-",
        address: company.address || "-",
        description: (company.description || "").trim(),
      })),
    [companies],
  );

  const companyFilterDefs = useMemo<TableFilterDef<CompanyRow>[]>(
    () => [
      {
        id: "companyId",
        label: "ID",
        ui: "number",
        match: (row, get) => {
          const q = get("companyId").trim().toLowerCase();
          if (!q) return true;
          return String(row.companyId).toLowerCase().includes(q);
        },
      },
      {
        id: "name",
        label: "Company Name",
        ui: "text",
        match: (row, get) => {
          const q = get("name").trim().toLowerCase();
          if (!q) return true;
          return row.name.toLowerCase().includes(q);
        },
      },
      {
        id: "companyCode",
        label: "Code",
        ui: "text",
        match: (row, get) => {
          const q = get("companyCode").trim().toLowerCase();
          if (!q) return true;
          return row.companyCode.toLowerCase().includes(q);
        },
      },
      {
        id: "email",
        label: "Email",
        ui: "text",
        match: (row, get) => {
          const q = get("email").trim().toLowerCase();
          if (!q) return true;
          return row.email.toLowerCase().includes(q);
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
        id: "status",
        label: "Status",
        ui: "status",
        match: (_row, get) => {
          const v = get("status");
          if (v === "all" || v === "active") return true;
          return false;
        },
      },
    ],
    [],
  );

  const columns = [
    { key: "companyId" as const, label: "ID" },
    { key: "name" as const, label: "Company Name" },
    { key: "companyCode" as const, label: "Code" },
    { key: "email" as const, label: "Email" },
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
  ];

  const handleAdd = () => {
    setForm(emptyCompanyForm());
    setFieldErrors({});
    setIsEditMode(false);
    setIsDialogOpen(true);
  };

  const handleEdit = async (row: CompanyRow) => {
    try {
      setError(null);
      const company = await getCompanyById(row.companyId);

      setForm({
        id: company.id,
        companyCode: company.companyCode || "",
        name: company.name || "",
        email: company.email || "",
        primaryPhoneNumber: company.primaryPhoneNumber || "",
        secondaryPhoneNumber: company.secondaryPhoneNumber || "",
        address: company.address || "",
        description: company.description || "",
        taxNumber: company.taxNumber || "",
        taxOfficeCode: company.taxOfficeCode || "",
        country: getCountryValueFromCode(company.countryCode),
      });
      setFieldErrors({});
      setIsEditMode(true);
      setIsDialogOpen(true);
    } catch (err) {
      const message = err instanceof Error ? err.message : "Failed to load company details.";
      setError(message);
      window.alert(message);
    }
  };

  const handleDelete = async (row: CompanyRow) => {
    const confirmed = window.confirm(`Delete "${row.name}"?`);
    if (!confirmed) return;

    try {
      setError(null);
      await deleteCompany(row.companyId);
      await loadCompanies(true);
    } catch (err) {
      const message = err instanceof Error ? err.message : "Failed to delete company.";
      setError(message);
      window.alert(message);
    }
  };

  const handleSave = async () => {
    const code = form.companyCode.trim();
    const name = form.name.trim();

    if (!code || !name) {
      const msg = "Company code and company name are required.";
      setError(msg);
      window.alert(msg);
      return;
    }

    const selectedCountry = Number(form.country);
    if (!Number.isFinite(selectedCountry) || selectedCountry <= 0) {
      const msg = "Country selection is required.";
      setError(msg);
      setFieldErrors((prev) => ({ ...prev, country: [msg] }));
      window.alert(msg);
      return;
    }

    const payload: CompanyMutationInput = {
      companyCode: code,
      name,
      description: form.description.trim() || undefined,
      address: form.address.trim() || undefined,
      taxNumber: form.taxNumber.trim() || undefined,
      taxOfficeCode: form.taxOfficeCode.trim() || undefined,
      email: form.email.trim() || undefined,
      primaryPhoneNumber: form.primaryPhoneNumber.trim() || undefined,
      secondaryPhoneNumber: form.secondaryPhoneNumber.trim() || undefined,
      country: selectedCountry,
    };

    try {
      setIsSubmitting(true);
      setError(null);
      setFieldErrors({});

      if (isEditMode && form.id) {
        await updateCompany(form.id, payload);
      } else {
        await createCompany(payload);
      }

      setIsDialogOpen(false);
      setIsEditMode(false);
      setForm(emptyCompanyForm());
      await loadCompanies(true);
    } catch (err) {
      const message =
        err instanceof Error
          ? err.message
          : "Company save failed due to an unexpected error.";
      if (err instanceof ApiFormError) {
        setFieldErrors(err.fieldErrors);
      }
      setError(message);
      window.alert(message);
    } finally {
      setIsSubmitting(false);
    }
  };

  if (loading) {
    return <div className="p-6 text-sm text-muted-foreground">Loading companies...</div>;
  }

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-3xl font-bold text-foreground">Companies</h1>
        <p className="text-muted-foreground mt-1">Manage companies and master details.</p>
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
            setForm(emptyCompanyForm());
          }
        }}
      >
        <AdvancedTableFilters defs={companyFilterDefs} data={rows}>
          {(filtered) => (
            <DataTable
              title="Company List"
              columns={columns}
              data={filtered}
              idSortKey="companyId"
              searchableFields={["name", "companyCode", "email", "phone", "address", "description", "id"]}
              searchPlaceholder="Search companies..."
              onAdd={handleAdd}
              onEdit={handleEdit}
              onDelete={handleDelete}
            />
          )}
        </AdvancedTableFilters>

        <DialogContent className="sm:max-w-lg">
          <DialogHeader>
            <DialogTitle>{isEditMode ? "Edit Company" : "Add Company"}</DialogTitle>
            <DialogDescription>Enter company details and save changes.</DialogDescription>
          </DialogHeader>

          <div className="grid grid-cols-1 gap-4 md:grid-cols-2">
            <div>
              <label className="mb-2 block text-sm font-medium text-foreground">
                Company Code
              </label>
              <Input
                value={form.companyCode}
                onChange={(e) => setForm((f) => ({ ...f, companyCode: e.target.value }))}
                placeholder="Enter company code"
              />
              {getFieldErrorMessage(fieldErrors, "companycode") && (
                <p className="mt-1 text-xs text-red-600">
                  {getFieldErrorMessage(fieldErrors, "companycode")}
                </p>
              )}
            </div>

            <div>
              <label className="mb-2 block text-sm font-medium text-foreground">
                Company Name
              </label>
              <Input
                value={form.name}
                onChange={(e) => setForm((f) => ({ ...f, name: e.target.value }))}
                placeholder="Enter company name"
              />
              {getFieldErrorMessage(fieldErrors, "name") && (
                <p className="mt-1 text-xs text-red-600">
                  {getFieldErrorMessage(fieldErrors, "name")}
                </p>
              )}
            </div>

            <div>
              <label className="mb-2 block text-sm font-medium text-foreground">Email</label>
              <Input
                value={form.email}
                onChange={(e) => setForm((f) => ({ ...f, email: e.target.value }))}
                placeholder="Enter email"
              />
              {getFieldErrorMessage(fieldErrors, "email") && (
                <p className="mt-1 text-xs text-red-600">
                  {getFieldErrorMessage(fieldErrors, "email")}
                </p>
              )}
            </div>

            <div>
              <label className="mb-2 block text-sm font-medium text-foreground">
                Primary Phone
              </label>
              <Input
                value={form.primaryPhoneNumber}
                onChange={(e) =>
                  setForm((f) => ({ ...f, primaryPhoneNumber: e.target.value }))
                }
                placeholder="Enter primary phone"
              />
              {getFieldErrorMessage(fieldErrors, "primaryphonenumber") && (
                <p className="mt-1 text-xs text-red-600">
                  {getFieldErrorMessage(fieldErrors, "primaryphonenumber")}
                </p>
              )}
            </div>

            <div>
              <label className="mb-2 block text-sm font-medium text-foreground">
                Secondary Phone
              </label>
              <Input
                value={form.secondaryPhoneNumber}
                onChange={(e) =>
                  setForm((f) => ({ ...f, secondaryPhoneNumber: e.target.value }))
                }
                placeholder="Enter secondary phone"
              />
              {getFieldErrorMessage(fieldErrors, "secondaryphonenumber") && (
                <p className="mt-1 text-xs text-red-600">
                  {getFieldErrorMessage(fieldErrors, "secondaryphonenumber")}
                </p>
              )}
            </div>

            <div>
              <label className="mb-2 block text-sm font-medium text-foreground">
                Tax Number
              </label>
              <Input
                value={form.taxNumber}
                onChange={(e) => setForm((f) => ({ ...f, taxNumber: e.target.value }))}
                placeholder="Enter tax number"
              />
              {getFieldErrorMessage(fieldErrors, "taxnumber") && (
                <p className="mt-1 text-xs text-red-600">
                  {getFieldErrorMessage(fieldErrors, "taxnumber")}
                </p>
              )}
            </div>

            <div>
              <label className="mb-2 block text-sm font-medium text-foreground">
                Tax Office Code
              </label>
              <Input
                value={form.taxOfficeCode}
                onChange={(e) => setForm((f) => ({ ...f, taxOfficeCode: e.target.value }))}
                placeholder="Enter tax office code"
              />
              {getFieldErrorMessage(fieldErrors, "taxofficecode") && (
                <p className="mt-1 text-xs text-red-600">
                  {getFieldErrorMessage(fieldErrors, "taxofficecode")}
                </p>
              )}
            </div>

            <div>
              <label className="mb-2 block text-sm font-medium text-foreground">Country</label>
              <select
                value={form.country}
                onChange={(e) => setForm((f) => ({ ...f, country: e.target.value }))}
                className="flex h-10 w-full rounded-md border border-input bg-background px-3 py-2 text-sm ring-offset-background"
              >
                <option value="">Select country</option>
                {COUNTRY_OPTIONS.map((option) => (
                  <option key={option.value} value={option.value}>
                    {option.label}
                  </option>
                ))}
              </select>
              {getFieldErrorMessage(fieldErrors, "country", "countrycode") && (
                <p className="mt-1 text-xs text-red-600">
                  {getFieldErrorMessage(fieldErrors, "country", "countrycode")}
                </p>
              )}
            </div>

            <div className="md:col-span-2">
              <label className="mb-2 block text-sm font-medium text-foreground">Address</label>
              <Input
                value={form.address}
                onChange={(e) => setForm((f) => ({ ...f, address: e.target.value }))}
                placeholder="Enter address"
              />
              {getFieldErrorMessage(fieldErrors, "address") && (
                <p className="mt-1 text-xs text-red-600">
                  {getFieldErrorMessage(fieldErrors, "address")}
                </p>
              )}
            </div>

            <div className="md:col-span-2">
              <label className="mb-2 block text-sm font-medium text-foreground">
                Description
              </label>
              <Input
                value={form.description}
                onChange={(e) => setForm((f) => ({ ...f, description: e.target.value }))}
                placeholder="Enter description"
              />
              {getFieldErrorMessage(fieldErrors, "description") && (
                <p className="mt-1 text-xs text-red-600">
                  {getFieldErrorMessage(fieldErrors, "description")}
                </p>
              )}
            </div>
          </div>

          <div className="flex justify-end gap-3">
            <Button
              variant="outline"
              onClick={() => {
                setIsDialogOpen(false);
                setIsEditMode(false);
                setForm(emptyCompanyForm());
              }}
              disabled={isSubmitting}
            >
              Cancel
            </Button>
            <Button onClick={handleSave} disabled={isSubmitting}>
              {isSubmitting ? "Saving..." : "Save"}
            </Button>
          </div>
        </DialogContent>
      </Dialog>
    </div>
  );
}

