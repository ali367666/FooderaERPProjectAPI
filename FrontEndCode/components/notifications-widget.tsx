"use client";

import { Card } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { Notification } from "@/lib/mock-data";
import {
  Info,
  CheckCircle,
  AlertTriangle,
  AlertCircle,
  X,
} from "lucide-react";

interface NotificationsWidgetProps {
  notifications: Notification[];
}

const typeConfig = {
  info: {
    icon: Info,
    color: "bg-blue-100 text-blue-700",
    dot: "bg-blue-500",
  },
  success: {
    icon: CheckCircle,
    color: "bg-emerald-100 text-emerald-700",
    dot: "bg-emerald-500",
  },
  warning: {
    icon: AlertTriangle,
    color: "bg-amber-100 text-amber-700",
    dot: "bg-amber-500",
  },
  error: {
    icon: AlertCircle,
    color: "bg-red-100 text-red-700",
    dot: "bg-red-500",
  },
};

export function NotificationsWidget({
  notifications,
}: NotificationsWidgetProps) {
  const unreadCount = notifications.filter((n) => !n.read).length;

  const formatTime = (date: Date) => {
    const now = new Date();
    const diff = now.getTime() - date.getTime();
    const minutes = Math.floor(diff / (1000 * 60));
    const hours = Math.floor(diff / (1000 * 60 * 60));
    const days = Math.floor(diff / (1000 * 60 * 60 * 24));

    if (minutes < 60) return `${minutes}m ago`;
    if (hours < 24) return `${hours}h ago`;
    return `${days}d ago`;
  };

  return (
    <Card className="p-6 border border-border bg-card">
      <div className="flex items-center justify-between mb-6">
        <h2 className="text-lg font-semibold text-foreground">Notifications</h2>
        {unreadCount > 0 && (
          <span className="text-xs font-medium bg-red-100 text-red-700 px-2.5 py-0.5 rounded-full">
            {unreadCount} new
          </span>
        )}
      </div>

      <div className="space-y-2">
        {notifications.slice(0, 4).map((notification) => {
          const config = typeConfig[notification.type];
          const Icon = config.icon;

          return (
            <div
              key={notification.id}
              className={`p-3 rounded-lg border transition-colors ${
                notification.read
                  ? "border-border bg-background/50"
                  : "border-border bg-muted/50"
              } hover:bg-muted`}
            >
              <div className="flex items-start gap-3">
                <div className={`p-1.5 rounded-lg ${config.color} shrink-0`}>
                  <Icon size={16} />
                </div>
                <div className="flex-1 min-w-0">
                  <div className="flex items-center gap-2">
                    <p className="text-sm font-medium text-foreground">
                      {notification.title}
                    </p>
                    {!notification.read && (
                      <div
                        className={`w-2 h-2 rounded-full ${config.dot}`}
                      />
                    )}
                  </div>
                  <p className="text-xs text-muted-foreground mt-0.5">
                    {notification.message}
                  </p>
                  <p className="text-xs text-muted-foreground mt-1.5">
                    {formatTime(notification.timestamp)}
                  </p>
                </div>
              </div>
            </div>
          );
        })}
      </div>

      {notifications.length === 0 && (
        <div className="text-center py-8">
          <CheckCircle size={32} className="mx-auto text-muted-foreground mb-2 opacity-50" />
          <p className="text-sm text-muted-foreground">No notifications</p>
        </div>
      )}
    </Card>
  );
}
