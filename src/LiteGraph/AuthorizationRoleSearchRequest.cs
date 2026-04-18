namespace LiteGraph
{
    using System;

    /// <summary>
    /// Authorization role search request.
    /// </summary>
    public class AuthorizationRoleSearchRequest
    {
        #region Public-Members

        /// <summary>
        /// Tenant GUID.
        /// </summary>
        public Guid? TenantGUID { get; set; } = null;

        /// <summary>
        /// Role name.
        /// </summary>
        public string Name { get; set; } = null;

        /// <summary>
        /// Built-in role flag.
        /// </summary>
        public bool? BuiltIn { get; set; } = null;

        /// <summary>
        /// Built-in role kind.
        /// </summary>
        public BuiltInRoleEnum? BuiltInRole { get; set; } = null;

        /// <summary>
        /// Resource scope.
        /// </summary>
        public AuthorizationResourceScopeEnum? ResourceScope { get; set; } = null;

        /// <summary>
        /// Permission included in the role.
        /// </summary>
        public AuthorizationPermissionEnum? Permission { get; set; } = null;

        /// <summary>
        /// Resource type included in the role.
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
        public AuthorizationRoleSearchRequest()
        {
        }

        #endregion
    }
}
