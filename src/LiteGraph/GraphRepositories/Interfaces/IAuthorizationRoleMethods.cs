namespace LiteGraph.GraphRepositories.Interfaces
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Interface for authorization role methods.
    /// Graph repository base methods are responsible only for primitives, not input validation or cross-cutting.
    /// </summary>
    public interface IAuthorizationRoleMethods
    {
        /// <summary>
        /// Create an authorization role.
        /// </summary>
        /// <param name="role">Role.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Role.</returns>
        Task<AuthorizationRole> CreateRole(AuthorizationRole role, CancellationToken token = default);

        /// <summary>
        /// Read a role by GUID.
        /// </summary>
        /// <param name="guid">GUID.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Role.</returns>
        Task<AuthorizationRole> ReadRoleByGuid(Guid guid, CancellationToken token = default);

        /// <summary>
        /// Read a role by tenant and name.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID. Null searches global roles.</param>
        /// <param name="name">Role name.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Role.</returns>
        Task<AuthorizationRole> ReadRoleByName(Guid? tenantGuid, string name, CancellationToken token = default);

        /// <summary>
        /// Search roles.
        /// </summary>
        /// <param name="search">Search request.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Search result.</returns>
        Task<AuthorizationRoleSearchResult> SearchRoles(AuthorizationRoleSearchRequest search, CancellationToken token = default);

        /// <summary>
        /// Update an authorization role.
        /// </summary>
        /// <param name="role">Role.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Role.</returns>
        Task<AuthorizationRole> UpdateRole(AuthorizationRole role, CancellationToken token = default);

        /// <summary>
        /// Delete a role by GUID.
        /// </summary>
        /// <param name="guid">GUID.</param>
        /// <param name="token">Cancellation token.</param>
        Task DeleteRoleByGuid(Guid guid, CancellationToken token = default);

        /// <summary>
        /// Create a user role assignment.
        /// </summary>
        /// <param name="assignment">Assignment.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Assignment.</returns>
        Task<UserRoleAssignment> CreateUserRole(UserRoleAssignment assignment, CancellationToken token = default);

        /// <summary>
        /// Read a user role assignment by GUID.
        /// </summary>
        /// <param name="guid">GUID.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Assignment.</returns>
        Task<UserRoleAssignment> ReadUserRoleByGuid(Guid guid, CancellationToken token = default);

        /// <summary>
        /// Search user role assignments.
        /// </summary>
        /// <param name="search">Search request.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Search result.</returns>
        Task<UserRoleAssignmentSearchResult> SearchUserRoles(UserRoleAssignmentSearchRequest search, CancellationToken token = default);

        /// <summary>
        /// Update a user role assignment.
        /// </summary>
        /// <param name="assignment">Assignment.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Assignment.</returns>
        Task<UserRoleAssignment> UpdateUserRole(UserRoleAssignment assignment, CancellationToken token = default);

        /// <summary>
        /// Delete a user role assignment by GUID.
        /// </summary>
        /// <param name="guid">GUID.</param>
        /// <param name="token">Cancellation token.</param>
        Task DeleteUserRoleByGuid(Guid guid, CancellationToken token = default);

        /// <summary>
        /// Delete user role assignments matching a search request.
        /// </summary>
        /// <param name="search">Search request.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Deleted record count.</returns>
        Task<int> DeleteUserRoles(UserRoleAssignmentSearchRequest search, CancellationToken token = default);

        /// <summary>
        /// Create a credential scope assignment.
        /// </summary>
        /// <param name="assignment">Assignment.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Assignment.</returns>
        Task<CredentialScopeAssignment> CreateCredentialScope(CredentialScopeAssignment assignment, CancellationToken token = default);

        /// <summary>
        /// Read a credential scope assignment by GUID.
        /// </summary>
        /// <param name="guid">GUID.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Assignment.</returns>
        Task<CredentialScopeAssignment> ReadCredentialScopeByGuid(Guid guid, CancellationToken token = default);

        /// <summary>
        /// Search credential scope assignments.
        /// </summary>
        /// <param name="search">Search request.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Search result.</returns>
        Task<CredentialScopeAssignmentSearchResult> SearchCredentialScopes(CredentialScopeAssignmentSearchRequest search, CancellationToken token = default);

        /// <summary>
        /// Update a credential scope assignment.
        /// </summary>
        /// <param name="assignment">Assignment.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Assignment.</returns>
        Task<CredentialScopeAssignment> UpdateCredentialScope(CredentialScopeAssignment assignment, CancellationToken token = default);

        /// <summary>
        /// Delete a credential scope assignment by GUID.
        /// </summary>
        /// <param name="guid">GUID.</param>
        /// <param name="token">Cancellation token.</param>
        Task DeleteCredentialScopeByGuid(Guid guid, CancellationToken token = default);

        /// <summary>
        /// Delete credential scope assignments matching a search request.
        /// </summary>
        /// <param name="search">Search request.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Deleted record count.</returns>
        Task<int> DeleteCredentialScopes(CredentialScopeAssignmentSearchRequest search, CancellationToken token = default);
    }
}
