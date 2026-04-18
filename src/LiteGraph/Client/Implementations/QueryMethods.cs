namespace LiteGraph.Client.Implementations
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using LiteGraph.Client.Interfaces;
    using LiteGraph.GraphRepositories;

    /// <summary>
    /// Native graph query methods.
    /// </summary>
    public class QueryMethods : IQueryMethods
    {
        private readonly QueryExecutionEngine _ExecutionEngine;

        /// <summary>
        /// Instantiate.
        /// </summary>
        /// <param name="client">LiteGraph client.</param>
        /// <param name="repo">Graph repository.</param>
        public QueryMethods(LiteGraphClient client, GraphRepositoryBase repo)
        {
            if (client == null) throw new ArgumentNullException(nameof(client));
            if (repo == null) throw new ArgumentNullException(nameof(repo));
            _ExecutionEngine = new QueryExecutionEngine(client, repo);
        }

        /// <inheritdoc />
        public Task<GraphQueryResult> Execute(Guid tenantGuid, Guid graphGuid, GraphQueryRequest request, CancellationToken token = default)
        {
            return _ExecutionEngine.Execute(tenantGuid, graphGuid, request, token);
        }
    }
}
