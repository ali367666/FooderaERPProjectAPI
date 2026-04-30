"use client";

import type { ReactNode } from "react";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";

type MasterFormCardProps = {
  title: string;
  description?: string;
  children: ReactNode;
};

export function MasterFormCard({ title, description, children }: MasterFormCardProps) {
  return (
    <Card className="border-border">
      <CardHeader className="pb-4">
        <CardTitle className="text-base">{title}</CardTitle>
        {description ? <p className="text-sm text-muted-foreground font-normal">{description}</p> : null}
      </CardHeader>
      <CardContent className="space-y-4">{children}</CardContent>
    </Card>
  );
}
