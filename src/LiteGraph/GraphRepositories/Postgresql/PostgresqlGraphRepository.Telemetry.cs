namespace LiteGraph.GraphRepositories.Postgresql
{
    using System;
    using System.Data;
    using System.Diagnostics;
    using System.Threading.Tasks;

    public partial class PostgresqlGraphRepository
    {
        private DataTable ExecuteRepositoryOperation(string operation, bool isTransaction, int statementCount, Func<DataTable> action)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            using Activity activity = StartRepositoryOperationActivity(operation, isTransaction, statementCount);

            try
            {
                DataTable result = action();
                stopwatch.Stop();
                CompleteRepositoryOperation(activity, operation, isTransaction, statementCount, result?.Rows.Count ?? 0, stopwatch.Elapsed.TotalMilliseconds, true, null);
                return result;
            }
            catch (Exception e)
            {
                stopwatch.Stop();
                CompleteRepositoryOperation(activity, operation, isTransaction, statementCount, 0, stopwatch.Elapsed.TotalMilliseconds, false, e);
                throw;
            }
        }

        private async Task<DataTable> ExecuteRepositoryOperationAsync(string operation, bool isTransaction, int statementCount, Func<Task<DataTable>> action)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            using Activity activity = StartRepositoryOperationActivity(operation, isTransaction, statementCount);

            try
            {
                DataTable result = await action().ConfigureAwait(false);
                stopwatch.Stop();
                CompleteRepositoryOperation(activity, operation, isTransaction, statementCount, result?.Rows.Count ?? 0, stopwatch.Elapsed.TotalMilliseconds, true, null);
                return result;
            }
            catch (Exception e)
            {
                stopwatch.Stop();
                CompleteRepositoryOperation(activity, operation, isTransaction, statementCount, 0, stopwatch.Elapsed.TotalMilliseconds, false, e);
                throw;
            }
        }

        private Activity StartRepositoryOperationActivity(string operation, bool isTransaction, int statementCount)
        {
            Activity activity = LiteGraphTelemetry.ActivitySource.StartActivity(LiteGraphTelemetry.RepositoryOperationActivityName, ActivityKind.Client);
            if (activity == null) return null;

            activity.SetTag("db.system", "postgresql");
            activity.SetTag("litegraph.repository.provider", ProviderName);
            activity.SetTag("litegraph.repository.operation", operation);
            activity.SetTag("litegraph.repository.transactional", isTransaction || _Transaction != null);
            activity.SetTag("litegraph.repository.statement_count", statementCount < 0 ? 0 : statementCount);
            return activity;
        }

        private void CompleteRepositoryOperation(
            Activity activity,
            string operation,
            bool isTransaction,
            int statementCount,
            int rowCount,
            double durationMs,
            bool success,
            Exception exception)
        {
            if (activity != null)
            {
                activity.SetTag("litegraph.repository.success", success);
                activity.SetTag("litegraph.repository.rows", rowCount);
                activity.SetTag("litegraph.repository.duration_ms", durationMs);

                if (success) LiteGraphTelemetry.SetActivityOk(activity);
                else LiteGraphTelemetry.SetActivityException(activity, exception);
            }

            LiteGraphTelemetry.RecordRepositoryOperation(new RepositoryOperationTelemetryEventArgs(
                ProviderName,
                operation,
                success,
                isTransaction || _Transaction != null,
                statementCount,
                rowCount,
                durationMs));
        }

        private static string ClassifySqlOperation(string query, bool isTransaction)
        {
            if (isTransaction) return "transaction";
            if (String.IsNullOrWhiteSpace(query)) return "unknown";

            string trimmed = query.TrimStart();
            int length = 0;
            while (length < trimmed.Length && !Char.IsWhiteSpace(trimmed[length]) && trimmed[length] != ';')
            {
                length++;
            }

            if (length < 1) return "unknown";

            string verb = trimmed.Substring(0, length).Trim().ToUpperInvariant();
            switch (verb)
            {
                case "SELECT":
                case "WITH":
                    return "read";
                case "INSERT":
                case "UPDATE":
                case "DELETE":
                case "CREATE":
                case "DROP":
                case "ALTER":
                case "REPLACE":
                case "BEGIN":
                case "COMMIT":
                case "END":
                    return "write";
                default:
                    return verb.ToLowerInvariant();
            }
        }
    }
}
