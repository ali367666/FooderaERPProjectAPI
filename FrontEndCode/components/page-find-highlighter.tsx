"use client";

import { useEffect } from "react";
import { usePathname } from "next/navigation";
import { foldText } from "@/lib/search-text";

const FLASH_CLASS = "find-flash";

/**
 * Opened from the sidebar search (`?find=word`): scrolls to the first place in the page that contains
 * the searched text and flashes it, so the word is easy to spot among many settings.
 */
export function PageFindHighlighter() {
  const pathname = usePathname();

  useEffect(() => {
    const find = new URLSearchParams(window.location.search).get("find")?.trim();
    if (!find) return;
    const words = foldText(find).split(/\s+/).filter(Boolean);
    if (words.length === 0) return;

    let attempts = 0;
    let flashed: HTMLElement | null = null;
    let timer = 0;

    const tryFind = () => {
      attempts += 1;
      const root = document.querySelector("main");
      if (root) {
        const walker = document.createTreeWalker(root, NodeFilter.SHOW_TEXT);
        let node: Node | null;
        while ((node = walker.nextNode())) {
          const parent = node.parentElement;
          if (!parent || /^(SCRIPT|STYLE|NOSCRIPT)$/.test(parent.tagName)) continue;
          const text = foldText(node.textContent);
          if (text.length < 2 || !words.every((w) => text.includes(w))) continue;
          if (!parent.offsetParent) continue; // hidden
          flashed = parent;
          parent.scrollIntoView({ block: "center", behavior: "smooth" });
          parent.classList.add(FLASH_CLASS);
          window.setTimeout(() => parent.classList.remove(FLASH_CLASS), 3500);
          return;
        }
      }
      // Pages load their data first; keep looking for a few seconds.
      if (attempts < 20) timer = window.setTimeout(tryFind, 300);
    };

    timer = window.setTimeout(tryFind, 300);
    return () => {
      window.clearTimeout(timer);
      flashed?.classList.remove(FLASH_CLASS);
    };
  }, [pathname]);

  return null;
}
