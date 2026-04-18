import TransactionResult from '../../src/models/TransactionResult';
import { api } from '../setupTest';
import { mockGraphGuid, mockNodeGuid, mockTagGuid, transactionFailureResponse, transactionResponse } from './mockData';

describe('TransactionRoute Tests', () => {
  afterEach(() => {
    jest.restoreAllMocks();
  });

  test('builds typed graph transaction requests', () => {
    const request = api
      .transaction(mockGraphGuid)
      .withMaxOperations(10)
      .withTimeoutSeconds(30)
      .createNode({ GUID: mockNodeGuid, Name: 'Ada' })
      .attachTag({ GUID: mockTagGuid, NodeGUID: mockNodeGuid, Key: 'role', Value: 'engineer' })
      .deleteNode(mockNodeGuid)
      .build();

    expect(request.MaxOperations).toBe(10);
    expect(request.TimeoutSeconds).toBe(30);
    expect(request.Operations).toHaveLength(3);
    expect(request.Operations[0]).toMatchObject({
      OperationType: 'Create',
      ObjectType: 'Node',
      Payload: { GUID: mockNodeGuid, Name: 'Ada' },
    });
    expect(request.Operations[1]).toMatchObject({
      OperationType: 'Attach',
      ObjectType: 'Tag',
      GUID: null,
    });
    expect(request.Operations[2]).toMatchObject({
      OperationType: 'Delete',
      ObjectType: 'Node',
      GUID: mockNodeGuid,
    });
  });

  test('executes a graph transaction', async () => {
    jest.spyOn(api, 'post').mockImplementation(async (url, request, model) => new model(transactionResponse(request)));

    const result = await api.executeTransaction(mockGraphGuid, {
      Operations: [
        {
          OperationType: 'Create',
          ObjectType: 'Node',
          Payload: { GUID: mockNodeGuid, Name: 'Ada' },
        },
      ],
    });

    expect(result instanceof TransactionResult).toBe(true);
    expect(result.Success).toBe(true);
    expect(result.Operations[0].GUID).toBe(mockNodeGuid);
  });

  test('executes a transaction from the async builder', async () => {
    jest.spyOn(api, 'post').mockImplementation(async (url, request, model) => new model(transactionResponse(request)));

    const result = await api.transaction(mockGraphGuid).createNode({ GUID: mockNodeGuid, Name: 'Ada' }).execute();

    expect(result.Success).toBe(true);
    expect(result.RolledBack).toBe(false);
    expect(result.Operations).toHaveLength(1);
  });

  test('returns rollback details when a transaction fails', async () => {
    jest.spyOn(api, 'post').mockImplementation(async (url, request, model) => new model(transactionFailureResponse));

    const result = await api.transaction(mockGraphGuid).createNode({ GUID: mockNodeGuid, Name: 'fail' }).execute();

    expect(result.Success).toBe(false);
    expect(result.RolledBack).toBe(true);
    expect(result.FailedOperationIndex).toBe(0);
    expect(result.Operations[0].Error).toBe('duplicate node');
  });
});
