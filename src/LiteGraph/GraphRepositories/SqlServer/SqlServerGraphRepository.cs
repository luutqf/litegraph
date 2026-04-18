namespace LiteGraph.GraphRepositories.SqlServer
{
    /// <summary>
    /// SQL Server graph repository placeholder.
    /// </summary>
    public sealed class SqlServerGraphRepository : UnsupportedGraphRepository
    {
        /// <summary>
        /// Instantiate.
        /// </summary>
        /// <param name="settings">Database settings.</param>
        public SqlServerGraphRepository(DatabaseSettings settings) : base("SQL Server", settings)
        {
        }
    }
}
