namespace Test.Shared
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text.Json;
    using System.Text.Json.Serialization;
    using System.Threading;
    using System.Threading.Tasks;
    using LiteGraph;
    using LiteGraph.GraphRepositories.Sqlite;
    using LiteGraph.Indexing.Vector;
    using Touchstone.Core;

    public static partial class LiteGraphTouchstoneSuites
    {
        private static TestSuiteDescriptor CreateVectorSearchSuite()
        {
            return new TestSuiteDescriptor(
                suiteId: "Vector.Search",
                displayName: "Deterministic vector search coverage",
                cases: new List<TestCaseDescriptor>
                {
                    new TestCaseDescriptor(
                        suiteId: "Vector.Search",
                        caseId: "CosineSimilarity",
                        displayName: "Cosine similarity returns the exact matching node first",
                        executeAsync: ExecuteVectorSearchSuiteAsync)
                });
        }

        private static TestSuiteDescriptor CreateVectorIndexImplementationSuite()
        {
            return new TestSuiteDescriptor(
                suiteId: "Vector.Index.Implementation",
                displayName: "Vector index configuration and lifecycle coverage",
                cases: new List<TestCaseDescriptor>
                {
                    new TestCaseDescriptor(
                        suiteId: "Vector.Index.Implementation",
                        caseId: "Configuration",
                        displayName: "Graph configuration round-trips through VectorIndexConfiguration",
                        executeAsync: ExecuteVectorIndexConfigurationSuiteAsync),
                    new TestCaseDescriptor(
                        suiteId: "Vector.Index.Implementation",
                        caseId: "Lifecycle",
                        displayName: "HNSW RAM index supports enable, search, add, and remove flows",
                        executeAsync: ExecuteVectorIndexLifecycleSuiteAsync)
                });
        }

        private static TestSuiteDescriptor CreateVectorIndexSearchSuite()
        {
            return new TestSuiteDescriptor(
                suiteId: "Vector.Index.Search",
                displayName: "HNSW RAM and SQLite search parity coverage",
                cases: new List<TestCaseDescriptor>
                {
                    new TestCaseDescriptor(
                        suiteId: "Vector.Index.Search",
                        caseId: "RamVsSqlite",
                        displayName: "HNSW RAM and SQLite indices both return deterministic matches",
                        executeAsync: ExecuteVectorIndexSearchSuiteAsync)
                });
        }

        private static async Task ExecuteVectorSearchSuiteAsync(CancellationToken cancellationToken)
        {
            const int dimensionality = 16;
            string databasePath = BuildVectorArtifactPath("touchstone-vector-search.db");

            CleanupSqliteArtifacts(databasePath);

            LiteGraphClient? client = null;

            try
            {
                client = new LiteGraphClient(new SqliteGraphRepository(databasePath, false));
                client.InitializeRepository();

                TenantMetadata tenant = await client.Tenant.Create(new TenantMetadata
                {
                    Name = "Touchstone Vector Search Tenant"
                }, cancellationToken).ConfigureAwait(false);

                Graph graph = await client.Graph.Create(new Graph
                {
                    TenantGUID = tenant.GUID,
                    Name = "Touchstone Vector Search Graph"
                }, cancellationToken).ConfigureAwait(false);

                List<Node> nodes = await CreateDeterministicNodesWithVectorsAsync(
                    client,
                    tenant.GUID,
                    graph.GUID,
                    startIndex: 0,
                    count: 12,
                    dimensionality: dimensionality,
                    cancellationToken).ConfigureAwait(false);

                List<VectorSearchResult> results = await SearchVectorsAsync(
                    client,
                    tenant.GUID,
                    graph.GUID,
                    BuildDeterministicVector(0, dimensionality),
                    VectorSearchTypeEnum.CosineSimilarity,
                    topK: 5,
                    cancellationToken).ConfigureAwait(false);

                AssertTopNode(results, nodes[0].GUID, "Cosine similarity");
                AssertTrue(results.Count <= 5, "Cosine similarity should respect TopK");
                AssertTrue(results[0].Score.HasValue, "Cosine similarity top result score");
            }
            finally
            {
                client?.Dispose();
                CleanupSqliteArtifacts(databasePath);
            }
        }

        private static Task ExecuteVectorIndexConfigurationSuiteAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            Graph graphWithoutIndex = new Graph
            {
                GUID = Guid.NewGuid(),
                TenantGUID = Guid.NewGuid(),
                Name = "Graph Without Indexing",
                VectorIndexType = null,
                VectorIndexFile = null,
                VectorIndexThreshold = null,
                VectorDimensionality = null,
                VectorIndexM = null,
                VectorIndexEf = null,
                VectorIndexEfConstruction = null
            };

            VectorIndexConfiguration graphConfig = new VectorIndexConfiguration(graphWithoutIndex);
            AssertEqual(VectorIndexTypeEnum.None, graphConfig.VectorIndexType, "Graph constructor should map missing index type to None");
            AssertEqual(null, graphConfig.VectorIndexFile, "Graph constructor should preserve null index file");
            AssertEqual(null, graphConfig.VectorIndexThreshold, "Graph constructor should preserve null threshold");
            AssertEqual(null, graphConfig.VectorDimensionality, "Graph constructor should preserve null dimensionality");
            AssertEqual(16, graphConfig.VectorIndexM, "Graph constructor should default VectorIndexM");
            AssertEqual(50, graphConfig.VectorIndexEf, "Graph constructor should default VectorIndexEf");
            AssertEqual(200, graphConfig.VectorIndexEfConstruction, "Graph constructor should default VectorIndexEfConstruction");

            string json = @"{
  ""VectorIndexType"": ""HnswSqlite"",
  ""VectorIndexFile"": ""graph-index.sqlite"",
  ""VectorIndexThreshold"": 4,
  ""VectorDimensionality"": 32,
  ""VectorIndexM"": 12,
  ""VectorIndexEf"": 48,
  ""VectorIndexEfConstruction"": 96
}";

            JsonSerializerOptions options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: false) }
            };

            VectorIndexConfiguration? deserialized = JsonSerializer.Deserialize<VectorIndexConfiguration>(json, options);
            AssertNotNull(deserialized, "JSON configuration");
            AssertEqual(VectorIndexTypeEnum.HnswSqlite, deserialized!.VectorIndexType, "JSON should deserialize HnswSqlite");
            AssertEqual("graph-index.sqlite", deserialized.VectorIndexFile, "JSON should preserve VectorIndexFile");
            AssertEqual(4, deserialized.VectorIndexThreshold, "JSON should preserve VectorIndexThreshold");
            AssertEqual(32, deserialized.VectorDimensionality, "JSON should preserve VectorDimensionality");
            AssertEqual(12, deserialized.VectorIndexM, "JSON should preserve VectorIndexM");
            AssertEqual(48, deserialized.VectorIndexEf, "JSON should preserve VectorIndexEf");
            AssertEqual(96, deserialized.VectorIndexEfConstruction, "JSON should preserve VectorIndexEfConstruction");

            return Task.CompletedTask;
        }

        private static async Task ExecuteVectorIndexLifecycleSuiteAsync(CancellationToken cancellationToken)
        {
            const int dimensionality = 64;
            const int initialNodeCount = 24;
            const int additionalNodeCount = 12;
            const int removalCount = 6;
            string databasePath = BuildVectorArtifactPath("touchstone-vector-index-lifecycle.db");

            CleanupSqliteArtifacts(databasePath);

            LiteGraphClient? client = null;

            try
            {
                client = new LiteGraphClient(new SqliteGraphRepository(databasePath, false));
                client.InitializeRepository();

                TenantMetadata tenant = await client.Tenant.Create(new TenantMetadata
                {
                    Name = "Touchstone Vector Index Lifecycle Tenant"
                }, cancellationToken).ConfigureAwait(false);

                Graph graph = await client.Graph.Create(new Graph
                {
                    TenantGUID = tenant.GUID,
                    Name = "Touchstone Vector Index Lifecycle Graph"
                }, cancellationToken).ConfigureAwait(false);

                List<Node> initialNodes = await CreateDeterministicNodesWithVectorsAsync(
                    client,
                    tenant.GUID,
                    graph.GUID,
                    startIndex: 0,
                    count: initialNodeCount,
                    dimensionality: dimensionality,
                    cancellationToken).ConfigureAwait(false);

                VectorIndexConfiguration configuration = new VectorIndexConfiguration
                {
                    VectorIndexType = VectorIndexTypeEnum.HnswRam,
                    VectorDimensionality = dimensionality,
                    VectorIndexThreshold = 1,
                    VectorIndexM = 8,
                    VectorIndexEf = 32,
                    VectorIndexEfConstruction = 64
                };

                await client.Graph.EnableVectorIndexing(
                    tenant.GUID,
                    graph.GUID,
                    configuration,
                    cancellationToken).ConfigureAwait(false);

                VectorIndexStatistics? initialStats = await client.Graph.GetVectorIndexStatistics(
                    tenant.GUID,
                    graph.GUID,
                    cancellationToken).ConfigureAwait(false);

                AssertNotNull(initialStats, "Initial vector index statistics");
                AssertEqual(VectorIndexTypeEnum.HnswRam, initialStats!.IndexType, "Initial vector index type");
                AssertEqual(initialNodeCount, initialStats.VectorCount, "Initial indexed vector count");
                AssertEqual(dimensionality, initialStats.Dimensions, "Initial vector dimensionality");
                AssertTrue(initialStats.IsLoaded, "Initial HNSW RAM index should be loaded");

                List<VectorSearchResult> cosineResults = await SearchVectorsAsync(
                    client,
                    tenant.GUID,
                    graph.GUID,
                    BuildDeterministicVector(0, dimensionality),
                    VectorSearchTypeEnum.CosineSimilarity,
                    topK: 3,
                    cancellationToken).ConfigureAwait(false);

                AssertTopNode(cosineResults, initialNodes[0].GUID, "HNSW RAM cosine similarity");

                List<Node> additionalNodes = await CreateDeterministicNodesWithVectorsAsync(
                    client,
                    tenant.GUID,
                    graph.GUID,
                    startIndex: initialNodeCount,
                    count: additionalNodeCount,
                    dimensionality: dimensionality,
                    cancellationToken).ConfigureAwait(false);

                client.Flush();

                VectorIndexStatistics? postInsertStats = await client.Graph.GetVectorIndexStatistics(
                    tenant.GUID,
                    graph.GUID,
                    cancellationToken).ConfigureAwait(false);

                AssertNotNull(postInsertStats, "Post-insert vector index statistics");
                AssertEqual(initialNodeCount + additionalNodeCount, postInsertStats!.VectorCount, "Indexed vector count after insert");

                List<VectorSearchResult> dotProductResults = await SearchVectorsAsync(
                    client,
                    tenant.GUID,
                    graph.GUID,
                    BuildDeterministicVector(initialNodeCount, dimensionality),
                    VectorSearchTypeEnum.DotProduct,
                    topK: 3,
                    cancellationToken).ConfigureAwait(false);

                AssertTopNode(dotProductResults, additionalNodes[0].GUID, "HNSW RAM dot product");

                for (int i = 0; i < removalCount; i++)
                {
                    await client.Node.DeleteByGuid(
                        tenant.GUID,
                        graph.GUID,
                        initialNodes[i].GUID,
                        cancellationToken).ConfigureAwait(false);
                }

                client.Flush();

                VectorIndexStatistics? postDeleteStats = await client.Graph.GetVectorIndexStatistics(
                    tenant.GUID,
                    graph.GUID,
                    cancellationToken).ConfigureAwait(false);

                AssertNotNull(postDeleteStats, "Post-delete vector index statistics");
                AssertEqual(initialNodeCount + additionalNodeCount - removalCount, postDeleteStats!.VectorCount, "Indexed vector count after delete");

                List<VectorSearchResult> euclidianResults = await SearchVectorsAsync(
                    client,
                    tenant.GUID,
                    graph.GUID,
                    BuildDeterministicVector(initialNodeCount + 1, dimensionality),
                    VectorSearchTypeEnum.EuclidianDistance,
                    topK: 1,
                    cancellationToken).ConfigureAwait(false);

                AssertTopNode(euclidianResults, additionalNodes[1].GUID, "HNSW RAM euclidian distance");
            }
            finally
            {
                client?.Dispose();
                CleanupSqliteArtifacts(databasePath);
            }
        }

        private static async Task ExecuteVectorIndexSearchSuiteAsync(CancellationToken cancellationToken)
        {
            const int dimensionality = 32;
            const int nodeCount = 18;
            string ramDatabasePath = BuildVectorArtifactPath("touchstone-vector-index-search-ram.db");
            string sqliteDatabasePath = BuildVectorArtifactPath("touchstone-vector-index-search-sqlite.db");
            string sqliteIndexPath = BuildVectorArtifactPath("touchstone-vector-index-search-index.sqlite");

            CleanupSqliteArtifacts(ramDatabasePath, sqliteDatabasePath, sqliteIndexPath);

            VectorIndexStatistics ramStats = await ExecuteIndexedSearchVariantAsync(
                databasePath: ramDatabasePath,
                indexType: VectorIndexTypeEnum.HnswRam,
                indexFile: null,
                dimensionality: dimensionality,
                nodeCount: nodeCount,
                cancellationToken: cancellationToken).ConfigureAwait(false);

            VectorIndexStatistics sqliteStats = await ExecuteIndexedSearchVariantAsync(
                databasePath: sqliteDatabasePath,
                indexType: VectorIndexTypeEnum.HnswSqlite,
                indexFile: sqliteIndexPath,
                dimensionality: dimensionality,
                nodeCount: nodeCount,
                cancellationToken: cancellationToken).ConfigureAwait(false);

            try
            {
                AssertEqual(VectorIndexTypeEnum.HnswRam, ramStats.IndexType, "RAM index type");
                AssertEqual(VectorIndexTypeEnum.HnswSqlite, sqliteStats.IndexType, "SQLite index type");
                AssertEqual(nodeCount, ramStats.VectorCount, "RAM vector count");
                AssertEqual(nodeCount, sqliteStats.VectorCount, "SQLite vector count");
                AssertEqual(dimensionality, ramStats.Dimensions, "RAM dimensionality");
                AssertEqual(dimensionality, sqliteStats.Dimensions, "SQLite dimensionality");
                AssertEqual(sqliteIndexPath, sqliteStats.IndexFile, "SQLite index file path");
                AssertTrue(File.Exists(sqliteIndexPath), "SQLite index file should exist");
            }
            finally
            {
                CleanupSqliteArtifacts(ramDatabasePath, sqliteDatabasePath, sqliteIndexPath);
            }
        }

        private static async Task<VectorIndexStatistics> ExecuteIndexedSearchVariantAsync(
            string databasePath,
            VectorIndexTypeEnum indexType,
            string? indexFile,
            int dimensionality,
            int nodeCount,
            CancellationToken cancellationToken)
        {
            CleanupSqliteArtifacts(databasePath, indexFile);

            LiteGraphClient? client = null;

            try
            {
                client = new LiteGraphClient(new SqliteGraphRepository(databasePath, false));
                client.InitializeRepository();

                TenantMetadata tenant = await client.Tenant.Create(new TenantMetadata
                {
                    Name = "Touchstone " + indexType + " Tenant"
                }, cancellationToken).ConfigureAwait(false);

                Graph graph = await client.Graph.Create(new Graph
                {
                    TenantGUID = tenant.GUID,
                    Name = "Touchstone " + indexType + " Graph",
                    VectorIndexType = indexType,
                    VectorIndexFile = indexFile,
                    VectorDimensionality = dimensionality,
                    VectorIndexThreshold = 1,
                    VectorIndexM = 8,
                    VectorIndexEf = 32,
                    VectorIndexEfConstruction = 64
                }, cancellationToken).ConfigureAwait(false);

                List<Node> nodes = await CreateDeterministicNodesWithVectorsAsync(
                    client,
                    tenant.GUID,
                    graph.GUID,
                    startIndex: 0,
                    count: nodeCount,
                    dimensionality: dimensionality,
                    cancellationToken).ConfigureAwait(false);

                client.Flush();

                VectorIndexStatistics? stats = await client.Graph.GetVectorIndexStatistics(
                    tenant.GUID,
                    graph.GUID,
                    cancellationToken).ConfigureAwait(false);

                AssertNotNull(stats, indexType + " vector index statistics");

                List<VectorSearchResult> results = await SearchVectorsAsync(
                    client,
                    tenant.GUID,
                    graph.GUID,
                    BuildDeterministicVector(0, dimensionality),
                    VectorSearchTypeEnum.CosineSimilarity,
                    topK: 3,
                    cancellationToken).ConfigureAwait(false);

                AssertTopNode(results, nodes[0].GUID, indexType + " cosine similarity");
                return stats!;
            }
            finally
            {
                client?.Dispose();
            }
        }

        private static async Task<List<Node>> CreateDeterministicNodesWithVectorsAsync(
            LiteGraphClient client,
            Guid tenantGuid,
            Guid graphGuid,
            int startIndex,
            int count,
            int dimensionality,
            CancellationToken cancellationToken)
        {
            List<Node> nodes = new List<Node>(count);

            for (int i = 0; i < count; i++)
            {
                int vectorIndex = startIndex + i;
                Guid nodeGuid = Guid.NewGuid();

                nodes.Add(new Node
                {
                    GUID = nodeGuid,
                    TenantGUID = tenantGuid,
                    GraphGUID = graphGuid,
                    Name = "Vector Node " + vectorIndex,
                    Data = new { Index = vectorIndex },
                    Vectors = new List<VectorMetadata>
                    {
                        new VectorMetadata
                        {
                            GUID = Guid.NewGuid(),
                            TenantGUID = tenantGuid,
                            GraphGUID = graphGuid,
                            NodeGUID = nodeGuid,
                            Model = "touchstone-deterministic",
                            Dimensionality = dimensionality,
                            Content = "Vector node " + vectorIndex,
                            Vectors = BuildDeterministicVector(vectorIndex, dimensionality)
                        }
                    }
                });
            }

            return await client.Node.CreateMany(
                tenantGuid,
                graphGuid,
                nodes,
                cancellationToken).ConfigureAwait(false);
        }

        private static async Task<List<VectorSearchResult>> SearchVectorsAsync(
            LiteGraphClient client,
            Guid tenantGuid,
            Guid graphGuid,
            List<float> embeddings,
            VectorSearchTypeEnum searchType,
            int topK,
            CancellationToken cancellationToken)
        {
            VectorSearchRequest request = new VectorSearchRequest
            {
                TenantGUID = tenantGuid,
                GraphGUID = graphGuid,
                Domain = VectorSearchDomainEnum.Node,
                SearchType = searchType,
                Embeddings = embeddings,
                TopK = topK
            };

            if (searchType == VectorSearchTypeEnum.EuclidianDistance)
            {
                request.MaximumDistance = 0.01f;
            }
            else if (searchType == VectorSearchTypeEnum.CosineSimilarity)
            {
                request.MinimumScore = 0.0f;
            }
            else if (searchType == VectorSearchTypeEnum.DotProduct)
            {
                request.MinimumInnerProduct = 0.0f;
            }

            List<VectorSearchResult> results = new List<VectorSearchResult>();

            await foreach (VectorSearchResult result in client.Vector.Search(request, cancellationToken)
                .WithCancellation(cancellationToken)
                .ConfigureAwait(false))
            {
                results.Add(result);
            }

            return results;
        }

        private static void AssertTopNode(
            IReadOnlyList<VectorSearchResult> results,
            Guid expectedNodeGuid,
            string context)
        {
            AssertTrue(results.Count > 0, context + " should return at least one result");
            AssertNotNull(results[0].Node, context + " top result node");
            AssertEqual(expectedNodeGuid, results[0].Node!.GUID, context + " should return the exact match first");
        }

        private static List<float> BuildDeterministicVector(int index, int dimensionality)
        {
            if (index < 0) throw new ArgumentOutOfRangeException(nameof(index));
            if (dimensionality < 1) throw new ArgumentOutOfRangeException(nameof(dimensionality));
            if (index >= dimensionality) throw new ArgumentOutOfRangeException(nameof(index), "Index must be less than the dimensionality for deterministic one-hot vectors.");

            List<float> vector = Enumerable.Repeat(0.0f, dimensionality).ToList();
            vector[index] = 1.0f;
            return vector;
        }

        private static string BuildVectorArtifactPath(string filename)
        {
            string directory = Path.Combine(Path.GetTempPath(), "LiteGraph.Touchstone");
            Directory.CreateDirectory(directory);
            return Path.Combine(directory, filename);
        }

        private static void CleanupSqliteArtifacts(params string?[] paths)
        {
            foreach (string? path in paths)
            {
                if (String.IsNullOrEmpty(path)) continue;

                DeleteIfExists(path);
                DeleteIfExists(path + "-shm");
                DeleteIfExists(path + "-wal");
            }
        }

        private static void DeleteIfExists(string path)
        {
            if (!File.Exists(path)) return;

            try
            {
                File.Delete(path);
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }
    }
}
