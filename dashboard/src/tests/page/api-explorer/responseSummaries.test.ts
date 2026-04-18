import { getQueryErrorSummary, getTransactionFailureSummary } from '@/page/api-explorer/responseSummaries';

describe('API Explorer response summaries', () => {
  it('summarizes failed graph transaction responses', () => {
    const summary = getTransactionFailureSummary({
      Success: false,
      RolledBack: true,
      FailedOperationIndex: 1,
      Error: 'duplicate node',
      Operations: [
        { Index: 0, OperationType: 'Create', ObjectType: 'Node', Success: true },
        {
          Index: 1,
          OperationType: 'Create',
          ObjectType: 'Node',
          GUID: 'node-1',
          Success: false,
          Error: 'duplicate node',
        },
      ],
    });

    expect(summary).toContain('Create Node');
    expect(summary).toContain('node-1');
    expect(summary).toContain('index 1');
    expect(summary).toContain('duplicate node');
    expect(summary).toContain('rolled back');
  });

  it('ignores successful responses', () => {
    expect(getTransactionFailureSummary({ Success: true })).toBeNull();
  });

  it('summarizes graph query line and column errors', () => {
    const summary = getQueryErrorSummary({
      Error: 'BadRequest',
      Description: "WHERE operator expected at line 2, column 14.",
    });

    expect(summary).toContain('line 2, column 14');
    expect(summary).toContain('WHERE operator expected');
  });

  it('ignores non-query errors', () => {
    expect(getQueryErrorSummary({ Error: 'NotFound', Description: 'Graph not found.' })).toBeNull();
  });
});
