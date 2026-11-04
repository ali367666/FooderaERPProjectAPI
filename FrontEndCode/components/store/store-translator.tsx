"use client";

import { useEffect } from "react";
import { STORE_EN } from "@/lib/store/i18n-en";

const ATTRS = ["placeholder", "title", "aria-label"];
const SKIP = new Set(["SCRIPT", "STYLE", "TEXTAREA", "INPUT", "CODE"]);

/**
 * Shows the store screens in English. The screens are written in Azerbaijani; while English is on, every text and
 * placeholder found in the dictionary is swapped in place (and put back when switched off). Texts that are not in
 * the dictionary, such as server messages, stay as they are.
 */
export default function StoreTranslator({ lang }: { lang: "az" | "en" }) {
  useEffect(() => {
    if (lang !== "en") return;

    const written = new WeakMap<Node, string>(); // what we put into a text node
    const originals = new WeakMap<Node, string>(); // what it said before
    const touched = new Set<Text>();
    const attrWritten = new WeakMap<Element, Map<string, [string, string]>>(); // attr -> [original, english]
    const touchedEls = new Set<Element>();

    const english = (s: string): string | null => {
      const t = s.trim();
      const en = t ? STORE_EN[t] : undefined;
      return en ? s.replace(t, en) : null;
    };

    const doText = (n: Text) => {
      if (written.get(n) === n.data) return;
      const parent = n.parentElement;
      if (!parent || SKIP.has(parent.tagName)) return;
      const en = english(n.data);
      if (en === null) return;
      originals.set(n, n.data);
      written.set(n, en);
      n.data = en;
      touched.add(n);
    };

    const doElement = (el: Element) => {
      for (const attr of ATTRS) {
        const v = el.getAttribute(attr);
        if (!v) continue;
        const known = attrWritten.get(el)?.get(attr);
        if (known && known[1] === v) continue;
        const en = english(v);
        if (en === null) continue;
        if (!attrWritten.has(el)) attrWritten.set(el, new Map());
        attrWritten.get(el)!.set(attr, [v, en]);
        el.setAttribute(attr, en);
        touchedEls.add(el);
      }
    };

    const walk = (root: Node) => {
      if (root.nodeType === Node.TEXT_NODE) {
        doText(root as Text);
        return;
      }
      if (root.nodeType !== Node.ELEMENT_NODE) return;
      const tw = document.createTreeWalker(root, NodeFilter.SHOW_TEXT | NodeFilter.SHOW_ELEMENT);
      let node: Node | null = root;
      while (node) {
        if (node.nodeType === Node.TEXT_NODE) doText(node as Text);
        else if (node.nodeType === Node.ELEMENT_NODE) doElement(node as Element);
        node = tw.nextNode();
      }
    };

    const observer = new MutationObserver((records) => {
      observer.disconnect();
      for (const r of records) {
        if (r.type === "characterData") doText(r.target as Text);
        else if (r.type === "attributes") doElement(r.target as Element);
        else r.addedNodes.forEach((n) => walk(n));
      }
      observer.observe(document.body, { childList: true, subtree: true, characterData: true, attributes: true, attributeFilter: ATTRS });
    });

    walk(document.body);
    observer.observe(document.body, { childList: true, subtree: true, characterData: true, attributes: true, attributeFilter: ATTRS });

    return () => {
      observer.disconnect();
      touched.forEach((n) => {
        const original = originals.get(n);
        if (original !== undefined && written.get(n) === n.data) n.data = original;
      });
      touchedEls.forEach((el) => {
        attrWritten.get(el)?.forEach(([original, en], attr) => {
          if (el.getAttribute(attr) === en) el.setAttribute(attr, original);
        });
      });
    };
  }, [lang]);

  return null;
}
