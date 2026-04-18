namespace LiteGraph.GraphRepositories.Postgresql.Implementations
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using LiteGraph.GraphRepositories.Interfaces;
    using LiteGraph.GraphRepositories.Postgresql;

    /// <summary>
    /// Admin methods.
    /// Graph repository base methods are responsible only for primitives, not input validation or cross-cutting.
    /// </summary>
    public class AdminMethods : IAdminMethods
    {
        #region Public-Members

        #endregion

        #region Private-Members

        private PostgresqlGraphRepository _Repo = null;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Admin methods.
        /// </summary>
        /// <param name="repo">Graph repository.</param>
        public AdminMethods(PostgresqlGraphRepository repo)
        {
            _Repo = repo ?? throw new ArgumentNullException(nameof(repo));
        }

        #endregion

        #region Public-Methods

        /// <inheritdoc />
        public async Task Backup(string outputFilename, CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(outputFilename)) throw new ArgumentNullException(nameof(outputFilename));
            token.ThrowIfCancellationRequested();
            await Task.CompletedTask.ConfigureAwait(false);
            throw new NotSupportedException("PostgreSQL repository backup must be performed with PostgreSQL-native tools such as pg_dump.");
        }

        #endregion

        #region Private-Methods

        #endregion
    }
}

