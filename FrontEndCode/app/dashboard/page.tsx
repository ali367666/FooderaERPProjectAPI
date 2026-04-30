'use client';

import { useEffect, useMemo, useState } from 'react';
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
import { useSelectedCompany } from '@/contexts/selected-company-context';
import { getCompanies } from '@/lib/services/company-service';
import { getWarehouses } from '@/lib/services/warehouse-service';
import {
  searchWarehouseStockBalancesForAllCompanies,
  type WarehouseStockBalanceRow,
} from '@/lib/services/warehouse-stock-balance-service';
import {
  getStockItemsForAllCompanies,
  type StockItem,
} from '@/lib/services/stock-item-service';
import {
  getAllStockCategoriesForAllCompanies,
  type StockCategory,
} from '@/lib/services/stock-category-service';
import {
  searchStockMovementsForAllCompanies,
  type StockMovementRow,
} from '@/lib/services/stock-movement-service';
import { formatUnit } from '@/lib/format-unit';
import {
  getStockRequests,
  approveStockRequest,
  rejectStockRequest,
  StockRequestStatus,
  type StockRequestDto,
} from '@/lib/services/stock-request-service';
import {
  getWarehouseTransfers,
  approveWarehouseTransfer,
  rejectWarehouseTransfer,
  TransferStatus,
  type WarehouseTransferDto,
} from '@/lib/services/warehouse-transfer-service';
import { getOrders, type OrderDto } from '@/lib/services/order-service';
import {
  type DashboardInventoryAlert,
  type DashboardPendingApproval,
  type DashboardRecentActivity,
} from '@/lib/dashboard-types';

const CHART_COLORS = ['#0f3d2e', '#556b3f', '#d97706', '#059669', '#84cc16', '#0891b2'];

type DashboardData = {
  pendingApprovalsCount: number;
  criticalAlertsCount: number;
  inventoryInStockCount: number;
  totalInventoryItemsCount: number;
  lowStockCount: number;
  pendingApprovals: DashboardPendingApproval[];
  inventoryAlerts: DashboardInventoryAlert[];
  recentActivities: DashboardRecentActivity[];
  stockMovementsByDay: { date: string; received: number; issued: number }[];
  orderStatusDistribution: { status: string; count: number }[];
  inventoryByCategory: { categoryName: string; quantity: number }[];
  topItemsByStock: { id: number; name: string; sku: string; quantity: number; unit: string }[];
};

const EMPTY_DASHBOARD_DATA: DashboardData = {
  pendingApprovalsCount: 0,
  criticalAlertsCount: 0,
  inventoryInStockCount: 0,
  totalInventoryItemsCount: 0,
  lowStockCount: 0,
  pendingApprovals: [],
  inventoryAlerts: [],
  recentActivities: [],
  stockMovementsByDay: [],
  orderStatusDistribution: [],
  inventoryByCategory: [],
  topItemsByStock: [],
};

function formatStatusLabel(status: string): string {
  return status
    .replace(/_/g, ' ')
    .replace(/([a-z])([A-Z])/g, '$1 $2')
    .replace(/\b\w/g, (char) => char.toUpperCase());
}

export default function DashboardPage() {
  const [timeFilter, setTimeFilter] = useState<'7d' | '30d'>('7d');
  const [dashboardData, setDashboardData] = useState<DashboardData>(EMPTY_DASHBOARD_DATA);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [reloadToken, setReloadToken] = useState(0);
  const { selectedCompanyId } = useSelectedCompany();

  const days = timeFilter === '7d' ? 7 : 30;

  useEffect(() => {
    let cancelled = false;

    async function loadDashboard() {
      try {
        setLoading(true);
        setError(null);
        const companies = await getCompanies();
        const companyIds = companies.map((company) => company.id);
        if (companyIds.length === 0) {
          setDashboardData(EMPTY_DASHBOARD_DATA);
          return;
        }

        const [
          warehouses,
          warehouseStocks,
          stockItems,
          stockCategories,
          stockMovements,
          stockRequests,
          warehouseTransfers,
          orders,
        ] = await Promise.all([
          getWarehouses(),
          searchWarehouseStockBalancesForAllCompanies(companyIds),
          getStockItemsForAllCompanies(companyIds),
          getAllStockCategoriesForAllCompanies(companyIds),
          searchStockMovementsForAllCompanies(companyIds),
          getStockRequests(),
          getWarehouseTransfers(),
          getOrders(),
        ]);

        const filtered = buildDashboardData({
          days,
          selectedCompanyId,
          warehouses,
          warehouseStocks,
          stockItems,
          stockCategories,
          stockMovements,
          stockRequests,
          warehouseTransfers,
          orders,
        });

        if (!cancelled) {
          setDashboardData(filtered);
        }
      } catch (err) {
        if (!cancelled) {
          setError(err instanceof Error ? err.message : 'Failed to fetch dashboard summary');
          setDashboardData(EMPTY_DASHBOARD_DATA);
        }
      } finally {
        if (!cancelled) {
          setLoading(false);
        }
      }
    }

    void loadDashboard();

    return () => {
      cancelled = true;
    };
  }, [days, reloadToken, selectedCompanyId]);

  const stockMovementData = useMemo(() =>
      dashboardData.stockMovementsByDay.map((item) => ({
        date: new Date(item.date).toLocaleDateString(undefined, {
          month: 'short',
          day: 'numeric',
        }),
        received: item.received,
        issued: item.issued,
      })),
    [dashboardData.stockMovementsByDay]);

  const orderStatusData = useMemo(
    () =>
      dashboardData.orderStatusDistribution.map((item) => ({
        name: formatStatusLabel(item.status),
        value: item.count,
      })),
    [dashboardData.orderStatusDistribution],
  );

  const inventoryByCategoryData = useMemo(
    () =>
      dashboardData.inventoryByCategory.map((item, index) => ({
        name: item.categoryName,
        value: item.quantity,
        fill: CHART_COLORS[index % CHART_COLORS.length],
      })),
    [dashboardData.inventoryByCategory],
  );

  const maxTopItemQuantity = useMemo(
    () => Math.max(...dashboardData.topItemsByStock.map((item) => item.quantity), 0),
    [dashboardData.topItemsByStock],
  );

  const isEmpty = useMemo(
    () =>
      dashboardData.pendingApprovalsCount === 0 &&
      dashboardData.criticalAlertsCount === 0 &&
      dashboardData.inventoryInStockCount === 0 &&
      dashboardData.totalInventoryItemsCount === 0 &&
      dashboardData.lowStockCount === 0 &&
      dashboardData.pendingApprovals.length === 0 &&
      dashboardData.inventoryAlerts.length === 0 &&
      dashboardData.recentActivities.length === 0 &&
      dashboardData.stockMovementsByDay.every((item) => item.received === 0 && item.issued === 0) &&
      dashboardData.orderStatusDistribution.length === 0 &&
      dashboardData.inventoryByCategory.length === 0 &&
      dashboardData.topItemsByStock.length === 0,
    [dashboardData],
  );

  const renderEmptyChart = (message: string) => (
    <div className="flex h-[300px] items-center justify-center text-sm text-muted-foreground">
      {message}
    </div>
  );

  return (
    <div className="space-y-6">
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

      {loading ? (
        <Card>
          <CardContent className="py-12 text-center text-muted-foreground">
            Loading dashboard data...
          </CardContent>
        </Card>
      ) : error ? (
        <Card>
          <CardHeader>
            <CardTitle>Unable to load dashboard</CardTitle>
            <CardDescription>{error}</CardDescription>
          </CardHeader>
          <CardContent>
            <Button onClick={() => setReloadToken((value) => value + 1)}>Retry</Button>
          </CardContent>
        </Card>
      ) : isEmpty ? (
        <Card>
          <CardHeader>
            <CardTitle>No dashboard data available</CardTitle>
            <CardDescription>
              {selectedCompanyId == null
                ? 'No dashboard activity was found for the selected date range.'
                : 'This company has no dashboard data for the selected date range.'}
            </CardDescription>
          </CardHeader>
        </Card>
      ) : (
        <>
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
            <SummaryCard
              title="Pending Approvals"
              value={dashboardData.pendingApprovalsCount}
              icon={AlertTriangle}
              description="Require action"
            />
            <SummaryCard
              title="Critical Alerts"
              value={dashboardData.criticalAlertsCount}
              icon={AlertTriangle}
              description="Inventory at risk"
            />
            <SummaryCard
              title="Inventory Status"
              value={`${dashboardData.inventoryInStockCount}/${dashboardData.totalInventoryItemsCount}`}
              icon={Package}
              description="Items in stock"
            />
            <SummaryCard
              title="Low Stock"
              value={dashboardData.lowStockCount}
              icon={TrendingUp}
              description="Monitoring inventory"
            />
          </div>

          <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
            <div className="lg:col-span-2">
              <PendingApprovalsWidget
                approvals={dashboardData.pendingApprovals}
                onApprove={async (approval) => {
                  if (approval.type === 'stock_request') {
                    const id = Number(approval.id.split('-').pop());
                    if (Number.isFinite(id)) await approveStockRequest(id);
                  } else if (approval.type === 'warehouse_transfer') {
                    const id = Number(approval.id.split('-').pop());
                    if (Number.isFinite(id)) await approveWarehouseTransfer(id);
                  }
                  setReloadToken((value) => value + 1);
                }}
                onReject={async (approval) => {
                  if (approval.type === 'stock_request') {
                    const id = Number(approval.id.split('-').pop());
                    if (Number.isFinite(id)) await rejectStockRequest(id);
                  } else if (approval.type === 'warehouse_transfer') {
                    const id = Number(approval.id.split('-').pop());
                    if (Number.isFinite(id)) await rejectWarehouseTransfer(id);
                  }
                  setReloadToken((value) => value + 1);
                }}
              />
            </div>

            <div>
              <ERPQuickActionsWidget />
            </div>
          </div>

          <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
            <InventoryAlertsWidget alerts={dashboardData.inventoryAlerts} />
            <div className="lg:col-span-2">
              <SystemActivityWidget activities={dashboardData.recentActivities} />
            </div>
          </div>

          <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
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
                {stockMovementData.length === 0 ? (
                  renderEmptyChart('No stock movement data for this period')
                ) : (
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
                )}
              </CardContent>
            </Card>

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
                {orderStatusData.length === 0 ? (
                  renderEmptyChart('No orders found for this period')
                ) : (
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
                          {orderStatusData.map((_, index) => (
                            <Cell key={`order-status-${index}`} fill={CHART_COLORS[index % CHART_COLORS.length]} />
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
                  </div>
                )}
              </CardContent>
            </Card>

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
                {inventoryByCategoryData.length === 0 ? (
                  renderEmptyChart('No inventory category data available')
                ) : (
                  <ResponsiveContainer width="100%" height={300}>
                    <PieChart>
                      <Pie
                        data={inventoryByCategoryData}
                        cx="50%"
                        cy="50%"
                        labelLine={false}
                        label={({ name, value }) => `${name}: ${value}`}
                        outerRadius={100}
                        fill="#8884d8"
                        dataKey="value"
                      >
                        {inventoryByCategoryData.map((entry, index) => (
                          <Cell key={`inventory-category-${entry.name}-${index}`} fill={entry.fill} />
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
                )}
              </CardContent>
            </Card>

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
                {dashboardData.topItemsByStock.length === 0 ? (
                  <div className="py-8 text-center text-muted-foreground">
                    No inventory items available
                  </div>
                ) : (
                  <div className="space-y-4">
                    {dashboardData.topItemsByStock.map((item) => (
                      <div key={item.id} className="space-y-2">
                        <div className="flex items-center justify-between">
                          <div>
                            <p className="font-medium text-sm">{item.name}</p>
                            <p className="text-xs text-muted-foreground">{item.sku}</p>
                          </div>
                          <Badge variant="secondary">
                            {item.quantity} {item.unit}
                          </Badge>
                        </div>
                        <div className="w-full bg-gray-200 rounded-full h-2">
                          <div
                            className="h-2 rounded-full bg-primary"
                            style={{
                              width: `${maxTopItemQuantity > 0 ? (item.quantity / maxTopItemQuantity) * 100 : 0}%`,
                            }}
                          ></div>
                        </div>
                      </div>
                    ))}
                  </div>
                )}
              </CardContent>
            </Card>
          </div>
        </>
      )}
    </div>
  );
}

function buildDashboardData(input: {
  days: number;
  selectedCompanyId: number | null;
  warehouses: Awaited<ReturnType<typeof getWarehouses>>;
  warehouseStocks: WarehouseStockBalanceRow[];
  stockItems: StockItem[];
  stockCategories: StockCategory[];
  stockMovements: StockMovementRow[];
  stockRequests: StockRequestDto[];
  warehouseTransfers: WarehouseTransferDto[];
  orders: OrderDto[];
}): DashboardData {
  const {
    days,
    selectedCompanyId,
    warehouses,
    warehouseStocks,
    stockItems,
    stockCategories,
    stockMovements,
    stockRequests,
    warehouseTransfers,
    orders,
  } = input;

  const warehouseById = new Map(warehouses.map((warehouse) => [warehouse.id, warehouse]));
  const stockItemById = new Map(stockItems.map((stockItem) => [stockItem.id, stockItem]));
  const categoryById = new Map(stockCategories.map((category) => [category.id, category]));
  const selectedWarehouseIds = new Set(
    selectedCompanyId == null
      ? warehouses.map((warehouse) => warehouse.id)
      : warehouses.filter((warehouse) => warehouse.companyId === selectedCompanyId).map((warehouse) => warehouse.id),
  );

  const isCompanyVisible = (companyId: number | null | undefined): boolean =>
    selectedCompanyId == null || (companyId ?? 0) === selectedCompanyId;

  const visibleWarehouseStocks = warehouseStocks.filter(
    (stock) =>
      selectedWarehouseIds.has(stock.warehouseId) ||
      isCompanyVisible(stock.companyId),
  );

  const visibleStockItems = stockItems.filter((item) => isCompanyVisible(item.companyId));
  const visibleStockRequests = stockRequests.filter((request) => {
    if (isCompanyVisible(request.companyId)) return true;
    return selectedWarehouseIds.has(request.requestingWarehouseId) || selectedWarehouseIds.has(request.supplyingWarehouseId);
  });
  const visibleWarehouseTransfers = warehouseTransfers.filter((transfer) => {
    if (isCompanyVisible(transfer.companyId)) return true;
    return selectedWarehouseIds.has(transfer.fromWarehouseId) || selectedWarehouseIds.has(transfer.toWarehouseId);
  });
  const visibleOrders = orders.filter((order) => isCompanyVisible(order.companyId));

  const now = new Date();
  const rangeStart = new Date(now);
  rangeStart.setHours(0, 0, 0, 0);
  rangeStart.setDate(rangeStart.getDate() - (days - 1));

  const visibleStockMovements = stockMovements.filter((movement) => {
    if (!isCompanyVisible(movement.companyId)) return false;
    const movementDate = new Date(movement.movementDate);
    return movementDate >= rangeStart;
  });

  const getThreshold = (stock: WarehouseStockBalanceRow): number => {
    const threshold = stock.minimumQuantity ?? stock.reorderLevel ?? 0;
    return Number.isFinite(threshold) ? threshold : 0;
  };

  const inventoryAlerts: DashboardInventoryAlert[] = visibleWarehouseStocks
    .filter((stock) => stock.quantity <= getThreshold(stock))
    .map((stock) => ({
      id: stock.id,
      itemName: stock.stockItemName,
      sku: stockItemById.get(stock.stockItemId)?.barcode ?? `ITEM-${stock.stockItemId}`,
      currentStock: stock.quantity,
      minimumStock: getThreshold(stock),
      alertType: stock.quantity <= 0 ? 'critical' : 'low_stock',
      unit: stock.unitLabel,
    }))
    .sort((a, b) => a.currentStock - b.currentStock);

  const pendingApprovals: DashboardPendingApproval[] = [
    ...visibleStockRequests
      .filter((request) => request.status === StockRequestStatus.Submitted)
      .map((request) => ({
        id: `stock-request-${request.id}`,
        type: 'stock_request' as const,
        title: `Stock Request #${request.id}`,
        createdBy: request.requestingWarehouseName || 'Warehouse',
        status: 'pending' as const,
        createdAt: request.createdAtUtc ?? new Date().toISOString(),
        items: request.lines.slice(0, 3).map((line) => line.stockItemName).join(', ') || null,
        quantity: request.lines.reduce((sum, line) => sum + line.quantity, 0),
        actionable: true,
      })),
    ...visibleWarehouseTransfers
      .filter((transfer) => transfer.status === TransferStatus.Pending)
      .map((transfer) => ({
        id: `warehouse-transfer-${transfer.id}`,
        type: 'warehouse_transfer' as const,
        title: `Warehouse Transfer #${transfer.id}`,
        createdBy: transfer.fromWarehouseName || 'Warehouse',
        status: 'pending' as const,
        createdAt: transfer.transferDate,
        items: transfer.lines.slice(0, 3).map((line) => line.stockItemName).join(', ') || null,
        quantity: transfer.lines.reduce((sum, line) => sum + line.quantity, 0),
        actionable: true,
      })),
    ...visibleOrders
      .filter((order) => order.status === 'draft')
      .map((order) => ({
        id: `order-${order.id}`,
        type: 'order' as const,
        title: `Order ${order.orderNumber}`,
        createdBy: order.waiterName ?? 'Order workflow',
        status: 'pending' as const,
        createdAt: order.openedAt,
        items: null,
        quantity: null,
        actionable: false,
      })),
  ]
    .sort((a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime())
    .slice(0, 10);

  const recentActivities: DashboardRecentActivity[] = [
    ...visibleStockMovements.map((movement) => ({
      id: `stock-movement-${movement.id}`,
      type: 'stock_movement' as const,
      description: movement.movementType,
      details: (() => {
        const u = formatUnit(movement.stockItemUnit ?? undefined);
        const qty = u ? `${movement.quantity} ${u}` : String(movement.quantity);
        return `${movement.stockItemName} - ${qty}`;
      })(),
      user: movement.warehouseName || movement.fromWarehouseName || movement.toWarehouseName || 'System',
      timestamp: movement.movementDate,
      status: 'completed' as const,
    })),
    ...visibleOrders.map((order) => ({
      id: `order-${order.id}`,
      type: 'order_update' as const,
      description: `Order ${order.orderNumber} ${formatStatusLabel(order.status)}`,
      details: order.restaurantName ?? 'Restaurant order',
      user: order.waiterName ?? 'System',
      timestamp: order.openedAt,
      status: order.status === 'paid' || order.status === 'served' ? 'completed' : 'in_progress' as const,
    })),
    ...visibleStockRequests.map((request) => ({
      id: `stock-request-${request.id}`,
      type: 'approval' as const,
      description: `Stock Request #${request.id} ${formatStatusLabel(String(request.status))}`,
      details: `${request.requestingWarehouseName} -> ${request.supplyingWarehouseName}`,
      user: request.requestingWarehouseName || 'System',
      timestamp: request.createdAtUtc ?? new Date().toISOString(),
      status: request.status === StockRequestStatus.Approved ? 'completed' : 'pending' as const,
    })),
    ...visibleWarehouseTransfers.map((transfer) => ({
      id: `transfer-${transfer.id}`,
      type: 'transfer' as const,
      description: `Transfer #${transfer.id} ${formatStatusLabel(String(transfer.status))}`,
      details: `${transfer.fromWarehouseName} -> ${transfer.toWarehouseName}`,
      user: transfer.fromWarehouseName || 'System',
      timestamp: transfer.transferDate,
      status:
        transfer.status === TransferStatus.Completed
          ? 'completed'
          : transfer.status === TransferStatus.Pending
            ? 'pending'
            : 'in_progress' as const,
    })),
  ]
    .sort((a, b) => new Date(b.timestamp).getTime() - new Date(a.timestamp).getTime())
    .slice(0, 10);

  const stockMovementsByDay = Array.from({ length: days }, (_, index) => {
    const date = new Date(rangeStart);
    date.setDate(rangeStart.getDate() + index);
    const dateKey = date.toISOString().split('T')[0];
    const dayRows = visibleStockMovements.filter(
      (movement) => movement.movementDate.split('T')[0] === dateKey,
    );
    return {
      date: date.toISOString(),
      received: dayRows
        .filter((movement) =>
          movement.movementType.toLowerCase().includes('in') ||
          movement.movementType.toLowerCase().includes('received'),
        )
        .reduce((sum, movement) => sum + movement.quantity, 0),
      issued: dayRows
        .filter((movement) =>
          movement.movementType.toLowerCase().includes('out') ||
          movement.movementType.toLowerCase().includes('issued'),
        )
        .reduce((sum, movement) => sum + movement.quantity, 0),
    };
  });

  const orderStatusDistribution = Object.entries(
    visibleOrders.reduce<Record<string, number>>((acc, order) => {
      acc[order.status] = (acc[order.status] ?? 0) + 1;
      return acc;
    }, {}),
  ).map(([status, count]) => ({ status, count }));

  const inventoryByCategory = Object.entries(
    visibleWarehouseStocks.reduce<Record<string, number>>((acc, stock) => {
      const stockItem = stockItemById.get(stock.stockItemId);
      const categoryName = stockItem
        ? categoryById.get(stockItem.categoryId)?.name ?? stockItem.categoryName ?? 'Uncategorized'
        : 'Uncategorized';
      acc[categoryName] = (acc[categoryName] ?? 0) + stock.quantity;
      return acc;
    }, {}),
  )
    .map(([categoryName, quantity]) => ({ categoryName, quantity }))
    .sort((a, b) => b.quantity - a.quantity);

  const topItemsByStock = Object.values(
    visibleWarehouseStocks.reduce<Record<number, { id: number; name: string; sku: string; quantity: number; unit: string }>>(
      (acc, stock) => {
        const stockItem = stockItemById.get(stock.stockItemId);
        const existing = acc[stock.stockItemId] ?? {
          id: stock.stockItemId,
          name: stock.stockItemName,
          sku: stockItem?.barcode ?? `ITEM-${stock.stockItemId}`,
          quantity: 0,
          unit: stock.unitLabel,
        };
        existing.quantity += stock.quantity;
        acc[stock.stockItemId] = existing;
        return acc;
      },
      {},
    ),
  )
    .sort((a, b) => b.quantity - a.quantity)
    .slice(0, 5);

  return {
    pendingApprovalsCount: pendingApprovals.length,
    criticalAlertsCount: inventoryAlerts.filter((alert) => alert.alertType === 'critical').length,
    inventoryInStockCount: visibleWarehouseStocks.filter((stock) => stock.quantity > 0).length,
    totalInventoryItemsCount: visibleStockItems.length,
    lowStockCount: inventoryAlerts.length,
    pendingApprovals,
    inventoryAlerts,
    recentActivities,
    stockMovementsByDay,
    orderStatusDistribution,
    inventoryByCategory,
    topItemsByStock,
  };
}
