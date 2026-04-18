namespace LiteGraph.GraphRepositories.Postgresql.Implementations
{
    using System;
    using System.Data;
    using System.Threading;
    using System.Threading.Tasks;
    using LiteGraph.GraphRepositories.Interfaces;
    using LiteGraph.GraphRepositories.Postgresql.Queries;

    /// <summary>
    /// Authorization audit methods.
    /// Graph repository base methods are responsible only for primitives, not input validation or cross-cutting.
    /// </summary>
    public class AuthorizationAuditMethods : IAuthorizationAuditMethods
    {
        #region Public-Members

        #endregion

        #region Private-Members

        private readonly PostgresqlGraphRepository _Repo = null;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Authorization audit methods.
        /// </summary>
        /// <param name="repo">Graph repository.</param>
        public AuthorizationAuditMethods(PostgresqlGraphRepository repo)
        {
            _Repo = repo ?? throw new ArgumentNullException(nameof(repo));
        }

        #endregion

        #region Public-Methods

        /// <inheritdoc />
        public async Task Insert(AuthorizationAuditEntry entry, CancellationToken token = default)
        {
            if (entry == null) throw new ArgumentNullException(nameof(entry));
            token.ThrowIfCancellationRequested();
            await _Repo.ExecuteQueryAsync(AuthorizationAuditQueries.Insert(entry), true, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<AuthorizationAuditEntry> ReadByGuid(Guid guid, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            DataTable result = await _Repo.ExecuteQueryAsync(AuthorizationAuditQueries.SelectByGuid(guid), false, token).ConfigureAwait(false);
            if (result == null || result.Rows.Count < 1) return null;
            return RowToEntry(result.Rows[0]);
        }

        /// <inheritdoc />
        public async Task<AuthorizationAuditSearchResult> Search(AuthorizationAuditSearchRequest search, CancellationToken token = default)
        {
            if (search == null) throw new ArgumentNullException(nameof(search));
            token.ThrowIfCancellationRequested();

            AuthorizationAuditSearchResult ret = new AuthorizationAuditSearchResult
            {
                Page = search.Page,
                PageSize = search.PageSize
            };

            DataTable countTable = await _Repo.ExecuteQueryAsync(AuthorizationAuditQueries.Search(search, true), false, token).ConfigureAwait(false);
            if (countTable != null && countTable.Rows.Count > 0 && countTable.Columns.Contains("record_count"))
                ret.TotalCount = Convert.ToInt64(countTable.Rows[0]["record_count"]);

            ret.TotalPages = (int)Math.Ceiling((double)ret.TotalCount / search.PageSize);

            DataTable dataTable = await _Repo.ExecuteQueryAsync(AuthorizationAuditQueries.Search(search, false), false, token).ConfigureAwait(false);
            if (dataTable != null && dataTable.Rows.Count > 0)
            {
                foreach (DataRow row in dataTable.Rows)
                {
                    token.ThrowIfCancellationRequested();
                    ret.Objects.Add(RowToEntry(row));
                }
            }

            return ret;
        }

        /// <inheritdoc />
        public async Task DeleteByGuid(Guid guid, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            await _Repo.ExecuteQueryAsync(AuthorizationAuditQueries.Delete(guid), true, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<int> DeleteMany(AuthorizationAuditSearchRequest search, CancellationToken token = default)
        {
            if (search == null) throw new ArgumentNullException(nameof(search));
            token.ThrowIfCancellationRequested();

            DataTable countTable = await _Repo.ExecuteQueryAsync(AuthorizationAuditQueries.Search(search, true), false, token).ConfigureAwait(false);
            int count = 0;
            if (countTable != null && countTable.Rows.Count > 0 && countTable.Columns.Contains("record_count"))
                count = Convert.ToInt32(countTable.Rows[0]["record_count"]);

            await _Repo.ExecuteQueryAsync(AuthorizationAuditQueries.DeleteMany(search), true, token).ConfigureAwait(false);
            return count;
        }

        /// <inheritdoc />
        public async Task<int> DeleteOlderThan(DateTime cutoffUtc, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();

            AuthorizationAuditSearchRequest search = new AuthorizationAuditSearchRequest
            {
                ToUtc = cutoffUtc,
                PageSize = 1
            };

            return await DeleteMany(search, token).ConfigureAwait(false);
        }

        #endregion

        #region Private-Methods

        private static AuthorizationAuditEntry RowToEntry(DataRow row)
        {
            AuthorizationAuditEntry entry = new AuthorizationAuditEntry();

            string guidStr = Converters.GetDataRowStringValue(row, "guid");
            if (!String.IsNullOrEmpty(guidStr) && Guid.TryParse(guidStr, out Guid guid)) entry.GUID = guid;

            string createdStr = Converters.GetDataRowStringValue(row, "createdutc");
            if (!String.IsNullOrEmpty(createdStr) && DateTime.TryParse(createdStr, out DateTime created))
                entry.CreatedUtc = DateTime.SpecifyKind(created, DateTimeKind.Utc);

            entry.RequestId = Converters.GetDataRowStringValue(row, "requestid");
            entry.CorrelationId = Converters.GetDataRowStringValue(row, "correlationid");
            entry.TraceId = Converters.GetDataRowStringValue(row, "traceid");
            entry.TenantGUID = GetNullableGuid(row, "tenantguid");
            entry.GraphGUID = GetNullableGuid(row, "graphguid");
            entry.UserGUID = GetNullableGuid(row, "userguid");
            entry.CredentialGUID = GetNullableGuid(row, "credentialguid");
            entry.RequestType = Converters.GetDataRowStringValue(row, "requesttype");
            entry.Method = Converters.GetDataRowStringValue(row, "method");
            entry.Path = Converters.GetDataRowStringValue(row, "path");
            entry.SourceIp = Converters.GetDataRowStringValue(row, "sourceip");
            entry.AuthenticationResult = Converters.GetDataRowStringValue(row, "authenticationresult");
            entry.AuthorizationResult = Converters.GetDataRowStringValue(row, "authorizationresult");
            entry.Reason = Converters.GetDataRowStringValue(row, "reason");
            entry.RequiredScope = Converters.GetDataRowStringValue(row, "requiredscope");
            entry.IsAdmin = Converters.GetDataRowIntValue(row, "isadmin") == 1;
            entry.StatusCode = Converters.GetDataRowIntValue(row, "statuscode");
            entry.Description = Converters.GetDataRowStringValue(row, "description");

            return entry;
        }

        private static Guid? GetNullableGuid(DataRow row, string column)
        {
            string value = Converters.GetDataRowStringValue(row, column);
            if (!String.IsNullOrEmpty(value) && Guid.TryParse(value, out Guid guid)) return guid;
            return null;
        }

        #endregion
    }
}

