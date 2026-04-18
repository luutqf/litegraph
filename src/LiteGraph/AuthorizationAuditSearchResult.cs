namespace LiteGraph
{
    using System.Collections.Generic;

    /// <summary>
    /// Paginated result of an authorization audit search.
    /// </summary>
    public class AuthorizationAuditSearchResult
    {
        #region Public-Members

        /// <summary>
        /// Objects on the current page.
        /// </summary>
        public List<AuthorizationAuditEntry> Objects { get; set; } = new List<AuthorizationAuditEntry>();

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
        public AuthorizationAuditSearchResult()
        {
        }

        #endregion
    }
}
