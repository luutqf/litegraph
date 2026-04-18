namespace LiteGraph.GraphRepositories
{
    using System;
    using LiteGraph.GraphRepositories.Interfaces;

    /// <summary>
    /// Provider placeholder used before a full repository implementation exists.
    /// </summary>
    public abstract class UnsupportedGraphRepository : GraphRepositoryBase
    {
        #region Public-Members

        /// <summary>
        /// Provider settings.
        /// </summary>
        public DatabaseSettings Settings { get; }

        /// <inheritdoc />
        public override IAdminMethods Admin { get { throw Unsupported(nameof(Admin)); } }

        /// <inheritdoc />
        public override ITenantMethods Tenant { get { throw Unsupported(nameof(Tenant)); } }

        /// <inheritdoc />
        public override IUserMethods User { get { throw Unsupported(nameof(User)); } }

        /// <inheritdoc />
        public override ICredentialMethods Credential { get { throw Unsupported(nameof(Credential)); } }

        /// <inheritdoc />
        public override ILabelMethods Label { get { throw Unsupported(nameof(Label)); } }

        /// <inheritdoc />
        public override ITagMethods Tag { get { throw Unsupported(nameof(Tag)); } }

        /// <inheritdoc />
        public override IVectorMethods Vector { get { throw Unsupported(nameof(Vector)); } }

        /// <inheritdoc />
        public override IGraphMethods Graph { get { throw Unsupported(nameof(Graph)); } }

        /// <inheritdoc />
        public override INodeMethods Node { get { throw Unsupported(nameof(Node)); } }

        /// <inheritdoc />
        public override IEdgeMethods Edge { get { throw Unsupported(nameof(Edge)); } }

        /// <inheritdoc />
        public override IBatchMethods Batch { get { throw Unsupported(nameof(Batch)); } }

        /// <inheritdoc />
        public override IVectorIndexMethods VectorIndex { get { throw Unsupported(nameof(VectorIndex)); } }

        /// <inheritdoc />
        public override IRequestHistoryMethods RequestHistory { get { throw Unsupported(nameof(RequestHistory)); } }

        /// <inheritdoc />
        public override IAuthorizationAuditMethods AuthorizationAudit { get { throw Unsupported(nameof(AuthorizationAudit)); } }

        /// <inheritdoc />
        public override IAuthorizationRoleMethods AuthorizationRoles { get { throw Unsupported(nameof(AuthorizationRoles)); } }

        #endregion

        #region Private-Members

        private readonly string _ProviderName;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        /// <param name="providerName">Provider name.</param>
        /// <param name="settings">Database settings.</param>
        protected UnsupportedGraphRepository(string providerName, DatabaseSettings settings)
        {
            if (String.IsNullOrWhiteSpace(providerName)) throw new ArgumentNullException(nameof(providerName));
            _ProviderName = providerName;
            Settings = settings?.Clone() ?? throw new ArgumentNullException(nameof(settings));
        }

        #endregion

        #region Public-Methods

        /// <inheritdoc />
        public override void InitializeRepository()
        {
            throw Unsupported(nameof(InitializeRepository));
        }

        /// <inheritdoc />
        public override void Flush()
        {
            throw Unsupported(nameof(Flush));
        }

        #endregion

        #region Protected-Methods

        /// <summary>
        /// Create a provider-specific unsupported-operation exception.
        /// </summary>
        /// <param name="operation">Operation name.</param>
        /// <returns>Exception.</returns>
        protected NotSupportedException Unsupported(string operation)
        {
            return new NotSupportedException(
                _ProviderName
                + " graph repository operation '"
                + operation
                + "' is not implemented yet.");
        }

        #endregion
    }
}
