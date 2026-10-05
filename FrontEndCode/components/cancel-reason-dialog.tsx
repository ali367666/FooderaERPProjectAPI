"use client";

import { useCallback, useRef, useState } from "react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import type { CancelReason } from "@/lib/services/order-service";

/** Preset ləğv səbəbləri — shown on the POS and in the cancellations report as-is. */
export const CANCEL_REASONS = [
  "Müştəri imtina etdi",
  "Səhv vuruldu",
  "Məhsul bitib",
  "Gecikmə",
  "Keyfiyyət problemi",
  "Digər",
] as const;

type Pending = { title: string; resolve: (value: CancelReason | null) => void };

/**
 * Asks for a cancellation reason. Usage:
 *   const [reasonDialog, askReason] = useCancelReason();
 *   const r = await askReason("Sifarişi ləğv et"); if (!r) return;
 * and render {reasonDialog} once in the page.
 */
export function useCancelReason(): [React.ReactNode, (title: string) => Promise<CancelReason | null>] {
  const [pending, setPending] = useState<Pending | null>(null);
  const [reason, setReason] = useState("");
  const [note, setNote] = useState("");
  const pendingRef = useRef<Pending | null>(null);

  const ask = useCallback((title: string) => {
    pendingRef.current?.resolve(null);
    return new Promise<CancelReason | null>((resolve) => {
      const next = { title, resolve };
      pendingRef.current = next;
      setReason("");
      setNote("");
      setPending(next);
    });
  }, []);

  const finish = (value: CancelReason | null) => {
    pendingRef.current?.resolve(value);
    pendingRef.current = null;
    setPending(null);
  };

  const dialog = (
    <Dialog open={pending != null} onOpenChange={(open) => !open && finish(null)}>
      <DialogContent className="sm:max-w-md">
        <DialogHeader>
          <DialogTitle>{pending?.title}</DialogTitle>
          <DialogDescription>Ləğv səbəbini seçin — ləğvlər hesabatında görünəcək.</DialogDescription>
        </DialogHeader>
        <div className="grid grid-cols-2 gap-2">
          {CANCEL_REASONS.map((r) => (
            <Button key={r} type="button" variant={reason === r ? "default" : "outline"} onClick={() => setReason(r)}>
              {r}
            </Button>
          ))}
        </div>
        <div>
          <Label htmlFor="cancel-note">Qeyd (istəyə görə)</Label>
          <Input
            id="cancel-note"
            className="mt-1"
            value={note}
            maxLength={500}
            onChange={(e) => setNote(e.target.value)}
            placeholder={reason === "Digər" ? "Səbəbi yazın" : ""}
          />
        </div>
        <div className="flex justify-end gap-2">
          <Button variant="outline" onClick={() => finish(null)}>
            İmtina
          </Button>
          <Button
            variant="destructive"
            disabled={!reason || (reason === "Digər" && !note.trim())}
            onClick={() => finish({ reason, note: note.trim() || undefined })}
          >
            Ləğv et
          </Button>
        </div>
      </DialogContent>
    </Dialog>
  );

  return [dialog, ask];
}
