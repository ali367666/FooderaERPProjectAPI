"use client";

import { useEffect, useState } from "react";
import { CreditCard, QrCode } from "lucide-react";
import { cn } from "@/lib/utils";
import { getPaymentBanks, type PaymentBank } from "@/lib/services/order-service";

/** Logos in /public/banks (from Wikimedia Commons, see ATTRIBUTION.md there), matched on the bank's name. */
function bankLogo(name: string): string | null {
  const key = name.toLocaleLowerCase("az").replace(/ş/g, "s");
  if (key.includes("kapital")) return "/banks/kapital.png";
  if (key.includes("pasa")) return "/banks/pasha.png";
  if (key.includes("abb") || key.includes("beynəlxalq")) return "/banks/abb.png";
  return null;
}

const isQr = (name: string) => name.toLocaleLowerCase("az").includes("qr");

/**
 * The company's payment banks (Kapital, Paşa, ABB, QR kod) — a card payment must name one.
 * Loaded once the dialog first opens; one bank is picked for the cashier straight away.
 */
export function usePaymentBanks(open: boolean) {
  const [banks, setBanks] = useState<PaymentBank[] | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!open || banks !== null) return;
    let cancelled = false;
    getPaymentBanks()
      .then((list) => !cancelled && setBanks(list))
      .catch((err) => !cancelled && setError(err instanceof Error ? err.message : "Banklar yüklənmədi"));
    return () => {
      cancelled = true;
    };
  }, [open, banks]);

  return { banks, error };
}

export function BankPicker({
  banks,
  error,
  value,
  onChange,
  disabled,
}: {
  banks: PaymentBank[] | null;
  error?: string | null;
  value: number | null;
  onChange: (bankId: number) => void;
  disabled?: boolean;
}) {
  // A single bank needs no question.
  useEffect(() => {
    if (banks?.length === 1 && value !== banks[0].id) onChange(banks[0].id);
  }, [banks, value, onChange]);

  if (error) return <p className="text-sm text-destructive">{error}</p>;
  if (banks === null) return <p className="text-sm text-muted-foreground">Banklar yüklənir…</p>;
  if (banks.length === 0) return <p className="text-sm text-destructive">Kart ödənişi üçün bank tapılmadı.</p>;

  return (
    <div className="space-y-1.5">
      <p className="text-sm font-medium">Bank</p>
      <BankTiles banks={banks} value={value} onPick={onChange} disabled={disabled} />
    </div>
  );
}

/** The logo tiles alone; a tap calls `onPick` (quick sale sells on it straight away). */
export function BankTiles({
  banks,
  value = null,
  onPick,
  disabled,
}: {
  banks: PaymentBank[];
  value?: number | null;
  onPick: (bankId: number) => void;
  disabled?: boolean;
}) {
  return (
    <div className="grid grid-cols-2 gap-2">
      {banks.map((bank) => {
        const logo = bankLogo(bank.name);
        const selected = value === bank.id;
        return (
          <button
            key={bank.id}
            type="button"
            disabled={disabled}
            onClick={() => onPick(bank.id)}
            aria-pressed={selected}
            aria-label={bank.name}
            className={cn(
              // Logos are drawn for a white background, so the tile stays white in dark mode too.
              "flex h-16 items-center justify-center rounded-lg border-2 bg-white px-3 transition-all",
              "active:scale-95 disabled:opacity-50",
              selected ? "border-primary ring-2 ring-primary/30" : "border-border hover:border-primary/50",
            )}
          >
            {logo ? (
              // eslint-disable-next-line @next/next/no-img-element
              <img src={logo} alt={bank.name} className="max-h-10 max-w-full object-contain" />
            ) : (
              <span className="flex items-center gap-2 text-base font-semibold text-neutral-900">
                {isQr(bank.name) ? <QrCode className="h-6 w-6" /> : <CreditCard className="h-6 w-6" />}
                {bank.name}
              </span>
            )}
          </button>
        );
      })}
    </div>
  );
}
