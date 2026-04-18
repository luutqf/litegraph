namespace LiteGraph.GraphRepositories.Interfaces
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using LiteGraph;

    /// <summary>
    /// Interface for request history methods.
    /// Graph repository base methods are responsible only for primitives, not input validation or cross-cutting.
    /// </summary>
    public interface IRequestHistoryMethods
    {
        /// <summary>
        /// Insert a request history record.
        /// </summary>
        /// <param name="detail">Request detail.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Task.</returns>
        Task Insert(RequestHistoryDetail detail, CancellationToken token = default);

        /// <summary>
        /// Read the metadata entry for a request history record.
        /// </summary>
        /// <param name="guid">GUID.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Entry, or null if not found.</returns>
        Task<RequestHistoryEntry> ReadByGuid(Guid guid, CancellationToken token = default);

        /// <summary>
        /// Read the full detail (headers and bodies) for a request history record.
        /// </summary>
        /// <param name="guid">GUID.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Detail, or null if not found.</returns>
        Task<RequestHistoryDetail> ReadDetailByGuid(Guid guid, CancellationToken token = default);

        /// <summary>
        /// Search for request history records with filters and pagination.
        /// </summary>
        /// <param name="search">Search request.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Search result.</returns>
        Task<RequestHistorySearchResult> Search(RequestHistorySearchRequest search, CancellationToken token = default);

        /// <summary>
        /// Get a bucketed summary of request history over a time window.
        /// </summary>
        /// <param name="tenantGuid">Tenant filter, or null for all tenants.</param>
        /// <param name="interval">Bucket interval: minute, 15minute, hour, 6hour, day.</param>
        /// <param name="startUtc">Inclusive start of the window.</param>
        /// <param name="endUtc">Exclusive end of the window.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Summary.</returns>
        Task<RequestHistorySummary> GetSummary(
            Guid? tenantGuid,
            string interval,
            DateTime startUtc,
            DateTime endUtc,
            CancellationToken token = default);

        /// <summary>
        /// Delete a single request history record.
        /// </summary>
        /// <param name="guid">GUID.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Task.</returns>
        Task DeleteByGuid(Guid guid, CancellationToken token = default);

        /// <summary>
        /// Delete multiple request history records matching the search.
        /// </summary>
        /// <param name="search">Search filters.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Number of rows deleted.</returns>
        Task<int> DeleteMany(RequestHistorySearchRequest search, CancellationToken token = default);

        /// <summary>
        /// Delete records older than the specified UTC timestamp.
        /// </summary>
        /// <param name="cutoffUtc">Cutoff.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Number of rows deleted.</returns>
        Task<int> DeleteOlderThan(DateTime cutoffUtc, CancellationToken token = default);
    }
}
