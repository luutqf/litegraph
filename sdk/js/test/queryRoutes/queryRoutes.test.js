import GraphQueryResult from '../../src/models/GraphQueryResult';
import { api } from '../setupTest';

const mockGraphGuid = 'graph-1';

describe('QueryRoute Tests', () => {
  afterEach(() => {
    jest.restoreAllMocks();
  });

  test('builds native graph query requests', () => {
    const request = api.queryRequest(
      'MATCH (n:Person) WHERE n.name = $name RETURN n LIMIT 1',
      { name: 'Ada' },
      { MaxResults: 25, TimeoutSeconds: 12, IncludeProfile: true }
    );

    expect(request).toMatchObject({
      Query: 'MATCH (n:Person) WHERE n.name = $name RETURN n LIMIT 1',
      Parameters: { name: 'Ada' },
      MaxResults: 25,
      TimeoutSeconds: 12,
      IncludeProfile: true,
    });
  });

  test('executes a native graph query from query text', async () => {
    jest.spyOn(api, 'post').mockImplementation(async (url, request, model) => new model({
      Mutated: false,
      Rows: [{ n: { GUID: 'node-1', Name: 'Ada' } }],
      Nodes: [{ GUID: 'node-1', Name: 'Ada' }],
      RowCount: 1,
      Plan: { Kind: 'MatchNode', Mutates: false, EstimatedCost: 10 },
    }));

    const result = await api.executeQuery(
      mockGraphGuid,
      'MATCH (n:Person) WHERE n.name = $name RETURN n LIMIT 1',
      { name: 'Ada' }
    );

    expect(result instanceof GraphQueryResult).toBe(true);
    expect(api.post).toHaveBeenCalledWith(
      `${api.endpoint}v1.0/tenants/${api.tenantGuid}/graphs/${mockGraphGuid}/query`,
      expect.objectContaining({ Parameters: { name: 'Ada' } }),
      GraphQueryResult,
      undefined
    );
    expect(result.RowCount).toBe(1);
    expect(result.Nodes[0].name).toBe('Ada');
    expect(result.Plan.Kind).toBe('MatchNode');
  });

  test('executes a native graph query request object', async () => {
    jest.spyOn(api, 'post').mockImplementation(async (url, request, model) => new model({
      Rows: [],
      RowCount: 0,
    }));

    await api.executeQuery(mockGraphGuid, {
      Query: 'OPTIONAL MATCH (n) RETURN n LIMIT 1',
      Parameters: {},
    });

    expect(api.post.mock.calls[0][1].Query).toMatch(/^OPTIONAL MATCH/);
  });
});
