import { Card } from "@/components/ui/card";
import { TrendingUp, TrendingDown } from "lucide-react";
import { LucideIcon } from "lucide-react";

interface SummaryCardProps {
  icon: LucideIcon;
  title: string;
  value: string | number;
  trend?: {
    direction: "up" | "down";
    percentage: number;
  };
  description?: string;
}

export function SummaryCard({
  icon: Icon,
  title,
  value,
  trend,
  description,
}: SummaryCardProps) {
  return (
    <Card className="p-6 border border-border bg-card hover:shadow-md transition-shadow">
      <div className="flex items-start justify-between">
        <div className="flex-1">
          <p className="text-sm font-medium text-muted-foreground">{title}</p>
          <p className="text-3xl font-bold text-foreground mt-2">{value}</p>
          {description && (
            <p className="text-xs text-muted-foreground mt-1">{description}</p>
          )}
        </div>
        <div className="p-3 rounded-lg bg-primary/10">
          <Icon size={24} className="text-primary" />
        </div>
      </div>

      {trend && (
        <div className="mt-4 flex items-center gap-1">
          <div
            className={`flex items-center gap-1 text-sm font-medium ${
              trend.direction === "up" ? "text-emerald-600" : "text-red-600"
            }`}
          >
            {trend.direction === "up" ? (
              <TrendingUp size={16} />
            ) : (
              <TrendingDown size={16} />
            )}
            <span>{trend.percentage}%</span>
          </div>
          <span className="text-xs text-muted-foreground">
            {trend.direction === "up" ? "increase" : "decrease"}
          </span>
        </div>
      )}
    </Card>
  );
}
