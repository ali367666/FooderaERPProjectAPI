"use client";

import { Button } from "@/components/ui/button";
import type { ComponentProps } from "react";

export type WorkflowActionItem = {
  key: string;
  label: string;
  onClick: () => void;
  variant?: ComponentProps<typeof Button>["variant"];
  show: boolean;
  disabled?: boolean;
  title?: string;
};

export function WorkflowActions({ actions }: { actions: WorkflowActionItem[] }) {
  const visible = actions.filter((a) => a.show);
  if (visible.length === 0) return null;
  return (
    <div className="flex flex-wrap gap-2">
      {visible.map((a) => (
        <Button
          key={a.key}
          type="button"
          size="sm"
          variant={a.variant ?? "secondary"}
          disabled={a.disabled}
          title={a.title}
          onClick={a.onClick}
        >
          {a.label}
        </Button>
      ))}
    </div>
  );
}
