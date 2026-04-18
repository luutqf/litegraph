type TransactionOperationFailure = {
  Index?: number;
  OperationType?: string;
  ObjectType?: string;
  GUID?: string;
  Success?: boolean;
  Error?: string;
};

type TransactionFailure = {
  Success?: boolean;
  RolledBack?: boolean;
  FailedOperationIndex?: number;
  Error?: string;
  Operations?: TransactionOperationFailure[];
};

const asRecord = (value: unknown): Record<string, unknown> | null => {
  if (value && typeof value === 'object' && !Array.isArray(value)) {
    return value as Record<string, unknown>;
  }
  return null;
};

export const getTransactionFailureSummary = (json: unknown): string | null => {
  const record = asRecord(json) as TransactionFailure | null;
  if (!record || record.Success !== false) return null;

  const failedIndex = record.FailedOperationIndex;
  const failedOperation = Array.isArray(record.Operations)
    ? record.Operations.find((operation) => operation.Index === failedIndex || operation.Success === false)
    : undefined;
  const operationType = failedOperation?.OperationType || 'operation';
  const objectType = failedOperation?.ObjectType || 'object';
  const guid = failedOperation?.GUID ? ` (${failedOperation.GUID})` : '';
  const error = failedOperation?.Error || record.Error || 'No error detail returned.';
  const rolledBack = record.RolledBack ? ' The transaction was rolled back.' : '';
  const indexText = typeof failedIndex === 'number' ? ` at index ${failedIndex}` : '';

  return `${operationType} ${objectType}${guid} failed${indexText}: ${error}.${rolledBack}`;
};

export const getQueryErrorSummary = (json: unknown): string | null => {
  const record = asRecord(json);
  if (!record) return null;

  const description =
    typeof record.Description === 'string'
      ? record.Description
      : typeof record.Message === 'string'
        ? record.Message
        : '';

  if (!description || !/query|MATCH|CREATE|CALL|line \d+, column \d+/i.test(description)) return null;

  const location = description.match(/line \d+, column \d+/i)?.[0];
  return location ? `Query error at ${location}: ${description}` : `Query error: ${description}`;
};
