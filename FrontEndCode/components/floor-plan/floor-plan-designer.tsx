"use client";

import { useCallback, useEffect, useRef, useState } from "react";
import {
  type RestaurantTable,
  type TableLayoutUpdate,
  updateTableLayout,
} from "@/lib/services/restaurant-table-service";
import { Button } from "@/components/ui/button";
import {
  Save,
  RotateCcw,
  Grid3X3,
  ZoomIn,
  ZoomOut,
  RefreshCw,
} from "lucide-react";
import { toApiFormError } from "@/lib/api-error";
import { TableFurniture } from "./table-furniture";
import { cn } from "@/lib/utils";

type TableNode = RestaurantTable & {
  isDirty?: boolean;
};

const GRID = 10;
const MIN_ZOOM = 0.4;
const MAX_ZOOM = 2;

function snap(v: number) {
  return Math.round(v / GRID) * GRID;
}

type Props = {
  tables: RestaurantTable[];
  restaurantId: number | null;
  onSaved?: () => void;
};

export function FloorPlanDesigner({ tables, restaurantId, onSaved }: Props) {
  const canvasRef = useRef<HTMLDivElement>(null);
  const [nodes, setNodes] = useState<TableNode[]>([]);
  const [selectedId, setSelectedId] = useState<number | null>(null);
  const [zoom, setZoom] = useState(1);
  const [pan, setPan] = useState({ x: 20, y: 20 });
  const [showGrid, setShowGrid] = useState(true);
  const [saving, setSaving] = useState(false);
  const [saveError, setSaveError] = useState<string | null>(null);

  // Drag state stored in ref to avoid re-renders during drag
  const drag = useRef<{
    active: boolean;
    tableId: number;
    startMouseX: number;
    startMouseY: number;
    startPosX: number;
    startPosY: number;
  } | null>(null);

  // Pan state
  const panDrag = useRef<{
    active: boolean;
    startMouseX: number;
    startMouseY: number;
    startPanX: number;
    startPanY: number;
  } | null>(null);

  // Sync external tables → local nodes
  useEffect(() => {
    const filtered = restaurantId
      ? tables.filter((t) => t.restaurantId === restaurantId)
      : tables;
    setNodes(filtered.map((t) => ({ ...t, isDirty: false })));
    setSelectedId(null);
  }, [tables, restaurantId]);

  const dirtyCount = nodes.filter((n) => n.isDirty).length;

  // ── Drag table ──────────────────────────────────────────
  function handleTableMouseDown(e: React.MouseEvent, tableId: number) {
    if (e.button !== 0) return;
    e.stopPropagation();
    setSelectedId(tableId);

    const table = nodes.find((n) => n.id === tableId);
    if (!table) return;

    drag.current = {
      active: true,
      tableId,
      startMouseX: e.clientX,
      startMouseY: e.clientY,
      startPosX: table.posX,
      startPosY: table.posY,
    };
  }

  const handleMouseMove = useCallback(
    (e: MouseEvent) => {
      const d = drag.current;
      if (d?.active) {
        const dx = (e.clientX - d.startMouseX) / zoom;
        const dy = (e.clientY - d.startMouseY) / zoom;
        const newX = snap(d.startPosX + dx);
        const newY = snap(d.startPosY + dy);
        setNodes((prev) =>
          prev.map((n) =>
            n.id === d.tableId
              ? { ...n, posX: newX, posY: newY, isDirty: true }
              : n,
          ),
        );
        return;
      }
      const pd = panDrag.current;
      if (pd?.active) {
        setPan({
          x: pd.startPanX + (e.clientX - pd.startMouseX),
          y: pd.startPanY + (e.clientY - pd.startMouseY),
        });
      }
    },
    [zoom],
  );

  const handleMouseUp = useCallback(() => {
    drag.current = null;
    panDrag.current = null;
  }, []);

  useEffect(() => {
    window.addEventListener("mousemove", handleMouseMove);
    window.addEventListener("mouseup", handleMouseUp);
    return () => {
      window.removeEventListener("mousemove", handleMouseMove);
      window.removeEventListener("mouseup", handleMouseUp);
    };
  }, [handleMouseMove, handleMouseUp]);

  // ── Pan canvas (middle mouse or right click+drag) ───────
  function handleCanvasMouseDown(e: React.MouseEvent) {
    if (e.button === 1 || e.button === 2) {
      e.preventDefault();
      panDrag.current = {
        active: true,
        startMouseX: e.clientX,
        startMouseY: e.clientY,
        startPanX: pan.x,
        startPanY: pan.y,
      };
    } else {
      setSelectedId(null);
    }
  }

  // ── Zoom wheel ───────────────────────────────────────────
  function handleWheel(e: React.WheelEvent) {
    e.preventDefault();
    setZoom((z) => Math.min(MAX_ZOOM, Math.max(MIN_ZOOM, z - e.deltaY * 0.001)));
  }

  // ── Selected table controls ──────────────────────────────
  const selected = nodes.find((n) => n.id === selectedId) ?? null;

  function changeSelected(patch: Partial<TableNode>) {
    if (!selectedId) return;
    setNodes((prev) =>
      prev.map((n) =>
        n.id === selectedId ? { ...n, ...patch, isDirty: true } : n,
      ),
    );
  }

  // ── Save all dirty ───────────────────────────────────────
  async function handleSave() {
    const dirty = nodes.filter((n) => n.isDirty);
    if (dirty.length === 0) return;
    setSaving(true);
    setSaveError(null);
    try {
      await Promise.all(
        dirty.map((n) => {
          const payload: TableLayoutUpdate = {
            posX: n.posX,
            posY: n.posY,
            width: n.width,
            height: n.height,
            shape: n.shape,
            rotation: n.rotation,
          };
          return updateTableLayout(n.id, payload);
        }),
      );
      setNodes((prev) => prev.map((n) => ({ ...n, isDirty: false })));
      onSaved?.();
    } catch (e) {
      setSaveError(toApiFormError(e, "Saxlama xətası").message);
    } finally {
      setSaving(false);
    }
  }

  // ── Reset to last saved ─────────────────────────────────
  function handleReset() {
    const filtered = restaurantId
      ? tables.filter((t) => t.restaurantId === restaurantId)
      : tables;
    setNodes(filtered.map((t) => ({ ...t, isDirty: false })));
    setSelectedId(null);
  }

  return (
    <div className="flex flex-col h-full gap-3">
      {/* Toolbar */}
      <div className="flex items-center gap-2 flex-wrap">
        <Button
          size="sm"
          onClick={handleSave}
          disabled={dirtyCount === 0 || saving}
          className="gap-1.5"
        >
          <Save className="h-4 w-4" />
          {saving ? "Saxlanır..." : dirtyCount > 0 ? `Saxla (${dirtyCount})` : "Saxla"}
        </Button>
        <Button
          size="sm"
          variant="outline"
          onClick={handleReset}
          disabled={dirtyCount === 0}
          className="gap-1.5"
        >
          <RotateCcw className="h-4 w-4" />
          Sıfırla
        </Button>

        <div className="h-6 border-l border-border mx-1" />

        <Button
          size="sm"
          variant="outline"
          onClick={() => setShowGrid((v) => !v)}
          className={cn("gap-1.5", showGrid && "bg-muted")}
        >
          <Grid3X3 className="h-4 w-4" />
          Grid
        </Button>
        <Button
          size="sm"
          variant="outline"
          onClick={() => setZoom((z) => Math.min(MAX_ZOOM, z + 0.1))}
        >
          <ZoomIn className="h-4 w-4" />
        </Button>
        <span className="text-sm text-muted-foreground w-12 text-center">
          {Math.round(zoom * 100)}%
        </span>
        <Button
          size="sm"
          variant="outline"
          onClick={() => setZoom((z) => Math.max(MIN_ZOOM, z - 0.1))}
        >
          <ZoomOut className="h-4 w-4" />
        </Button>
        <Button
          size="sm"
          variant="outline"
          onClick={() => { setZoom(1); setPan({ x: 20, y: 20 }); }}
        >
          <RefreshCw className="h-4 w-4" />
        </Button>

        {saveError && (
          <span className="text-xs text-destructive ml-2">{saveError}</span>
        )}
        {dirtyCount > 0 && (
          <span className="text-xs text-amber-600 ml-2">
            {dirtyCount} masanın mövqeyi dəyişdi — saxlanmayıb
          </span>
        )}
      </div>

      <div className="flex gap-4 flex-1 min-h-0">
        {/* Canvas */}
        <div
          className="flex-1 rounded-xl border border-border bg-muted/30 overflow-hidden relative cursor-crosshair"
          onMouseDown={handleCanvasMouseDown}
          onWheel={handleWheel}
          onContextMenu={(e) => e.preventDefault()}
          ref={canvasRef}
        >
          {/* Grid overlay */}
          {showGrid && (
            <svg
              className="absolute inset-0 w-full h-full pointer-events-none"
              xmlns="http://www.w3.org/2000/svg"
            >
              <defs>
                <pattern
                  id="grid"
                  width={GRID * zoom}
                  height={GRID * zoom}
                  patternUnits="userSpaceOnUse"
                  x={pan.x % (GRID * zoom)}
                  y={pan.y % (GRID * zoom)}
                >
                  <path
                    d={`M ${GRID * zoom} 0 L 0 0 0 ${GRID * zoom}`}
                    fill="none"
                    stroke="currentColor"
                    strokeWidth="0.3"
                    className="text-border"
                  />
                </pattern>
              </defs>
              <rect width="100%" height="100%" fill="url(#grid)" />
            </svg>
          )}

          {/* Tables layer */}
          <div
            style={{
              position: "absolute",
              left: pan.x,
              top: pan.y,
              transformOrigin: "0 0",
              transform: `scale(${zoom})`,
            }}
          >
            {nodes.map((table) => (
              <div
                key={table.id}
                style={{
                  position: "absolute",
                  left: table.posX,
                  top: table.posY,
                  width: table.width,
                  height: table.height,
                  transform: `rotate(${table.rotation}deg)`,
                  transformOrigin: "center center",
                  cursor: "grab",
                  userSelect: "none",
                }}
                onMouseDown={(e) => handleTableMouseDown(e, table.id)}
              >
                <TableFurniture
                  width={table.width}
                  height={table.height}
                  shape={table.shape}
                  capacity={table.capacity}
                  name={table.name}
                  status={table.isOccupied ? "open" : "empty"}
                  selected={table.id === selectedId}
                  isDirty={table.isDirty}
                  isActive={table.isActive}
                />
              </div>
            ))}
          </div>

          {/* Empty state */}
          {nodes.length === 0 && (
            <div className="absolute inset-0 flex items-center justify-center">
              <p className="text-muted-foreground text-sm">
                Bu restoran üçün masa tapılmadı. Əvvəlcə masa yaradın.
              </p>
            </div>
          )}

          {/* Hint */}
          <div className="absolute bottom-2 right-3 text-[10px] text-muted-foreground/60 pointer-events-none">
            Masanı sürüşdürün • Scroll = zoom • Orta/Sağ klik = pan
          </div>
        </div>

        {/* Inspector panel */}
        {selected && (
          <div className="w-56 shrink-0 rounded-xl border border-border bg-card p-4 flex flex-col gap-4 overflow-y-auto">
            <div>
              <p className="text-xs font-semibold text-muted-foreground uppercase tracking-wider mb-2">
                Masa
              </p>
              <p className="font-semibold text-sm">{selected.name}</p>
              <p className="text-xs text-muted-foreground">{selected.capacity} nəfər</p>
            </div>

            <Inspector label="X" value={selected.posX} min={0} max={2000}
              onChange={(v) => changeSelected({ posX: snap(v) })} />
            <Inspector label="Y" value={selected.posY} min={0} max={2000}
              onChange={(v) => changeSelected({ posY: snap(v) })} />
            <Inspector label="Eni" value={selected.width} min={40} max={300}
              onChange={(v) => changeSelected({ width: snap(v) })} />
            <Inspector label="Hündürlük" value={selected.height} min={40} max={300}
              onChange={(v) => changeSelected({ height: snap(v) })} />
            <Inspector label="Fırlanma" value={selected.rotation} min={0} max={359}
              onChange={(v) => changeSelected({ rotation: v })} />

            <div>
              <p className="text-xs text-muted-foreground mb-1.5">Forma</p>
              <div className="flex flex-col gap-1.5">
                {(["square", "round", "rectangle"] as const).map((shape) => (
                  <button
                    key={shape}
                    onClick={() => changeSelected({ shape })}
                    className={cn(
                      "text-left px-3 py-1.5 rounded-lg text-xs border transition-colors",
                      selected.shape === shape
                        ? "bg-primary text-primary-foreground border-primary"
                        : "border-border hover:border-primary/40",
                    )}
                  >
                    {shape === "square" ? "Kvadrat" : shape === "round" ? "Dairəvi" : "Düzbucaqlı"}
                  </button>
                ))}
              </div>
            </div>

            {selected.isDirty && (
              <p className="text-[10px] text-amber-600">● Dəyişiklik saxlanmayıb</p>
            )}
          </div>
        )}
      </div>
    </div>
  );
}

function Inspector({
  label,
  value,
  min,
  max,
  onChange,
}: {
  label: string;
  value: number;
  min: number;
  max: number;
  onChange: (v: number) => void;
}) {
  return (
    <div>
      <div className="flex items-center justify-between mb-1">
        <p className="text-xs text-muted-foreground">{label}</p>
        <span className="text-xs font-mono">{value}</span>
      </div>
      <input
        type="range"
        min={min}
        max={max}
        value={value}
        onChange={(e) => onChange(Number(e.target.value))}
        className="w-full accent-primary"
      />
      <input
        type="number"
        min={min}
        max={max}
        value={value}
        onChange={(e) => onChange(Number(e.target.value))}
        className="mt-1 w-full rounded-md border border-border bg-background px-2 py-1 text-xs"
      />
    </div>
  );
}
