"use client";

import { useEffect, type Dispatch, type SetStateAction } from "react";

/**
 * Clears list row selection when the selected id is no longer present
 * in the current filtered/sorted result set (e.g. search or column filters changed).
 */
export function ListSelectionSync({
  visibleRowIds,
  setSelectedRowId,
}: {
  visibleRowIds: string[];
  setSelectedRowId: Dispatch<SetStateAction<string | null>>;
}) {
  const key = visibleRowIds.join("\u0001");
  useEffect(() => {
    const ids = key.length === 0 ? [] : key.split("\u0001");
    setSelectedRowId((id) => {
      if (id == null) return null;
      return ids.includes(id) ? id : null;
    });
  }, [key, setSelectedRowId]);

  return null;
}
