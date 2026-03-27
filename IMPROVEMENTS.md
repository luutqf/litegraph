# LiteGraph Improvements Roadmap

> **Status Key:** `[ ]` Not started | `[~]` In progress | `[x]` Complete | `[!]` Blocked | `[?]` Needs decision
>
> **Last updated:** 2026-03-26

---

## Phase 1: Developer Ergonomics (Target: Q2 2026)

Goal: Make the first 5 minutes magical for every developer.

---

### 1. OpenAPI 3.0 Specification + Swagger UI

**Priority:** P0 (Critical)
**Effort:** Small (Watson Webserver has built-in support since v6.5.x)
**Impact:** Unlocks auto-generated SDK clients in any language, provides interactive API explorer

#### Why This Matters
- Developers expect `/swagger` or `/openapi.json` on any REST API in 2026
- Enables tools like `openapi-generator` to produce typed clients in Go, Rust, Java, Swift, etc. without manual SDK work
- Interactive Swagger UI lets developers explore the API without reading docs
- Contract-first development: downstream teams can build against the spec before implementation is complete

#### Implementation Steps

- [x] **1.1** Upgrade `Watson` NuGet package from v6.4.0 to v6.6.0 in `LiteGraph.Server.csproj`
- [x] **1.2** Add `using WatsonWebserver.Core.OpenApi;` to `RestServiceHandler.cs`
- [x] **1.3** Call `_Webserver.UseOpenApi(...)` in `RestServiceHandler` constructor with:
  - [x] API title, version, description, contact, license
  - [x] Tag definitions for all 12 resource categories (Admin, Tenants, Users, Credentials, Graphs, Nodes, Edges, Labels, Tags, Vectors, VectorIndex, Routes)
  - [x] Security scheme definitions (Bearer token, email/password headers, security token)
  - [x] Server URL configuration
- [x] **1.4** Add `OpenApiRouteMetadata` to every route registration in `InitializeRoutes()`:
  - [x] Pre-authentication routes (4 routes: HEAD /, GET /, favicon, token/tenants)
  - [x] Token routes (2 routes)
  - [x] Admin routes (6 routes: backups CRUD + flush)
  - [x] Tenant routes (10 routes)
  - [x] User routes (8 routes)
  - [x] Credential routes (11 routes)
  - [x] Label routes (21 routes)
  - [x] Tag routes (21 routes)
  - [x] Vector routes (21 routes)
  - [x] Graph routes (18 routes including vector index)
  - [x] Node routes (22 routes including relationships)
  - [x] Edge routes (19 routes)
  - [x] Route/Traversal routes (1 route)
- [x] **1.5** Verify build succeeds with `dotnet build src/LiteGraph.Server/LiteGraph.Server.csproj`
- [ ] **1.6** Manual smoke test: start server, visit `/swagger`, verify all 169 routes appear grouped by tag
- [ ] **1.7** Verify `/openapi.json` is valid using an online OpenAPI validator

#### Acceptance Criteria
- `GET /openapi.json` returns valid OpenAPI 3.0.3 JSON with all 169 endpoints
- `GET /swagger` renders interactive Swagger UI with routes grouped by tag
- Every route has a human-readable summary and is assigned to exactly one tag
- Security schemes are correctly defined and referenced
- Path parameters (tenantGuid, graphGuid, nodeGuid, etc.) are auto-documented with correct types

#### Files Changed
- `src/LiteGraph.Server/LiteGraph.Server.csproj` (Watson version bump)
- `src/LiteGraph.Server/API/REST/RestServiceHandler.cs` (OpenAPI config + route metadata)

---

### 2. Query Language (LiteQL or Cypher Subset)

**Priority:** P1 (High)
**Effort:** Large (new parser + query executor)
**Impact:** Eliminates "20 API calls for one traversal" problem

#### Why This Matters
- Graph traversals often require chaining multiple REST calls (get node, get edges, get neighbors, filter)
- A query language collapses this into one call: `MATCH (p:Person)-[:KNOWS]->(f) WHERE p.data.age > 30 RETURN f`
- Competing graph databases (Neo4j, ArangoDB, DGraph) all offer query languages
- AI agents benefit enormously from structured query over multiple natural-language API calls

#### Implementation Steps

- [ ] **2.1** Design the query language syntax (decide: custom LiteQL, Cypher subset, or Gremlin subset)
  - [ ] Document supported clauses: MATCH, WHERE, RETURN, ORDER BY, LIMIT
  - [ ] Document supported operators: comparison, boolean, string matching, list containment
  - [ ] Document pattern matching syntax for nodes and edges
  - [ ] Write 20+ example queries covering common use cases
  - [ ] Publish design doc for community feedback
- [ ] **2.2** Implement tokenizer/lexer
  - [ ] File: `src/LiteGraph/Query/Lexer.cs`
  - [ ] Token types: keywords, identifiers, operators, literals, punctuation
  - [ ] Error reporting with line/column positions
  - [ ] Unit tests for all token types
- [ ] **2.3** Implement parser (AST generation)
  - [ ] File: `src/LiteGraph/Query/Parser.cs`
  - [ ] AST node types: MatchClause, WhereClause, ReturnClause, PatternNode, PatternEdge
  - [ ] File: `src/LiteGraph/Query/Ast/` (AST node classes)
  - [ ] Syntax error messages with suggestions
  - [ ] Unit tests for valid and invalid queries
- [ ] **2.4** Implement query planner/optimizer
  - [ ] File: `src/LiteGraph/Query/Planner.cs`
  - [ ] Convert AST to execution plan (sequence of repository operations)
  - [ ] Optimize: push filters down, use indexes when available
  - [ ] Estimate cost for each plan step
- [ ] **2.5** Implement query executor
  - [ ] File: `src/LiteGraph/Query/Executor.cs`
  - [ ] Execute plan against LiteGraphClient
  - [ ] Stream results via IAsyncEnumerable
  - [ ] Respect CancellationToken
  - [ ] Enforce query timeout (configurable, default 30s)
- [ ] **2.6** Add REST endpoint
  - [ ] `POST /v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/query` (accepts query string in body)
  - [ ] `POST /v1.0/tenants/{tenantGuid}/query` (cross-graph queries)
  - [ ] Response includes execution time and result count
- [ ] **2.7** Add to SDKs (C#, Python, JS)
  - [ ] `client.Query.Execute(tenantGuid, graphGuid, queryString)`
  - [ ] Typed result deserialization
- [ ] **2.8** Add to MCP tools
  - [ ] `graph/query` tool with query parameter
- [ ] **2.9** Performance testing
  - [ ] Benchmark against equivalent multi-call sequences
  - [ ] Test with 10K, 100K, 1M node graphs
  - [ ] Document performance characteristics

#### Acceptance Criteria
- Single-call graph pattern matching with filtering and projection
- Performance within 2x of equivalent hand-coded repository calls
- Syntax errors produce helpful messages with line/column positions
- Query timeout prevents runaway queries
- Works with vector-indexed graphs (vector similarity in WHERE clause)

#### Files Created
- `src/LiteGraph/Query/Lexer.cs`
- `src/LiteGraph/Query/Parser.cs`
- `src/LiteGraph/Query/Planner.cs`
- `src/LiteGraph/Query/Executor.cs`
- `src/LiteGraph/Query/Ast/*.cs` (AST node types)
- `src/Test.Query/` (test project)

---

### 3. SDK Resilience Layer

**Priority:** P1 (High)
**Effort:** Medium
**Impact:** Production-grade reliability for REST SDK consumers

#### Why This Matters
- Network failures, transient errors, and server restarts are inevitable in production
- Without built-in retry logic, every SDK consumer must implement their own
- Circuit breakers prevent cascading failures when the server is overloaded
- Connection pooling reduces latency for high-throughput workloads

#### Implementation Steps

- [ ] **3.1** C# SDK: Add retry with exponential backoff
  - [ ] Configurable max retries (default 3)
  - [ ] Configurable base delay (default 500ms)
  - [ ] Jitter to prevent thundering herd
  - [ ] Retry on: 429, 500, 502, 503, 504, network errors
  - [ ] Do NOT retry on: 400, 401, 403, 404, 409
  - [ ] Log retry attempts at Warning level
- [ ] **3.2** C# SDK: Add configurable per-operation timeouts
  - [ ] Default timeout: 30s for reads, 60s for writes, 300s for backups
  - [ ] Timeout overridable per-call via optional parameter
  - [ ] Timeout cancels the CancellationToken
- [ ] **3.3** C# SDK: Add circuit breaker
  - [ ] Track failure rate over sliding window (default 60s)
  - [ ] Open circuit at failure threshold (default 50%)
  - [ ] Half-open state allows probe requests after cooldown (default 30s)
  - [ ] Circuit state observable via event/property
- [ ] **3.4** Python SDK: Add retry with exponential backoff
  - [ ] Mirror C# retry behavior
  - [ ] Use `httpx` retry middleware or manual implementation
  - [ ] Configurable via constructor parameters
- [ ] **3.5** JS SDK: Add retry with exponential backoff
  - [ ] Mirror C# retry behavior
  - [ ] Use `superagent-retry` or manual implementation
  - [ ] Configurable via constructor options
- [ ] **3.6** All SDKs: Add connection health check
  - [ ] Periodic ping to server (configurable interval, default disabled)
  - [ ] Event/callback when connection state changes
  - [ ] Auto-reconnect on failure
- [ ] **3.7** Documentation and examples
  - [ ] Document retry configuration in SDK READMEs
  - [ ] Example: custom retry policy
  - [ ] Example: circuit breaker monitoring

#### Acceptance Criteria
- Transient 500/503 errors are automatically retried without caller intervention
- Circuit breaker prevents request storms against a failing server
- All retry/timeout/circuit-breaker settings are configurable
- Default behavior works well for 95% of use cases without configuration
- Retry attempts are logged for observability

#### Files Changed
- `sdk/csharp/src/LiteGraph.Sdk/` (resilience layer classes)
- `sdk/python/litegraph_sdk/base.py` (retry logic)
- `sdk/js/src/base/SdkBase.js` (retry logic)

---

### 4. Transactions / Batch Atomics

**Priority:** P2 (Medium)
**Effort:** Medium
**Impact:** Data consistency for multi-step graph mutations

#### Why This Matters
- Creating a node with edges and vectors requires 3+ API calls
- If any call fails, the graph is left in an inconsistent state
- Transactions allow all-or-nothing semantics for complex mutations
- Critical for import/migration workflows and AI agent operations

#### Implementation Steps

- [ ] **4.1** Design transaction API
  - [ ] `POST /v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/transaction`
  - [ ] Request body: array of operations with type, method, and payload
  - [ ] Response: array of results or single error (rollback)
  - [ ] Maximum operations per transaction (configurable, default 1000)
  - [ ] Transaction timeout (configurable, default 60s)
- [ ] **4.2** Implement transaction executor in repository layer
  - [ ] Wrap operations in SQLite transaction (`BEGIN IMMEDIATE ... COMMIT`)
  - [ ] Rollback on any operation failure
  - [ ] Return partial results up to failure point for diagnostics
- [ ] **4.3** Add to LiteGraphClient
  - [ ] `client.Batch.ExecuteTransaction(tenantGuid, graphGuid, operations)`
  - [ ] Transaction builder pattern: `client.Batch.BeginTransaction().AddNode(...).AddEdge(...).Commit()`
- [ ] **4.4** Add to REST SDKs
  - [ ] C# SDK: transaction builder
  - [ ] Python SDK: transaction context manager
  - [ ] JS SDK: transaction builder with async/await
- [ ] **4.5** Add to MCP tools
  - [ ] `batch/transaction` tool
- [ ] **4.6** Testing
  - [ ] Test rollback on failure
  - [ ] Test concurrent transactions
  - [ ] Test maximum operation limit
  - [ ] Performance comparison: transaction vs individual calls

#### Acceptance Criteria
- Multi-operation mutations succeed or fail atomically
- Rollback restores graph to pre-transaction state on any failure
- Performance overhead < 10% compared to individual calls
- Transaction timeout prevents long-running locks
- Clear error messages indicating which operation failed and why

---

## Phase 2: Ecosystem & Reach (Target: Q3 2026)

Goal: Meet developers where they are.

---

### 5. ASP.NET Core Migration (or Dual-Mode)

**Priority:** P2 (Medium)
**Effort:** Large
**Impact:** Unlocks standard .NET ecosystem middleware, hosting, and tooling

#### Why This Matters
- ASP.NET Core is the dominant .NET web framework with massive ecosystem
- Standard middleware: OpenTelemetry, rate limiting, response compression, health checks
- Dependency injection enables testability and modularity
- `dotnet watch` for hot reload during development
- Standard deployment: Azure App Service, AWS ECS, Google Cloud Run with zero custom config
- WatsonWebserver is capable but requires learning a non-standard API

#### Implementation Steps

- [ ] **5.1** Create `LiteGraph.Server.AspNet` project alongside existing server
  - [ ] Target net8.0 and net10.0
  - [ ] Reference LiteGraph core project
  - [ ] Use minimal API pattern for route registration
- [ ] **5.2** Implement middleware pipeline
  - [ ] Authentication middleware (bearer token, email/password, security token)
  - [ ] Request context middleware (build RequestContext from HttpContext)
  - [ ] Error handling middleware (exception → ApiErrorResponse)
  - [ ] Logging middleware (request/response logging)
  - [ ] CORS middleware (use built-in ASP.NET CORS)
- [ ] **5.3** Port all 169 routes to minimal API endpoints
  - [ ] Group by resource type using `MapGroup()`
  - [ ] Reuse existing ServiceHandler methods
  - [ ] Add OpenAPI attributes for Swagger generation
- [ ] **5.4** Add standard ASP.NET features
  - [ ] `/healthz` and `/readyz` endpoints
  - [ ] Response compression (gzip, brotli)
  - [ ] Rate limiting middleware
  - [ ] Built-in OpenAPI via `Microsoft.AspNetCore.OpenApi`
- [ ] **5.5** Configuration migration
  - [ ] Support both `litegraph.json` and `appsettings.json`
  - [ ] Environment variable binding via `IConfiguration`
  - [ ] Options pattern for typed configuration
- [ ] **5.6** Testing
  - [ ] `WebApplicationFactory` integration tests
  - [ ] Verify feature parity with Watson-based server
  - [ ] Performance comparison (Watson vs Kestrel)
- [ ] **5.7** Documentation
  - [ ] Migration guide from Watson server to ASP.NET server
  - [ ] Document which server to use and when

#### Acceptance Criteria
- 100% API compatibility with existing Watson-based server
- Standard ASP.NET middleware works out of the box
- Health check endpoints available for container orchestration
- Performance within 10% of Watson-based server
- Both servers can coexist in the solution

#### Files Created
- `src/LiteGraph.Server.AspNet/` (new project)

---

### 6. Python & JS SDK Parity

**Priority:** P0 (Critical)
**Effort:** Large
**Impact:** Developers in the two most popular languages for AI/ML get first-class LiteGraph support

#### Why This Matters
- Python is THE language for AI/ML — the primary audience for a vector-capable graph DB
- JavaScript/Node.js dominates backend web development and serverless functions
- SDK gaps force these developers to fall back to raw HTTP calls, losing type safety and convenience
- Vector operations (the key differentiator) are completely missing from Python SDK

#### Current Coverage Gap Analysis

| Feature Category | C# (reference) | Python | JavaScript |
|---|---|---|---|
| Admin (backup/restore/flush) | 6 methods | 0 | 0 |
| Vectors (CRUD + search) | 21 methods | 0 | 7 |
| Vector Index management | 5 methods | 0 | 0 |
| Graph subgraph/statistics | 4 methods | 0 | 1 |
| Enumerate (pagination v2) | ~15 methods | 0 | 0 |
| Scoped labels/tags/vectors | ~33 methods | 0 | 0 |
| Node routing/connectivity | 4 methods | 0 | 0 |
| Credential advanced | 3 methods | 0 | 0 |
| **Total missing** | — | **~90 methods** | **~50 methods** |

#### Implementation Steps — Python SDK

- [x] **6.1** Add `VectorResource` with full CRUD + search
  - [x] `create(vector_metadata)` → VectorMetadataModel
  - [x] `create_multiple(vectors)` → list[VectorMetadataModel]
  - [x] `retrieve(vector_guid)` → VectorMetadataModel
  - [x] `retrieve_all()` → list[VectorMetadataModel]
  - [x] `update(vector_guid, data)` → VectorMetadataModel
  - [x] `delete(vector_guid)` → None
  - [x] `delete_multiple(guids)` → None
  - [x] `exists(vector_guid)` → bool
  - [x] `search(search_request)` → list[VectorSearchResultModel]
  - [x] `read_graph_vectors(graph_guid)` → list[VectorMetadataModel]
  - [x] `read_node_vectors(graph_guid, node_guid)` → list[VectorMetadataModel]
  - [x] `read_edge_vectors(graph_guid, edge_guid)` → list[VectorMetadataModel]
  - [x] `delete_graph_vectors(graph_guid)` → None
  - [x] `delete_node_vectors(graph_guid, node_guid)` → None
  - [x] `delete_edge_vectors(graph_guid, edge_guid)` → None
- [x] **6.2** Add `AdminResource` with backup/restore/flush
  - [x] `list_backups()` → list[str]
  - [x] `create_backup()` → backup metadata
  - [x] `read_backup(filename)` → backup data
  - [x] `backup_exists(filename)` → bool
  - [x] `delete_backup(filename)` → None
  - [x] `flush()` → None
- [x] **6.3** Add `VectorSearchRequestModel` and `VectorSearchResultModel`
  - [x] Fields: tenant_guid, graph_guid, domain, search_type, vectors, top_k, labels, tags, filter
  - [x] Fields: score, distance, inner_product, graph, node, edge
- [x] **6.4** Add scoped label operations
  - [x] `Label.read_graph_labels(graph_guid)` → list[LabelModel]
  - [x] `Label.read_node_labels(graph_guid, node_guid)` → list[LabelModel]
  - [x] `Label.read_edge_labels(graph_guid, edge_guid)` → list[LabelModel]
  - [x] `Label.delete_graph_labels(graph_guid)` → None
  - [x] `Label.delete_node_labels(graph_guid, node_guid)` → None
  - [x] `Label.delete_edge_labels(graph_guid, edge_guid)` → None
- [x] **6.5** Add scoped tag operations
  - [x] `Tag.read_graph_tags(graph_guid)` → list[TagModel]
  - [x] `Tag.read_node_tags(graph_guid, node_guid)` → list[TagModel]
  - [x] `Tag.read_edge_tags(graph_guid, edge_guid)` → list[TagModel]
  - [x] `Tag.delete_graph_tags(graph_guid)` → None
  - [x] `Tag.delete_node_tags(graph_guid, node_guid)` → None
  - [x] `Tag.delete_edge_tags(graph_guid, edge_guid)` → None
- [x] **6.6** Add graph statistics and vector index methods
  - [x] `Graph.get_statistics(graph_guid)` → statistics object
  - [x] `Graph.enable_vector_index(graph_guid, config)` → None
  - [x] `Graph.disable_vector_index(graph_guid)` → None
  - [x] `Graph.rebuild_vector_index(graph_guid)` → None
  - [x] `Graph.get_vector_index_config(graph_guid)` → config object
  - [x] `Graph.get_vector_index_stats(graph_guid)` → stats object
  - [x] `Graph.get_subgraph(graph_guid, node_guid)` → graph data
- [x] **6.7** Add node connectivity and routing methods
  - [x] `Node.read_most_connected(graph_guid)` → list[NodeModel]
  - [x] `Node.read_least_connected(graph_guid)` → list[NodeModel]
- [x] **6.8** Export and register all new resources in `__init__.py`

#### Implementation Steps — JavaScript SDK

- [x] **6.9** Add Admin methods to `LiteGraphSdk`
  - [x] `listBackups()` → backup list
  - [x] `createBackup()` → backup metadata
  - [x] `readBackup(filename)` → backup data
  - [x] `backupExists(filename)` → bool
  - [x] `deleteBackup(filename)` → None
  - [x] `flushDatabase()` → None
- [x] **6.10** Add Graph vector index methods
  - [x] `enableVectorIndex(tenantGuid, graphGuid, config)` → result
  - [x] `disableVectorIndex(tenantGuid, graphGuid)` → result
  - [x] `rebuildVectorIndex(tenantGuid, graphGuid)` → result
  - [x] `getVectorIndexConfig(tenantGuid, graphGuid)` → config
  - [x] `getVectorIndexStats(tenantGuid, graphGuid)` → stats
- [x] **6.11** Add Graph advanced methods
  - [x] `getSubgraph(tenantGuid, graphGuid, nodeGuid, params)` → subgraph
  - [x] `getSubgraphStatistics(tenantGuid, graphGuid, nodeGuid, params)` → stats
  - [x] `getGraphStatistics(tenantGuid, graphGuid)` → stats
  - [x] `getAllGraphStatistics(tenantGuid)` → stats
- [x] **6.12** Add Node advanced methods
  - [x] `getMostConnectedNodes(tenantGuid, graphGuid)` → nodes
  - [x] `getLeastConnectedNodes(tenantGuid, graphGuid)` → nodes
- [x] **6.13** Add scoped label/tag/vector operations
  - [x] `readGraphLabels(tenantGuid, graphGuid)` → labels
  - [x] `readNodeLabels(tenantGuid, graphGuid, nodeGuid)` → labels
  - [x] `readEdgeLabels(tenantGuid, graphGuid, edgeGuid)` → labels
  - [x] `deleteGraphLabels(tenantGuid, graphGuid)` → result
  - [x] `deleteNodeLabels(tenantGuid, graphGuid, nodeGuid)` → result
  - [x] `deleteEdgeLabels(tenantGuid, graphGuid, edgeGuid)` → result
  - [x] Same pattern for tags and vectors (6 methods each)
- [x] **6.14** Add `VectorSearchRequest` and `VectorSearchResult` models (if not present)
- [ ] **6.15** Write tests for all new Python methods
- [ ] **6.16** Write tests for all new JS methods
- [ ] **6.17** Update README.md for both SDKs documenting new capabilities
- [ ] **6.18** Publish updated packages to PyPI and npm

#### Acceptance Criteria
- Python SDK covers 100% of vector CRUD + search operations
- Python SDK covers admin operations (backup/restore/flush)
- JavaScript SDK covers admin, vector index, and scoped operations
- All new methods follow existing SDK patterns and naming conventions
- New methods have proper error handling using existing exception classes
- TypeScript definitions updated for JS SDK (if applicable)

#### Files Changed/Created — Python
- `sdk/python/litegraph_sdk/resources/vectors.py` (new)
- `sdk/python/litegraph_sdk/resources/admin.py` (new)
- `sdk/python/litegraph_sdk/models/vector_search_request.py` (new)
- `sdk/python/litegraph_sdk/models/vector_search_result.py` (new)
- `sdk/python/litegraph_sdk/resources/graphs.py` (extended)
- `sdk/python/litegraph_sdk/resources/nodes.py` (extended)
- `sdk/python/litegraph_sdk/resources/labels.py` (extended)
- `sdk/python/litegraph_sdk/resources/tags.py` (extended)
- `sdk/python/litegraph_sdk/__init__.py` (updated exports)

#### Files Changed/Created — JavaScript
- `sdk/js/src/base/LiteGraphSdk.js` (extended with ~30 new methods)
- `sdk/js/src/models/VectorSearchRequest.js` (new, if needed)

---

### 7. Change Feeds / Event Streaming

**Priority:** P2 (Medium)
**Effort:** Large
**Impact:** Enables real-time dashboards, cache invalidation, event-driven architectures

#### Why This Matters
- Graph mutations currently require polling to detect changes
- Real-time dashboards need instant notification of node/edge changes
- Cache invalidation in distributed systems needs event streams
- Event sourcing patterns enable audit trails and temporal queries

#### Implementation Steps

- [ ] **7.1** Design event model
  - [ ] Event types: Created, Updated, Deleted for each entity (Graph, Node, Edge, Label, Tag, Vector)
  - [ ] Event payload: entity type, GUID, tenant GUID, graph GUID, before/after snapshots
  - [ ] Event ordering: monotonic sequence number per tenant
  - [ ] Event retention: configurable TTL (default 24h)
- [ ] **7.2** Implement event bus in core library
  - [ ] In-memory event buffer with configurable capacity
  - [ ] Pub/sub pattern: subscribe to event types and/or entity GUIDs
  - [ ] Thread-safe event dispatch
- [ ] **7.3** Add SSE (Server-Sent Events) endpoint
  - [ ] `GET /v1.0/tenants/{tenantGuid}/events` (all events in tenant)
  - [ ] `GET /v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/events` (graph-scoped)
  - [ ] Query params: `since` (sequence number), `types` (filter by event type)
  - [ ] Automatic reconnection support via `Last-Event-ID` header
- [ ] **7.4** Add WebSocket event endpoint (optional)
  - [ ] `ws://host:port/v1.0/tenants/{tenantGuid}/events`
  - [ ] Bidirectional: client can send subscription filters
  - [ ] Heartbeat/ping to detect dead connections
- [ ] **7.5** Add webhook registration
  - [ ] `POST /v1.0/tenants/{tenantGuid}/webhooks` (register callback URL)
  - [ ] Retry failed webhook deliveries with exponential backoff
  - [ ] HMAC signature verification for webhook payloads
  - [ ] Webhook management (list, update, delete, pause)
- [ ] **7.6** Add to SDKs
  - [ ] C#: `client.Events.Subscribe(tenantGuid, callback)`
  - [ ] Python: async iterator `async for event in client.events.stream()`
  - [ ] JS: `client.events.on('NodeCreated', callback)`
- [ ] **7.7** Testing
  - [ ] Test event ordering guarantees
  - [ ] Test reconnection with `Last-Event-ID`
  - [ ] Test webhook delivery and retry
  - [ ] Load test: 10K events/second

#### Acceptance Criteria
- Real-time event delivery < 100ms from mutation to subscriber notification
- Event ordering is guaranteed per-tenant
- SSE endpoint supports reconnection without event loss
- Webhook delivery retries failed calls up to configurable limit
- Events are retained for configurable duration (default 24h)

---

### 8. Schema Constraints (Optional Validation)

**Priority:** P3 (Low)
**Effort:** Medium
**Impact:** Data quality enforcement without rigid schema requirements

#### Why This Matters
- Property graphs are schema-free by design, but real applications need data quality guardrails
- "Every Person node must have an email tag" should be enforceable without application code
- Schema validation catches data errors at write time instead of read time
- Optional schemas maintain flexibility while adding safety

#### Implementation Steps

- [ ] **8.1** Design schema model
  - [ ] Schema applied per-graph (not global)
  - [ ] Node type definitions: required labels, required tags, required data fields
  - [ ] Edge type definitions: required labels, valid from/to node types, required data fields
  - [ ] Validation mode: `Enforce` (reject invalid), `Warn` (log but allow), `Disabled`
- [ ] **8.2** Implement schema storage
  - [ ] `GraphSchema` table in SQLite
  - [ ] CRUD operations via repository layer
  - [ ] Schema versioning (schema changes don't break existing data)
- [ ] **8.3** Implement validation engine
  - [ ] Validate on node/edge create and update
  - [ ] Batch validation for import operations
  - [ ] Validation errors include path to failing constraint
- [ ] **8.4** Add REST endpoints
  - [ ] `PUT /v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/schema` (create/update)
  - [ ] `GET /v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/schema` (read)
  - [ ] `DELETE /v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/schema` (remove)
  - [ ] `POST /v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/schema/validate` (validate existing data)
- [ ] **8.5** Add to SDKs and MCP tools
- [ ] **8.6** Documentation and examples

#### Acceptance Criteria
- Schema validation is optional and disabled by default
- Validation errors include clear descriptions of what failed and why
- Existing data is not affected when a schema is applied (only new writes are validated)
- Warn mode logs violations without rejecting writes
- Schema can be exported/imported for reuse across graphs

---

## Phase 3: Scale & Enterprise (Target: Q4 2026 - Q1 2027)

Goal: Production confidence at scale.

---

### 9. Pluggable Storage Backends

**Priority:** P1 (High)
**Effort:** Very Large
**Impact:** Removes SQLite single-writer bottleneck, enables enterprise deployment

#### Why This Matters
- SQLite is excellent for single-server, moderate-load deployments
- Enterprise customers need PostgreSQL (or similar) for write concurrency, replication, and operational tooling
- A pluggable backend lets users choose based on their needs without code changes
- SQLite remains the zero-config default; PostgreSQL is the production recommendation

#### Implementation Steps

- [ ] **9.1** Refactor repository interfaces
  - [ ] Ensure all repository interfaces are storage-agnostic (no SQLite-specific types leak)
  - [ ] Abstract query builder patterns into reusable components
  - [ ] Define connection management interface
- [ ] **9.2** Implement PostgreSQL backend
  - [ ] `src/LiteGraph.GraphRepositories.Postgres/` (new project)
  - [ ] Use Npgsql for database access
  - [ ] Schema creation/migration scripts
  - [ ] Connection pooling via NpgsqlDataSource
  - [ ] Advisory locks for concurrent write coordination
- [ ] **9.3** Implement backend selection via configuration
  - [ ] `litegraph.json`: `"StorageBackend": "sqlite"` or `"postgres"`
  - [ ] Connection string configuration
  - [ ] Factory pattern for repository creation
- [ ] **9.4** Data migration tools
  - [ ] Export from SQLite, import to PostgreSQL (and vice versa)
  - [ ] Incremental migration for large databases
  - [ ] Verification tool to compare source/target
- [ ] **9.5** Performance benchmarking
  - [ ] Compare SQLite vs PostgreSQL for various workloads
  - [ ] Document when to use which backend
  - [ ] Publish benchmark results
- [ ] **9.6** Testing
  - [ ] Run full test suite against both backends
  - [ ] Concurrent write tests (PostgreSQL advantage)
  - [ ] Connection failure and recovery tests

#### Acceptance Criteria
- PostgreSQL backend passes 100% of existing test suite
- Backend selection is configuration-only (no code changes required)
- SQLite performance is not degraded by the abstraction layer
- PostgreSQL supports concurrent writes from multiple server instances
- Migration tool can transfer 1M+ entities without data loss

---

### 10. RBAC & Fine-Grained Permissions

**Priority:** P2 (Medium)
**Effort:** Large
**Impact:** Enterprise-grade access control

#### Why This Matters
- Current auth is tenant-scoped only: all authenticated users can access everything in their tenant
- Enterprises need role-based access: some users read-only, some admin, some restricted to specific graphs
- API key scoping prevents over-privileged service accounts
- OIDC/OAuth2 integration enables SSO with existing identity providers

#### Implementation Steps

- [ ] **10.1** Design permission model
  - [ ] Roles: `TenantAdmin`, `GraphAdmin`, `Editor`, `Viewer`, `Custom`
  - [ ] Permissions: `Read`, `Write`, `Delete`, `Admin` per resource type
  - [ ] Scopes: Tenant-level, Graph-level, or Global
  - [ ] Role inheritance: TenantAdmin > GraphAdmin > Editor > Viewer
- [ ] **10.2** Implement role storage
  - [ ] `Role` table: GUID, TenantGUID, Name, Permissions (JSON)
  - [ ] `UserRole` table: UserGUID, RoleGUID, Scope (tenant/graph GUID)
  - [ ] `CredentialScope` table: CredentialGUID, AllowedOperations, AllowedGraphs
- [ ] **10.3** Implement authorization engine
  - [ ] Check permissions on every request after authentication
  - [ ] Cache permission lookups (user → roles → permissions)
  - [ ] Log authorization failures for audit
- [ ] **10.4** Add REST endpoints for role management
  - [ ] CRUD operations for roles
  - [ ] Assign/revoke roles for users
  - [ ] List effective permissions for a user
- [ ] **10.5** OIDC/OAuth2 integration
  - [ ] Accept JWT tokens from external identity providers
  - [ ] Map JWT claims to LiteGraph roles
  - [ ] Support JWKS endpoint for key rotation
- [ ] **10.6** API key scoping
  - [ ] Restrict credentials to specific operations (read-only, specific graphs, etc.)
  - [ ] Credential usage audit log
- [ ] **10.7** Testing and documentation
  - [ ] Permission matrix tests (every role × every operation × every scope)
  - [ ] Documentation: permission model, setup guide, migration from current auth

#### Acceptance Criteria
- At minimum, Viewer (read-only) and Editor roles work out of the box
- Graph-level permissions allow isolating sensitive graphs within a tenant
- API keys can be scoped to specific operations for least-privilege service accounts
- Existing deployments continue to work (default: all users get Editor role)
- Performance overhead < 5ms per request for permission checks (cached)

---

### 11. Distributed Vector Indexing

**Priority:** P3 (Low)
**Effort:** Very Large
**Impact:** Support for vector collections larger than single-server memory

#### Why This Matters
- Current HNSW index is single-machine, bounded by available RAM
- Large-scale RAG applications may have 10M+ vectors
- Sharded indexes distribute memory and compute across nodes
- GPU-accelerated distance computation enables real-time search at scale

#### Implementation Steps

- [ ] **11.1** Design sharding strategy
  - [ ] Partition vectors by graph GUID (graph-per-shard)
  - [ ] Partition within large graphs by hash ring
  - [ ] Shard metadata stored in coordination service (etcd/Consul/SQLite)
- [ ] **11.2** Implement shard manager
  - [ ] Discover and connect to shard nodes
  - [ ] Route queries to correct shard(s)
  - [ ] Fan-out for cross-shard searches
  - [ ] Merge results from multiple shards
- [ ] **11.3** Implement replication
  - [ ] Read replicas for search (eventually consistent)
  - [ ] Write forwarding to primary shard
  - [ ] Automatic failover on primary failure
- [ ] **11.4** GPU acceleration (optional)
  - [ ] CUDA kernels for cosine similarity, Euclidean distance, dot product
  - [ ] Batched distance computation
  - [ ] Fallback to CPU when GPU unavailable
- [ ] **11.5** Testing
  - [ ] Test with 10M vectors across 4 shards
  - [ ] Measure recall at various shard counts
  - [ ] Test failover and recovery
  - [ ] Benchmark: single node vs distributed

#### Acceptance Criteria
- Support 10M+ vectors across multiple nodes
- Search latency < 500ms at 95th percentile for 10M vectors
- Recall > 95% compared to brute-force search
- Automatic rebalancing when shards are added/removed
- Graceful degradation when shards are unavailable

---

### 12. Observability

**Priority:** P1 (High)
**Effort:** Medium
**Impact:** Production debugging and performance monitoring

#### Why This Matters
- Current logging is syslog-based with limited structure
- OpenTelemetry is the industry standard for distributed tracing
- Prometheus metrics enable alerting on latency, error rates, and resource usage
- Query profiling helps developers optimize slow operations

#### Implementation Steps

- [ ] **12.1** Add OpenTelemetry tracing
  - [ ] Trace spans for every REST endpoint
  - [ ] Trace spans for repository operations (SQL queries)
  - [ ] Trace spans for vector search (index lookup + distance computation)
  - [ ] Propagate trace context from incoming requests
  - [ ] Export to configurable backend (Jaeger, Zipkin, OTLP)
- [ ] **12.2** Add Prometheus metrics endpoint
  - [ ] `GET /metrics` (Prometheus exposition format)
  - [ ] Request counter by endpoint, method, status code
  - [ ] Request duration histogram by endpoint
  - [ ] Active connections gauge
  - [ ] Graph/node/edge count gauges
  - [ ] Vector search latency histogram
  - [ ] Cache hit/miss ratios
  - [ ] SQLite connection pool utilization
- [ ] **12.3** Structured JSON logging
  - [ ] JSON log format with timestamp, level, message, context fields
  - [ ] Correlation ID in every log entry (from request GUID)
  - [ ] Configurable log output: console, file, OTLP
  - [ ] Backward-compatible with syslog (can use both)
- [ ] **12.4** Query performance profiling
  - [ ] `X-LiteGraph-Profile: true` header enables profiling for a request
  - [ ] Response includes timing breakdown: parse, auth, query, serialize
  - [ ] Slow query logging (threshold configurable, default 1s)
- [ ] **12.5** Dashboard integration
  - [ ] Grafana dashboard template (JSON import)
  - [ ] Alert rules for common failure patterns
  - [ ] Documentation: setup guide for Prometheus + Grafana

#### Acceptance Criteria
- OpenTelemetry traces flow through the full request lifecycle
- Prometheus metrics cover the top 10 operational signals
- Structured JSON logs include correlation IDs for request tracing
- Profiling header has < 5% performance overhead when enabled
- Grafana dashboard template works out of the box

---

## Phase 4: Platform (Target: 2027+)

Goal: From database to platform.

---

### 13. Graph Analytics Engine

**Priority:** P3 (Low)
**Effort:** Very Large
**Impact:** Built-in graph algorithms eliminate need for external tools

#### Implementation Steps

- [ ] **13.1** PageRank algorithm
- [ ] **13.2** Community detection (Louvain method)
- [ ] **13.3** Shortest path (Dijkstra, A*)
- [ ] **13.4** Centrality measures (betweenness, closeness, degree)
- [ ] **13.5** Connected components
- [ ] **13.6** REST endpoints: `POST /v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/analytics/{algorithm}`
- [ ] **13.7** Results stored as node/edge tags for subsequent queries
- [ ] **13.8** Async execution for large graphs with progress reporting

---

### 14. Hybrid Search (Vector + Graph Traversal)

**Priority:** P0 (Critical - THIS IS THE KILLER FEATURE)
**Effort:** Large
**Impact:** No other lightweight graph DB combines semantic similarity with relationship traversal

#### Why This Matters
This is the feature that makes LiteGraph irreplaceable for RAG applications:
- "Find nodes semantically similar to this query that are within 2 hops of this context node"
- Combines the best of vector databases (semantic search) with graph databases (relationship traversal)
- Current workflow requires: (1) vector search, (2) for each result, traverse graph, (3) filter by proximity — 3 separate operations
- Hybrid search does this in one call with query-time optimization

#### Implementation Steps

- [ ] **14.1** Design hybrid query model
  - [ ] `HybridSearchRequest`: vector query + graph traversal constraints
  - [ ] Parameters: query vector, starting node(s), max hops, top K, filters
  - [ ] Scoring: weighted combination of vector similarity and graph distance
  - [ ] Execution strategies: vector-first, graph-first, interleaved
- [ ] **14.2** Implement vector-first strategy
  - [ ] Run vector search to get top N candidates
  - [ ] Filter candidates by graph reachability from starting node(s)
  - [ ] Return intersection with combined scores
- [ ] **14.3** Implement graph-first strategy
  - [ ] Traverse graph from starting node(s) up to max hops
  - [ ] Run vector search only on reachable nodes
  - [ ] Return sorted by vector similarity
- [ ] **14.4** Implement interleaved strategy
  - [ ] Expand graph frontier one hop at a time
  - [ ] At each hop, score frontier nodes by vector similarity
  - [ ] Prune low-similarity branches early
  - [ ] Stop when top K results are stable
- [ ] **14.5** Add REST endpoint
  - [ ] `POST /v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/search/hybrid`
  - [ ] Response includes: matched nodes, similarity scores, graph paths
- [ ] **14.6** Add to SDKs and MCP tools
- [ ] **14.7** Performance benchmarking
  - [ ] Compare strategies for different graph shapes and vector distributions
  - [ ] Optimize for the common case (< 1000 candidate nodes)
  - [ ] Document strategy selection guidelines

#### Acceptance Criteria
- Single API call combines vector similarity with graph proximity
- Results include both similarity scores and graph paths
- Performance < 500ms for graphs with 100K nodes and 10K vectors (with HNSW index)
- At least 2 execution strategies with automatic selection heuristic
- Quality: 90%+ recall compared to exhaustive search

---

### 15. LiteGraph Cloud (Managed Service)

**Priority:** Depends on traction
**Effort:** Very Large
**Impact:** Removes all operational burden for developers

#### Implementation Steps

- [ ] **15.1** Multi-tenant hosting infrastructure
- [ ] **15.2** Usage-based billing (API calls, storage, vector dimensions)
- [ ] **15.3** Auto-scaling based on query load
- [ ] **15.4** Automated backups and point-in-time recovery
- [ ] **15.5** Dashboard: usage metrics, query analytics, billing
- [ ] **15.6** Free tier: 1 tenant, 10K nodes, 1K vectors
- [ ] **15.7** SOC 2 / GDPR compliance
- [ ] **15.8** Global regions (US, EU, APAC)

---

### 16. Plugin System

**Priority:** P3 (Low)
**Effort:** Large
**Impact:** Community-driven extensibility

#### Implementation Steps

- [ ] **16.1** Define plugin interfaces
  - [ ] `IStoragePlugin` — custom storage backends
  - [ ] `IAuthPlugin` — custom authentication providers
  - [ ] `IAnalyticsPlugin` — custom graph algorithms
  - [ ] `IEventPlugin` — custom event handlers (pre/post mutation hooks)
  - [ ] `ISerializerPlugin` — custom serialization formats
- [ ] **16.2** Plugin discovery and loading
  - [ ] Load plugins from configurable directory
  - [ ] NuGet package support for plugin distribution
  - [ ] Plugin version compatibility checking
  - [ ] Hot reload for event plugins (no restart required)
- [ ] **16.3** Plugin SDK
  - [ ] NuGet package: `LiteGraph.Plugin.Sdk`
  - [ ] Base classes and utilities for plugin development
  - [ ] Testing harness for plugin validation
  - [ ] Documentation and sample plugins
- [ ] **16.4** Community plugins (examples)
  - [ ] Neo4j import/export plugin
  - [ ] Weaviate vector sync plugin
  - [ ] Elasticsearch full-text search plugin
  - [ ] Redis cache plugin

---

## Quick Wins (Implement This Week)

These items provide outsized impact with minimal effort:

- [x] **QW-1** Add `/openapi.json` and `/swagger` endpoints (item #1 above — Watson has built-in support, just enable it)
- [ ] **QW-2** Add `POST /v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/query` endpoint accepting structured multi-hop traversal JSON (not a full query language, but structured multi-operation in one call)
- [ ] **QW-3** Add retry logic to C# SDK (wrap RestWrapper calls with 3 retries + exponential backoff)
- [x] **QW-4** Python SDK: Add Vector resource (the most critical gap for the AI/ML audience)
- [x] **QW-5** JS SDK: Add Admin methods (backup/restore is table stakes for production use)

---

## Appendix: Competitive Positioning

| Feature | LiteGraph | Neo4j | Weaviate | Pinecone | Redis |
|---|---|---|---|---|---|
| Embeddable (in-process) | Yes | No | No | No | Yes (limited) |
| REST API | Yes | Yes | Yes | Yes | Yes |
| Multi-tenant | Built-in | Enterprise only | Yes | Yes | No |
| Graph traversal | Yes | Yes | No | No | Limited |
| Vector search (HNSW) | Yes | Add-on | Yes | Yes | Yes |
| Hybrid search | Planned | Limited | Yes | No | No |
| Query language | Planned | Cypher | GraphQL | No | Redis commands |
| MCP integration | 145+ tools | No | No | No | No |
| SQLite backend | Yes | No | No | N/A | No |
| Open source | MIT | GPL/Commercial | BSD | No | BSD/Commercial |
| Package size | < 5MB | > 100MB (JVM) | > 50MB (Go) | N/A (SaaS) | > 10MB |

**Strategic differentiation:** LiteGraph is the only database that combines embeddable deployment, graph traversal, vector search, multi-tenancy, and AI agent integration (MCP) in a single lightweight package under MIT license.
