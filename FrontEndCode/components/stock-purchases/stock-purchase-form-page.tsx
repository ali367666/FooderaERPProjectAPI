"use client";

import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { toast } from "sonner";
import {
  getStockPurchaseById,
  createStockPurchase,
  updateStockPurchase,
  type StockPurchaseDto,
  type CreateStockPurchasePayload,
  type UpdateStockPurchasePayload,
} from "@/lib/services/stock-purchase-service";
import { StockPurchaseMasterForm } from "./stock-purchase-master-form";
import { useSelectedCompany } from "@/contexts/selected-company-context";
import { toApiFormError } from "@/lib/api-error";

type Props = {
  id?: number;
  mode: "create" | "edit" | "view";
};

export function StockPurchaseFormPage({ id, mode }: Props) {
  const router = useRouter();
  const { selectedCompanyId } = useSelectedCompany();
  const [purchase, setPurchase] = useState<StockPurchaseDto | undefined>(undefined);
  const [loading, setLoading] = useState(id != null);

  useEffect(() => {
    if (id == null) return;
    void (async () => {
      try {
        const data = await getStockPurchaseById(id);
        setPurchase(data);
      } catch (e) {
        toast.error(toApiFormError(e, "Failed to load purchase").message);
      } finally {
        setLoading(false);
      }
    })();
  }, [id]);

  const companyId = purchase?.companyId ?? selectedCompanyId ?? 0;
  const readOnly = mode === "view";

  if (loading) return <p className="text-sm text-muted-foreground">Loading…</p>;

  async function handleSave(payload: CreateStockPurchasePayload | UpdateStockPurchasePayload) {
    if (id == null) {
      const newId = await createStockPurchase(payload as CreateStockPurchasePayload);
      toast.success("Stock purchase created");
      router.push(`/dashboard/stock-purchases/${newId}?mode=view`);
    } else {
      await updateStockPurchase(id, payload as UpdateStockPurchasePayload);
      toast.success("Stock purchase updated");
      router.push(`/dashboard/stock-purchases/${id}?mode=view`);
    }
  }

  const title = mode === "create" ? "New Stock Purchase" : mode === "edit" ? "Edit Stock Purchase" : `Stock Purchase — ${purchase?.documentNo}`;

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-semibold tracking-tight">{title}</h1>
        {mode === "view" && purchase && (
          <p className="text-muted-foreground text-sm mt-1">
            Konturagent: {purchase.counterpartyName} · {purchase.warehouseName} · {purchase.currencyCode}
          </p>
        )}
      </div>
      <StockPurchaseMasterForm
        companyId={companyId}
        initial={purchase}
        readOnly={readOnly}
        onSave={handleSave}
        onCancel={() => router.push("/dashboard/stock-purchases")}
      />
    </div>
  );
}
