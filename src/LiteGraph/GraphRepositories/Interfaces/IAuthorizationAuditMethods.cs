namespace LiteGraph.GraphRepositories.Interfaces
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using LiteGraph;

    /// <summary>
    /// Interface for authorization audit methods.
    /// Graph repository base methods are responsible only for primitives, not input validation or cross-cutting.
    /// </summary>
    public interface IAuthorizationAuditMethods
    {
        /// <summary>
        /// Insert an authorization audit entry.
        /// </summary>
        /// <param name="entry">Audit entry.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Task.</returns>
        Task Insert(AuthorizationAuditEntry entry, CancellationToken token = default);

        /// <summary>
        /// Read an authorization audit entry.
        /// </summary>
        /// <param name="guid">GUID.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Entry, or null if not found.</returns>
        Task<AuthorizationAuditEntry> ReadByGuid(Guid guid, CancellationToken token = default);

        /// <summary>
        /// Search authorization audit entries with filters and pagination.
        /// </summary>
        /// <param name="search">Search request.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Search result.</returns>
        Task<AuthorizationAuditSearchResult> Search(AuthorizationAuditSearchRequest search, CancellationToken token = default);

        /// <summary>
        /// Delete an authorization audit entry.
        /// </summary>
        /// <param name="guid">GUID.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Task.</returns>
        Task DeleteByGuid(Guid guid, CancellationToken token = default);

        /// <summary>
        /// Delete multiple authorization audit entries matching the search.
        /// </summary>
        /// <param name="search">Search filters.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Number of rows deleted.</returns>
        Task<int> DeleteMany(AuthorizationAuditSearchRequest search, CancellationToken token = default);

        /// <summary>
        /// Delete authorization audit entries older than the specified UTC timestamp.
        /// </summary>
        /// <param name="cutoffUtc">Cutoff.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Number of rows deleted.</returns>
        Task<int> DeleteOlderThan(DateTime cutoffUtc, CancellationToken token = default);
    }
}
