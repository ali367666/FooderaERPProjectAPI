"use client";

import { useEffect, useState } from "react";
import type { OrderReceiptDto } from "@/lib/services/order-service";
import { renderReceipt, type ReceiptDesign } from "@/lib/receipt-render";

/** Shows the receipt exactly as it will be printed (the same image that is sent to the printer). */
export function ReceiptPreview({
  receipt,
  design,
  categoryName,
  className = "",
}: {
  receipt: OrderReceiptDto;
  design: ReceiptDesign;
  categoryName?: (id: number | null | undefined) => string;
  className?: string;
}) {
  const [src, setSrc] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;
    // Small delay so typing in the settings form doesn't redraw on every keystroke.
    const timer = window.setTimeout(() => {
      void renderReceipt(receipt, design, categoryName).then((canvas) => {
        if (!cancelled) setSrc(canvas.toDataURL("image/png"));
      });
    }, 150);
    return () => {
      cancelled = true;
      window.clearTimeout(timer);
    };
  }, [receipt, design, categoryName]);

  if (!src) return <div className="p-4 text-sm text-muted-foreground">Çek hazırlanır…</div>;
  return (
    // eslint-disable-next-line @next/next/no-img-element
    <img
      src={src}
      alt="Çekin önizləməsi"
      className={`mx-auto w-full max-w-[320px] border bg-white shadow-sm ${className}`}
    />
  );
}
