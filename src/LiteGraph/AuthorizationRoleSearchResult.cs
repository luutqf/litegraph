namespace LiteGraph
{
    using System.Collections.Generic;

    /// <summary>
    /// Authorization role search result.
    /// </summary>
    public class AuthorizationRoleSearchResult
    {
        #region Public-Members

        /// <summary>
        /// Objects.
        /// </summary>
        public List<AuthorizationRole> Objects { get; set; } = new List<AuthorizationRole>();

        /// <summary>
        /// Page index.
        /// </summary>
        public int Page { get; set; } = 0;

        /// <summary>
        /// Page size.
        /// </summary>
        public int PageSize { get; set; } = 100;

        /// <summary>
        /// Total matching records.
        /// </summary>
        public long TotalCount { get; set; } = 0;

        /// <summary>
        /// Total pages.
        /// </summary>
        public int TotalPages { get; set; } = 0;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public AuthorizationRoleSearchResult()
        {
        }

        #endregion
    }
}
