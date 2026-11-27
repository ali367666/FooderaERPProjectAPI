"use client";

import { useCallback, useEffect, useMemo, useState } from "react";
import { ChevronDown, ChevronRight } from "lucide-react";
import { toast } from "sonner";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Dialog, DialogContent, DialogFooter, DialogHeader, DialogTitle } from "@/components/ui/dialog";
import { cn } from "@/lib/utils";
import { downloadCsv } from "@/lib/store/csv";
import {
  getStoreRegisters,
  getStoreSalesMatrix,
  getStoreSuppliers,
  searchStoreCustomers,
  searchStoreProducts,
  type StoreSalesMatrixField,
  type StoreSalesMatrixGroup,
  type StoreSalesMatrixNode,
  type StoreSalesMatrixReport,
} from "@/lib/store/store-service";

const FIELDS: { key: StoreSalesMatrixField; label: string; decimals: number }[] = [
  { key: "quantity", label: "Miqdar", decimals: 3 },
  { key: "turnover", label: "Dövriyyə", decimals: 2 },
  { key: "grossTurnover", label: "Endirimsiz dövriyyə", decimals: 2 },
  { key: "cost", label: "Maya dəyəri", decimals: 2 },
  { key: "profit", label: "Mənfəət", decimals: 2 },
  { key: "profitPercent", label: "Mənfəət(%)", decimals: 2 },
];

const GROUPS: { key: StoreSalesMatrixGroup; label: string }[] = [
  { key: "day", label: "Gün" },
  { key: "customer", label: "Müştəri" },
  { key: "register", label: "Kassa" },
  { key: "supplier", label: "Tədarükçü" },
  { key: "product", label: "Mal" },
  { key: "document", label: "Hərəkət sənədi" },
];

type ConditionKey = "customer" | "register" | "supplier" | "product";
type Picked = { id: number; name: string };

const CONDITIONS: { key: ConditionKey; label: string }[] = [
  { key: "customer", label: "Müştəri" },
  { key: "register", label: "Kassa" },
  { key: "supplier", label: "Tədarükçü" },
  { key: "product", label: "Mal" },
];

type Draft = {
  fields: StoreSalesMatrixField[];
  /** All groupings in their order; `on` marks the ones used. */
  groups: { key: StoreSalesMatrixGroup; on: boolean }[];
  conditions: Record<ConditionKey, Picked[]>;
  /** A ticked condition with no items picked means "all"; only picked items narrow the report. */
  enabled: Record<ConditionKey, boolean>;
  sortBy: StoreSalesMatrixField | "";
  sortDesc: boolean;
};

const initialDraft = (): Draft => ({
  fields: FIELDS.map((f) => f.key),
  groups: GROUPS.map((g) => ({ key: g.key, on: g.key === "product" })),
  conditions: { customer: [], register: [], supplier: [], product: [] },
  enabled: { customer: false, register: false, supplier: false, product: false },
  sortBy: "",
  sortDesc: true,
});

const fmt = (v: number, decimals: number) =>
  v.toLocaleString("az-AZ", { minimumFractionDigits: decimals, maximumFractionDigits: decimals });

const groupLabel = (key: StoreSalesMatrixGroup | null) => GROUPS.find((g) => g.key === key)?.label ?? "";

/** Loads the choices a condition's list offers (everything the user may pick from). */
async function loadOptions(kind: ConditionKey, q: string): Promise<Picked[]> {
  if (kind === "customer") return (await searchStoreCustomers(q)).map((c) => ({ id: c.id, name: c.name }));
  if (kind === "register") {
    const all = (await getStoreRegisters()).map((r) => ({ id: r.id, name: r.name }));
    return q ? all.filter((r) => r.name.toLowerCase().includes(q.toLowerCase())) : all;
  }
  if (kind === "supplier") {
    const all = (await getStoreSuppliers()).map((s) => ({ id: s.id, name: s.name }));
    return q ? all.filter((s) => s.name.toLowerCase().includes(q.toLowerCase())) : all;
  }
  return (await searchStoreProducts({ q: q || undefined, pageSize: 50 })).items.map((p) => ({ id: p.id, name: p.name }));
}

function ConditionPicker({
  kind,
  title,
  selected,
  onClose,
  onSave,
}: {
  kind: ConditionKey;
  title: string;
  selected: Picked[];
  onClose: () => void;
  onSave: (items: Picked[]) => void;
}) {
  const [q, setQ] = useState("");
  const [options, setOptions] = useState<Picked[]>([]);
  const [loading, setLoading] = useState(false);
  const [chosen, setChosen] = useState<Map<number, Picked>>(() => new Map(selected.map((s) => [s.id, s])));

  useEffect(() => {
    let cancelled = false;
    setLoading(true);
    const timer = setTimeout(() => {
      loadOptions(kind, q.trim())
        .then((items) => !cancelled && setOptions(items))
        .catch(() => !cancelled && setOptions([]))
        .finally(() => !cancelled && setLoading(false));
    }, 250);
    return () => {
      cancelled = true;
      clearTimeout(timer);
    };
  }, [kind, q]);

  const toggle = (item: Picked) =>
    setChosen((prev) => {
      const next = new Map(prev);
      if (next.has(item.id)) next.delete(item.id);
      else next.set(item.id, item);
      return next;
    });

  return (
    <Dialog open onOpenChange={(o) => !o && onClose()}>
      <DialogContent className="sm:max-w-md">
        <DialogHeader>
          <DialogTitle>Siyahı: {title}</DialogTitle>
        </DialogHeader>
        <Input placeholder="Axtar…" value={q} onChange={(e) => setQ(e.target.value)} autoFocus />
        <div className="max-h-72 overflow-y-auto rounded-md border">
          {loading && <p className="p-3 text-sm text-muted-foreground">Yüklənir…</p>}
          {!loading && options.length === 0 && <p className="p-3 text-sm text-muted-foreground">Heç nə tapılmadı.</p>}
          {options.map((o) => (
            <label key={o.id} className="flex cursor-pointer items-center gap-2 border-b px-3 py-2 text-sm last:border-b-0 hover:bg-muted">
              <input type="checkbox" checked={chosen.has(o.id)} onChange={() => toggle(o)} />
              {o.name}
            </label>
          ))}
        </div>
        <p className="text-xs text-muted-foreground">Seçilmiş: {chosen.size}</p>
        <DialogFooter>
          <Button variant="outline" onClick={() => setChosen(new Map())}>
            Təmizlə
          </Button>
          <Button variant="outline" onClick={onClose}>
            Bağla
          </Button>
          <Button onClick={() => onSave([...chosen.values()])}>OK</Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}

type FlatRow = { node: StoreSalesMatrixNode; path: string; depth: number };

function flatten(nodes: StoreSalesMatrixNode[], collapsed: Set<string>, parent = "", depth = 0): FlatRow[] {
  return nodes.flatMap((node) => {
    const path = `${parent}/${node.key}`;
    const self: FlatRow = { node, path, depth };
    return collapsed.has(path) ? [self] : [self, ...flatten(node.children, collapsed, path, depth + 1)];
  });
}

/** The 1C-style "Satış hesabatı": groupings, columns and list conditions are chosen in the parameters dialog. */
export function SalesMatrixReport({ from, to }: { from: string; to: string }) {
  const [applied, setApplied] = useState<Draft>(initialDraft);
  const [draft, setDraft] = useState<Draft>(initialDraft);
  const [paramsOpen, setParamsOpen] = useState(false);
  const [paramTab, setParamTab] = useState<"fields" | "conditions" | "sort">("fields");
  const [picker, setPicker] = useState<ConditionKey | null>(null);
  const [report, setReport] = useState<StoreSalesMatrixReport | null>(null);
  const [loading, setLoading] = useState(false);
  const [collapsed, setCollapsed] = useState<Set<string>>(new Set());

  const run = useCallback(
    async (d: Draft) => {
      setLoading(true);
      try {
        setReport(
          await getStoreSalesMatrix({
            from,
            to,
            groupBy: d.groups.filter((g) => g.on).map((g) => g.key),
            customerIds: d.conditions.customer.map((c) => c.id),
            registerIds: d.conditions.register.map((c) => c.id),
            supplierIds: d.conditions.supplier.map((c) => c.id),
            productIds: d.conditions.product.map((c) => c.id),
            sortBy: d.sortBy || null,
            sortDesc: d.sortDesc,
          }),
        );
        setCollapsed(new Set());
      } catch (e) {
        toast.error(e instanceof Error ? e.message : "Hesabat yüklənmədi");
      } finally {
        setLoading(false);
      }
    },
    [from, to],
  );

  useEffect(() => {
    void run(applied);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  const fields = FIELDS.filter((f) => applied.fields.includes(f.key));
  const rows = useMemo(() => (report ? flatten(report.total.children, collapsed) : []), [report, collapsed]);
  const groupTitle = report?.groupBy.map(groupLabel).join(" / ") || "";

  const toggleCollapsed = (path: string) =>
    setCollapsed((prev) => {
      const next = new Set(prev);
      if (next.has(path)) next.delete(path);
      else next.add(path);
      return next;
    });

  const moveGroup = (index: number, delta: number) =>
    setDraft((d) => {
      const target = index + delta;
      if (target < 0 || target >= d.groups.length) return d;
      const groups = [...d.groups];
      [groups[index], groups[target]] = [groups[target], groups[index]];
      return { ...d, groups };
    });

  const apply = () => {
    if (draft.fields.length === 0) {
      toast.error("Ən azı bir sahə seçin.");
      return;
    }
    setApplied(draft);
    setParamsOpen(false);
    void run(draft);
  };

  const exportCsv = () => {
    if (!report) return;
    const header = [groupTitle || "Ad", ...fields.map((f) => f.label)];
    const line = (n: StoreSalesMatrixNode, depth: number) => [
      `${"  ".repeat(depth)}${n.name}`,
      ...fields.map((f) => n[f.key]),
    ];
    const walk = (n: StoreSalesMatrixNode, depth: number): (string | number)[][] => [line(n, depth), ...n.children.flatMap((c) => walk(c, depth + 1))];
    downloadCsv(`satis-hesabati-${from}-${to}.csv`, [header, line(report.total, 0), ...report.total.children.flatMap((c) => walk(c, 1))]);
  };

  return (
    <div className="space-y-3">
      <div className="flex flex-wrap items-center gap-2">
        <Button size="sm" onClick={() => void run(applied)} disabled={loading}>
          Tərtib et
        </Button>
        <Button
          size="sm"
          variant="outline"
          onClick={() => {
            setDraft(applied);
            setParamsOpen(true);
          }}
        >
          Parametrlər
        </Button>
        <Button size="sm" variant="outline" onClick={exportCsv} disabled={!report}>
          Excel/CSV ixrac
        </Button>
        <span className="text-sm text-muted-foreground">
          {from} – {to}
          {groupTitle && ` · ${groupTitle}`}
        </span>
      </div>

      {loading && <p className="text-sm text-muted-foreground">Yüklənir…</p>}

      {report && !loading && (
        <div className="overflow-x-auto rounded-lg border">
          <table className="w-full text-sm">
            <thead className="bg-muted/50">
              <tr>
                <th className="px-3 py-2 text-left font-medium">{groupTitle || "Satışlar"}</th>
                {fields.map((f) => (
                  <th key={f.key} className="px-3 py-2 text-right font-medium">
                    {f.label}
                  </th>
                ))}
              </tr>
            </thead>
            <tbody>
              <tr className="border-t bg-muted/30 font-bold">
                <td className="px-3 py-2 text-base">{report.total.name}</td>
                {fields.map((f) => (
                  <td key={f.key} className="px-3 py-2 text-right tabular-nums text-base">
                    {fmt(report.total[f.key], f.decimals)}
                  </td>
                ))}
              </tr>
              {rows.map(({ node, path, depth }) => {
                const hasChildren = node.children.length > 0;
                return (
                  <tr key={path} className={cn("border-t", hasChildren && "font-semibold")}>
                    <td className="px-3 py-1.5" style={{ paddingLeft: 12 + depth * 20 }}>
                      {hasChildren ? (
                        <button type="button" className="mr-1 inline-flex align-middle" onClick={() => toggleCollapsed(path)} aria-label="Aç / bağla">
                          {collapsed.has(path) ? <ChevronRight className="h-4 w-4" /> : <ChevronDown className="h-4 w-4" />}
                        </button>
                      ) : (
                        <span className="mr-1 inline-block w-4" />
                      )}
                      {node.name}
                    </td>
                    {fields.map((f) => (
                      <td key={f.key} className="px-3 py-1.5 text-right tabular-nums">
                        {fmt(node[f.key], f.decimals)}
                      </td>
                    ))}
                  </tr>
                );
              })}
              {rows.length === 0 && (
                <tr>
                  <td colSpan={fields.length + 1} className="px-3 py-6 text-center text-muted-foreground">
                    Bu dövrdə satış yoxdur.
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        </div>
      )}

      <Dialog open={paramsOpen} onOpenChange={setParamsOpen}>
        <DialogContent className="sm:max-w-3xl">
          <DialogHeader>
            <DialogTitle>Hesabatların parametrlərinin təyini</DialogTitle>
          </DialogHeader>

          <div className="flex gap-1 border-b">
            {(
              [
                ["fields", "Seçilən sahələr və qruplaşdırmalar"],
                ["conditions", "Seçim şərtləri"],
                ["sort", "Sıralama"],
              ] as const
            ).map(([key, label]) => (
              <button
                key={key}
                type="button"
                onClick={() => setParamTab(key)}
                className={cn("-mb-px border-b-2 px-3 py-1.5 text-sm", paramTab === key ? "border-primary font-medium" : "border-transparent text-muted-foreground")}
              >
                {label}
              </button>
            ))}
          </div>

          {paramTab === "fields" && (
            <div className="grid gap-4 md:grid-cols-2">
              <div>
                <p className="mb-2 text-sm font-medium">Seçilən sahələr</p>
                <div className="space-y-1 rounded-md border p-2">
                  {FIELDS.map((f) => (
                    <label key={f.key} className="flex items-center gap-2 text-sm">
                      <input
                        type="checkbox"
                        checked={draft.fields.includes(f.key)}
                        onChange={(e) =>
                          setDraft((d) => ({ ...d, fields: e.target.checked ? [...d.fields, f.key] : d.fields.filter((x) => x !== f.key) }))
                        }
                      />
                      {f.label}
                    </label>
                  ))}
                </div>
              </div>
              <div>
                <p className="mb-2 text-sm font-medium">Qruplaşdırma (sıra ilə)</p>
                <div className="rounded-md border">
                  {draft.groups.map((g, i) => (
                    <div key={g.key} className="flex items-center gap-2 border-b px-2 py-1 text-sm last:border-b-0">
                      <input
                        type="checkbox"
                        checked={g.on}
                        onChange={(e) => setDraft((d) => ({ ...d, groups: d.groups.map((x, xi) => (xi === i ? { ...x, on: e.target.checked } : x)) }))}
                      />
                      <span className="flex-1">{groupLabel(g.key)}</span>
                      <Button type="button" variant="ghost" size="sm" className="h-7 px-2" disabled={i === 0} onClick={() => moveGroup(i, -1)} aria-label="Yuxarı">
                        ↑
                      </Button>
                      <Button type="button" variant="ghost" size="sm" className="h-7 px-2" disabled={i === draft.groups.length - 1} onClick={() => moveGroup(i, 1)} aria-label="Aşağı">
                        ↓
                      </Button>
                    </div>
                  ))}
                </div>
              </div>
            </div>
          )}

          {paramTab === "conditions" && (
            <div className="rounded-md border">
              <div className="grid grid-cols-[1fr_1fr_2fr] bg-muted/50 px-3 py-1.5 text-xs font-medium">
                <span>Saha</span>
                <span>Seçim tipi</span>
                <span>Siyahı</span>
              </div>
              {CONDITIONS.map((c) => {
                const items = draft.conditions[c.key];
                const on = draft.enabled[c.key];
                return (
                  <div key={c.key} className="grid grid-cols-[1fr_1fr_2fr] items-center gap-2 border-t px-3 py-2 text-sm">
                    <label className="flex items-center gap-2">
                      <input
                        type="checkbox"
                        checked={on}
                        onChange={(e) =>
                          setDraft((d) => ({
                            ...d,
                            enabled: { ...d.enabled, [c.key]: e.target.checked },
                            // Unticking forgets the picked items; ticking alone means "all".
                            conditions: e.target.checked ? d.conditions : { ...d.conditions, [c.key]: [] },
                          }))
                        }
                      />
                      {c.label}
                    </label>
                    <span className="text-muted-foreground">Siyahıya daxil olan</span>
                    <button
                      type="button"
                      onClick={() => setPicker(c.key)}
                      className="truncate rounded-md border px-2 py-1 text-left hover:bg-muted"
                      title={items.map((i) => i.name).join(", ")}
                    >
                      {items.length > 0 ? items.map((i) => i.name).join(", ") : on ? "Hamısı (konkret seçmək üçün basın)" : "Seçmək üçün basın…"}
                    </button>
                  </div>
                );
              })}
            </div>
          )}

          {paramTab === "sort" && (
            <div className="space-y-3">
              <div className="space-y-1">
                <label htmlFor="mx-sort" className="text-sm font-medium">
                  Hər səviyyə üzrə sıralama sahəsi
                </label>
                <select
                  id="mx-sort"
                  className="h-9 w-full rounded-md border border-input bg-background px-3 text-sm"
                  value={draft.sortBy}
                  onChange={(e) => setDraft((d) => ({ ...d, sortBy: e.target.value as StoreSalesMatrixField | "" }))}
                >
                  <option value="">Ad üzrə (əlifba)</option>
                  {FIELDS.map((f) => (
                    <option key={f.key} value={f.key}>
                      {f.label}
                    </option>
                  ))}
                </select>
              </div>
              {draft.sortBy && (
                <label className="flex items-center gap-2 text-sm">
                  <input type="checkbox" checked={draft.sortDesc} onChange={(e) => setDraft((d) => ({ ...d, sortDesc: e.target.checked }))} />
                  Azalan sıra (böyükdən kiçiyə)
                </label>
              )}
            </div>
          )}

          <DialogFooter>
            <Button onClick={apply}>OK</Button>
            <Button variant="outline" onClick={() => setParamsOpen(false)}>
              Bağla
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {picker && (
        <ConditionPicker
          kind={picker}
          title={CONDITIONS.find((c) => c.key === picker)?.label ?? ""}
          selected={draft.conditions[picker]}
          onClose={() => setPicker(null)}
          onSave={(items) => {
            setDraft((d) => ({
              ...d,
              conditions: { ...d.conditions, [picker]: items },
              // Picking someone ticks the condition; clearing the list leaves it on "all" if it was ticked.
              enabled: { ...d.enabled, [picker]: d.enabled[picker] || items.length > 0 },
            }));
            setPicker(null);
          }}
        />
      )}
    </div>
  );
}
