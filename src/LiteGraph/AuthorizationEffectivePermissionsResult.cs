namespace LiteGraph
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Effective authorization permissions result.
    /// </summary>
    public class AuthorizationEffectivePermissionsResult
    {
        #region Public-Members

        /// <summary>
        /// Tenant GUID.
        /// </summary>
        public Guid TenantGUID { get; set; } = Guid.Empty;

        /// <summary>
        /// User GUID.
        /// </summary>
        public Guid? UserGUID { get; set; } = null;

        /// <summary>
        /// Credential GUID.
        /// </summary>
        public Guid? CredentialGUID { get; set; } = null;

        /// <summary>
        /// Requested graph GUID filter.
        /// </summary>
        public Guid? GraphGUID { get; set; } = null;

        /// <summary>
        /// Effective grants.
        /// </summary>
        public List<AuthorizationEffectiveGrant> Grants { get; set; } = new List<AuthorizationEffectiveGrant>();

        /// <summary>
        /// User role assignments used to build the result.
        /// </summary>
        public List<UserRoleAssignment> UserRoleAssignments { get; set; } = new List<UserRoleAssignment>();

        /// <summary>
        /// Credential scope assignments used to build the result.
        /// </summary>
        public List<CredentialScopeAssignment> CredentialScopeAssignments { get; set; } = new List<CredentialScopeAssignment>();

        /// <summary>
        /// Resolved roles used to build the result.
        /// </summary>
        public List<AuthorizationRole> Roles { get; set; } = new List<AuthorizationRole>();

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public AuthorizationEffectivePermissionsResult()
        {
        }

        #endregion
    }
}
