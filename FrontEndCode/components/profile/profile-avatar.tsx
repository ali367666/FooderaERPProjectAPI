"use client";

import { Avatar, AvatarFallback, AvatarImage } from "@/components/ui/avatar";
import { cn } from "@/lib/utils";

function initials(name: string): string {
  return name
    .trim()
    .split(/\s+/)
    .slice(0, 2)
    .map((part) => part.charAt(0).toLocaleUpperCase("az"))
    .join("");
}

/** The user's photo; their initials while there is none. */
export function ProfileAvatar({ name, url, className }: { name: string; url: string | null; className?: string }) {
  return (
    <Avatar className={cn("size-10", className)}>
      {url && <AvatarImage src={url} alt={name} className="object-cover" />}
      <AvatarFallback className="bg-primary text-primary-foreground font-semibold">{initials(name)}</AvatarFallback>
    </Avatar>
  );
}
