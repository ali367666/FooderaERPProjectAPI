'use client';

import { useState } from 'react';
import { SummaryCard } from '@/components/summary-card';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { PendingApprovalsWidget } from '@/components/pending-approvals-widget';
import { InventoryAlertsWidget } from '@/components/inventory-alerts-widget';
import { SystemActivityWidget } from '@/components/system-activity-widget';
import { ERPQuickActionsWidget } from '@/components/erp-quick-actions-widget';
import {
  LineChart,
  Line,
  BarChart,
  Bar,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  Legend,
  ResponsiveContainer,
  PieChart,
  Pie,
  Cell,
} from 'recharts';
import { Calendar, TrendingUp, Package, AlertTriangle } from 'lucide-react';
import {
  pendingApprovals,
  systemActivities,
  inventoryAlerts,
  inventoryItems,
} from '@/lib/mock-data';

// Mock data for charts
const stockMovementData = [
  { date: 'Mon', received: 240, issued: 180 },
  { date: 'Tue', received: 320, issued: 200 },
  { date: 'Wed', received: 280, issued: 220 },
  { date: 'Thu', received: 350, issued: 240 },
  { date: 'Fri', received: 410, issued: 290 },
  { date: 'Sat', received: 280, issued: 210 },
  { date: 'Sun', received: 200, issued: 150 },
];

const departmentInventoryData = [
  { name: 'Herbs & Spices', value: 35, fill: '#0f3d2e' },
  { name: 'Oils & Condiments', value: 25, fill: '#556b3f' },
  { name: 'Dairy', value: 20, fill: '#d97706' },
  { name: 'Canned Goods', value: 20, fill: '#059669' },
];

const orderStatusData = [
  { name: 'Pending', value: 12 },
  { name: 'Processing', value: 8 },
  { name: 'Shipped', value: 24 },
  { name: 'Delivered', value: 56 },
];

export default function DashboardPage() {
  const [timeFilter, setTimeFilter] = useState<"7d" | "30d">("7d");

  const criticalAlerts = inventoryAlerts.filter(
    (a) => a.alertType === "critical"
  ).length;

  const lowStockAlerts = inventoryAlerts.filter(
    (a) => a.alertType === "low_stock"
  ).length;

  const pendingApprovalsCount = pendingApprovals.filter(
    (a) => a.status === "pending"
  ).length;

  const inStockItems = inventoryItems.filter(
    (i) => i.status === "in_stock"
  ).length;

  const totalItems = inventoryItems.length;

  return (
    <div className="space-y-6">
      {/* Header with filters */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold">Operations Dashboard</h1>
          <p className="text-muted-foreground mt-1">
            Real-time ERP system overview and workflow management
          </p>
        </div>
        <div className="flex gap-2">
          <Button
            variant={timeFilter === '7d' ? 'default' : 'outline'}
            size="sm"
            onClick={() => setTimeFilter('7d')}
            className="gap-2"
          >
            <Calendar className="w-4 h-4" />
            Last 7 days
          </Button>
          <Button
            variant={timeFilter === '30d' ? 'default' : 'outline'}
            size="sm"
            onClick={() => setTimeFilter('30d')}
          >
            Last 30 days
          </Button>
        </div>
      </div>

      {/* Key Metrics */}
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
        <SummaryCard
          title="Pending Approvals"
          value={pendingApprovalsCount}
          icon={AlertTriangle}
          trend={{ direction: "up", percentage: 8 }}
          description="Require action"
        />
        <SummaryCard
          title="Critical Alerts"
          value={criticalAlerts}
          icon={AlertTriangle}
          trend={{ direction: "up", percentage: 12 }}
          description="Low stock items"
        />
        <SummaryCard
          title="Inventory Status"
          value={`${inStockItems}/${totalItems}`}
          icon={Package}
          trend={{ direction: "up", percentage: 5 }}
          description="Items in stock"
        />
        <SummaryCard
          title="Low Stock"
          value={lowStockAlerts}
          icon={TrendingUp}
          trend={{ direction: "down", percentage: 3 }}
          description="Monitoring inventory"
        />
      </div>

      {/* Main workflow section */}
      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        {/* Pending Approvals */}
        <div className="lg:col-span-2">
          <PendingApprovalsWidget approvals={pendingApprovals} />
        </div>

        {/* Quick Actions */}
        <div>
          <ERPQuickActionsWidget />
        </div>
      </div>

      {/* Inventory and Activity section */}
      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        <InventoryAlertsWidget alerts={inventoryAlerts} />
        <div className="lg:col-span-2">
          <SystemActivityWidget activities={systemActivities} />
        </div>
      </div>

      {/* Charts section */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        {/* Stock Movement Chart */}
        <Card>
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <TrendingUp className="w-5 h-5 text-primary" />
              Stock Movements
            </CardTitle>
            <CardDescription>
              {timeFilter === '7d' ? 'Last 7 days' : 'Last 30 days'} - Received vs Issued
            </CardDescription>
          </CardHeader>
          <CardContent>
            <ResponsiveContainer width="100%" height={300}>
              <LineChart data={stockMovementData}>
                <CartesianGrid strokeDasharray="3 3" />
                <XAxis dataKey="date" />
                <YAxis />
                <Tooltip 
                  contentStyle={{ 
                    backgroundColor: 'var(--card)',
                    border: '1px solid var(--border)',
                  }}
                />
                <Legend />
                <Line
                  type="monotone"
                  dataKey="received"
                  stroke="#0f3d2e"
                  strokeWidth={2}
                  name="Stock Received"
                  dot={{ fill: '#0f3d2e', r: 4 }}
                />
                <Line
                  type="monotone"
                  dataKey="issued"
                  stroke="#d97706"
                  strokeWidth={2}
                  name="Stock Issued"
                  dot={{ fill: '#d97706', r: 4 }}
                />
              </LineChart>
            </ResponsiveContainer>
          </CardContent>
        </Card>

        {/* Order Status Chart */}
        <Card>
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <Package className="w-5 h-5 text-primary" />
              Order Status Distribution
            </CardTitle>
            <CardDescription>
              Current status of all orders in system
            </CardDescription>
          </CardHeader>
          <CardContent>
            <div className="flex items-center justify-center">
              <ResponsiveContainer width="100%" height={300}>
                <PieChart>
                  <Pie
                    data={orderStatusData}
                    cx="50%"
                    cy="50%"
                    labelLine={false}
                    label={({ name, value }) => `${name}: ${value}`}
                    outerRadius={100}
                    fill="#8884d8"
                    dataKey="value"
                  >
                    <Cell fill="#0f3d2e" />
                    <Cell fill="#556b3f" />
                    <Cell fill="#d97706" />
                    <Cell fill="#059669" />
                  </Pie>
                  <Tooltip 
                    contentStyle={{ 
                      backgroundColor: 'var(--card)',
                      border: '1px solid var(--border)',
                    }}
                  />
                </PieChart>
              </ResponsiveContainer>
            </div>
          </CardContent>
        </Card>

        {/* Inventory by Category */}
        <Card>
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <Package className="w-5 h-5 text-primary" />
              Inventory by Category
            </CardTitle>
            <CardDescription>
              Distribution of items across categories
            </CardDescription>
          </CardHeader>
          <CardContent>
            <ResponsiveContainer width="100%" height={300}>
              <PieChart>
                <Pie
                  data={departmentInventoryData}
                  cx="50%"
                  cy="50%"
                  labelLine={false}
                  label={({ name, value }) => `${name}: ${value}%`}
                  outerRadius={100}
                  fill="#8884d8"
                  dataKey="value"
                >
                  {departmentInventoryData.map((entry, index) => (
                    <Cell key={`cell-${index}`} fill={entry.fill} />
                  ))}
                </Pie>
                <Tooltip 
                  contentStyle={{ 
                    backgroundColor: 'var(--card)',
                    border: '1px solid var(--border)',
                  }}
                />
              </PieChart>
            </ResponsiveContainer>
          </CardContent>
        </Card>

        {/* Top Items by Stock */}
        <Card>
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <TrendingUp className="w-5 h-5 text-primary" />
              Top Items by Stock
            </CardTitle>
            <CardDescription>
              Highest inventory levels across all items
            </CardDescription>
          </CardHeader>
          <CardContent>
            <div className="space-y-4">
              {inventoryItems
                .sort((a, b) => b.currentStock - a.currentStock)
                .slice(0, 5)
                .map((item) => (
                  <div key={item.id} className="space-y-2">
                    <div className="flex items-center justify-between">
                      <div>
                        <p className="font-medium text-sm">{item.name}</p>
                        <p className="text-xs text-muted-foreground">{item.sku}</p>
                      </div>
                      <Badge
                        variant={
                          item.status === 'in_stock'
                            ? 'default'
                            : item.status === 'critical'
                            ? 'destructive'
                            : 'secondary'
                        }
                      >
                        {item.currentStock} {item.unit}
                      </Badge>
                    </div>
                    <div className="w-full bg-gray-200 rounded-full h-2">
                      <div
                        className="h-2 rounded-full bg-primary"
                        style={{
                          width: `${Math.min((item.currentStock / (item.minimumStock * 2)) * 100, 100)}%`,
                        }}
                      ></div>
                    </div>
                  </div>
                ))}
            </div>
          </CardContent>
        </Card>
      </div>
    </div>
  );
}
