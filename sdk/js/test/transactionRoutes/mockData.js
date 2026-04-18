export const mockGraphGuid = '01010101-0101-0101-0101-010101010101';
export const mockNodeGuid = '02020202-0202-0202-0202-020202020202';
export const mockTagGuid = '03030303-0303-0303-0303-030303030303';

export const transactionResponse = (request) => ({
  Success: true,
  RolledBack: false,
  FailedOperationIndex: null,
  Error: null,
  DurationMs: 3.25,
  Operations: request.Operations.map((operation, index) => ({
    Index: index,
    OperationType: operation.OperationType,
    ObjectType: operation.ObjectType,
    GUID: operation.GUID || operation.Payload?.GUID || mockNodeGuid,
    Success: true,
    Result: operation.Payload || null,
  })),
});

export const transactionFailureResponse = {
  Success: false,
  RolledBack: true,
  FailedOperationIndex: 0,
  Error: 'duplicate node',
  DurationMs: 1.25,
  Operations: [
    {
      Index: 0,
      OperationType: 'Create',
      ObjectType: 'Node',
      GUID: mockNodeGuid,
      Success: false,
      Error: 'duplicate node',
    },
  ],
};
