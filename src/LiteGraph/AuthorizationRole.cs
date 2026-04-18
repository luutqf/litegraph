namespace LiteGraph
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Stored authorization role.
    /// </summary>
    public class AuthorizationRole
    {
        #region Public-Members

        /// <summary>
        /// GUID.
        /// </summary>
        public Guid GUID { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Tenant GUID. Null identifies a global built-in role definition.
        /// </summary>
        public Guid? TenantGUID { get; set; } = null;

        /// <summary>
        /// Role name.
        /// </summary>
        public string Name { get; set; } = null;

        /// <summary>
        /// Display name.
        /// </summary>
        public string DisplayName { get; set; } = null;

        /// <summary>
        /// Description.
        /// </summary>
        public string Description { get; set; } = null;

        /// <summary>
        /// True for built-in role definitions.
        /// </summary>
        public bool BuiltIn { get; set; } = false;

        /// <summary>
        /// Built-in role kind.
        /// </summary>
        public BuiltInRoleEnum BuiltInRole { get; set; } = BuiltInRoleEnum.Custom;

        /// <summary>
        /// Resource scope.
        /// </summary>
        public AuthorizationResourceScopeEnum ResourceScope { get; set; } = AuthorizationResourceScopeEnum.Graph;

        /// <summary>
        /// Permissions granted by the role.
        /// </summary>
        public List<AuthorizationPermissionEnum> Permissions { get; set; } = new List<AuthorizationPermissionEnum>();

        /// <summary>
        /// Resource types covered by the role.
        /// </summary>
        public List<AuthorizationResourceTypeEnum> ResourceTypes { get; set; } = new List<AuthorizationResourceTypeEnum>();

        /// <summary>
        /// True if tenant-level role assignment is intended to inherit to graphs in the tenant.
        /// </summary>
        public bool InheritsToGraphs { get; set; } = false;

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
        public AuthorizationRole()
        {
        }

        /// <summary>
        /// Create a stored role from a role definition.
        /// </summary>
        /// <param name="definition">Role definition.</param>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <returns>Authorization role.</returns>
        public static AuthorizationRole FromDefinition(RoleDefinition definition, Guid? tenantGuid = null)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));

            return new AuthorizationRole
            {
                TenantGUID = tenantGuid,
                Name = definition.Name,
                DisplayName = definition.DisplayName,
                Description = definition.Description,
                BuiltIn = definition.BuiltIn,
                BuiltInRole = definition.BuiltInRole,
                ResourceScope = definition.ResourceScope,
                Permissions = definition.Permissions != null ? new List<AuthorizationPermissionEnum>(definition.Permissions) : new List<AuthorizationPermissionEnum>(),
                ResourceTypes = definition.ResourceTypes != null ? new List<AuthorizationResourceTypeEnum>(definition.ResourceTypes) : new List<AuthorizationResourceTypeEnum>(),
                InheritsToGraphs = definition.InheritsToGraphs
            };
        }

        #endregion

        #region Public-Methods

        /// <summary>
        /// Check whether the role grants a permission.
        /// </summary>
        /// <param name="permission">Permission.</param>
        /// <returns>True if granted.</returns>
        public bool HasPermission(AuthorizationPermissionEnum permission)
        {
            return Permissions != null && Permissions.Contains(permission);
        }

        /// <summary>
        /// Check whether the role applies to a resource type.
        /// </summary>
        /// <param name="resourceType">Resource type.</param>
        /// <returns>True if applicable.</returns>
        public bool AppliesTo(AuthorizationResourceTypeEnum resourceType)
        {
            return ResourceTypes != null && ResourceTypes.Contains(resourceType);
        }

        /// <summary>
        /// Convert to a role definition.
        /// </summary>
        /// <returns>Role definition.</returns>
        public RoleDefinition ToDefinition()
        {
            return new RoleDefinition
            {
                Name = Name,
                DisplayName = DisplayName,
                Description = Description,
                BuiltInRole = BuiltInRole,
                BuiltIn = BuiltIn,
                ResourceScope = ResourceScope,
                Permissions = Permissions != null ? new List<AuthorizationPermissionEnum>(Permissions) : new List<AuthorizationPermissionEnum>(),
                ResourceTypes = ResourceTypes != null ? new List<AuthorizationResourceTypeEnum>(ResourceTypes) : new List<AuthorizationResourceTypeEnum>(),
                InheritsToGraphs = InheritsToGraphs
            };
        }

        /// <summary>
        /// Clone.
        /// </summary>
        /// <returns>Authorization role.</returns>
        public AuthorizationRole Clone()
        {
            return new AuthorizationRole
            {
                GUID = GUID,
                TenantGUID = TenantGUID,
                Name = Name,
                DisplayName = DisplayName,
                Description = Description,
                BuiltIn = BuiltIn,
                BuiltInRole = BuiltInRole,
                ResourceScope = ResourceScope,
                Permissions = Permissions != null ? new List<AuthorizationPermissionEnum>(Permissions) : new List<AuthorizationPermissionEnum>(),
                ResourceTypes = ResourceTypes != null ? new List<AuthorizationResourceTypeEnum>(ResourceTypes) : new List<AuthorizationResourceTypeEnum>(),
                InheritsToGraphs = InheritsToGraphs,
                CreatedUtc = CreatedUtc,
                LastUpdateUtc = LastUpdateUtc
            };
        }

        #endregion
    }
}
