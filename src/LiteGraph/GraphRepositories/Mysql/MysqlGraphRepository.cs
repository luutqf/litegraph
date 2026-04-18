namespace LiteGraph.GraphRepositories.Mysql
{
    /// <summary>
    /// MySQL graph repository placeholder.
    /// </summary>
    public sealed class MysqlGraphRepository : UnsupportedGraphRepository
    {
        /// <summary>
        /// Instantiate.
        /// </summary>
        /// <param name="settings">Database settings.</param>
        public MysqlGraphRepository(DatabaseSettings settings) : base("MySQL", settings)
        {
        }
    }
}
