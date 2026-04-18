namespace LiteGraph
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Credential scope assignment.
    /// </summary>
    public class CredentialScopeAssignment
    {
        #region Public-Members

        /// <summary>
        /// GUID.
        /// </summary>
        public Guid GUID { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Tenant GUID.
        /// </summary>
        public Guid TenantGUID { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Credential GUID.
        /// </summary>
        public Guid CredentialGUID { get; set; } = Guid.NewGuid();

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
        public AuthorizationResourceScopeEnum ResourceScope { get; set; } = AuthorizationResourceScopeEnum.Graph;

        /// <summary>
        /// Graph GUID for graph-scoped assignments.
        /// </summary>
        public Guid? GraphGUID { get; set; } = null;

        /// <summary>
        /// Permissions granted by this credential scope.
        /// </summary>
        public List<AuthorizationPermissionEnum> Permissions { get; set; } = new List<AuthorizationPermissionEnum>();

        /// <summary>
        /// Resource types covered by this credential scope.
        /// </summary>
        public List<AuthorizationResourceTypeEnum> ResourceTypes { get; set; } = new List<AuthorizationResourceTypeEnum>();

        /// <summary>
        /// Creation timestamp, in UTC.
        /// </summary>
        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Last update timestamp, in UTC.
        /// </summary>
        public DateTime LastUpdateUtc { get; set; } = DateTime.UtcNow;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public CredentialScopeAssignment()
        {
        }

        #endregion
    }
}
