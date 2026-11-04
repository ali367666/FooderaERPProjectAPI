"use client";

import { useState } from "react";
import { toast } from "sonner";
import { Button } from "@/components/ui/button";
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from "@/components/ui/dialog";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";

/** Asks for the reason and cancels a payment by a counter entry (the payment stays in the history, marked reversed). */
export default function ReversePaymentDialog({
  open,
  title,
  onClose,
  onConfirm,
}: {
  open: boolean;
  title: string;
  onClose: () => void;
  onConfirm: (reason: string) => Promise<void>;
}) {
  const [reason, setReason] = useState("");
  const [busy, setBusy] = useState(false);

  const confirm = async () => {
    setBusy(true);
    try {
      await onConfirm(reason.trim());
      setReason("");
      onClose();
    } catch (e) {
      toast.error(e instanceof Error ? e.message : "Ödəniş ləğv edilmədi.");
    } finally {
      setBusy(false);
    }
  };

  return (
    <Dialog open={open} onOpenChange={(o) => !o && !busy && onClose()}>
      <DialogContent className="sm:max-w-sm">
        <DialogHeader>
          <DialogTitle>{title}</DialogTitle>
          <DialogDescription>Ödəniş silinmir, əks yazılışla ləğv edilir və tarixçədə qalır.</DialogDescription>
        </DialogHeader>
        <div className="space-y-2">
          <Label htmlFor="rev-reason">Səbəb</Label>
          <Input id="rev-reason" value={reason} onChange={(e) => setReason(e.target.value)} />
        </div>
        <DialogFooter>
          <Button variant="outline" disabled={busy} onClick={onClose}>
            Geri
          </Button>
          <Button variant="destructive" disabled={busy || !reason.trim()} onClick={() => void confirm()}>
            Ləğv et
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
