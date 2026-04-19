'use client';

import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Plus, Package, Truck, ShoppingCart } from 'lucide-react';
import { useState } from 'react';

export function ERPQuickActionsWidget() {
  const [successAction, setSuccessAction] = useState<string | null>(null);

  const handleAction = (action: string) => {
    setSuccessAction(action);
    setTimeout(() => setSuccessAction(null), 2000);
  };

  const quickActions = [
    {
      id: 'stock_request',
      label: 'Create Stock Request',
      icon: Package,
      color: 'bg-blue-50 hover:bg-blue-100 text-blue-700',
      description: 'Request new stock from suppliers'
    },
    {
      id: 'transfer',
      label: 'Create Transfer',
      icon: Truck,
      color: 'bg-purple-50 hover:bg-purple-100 text-purple-700',
      description: 'Transfer inventory between locations'
    },
    {
      id: 'order',
      label: 'Create Order',
      icon: ShoppingCart,
      color: 'bg-orange-50 hover:bg-orange-100 text-orange-700',
      description: 'Create a new purchase order'
    },
  ];

  return (
    <Card className="col-span-1 md:col-span-1">
      <CardHeader className="pb-3">
        <CardTitle className="flex items-center gap-2">
          <Plus className="w-5 h-5 text-primary" />
          Quick Actions
        </CardTitle>
        <CardDescription>Frequently used operations</CardDescription>
      </CardHeader>
      <CardContent>
        <div className="grid grid-cols-1 gap-2">
          {quickActions.map(action => {
            const Icon = action.icon;
            const isSuccess = successAction === action.id;

            return (
              <Button
                key={action.id}
                variant="ghost"
                className={`h-auto p-3 justify-start transition-all ${
                  isSuccess 
                    ? 'bg-green-50 text-green-700' 
                    : action.color
                }`}
                onClick={() => handleAction(action.id)}
              >
                <Icon className="w-4 h-4 mr-3 flex-shrink-0" />
                <div className="text-left">
                  <p className="font-medium text-sm">
                    {isSuccess ? '✓ Action queued' : action.label}
                  </p>
                  {!isSuccess && (
                    <p className="text-xs opacity-70">{action.description}</p>
                  )}
                </div>
              </Button>
            );
          })}
        </div>
      </CardContent>
    </Card>
  );
}
