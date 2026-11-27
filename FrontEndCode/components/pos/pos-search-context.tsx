"use client";

import { createContext, useContext } from "react";

/** The POS header's search box: the layout owns the text, the tables page filters by it. */
export type PosSearch = {
  query: string;
  setQuery: (value: string) => void;
};

export const PosSearchContext = createContext<PosSearch>({ query: "", setQuery: () => {} });

export function usePosSearch(): PosSearch {
  return useContext(PosSearchContext);
}
