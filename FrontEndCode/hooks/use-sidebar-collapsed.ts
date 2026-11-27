"use client";

import { useCallback, useEffect, useState } from "react";

const STORAGE_KEY = "dashboardSidebarCollapsed";

/** Whether the left panel is folded; remembered in this browser. */
export function useSidebarCollapsed(): [boolean, (collapsed: boolean) => void] {
  const [collapsed, setCollapsedState] = useState(false);

  useEffect(() => {
    try {
      setCollapsedState(localStorage.getItem(STORAGE_KEY) === "1");
    } catch {
      // storage blocked: the panel starts open
    }
  }, []);

  const setCollapsed = useCallback((value: boolean) => {
    setCollapsedState(value);
    try {
      localStorage.setItem(STORAGE_KEY, value ? "1" : "0");
    } catch {
      // storage blocked: the choice lasts until the page is reloaded
    }
  }, []);

  return [collapsed, setCollapsed];
}
