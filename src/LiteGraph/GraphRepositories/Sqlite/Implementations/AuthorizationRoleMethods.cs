namespace LiteGraph.GraphRepositories.Sqlite.Implementations
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Threading;
    using System.Threading.Tasks;
    using LiteGraph.GraphRepositories.Interfaces;
    using LiteGraph.GraphRepositories.Sqlite.Queries;

    /// <summary>
    /// Authorization role methods.
    /// Graph repository base methods are responsible only for primitives, not input validation or cross-cutting.
    /// </summary>
    public class AuthorizationRoleMethods : IAuthorizationRoleMethods
    {
        #region Private-Members

        private readonly SqliteGraphRepository _Repo = null;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Authorization role methods.
        /// </summary>
        /// <param name="repo">Graph repository.</param>
        public AuthorizationRoleMethods(SqliteGraphRepository repo)
        {
            _Repo = repo ?? throw new ArgumentNullException(nameof(repo));
        }

        #endregion

        #region Public-Methods

        /// <inheritdoc />
        public async Task<AuthorizationRole> CreateRole(AuthorizationRole role, CancellationToken token = default)
        {
            if (role == null) throw new ArgumentNullException(nameof(role));
            token.ThrowIfCancellationRequested();

            DataTable result = await _Repo.ExecuteQueryAsync(AuthorizationRoleQueries.InsertRole(role), true, token).ConfigureAwait(false);
            if (result == null || result.Rows.Count < 1) return null;
            AuthorizationRole created = RowToRole(result.Rows[0]);
            AuthorizationPolicyChangeTracker.SignalChanged();
            return created;
        }

        /// <inheritdoc />
        public async Task<AuthorizationRole> ReadRoleByGuid(Guid guid, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();

            DataTable result = await _Repo.ExecuteQueryAsync(AuthorizationRoleQueries.SelectRoleByGuid(guid), false, token).ConfigureAwait(false);
            if (result == null || result.Rows.Count < 1) return null;
            return RowToRole(result.Rows[0]);
        }

        /// <inheritdoc />
        public async Task<AuthorizationRole> ReadRoleByName(Guid? tenantGuid, string name, CancellationToken token = default)
        {
            if (String.IsNullOrWhiteSpace(name)) throw new ArgumentNullException(nameof(name));
            token.ThrowIfCancellationRequested();

            DataTable result = await _Repo.ExecuteQueryAsync(AuthorizationRoleQueries.SelectRoleByName(tenantGuid, name), false, token).ConfigureAwait(false);
            if (result == null || result.Rows.Count < 1) return null;
            return RowToRole(result.Rows[0]);
        }

        /// <inheritdoc />
        public async Task<AuthorizationRoleSearchResult> SearchRoles(AuthorizationRoleSearchRequest search, CancellationToken token = default)
        {
            if (search == null) throw new ArgumentNullException(nameof(search));
            ValidatePaging(search.Page, search.PageSize);
            token.ThrowIfCancellationRequested();

            AuthorizationRoleSearchResult ret = new AuthorizationRoleSearchResult
            {
                Page = search.Page,
                PageSize = search.PageSize
            };

            DataTable countTable = await _Repo.ExecuteQueryAsync(AuthorizationRoleQueries.SearchRoles(search, true), false, token).ConfigureAwait(false);
            ret.TotalCount = CountFromTable(countTable);
            ret.TotalPages = (int)Math.Ceiling((double)ret.TotalCount / search.PageSize);

            DataTable dataTable = await _Repo.ExecuteQueryAsync(AuthorizationRoleQueries.SearchRoles(search, false), false, token).ConfigureAwait(false);
            if (dataTable != null && dataTable.Rows.Count > 0)
            {
                foreach (DataRow row in dataTable.Rows)
                {
                    token.ThrowIfCancellationRequested();
                    ret.Objects.Add(RowToRole(row));
                }
            }

            return ret;
        }

        /// <inheritdoc />
        public async Task<AuthorizationRole> UpdateRole(AuthorizationRole role, CancellationToken token = default)
        {
            if (role == null) throw new ArgumentNullException(nameof(role));
            token.ThrowIfCancellationRequested();

            DataTable result = await _Repo.ExecuteQueryAsync(AuthorizationRoleQueries.UpdateRole(role), true, token).ConfigureAwait(false);
            if (result == null || result.Rows.Count < 1) return null;
            AuthorizationRole updated = RowToRole(result.Rows[0]);
            AuthorizationPolicyChangeTracker.SignalChanged();
            return updated;
        }

        /// <inheritdoc />
        public async Task DeleteRoleByGuid(Guid guid, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            await _Repo.ExecuteQueryAsync(AuthorizationRoleQueries.DeleteRole(guid), true, token).ConfigureAwait(false);
            AuthorizationPolicyChangeTracker.SignalChanged();
        }

        /// <inheritdoc />
        public async Task<UserRoleAssignment> CreateUserRole(UserRoleAssignment assignment, CancellationToken token = default)
        {
            if (assignment == null) throw new ArgumentNullException(nameof(assignment));
            token.ThrowIfCancellationRequested();

            DataTable result = await _Repo.ExecuteQueryAsync(AuthorizationRoleQueries.InsertUserRole(assignment), true, token).ConfigureAwait(false);
            if (result == null || result.Rows.Count < 1) return null;
            UserRoleAssignment created = RowToUserRole(result.Rows[0]);
            AuthorizationPolicyChangeTracker.SignalChanged();
            return created;
        }

        /// <inheritdoc />
        public async Task<UserRoleAssignment> ReadUserRoleByGuid(Guid guid, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();

            DataTable result = await _Repo.ExecuteQueryAsync(AuthorizationRoleQueries.SelectUserRoleByGuid(guid), false, token).ConfigureAwait(false);
            if (result == null || result.Rows.Count < 1) return null;
            return RowToUserRole(result.Rows[0]);
        }

        /// <inheritdoc />
        public async Task<UserRoleAssignmentSearchResult> SearchUserRoles(UserRoleAssignmentSearchRequest search, CancellationToken token = default)
        {
            if (search == null) throw new ArgumentNullException(nameof(search));
            ValidatePaging(search.Page, search.PageSize);
            token.ThrowIfCancellationRequested();

            UserRoleAssignmentSearchResult ret = new UserRoleAssignmentSearchResult
            {
                Page = search.Page,
                PageSize = search.PageSize
            };

            DataTable countTable = await _Repo.ExecuteQueryAsync(AuthorizationRoleQueries.SearchUserRoles(search, true), false, token).ConfigureAwait(false);
            ret.TotalCount = CountFromTable(countTable);
            ret.TotalPages = (int)Math.Ceiling((double)ret.TotalCount / search.PageSize);

            DataTable dataTable = await _Repo.ExecuteQueryAsync(AuthorizationRoleQueries.SearchUserRoles(search, false), false, token).ConfigureAwait(false);
            if (dataTable != null && dataTable.Rows.Count > 0)
            {
                foreach (DataRow row in dataTable.Rows)
                {
                    token.ThrowIfCancellationRequested();
                    ret.Objects.Add(RowToUserRole(row));
                }
            }

            return ret;
        }

        /// <inheritdoc />
        public async Task<UserRoleAssignment> UpdateUserRole(UserRoleAssignment assignment, CancellationToken token = default)
        {
            if (assignment == null) throw new ArgumentNullException(nameof(assignment));
            token.ThrowIfCancellationRequested();

            DataTable result = await _Repo.ExecuteQueryAsync(AuthorizationRoleQueries.UpdateUserRole(assignment), true, token).ConfigureAwait(false);
            if (result == null || result.Rows.Count < 1) return null;
            UserRoleAssignment updated = RowToUserRole(result.Rows[0]);
            AuthorizationPolicyChangeTracker.SignalChanged();
            return updated;
        }

        /// <inheritdoc />
        public async Task DeleteUserRoleByGuid(Guid guid, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            await _Repo.ExecuteQueryAsync(AuthorizationRoleQueries.DeleteUserRole(guid), true, token).ConfigureAwait(false);
            AuthorizationPolicyChangeTracker.SignalChanged();
        }

        /// <inheritdoc />
        public async Task<int> DeleteUserRoles(UserRoleAssignmentSearchRequest search, CancellationToken token = default)
        {
            if (search == null) throw new ArgumentNullException(nameof(search));
            ValidatePaging(search.Page, search.PageSize);
            token.ThrowIfCancellationRequested();

            DataTable countTable = await _Repo.ExecuteQueryAsync(AuthorizationRoleQueries.SearchUserRoles(search, true), false, token).ConfigureAwait(false);
            int count = Convert.ToInt32(CountFromTable(countTable));

            await _Repo.ExecuteQueryAsync(AuthorizationRoleQueries.DeleteUserRoles(search), true, token).ConfigureAwait(false);
            if (count > 0) AuthorizationPolicyChangeTracker.SignalChanged();
            return count;
        }

        /// <inheritdoc />
        public async Task<CredentialScopeAssignment> CreateCredentialScope(CredentialScopeAssignment assignment, CancellationToken token = default)
        {
            if (assignment == null) throw new ArgumentNullException(nameof(assignment));
            token.ThrowIfCancellationRequested();

            DataTable result = await _Repo.ExecuteQueryAsync(AuthorizationRoleQueries.InsertCredentialScope(assignment), true, token).ConfigureAwait(false);
            if (result == null || result.Rows.Count < 1) return null;
            CredentialScopeAssignment created = RowToCredentialScope(result.Rows[0]);
            AuthorizationPolicyChangeTracker.SignalChanged();
            return created;
        }

        /// <inheritdoc />
        public async Task<CredentialScopeAssignment> ReadCredentialScopeByGuid(Guid guid, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();

            DataTable result = await _Repo.ExecuteQueryAsync(AuthorizationRoleQueries.SelectCredentialScopeByGuid(guid), false, token).ConfigureAwait(false);
            if (result == null || result.Rows.Count < 1) return null;
            return RowToCredentialScope(result.Rows[0]);
        }

        /// <inheritdoc />
        public async Task<CredentialScopeAssignmentSearchResult> SearchCredentialScopes(CredentialScopeAssignmentSearchRequest search, CancellationToken token = default)
        {
            if (search == null) throw new ArgumentNullException(nameof(search));
            ValidatePaging(search.Page, search.PageSize);
            token.ThrowIfCancellationRequested();

            CredentialScopeAssignmentSearchResult ret = new CredentialScopeAssignmentSearchResult
            {
                Page = search.Page,
                PageSize = search.PageSize
            };

            DataTable countTable = await _Repo.ExecuteQueryAsync(AuthorizationRoleQueries.SearchCredentialScopes(search, true), false, token).ConfigureAwait(false);
            ret.TotalCount = CountFromTable(countTable);
            ret.TotalPages = (int)Math.Ceiling((double)ret.TotalCount / search.PageSize);

            DataTable dataTable = await _Repo.ExecuteQueryAsync(AuthorizationRoleQueries.SearchCredentialScopes(search, false), false, token).ConfigureAwait(false);
            if (dataTable != null && dataTable.Rows.Count > 0)
            {
                foreach (DataRow row in dataTable.Rows)
                {
                    token.ThrowIfCancellationRequested();
                    ret.Objects.Add(RowToCredentialScope(row));
                }
            }

            return ret;
        }

        /// <inheritdoc />
        public async Task<CredentialScopeAssignment> UpdateCredentialScope(CredentialScopeAssignment assignment, CancellationToken token = default)
        {
            if (assignment == null) throw new ArgumentNullException(nameof(assignment));
            token.ThrowIfCancellationRequested();

            DataTable result = await _Repo.ExecuteQueryAsync(AuthorizationRoleQueries.UpdateCredentialScope(assignment), true, token).ConfigureAwait(false);
            if (result == null || result.Rows.Count < 1) return null;
            CredentialScopeAssignment updated = RowToCredentialScope(result.Rows[0]);
            AuthorizationPolicyChangeTracker.SignalChanged();
            return updated;
        }

        /// <inheritdoc />
        public async Task DeleteCredentialScopeByGuid(Guid guid, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            await _Repo.ExecuteQueryAsync(AuthorizationRoleQueries.DeleteCredentialScope(guid), true, token).ConfigureAwait(false);
            AuthorizationPolicyChangeTracker.SignalChanged();
        }

        /// <inheritdoc />
        public async Task<int> DeleteCredentialScopes(CredentialScopeAssignmentSearchRequest search, CancellationToken token = default)
        {
            if (search == null) throw new ArgumentNullException(nameof(search));
            ValidatePaging(search.Page, search.PageSize);
            token.ThrowIfCancellationRequested();

            DataTable countTable = await _Repo.ExecuteQueryAsync(AuthorizationRoleQueries.SearchCredentialScopes(search, true), false, token).ConfigureAwait(false);
            int count = Convert.ToInt32(CountFromTable(countTable));

            await _Repo.ExecuteQueryAsync(AuthorizationRoleQueries.DeleteCredentialScopes(search), true, token).ConfigureAwait(false);
            if (count > 0) AuthorizationPolicyChangeTracker.SignalChanged();
            return count;
        }

        #endregion

        #region Private-Methods

        private static void ValidatePaging(int page, int pageSize)
        {
            if (page < 0) throw new ArgumentOutOfRangeException(nameof(page));
            if (pageSize < 1) throw new ArgumentOutOfRangeException(nameof(pageSize));
        }

        private static long CountFromTable(DataTable table)
        {
            if (table != null && table.Rows.Count > 0 && table.Columns.Contains("record_count"))
                return Convert.ToInt64(table.Rows[0]["record_count"]);

            return 0;
        }

        private static AuthorizationRole RowToRole(DataRow row)
        {
            AuthorizationRole role = new AuthorizationRole();

            role.GUID = GetGuid(row, "guid");
            role.TenantGUID = GetNullableGuid(row, "tenantguid");
            role.Name = Converters.GetDataRowStringValue(row, "name");
            role.DisplayName = Converters.GetDataRowStringValue(row, "displayname");
            role.Description = Converters.GetDataRowStringValue(row, "description");
            role.BuiltIn = Converters.GetDataRowIntValue(row, "builtin") == 1;
            role.BuiltInRole = GetEnum(row, "builtinrole", BuiltInRoleEnum.Custom);
            role.ResourceScope = GetEnum(row, "resourcescope", AuthorizationResourceScopeEnum.Graph);
            role.Permissions = Converters.GetDataRowJsonListValue<AuthorizationPermissionEnum>(row, "permissions") ?? new List<AuthorizationPermissionEnum>();
            role.ResourceTypes = Converters.GetDataRowJsonListValue<AuthorizationResourceTypeEnum>(row, "resourcetypes") ?? new List<AuthorizationResourceTypeEnum>();
            role.InheritsToGraphs = Converters.GetDataRowIntValue(row, "inheritstographs") == 1;
            role.CreatedUtc = GetUtc(row, "createdutc");
            role.LastUpdateUtc = GetUtc(row, "lastupdateutc");

            return role;
        }

        private static UserRoleAssignment RowToUserRole(DataRow row)
        {
            UserRoleAssignment assignment = new UserRoleAssignment();

            assignment.GUID = GetGuid(row, "guid");
            assignment.TenantGUID = GetGuid(row, "tenantguid");
            assignment.UserGUID = GetGuid(row, "userguid");
            assignment.RoleGUID = GetNullableGuid(row, "roleguid");
            assignment.RoleName = Converters.GetDataRowStringValue(row, "rolename");
            assignment.ResourceScope = GetEnum(row, "resourcescope", AuthorizationResourceScopeEnum.Graph);
            assignment.GraphGUID = GetNullableGuid(row, "graphguid");
            assignment.CreatedUtc = GetUtc(row, "createdutc");
            assignment.LastUpdateUtc = GetUtc(row, "lastupdateutc");

            return assignment;
        }

        private static CredentialScopeAssignment RowToCredentialScope(DataRow row)
        {
            CredentialScopeAssignment assignment = new CredentialScopeAssignment();

            assignment.GUID = GetGuid(row, "guid");
            assignment.TenantGUID = GetGuid(row, "tenantguid");
            assignment.CredentialGUID = GetGuid(row, "credentialguid");
            assignment.RoleGUID = GetNullableGuid(row, "roleguid");
            assignment.RoleName = Converters.GetDataRowStringValue(row, "rolename");
            assignment.ResourceScope = GetEnum(row, "resourcescope", AuthorizationResourceScopeEnum.Graph);
            assignment.GraphGUID = GetNullableGuid(row, "graphguid");
            assignment.Permissions = Converters.GetDataRowJsonListValue<AuthorizationPermissionEnum>(row, "permissions") ?? new List<AuthorizationPermissionEnum>();
            assignment.ResourceTypes = Converters.GetDataRowJsonListValue<AuthorizationResourceTypeEnum>(row, "resourcetypes") ?? new List<AuthorizationResourceTypeEnum>();
            assignment.CreatedUtc = GetUtc(row, "createdutc");
            assignment.LastUpdateUtc = GetUtc(row, "lastupdateutc");

            return assignment;
        }

        private static Guid GetGuid(DataRow row, string column)
        {
            string value = Converters.GetDataRowStringValue(row, column);
            if (!String.IsNullOrEmpty(value) && Guid.TryParse(value, out Guid guid)) return guid;
            return Guid.Empty;
        }

        private static Guid? GetNullableGuid(DataRow row, string column)
        {
            string value = Converters.GetDataRowStringValue(row, column);
            if (!String.IsNullOrEmpty(value) && Guid.TryParse(value, out Guid guid)) return guid;
            return null;
        }

        private static T GetEnum<T>(DataRow row, string column, T defaultValue) where T : struct
        {
            string value = Converters.GetDataRowStringValue(row, column);
            if (!String.IsNullOrEmpty(value) && Enum.TryParse<T>(value, true, out T parsed)) return parsed;
            return defaultValue;
        }

        private static DateTime GetUtc(DataRow row, string column)
        {
            string value = Converters.GetDataRowStringValue(row, column);
            if (!String.IsNullOrEmpty(value) && DateTime.TryParse(value, out DateTime parsed))
                return DateTime.SpecifyKind(parsed, DateTimeKind.Utc);

            return DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc);
        }

        #endregion
    }
}
