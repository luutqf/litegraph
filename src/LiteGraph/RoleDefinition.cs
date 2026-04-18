namespace LiteGraph
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Role definition.
    /// </summary>
    public class RoleDefinition
    {
        #region Public-Members

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
        /// Built-in role kind.
        /// </summary>
        public BuiltInRoleEnum BuiltInRole { get; set; } = BuiltInRoleEnum.Custom;

        /// <summary>
        /// True for built-in role definitions.
        /// </summary>
        public bool BuiltIn { get; set; } = false;

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

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public RoleDefinition()
        {
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
        /// Clone.
        /// </summary>
        /// <returns>Role definition.</returns>
        public RoleDefinition Clone()
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

        #endregion
    }
}
