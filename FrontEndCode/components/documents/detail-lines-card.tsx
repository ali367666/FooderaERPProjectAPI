"use client";

import type { ReactNode } from "react";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";

type DetailLinesCardProps = {
  title?: string;
  lineCount: number;
  headerRight?: ReactNode;
  children: ReactNode;
};

export function DetailLinesCard({
  title = "Detail lines",
  lineCount,
  headerRight,
  children,
}: DetailLinesCardProps) {
  return (
    <Card className="border-border">
      <CardHeader className="flex flex-row flex-wrap items-center justify-between gap-2 pb-4">
        <CardTitle className="text-base">
          {title}{" "}
          <span className="text-muted-foreground font-normal">({lineCount} lines)</span>
        </CardTitle>
        {headerRight}
      </CardHeader>
      <CardContent>{children}</CardContent>
    </Card>
  );
}
