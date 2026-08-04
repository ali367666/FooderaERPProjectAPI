"use client";

import { useCallback, useEffect, useState } from "react";
import {
  BarChart, Bar, LineChart, Line, XAxis, YAxis, CartesianGrid,
  Tooltip, ResponsiveContainer, PieChart, Pie, Cell, Legend,
} from "recharts";
import { getDashboardAnalytics, type DashboardAnalytics } from "@/lib/services/analytics-service";
import { toApiFormError } from "@/lib/api-error";
import {
  TrendingUp, ShoppingBag, Users, LayoutGrid,
  RefreshCw, Award, Clock,
} from "lucide-react";
import { Button } from "@/components/ui/button";
import { cn } from "@/lib/utils";

function fmt(n: number) {
  return new Intl.NumberFormat("az-AZ", {
    minimumFractionDigits: 2,
    maximumFractionDigits: 2,
  }).format(n) + " ₼";
}

function fmtShort(n: number) {
  if (n >= 1000) return (n / 1000).toFixed(1) + "K ₼";
  return n.toFixed(0) + " ₼";
}

const CHART_COLORS = ["#6366f1", "#22c55e", "#f59e0b", "#ef4444", "#a855f7", "#0ea5e9", "#14b8a6"];

// ── Stat card ───────────────────────────────────────────────────────────────

function StatCard({
  label, value, sub, icon: Icon, color,
}: {
  label: string;
  value: string;
  sub?: string;
  icon: React.ElementType;
  color: string;
}) {
  return (
    <div className="rounded-xl border border-border bg-card p-5 flex items-start gap-4">
      <div className={cn("rounded-lg p-2.5", color)}>
        <Icon className="h-5 w-5" />
      </div>
      <div className="min-w-0">
        <p className="text-sm text-muted-foreground">{label}</p>
        <p className="text-2xl font-semibold tracking-tight mt-0.5">{value}</p>
        {sub && <p className="text-xs text-muted-foreground mt-0.5">{sub}</p>}
      </div>
    </div>
  );
}

// ── Section wrapper ──────────────────────────────────────────────────────────

function Section({ title, children, className }: { title: string; children: React.ReactNode; className?: string }) {
  return (
    <div className={cn("rounded-xl border border-border bg-card p-5", className)}>
      <h3 className="text-sm font-semibold text-muted-foreground uppercase tracking-wider mb-4">{title}</h3>
      {children}
    </div>
  );
}

// ── Custom tooltip ───────────────────────────────────────────────────────────

function ChartTooltip({ active, payload, label }: any) {
  if (!active || !payload?.length) return null;
  return (
    <div className="rounded-lg border border-border bg-card shadow-md px-3 py-2 text-xs">
      <p className="font-medium mb-1">{label}</p>
      {payload.map((p: any, i: number) => (
        <p key={i} style={{ color: p.color }}>
          {p.name}: {typeof p.value === "number" && p.name?.includes("Sifariş")
            ? p.value
            : fmtShort(p.value)}
        </p>
      ))}
    </div>
  );
}

// ── Main ─────────────────────────────────────────────────────────────────────

export function RestaurantDashboard() {
  const [data, setData] = useState<DashboardAnalytics | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [lastUpdated, setLastUpdated] = useState<Date | null>(null);

  const load = useCallback(async () => {
    try {
      setLoading(true);
      setError(null);
      const d = await getDashboardAnalytics();
      setData(d);
      setLastUpdated(new Date());
    } catch (e) {
      setError(toApiFormError(e, "Analitika yüklənmədi").message);
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    void load();
    const iv = setInterval(() => void load(), 60_000);
    return () => clearInterval(iv);
  }, [load]);

  if (loading && !data) {
    return (
      <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
        {Array.from({ length: 8 }).map((_, i) => (
          <div key={i} className="rounded-xl border border-border bg-muted animate-pulse h-28" />
        ))}
      </div>
    );
  }

  if (error) {
    return (
      <div className="rounded-xl border border-destructive/40 bg-destructive/10 px-5 py-4 text-sm text-destructive">
        {error}
        <Button size="sm" variant="outline" onClick={() => void load()} className="ml-3">
          Yenidən cəhd et
        </Button>
      </div>
    );
  }

  if (!data) return null;

  const occupancyPct = data.totalActiveTables > 0
    ? Math.round((data.currentlyOccupiedTables / data.totalActiveTables) * 100)
    : 0;

  const occupancyData = [
    { name: "Dolu", value: data.currentlyOccupiedTables },
    { name: "Boş", value: Math.max(0, data.totalActiveTables - data.currentlyOccupiedTables) },
  ];

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-semibold tracking-tight">Analitika</h1>
          {lastUpdated && (
            <p className="text-xs text-muted-foreground mt-0.5">
              Son yenilənmə: {lastUpdated.toLocaleTimeString("az-AZ")}
            </p>
          )}
        </div>
        <Button variant="outline" size="sm" onClick={() => void load()} disabled={loading} className="gap-1.5">
          <RefreshCw className={cn("h-4 w-4", loading && "animate-spin")} />
          Yenilə
        </Button>
      </div>

      {/* KPI cards */}
      <div className="grid grid-cols-2 lg:grid-cols-4 gap-4">
        <StatCard
          label="Bu günün gəliri"
          value={fmt(data.todayRevenue)}
          sub={`${data.todayOrderCount} sifariş`}
          icon={TrendingUp}
          color="bg-indigo-100 text-indigo-600 dark:bg-indigo-950 dark:text-indigo-400"
        />
        <StatCard
          label="Bu həftə"
          value={fmt(data.weekRevenue)}
          icon={TrendingUp}
          color="bg-blue-100 text-blue-600 dark:bg-blue-950 dark:text-blue-400"
        />
        <StatCard
          label="Bu ay"
          value={fmt(data.monthRevenue)}
          sub={`${data.monthOrderCount} sifariş`}
          icon={ShoppingBag}
          color="bg-emerald-100 text-emerald-600 dark:bg-emerald-950 dark:text-emerald-400"
        />
        <StatCard
          label="Bu il"
          value={fmt(data.yearRevenue)}
          icon={Award}
          color="bg-amber-100 text-amber-600 dark:bg-amber-950 dark:text-amber-400"
        />
        <StatCard
          label="Orta sifariş"
          value={fmt(data.averageOrderValue)}
          sub="bu ay"
          icon={ShoppingBag}
          color="bg-purple-100 text-purple-600 dark:bg-purple-950 dark:text-purple-400"
        />
        <StatCard
          label="Masa dolulugu"
          value={`${occupancyPct}%`}
          sub={`${data.currentlyOccupiedTables} / ${data.totalActiveTables} masa`}
          icon={LayoutGrid}
          color="bg-rose-100 text-rose-600 dark:bg-rose-950 dark:text-rose-400"
        />
        <StatCard
          label="Aktiv sifarişlər"
          value={String(data.currentlyOccupiedTables)}
          sub="hazırda açıq"
          icon={Clock}
          color="bg-orange-100 text-orange-600 dark:bg-orange-950 dark:text-orange-400"
        />
        <StatCard
          label="Ofisiantlar"
          value={String(data.waiterPerformance.length)}
          sub="bu ay aktiv"
          icon={Users}
          color="bg-teal-100 text-teal-600 dark:bg-teal-950 dark:text-teal-400"
        />
      </div>

      {/* Charts row 1 */}
      <div className="grid grid-cols-1 lg:grid-cols-3 gap-4">
        {/* Daily revenue — last 7 days */}
        <Section title="Son 7 günün gəliri" className="lg:col-span-2">
          <ResponsiveContainer width="100%" height={200}>
            <BarChart data={data.dailyRevenue} margin={{ top: 0, right: 0, left: -20, bottom: 0 }}>
              <CartesianGrid strokeDasharray="3 3" stroke="var(--color-border-tertiary)" />
              <XAxis dataKey="date" tick={{ fontSize: 11 }} />
              <YAxis tickFormatter={fmtShort} tick={{ fontSize: 11 }} />
              <Tooltip content={<ChartTooltip />} />
              <Bar dataKey="revenue" name="Gəlir" fill="#6366f1" radius={[4, 4, 0, 0]} />
            </BarChart>
          </ResponsiveContainer>
        </Section>

        {/* Occupancy donut */}
        <Section title="Masa dolulugu">
          <div className="flex flex-col items-center gap-2">
            <div className="relative">
              <ResponsiveContainer width={160} height={160}>
                <PieChart>
                  <Pie
                    data={occupancyData}
                    cx="50%"
                    cy="50%"
                    innerRadius={50}
                    outerRadius={72}
                    dataKey="value"
                    startAngle={90}
                    endAngle={-270}
                  >
                    <Cell fill="#6366f1" />
                    <Cell fill="var(--color-border-tertiary)" />
                  </Pie>
                </PieChart>
              </ResponsiveContainer>
              <div className="absolute inset-0 flex flex-col items-center justify-center pointer-events-none">
                <span className="text-2xl font-bold">{occupancyPct}%</span>
                <span className="text-[10px] text-muted-foreground">dolu</span>
              </div>
            </div>
            <div className="flex gap-4 text-xs">
              <span className="flex items-center gap-1.5">
                <span className="w-2.5 h-2.5 rounded-full bg-indigo-500" />
                Dolu ({data.currentlyOccupiedTables})
              </span>
              <span className="flex items-center gap-1.5">
                <span className="w-2.5 h-2.5 rounded-full bg-border" />
                Boş ({Math.max(0, data.totalActiveTables - data.currentlyOccupiedTables)})
              </span>
            </div>
          </div>
        </Section>
      </div>

      {/* Charts row 2 */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-4">
        {/* Hourly sales */}
        <Section title="Bu gün saatlıq satış">
          <ResponsiveContainer width="100%" height={180}>
            <LineChart data={data.hourlySales} margin={{ top: 0, right: 0, left: -20, bottom: 0 }}>
              <CartesianGrid strokeDasharray="3 3" stroke="var(--color-border-tertiary)" />
              <XAxis dataKey="label" tick={{ fontSize: 10 }} interval={2} />
              <YAxis tickFormatter={fmtShort} tick={{ fontSize: 11 }} />
              <Tooltip content={<ChartTooltip />} />
              <Line
                type="monotone"
                dataKey="revenue"
                name="Gəlir"
                stroke="#6366f1"
                strokeWidth={2}
                dot={false}
                activeDot={{ r: 4 }}
              />
            </LineChart>
          </ResponsiveContainer>
        </Section>

        {/* Top menu items */}
        <Section title="Top məhsullar (bu ay)">
          {data.topMenuItems.length === 0 ? (
            <p className="text-sm text-muted-foreground text-center py-8">Məlumat yoxdur</p>
          ) : (
            <div className="space-y-2">
              {data.topMenuItems.slice(0, 6).map((item, i) => {
                const maxQty = data.topMenuItems[0].totalQuantity;
                const pct = maxQty > 0 ? (item.totalQuantity / maxQty) * 100 : 0;
                return (
                  <div key={item.menuItemId} className="flex items-center gap-3">
                    <span className="text-xs text-muted-foreground w-4 text-right">{i + 1}</span>
                    <div className="flex-1 min-w-0">
                      <div className="flex items-center justify-between mb-0.5">
                        <span className="text-xs font-medium truncate">{item.name}</span>
                        <span className="text-xs text-muted-foreground ml-2 shrink-0">
                          {item.totalQuantity} ədəd
                        </span>
                      </div>
                      <div className="h-1.5 rounded-full bg-muted overflow-hidden">
                        <div
                          className="h-full rounded-full bg-indigo-500 transition-all"
                          style={{ width: `${pct}%`, backgroundColor: CHART_COLORS[i % CHART_COLORS.length] }}
                        />
                      </div>
                    </div>
                    <span className="text-xs text-muted-foreground w-16 text-right shrink-0">
                      {fmtShort(item.totalRevenue)}
                    </span>
                  </div>
                );
              })}
            </div>
          )}
        </Section>
      </div>

      {/* Row 3 — Waiter performance + Restaurant breakdown */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-4">
        {/* Waiter performance */}
        <Section title="Ofisiant performansı (bu ay)">
          {data.waiterPerformance.length === 0 ? (
            <p className="text-sm text-muted-foreground text-center py-8">Məlumat yoxdur</p>
          ) : (
            <div className="space-y-3">
              {data.waiterPerformance.slice(0, 5).map((w, i) => (
                <div key={w.waiterId} className="flex items-center gap-3">
                  <div className={cn(
                    "w-7 h-7 rounded-full flex items-center justify-center text-xs font-bold shrink-0",
                    i === 0 ? "bg-amber-100 text-amber-700" :
                    i === 1 ? "bg-slate-100 text-slate-600" :
                    i === 2 ? "bg-orange-100 text-orange-700" :
                    "bg-muted text-muted-foreground"
                  )}>
                    {i + 1}
                  </div>
                  <div className="flex-1 min-w-0">
                    <p className="text-sm font-medium truncate">{w.waiterName}</p>
                    <p className="text-xs text-muted-foreground">{w.orderCount} sifariş · orta {fmt(w.averageOrderValue)}</p>
                  </div>
                  <span className="text-sm font-semibold text-indigo-600 dark:text-indigo-400 shrink-0">
                    {fmtShort(w.totalRevenue)}
                  </span>
                </div>
              ))}
            </div>
          )}
        </Section>

        {/* Restaurant revenue breakdown */}
        {data.restaurantRevenue.length > 1 && (
          <Section title="Restoran üzrə gəlir (bu ay)">
            <ResponsiveContainer width="100%" height={180}>
              <BarChart data={data.restaurantRevenue} layout="vertical" margin={{ top: 0, right: 40, left: 0, bottom: 0 }}>
                <CartesianGrid strokeDasharray="3 3" stroke="var(--color-border-tertiary)" horizontal={false} />
                <XAxis type="number" tickFormatter={fmtShort} tick={{ fontSize: 10 }} />
                <YAxis type="category" dataKey="restaurantName" tick={{ fontSize: 11 }} width={90} />
                <Tooltip content={<ChartTooltip />} />
                <Bar dataKey="revenue" name="Gəlir" radius={[0, 4, 4, 0]}>
                  {data.restaurantRevenue.map((_, i) => (
                    <Cell key={i} fill={CHART_COLORS[i % CHART_COLORS.length]} />
                  ))}
                </Bar>
              </BarChart>
            </ResponsiveContainer>
          </Section>
        )}

        {data.restaurantRevenue.length <= 1 && (
          <Section title="Ən çox sifarişli saatlar (bu ay)">
            <ResponsiveContainer width="100%" height={180}>
              <BarChart
                data={data.hourlySales.filter(h => h.orderCount > 0)}
                margin={{ top: 0, right: 0, left: -20, bottom: 0 }}
              >
                <CartesianGrid strokeDasharray="3 3" stroke="var(--color-border-tertiary)" />
                <XAxis dataKey="label" tick={{ fontSize: 10 }} interval={1} />
                <YAxis tick={{ fontSize: 11 }} />
                <Tooltip content={<ChartTooltip />} />
                <Bar dataKey="orderCount" name="Sifariş sayı" fill="#22c55e" radius={[4, 4, 0, 0]} />
              </BarChart>
            </ResponsiveContainer>
          </Section>
        )}
      </div>
    </div>
  );
}
