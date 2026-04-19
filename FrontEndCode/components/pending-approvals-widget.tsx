'use client';

import { PendingApproval } from '@/lib/mock-data';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { CheckCircle, XCircle, Eye, Clock } from 'lucide-react';
import { useState } from 'react';

interface PendingApprovalsWidgetProps {
  approvals: PendingApproval[];
}

const typeColors = {
  stock_request: 'bg-blue-100 text-blue-800',
  warehouse_transfer: 'bg-purple-100 text-purple-800',
  order: 'bg-orange-100 text-orange-800',
};

const typeLabels = {
  stock_request: 'Stock Request',
  warehouse_transfer: 'Warehouse Transfer',
  order: 'Order',
};

export function PendingApprovalsWidget({ approvals }: PendingApprovalsWidgetProps) {
  const [actions, setActions] = useState<{ [key: string]: string }>({});

  const handleApprove = (id: string) => {
    setActions(prev => ({ ...prev, [id]: 'approved' }));
  };

  const handleReject = (id: string) => {
    setActions(prev => ({ ...prev, [id]: 'rejected' }));
  };

  const getIcon = (type: PendingApproval['type']) => {
    switch (type) {
      case 'stock_request':
        return '📦';
      case 'warehouse_transfer':
        return '🚚';
      case 'order':
        return '🛒';
      default:
        return '📝';
    }
  };

  const formatDate = (date: Date) => {
    const now = new Date();
    const diff = now.getTime() - date.getTime();
    const hours = Math.floor(diff / (1000 * 60 * 60));
    if (hours < 1) {
      const mins = Math.floor(diff / (1000 * 60));
      return `${mins}m ago`;
    }
    if (hours < 24) {
      return `${hours}h ago`;
    }
    return date.toLocaleDateString();
  };

  const pendingCount = approvals.filter(a => actions[a.id] !== 'approved' && actions[a.id] !== 'rejected').length;

  return (
    <Card className="col-span-1 md:col-span-2">
      <CardHeader className="pb-3">
        <div className="flex items-center justify-between">
          <div>
            <CardTitle className="flex items-center gap-2">
              <Clock className="w-5 h-5 text-primary" />
              Pending Approvals
            </CardTitle>
            <CardDescription>
              {pendingCount} approval{pendingCount !== 1 ? 's' : ''} waiting for action
            </CardDescription>
          </div>
          {pendingCount > 0 && (
            <Badge variant="destructive" className="text-lg px-3 py-1">
              {pendingCount}
            </Badge>
          )}
        </div>
      </CardHeader>
      <CardContent>
        {approvals.length === 0 ? (
          <div className="text-center py-8 text-muted-foreground">
            <p>No pending approvals</p>
          </div>
        ) : (
          <div className="space-y-3">
            {approvals.map(approval => {
              const actionState = actions[approval.id];
              if (actionState === 'approved' || actionState === 'rejected') {
                return null;
              }

              return (
                <div key={approval.id} className="border rounded-lg p-4 hover:bg-muted/50 transition-colors">
                  <div className="flex items-start justify-between gap-4">
                    <div className="flex-1 min-w-0">
                      <div className="flex items-center gap-2 mb-2">
                        <span className="text-xl">{getIcon(approval.type)}</span>
                        <Badge className={typeColors[approval.type]}>
                          {typeLabels[approval.type]}
                        </Badge>
                        <span className="text-xs text-muted-foreground">
                          {formatDate(approval.createdAt)}
                        </span>
                      </div>
                      <h4 className="font-medium truncate">{approval.title}</h4>
                      <p className="text-sm text-muted-foreground mt-1">
                        Created by <span className="font-medium">{approval.createdBy}</span>
                      </p>
                      {approval.items && (
                        <p className="text-sm text-muted-foreground mt-1">
                          Items: {approval.items}
                          {approval.quantity && ` (${approval.quantity} units)`}
                        </p>
                      )}
                    </div>
                    <div className="flex gap-2 flex-shrink-0">
                      <Button
                        size="sm"
                        variant="outline"
                        onClick={() => handleApprove(approval.id)}
                        className="text-green-600 hover:text-green-700 hover:bg-green-50"
                      >
                        <CheckCircle className="w-4 h-4 mr-1" />
                        Approve
                      </Button>
                      <Button
                        size="sm"
                        variant="outline"
                        onClick={() => handleReject(approval.id)}
                        className="text-red-600 hover:text-red-700 hover:bg-red-50"
                      >
                        <XCircle className="w-4 h-4 mr-1" />
                        Reject
                      </Button>
                      <Button
                        size="sm"
                        variant="ghost"
                      >
                        <Eye className="w-4 h-4" />
                      </Button>
                    </div>
                  </div>
                </div>
              );
            })}
          </div>
        )}
      </CardContent>
    </Card>
  );
}
