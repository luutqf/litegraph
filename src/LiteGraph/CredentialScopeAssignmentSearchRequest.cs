namespace LiteGraph
{
    using System;

    /// <summary>
    /// Credential scope assignment search request.
    /// </summary>
    public class CredentialScopeAssignmentSearchRequest
    {
        #region Public-Members

        /// <summary>
        /// Tenant GUID.
        /// </summary>
        public Guid? TenantGUID { get; set; } = null;

        /// <summary>
        /// Credential GUID.
        /// </summary>
        public Guid? CredentialGUID { get; set; } = null;

        /// <summary>
        /// Role GUID.
        /// </summary>
        public Guid? RoleGUID { get; set; } = null;

        /// <summary>
        /// Role name.
        /// </summary>
        public string RoleName { get; set; } = null;

        /// <summary>
        /// Resource scope.
        /// </summary>
        public AuthorizationResourceScopeEnum? ResourceScope { get; set; } = null;

        /// <summary>
        /// Graph GUID.
        /// </summary>
        public Guid? GraphGUID { get; set; } = null;

        /// <summary>
        /// Permission included in this credential scope.
        /// </summary>
        public AuthorizationPermissionEnum? Permission { get; set; } = null;

        /// <summary>
        /// Resource type included in this credential scope.
        /// </summary>
        public AuthorizationResourceTypeEnum? ResourceType { get; set; } = null;

        /// <summary>
        /// Earliest creation timestamp, inclusive.
        /// </summary>
        public DateTime? FromUtc { get; set; } = null;

        /// <summary>
        /// Latest creation timestamp, exclusive.
        /// </summary>
        public DateTime? ToUtc { get; set; } = null;

        /// <summary>
        /// Page index.
        /// </summary>
        public int Page { get; set; } = 0;

        /// <summary>
        /// Page size.
        /// </summary>
        public int PageSize { get; set; } = 100;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public CredentialScopeAssignmentSearchRequest()
        {
        }

        #endregion
    }
}
