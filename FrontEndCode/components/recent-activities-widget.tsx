"use client";

import { Card } from "@/components/ui/card";
import { RecentActivity } from "@/lib/mock-data";
import { Plus, Edit2, Trash2, ArrowRight } from "lucide-react";

interface RecentActivitiesWidgetProps {
  activities: RecentActivity[];
}

const typeConfig = {
  add: { icon: Plus, color: "text-emerald-600", bg: "bg-emerald-100" },
  update: { icon: Edit2, color: "text-blue-600", bg: "bg-blue-100" },
  delete: { icon: Trash2, color: "text-red-600", bg: "bg-red-100" },
  status_change: {
    icon: ArrowRight,
    color: "text-purple-600",
    bg: "bg-purple-100",
  },
};

export function RecentActivitiesWidget({
  activities,
}: RecentActivitiesWidgetProps) {
  const formatTime = (date: Date) => {
    const now = new Date();
    const diff = now.getTime() - date.getTime();
    const hours = Math.floor(diff / (1000 * 60 * 60));
    const days = Math.floor(diff / (1000 * 60 * 60 * 24));

    if (hours === 0) return "Just now";
    if (hours < 24) return `${hours}h ago`;
    if (days === 1) return "Yesterday";
    return `${days}d ago`;
  };

  return (
    <Card className="p-6 border border-border bg-card">
      <div className="flex items-center justify-between mb-6">
        <h2 className="text-lg font-semibold text-foreground">Recent Activity</h2>
        <span className="text-xs font-medium text-muted-foreground">
          Last 24 hours
        </span>
      </div>

      <div className="space-y-3">
        {activities.slice(0, 5).map((activity) => {
          const config = typeConfig[activity.type];
          const Icon = config.icon;

          return (
            <div
              key={activity.id}
              className="flex items-start gap-3 p-3 rounded-lg border border-border hover:bg-muted/50 transition-colors"
            >
              <div className={`p-2 rounded-lg ${config.bg} shrink-0`}>
                <Icon size={16} className={config.color} />
              </div>
              <div className="flex-1 min-w-0">
                <p className="text-sm font-medium text-foreground">
                  {activity.action}
                </p>
                <p className="text-xs text-muted-foreground mt-0.5">
                  {activity.description}
                </p>
                <p className="text-xs text-muted-foreground mt-2">
                  <span className="font-medium">{activity.user}</span> •{" "}
                  {formatTime(activity.timestamp)}
                </p>
              </div>
            </div>
          );
        })}
      </div>

      {activities.length === 0 && (
        <div className="text-center py-8">
          <p className="text-sm text-muted-foreground">No recent activity</p>
        </div>
      )}
    </Card>
  );
}
