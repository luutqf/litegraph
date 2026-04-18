namespace LiteGraph
{
    using System;

    /// <summary>
    /// User role assignment.
    /// </summary>
    public class UserRoleAssignment
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
        /// User GUID.
        /// </summary>
        public Guid UserGUID { get; set; } = Guid.NewGuid();

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
        public UserRoleAssignment()
        {
        }

        #endregion
    }
}
