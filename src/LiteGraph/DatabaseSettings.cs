namespace LiteGraph
{
    using System;

    /// <summary>
    /// Provider-neutral database settings for graph repositories.
    /// </summary>
    public class DatabaseSettings
    {
        #region Public-Members

        /// <summary>
        /// Database provider type.
        /// </summary>
        public DatabaseTypeEnum Type { get; set; } = DatabaseTypeEnum.Sqlite;

        /// <summary>
        /// SQLite database filename.  Preserved for backward-compatible zero-config deployments.
        /// </summary>
        public string Filename
        {
            get
            {
                return _Filename;
            }
            set
            {
                if (String.IsNullOrEmpty(value)) throw new ArgumentNullException(nameof(Filename));
                _Filename = value;
            }
        }

        /// <summary>
        /// Use an in-memory SQLite database and flush it to <see cref="Filename"/> when instructed.
        /// </summary>
        public bool InMemory { get; set; } = false;

        /// <summary>
        /// Provider hostname.
        /// </summary>
        public string Hostname { get; set; } = "localhost";

        /// <summary>
        /// Provider port.
        /// </summary>
        public int? Port
        {
            get
            {
                return _Port;
            }
            set
            {
                if (value != null && (value < 0 || value > 65535)) throw new ArgumentOutOfRangeException(nameof(Port));
                _Port = value;
            }
        }

        /// <summary>
        /// Database name for server-backed providers.
        /// </summary>
        public string DatabaseName { get; set; } = "litegraph";

        /// <summary>
        /// Username for server-backed providers.
        /// </summary>
        public string Username { get; set; } = null;

        /// <summary>
        /// Password for server-backed providers.
        /// </summary>
        public string Password { get; set; } = null;

        /// <summary>
        /// Schema name for providers that support schemas.
        /// </summary>
        public string Schema { get; set; } = "litegraph";

        /// <summary>
        /// Optional provider connection string.
        /// </summary>
        public string ConnectionString { get; set; } = null;

        /// <summary>
        /// Maximum connection pool size for providers that support pooling.
        /// </summary>
        public int MaxConnections
        {
            get
            {
                return _MaxConnections;
            }
            set
            {
                if (value < 1) throw new ArgumentOutOfRangeException(nameof(MaxConnections));
                _MaxConnections = value;
            }
        }

        /// <summary>
        /// Command timeout, in seconds.
        /// </summary>
        public int CommandTimeoutSeconds
        {
            get
            {
                return _CommandTimeoutSeconds;
            }
            set
            {
                if (value < 1) throw new ArgumentOutOfRangeException(nameof(CommandTimeoutSeconds));
                _CommandTimeoutSeconds = value;
            }
        }

        #endregion

        #region Private-Members

        private string _Filename = "litegraph.db";
        private int? _Port = null;
        private int _MaxConnections = 32;
        private int _CommandTimeoutSeconds = 30;

        #endregion

        #region Public-Methods

        /// <summary>
        /// Clone the current settings.
        /// </summary>
        /// <returns>Cloned settings.</returns>
        public DatabaseSettings Clone()
        {
            return new DatabaseSettings
            {
                Type = Type,
                Filename = Filename,
                InMemory = InMemory,
                Hostname = Hostname,
                Port = Port,
                DatabaseName = DatabaseName,
                Username = Username,
                Password = Password,
                Schema = Schema,
                ConnectionString = ConnectionString,
                MaxConnections = MaxConnections,
                CommandTimeoutSeconds = CommandTimeoutSeconds
            };
        }

        /// <summary>
        /// Return a safe description suitable for logs.
        /// </summary>
        /// <returns>Redacted provider description.</returns>
        public string ToSafeString()
        {
            switch (Type)
            {
                case DatabaseTypeEnum.Sqlite:
                    return "Type=Sqlite; Filename=" + Filename + "; InMemory=" + InMemory;
                default:
                    return "Type=" + Type
                        + "; Hostname=" + Hostname
                        + "; Port=" + (Port != null ? Port.Value.ToString() : "(default)")
                        + "; DatabaseName=" + DatabaseName
                        + "; Username=" + (!String.IsNullOrEmpty(Username) ? Username : "(none)")
                        + "; Password=" + (!String.IsNullOrEmpty(Password) ? "***" : "(none)")
                        + "; Schema=" + Schema
                        + "; MaxConnections=" + MaxConnections
                        + "; CommandTimeoutSeconds=" + CommandTimeoutSeconds
                        + "; ConnectionString=" + (!String.IsNullOrEmpty(ConnectionString) ? "***" : "(none)");
            }
        }

        #endregion
    }
}
