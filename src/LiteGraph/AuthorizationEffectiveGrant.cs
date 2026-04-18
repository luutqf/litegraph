namespace LiteGraph
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Effective authorization grant.
    /// </summary>
    public class AuthorizationEffectiveGrant
    {
        #region Public-Members

        /// <summary>
        /// Grant source.
        /// </summary>
        public string Source { get; set; } = null;

        /// <summary>
        /// Assignment GUID.
        /// </summary>
        public Guid AssignmentGUID { get; set; } = Guid.Empty;

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
        /// Graph GUID.
        /// </summary>
        public Guid? GraphGUID { get; set; } = null;

        /// <summary>
        /// Permissions granted.
        /// </summary>
        public List<AuthorizationPermissionEnum> Permissions { get; set; } = new List<AuthorizationPermissionEnum>();

        /// <summary>
        /// Resource types granted.
        /// </summary>
        public List<AuthorizationResourceTypeEnum> ResourceTypes { get; set; } = new List<AuthorizationResourceTypeEnum>();

        /// <summary>
        /// True if tenant-scoped grant inherits to graph resources.
        /// </summary>
        public bool InheritsToGraphs { get; set; } = false;

        /// <summary>
        /// True if this grant applies to the requested graph filter.
        /// </summary>
        public bool AppliesToRequestedGraph { get; set; } = true;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public AuthorizationEffectiveGrant()
        {
        }

        #endregion
    }
}
