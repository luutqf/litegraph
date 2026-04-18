namespace LiteGraph.Client.Interfaces
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Interface for native graph query methods.
    /// </summary>
    public interface IQueryMethods
    {
        /// <summary>
        /// Execute a LiteGraph-native graph query in one tenant and graph.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="graphGuid">Graph GUID.</param>
        /// <param name="request">Query request.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Query result.</returns>
        Task<GraphQueryResult> Execute(Guid tenantGuid, Guid graphGuid, GraphQueryRequest request, CancellationToken token = default);
    }
}
