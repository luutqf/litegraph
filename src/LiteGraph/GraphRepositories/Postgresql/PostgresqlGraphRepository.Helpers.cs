namespace LiteGraph.GraphRepositories.Postgresql
{
    using System;
    using System.Data;
    using System.Threading;
    using System.Threading.Tasks;
    using LiteGraph.GraphRepositories.Postgresql.Queries;
    using Npgsql;

    public partial class PostgresqlGraphRepository
    {
        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
            if (Disposed) return;

            if (disposing)
            {
                lock (_QueryLock)
                {
                    if (_Transaction != null)
                    {
                        try { _Transaction.Rollback(); } catch { }
                        ClearGraphTransaction();
                    }

                    VectorIndexManager?.Dispose();
                    VectorIndexManager = null;
                    _DataSource?.Dispose();
                }
            }

            base.Dispose(disposing);
        }

        /// <inheritdoc />
        protected override async ValueTask DisposeAsyncCore()
        {
            if (Disposed) return;

            NpgsqlTransaction transaction = null;
            NpgsqlConnection conn = null;

            lock (_QueryLock)
            {
                transaction = _Transaction;
                conn = _TransactionConnection;

                _Transaction = null;
                _TransactionConnection = null;
                _GraphTransactionTenantGUID = null;
                _GraphTransactionGraphGUID = null;
                _GraphTransactionVectorIndexTouched = false;
                _GraphTransactionVectorIndexFailed = false;
                _GraphTransactionVectorIndexDirtyReason = null;
            }

            if (transaction != null)
            {
                try { await transaction.RollbackAsync().ConfigureAwait(false); } catch { }
                try { await transaction.DisposeAsync().ConfigureAwait(false); } catch { }
            }

            if (conn != null)
            {
                try { await conn.CloseAsync().ConfigureAwait(false); } catch { }
                try { await conn.DisposeAsync().ConfigureAwait(false); } catch { }
            }

            VectorIndexManager?.Dispose();
            VectorIndexManager = null;
            await _DataSource.DisposeAsync().ConfigureAwait(false);

            base.Dispose(true);
        }

        private void ClearGraphTransaction()
        {
            NpgsqlTransaction transaction = _Transaction;
            NpgsqlConnection conn = _TransactionConnection;

            _Transaction = null;
            _TransactionConnection = null;
            _GraphTransactionTenantGUID = null;
            _GraphTransactionGraphGUID = null;
            _GraphTransactionVectorIndexTouched = false;
            _GraphTransactionVectorIndexFailed = false;
            _GraphTransactionVectorIndexDirtyReason = null;

            try { transaction?.Dispose(); } catch { }
            try { conn?.Close(); } catch { }
            try { conn?.Dispose(); } catch { }
        }

        private void MarkVectorIndexDirtyAfterTransaction(Guid tenantGuid, Guid graphGuid, string reason)
        {
            try
            {
                ExecuteQuery(GraphQueries.SetVectorIndexDirty(
                    tenantGuid,
                    graphGuid,
                    true,
                    reason ?? "Graph transaction completed with uncertain vector index state"), true);
            }
            catch (Exception e)
            {
                Logging.Log(SeverityEnum.Warn, "failed to mark vector index dirty after graph transaction: " + e.Message);
            }
        }

        private void EnsureBuiltInAuthorizationRoles()
        {
            bool changed = false;

            foreach (RoleDefinition definition in AuthorizationPolicyDefinitions.BuiltInRoles)
            {
                AuthorizationRole role = AuthorizationRole.FromDefinition(definition);
                DataTable existing = ExecuteQuery(AuthorizationRoleQueries.SelectRoleByName(null, role.Name));

                if (existing != null && existing.Rows.Count > 0)
                {
                    DataRow row = existing.Rows[0];
                    string guid = Converters.GetDataRowStringValue(row, "guid");
                    if (!String.IsNullOrEmpty(guid) && Guid.TryParse(guid, out Guid parsedGuid))
                        role.GUID = parsedGuid;

                    string created = Converters.GetDataRowStringValue(row, "createdutc");
                    if (!String.IsNullOrEmpty(created) && DateTime.TryParse(created, out DateTime parsedCreated))
                        role.CreatedUtc = DateTime.SpecifyKind(parsedCreated, DateTimeKind.Utc);

                    ExecuteQuery(AuthorizationRoleQueries.UpdateRole(role), true);
                    changed = true;
                }
                else
                {
                    ExecuteQuery(AuthorizationRoleQueries.InsertRole(role), true);
                    changed = true;
                }
            }

            if (changed) AuthorizationPolicyChangeTracker.SignalChanged();
        }

        private async Task EnsureBuiltInAuthorizationRolesAsync(CancellationToken token)
        {
            bool changed = false;

            foreach (RoleDefinition definition in AuthorizationPolicyDefinitions.BuiltInRoles)
            {
                token.ThrowIfCancellationRequested();
                AuthorizationRole role = AuthorizationRole.FromDefinition(definition);
                DataTable existing = await ExecuteQueryAsync(AuthorizationRoleQueries.SelectRoleByName(null, role.Name), false, token).ConfigureAwait(false);

                if (existing != null && existing.Rows.Count > 0)
                {
                    DataRow row = existing.Rows[0];
                    string guid = Converters.GetDataRowStringValue(row, "guid");
                    if (!String.IsNullOrEmpty(guid) && Guid.TryParse(guid, out Guid parsedGuid))
                        role.GUID = parsedGuid;

                    string created = Converters.GetDataRowStringValue(row, "createdutc");
                    if (!String.IsNullOrEmpty(created) && DateTime.TryParse(created, out DateTime parsedCreated))
                        role.CreatedUtc = DateTime.SpecifyKind(parsedCreated, DateTimeKind.Utc);

                    await ExecuteQueryAsync(AuthorizationRoleQueries.UpdateRole(role), true, token).ConfigureAwait(false);
                    changed = true;
                }
                else
                {
                    await ExecuteQueryAsync(AuthorizationRoleQueries.InsertRole(role), true, token).ConfigureAwait(false);
                    changed = true;
                }
            }

            if (changed) AuthorizationPolicyChangeTracker.SignalChanged();
        }

        private static NpgsqlConnectionStringBuilder BuildConnectionString(DatabaseSettings settings)
        {
            NpgsqlConnectionStringBuilder builder = !String.IsNullOrWhiteSpace(settings.ConnectionString)
                ? new NpgsqlConnectionStringBuilder(settings.ConnectionString)
                : new NpgsqlConnectionStringBuilder
                {
                    Host = settings.Hostname,
                    Database = settings.DatabaseName
                };

            if (settings.Port.HasValue) builder.Port = settings.Port.Value;
            if (!String.IsNullOrWhiteSpace(settings.Username)) builder.Username = settings.Username;
            if (!String.IsNullOrWhiteSpace(settings.Password)) builder.Password = settings.Password;
            builder.MaxPoolSize = settings.MaxConnections;
            builder.CommandTimeout = settings.CommandTimeoutSeconds;
            builder.Pooling = true;
            return builder;
        }

        private static string NormalizeSchema(string schema)
        {
            if (String.IsNullOrWhiteSpace(schema)) return "litegraph";
            foreach (char c in schema)
            {
                if (!(Char.IsLetterOrDigit(c) || c == '_'))
                    throw new ArgumentException("PostgreSQL schema names may contain only letters, digits, and underscores.", nameof(schema));
            }
            return schema;
        }

        internal static string QuoteIdentifier(string identifier)
        {
            return "\"" + identifier.Replace("\"", "\"\"") + "\"";
        }
    }
}
