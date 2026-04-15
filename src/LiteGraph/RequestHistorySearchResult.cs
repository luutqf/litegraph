namespace LiteGraph
{
    using System.Collections.Generic;

    /// <summary>
    /// Paginated result of a request history search.
    /// </summary>
    public class RequestHistorySearchResult
    {
        #region Public-Members

        /// <summary>
        /// Objects on the current page.
        /// </summary>
        public List<RequestHistoryEntry> Objects { get; set; } = new List<RequestHistoryEntry>();

        /// <summary>
        /// Total number of matching records.
        /// </summary>
        public long TotalCount { get; set; } = 0;

        /// <summary>
        /// Page index.
        /// </summary>
        public int Page { get; set; } = 0;

        /// <summary>
        /// Page size.
        /// </summary>
        public int PageSize { get; set; } = 25;

        /// <summary>
        /// Total number of pages.
        /// </summary>
        public int TotalPages { get; set; } = 0;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public RequestHistorySearchResult()
        {
        }

        #endregion
    }
}
