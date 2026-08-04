"use client";

/**
 * Flat colored "badge" style table renderer, inspired by modern
 * hostess/reservation apps: bold color fill, large table number,
 * small flat chair squares around it (one per capacity seat).
 * Color = status, instantly recognizable without reading text.
 */

export type TableStatus = "empty" | "open" | "in_preparation" | "ready" | "served";

type StatusPalette = { fill: string; border: string; text: string; tag: string; tagText: string; chair: string };

const STATUS_COLORS: Record<TableStatus, StatusPalette> = {
  empty:          { fill: "#f0fdfa", border: "#5eead4", text: "#0f766e", tag: "#5eead4", tagText: "#134e4a", chair: "#5eead4" },
  open:           { fill: "#6366f1", border: "#4338ca", text: "#ffffff", tag: "#312e81", tagText: "#ffffff", chair: "#818cf8" },
  in_preparation: { fill: "#fb923c", border: "#c2410c", text: "#ffffff", tag: "#9a3412", tagText: "#ffffff", chair: "#fdba74" },
  ready:          { fill: "#34d399", border: "#047857", text: "#ffffff", tag: "#065f46", tagText: "#ffffff", chair: "#6ee7b7" },
  served:         { fill: "#f472b6", border: "#be185d", text: "#ffffff", tag: "#9d174d", tagText: "#ffffff", chair: "#f9a8d4" },
};

type Props = {
  width: number;
  height: number;
  shape: "square" | "round" | "rectangle";
  capacity: number;
  name: string;
  status?: TableStatus;
  selected?: boolean;
  isDirty?: boolean;
  isActive?: boolean;
};

export function TableFurniture({
  width,
  height,
  shape,
  capacity,
  name,
  status = "empty",
  selected = false,
  isDirty = false,
  isActive = true,
}: Props) {
  const colors = STATUS_COLORS[status] ?? STATUS_COLORS.empty;

  const CHAIR = Math.max(7, Math.min(11, Math.min(width, height) * 0.12));
  const GAP = 3;
  const pad = CHAIR + GAP + 2;

  const tX = pad, tY = pad;
  const tW = width - pad * 2, tH = height - pad * 2;
  const tCx = tX + tW / 2, tCy = tY + tH / 2;

  const chairs = buildChairs(shape, capacity, tX, tY, tW, tH, CHAIR, GAP);

  const radius = shape === "round" ? Math.min(tW, tH) / 2 : shape === "square" ? Math.min(tW, tH) * 0.28 : tH * 0.22;
  const ringColor = selected ? "#6366f1" : isDirty ? "#f59e0b" : null;
  const uid = name.replace(/[^a-zA-Z0-9]/g, "") + Math.round(width);
  const shadowId = `ts-${uid}`;

  const numFontSize = Math.max(13, Math.min(24, Math.min(tW, tH) * 0.34));
  const tagFontSize = Math.max(8, Math.min(11, tW * 0.16));
  const showTag = Math.min(tW, tH) > 38;

  return (
    <svg
      width={width}
      height={height}
      xmlns="http://www.w3.org/2000/svg"
      style={{ overflow: "visible", opacity: isActive ? 1 : 0.45 }}
    >
      <defs>
        <filter id={shadowId} x="-30%" y="-30%" width="160%" height="160%">
          <feDropShadow dx="0" dy="1.5" stdDeviation="2" floodColor="#00000025" />
        </filter>
      </defs>

      {/* Chairs — one flat square per seat */}
      {chairs.map((c, i) => (
        <rect
          key={i}
          x={c.cx - CHAIR / 2}
          y={c.cy - CHAIR / 2}
          width={CHAIR}
          height={CHAIR}
          rx={CHAIR * 0.3}
          ry={CHAIR * 0.3}
          fill={colors.chair}
          opacity={status === "empty" ? 0.7 : 0.95}
        />
      ))}

      {/* Table badge */}
      {shape === "round" ? (
        <circle
          cx={tCx} cy={tCy} r={Math.min(tW, tH) / 2}
          fill={colors.fill} stroke={colors.border} strokeWidth="2"
          filter={`url(#${shadowId})`}
        />
      ) : (
        <rect
          x={tX} y={tY} width={tW} height={tH}
          rx={radius} ry={radius}
          fill={colors.fill} stroke={colors.border} strokeWidth="2"
          filter={`url(#${shadowId})`}
        />
      )}

      {/* Glossy highlight for a brighter, more pleasant look */}
      <ellipse
        cx={tCx - tW * 0.15}
        cy={tY + tH * 0.18}
        rx={tW * 0.32}
        ry={tH * 0.14}
        fill="#ffffff"
        opacity={status === "empty" ? 0.25 : 0.22}
      />

      {/* Table number */}
      <text
        x={tCx}
        y={showTag ? tCy - tH * 0.1 : tCy}
        textAnchor="middle"
        dominantBaseline="middle"
        fontSize={numFontSize}
        fontWeight="700"
        fill={colors.text}
        fontFamily="system-ui, sans-serif"
      >
        {name}
      </text>

      {/* Capacity tag */}
      {showTag && (
        <g transform={`translate(${tCx}, ${tCy + tH * 0.28})`}>
          <rect x={-14} y={-7} width={28} height={14} rx={7} fill={colors.tag} opacity="0.9" />
          <text
            x={0} y={0.5}
            textAnchor="middle"
            dominantBaseline="middle"
            fontSize={tagFontSize}
            fontWeight="600"
            fill={colors.tagText}
            fontFamily="system-ui, sans-serif"
          >
            {capacity}
          </text>
        </g>
      )}

      {/* Selected / dirty ring */}
      {ringColor && (
        shape === "round" ? (
          <circle
            cx={tCx} cy={tCy} r={Math.min(tW, tH) / 2 + 4}
            fill="none" stroke={ringColor} strokeWidth="2.5" strokeDasharray="5 3"
          />
        ) : (
          <rect
            x={tX - 4} y={tY - 4} width={tW + 8} height={tH + 8}
            rx={radius + 3} fill="none" stroke={ringColor} strokeWidth="2.5" strokeDasharray="5 3"
          />
        )
      )}
    </svg>
  );
}

type Chair = { cx: number; cy: number };

function buildChairs(
  shape: string,
  capacity: number,
  tX: number, tY: number, tW: number, tH: number,
  cSize: number, gap: number,
): Chair[] {
  const chairs: Chair[] = [];
  const n = Math.max(1, Math.min(capacity, 14));

  if (shape === "round") {
    const rx = tW / 2, ry = tH / 2;
    const cx0 = tX + rx, cy0 = tY + ry;
    for (let i = 0; i < n; i++) {
      const angle = (i / n) * Math.PI * 2 - Math.PI / 2;
      const dist = Math.min(rx, ry) + gap + cSize / 2;
      chairs.push({ cx: cx0 + Math.cos(angle) * dist, cy: cy0 + Math.sin(angle) * dist });
    }
    return chairs;
  }

  const slotsTop = Math.max(1, Math.floor(tW / (cSize + 3)));
  const slotsLeft = Math.max(1, Math.floor(tH / (cSize + 3)));
  const allocation = allocate(n, [slotsTop, slotsTop, slotsLeft, slotsLeft]);
  const sides: Array<"top" | "bottom" | "left" | "right"> = ["top", "bottom", "left", "right"];

  sides.forEach((side, si) => {
    const count = allocation[si];
    if (count === 0) return;
    for (let i = 0; i < count; i++) {
      const t = (i + 0.5) / count;
      let cx: number, cy: number;
      if (side === "top") { cx = tX + t * tW; cy = tY - gap - cSize / 2; }
      else if (side === "bottom") { cx = tX + (1 - t) * tW; cy = tY + tH + gap + cSize / 2; }
      else if (side === "left") { cx = tX - gap - cSize / 2; cy = tY + (1 - t) * tH; }
      else { cx = tX + tW + gap + cSize / 2; cy = tY + t * tH; }
      chairs.push({ cx, cy });
    }
  });

  return chairs;
}

function allocate(total: number, maxPerSlot: number[]): number[] {
  const result = [0, 0, 0, 0];
  let remaining = total;
  const order = [0, 2, 1, 3];
  for (const i of order) {
    if (remaining <= 0) break;
    const give = Math.min(maxPerSlot[i], Math.ceil(remaining / 2));
    result[i] = give;
    remaining -= give;
  }
  return result;
}
