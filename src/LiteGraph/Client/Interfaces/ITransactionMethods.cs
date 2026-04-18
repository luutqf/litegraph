namespace LiteGraph.Client.Interfaces
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Interface for graph-scoped transaction methods.
    /// </summary>
    public interface ITransactionMethods
    {
        /// <summary>
        /// Create a fluent transaction request builder.
        /// </summary>
        /// <returns>Transaction request builder.</returns>
        TransactionRequestBuilder CreateRequestBuilder();

        /// <summary>
        /// Execute a graph-scoped transaction in one tenant and one graph.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="graphGuid">Graph GUID.</param>
        /// <param name="request">Transaction request.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Transaction result.</returns>
        Task<TransactionResult> Execute(Guid tenantGuid, Guid graphGuid, TransactionRequest request, CancellationToken token = default);
    }
}
