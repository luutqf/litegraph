namespace LiteGraph.GraphRepositories
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Data;
    using System.Linq;
    using ExpressionTree;
    using LiteGraph;
    using LiteGraph.GraphRepositories.Interfaces;
    using LiteGraph.Serialization;
    using Microsoft.Data.Sqlite;

    /// <summary>
    /// Graph repository base class.
    /// The graph repository base class is only responsible for primitives.
    /// Validation and cross-cutting functions should be performed in LiteGraphClient rather than in the graph repository base.
    /// </summary>
    public abstract class GraphRepositoryBase : IDisposable
    {
        #region Public-Members

        /// <summary>
        /// Logging.
        /// </summary>
        public LoggingSettings Logging
        {
            get
            {
                return _Logging;
            }
            set
            {
                if (value == null) value = new LoggingSettings();
                _Logging = value;
            }
        }

        /// <summary>
        /// Serializer.
        /// </summary>
        public Serializer Serializer
        {
            get
            {
                return _Serializer;
            }
            set
            {
                if (value == null) throw new ArgumentNullException(nameof(Serializer));
                _Serializer = value;
            }
        }

        /// <summary>
        /// Admin methods.
        /// </summary>
        public abstract IAdminMethods Admin { get; }

        /// <summary>
        /// Tenant methods.
        /// </summary>
        public abstract ITenantMethods Tenant { get; }

        /// <summary>
        /// User methods.
        /// </summary>
        public abstract IUserMethods User { get; }

        /// <summary>
        /// Credential methods.
        /// </summary>
        public abstract ICredentialMethods Credential { get; }

        /// <summary>
        /// Label methods.
        /// </summary>
        public abstract ILabelMethods Label { get; }

        /// <summary>
        /// Tag methods.
        /// </summary>
        public abstract ITagMethods Tag { get; }

        /// <summary>
        /// Vector methods.
        /// </summary>
        public abstract IVectorMethods Vector { get; }

        /// <summary>
        /// Graph methods.
        /// </summary>
        public abstract IGraphMethods Graph { get; }

        /// <summary>
        /// Node methods.
        /// </summary>
        public abstract INodeMethods Node { get; }

        /// <summary>
        /// Edge methods.
        /// </summary>
        public abstract IEdgeMethods Edge { get; }

        /// <summary>
        /// Batch methods.
        /// </summary>
        public abstract IBatchMethods Batch { get; }

        /// <summary>
        /// Vector index methods.
        /// </summary>
        public abstract IVectorIndexMethods VectorIndex { get; }

        /// <summary>
        /// Request history methods.
        /// </summary>
        public abstract IRequestHistoryMethods RequestHistory { get; }

        #endregion

        #region Private-Members

        private LoggingSettings _Logging = new LoggingSettings();
        private Serializer _Serializer = new Serializer();
        private bool _Disposed = false;

        #endregion

        #region Public-Methods

        /// <summary>
        /// Initialize the repository.
        /// </summary>
        public abstract void InitializeRepository();

        /// <summary>
        /// Flush database contents to disk.  Only required if using an in-memory instance of a LiteGraph database.
        /// </summary>
        public abstract void Flush();

        /// <summary>
        /// Dispose resources owned by the repository.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion

        #region Protected-Methods

        /// <summary>
        /// Dispose resources owned by the repository.
        /// </summary>
        /// <param name="disposing">Disposing managed resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            _Disposed = true;
        }

        /// <summary>
        /// Throw if the repository has been disposed.
        /// </summary>
        protected void ThrowIfDisposed()
        {
            if (_Disposed) throw new ObjectDisposedException(GetType().FullName);
        }

        /// <summary>
        /// Indicates whether the repository has been disposed.
        /// </summary>
        protected bool Disposed
        {
            get
            {
                return _Disposed;
            }
        }

        #endregion

        #region Private-Methods

        #endregion
    }
}
