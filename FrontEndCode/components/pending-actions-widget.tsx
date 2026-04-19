"use client";

import { Card } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { PendingAction } from "@/lib/mock-data";
import { Clock, AlertCircle, CheckCircle, FileText } from "lucide-react";

interface PendingActionsWidgetProps {
  actions: PendingAction[];
}

const typeConfig = {
  approval: { icon: CheckCircle, color: "bg-blue-100 text-blue-700" },
  request: { icon: FileText, color: "bg-purple-100 text-purple-700" },
  review: { icon: Clock, color: "bg-amber-100 text-amber-700" },
  urgent: { icon: AlertCircle, color: "bg-red-100 text-red-700" },
};

export function PendingActionsWidget({ actions }: PendingActionsWidgetProps) {
  const formatTime = (date: Date) => {
    const now = new Date();
    const diff = date.getTime() - now.getTime();
    const days = Math.ceil(diff / (1000 * 60 * 60 * 24));
    
    if (days === 0) return "Today";
    if (days === 1) return "Tomorrow";
    return `${days} days`;
  };

  return (
    <Card className="p-6 border border-border bg-card">
      <div className="flex items-center justify-between mb-6">
        <h2 className="text-lg font-semibold text-foreground">Pending Actions</h2>
        <span className="text-xs font-medium bg-accent/20 text-accent px-2.5 py-0.5 rounded-full">
          {actions.length} items
        </span>
      </div>

      <div className="space-y-3">
        {actions.map((action) => {
          const config = typeConfig[action.type];
          const Icon = config.icon;

          return (
            <div
              key={action.id}
              className="flex items-start gap-4 p-3 rounded-lg border border-border hover:bg-muted/50 transition-colors"
            >
              <div className={`p-2 rounded-lg ${config.color}`}>
                <Icon size={16} />
              </div>
              <div className="flex-1 min-w-0">
                <p className="text-sm font-medium text-foreground truncate">
                  {action.title}
                </p>
                <p className="text-xs text-muted-foreground mt-0.5">
                  {action.description}
                </p>
                <div className="flex items-center gap-2 mt-2">
                  <Clock size={12} className="text-muted-foreground" />
                  <span className="text-xs text-muted-foreground">
                    Due: {formatTime(action.dueDate)}
                  </span>
                </div>
              </div>
              <Button
                variant="ghost"
                size="sm"
                className="text-xs shrink-0 hover:bg-primary/10 text-primary"
              >
                Act
              </Button>
            </div>
          );
        })}
      </div>

      {actions.length === 0 && (
        <div className="text-center py-8">
          <CheckCircle size={32} className="mx-auto text-emerald-500 mb-2" />
          <p className="text-sm text-muted-foreground">All caught up!</p>
        </div>
      )}
    </Card>
  );
}
