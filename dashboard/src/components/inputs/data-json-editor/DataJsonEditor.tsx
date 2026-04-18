'use client';
import React from 'react';
import JsonEditorWithAce from '@/components/inputs/json-editor/JsonEditorWithAce';

type Props = {
  value?: unknown;
  onChange?: (json: unknown) => void;
  uniqueKey?: string;
  mode?: 'code' | 'tree' | 'view' | 'form' | 'text';
  readonly?: boolean;
};

const isPlainObject = (v: unknown): v is Record<string, unknown> =>
  !!v && typeof v === 'object' && !Array.isArray(v);

const DataJsonEditor: React.FC<Props> = ({
  value,
  onChange,
  uniqueKey,
  mode = 'code',
  readonly = false,
}) => {
  const safeValue: unknown = value === undefined || value === null ? {} : value;

  return (
    <JsonEditorWithAce
      key={uniqueKey}
      value={isPlainObject(safeValue) || Array.isArray(safeValue) ? safeValue : {}}
      onChange={(json: unknown) => {
        if (onChange) onChange(json);
      }}
      mode={readonly ? 'view' : mode}
      enableSort={false}
      enableTransform={false}
    />
  );
};

export default DataJsonEditor;
