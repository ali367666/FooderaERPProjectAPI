"use client";

import { cn } from "@/lib/utils";

/** Shared frame of the POS tab pages (Sifarişlər, Tez satışlar, Rezervasiyalar, ...). */
export function PosPage({
  title,
  description,
  actions,
  className,
  children,
}: {
  title: string;
  description?: string;
  actions?: React.ReactNode;
  className?: string;
  children: React.ReactNode;
}) {
  return (
    <div className={cn("space-y-4 p-3 sm:p-4", className)}>
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div>
          <h1 className="text-xl font-bold">{title}</h1>
          {description && <p className="text-sm text-muted-foreground">{description}</p>}
        </div>
        {actions && <div className="flex flex-wrap items-center gap-2">{actions}</div>}
      </div>
      {children}
    </div>
  );
}

export function PosPageLoading() {
  return <div className="flex h-full items-center justify-center text-muted-foreground">Yüklənir...</div>;
}

export function PosPageError({ message }: { message: string }) {
  return (
    <div className="rounded-md border border-destructive/30 bg-destructive/10 px-4 py-3 text-sm text-destructive">
      {message}
    </div>
  );
}

export function PosEmpty({ children }: { children: React.ReactNode }) {
  return <p className="py-12 text-center text-sm text-muted-foreground">{children}</p>;
}

/**
 * Tile grid of the tables screen. `wide` gives fewer, larger columns for tiles that carry
 * buttons or several lines (reservations, kitchen, receipts).
 */
export function PosTileGrid({ wide = false, children }: { wide?: boolean; children: React.ReactNode }) {
  return (
    <div
      className={cn(
        "grid gap-3",
        wide
          ? "grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 2xl:grid-cols-5"
          : "grid-cols-3 sm:grid-cols-4 md:grid-cols-5 lg:grid-cols-6 xl:grid-cols-7 2xl:grid-cols-8",
      )}
    >
      {children}
    </div>
  );
}

/**
 * A tile like a table on the tables screen: `filled` is the colour of a busy table (white text
 * on it), otherwise it is a free table's card.
 */
export function posTileClass(filled?: string | null) {
  return cn(
    "relative flex min-h-32 select-none flex-col items-center justify-center gap-0.5 rounded-xl border-2 p-2 text-center transition-all",
    filled
      ? "border-transparent text-white shadow-md " + filled
      : "border-border bg-card text-card-foreground",
  );
}

/** Clickable variant of {@link posTileClass}. */
export function posTileButtonClass(filled?: string | null) {
  return cn(
    posTileClass(filled),
    "active:scale-95 disabled:cursor-not-allowed disabled:opacity-40",
    !filled && "hover:border-primary/40",
  );
}
