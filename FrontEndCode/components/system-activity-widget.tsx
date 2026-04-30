'use client';

import { DashboardRecentActivity } from '@/lib/dashboard-types';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Activity, Package, Truck, ShoppingCart, CheckCircle2 } from 'lucide-react';

interface SystemActivityWidgetProps {
  activities: DashboardRecentActivity[];
}

const activityIcons: Record<string, React.ReactNode> = {
  stock_movement: <Package className="w-4 h-4 text-blue-600" />,
  transfer: <Truck className="w-4 h-4 text-purple-600" />,
  order_update: <ShoppingCart className="w-4 h-4 text-orange-600" />,
  approval: <CheckCircle2 className="w-4 h-4 text-green-600" />,
};

const activityLabels: Record<string, string> = {
  stock_movement: 'Stock Movement',
  transfer: 'Transfer',
  order_update: 'Order Update',
  approval: 'Approval',
};

const statusBadgeClass: Record<string, string> = {
  completed: 'bg-green-100 text-green-800',
  in_progress: 'bg-blue-100 text-blue-800',
  pending: 'bg-amber-100 text-amber-800',
};

export function SystemActivityWidget({ activities }: SystemActivityWidgetProps) {
  const formatTime = (dateValue: string) => {
    const date = new Date(dateValue);
    const now = new Date();
    const diff = now.getTime() - date.getTime();
    const mins = Math.floor(diff / (1000 * 60));
    const hours = Math.floor(diff / (1000 * 60 * 60));
    const days = Math.floor(diff / (1000 * 60 * 60 * 24));

    if (mins < 1) return 'Just now';
    if (mins < 60) return `${mins}m ago`;
    if (hours < 24) return `${hours}h ago`;
    return `${days}d ago`;
  };

  return (
    <Card className="col-span-1 md:col-span-2 lg:col-span-1">
      <CardHeader className="pb-3">
        <CardTitle className="flex items-center gap-2">
          <Activity className="w-5 h-5 text-primary" />
          Recent Activity
        </CardTitle>
        <CardDescription>Latest system operations</CardDescription>
      </CardHeader>
      <CardContent>
        {activities.length === 0 ? (
          <div className="text-center py-8 text-muted-foreground">
            <p>No recent activities</p>
          </div>
        ) : (
          <div className="space-y-3 max-h-96 overflow-y-auto">
            {activities.map((activity, index) => (
              <div key={activity.id} className="relative pb-3">
                {index !== activities.length - 1 && (
                  <div className="absolute left-[18px] top-10 w-0.5 h-6 bg-border"></div>
                )}
                <div className="flex gap-3">
                  <div className="flex-shrink-0 w-9 h-9 rounded-full bg-muted flex items-center justify-center">
                    {activityIcons[activity.type]}
                  </div>
                  <div className="flex-1 min-w-0 pt-1">
                    <div className="flex items-start justify-between gap-2">
                      <div>
                        <p className="text-sm font-medium">
                          {activity.description}
                        </p>
                        <p className="text-xs text-muted-foreground mt-1">
                          {activity.details}
                        </p>
                      </div>
                      <Badge className={statusBadgeClass[activity.status]} variant="secondary">
                        {activity.status === 'completed' ? 'Done' : 
                         activity.status === 'in_progress' ? 'In Progress' : 'Pending'}
                      </Badge>
                    </div>
                    <div className="flex items-center gap-2 mt-2">
                      <span className="text-xs text-muted-foreground">
                        by <span className="font-medium">{activity.user}</span>
                      </span>
                      <span className="text-xs text-muted-foreground">
                        {formatTime(activity.timestamp)}
                      </span>
                    </div>
                  </div>
                </div>
              </div>
            ))}
          </div>
        )}
      </CardContent>
    </Card>
  );
}
