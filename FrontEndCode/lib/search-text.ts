/** Lower-cases and folds Azerbaijani letters, so "qebz" finds "Qəbz" and "cek" finds "Çek". */
export function foldText(value: string | null | undefined): string {
  if (!value) return "";
  return value
    .toLowerCase()
    .replace(/i̇/g, "i")
    .replace(/[ıİ]/g, "i")
    .replace(/ə/g, "e")
    .replace(/ö/g, "o")
    .replace(/ü/g, "u")
    .replace(/ş/g, "s")
    .replace(/ç/g, "c")
    .replace(/ğ/g, "g");
}

/** Every typed word must appear somewhere in the text, in any order. */
export function matchesAllWords(text: string, words: string[]): boolean {
  const folded = foldText(text);
  return words.every((w) => folded.includes(w));
}

export function searchWords(query: string): string[] {
  return foldText(query).split(/\s+/).filter(Boolean);
}
