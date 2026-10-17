"use client";

import { useEffect, useState } from "react";
import { Minus, Plus } from "lucide-react";
import { Button } from "@/components/ui/button";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import type { MenuItem } from "@/lib/services/menu-item-service";
import type { SetSelectionInput } from "@/lib/services/order-service";

type Props = {
  item: MenuItem | null;
  busy: boolean;
  onClose: () => void;
  onConfirm: (selections: SetSelectionInput[]) => void;
};

/** POS dialog for a SET with choice groups: the waiter fills every group to exactly its pick count. */
export function SetSelectionDialog({ item, busy, onClose, onConfirm }: Props) {
  // counts[groupId][menuItemId] = pieces picked
  const [counts, setCounts] = useState<Record<number, Record<number, number>>>({});

  useEffect(() => {
    setCounts({});
  }, [item?.id]);

  const groups = item?.setGroups ?? [];
  const pickedIn = (groupId: number) =>
    Object.values(counts[groupId] ?? {}).reduce((sum, n) => sum + n, 0);
  const complete = groups.length > 0 && groups.every((g) => pickedIn(g.id) === g.pickCount);

  const change = (groupId: number, menuItemId: number, delta: number, pickCount: number) => {
    setCounts((prev) => {
      const group = { ...(prev[groupId] ?? {}) };
      const total = Object.values(group).reduce((s, n) => s + n, 0);
      const current = group[menuItemId] ?? 0;
      if (delta > 0 && total >= pickCount) return prev;
      if (delta < 0 && current <= 0) return prev;
      group[menuItemId] = current + delta;
      return { ...prev, [groupId]: group };
    });
  };

  const handleConfirm = () => {
    const selections: SetSelectionInput[] = [];
    for (const group of groups) {
      for (const [menuItemId, quantity] of Object.entries(counts[group.id] ?? {})) {
        if (quantity > 0) selections.push({ groupId: group.id, menuItemId: Number(menuItemId), quantity });
      }
    }
    onConfirm(selections);
  };

  return (
    <Dialog open={item != null} onOpenChange={(o) => !o && onClose()}>
      <DialogContent className="sm:max-w-lg">
        <DialogHeader>
          <DialogTitle>{item?.name}</DialogTitle>
          <DialogDescription>Hər qrupdan tələb olunan sayda məhsul seçin.</DialogDescription>
        </DialogHeader>

        <div className="max-h-[60vh] space-y-5 overflow-y-auto pr-1">
          {groups.map((group) => {
            const picked = pickedIn(group.id);
            return (
              <div key={group.id} className="space-y-2">
                <div className="flex items-center justify-between">
                  <p className="text-sm font-semibold">{group.name}</p>
                  <span
                    className={`text-xs ${picked === group.pickCount ? "text-green-600" : "text-muted-foreground"}`}
                  >
                    {picked} / {group.pickCount}
                  </span>
                </div>
                <div className="space-y-1">
                  {group.options.map((option) => {
                    const count = counts[group.id]?.[option.menuItemId] ?? 0;
                    return (
                      <div
                        key={option.menuItemId}
                        className={`flex items-center justify-between rounded-md border px-3 py-2 text-sm ${
                          count > 0 ? "border-primary bg-primary/5" : ""
                        }`}
                      >
                        <span>{option.menuItemName}</span>
                        <div className="flex items-center gap-2">
                          <Button
                            type="button"
                            variant="outline"
                            size="icon"
                            className="h-8 w-8"
                            disabled={count === 0}
                            onClick={() => change(group.id, option.menuItemId, -1, group.pickCount)}
                          >
                            <Minus className="h-4 w-4" />
                          </Button>
                          <span className="w-5 text-center tabular-nums">{count}</span>
                          <Button
                            type="button"
                            variant="outline"
                            size="icon"
                            className="h-8 w-8"
                            disabled={picked >= group.pickCount}
                            onClick={() => change(group.id, option.menuItemId, 1, group.pickCount)}
                          >
                            <Plus className="h-4 w-4" />
                          </Button>
                        </div>
                      </div>
                    );
                  })}
                </div>
              </div>
            );
          })}
        </div>

        <DialogFooter>
          <Button variant="outline" onClick={onClose} disabled={busy}>
            Ləğv et
          </Button>
          <Button onClick={handleConfirm} disabled={busy || !complete}>
            Əlavə et
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
