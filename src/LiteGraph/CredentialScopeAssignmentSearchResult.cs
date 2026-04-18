namespace LiteGraph
{
    using System.Collections.Generic;

    /// <summary>
    /// Credential scope assignment search result.
    /// </summary>
    public class CredentialScopeAssignmentSearchResult
    {
        #region Public-Members

        /// <summary>
        /// Objects.
        /// </summary>
        public List<CredentialScopeAssignment> Objects { get; set; } = new List<CredentialScopeAssignment>();

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
        public CredentialScopeAssignmentSearchResult()
        {
        }

        #endregion
    }
}
