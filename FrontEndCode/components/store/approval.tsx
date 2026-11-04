"use client";

import { useCallback, useRef, useState } from "react";
import { ShieldCheck } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from "@/components/ui/dialog";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import type { StoreApproval } from "@/lib/store/store-service";

/** True when the server refused a step because it needs (or did not accept) a manager's approval. */
export function isApprovalError(error: unknown): boolean {
  const message = error instanceof Error ? error.message : "";
  return /menecer|təsdiq/i.test(message);
}

type Pending = { message: string; resolve: (approval: StoreApproval | null) => void };

/**
 * Manager approval at the till. `run(fn)` calls `fn` without approval first; when the server says a manager
 * must approve, a small window asks for the manager's name and password and `fn` is called again with them.
 */
export function useStoreApproval() {
  const [pending, setPending] = useState<Pending | null>(null);
  const [code, setCode] = useState("");
  const [reason, setReason] = useState("");
  const settle = useRef<((a: StoreApproval | null) => void) | null>(null);

  const ask = useCallback(
    (message: string) =>
      new Promise<StoreApproval | null>((resolve) => {
        settle.current = resolve;
        setCode("");
        setPending({ message, resolve });
      }),
    [],
  );

  const close = (approval: StoreApproval | null) => {
    settle.current?.(approval);
    settle.current = null;
    setPending(null);
  };

  const run = useCallback(
    async <T,>(fn: (approval?: StoreApproval) => Promise<T>): Promise<T> => {
      try {
        return await fn(undefined);
      } catch (first) {
        if (!isApprovalError(first)) throw first;

        let message = first instanceof Error ? first.message : "Menecer təsdiqi lazımdır.";
        // A wrong password asks again instead of dropping the whole step.
        for (let attempt = 0; attempt < 5; attempt++) {
          const approval = await ask(message);
          if (!approval) throw first;
          try {
            return await fn(approval);
          } catch (again) {
            if (!isApprovalError(again)) throw again;
            message = again instanceof Error ? again.message : message;
          }
        }
        throw first;
      }
    },
    [ask],
  );

  const dialog = (
    <Dialog open={pending != null} onOpenChange={(o) => !o && close(null)}>
      <DialogContent className="sm:max-w-sm">
        <DialogHeader>
          <DialogTitle className="flex items-center gap-2">
            <ShieldCheck className="h-5 w-5" />
            Menecer təsdiqi
          </DialogTitle>
          <DialogDescription>{pending?.message}</DialogDescription>
        </DialogHeader>
        <form
          className="space-y-3"
          onSubmit={(e) => {
            e.preventDefault();
            if (!code.trim()) return;
            close({ code: code.trim(), reason: reason.trim() || null });
          }}
        >
          <div className="space-y-1">
            <Label htmlFor="appr-code">Menecerin kodu</Label>
            <Input id="appr-code" type="password" autoFocus autoComplete="new-password" value={code} onChange={(e) => setCode(e.target.value)} />
          </div>
          <div className="space-y-1">
            <Label htmlFor="appr-reason">Səbəb (istəyə bağlı)</Label>
            <Input id="appr-reason" value={reason} onChange={(e) => setReason(e.target.value)} />
          </div>
          <DialogFooter>
            <Button type="button" variant="outline" onClick={() => close(null)}>
              Ləğv et
            </Button>
            <Button type="submit" disabled={!code.trim()}>
              Təsdiq et
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );

  return { run, dialog };
}
