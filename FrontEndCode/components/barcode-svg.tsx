"use client";

import { useEffect, useRef } from "react";
import JsBarcode from "jsbarcode";

type BarcodeSvgProps = {
  value: string;
  className?: string;
  height?: number;
};

export function BarcodeSvg({ value, className, height = 50 }: BarcodeSvgProps) {
  const ref = useRef<SVGSVGElement>(null);

  useEffect(() => {
    if (!ref.current || !value) return;
    try {
      JsBarcode(ref.current, value, {
        format: "CODE128",
        displayValue: true,
        height,
        width: 1.6,
        fontSize: 12,
        margin: 4,
      });
    } catch {
      // Value has characters CODE128 can't encode — leave the SVG empty.
    }
  }, [value, height]);

  if (!value) return null;
  return <svg ref={ref} className={className} />;
}
