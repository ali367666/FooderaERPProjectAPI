"use client";

import { useState } from "react";
import { Check, ChevronsUpDown } from "lucide-react";
import { Button } from "@/components/ui/button";
import {
  Command,
  CommandEmpty,
  CommandGroup,
  CommandInput,
  CommandItem,
  CommandList,
} from "@/components/ui/command";
import { Popover, PopoverContent, PopoverTrigger } from "@/components/ui/popover";
import { cn } from "@/lib/utils";
import type { StockItem } from "@/lib/services/stock-item-service";
import { unitLabel } from "@/lib/services/stock-item-service";

type StockItemComboboxProps = {
  items: StockItem[];
  value: number | null;
  onChange: (id: number) => void;
  disabled?: boolean;
  placeholder?: string;
};

export function StockItemCombobox({
  items,
  value,
  onChange,
  disabled,
  placeholder = "Select stock item…",
}: StockItemComboboxProps) {
  const [open, setOpen] = useState(false);
  const selected = value != null ? items.find((i) => i.id === value) : undefined;
  return (
    <Popover open={open} onOpenChange={setOpen}>
      <PopoverTrigger asChild>
        <Button
          type="button"
          variant="outline"
          role="combobox"
          aria-expanded={open}
          className="w-full justify-between font-normal h-10"
          disabled={disabled}
        >
          <span className="truncate">
            {selected ? `${selected.name} · ${unitLabel(selected.unit)}` : placeholder}
          </span>
          <ChevronsUpDown className="ml-2 h-4 w-4 shrink-0 opacity-50" />
        </Button>
      </PopoverTrigger>
      <PopoverContent className="w-[var(--radix-popover-trigger-width)] min-w-[240px] p-0" align="start">
        <Command>
          <CommandInput placeholder="Search items…" />
          <CommandList>
            <CommandEmpty>No item found.</CommandEmpty>
            <CommandGroup>
              {items.map((item) => (
                <CommandItem
                  key={item.id}
                  value={`${item.id} ${item.name} ${item.barcode ?? ""}`}
                  onSelect={() => {
                    onChange(item.id);
                    setOpen(false);
                  }}
                >
                  <Check className={cn("h-4 w-4", value === item.id ? "opacity-100" : "opacity-0")} />
                  <span className="truncate">{item.name}</span>
                  <span className="text-muted-foreground text-xs shrink-0">{unitLabel(item.unit)}</span>
                </CommandItem>
              ))}
            </CommandGroup>
          </CommandList>
        </Command>
      </PopoverContent>
    </Popover>
  );
}
