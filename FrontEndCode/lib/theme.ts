/**
 * Company theme ("Rənglər və dizayn") — overrides the CSS design tokens from globals.css at runtime.
 * With no colour/corner style saved, the stylesheet defaults stay in effect.
 */

type ThemeSettings = {
  themePrimaryColor?: string | null;
  themeRadius?: string | null;
};

const RADIUS: Record<string, string> = {
  square: "0.125rem",
  medium: "0.625rem",
  round: "1rem",
};

function isHexColor(value: string): boolean {
  return /^#([0-9a-f]{3}|[0-9a-f]{6})$/i.test(value);
}

/** Black or white text, whichever reads better on the given background. */
function readableForeground(hex: string): string {
  const full = hex.length === 4 ? `#${hex[1]}${hex[1]}${hex[2]}${hex[2]}${hex[3]}${hex[3]}` : hex;
  const r = parseInt(full.slice(1, 3), 16);
  const g = parseInt(full.slice(3, 5), 16);
  const b = parseInt(full.slice(5, 7), 16);
  const luminance = (0.299 * r + 0.587 * g + 0.114 * b) / 255;
  return luminance > 0.6 ? "#111111" : "#ffffff";
}

export function applyTheme(settings: ThemeSettings | null | undefined): void {
  if (typeof document === "undefined") return;
  const root = document.documentElement.style;

  const color = settings?.themePrimaryColor?.trim();
  if (color && isHexColor(color)) {
    root.setProperty("--primary", color);
    root.setProperty("--ring", color);
    root.setProperty("--primary-foreground", readableForeground(color));
  } else {
    root.removeProperty("--primary");
    root.removeProperty("--ring");
    root.removeProperty("--primary-foreground");
  }

  const radius = settings?.themeRadius ? RADIUS[settings.themeRadius] : undefined;
  if (radius) root.setProperty("--radius", radius);
  else root.removeProperty("--radius");
}
