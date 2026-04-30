'use client';

import { DashboardInventoryAlert } from '@/lib/dashboard-types';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { AlertTriangle, AlertCircle } from 'lucide-react';

interface InventoryAlertsWidgetProps {
  alerts: DashboardInventoryAlert[];
}

export function InventoryAlertsWidget({ alerts }: InventoryAlertsWidgetProps) {
  const criticalCount = alerts.filter(a => a.alertType === 'critical').length;
  const lowStockCount = alerts.filter(a => a.alertType === 'low_stock').length;

  const getAlertIcon = (alertType: string) => {
    return alertType === 'critical' ? (
      <AlertTriangle className="w-4 h-4 text-red-600" />
    ) : (
      <AlertCircle className="w-4 h-4 text-amber-600" />
    );
  };

  const getAlertBadgeClass = (alertType: string) => {
    return alertType === 'critical' 
      ? 'bg-red-100 text-red-800' 
      : 'bg-amber-100 text-amber-800';
  };

  const getProgressColor = (current: number, minimum: number) => {
    const percentage = (current / minimum) * 100;
    if (percentage < 25) return 'bg-red-500';
    if (percentage < 50) return 'bg-amber-500';
    return 'bg-yellow-500';
  };

  return (
    <Card className="col-span-1 md:col-span-1">
      <CardHeader className="pb-3">
        <div className="flex items-center justify-between">
          <div>
            <CardTitle className="flex items-center gap-2">
              <AlertTriangle className="w-5 h-5 text-red-600" />
              Inventory Alerts
            </CardTitle>
            <CardDescription>
              {criticalCount} critical, {lowStockCount} low stock
            </CardDescription>
          </div>
        </div>
      </CardHeader>
      <CardContent>
        {alerts.length === 0 ? (
          <div className="text-center py-8 text-muted-foreground">
            <p>All inventory levels normal</p>
          </div>
        ) : (
          <div className="space-y-4">
            {alerts.map(alert => (
              <div key={alert.id} className="border rounded-lg p-3 bg-muted/30">
                <div className="flex items-start justify-between gap-2 mb-2">
                  <div className="flex items-center gap-2 flex-1">
                    {getAlertIcon(alert.alertType)}
                    <div className="flex-1 min-w-0">
                      <h4 className="font-medium text-sm truncate">{alert.itemName}</h4>
                      <p className="text-xs text-muted-foreground">{alert.sku}</p>
                    </div>
                  </div>
                  <Badge className={getAlertBadgeClass(alert.alertType)}>
                    {alert.alertType === 'critical' ? 'Critical' : 'Low'}
                  </Badge>
                </div>

                <div className="mt-3 space-y-2">
                  <div className="flex justify-between items-center text-xs">
                    <span className="text-muted-foreground">
                      Current: <span className="font-medium text-foreground">{alert.currentStock} {alert.unit}</span>
                    </span>
                    <span className="text-muted-foreground">
                      Min: <span className="font-medium">{alert.minimumStock}</span>
                    </span>
                  </div>
                  <div className="w-full bg-gray-200 rounded-full h-1.5">
                    <div
                      className={`h-1.5 rounded-full ${getProgressColor(alert.currentStock, alert.minimumStock)}`}
                      style={{ width: `${Math.min((alert.currentStock / alert.minimumStock) * 100, 100)}%` }}
                    ></div>
                  </div>
                </div>

                <Button
                  size="sm"
                  variant="outline"
                  className="w-full mt-3 text-xs"
                >
                  Create Stock Request
                </Button>
              </div>
            ))}
          </div>
        )}
      </CardContent>
    </Card>
  );
}
