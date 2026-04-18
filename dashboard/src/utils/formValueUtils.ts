export const toPlainJson = <T,>(value: unknown, fallback: T): T => {
  if (value === undefined || value === null) return fallback;

  try {
    return JSON.parse(JSON.stringify(value)) as T;
  } catch {
    return fallback;
  }
};

export const tagsToFormList = (tags: unknown): Array<{ key: string; value: string }> => {
  const plainTags = toPlainJson<Record<string, unknown>>(tags, {});

  return Object.entries(plainTags).map(([key, value]) => ({
    key,
    value: value === undefined || value === null ? '' : String(value),
  }));
};

export const vectorsToFormList = (vectors: unknown): any[] => {
  return toPlainJson<any[]>(Array.isArray(vectors) ? vectors : [], []);
};
